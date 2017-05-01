using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Global;
using Network;
using VideoCapture;

public class Launcher : FontManager {
	public int fontSize = 12;

	string ip;
	string csuUrl;
	string parserUrl;
	string inPort;
	string sriUrl;
	bool makeLogs;
	bool captureVideo;
	VideoCaptureMode videoCaptureMode;
	VideoCaptureFilenameType prevVideoCaptureFilenameType;
	VideoCaptureFilenameType videoCaptureFilenameType;
	string customVideoFilenamePrefix;
	bool sortByEventString;
	bool captureParams;
	bool resetScene;
	string eventResetCounter;
	string autoEventsList;
	string startIndex;
	string captureDB;
	string videoOutputDir;
	bool editableVoxemes;
	bool eulaAccepted;

	EULAModalWindow eulaWindow;

	string ioPrefsPath = "";

	int bgLeft = Screen.width/6;
	int bgTop = Screen.height/12;
	int bgWidth = 4*Screen.width/6;
	int bgHeight = 10*Screen.height/12;
	int margin;
	
	Vector2 masterScrollPosition;
	Vector2 sceneBoxScrollPosition;
	
	string[] listItems;
	
	List<string> availableScenes = new List<string>();
	
	int selected = -1;
	string sceneSelected = "";
	
	object[] scenes;
	
	GUIStyle customStyle;

	private GUIStyle labelStyle;
	private GUIStyle textFieldStyle;
	private GUIStyle buttonStyle;

	float fontSizeModifier;
	
	// Use this for initialization
	void Start () {
		fontSizeModifier = (fontSize / defaultFontSize);
		LoadPrefs ();
		
#if UNITY_EDITOR
		string scenesDirPath = Application.dataPath + "/Scenes/";
		string [] fileEntries = Directory.GetFiles(Application.dataPath+"/Scenes/","*.unity");
		foreach (string s in fileEntries) {
			string sceneName = s.Remove(0,scenesDirPath.Length).Replace(".unity","");
			if (!sceneName.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name)) {
				availableScenes.Add(sceneName);
			}
		}
#endif 
#if UNITY_STANDALONE || UNITY_WEBPLAYER
		TextAsset scenesList = (TextAsset)Resources.Load("ScenesList", typeof(TextAsset));
		string[] scenes = scenesList.text.Split ('\n');
		foreach (string s in scenes) {
			if (s.Length > 0) {
				availableScenes.Add(s);
			}
		}
#endif

		listItems = availableScenes.ToArray ();

