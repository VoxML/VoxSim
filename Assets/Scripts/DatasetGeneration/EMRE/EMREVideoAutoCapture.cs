using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

using FlashbackVideoRecorder;
using SQLite4Unity3d;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.VideoCapture;

public enum EMREReferringModality {
	Linguistic,
	Gestural,
	Ensemble
};

public class EMREVideoDBEntry {
	[PrimaryKey, AutoIncrement]
    public int Id { get; set; }
	public string FilePath { get; set; }
	public string FocusObj { get; set; }
	public string ObjCoords { get; set; }
	public string RefModality { get; set; }
	public string DescriptionStr { get; set; }
	public string RelationSet { get; set; }
	public float ObjDistToAgent { get; set; }
	public int DistanceDistinction { get; set; }
	public string DistDistinctionType { get; set; }
	public string RelationalDescriptors { get; set; }
}

public class EMREVideoAutoCapture : MonoBehaviour {
	public int situations;
	public List<EMREReferringModality> referringModalities;
	public int situationIndex = 0;
	public int expModalityIndex = 0;
	public int focusObjIndex = 0;
	public bool captureExample = false;

	public KeyCode startCaptureKey;
	public KeyCode stopCaptureKey;

	FlashbackRecorder recorder;
	InputController inputController;
	EventManager eventManager;
	ObjectSelector objSelector;
	CommunicationsBridge commBridge;
	Predicates preds;
	ReferringExpressionGenerator refExpGenerator;

	public double eventTimeoutTime = 10000.0f;
	Timer eventTimeoutTimer;

	public double intervalWaitTime = 1000.0f;
	Timer intervalWaitTimer;

	bool initialWaitComplete = false;

	bool captureVideo, captureParams;

	bool capturing = false;
	bool writingFile = false;
	bool stopCaptureFlag = false;

	VideoCaptureMode captureMode;
	VideoCaptureFilenameType filenameScheme;
	bool sortByEvent;
	string filenamePrefix;
	string dbFile;
	string inputFile;
	int eventIndex;
	string videoDir;

	int eventsExecuted = 0;

	SQLiteConnection dbConnection;
	EMREVideoDBEntry dbEntry;
	string outFileName = string.Empty;

	public event EventHandler FileWritten;

	public void OnFileWritten(object sender, EventArgs e) {
		if (FileWritten != null) {
			FileWritten(this, e);
		}
	}

	// Use this for initialization
	void Start() {
		recorder = gameObject.GetComponent<FlashbackRecorder>();
		inputController = GameObject.Find("IOController").GetComponent<InputController>();
		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
		preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();

		refExpGenerator = GameObject.Find("ReferringExpressionGenerator")
			.GetComponent<ReferringExpressionGenerator>();

		captureVideo = (PlayerPrefs.GetInt("Capture Video") == 1);
		captureParams = (PlayerPrefs.GetInt("Capture Params") == 1);
		captureMode = (VideoCaptureMode) PlayerPrefs.GetInt("Video Capture Mode");
		filenameScheme = (VideoCaptureFilenameType) PlayerPrefs.GetInt("Video Capture Filename Type");
		sortByEvent = (PlayerPrefs.GetInt("Sort By Event String") == 1);
		filenamePrefix = PlayerPrefs.GetString("Custom Video Filename Prefix");
		dbFile = PlayerPrefs.GetString("Video Capture DB");
		videoDir = PlayerPrefs.GetString("Video Output Directory");

		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (videoDir != string.Empty) {
			recorder.SetOutputDirectory(Path.GetFullPath(Application.dataPath + videoDir));
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			refExpGenerator.ItemsSituated += StartCaptureProcess;

			intervalWaitTimer = new Timer(intervalWaitTime);
			intervalWaitTimer.Enabled = false;
			intervalWaitTimer.Elapsed += WaitComplete;

			eventTimeoutTimer = new Timer(eventTimeoutTime);
			eventTimeoutTimer.Enabled = false;
			eventTimeoutTimer.Elapsed += StopCapture;

			FileWritten += CaptureComplete;
		}
	}

	void StartCaptureProcess(object sender, EventArgs e) {
		eventTimeoutTimer.Interval = eventTimeoutTime;
		eventTimeoutTimer.Enabled = true;
		expModalityIndex = 0;
		refExpGenerator.world.interactionPrefs.gesturalReference =
			(referringModalities[expModalityIndex] == EMREReferringModality.Gestural ||
			 referringModalities[expModalityIndex] == EMREReferringModality.Ensemble);
		refExpGenerator.world.interactionPrefs.linguisticReference =
			(referringModalities[expModalityIndex] == EMREReferringModality.Linguistic ||
			 referringModalities[expModalityIndex] == EMREReferringModality.Ensemble);
		focusObjIndex = 0;
		captureExample = true;
	}

