using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;

using FlashbackVideoRecorder;
using SQLite4Unity3d;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.VideoCapture;

public class UnderspecificationVideoDBEntry {
	[PrimaryKey, AutoIncrement]
    public int Id { get; set; }
	public string FilePath { get; set; }
	public string InputString { get; set; }
	public string Parse { get; set; }
	public string ObjectResolvedParse { get; set; }
	public string EventPredicate { get; set; }
	public string Objects { get; set; }
	public string ParameterValues { get; set; }

	public override string ToString() {
		string head = string.Format("[VideoDBEntry: Id={0}," +
		                            " FilePath={1}," +
		                            " InputString={2}," +
		                            " Parse={3}," +
		                            " ObjectResolvedParse={4}," +
		                            " EventPredicate={5},", Id, FilePath, InputString, Parse, ObjectResolvedParse,
			EventPredicate);
		string valueContent = "";
		string tail = "]";
		return head + valueContent + tail;
	}
}

public class UnderspecificationVideoAutoCapture : MonoBehaviour {
	public KeyCode startCaptureKey;
	public KeyCode stopCaptureKey;

	public List<GameObject> availableObjs;

	FlashbackRecorder recorder;
	InputController inputController;
	EventManager eventManager;
	ObjectSelector objSelector;
	CommunicationsBridge commBridge;
	Predicates preds;

	public double eventTimeoutTime = 15000.0f;
	Timer eventTimeoutTimer;

	public double eventCompleteWaitTime = 1000.0f;
	Timer eventCompleteWaitTimer;

	bool initialWaitComplete = false;

	bool captureVideo, captureParams;

	bool capturing = false;
	bool writingFile = false;
	bool stopCaptureFlag = false;

	VideoCaptureMode captureMode;
	bool resetScene;
	int eventResetCounter;
    VideoCaptureFilenameType filenameScheme;
	bool sortByEvent;
	string filenamePrefix;
	string dbFile;
	string inputFile;
	int eventIndex;
	string videoDir;

	int eventsExecuted = 0;

	SQLiteConnection dbConnection;
	UnderspecificationVideoDBEntry dbEntry;
	string outFileName = string.Empty;

	public event EventHandler FileWritten;

	public void OnFileWritten(object sender, EventArgs e) {
		if (FileWritten != null) {
			FileWritten(this, e);
		}
	}

	List<GameObject> eventObjs;

	Dictionary<string, string> paramValues;

	// Use this for initialization
	void Start() {
		recorder = gameObject.GetComponent<FlashbackRecorder>();
		inputController = GameObject.Find("IOController").GetComponent<InputController>();
		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
		preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();

		captureVideo = (PlayerPrefs.GetInt("Capture Video") == 1);
		captureParams = (PlayerPrefs.GetInt("Capture Params") == 1);
		captureMode = (VideoCaptureMode) PlayerPrefs.GetInt("Video Capture Mode");
		resetScene = (PlayerPrefs.GetInt("Reset Between Events") == 1);
		eventResetCounter = PlayerPrefs.GetInt("Event Reset Counter");
		filenameScheme = (VideoCaptureFilenameType) PlayerPrefs.GetInt("Video Capture Filename Type");
		sortByEvent = (PlayerPrefs.GetInt("Sort By Event String") == 1);
		filenamePrefix = PlayerPrefs.GetString("Custom Video Filename Prefix");
		dbFile = PlayerPrefs.GetString("Video Capture DB");
		inputFile = PlayerPrefs.GetString("Auto Events List");
		eventIndex = PlayerPrefs.GetInt("Start Index");
		videoDir = PlayerPrefs.GetString("Video Output Directory");

		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (videoDir != string.Empty) {
			recorder.SetOutputDirectory(Path.GetFullPath(Application.dataPath + videoDir));
		}

		InitObjectDisabling();

		eventObjs = new List<GameObject>();

		if (captureMode == VideoCaptureMode.PerEvent) {
            // TODO: Default Listener Port no longer active, switch to Commander
			commBridge.PortOpened += StartAutoInput;

			inputController.InputReceived += PrepareScene;
			inputController.InputReceived += InputStringReceived;
			inputController.ParseComplete += ParseReceived;

			eventManager.ObjectsResolved += ObjectsResolved;
			eventManager.ObjectsResolved += FilterSpecifiedManner;
			eventManager.ObjectsResolved += EnableAffectedObjects;
			eventManager.ObjectsResolved += InsertWaitPeriod;
			//eventManager.ObjectsResolved += StartCapture;
			eventManager.SatisfactionCalculated += SatisfactionCalculated;
			eventManager.SatisfactionCalculated += EnableAffectedObjects;
			eventManager.SatisfactionCalculated += InsertWaitPeriod;
			//eventManager.SatisfactionCalculated += StartCapture;
			//eventManager.ExecuteEvent += EnableAffectedObjects;
			//eventManager.EventInserted += EventInserted;
			eventManager.ExecuteEvent += StartCapture;
			eventManager.QueueEmpty += EventComplete;

			preds.waitTimer.Elapsed += WaitComplete;
			preds.PrepareLog += PrepareLog;
			preds.ParamsCalculated += ParametersCalculated;

			eventTimeoutTimer = new Timer(eventTimeoutTime);
			eventTimeoutTimer.Enabled = false;
			eventTimeoutTimer.Elapsed += EventTimedOut;
			eventTimeoutTimer.Elapsed += StopCapture;

			eventCompleteWaitTimer = new Timer(eventCompleteWaitTime);
			eventCompleteWaitTimer.Enabled = false;
			eventCompleteWaitTimer.Elapsed += StopCapture;

			FileWritten += CaptureComplete;
		}
	}