		// get IP address
		foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
			if (ipAddress.AddressFamily.ToString() == "InterNetwork") {
				//Debug.Log(ipAddress.ToString());
				ip = ipAddress.ToString ();
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	void OnGUI () {
		labelStyle = new GUIStyle ("Label");
		textFieldStyle = new GUIStyle ("TextField");
		buttonStyle = new GUIStyle ("Button");
		bgLeft = Screen.width/6;
		bgTop = Screen.height/12;
		bgWidth = 4*Screen.width/6;
		bgHeight = 10*Screen.height/12;
		margin = 0;
		
		GUI.Box (new Rect (bgLeft, bgTop, bgWidth, bgHeight), "");

		masterScrollPosition = GUI.BeginScrollView (new Rect(bgLeft + 5, bgTop + 5, bgWidth - 10, bgHeight - 70), masterScrollPosition,
			new Rect(bgLeft + margin, bgTop + 5, bgWidth - 10, bgHeight - 70));

		GUI.Label (new Rect (bgLeft + 10, bgTop + 35, 90*fontSizeModifier, 25*fontSizeModifier), "Listener Port");
		inPort = GUI.TextField (new Rect (bgLeft+100, bgTop+35, 60, 25*fontSizeModifier), inPort);

		GUI.Button (new Rect (bgLeft + 165, bgTop + 35, 10, 10), new GUIContent ("*", "IP: " + ip));
		if (GUI.tooltip != string.Empty) {
			GUI.TextArea (new Rect (bgLeft + 175, bgTop + 35, GUI.skin.label.CalcSize (new GUIContent ("IP: "+ip)).x+10, 20), GUI.tooltip);
		}

		GUI.Label (new Rect (bgLeft + 10, bgTop + 65, 90*fontSizeModifier, 25*fontSizeModifier), "SRI URL");
		sriUrl = GUI.TextField (new Rect (bgLeft+100, bgTop+65, 150, 25*fontSizeModifier), sriUrl);

		GUI.Label (new Rect (bgLeft + 10, bgTop + 95, 90*fontSizeModifier, 25*fontSizeModifier), "Make Logs");
		makeLogs = GUI.Toggle (new Rect (bgLeft+100, bgTop+95, 150, 25*fontSizeModifier), makeLogs, string.Empty);

		GUI.Label (new Rect (bgLeft + 10, bgTop + 125, 90*fontSizeModifier, 40*fontSizeModifier), "Parser URL");
		parserUrl = GUI.TextField (new Rect (bgLeft+100, bgTop+125, 150, 25*fontSizeModifier), parserUrl);
		GUI.Label (new Rect (bgLeft + 10, bgTop + 150, 300, 50), "(Leave empty to use simple regex parser)");

		GUI.Label (new Rect (bgLeft + 10, bgTop + 180, 90*fontSizeModifier, 25*fontSizeModifier), "CSU URL");
		csuUrl = GUI.TextField (new Rect (bgLeft+100, bgTop+180, 150, 25*fontSizeModifier), csuUrl);

		GUI.Label (new Rect (bgLeft + 10, bgTop + 210, 90*fontSizeModifier, 25*fontSizeModifier), "Capture Video");
		captureVideo = GUI.Toggle (new Rect (bgLeft+100, bgTop+210, 20, 25*fontSizeModifier), captureVideo, string.Empty);

		if (captureVideo) {
			captureParams = false;
		}

		GUI.Label (new Rect (bgLeft + 135, bgTop + 210, 150*fontSizeModifier, 25*fontSizeModifier), "Capture Params");
		captureParams = GUI.Toggle (new Rect (bgLeft+235, bgTop+210, 20, 25*fontSizeModifier), captureParams, string.Empty);

		if (captureParams) {
			captureVideo = false;
		}

		if (captureVideo) {
			string warningText = "Enabling this option may affect performance";
			GUI.TextArea (new Rect (bgLeft + 10, bgTop + 235, GUI.skin.label.CalcSize (new GUIContent (warningText)).x + 10, 20 * fontSizeModifier),
				warningText);

			GUI.Label (new Rect (bgLeft + 15, bgTop + 260, GUI.skin.label.CalcSize (new GUIContent ("Video Capture Mode")).x + 10, 20 * fontSizeModifier),
				"Video Capture Mode");

			string[] videoCaptureModeLabels = new string[]{ "Manual", "Full-Time", "Per Event" };
			videoCaptureMode = (VideoCaptureMode)GUI.SelectionGrid (
				new Rect (bgLeft + 15, bgTop + 280, 150, 20 * fontSizeModifier * videoCaptureModeLabels.Length),
				(int)videoCaptureMode, videoCaptureModeLabels, 1, "toggle");

			if (videoCaptureMode == VideoCaptureMode.PerEvent) {
				GUI.Label (new Rect (bgLeft + 40, bgTop + 340, 130 * fontSizeModifier, 20 * fontSizeModifier), "Reset Scene Between");
				GUI.Label (new Rect (bgLeft + 40, bgTop + 355, 130 * fontSizeModifier, 20 * fontSizeModifier), "Every");
				resetScene = GUI.Toggle (new Rect (bgLeft + 25, bgTop + 347, 20, 25 * fontSizeModifier), resetScene, string.Empty);
				if (resetScene) {
					eventResetCounter = Regex.Replace (GUI.TextField (new Rect (bgLeft + 15 + 62 * fontSizeModifier, bgTop + 355, 25, 20 * fontSizeModifier),
						eventResetCounter), @"[^0-9]", "");
				}
				else {
					eventResetCounter = Regex.Replace (GUI.TextField (new Rect (bgLeft + 15 + 62 * fontSizeModifier, bgTop + 355, 25, 20 * fontSizeModifier),
						eventResetCounter), @"[^0-9]", "");
				}
				GUI.Label (new Rect (bgLeft + 15 + 90 * fontSizeModifier, bgTop + 355, 130 * fontSizeModifier, 20 * fontSizeModifier), "Events");

				GUI.Label (new Rect (bgLeft + 15, bgTop + 380, 130 * fontSizeModifier, 25 * fontSizeModifier), "Auto-Input Script");
				autoEventsList = GUI.TextField (new Rect (bgLeft + 140 * fontSizeModifier, bgTop + 380, 150, 25 * fontSizeModifier), autoEventsList);
				GUI.Label (new Rect (bgLeft + 290 * fontSizeModifier, bgTop + 380, 30 * fontSizeModifier, 25 * fontSizeModifier), ".py : ");
				startIndex = Regex.Replace (GUI.TextField (new Rect (bgLeft + 320 * fontSizeModifier, bgTop + 380, 40, 20 * fontSizeModifier),
					startIndex), @"[^0-9]", "");
				GUI.Label (new Rect (bgLeft + 15, bgTop + 405, 300, 50), "(Leave empty to input events manually)");
			}

			GUI.Label (new Rect (bgLeft + 15 + 160 * fontSizeModifier, bgTop + 260, GUI.skin.label.CalcSize (new GUIContent ("Capture Filename Type")).x + 10, 20 * fontSizeModifier),
				"Capture Filename Type");

			//prevVideoCaptureFilenameType = videoCaptureFilenameType;
			string[] videoCaptureFilenameTypeLabels = new string[]{ "Flashback Default", "Event String", "Custom" };
			videoCaptureFilenameType = (VideoCaptureFilenameType)GUI.SelectionGrid (
				new Rect (bgLeft + 15 + 160 * fontSizeModifier, bgTop + 280, 150, 20 * fontSizeModifier * videoCaptureFilenameTypeLabels.Length),
				(int)videoCaptureFilenameType, videoCaptureFilenameTypeLabels, 1, "toggle");

			// EventString can only be used with PerEvent
			if (videoCaptureMode != VideoCaptureMode.PerEvent) {
				if (videoCaptureFilenameType == VideoCaptureFilenameType.EventString) {
					videoCaptureFilenameType = VideoCaptureFilenameType.FlashbackDefault;
				}
			}

			if (videoCaptureFilenameType == VideoCaptureFilenameType.EventString) {
				GUI.Label (new Rect (bgLeft + 30 + 170 * fontSizeModifier, bgTop + 340, 120 * fontSizeModifier, 40 * fontSizeModifier), "Sort Videos By Event String");
				sortByEventString = GUI.Toggle (new Rect (bgLeft + 15 + 170 * fontSizeModifier, bgTop + 347, 150, 25 * fontSizeModifier), sortByEventString, string.Empty);
			}
			else if (videoCaptureFilenameType == VideoCaptureFilenameType.Custom) {
				customVideoFilenamePrefix = GUI.TextArea (new Rect (bgLeft + 15 + 170 * fontSizeModifier, bgTop + 345, 150, 25 * fontSizeModifier),
					customVideoFilenamePrefix);
			}

			GUI.Label (new Rect (bgLeft + 15, bgTop + 380 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 120 * fontSizeModifier, 25 * fontSizeModifier), "Video Output Folder");
			videoOutputDir = GUI.TextField (new Rect (bgLeft + 140 * fontSizeModifier, bgTop + 380 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 150, 25 * fontSizeModifier), videoOutputDir);

			GUI.Label (new Rect (bgLeft + 15, bgTop + 410 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 120 * fontSizeModifier, 25 * fontSizeModifier), "Video Database File");
			captureDB = GUI.TextField (new Rect (bgLeft + 140 * fontSizeModifier, bgTop + 410 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 150, 25 * fontSizeModifier), captureDB);
			GUI.Label (new Rect (bgLeft + 290 * fontSizeModifier, bgTop + 410 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 25 * fontSizeModifier, 25 * fontSizeModifier), ".db");
			GUI.Label (new Rect (bgLeft + 15, bgTop + 435 + (60 * System.Convert.ToSingle ((videoCaptureMode == VideoCaptureMode.PerEvent))), 300, 50), "(Leave empty to omit video info from database)");
		}
		else if (captureParams) {
			GUI.Label (new Rect (bgLeft + 30, bgTop + 235, 130 * fontSizeModifier, 20 * fontSizeModifier), "Reset Scene Between");
			GUI.Label (new Rect (bgLeft + 30, bgTop + 250, 130 * fontSizeModifier, 20 * fontSizeModifier), "Every");
			resetScene = GUI.Toggle (new Rect (bgLeft + 15, bgTop + 242, 20, 25 * fontSizeModifier), resetScene, string.Empty);
			if (resetScene) {
				eventResetCounter = Regex.Replace (GUI.TextField (new Rect (bgLeft + 15 + 52 * fontSizeModifier, bgTop + 250, 25, 20 * fontSizeModifier),
					eventResetCounter), @"[^0-9]", "");
			}
			else {
				eventResetCounter = Regex.Replace (GUI.TextField (new Rect (bgLeft + 15 + 52 * fontSizeModifier, bgTop + 250, 25, 20 * fontSizeModifier),
					eventResetCounter), @"[^0-9]", "");
			}
			GUI.Label (new Rect (bgLeft + 15 + 90 * fontSizeModifier, bgTop + 250, 130 * fontSizeModifier, 20 * fontSizeModifier), "Events");

			GUI.Label (new Rect (bgLeft + 15, bgTop + 275, 130 * fontSizeModifier, 25 * fontSizeModifier), "Auto-Input Script");
			autoEventsList = GUI.TextField (new Rect (bgLeft + 140 * fontSizeModifier, bgTop + 275, 150, 25 * fontSizeModifier), autoEventsList);
			GUI.Label (new Rect (bgLeft + 290 * fontSizeModifier, bgTop + 275, 30 * fontSizeModifier, 25 * fontSizeModifier), ".py : ");
			startIndex = Regex.Replace (GUI.TextField (new Rect (bgLeft + 320 * fontSizeModifier, bgTop + 275, 40, 20 * fontSizeModifier),
				startIndex), @"[^0-9]", "");
			GUI.Label (new Rect (bgLeft + 15, bgTop + 300, 300, 50), "(Leave empty to input events manually)");

			GUI.Label (new Rect (bgLeft + 15, bgTop + 335, 120 * fontSizeModifier, 25 * fontSizeModifier), "Capture Database");
			captureDB = GUI.TextField (new Rect (bgLeft + 140 * fontSizeModifier, bgTop + 335 , 150, 25 * fontSizeModifier), captureDB);
			GUI.Label (new Rect (bgLeft + 290 * fontSizeModifier, bgTop + 335, 25 * fontSizeModifier, 25 * fontSizeModifier), ".db");
			GUI.Label (new Rect (bgLeft + 15, bgTop + 360, 300, 50), "(Leave empty to omit param info from database)");
		}

		GUILayout.BeginArea(new Rect(13*Screen.width/24, bgTop + 35, 3*Screen.width/12, 3*Screen.height/6), GUI.skin.window);
		sceneBoxScrollPosition = GUILayout.BeginScrollView(sceneBoxScrollPosition, false, false); 
		GUILayout.BeginVertical(GUI.skin.box);
		
		customStyle = GUI.skin.button;
		//customStyle.active.background = Texture2D.whiteTexture;
		//customStyle.onActive.background = Texture2D.whiteTexture;
		//customStyle.active.textColor = Color.black;
		//customStyle.onActive.textColor = Color.black;
		
		selected = GUILayout.SelectionGrid(selected, listItems, 1, customStyle, GUILayout.ExpandWidth(true));
		
		if (selected >= 0) {
			sceneSelected = listItems [selected];
		}
		
		GUILayout.EndVertical();
		GUILayout.EndScrollView();
		GUILayout.EndArea();

		GUI.Label (new Rect (13*Screen.width/24, bgTop + 35 + (3*Screen.height/6) + 10*fontSizeModifier, 150*fontSizeModifier, 25*fontSizeModifier), "Make Voxemes Editable");
		editableVoxemes = GUI.Toggle (new Rect ((13*Screen.width/24) + (150*fontSizeModifier), bgTop + 35 + (3*Screen.height/6) + 10*fontSizeModifier, 150, 25*fontSizeModifier), editableVoxemes, string.Empty);
		
		Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent("Scenes"));
		
		GUI.Label (new Rect (2*Screen.width/3 - textDimensions.x/2, bgTop + 35, textDimensions.x, 25), "Scenes");
		GUI.EndScrollView ();

		GUI.Label (new Rect (bgLeft + 10, bgTop + bgHeight - 90, 90*fontSizeModifier, 25*fontSizeModifier), "External Prefs");
		ioPrefsPath = GUI.TextField (new Rect (bgLeft+100, bgTop + bgHeight - 90, 150, 25*fontSizeModifier), ioPrefsPath);

		if (GUI.Button (new Rect (bgLeft + 10, bgTop + bgHeight - 60, 100, 20), "Export Prefs")) {
#if UNITY_STANDALONE_OSX
			ExportPrefs ("../../" + ioPrefsPath);
#else
			ExportPrefs (ioPrefsPath);
#endif
		}

		if (GUI.Button (new Rect (bgLeft + 10, bgTop + bgHeight - 30, 100, 20), "Import Prefs")) {
#if UNITY_STANDALONE_OSX
			ImportPrefs ("../../" + ioPrefsPath);
#else
			ImportPrefs (ioPrefsPath);
#endif
		}

		if (GUI.Button (new Rect ((Screen.width / 2 - 50) - 125, bgTop + bgHeight - 60, 100, 50), "Revert Prefs")) {
			LoadPrefs ();
		}

		if (GUI.Button (new Rect (Screen.width / 2 - 50, bgTop + bgHeight - 60, 100, 50), "Save Prefs")) {
			SavePrefs ();
		}

		if (GUI.Button (new Rect ((Screen.width / 2 - 50) + 125, bgTop + bgHeight - 60, 100, 50), "Save & Launch")) {
			if (sceneSelected != "") {
				SavePrefs ();

				if (eulaAccepted) {
					StartCoroutine (SceneHelper.LoadScene (sceneSelected));
				}
				else {
					PopUpEULAWindow ();
				}
			}
		}

		textDimensions = GUI.skin.label.CalcSize (new GUIContent ("VoxSim"));
		GUI.Label (new Rect (((2 * bgLeft + bgWidth) / 2) - textDimensions.x / 2, bgTop, textDimensions.x, 25), "VoxSim");
	}