	// Update is called once per frame
	void Update() {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (captureExample) {
			StartCapture(null, null);
			intervalWaitTimer.Enabled = true;
			captureExample = false;
		}

		if (initialWaitComplete) {
			intervalWaitTimer.Interval = intervalWaitTime;
			intervalWaitTimer.Enabled = false;
			Debug.Log(string.Format("Focusing object {0}", focusObjIndex));
			refExpGenerator.OnObjectSelected(this,
				new SelectionEventArgs(refExpGenerator.world.availableObjs[focusObjIndex]));
			initialWaitComplete = false;
		}

		if (stopCaptureFlag) {
			dbEntry.FilePath = outFileName;
			dbEntry.FocusObj = refExpGenerator.focusObj.name;
			dbEntry.ObjCoords = String.Join("\n",
				refExpGenerator.objSelector.allVoxemes.Select(
						v => string.Format("{0}:{1}", v.name, Helper.VectorToParsable(v.transform.position)))
					.ToArray());
			dbEntry.RefModality = referringModalities[expModalityIndex].ToString();
			dbEntry.DescriptionStr = refExpGenerator.fullDesc;
			dbEntry.RelationSet = String.Join("\n", refExpGenerator.relationTracker.relStrings.ToArray());
			dbEntry.ObjDistToAgent =
				Vector3.Distance(refExpGenerator.agent.transform.position,
					refExpGenerator.focusObj.transform.position);
			dbEntry.DistanceDistinction = Convert.ToInt32(refExpGenerator.distanceDistinction);
			dbEntry.DistDistinctionType = refExpGenerator.distanceDistinction
				? (refExpGenerator.relativeDistance ? "Relative" : "Absolute")
				: null;
			dbEntry.RelationalDescriptors = String.Join("\n", refExpGenerator.descriptors.ToArray());

			SaveCapture();
			stopCaptureFlag = false;
		}

		if ((writingFile) && (recorder.GetNumberOfPendingFiles() == 0)) {
			Debug.Log("File written to disk.");
			OnFileWritten(this, null);
			writingFile = false;
		}

		if (captureMode == VideoCaptureMode.Manual) {
			if ((!capturing) && (!writingFile)) {
				if (Input.GetKeyDown(startCaptureKey)) {
					StartCapture(null, null);
				}
			}

			if (!writingFile) {
				if (Input.GetKeyDown(stopCaptureKey)) {
					StopCapture(null, null);
				}
			}
		}
	}

	void StartCapture(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (filenameScheme == VideoCaptureFilenameType.EventString) {
			outFileName = string.Format("{0}-{1}", (((InputEventArgs) e).InputString).Replace(" ", "_"),
				DateTime.Now.ToString("yyyy-MM-dd-HHmmss"));

			if (sortByEvent) {
				outFileName = string.Format("{0}/{1}", (((InputEventArgs) e).InputString).Replace(" ", "_"),
					outFileName);
			}
		}
		else {
			outFileName = string.Format("{0}-{1}", filenamePrefix, DateTime.Now.ToString("yyyy-MM-dd-HHmmss"));
		}

		if (dbFile != string.Empty) {
			OpenDB();
			dbEntry = new EMREVideoDBEntry();
		}

		if (!capturing) {
			recorder.StartCapture();
			Debug.Log("Starting video capture...");

			capturing = true;
			stopCaptureFlag = false;
		}
	}

	void WaitComplete(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		initialWaitComplete = true;
	}

	void SaveCapture() {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			eventTimeoutTimer.Enabled = false;
			eventTimeoutTimer.Interval = eventTimeoutTime;

			intervalWaitTimer.Enabled = false;
			intervalWaitTimer.Interval = intervalWaitTime;
		}

		recorder.StopCapture();

		if (filenameScheme == VideoCaptureFilenameType.FlashbackDefault) {
			recorder.SaveCapturedFrames();

			if (dbFile != string.Empty) {
				dbEntry.FilePath = "Flashback_" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
			}
		}
		else {
			recorder.SaveCapturedFrames(outFileName);
		}

		if (dbFile != string.Empty) {
			WriteToDB();
		}

		capturing = false;
		writingFile = true;

		Debug.Log("Stopping video capture.");
	}

	void StopCapture(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		if (capturing) {
			stopCaptureFlag = true;
		}
	}

	void CaptureComplete(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (situationIndex >= situations) {
			return;
		}

		expModalityIndex++;
		if (expModalityIndex >= referringModalities.Count) {
			expModalityIndex = 0;
			focusObjIndex++;
			if (focusObjIndex >= refExpGenerator.world.availableObjs.Count) {
				focusObjIndex = 0;
				refExpGenerator.OnPlaceObjects(this, null);
				situationIndex++;
			}
		}

		refExpGenerator.world.interactionPrefs.gesturalReference =
			(referringModalities[expModalityIndex] == EMREReferringModality.Gestural ||
			 referringModalities[expModalityIndex] == EMREReferringModality.Ensemble);
		refExpGenerator.world.interactionPrefs.linguisticReference =
			(referringModalities[expModalityIndex] == EMREReferringModality.Linguistic ||
			 referringModalities[expModalityIndex] == EMREReferringModality.Ensemble);

		eventTimeoutTimer.Interval = eventTimeoutTime;
		eventTimeoutTimer.Enabled = true;
		captureExample = true;
	}

	void OpenDB() {
		dbConnection = new SQLiteConnection(
			string.Format("{0}.db", Path.GetFullPath(Application.dataPath + dbFile)),
			SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

		dbConnection.CreateTable<EMREVideoDBEntry>();
	}

	void WriteToDB() {
		if (dbEntry != null) {
			dbConnection.InsertAll(new[] {dbEntry});
		}
	}
}