	void StartAutoInput(object sender, EventArgs e) {
		if (inputFile != string.Empty) {
			string fullPath = Path.GetFullPath(Application.dataPath + inputFile);
			if (File.Exists(fullPath + ".py")) {
				string[] args = new string[] {
					fullPath.Remove(fullPath.LastIndexOf('/') + 1),
					inputFile.Substring(inputFile.LastIndexOf('/') + 1), eventIndex.ToString(),
					PlayerPrefs.GetString("Listener Port")
				};
				string result = Marshal.PtrToStringAuto(CommunicationsBridge.PythonCall(
					Application.dataPath + "/Externals/python/", "auto_event_script", "send_next_event_to_port",
					args, args.Length));
				eventIndex = Convert.ToInt32(result);
			}
			else {
				Debug.Log(string.Format("File {0} does not exist!",
					Path.GetFullPath(Application.dataPath + inputFile)));
			}
		}
	}

	// Update is called once per frame
	void Update() {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if (stopCaptureFlag) {
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

	public void InitObjectDisabling() {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		for (int i = 0; i < availableObjs.Count; i++) {
			availableObjs[i] = Helper.GetMostImmediateParentVoxeme(availableObjs[i]);
		}
	}

	void DisableObjects() {
		if (!captureVideo) {
			return;
		}

		foreach (GameObject go in availableObjs) {
			//eventManager.InsertEvent (string.Format("disable({0})",go.name), 0);
			preds.DISABLE(new GameObject[] {go});
		}
	}

	void EnableObjects() {
		if (!captureVideo) {
			return;
		}

		foreach (GameObject go in availableObjs) {
			//eventManager.InsertEvent (string.Format("disable({0})",go.name), 0);
			preds.ENABLE(new GameObject[] {go});
		}
	}

	void InputStringReceived(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		initialWaitComplete = false;

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
			dbEntry = new UnderspecificationVideoDBEntry();
			dbEntry.FilePath = outFileName;
			dbEntry.InputString = ((InputEventArgs) e).InputString;
		}
	}

	void ParseReceived(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		string parse = ((InputEventArgs) e).InputString;

		if (dbEntry != null) {
			dbEntry.Parse = parse;

			dbEntry.EventPredicate = Helper.GetTopPredicate(dbEntry.Parse);
		}

		paramValues = UnderspecifiedPredicateParameters.InitPredicateParametersCollection();
	}

	void PrepareScene(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			if ((resetScene) && (eventsExecuted % eventResetCounter == 0)) {
				EnableObjects();
				GameObject.Find("VoxWorld").GetComponent<ObjectSelector>().SendMessage("ResetScene");
				DisableObjects();
				eventObjs.Clear();
			}
		}
	}

	void StartCapture(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		//if (initialWaitComplete) {
		if (!capturing) {
//					if (captureMode == VideoCaptureMode.PerEvent) {
//						eventManager.InsertEvent ("wait()", 0);
//
//						eventTimeoutTimer.Enabled = true;
//					}

			recorder.StartCapture();
			Debug.Log("Starting video capture...");

			capturing = true;
			stopCaptureFlag = false;
		}

		//}
	}