	void PopUpEULAWindow () {
		eulaWindow = gameObject.AddComponent<EULAModalWindow> ();
		eulaWindow.windowRect = new Rect (bgLeft + 25 , bgTop + 25, bgWidth - 50, bgHeight - 50);
		eulaWindow.windowTitle = "VoxSim End User License Agreement";
		eulaWindow.Render = true;
		eulaWindow.AllowDrag = false;
		eulaWindow.AllowResize = false;
	}

	void EULAAccepted(bool accepted) {
		eulaAccepted = accepted;
		PlayerPrefs.SetInt("EULA Accepted", System.Convert.ToInt32(eulaAccepted));
	}

	void LoadPrefs() {
		inPort = PlayerPrefs.GetString("Listener Port");
		sriUrl = PlayerPrefs.GetString("SRI URL");
		parserUrl = PlayerPrefs.GetString("Parser URL");
		csuUrl = PlayerPrefs.GetString("CSU URL");
		makeLogs = (PlayerPrefs.GetInt("Make Logs") == 1);
		captureVideo = (PlayerPrefs.GetInt("Capture Video") == 1);
		captureParams = (PlayerPrefs.GetInt("Capture Params") == 1);
		videoCaptureMode = (VideoCaptureMode)PlayerPrefs.GetInt("Video Capture Mode");
		resetScene = (PlayerPrefs.GetInt("Reset Between Events") == 1);
		eventResetCounter = PlayerPrefs.GetInt ("Event Reset Counter").ToString ();
		videoCaptureFilenameType = (VideoCaptureFilenameType)PlayerPrefs.GetInt("Video Capture Filename Type");
		sortByEventString = (PlayerPrefs.GetInt("Sort By Event String") == 1);
		customVideoFilenamePrefix = PlayerPrefs.GetString("Custom Video Filename Prefix");
		autoEventsList = PlayerPrefs.GetString("Auto Events List");
		startIndex = PlayerPrefs.GetInt("Start Index").ToString();
		captureDB = PlayerPrefs.GetString("Video Capture DB");
		videoOutputDir = PlayerPrefs.GetString("Video Output Directory");
		editableVoxemes = (PlayerPrefs.GetInt("Make Voxemes Editable") == 1);
		eulaAccepted = (PlayerPrefs.GetInt("EULA Accepted") == 1);
	}

	void ImportPrefs(string path) {
		string line;
		using (StreamReader inputFile = new StreamReader (Path.GetFullPath (Application.dataPath + "/" + path))) {
			while ((line = inputFile.ReadLine ()) != null) { 
				switch (line.Split (',') [0]) {
				case "Listener Port":
					inPort = line.Split (',') [1].Trim();
					break;
				
				case "SRI URL":
					sriUrl = line.Split (',') [1].Trim();
					break;
				
				case "CSU URL":
					csuUrl = line.Split (',') [1].Trim();
					break;

				case "Parser URL":
					parserUrl = line.Split (',') [1].Trim();
					break;
				
				case "Make Logs":
					makeLogs = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;
				
				case "Capture Video":
					captureVideo = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;

				case "Capture Params":
					captureParams = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;
				
				case "Video Capture Mode":
					videoCaptureMode = (VideoCaptureMode)System.Convert.ToInt32(line.Split (',') [1].Trim());
					break;
				
				case "Reset Between Events":
					resetScene = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;
				
				case "Event Reset Counter":
					eventResetCounter = line.Split (',') [1].Trim();
					break;
				
				case "Video Capture Filename Type":
					videoCaptureFilenameType = (VideoCaptureFilenameType)System.Convert.ToInt32(line.Split (',') [1].Trim());
					break;
				
				case "Sort By Event String":
					sortByEventString = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;
				
				case "Custom Video Filename Prefix":
					customVideoFilenamePrefix = line.Split (',') [1].Trim();
					break;
				
				case "Auto Events List":
					autoEventsList = line.Split (',') [1].Trim();
					break;
				
				case "Start Index":
					startIndex = line.Split (',') [1].Trim();
					break;
				
				case "Video Capture DB":
					captureDB = line.Split (',') [1].Trim();
					break;
				
				case "Video Output Directory":
					videoOutputDir = line.Split (',') [1].Trim();
					break;

				case "Make Voxemes Editable":
					editableVoxemes = System.Convert.ToBoolean(line.Split (',') [1].Trim());
					break;

				default:
					break;
				}
			}
		}
	}