	void InsertWaitPeriod(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			if ((!initialWaitComplete) && (eventManager.events[0] != "wait()")) {
				eventManager.InsertEvent("wait()", 0);
				eventTimeoutTimer.Enabled = true;
			}
			else {
				eventManager.InsertEvent("wait()", 1);
			}
		}
	}

	void WaitComplete(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		initialWaitComplete = true;
	}

	void EnableAffectedObjects(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			foreach (GameObject obj in eventObjs) {
				preds.ENABLE(new GameObject[] {obj});
			}
		}
	}

	void SaveCapture() {
		if (!captureVideo) {
			return;
		}

		if (captureMode == VideoCaptureMode.PerEvent) {
			eventTimeoutTimer.Enabled = false;
			eventTimeoutTimer.Interval = eventTimeoutTime;

			eventCompleteWaitTimer.Enabled = false;
			eventCompleteWaitTimer.Interval = eventCompleteWaitTime;
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

		paramValues.Clear();

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

	void EventTimedOut(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		eventManager.AbortEvent();
	}

	void EventComplete(object sender, EventArgs e) {
		if (!captureVideo) {
			return;
		}

		if (capturing) {
			eventCompleteWaitTimer.Enabled = true;
		}

		eventsExecuted++;
	}

	void ObjectsResolved(object sender, EventArgs e) {
		if (dbEntry != null) {
			dbEntry.ObjectResolvedParse = ((EventManagerArgs) e).EventString;

			string[] constituents = dbEntry.ObjectResolvedParse.Split(new char[] {'(', ',', ')'});
			List<String> argsStrings = new List<string>();

			foreach (string constituent in constituents) {
				if ((GameObject.Find(constituent) != null) ||
				    (objSelector.disabledObjects.Find(o => o.name == constituent) != null)) {
					string objName = GameObject.Find(constituent) != null
						? GameObject.Find(constituent).name
						: objSelector.disabledObjects.Find(o => o.name == constituent).name;

					if (!argsStrings.Contains(constituent)) {
						argsStrings.Add(constituent);
					}
				}
			}

			dbEntry.Objects = string.Join(", ", argsStrings.ToArray());
		}
	}

	void FilterSpecifiedManner(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		string specifiedEvent = UnderspecifiedPredicateParameters.FilterSpecifiedManner(((EventManagerArgs) e).EventString);

		AddConstituentObjectsToAffectedList(specifiedEvent);

		if (specifiedEvent != ((EventManagerArgs) e).EventString) {
			paramValues["MotionManner"] = specifiedEvent;

			if ((captureVideo) && (!captureParams)) {
				eventManager.InsertEvent(specifiedEvent, 1);
				eventManager.RemoveEvent(0);
				if (captureMode == VideoCaptureMode.PerEvent) {
					eventManager.InsertEvent("wait()", 0);
				}
			}
			else if ((captureParams) && (!captureVideo)) {
				eventManager.InsertEvent(specifiedEvent, 1);
				eventManager.AbortEvent();
			}
		}
	}

	void SatisfactionCalculated(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		AddConstituentObjectsToAffectedList(((EventManagerArgs) e).EventString);
	}

	void PrepareLog(object sender, EventArgs e) {
		paramValues[((ParamsEventArgs) e).KeyValue.Key] = ((ParamsEventArgs) e).KeyValue.Value;
	}

	void ParametersCalculated(object sender, EventArgs e) {
		Dictionary<string, string> values = paramValues.Where(kvp => kvp.Value != string.Empty)
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
//			foreach (string key in values.Keys) {
//				Debug.Log (key + " " + values [key]);
//			}
//			Debug.Break ();
		//dbEntry.ParameterValues = Helper.SerializeObjectToBinary (values);

		if (dbEntry != null) {
			dbEntry.ParameterValues = Helper.SerializeObjectToJSON(new PredicateParametersJSON(values));
		}

		if ((captureParams) && (!captureVideo)) {
			if (dbFile != string.Empty) {
				WriteToDB();
			}

			eventManager.AbortEvent();
			paramValues.Clear();
			GameObject.Find("VoxWorld").GetComponent<ObjectSelector>().SendMessage("ResetScene");
			CaptureComplete(null, null);
		}
	}

	void CaptureComplete(object sender, EventArgs e) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		if ((eventIndex != -1) && (inputFile != string.Empty)) {
			string fullPath = Path.GetFullPath(Application.dataPath + inputFile);
			if (File.Exists(fullPath + ".py")) {
				string[] args = new string[] {
					fullPath.Remove(fullPath.LastIndexOf('/') + 1),
					inputFile.Substring(inputFile.LastIndexOf('/') + 1), eventIndex.ToString(),
					PlayerPrefs.GetString("Listener Port")
				};
				string result = Marshal.PtrToStringAuto(CommunicationsBridge.PythonCall(
					Application.dataPath + "/Externals/python/", "auto_event_script", "send_next_event_to_port",
					args, args.Length));
				eventIndex = Convert.ToInt32(result);
			}
			else {
				Debug.Log(string.Format("File {0} does not exist!",
					Path.GetFullPath(Application.dataPath + inputFile)));
			}
		}
		else {
			EnableObjects();
			GameObject.Find("VoxWorld").GetComponent<ObjectSelector>().SendMessage("ResetScene");
		}
	}

	void AddConstituentObjectsToAffectedList(string eventForm) {
		if ((!captureVideo) && (!captureParams)) {
			return;
		}

		string[] constituents = eventForm.Split(new char[] {'(', ',', ')'});

		foreach (string constituent in constituents) {
			if ((GameObject.Find(constituent) != null) ||
			    (objSelector.disabledObjects.Find(o => o.name == constituent) != null)) {
				GameObject obj = GameObject.Find(constituent) != null
					? GameObject.Find(constituent)
					: objSelector.disabledObjects.Find(o => o.name == constituent);

				if (!eventObjs.Contains(obj)) {
					eventObjs.Add(obj);
				}
			}
		}
	}

	void OpenDB() {
		dbConnection = new SQLiteConnection(
			string.Format("{0}.db", Path.GetFullPath(Application.dataPath + dbFile)),
			SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);

		dbConnection.CreateTable<UnderspecificationVideoDBEntry>();
	}

	void WriteToDB() {
		if (dbEntry != null) {
			dbConnection.InsertAll(new[] {dbEntry});
		}
	}
}