	void ExportPrefs(string path) {
		SavePrefs ();
		if ((eventResetCounter == string.Empty) || (eventResetCounter == "0")) {
			eventResetCounter = "1";
		}

		if (startIndex == string.Empty) {
			startIndex = "0";
		}

		Dictionary<string, object> prefsDict = new Dictionary<string, object> ();
		prefsDict.Add ("Listener Port", PlayerPrefs.GetString ("Listener Port"));
		prefsDict.Add ("SRI URL", PlayerPrefs.GetString ("SRI URL"));
		prefsDict.Add ("CSU URL", PlayerPrefs.GetString ("CSU URL"));
		prefsDict.Add ("Parser URL", PlayerPrefs.GetString ("Parser URL"));
		prefsDict.Add ("Make Logs", (PlayerPrefs.GetInt ("Make Logs") == 1));
		prefsDict.Add ("Capture Video", (PlayerPrefs.GetInt ("Capture Video") == 1));
		prefsDict.Add ("Capture Params", (PlayerPrefs.GetInt ("Capture Params") == 1));
		prefsDict.Add ("Video Capture Mode", PlayerPrefs.GetInt ("Video Capture Mode"));
		prefsDict.Add ("Reset Between Events", (PlayerPrefs.GetInt ("Reset Between Events") == 1));
		prefsDict.Add ("Event Reset Counter", PlayerPrefs.GetInt ("Event Reset Counter").ToString ());
		prefsDict.Add ("Video Capture Filename Type", PlayerPrefs.GetInt ("Video Capture Filename Type"));
		prefsDict.Add ("Sort By Event String", (PlayerPrefs.GetInt ("Sort By Event String") == 1));
		prefsDict.Add ("Custom Video Filename Prefix", PlayerPrefs.GetString ("Custom Video Filename Prefix"));
		prefsDict.Add ("Auto Events List", PlayerPrefs.GetString ("Auto Events List"));
		prefsDict.Add ("Start Index", PlayerPrefs.GetInt ("Start Index").ToString ());
		prefsDict.Add ("Video Capture DB", PlayerPrefs.GetString("Video Capture DB"));
		prefsDict.Add ("Video Output Directory", PlayerPrefs.GetString("Video Output Directory"));
		prefsDict.Add ("Make Voxemes Editable", (PlayerPrefs.GetInt("Make Voxemes Editable") == 1));

		using (StreamWriter outputFile = new StreamWriter (Path.GetFullPath (Application.dataPath + "/" + path))) {
			foreach (var entry in prefsDict) {
				outputFile.WriteLine (string.Format("{0},{1}",entry.Key,entry.Value));
			}
		}
	}
	
	void SavePrefs() {
		if ((eventResetCounter == string.Empty) || (eventResetCounter == "0")) {
			eventResetCounter = "1";
		}

		if (startIndex == string.Empty) {
			startIndex = "0";
		}

		PlayerPrefs.SetString("Listener Port", inPort);
		PlayerPrefs.SetString("SRI URL", sriUrl);
		PlayerPrefs.SetString("CSU URL", csuUrl);
		PlayerPrefs.SetString("Parser URL", parserUrl);
		PlayerPrefs.SetInt("Make Logs", System.Convert.ToInt32(makeLogs));
		PlayerPrefs.SetInt("Capture Video", System.Convert.ToInt32(captureVideo));
		PlayerPrefs.SetInt("Capture Params", System.Convert.ToInt32(captureParams));
		PlayerPrefs.SetInt("Video Capture Mode", System.Convert.ToInt32(videoCaptureMode));
		PlayerPrefs.SetInt("Reset Between Events", System.Convert.ToInt32(resetScene));
		PlayerPrefs.SetInt("Event Reset Counter", System.Convert.ToInt32(eventResetCounter));
		PlayerPrefs.SetInt("Video Capture Filename Type", System.Convert.ToInt32(videoCaptureFilenameType));
		PlayerPrefs.SetInt("Sort By Event String", System.Convert.ToInt32(sortByEventString));
		PlayerPrefs.SetString("Custom Video Filename Prefix", customVideoFilenamePrefix);
		PlayerPrefs.SetString("Auto Events List", autoEventsList);
		PlayerPrefs.SetInt("Start Index", System.Convert.ToInt32(startIndex));
		PlayerPrefs.SetString("Video Capture DB", captureDB);
		PlayerPrefs.SetString("Video Output Directory", videoOutputDir);
		PlayerPrefs.SetInt("Make Voxemes Editable", System.Convert.ToInt32(editableVoxemes));
	}
}

