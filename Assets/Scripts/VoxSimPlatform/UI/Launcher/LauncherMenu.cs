using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.UI.UIButtons;
using VoxSimPlatform.VideoCapture;

namespace VoxSimPlatform {
    namespace UI {
        namespace Launcher {
            public class VoxSimUserPrefs {
                public bool MakeLogs = false;
                public string LogsPrefix = string.Empty;
                public bool ActionOnlyLogs = false;
                public bool FullStateInfo = false;
                public bool LogTimestamps = false;
                public VoxSimSocketConfig SocketConfig = null;
                public CapturePrefs CapturePrefs = null;
                public bool MakeVoxemesEditable = false;

                public VoxSimUserPrefs() {
                    CapturePrefs = new CapturePrefs();
                    SocketConfig = new VoxSimSocketConfig();
                }
            }

            public class CapturePrefs {
                public bool CaptureVideo = false;
                public bool CaptureParams = false;
                public string VideoCaptureMode = string.Empty;
                public bool ResetBetweenEvents = false;
                public int EventResetCounter = 0;
                public string VideoCaptureFilenameType = string.Empty;
                public bool SortByEventString = false;
                public string CustomVideoFilenamePrefix = string.Empty;
                public string AutoEventsList = string.Empty;
                public int StartIndex = 0;
                public string VideoCaptureDB = string.Empty;
                public string VideoOutputDirectory = string.Empty;
            }

            public class LauncherMenu : FontManager {
            	public int fontSize = 12;

            	string launcherTitle = "VoxSim";

            	[HideInInspector] 
                public string ip;

            	[HideInInspector] 
                public string ipContent = "IP";

            	[HideInInspector] 
                public string inPort;

            	[HideInInspector] 
                public int numUrls = 0;
            	int addUrl = -1;
            	List<int> removeUrl = new List<int>();

            	[HideInInspector] 
                public List<string> urlLabels = new List<string>();

                [HideInInspector] 
                public List<string> urlTypes = new List<string>();

            	[HideInInspector] 
                public List<string> urls = new List<string>();

                [HideInInspector] 
                public List<bool> urlActiveStatuses = new List<bool>();

            	[HideInInspector] 
                public bool makeLogs;

            	[HideInInspector] 
                public string logsPrefix;

            	[HideInInspector]
                public bool actionsOnly;

            	[HideInInspector]
                public bool fullState;

            	[HideInInspector]
                public bool logTimestamps;

            	[HideInInspector] 
                public bool captureVideo;

            	[HideInInspector] 
                public VideoCaptureMode videoCaptureMode;

            	[HideInInspector] 
                public VideoCaptureFilenameType prevVideoCaptureFilenameType;

            	[HideInInspector] 
                public VideoCaptureFilenameType videoCaptureFilenameType;

            	[HideInInspector] 
                public string customVideoFilenamePrefix;

            	[HideInInspector] 
                public bool sortByEventString;

            	[HideInInspector] 
                public bool captureParams;

            	[HideInInspector] 
                public bool resetScene;

            	[HideInInspector] 
                public string eventResetCounter;

            	[HideInInspector] 
                public string autoEventsList;

            	[HideInInspector] 
                public string startIndex;

            	[HideInInspector] 
                public string captureDB;

            	[HideInInspector] 
                public string videoOutputDir;

            	[HideInInspector] 
                public bool editableVoxemes;

            //	[HideInInspector]
            //	public bool teachingAgent;

            	[HideInInspector] 
                public bool eulaAccepted;

            	ModalWindowManager windowManager;
            	EULAModalWindow eulaWindow;

            	UIButtonManager buttonManager;
            	ExportPrefsUIButton exportPrefsButton;
            	ImportPrefsUIButton importPrefsButton;

            	List<UIButton> uiButtons = new List<UIButton>();

            	int bgLeft = Screen.width / 6;
            	int bgTop = Screen.height / 12;
            	int bgWidth = 4 * Screen.width / 6;
            	int bgHeight = 10 * Screen.height / 12;
            	int margin;

            	Vector2 masterScrollPosition;
            	Vector2 sceneBoxScrollPosition;
            	Vector2 logsPrefsBoxScrollPosition;
            	Vector2 urlBoxScrollPosition;
            	Vector2 videoPrefsBoxScrollPosition;
            	Vector2 paramPrefsBoxScrollPosition;

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

            	public bool Draw {
            		get { return draw; }
            		set {
            			draw = value;
            			foreach (UIButton button in uiButtons) {
            				button.Draw = value;
            			}
            		}
            	}

            	bool draw;

            	void Awake() {
            		Draw = true;

#if UNITY_IOS
            		Screen.SetResolution(1280,960,true);
            		Debug.Log(Screen.currentResolution);
#endif

            		fontSizeModifier = (fontSize / defaultFontSize);

            		windowManager = GameObject.Find("VoxWorld").GetComponent<ModalWindowManager>();
            		buttonManager = GameObject.Find("VoxWorld").GetComponent<UIButtonManager>();
            		buttonManager.windowPort = new Rect(bgLeft, bgTop, bgWidth, bgHeight);
            	}

            	// Use this for initialization
            	void Start() {
            		LoadPrefs();
#if UNITY_EDITOR
            		string scenesDirPath = Application.dataPath + "/Scenes/";
            		string[] fileEntries = Directory.GetFiles(Application.dataPath + "/Scenes/", "*.unity");
            		foreach (string s in fileEntries) {
            			string sceneName = s.Remove(0, scenesDirPath.Length).Replace(".unity", "");
            			if (!sceneName.Equals(SceneManager.GetActiveScene().name)) {
            				Debug.Log(string.Format("Launcher.Start: Adding scene {0} to available scenes", sceneName));
            				availableScenes.Add(sceneName);
            			}
            		}
#elif UNITY_STANDALONE || UNITY_IOS || UNITY_WEBPLAYER
            		// What if ScenesList has been deleted?
            		TextAsset scenesList = (TextAsset)Resources.Load("ScenesList", typeof(TextAsset));
            		string[] scenes = scenesList.text.Split ('\r','\n');
            		foreach (string s in scenes) {
            			if ((s.Length > 0) && (!s.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name))) {
                            Debug.Log(string.Format("Launcher.Start: Adding scene {0} to available scenes", s));
            				availableScenes.Add(s);
            			}
            		}
#endif
                    GetMyIP();

            		listItems = availableScenes.ToArray();

#if !UNITY_IOS
                    exportPrefsButton = gameObject.GetComponent<ExportPrefsUIButton>();
                    importPrefsButton = gameObject.GetComponent<ImportPrefsUIButton>();

                    uiButtons.Add(exportPrefsButton);
                    uiButtons.Add(importPrefsButton);
#endif
            	}

            	// Update is called once per frame
            	void Update() {
            		Draw = (GameObject.Find("FileBrowser") == null);
            	}

            	void OnGUI() {
            		if (!Draw) {
            			return;
            		}

            		labelStyle = new GUIStyle("Label");
            		textFieldStyle = new GUIStyle("TextField");
            		buttonStyle = new GUIStyle("Button");
            		bgLeft = Screen.width / 6;
            		bgTop = Screen.height / 12;
            		bgWidth = 4 * Screen.width / 6;
            		bgHeight = 10 * Screen.height / 12;
            		margin = 0;

            		GUI.Box(new Rect(bgLeft, bgTop, bgWidth, bgHeight), "");

            		masterScrollPosition = GUI.BeginScrollView(new Rect(bgLeft + 5, bgTop + 5, bgWidth - 10, bgHeight - 70),
            			masterScrollPosition,
            			new Rect(bgLeft + margin, bgTop + 5, bgWidth - 10, bgHeight - 70));

#if UNITY_IOS
            		if (GUI.Button (new Rect (bgLeft + 165, bgTop + 35, GUI.skin.label.CalcSize (new GUIContent (ipContent)).x+10, 25*fontSizeModifier),
            			new GUIContent (ipContent))) {
            			if (ipContent == "IP") {
            				ipContent = ip;
            			}
            			else {
            				ipContent = "IP";
            			}
            		}
#endif

#if !UNITY_IOS
            		GUI.Label(new Rect(bgLeft + 10, bgTop + 35, 90 * fontSizeModifier, 25 * fontSizeModifier), "Make Logs");
            		makeLogs = GUI.Toggle(new Rect(bgLeft + 100, bgTop + 35, 25, 25 * fontSizeModifier), makeLogs, string.Empty);

            		if (makeLogs) {
            			GUILayout.BeginArea(new Rect(bgLeft + 10, bgTop + 60, 290 * fontSizeModifier, 45 * fontSizeModifier),
            				GUI.skin.box);
            			logsPrefsBoxScrollPosition = GUILayout.BeginScrollView(logsPrefsBoxScrollPosition, false, false);
            			GUILayout.BeginVertical(GUI.skin.box);

            			//GUILayout.Label("Video Capture Mode", GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("Video Capture Mode")).x + 10));

            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Prefix", GUILayout.Width(150 * fontSizeModifier));
            			logsPrefix = GUILayout.TextField(logsPrefix, GUILayout.Width(80 * fontSizeModifier));
            			GUILayout.EndHorizontal();

            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Actions Only Logs", GUILayout.Width(150 * fontSizeModifier));
            			actionsOnly = GUILayout.Toggle(actionsOnly, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            			GUILayout.EndHorizontal();

            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Full State Info", GUILayout.Width(150 * fontSizeModifier));
            			fullState = GUILayout.Toggle(fullState, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            			GUILayout.EndHorizontal();

            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Timestamps", GUILayout.Width(150 * fontSizeModifier));
            			logTimestamps = GUILayout.Toggle(logTimestamps, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            			GUILayout.EndHorizontal();

            			GUILayout.EndVertical();
            			GUILayout.EndScrollView();
            			GUILayout.EndArea();
            		}
#endif

            		GUILayout.BeginArea(new Rect(bgLeft + 10, bgTop + 110, 290 * fontSizeModifier, 120 * fontSizeModifier),
            			GUI.skin.box);
                    Vector2 connectionsLabelDimensions = GUI.skin.label.CalcSize(new GUIContent("Connections"));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Connections", GUILayout.Width(connectionsLabelDimensions.x));
                    GUILayout.Button(new GUIContent("*", "IP: " + ip),GUILayout.Width(10 * fontSizeModifier), GUILayout.Height(10 * fontSizeModifier));
                    if (GUI.tooltip != string.Empty) {
                        GetMyIP();
                        GUILayout.Label(GUI.tooltip, GUILayout.MaxWidth(GUI.skin.label.CalcSize(new GUIContent("IP: " + ip)).x + 10), GUILayout.ExpandWidth(false));
                    }
                    else {
                        GUILayout.Label(string.Empty);
                    }
                    GUILayout.EndHorizontal();
            		urlBoxScrollPosition = GUILayout.BeginScrollView(urlBoxScrollPosition, false, false);
            		GUILayout.BeginVertical(GUI.skin.box);

            		for (int i = 0; i < urls.Count; i++) {
                        GUILayout.BeginVertical(GUI.skin.box);
            			GUILayout.BeginHorizontal();
            			urlLabels[i] = GUILayout.TextField(urlLabels[i], GUILayout.Width(80 * fontSizeModifier));
            			urls[i] = GUILayout.TextField(urls[i], GUILayout.Width(140 * fontSizeModifier));
                        urlActiveStatuses[i] = GUILayout.Toggle(urlActiveStatuses[i], string.Empty, GUILayout.Width(20 * fontSizeModifier));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        urlTypes[i] = GUILayout.TextField(urlTypes[i], GUILayout.Width(80 * fontSizeModifier));
                        removeUrl.Add(-1);
                        removeUrl[i] = GUILayout.SelectionGrid(removeUrl[i], new string[] {"-"}, 1, GUI.skin.button,
                            GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
            			GUILayout.EndVertical();

            			if (removeUrl[i] == 0) {
            				removeUrl[i] = -1;
            				urlLabels.RemoveAt(i);
                            urlTypes.RemoveAt(i);
            				urls.RemoveAt(i);
                            urlActiveStatuses.RemoveAt(i);
            				numUrls--;
            			}
            		}

            		addUrl = GUILayout.SelectionGrid(addUrl, new string[] {"+"}, 1, GUI.skin.button, GUILayout.ExpandWidth(true));
            		if (addUrl == 0) {
            			// add new url
            			numUrls++;
            			for (int j = 1; j <= urls.Count + 1; j++) {
            				if (!urlLabels.Contains(string.Format("URL {0}", j))) {
            					urlLabels.Add(string.Format("URL {0}", j));
                                urlTypes.Add("");
            					urls.Add("");
                                urlActiveStatuses.Add(true);
            					break;
            				}
            			}

            			addUrl = -1;
            		}

            		GUILayout.EndVertical();
                    GUILayout.EndScrollView();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Load Socket Config", GUILayout.Width(135 * fontSizeModifier))) {
                        // read in the socket config file and deserialize it to an instance of VoxSimSocketConfig
                        XmlSerializer serializer = new XmlSerializer(typeof(VoxSimSocketConfig));
                        try {
                            using (var stream = new FileStream("local_config/socket_config.xml", FileMode.Open)) {
                                VoxSimSocketConfig config = serializer.Deserialize(stream) as VoxSimSocketConfig;

                                urlLabels.Clear();
                                urlTypes.Clear();
                                urls.Clear();
                                urlActiveStatuses.Clear();
                                numUrls = 0;

                                foreach (VoxSimSocket socket in config.Sockets) {
                                    urlLabels.Add(socket.Name);
                                    urlTypes.Add(socket.Type);
                                    urls.Add(socket.URL);
                                    urlActiveStatuses.Add(socket.Enabled);
                                    numUrls++;
                                }
                            }
                        }
                        catch (FileNotFoundException ex) {
                            // if local_config/socket_config.xml has been removed or renamed
                            //  create a new, empty one
                            using (var stream = new FileStream("local_config/socket_config.xml", FileMode.Create)) {
                                serializer.Serialize(stream, new VoxSimSocketConfig());

                                urlLabels.Clear();
                                urlTypes.Clear();
                                urls.Clear();
                                urlActiveStatuses.Clear();
                                numUrls = 0;
                            }
                        }
                    }
                    if (GUILayout.Button("Save Socket Config", GUILayout.Width(135 * fontSizeModifier))) {
                        XmlSerializer serializer = new XmlSerializer(typeof(VoxSimSocketConfig));
                        using (var stream = new FileStream("local_config/socket_config.xml", FileMode.Create)) {
                            VoxSimSocketConfig socketConfig = new VoxSimSocketConfig();
                            for (int i = 0; i < numUrls; i++) {
                                VoxSimSocket socket = new VoxSimSocket();
                                socket.Name = urlLabels[i];
                                socket.Type = urlTypes[i];
                                socket.URL = urls[i];
                                socket.Enabled = urlActiveStatuses[i];
                                socketConfig.Sockets.Add(socket);
                            }

                            List<string> urlStrings = new List<string>();
                            string urlsString = string.Empty;
                            for (int i = 0; i < numUrls; i++) {
                                urlStrings.Add(string.Format("{0}|{1}={2},{3}", urlLabels[i], urlTypes[i], urls[i], urlActiveStatuses[i].ToString()));
                            }
                            urlsString = string.Join(";", urlStrings);

                            PlayerPrefs.SetString("URLs", urlsString);

                            serializer.Serialize(stream, socketConfig);
                        }
                    }
                    GUILayout.EndHorizontal();
            		GUILayout.EndArea();

            #if !UNITY_IOS
            		GUI.Label(new Rect(bgLeft + 10, bgTop + 230, 90 * fontSizeModifier, 25 * fontSizeModifier), "Capture Video");
            		captureVideo = GUI.Toggle(new Rect(bgLeft + 100, bgTop + 230, 20, 25 * fontSizeModifier), captureVideo,
            			string.Empty);

            		if (captureVideo) {
            			captureParams = false;
            		}

            		GUI.Label(new Rect(bgLeft + 135, bgTop + 230, 150 * fontSizeModifier, 25 * fontSizeModifier), "Capture Params");
            		captureParams = GUI.Toggle(new Rect(bgLeft + 235, bgTop + 230, 20, 25 * fontSizeModifier), captureParams,
            			string.Empty);

            		if (captureParams) {
            			captureVideo = false;
            		}

            		if (captureVideo) {
            			GUILayout.BeginArea(new Rect(bgLeft + 10, bgTop + 255,
            				(((13 * Screen.width / 24) - 20 * fontSizeModifier) - bgLeft < 395 * fontSizeModifier)
            					? ((13 * Screen.width / 24) - 20 * fontSizeModifier) - (bgLeft)
            					: 395 * fontSizeModifier,
            				(bgTop + bgHeight - 80) - (bgTop + 245) < 210 * fontSizeModifier
            					? (bgTop + bgHeight - 80) - (bgTop + 245)
            					: 210 * fontSizeModifier), GUI.skin.box);
            			videoPrefsBoxScrollPosition = GUILayout.BeginScrollView(videoPrefsBoxScrollPosition, false, false,
            				GUILayout.ExpandWidth(true), GUILayout.MaxWidth((13 * Screen.width / 24) - 20 * fontSizeModifier));
            			GUILayout.BeginVertical(GUI.skin.box);

            			string warningText = "Enabling this option may affect performance";
            			GUILayout.TextArea(warningText,
            				GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(warningText)).x + 10),
            				GUILayout.Height(20 * fontSizeModifier));

            			GUILayout.BeginHorizontal();
            			GUILayout.BeginVertical();
            			GUILayout.Label("Video Capture Mode",
            				GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("Video Capture Mode")).x + 10));

            			string[] videoCaptureModeLabels = new string[] {"Manual", "Full-Time", "Per Event"};
            			videoCaptureMode = (VideoCaptureMode) GUILayout.SelectionGrid((int) videoCaptureMode,
            				videoCaptureModeLabels, 1, "toggle",
            				GUILayout.Width(150 * fontSizeModifier));
            			GUILayout.EndVertical();

            			GUILayout.BeginVertical();
            			GUILayout.Label("Capture Filename Type",
            				GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("Capture Filename Type")).x + 10));

            			string[] videoCaptureFilenameTypeLabels = new string[] {"Flashback Default", "Event String", "Custom"};
            			videoCaptureFilenameType =
            				(VideoCaptureFilenameType) GUILayout.SelectionGrid((int) videoCaptureFilenameType,
            					videoCaptureFilenameTypeLabels, 1, "toggle");

            			// EventString can only be used with PerEvent
            			if (videoCaptureMode != VideoCaptureMode.PerEvent) {
            				if (videoCaptureFilenameType == VideoCaptureFilenameType.EventString) {
            					videoCaptureFilenameType = VideoCaptureFilenameType.FlashbackDefault;
            				}
            			}

            			if (videoCaptureFilenameType == VideoCaptureFilenameType.EventString) {
            				GUILayout.BeginHorizontal();
            				sortByEventString =
            					GUILayout.Toggle(sortByEventString, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            				GUILayout.Label("Sort Videos By Event String", GUILayout.Width(120 * fontSizeModifier));
            				GUILayout.EndHorizontal();
            			}
            			else if (videoCaptureFilenameType == VideoCaptureFilenameType.Custom) {
            				customVideoFilenamePrefix =
            					GUILayout.TextField(customVideoFilenamePrefix, GUILayout.Width(150 * fontSizeModifier));
            			}

            			GUILayout.EndVertical();
            			GUILayout.EndHorizontal();

            			if (videoCaptureMode == VideoCaptureMode.PerEvent) {
            				GUILayout.BeginVertical();
            				GUILayout.BeginHorizontal();
            				resetScene = GUILayout.Toggle(resetScene, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            				GUILayout.BeginVertical();
            				GUILayout.Label("Reset Scene Between", GUILayout.Width(130 * fontSizeModifier));
            				GUILayout.BeginHorizontal();
            				GUILayout.Label("Every", GUILayout.Width(35 * fontSizeModifier));
            				eventResetCounter =
            					Regex.Replace(GUILayout.TextField(eventResetCounter, GUILayout.Width(25 * fontSizeModifier)),
            						@"[^0-9]", "");
            				GUILayout.Label("Events");
            				GUILayout.EndHorizontal();
            				GUILayout.EndVertical();
            				GUILayout.EndHorizontal();
            				GUILayout.EndVertical();

            				GUILayout.BeginVertical();
            				GUILayout.BeginHorizontal();
            				GUILayout.Label("Auto-Input Script", GUILayout.Width(120 * fontSizeModifier));
            				autoEventsList = GUILayout.TextField(autoEventsList, GUILayout.Width(150 * fontSizeModifier));
            				GUILayout.Label(".py : ", GUILayout.Width(30 * fontSizeModifier));
            				startIndex = Regex.Replace(GUILayout.TextField(startIndex, GUILayout.Width(40 * fontSizeModifier)),
            					@"[^0-9]", "");
            				GUILayout.EndHorizontal();
            				GUILayout.Label("(Leave empty to input events manually)");
            				GUILayout.EndVertical();
            			}

            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Video Output Folder", GUILayout.Width(120 * fontSizeModifier));
            			videoOutputDir = GUILayout.TextField(videoOutputDir, GUILayout.Width(150 * fontSizeModifier));
            			GUILayout.EndHorizontal();

            			GUILayout.BeginVertical();
            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Video Database File", GUILayout.Width(120 * fontSizeModifier));
            			captureDB = GUILayout.TextField(captureDB, GUILayout.Width(150 * fontSizeModifier));
            			GUILayout.Label(".db", GUILayout.Width(25 * fontSizeModifier));
            			GUILayout.EndHorizontal();
            			GUILayout.Label("(Leave empty to omit video info from database)", GUILayout.Width(300 * fontSizeModifier));
            			GUILayout.EndVertical();

            			GUILayout.EndVertical();
            			GUILayout.EndScrollView();
            			GUILayout.EndArea();
            		}
            		else if (captureParams) {
            			GUILayout.BeginArea(new Rect(bgLeft + 10, bgTop + 255,
            				(((13 * Screen.width / 24) - 20 * fontSizeModifier) - bgLeft < 380 * fontSizeModifier)
            					? ((13 * Screen.width / 24) - 20 * fontSizeModifier) - (bgLeft)
            					: 380 * fontSizeModifier,
            				(bgTop + bgHeight - 80) - (bgTop + 245) < 170 * fontSizeModifier
            					? (bgTop + bgHeight - 80) - (bgTop + 245)
            					: 170 * fontSizeModifier), GUI.skin.box);
            			paramPrefsBoxScrollPosition = GUILayout.BeginScrollView(paramPrefsBoxScrollPosition, false, false);
            			GUILayout.BeginVertical(GUI.skin.box);

            			GUILayout.BeginVertical();
            			GUILayout.BeginHorizontal();
            			resetScene = GUILayout.Toggle(resetScene, string.Empty, GUILayout.Width(20 * fontSizeModifier));
            			GUILayout.BeginVertical();
            			GUILayout.Label("Reset Scene Between", GUILayout.Width(130 * fontSizeModifier));
            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Every", GUILayout.Width(35 * fontSizeModifier));
            			eventResetCounter =
            				Regex.Replace(GUILayout.TextField(eventResetCounter, GUILayout.Width(25 * fontSizeModifier)), @"[^0-9]",
            					"");
            			GUILayout.Label("Events");
            			GUILayout.EndHorizontal();
            			GUILayout.EndVertical();
            			GUILayout.EndHorizontal();
            			GUILayout.EndVertical();

            			GUILayout.BeginVertical();
            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Auto-Input Script", GUILayout.Width(120 * fontSizeModifier));
            			autoEventsList = GUILayout.TextField(autoEventsList, GUILayout.Width(150 * fontSizeModifier));
            			GUILayout.Label(".py : ", GUILayout.Width(30 * fontSizeModifier));
            			startIndex = Regex.Replace(GUILayout.TextField(startIndex, GUILayout.Width(40 * fontSizeModifier)),
            				@"[^0-9]", "");
            			GUILayout.EndHorizontal();
            			GUILayout.Label("(Leave empty to input events manually)");
            			GUILayout.EndVertical();

            			GUILayout.BeginVertical();
            			GUILayout.BeginHorizontal();
            			GUILayout.Label("Capture Database", GUILayout.Width(120 * fontSizeModifier));
            			captureDB = GUILayout.TextField(captureDB, GUILayout.Width(150 * fontSizeModifier));
            			GUILayout.Label(".db", GUILayout.Width(25 * fontSizeModifier));
            			GUILayout.EndHorizontal();
            			GUILayout.Label("(Leave empty to omit param info from database)", GUILayout.Width(300 * fontSizeModifier));
            			GUILayout.EndVertical();

            			GUILayout.EndVertical();
            			GUILayout.EndScrollView();
            			GUILayout.EndArea();
            		}
            #endif

            		GUILayout.BeginArea(new Rect(13 * Screen.width / 24, bgTop + 35, 3 * Screen.width / 12, 3 * Screen.height / 6),
            			GUI.skin.window);
            		sceneBoxScrollPosition = GUILayout.BeginScrollView(sceneBoxScrollPosition, false, false);
            		GUILayout.BeginVertical(GUI.skin.box);

            		customStyle = GUI.skin.button;
            		//customStyle.active.background = Texture2D.whiteTexture;
            		//customStyle.onActive.background = Texture2D.whiteTexture;
            		//customStyle.active.textColor = Color.black;
            		//customStyle.onActive.textColor = Color.black;

            		selected = GUILayout.SelectionGrid(selected, listItems, 1, customStyle, GUILayout.ExpandWidth(true));

            		if (selected >= 0) {
            			// extract the scene name (alone) from the path that's displayed in the scene selection list
            			sceneSelected = listItems[selected].Split('/').Last();
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndScrollView();
            		GUILayout.EndArea();

            		GUI.Label(
            			new Rect(13 * Screen.width / 24, bgTop + 35 + (3 * Screen.height / 6) + 10 * fontSizeModifier,
            				150 * fontSizeModifier, 25 * fontSizeModifier), "Make Voxemes Editable");
            		editableVoxemes =
            			GUI.Toggle(
            				new Rect((13 * Screen.width / 24) + (150 * fontSizeModifier),
            					bgTop + 35 + (3 * Screen.height / 6) + 10 * fontSizeModifier, 150, 25 * fontSizeModifier),
            				editableVoxemes, string.Empty);

            //		GUI.Label (new Rect ((13*Screen.width/24 + 3*Screen.width/12) - (150*fontSizeModifier), bgTop + 35 + (3*Screen.height/6) + 10*fontSizeModifier, 150*fontSizeModifier, 25*fontSizeModifier), "Use Teaching Agent");
            //		teachingAgent = GUI.Toggle (new Rect ((13*Screen.width/24 + 3*Screen.width/12) - (25*fontSizeModifier), bgTop + 35 + (3*Screen.height/6) + 10*fontSizeModifier, 150, 25*fontSizeModifier), teachingAgent, string.Empty);

            		Vector2 scenesLabelDimensions = GUI.skin.label.CalcSize(new GUIContent("Scenes"));

            		GUI.Label(new Rect(2 * Screen.width / 3 - scenesLabelDimensions.x / 2, bgTop + 35, scenesLabelDimensions.x, 25), "Scenes");
            		GUI.EndScrollView();

            		if (GUI.Button(
            			new Rect((Screen.width / 2 - 50) - 125, bgTop + bgHeight - 60, 100 * fontSizeModifier,
            				50 * fontSizeModifier), "Revert Prefs")) {
            			LoadPrefs();
            		}

            		if (GUI.Button(
            			new Rect(Screen.width / 2 - 50, bgTop + bgHeight - 60, 100 * fontSizeModifier, 50 * fontSizeModifier),
            			"Save Prefs")) {
            			SavePrefs();
            		}

            		if (GUI.Button(
            			new Rect((Screen.width / 2 - 50) + 125, bgTop + bgHeight - 60, 100 * fontSizeModifier,
            				50 * fontSizeModifier), "Save & Launch")) {
            			if (sceneSelected != "") {
            				SavePrefs();

            				if (eulaAccepted) {
            					Debug.Log(string.Format("Launching scene {0}", sceneSelected));
            					StartCoroutine(SceneHelper.LoadScene(sceneSelected));
            				}
            				else {
            					PopUpEULAWindow();
            				}
            			}
            		}

            		scenesLabelDimensions = GUI.skin.label.CalcSize(new GUIContent(launcherTitle));
            		GUI.Label(new Rect(((2 * bgLeft + bgWidth) / 2) - scenesLabelDimensions.x / 2, bgTop, scenesLabelDimensions.x, 25),
            			launcherTitle);
            	}

                void GetMyIP() {
                // get IP address
#if !UNITY_IOS
                    foreach (IPAddress ipAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
                        if (ipAddress.AddressFamily.ToString() == "InterNetwork") {
                            //Debug.Log(ipAddress.ToString());
                            ip = ipAddress.ToString();
                        }
                    }
#else
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()){
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                            //Debug.Log(ni.Name);
                            foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses) {
                                if (ipInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
            //                      Debug.Log (ipInfo.Address.ToString());
                                    ip = ipInfo.Address.ToString();
                                }
                            }
                        }  
                    }
#endif
                }

            	void PopUpEULAWindow() {
            		eulaWindow = gameObject.AddComponent<EULAModalWindow>();
            		eulaWindow.windowRect = new Rect(bgLeft + 25, bgTop + 25, bgWidth - 50, bgHeight - 50);
            		eulaWindow.windowTitle = "VoxSim End User License Agreement";
            		eulaWindow.Render = true;
            		eulaWindow.AllowDrag = false;
            		eulaWindow.AllowResize = false;
            	}

            	void EULAAccepted(bool accepted) {
            		eulaAccepted = accepted;
            		PlayerPrefs.SetInt("EULA Accepted", Convert.ToInt32(eulaAccepted));
            	}

            	void LoadPrefs() {
            		inPort = PlayerPrefs.GetString("Listener Port");
            		makeLogs = (PlayerPrefs.GetInt("Make Logs") == 1);
            		logsPrefix = PlayerPrefs.GetString("Logs Prefix");
            		actionsOnly = (PlayerPrefs.GetInt("Actions Only Logs") == 1);
            		fullState = (PlayerPrefs.GetInt("Full State Info") == 1);
            		logTimestamps = (PlayerPrefs.GetInt("Timestamps") == 1);

            		numUrls = 0;
            		string urlsString = PlayerPrefs.GetString("URLs");
            		foreach (string urlString in urlsString.Split(';')) {
                        // e.g., Fusion|FusionSocket=tarski.cs-i.brandeis.edu:9126,true;EpiSim|EpiSimSocket=localhost:5000,false
                        if (urlString.Contains("|")) {
                            urlTypes.Add(urlString.Split('|')[1].Split('=')[0]);
                			if (urlString.Contains("=")) {
                				urlLabels.Add(urlString.Split('|')[0]);
                                if (urlString.Contains(",")) {
                				    urls.Add(urlString.Split('=')[1].Split(',')[0]);
                                    urlActiveStatuses.Add(Convert.ToBoolean(urlString.Split('=')[1].Split(',')[1]));
                                }
                                else {
                                    urls.Add(urlString.Split('=')[1]);
                                    urlActiveStatuses.Add(false);
                                }
                            }
                            else {
                                urlTypes.Add(urlString.Split('|')[1]);
                                urlLabels.Add(urlString.Split('|')[0]);
                                urls.Add(string.Empty);
                                if (urlString.Contains(",")) {
                                    urlActiveStatuses.Add(Convert.ToBoolean(urlString.Split('|')[1].Split(',')[1]));
                                }
                                else {
                                    urlActiveStatuses.Add(false);
                                }
                            }
            			}
                        else {
                            urlTypes.Add(string.Empty);
                            if (urlString.Contains("=")) {
                                urlLabels.Add(urlString.Split('=')[0]);
                                if (urlString.Contains(",")) {
                                    urls.Add(urlString.Split('=')[1].Split(',')[0]);
                                    urlActiveStatuses.Add(Convert.ToBoolean(urlString.Split('=')[1].Split(',')[0]));
                                }
                                else {
                                    urls.Add(urlString.Split('=')[1]);
                                    urlActiveStatuses.Add(false);
                                }
                            }
                            else {
                                urlLabels.Add(string.Empty);
                                urls.Add(string.Empty);
                                if (urlString.Contains(",")) {
                                    urlActiveStatuses.Add(Convert.ToBoolean(urlString.Split('|')[1].Split(',')[0]));
                                }
                                else {
                                    urlActiveStatuses.Add(false);
                                }
                            }
                        }
                        numUrls++;
            		}

            		captureVideo = (PlayerPrefs.GetInt("Capture Video") == 1);
            		captureParams = (PlayerPrefs.GetInt("Capture Params") == 1);
            		videoCaptureMode = (VideoCaptureMode) PlayerPrefs.GetInt("Video Capture Mode");
            		resetScene = (PlayerPrefs.GetInt("Reset Between Events") == 1);
            		eventResetCounter = PlayerPrefs.GetInt("Event Reset Counter").ToString();
            		videoCaptureFilenameType = (VideoCaptureFilenameType) PlayerPrefs.GetInt("Video Capture Filename Type");
            		sortByEventString = (PlayerPrefs.GetInt("Sort By Event String") == 1);
            		customVideoFilenamePrefix = PlayerPrefs.GetString("Custom Video Filename Prefix");
            		autoEventsList = PlayerPrefs.GetString("Auto Events List");
            		startIndex = PlayerPrefs.GetInt("Start Index").ToString();
            		captureDB = PlayerPrefs.GetString("Video Capture DB");
            		videoOutputDir = PlayerPrefs.GetString("Video Output Directory");
            		editableVoxemes = (PlayerPrefs.GetInt("Make Voxemes Editable") == 1);
            		eulaAccepted = (PlayerPrefs.GetInt("EULA Accepted") == 1);
            	}

            	public VoxSimUserPrefs SavePrefs() {
                    VoxSimUserPrefs userPrefs = new VoxSimUserPrefs();
            		if ((eventResetCounter == string.Empty) || (eventResetCounter == "0")) {
            			eventResetCounter = "1";
            		}

            		if (startIndex == string.Empty) {
            			startIndex = "0";
            		}

            		PlayerPrefs.SetString("Listener Port", inPort);
            		PlayerPrefs.SetInt("Make Logs", Convert.ToInt32(makeLogs));
            		PlayerPrefs.SetString("Logs Prefix", logsPrefix);
            		PlayerPrefs.SetInt("Actions Only Logs", Convert.ToInt32(actionsOnly));
            		PlayerPrefs.SetInt("Full State Info", Convert.ToInt32(fullState));
            		PlayerPrefs.SetInt("Timestamps", Convert.ToInt32(logTimestamps));

                    List<string> urlStrings = new List<string>();
            		string urlsString = string.Empty;
            		for (int i = 0; i < numUrls; i++) {
            			urlStrings.Add(string.Format("{0}|{1}={2},{3}", urlLabels[i], urlTypes[i], urls[i], urlActiveStatuses[i].ToString()));
            		}
                    urlsString = string.Join(";", urlStrings);
                    Debug.Log(urlsString);

            		PlayerPrefs.SetString("URLs", urlsString);

            		PlayerPrefs.SetInt("Capture Video", Convert.ToInt32(captureVideo));
            		PlayerPrefs.SetInt("Capture Params", Convert.ToInt32(captureParams));
            		PlayerPrefs.SetInt("Video Capture Mode", Convert.ToInt32(videoCaptureMode));
            		PlayerPrefs.SetInt("Reset Between Events", Convert.ToInt32(resetScene));
            		PlayerPrefs.SetInt("Event Reset Counter", Convert.ToInt32(eventResetCounter));
            		PlayerPrefs.SetInt("Video Capture Filename Type", Convert.ToInt32(videoCaptureFilenameType));
            		PlayerPrefs.SetInt("Sort By Event String", Convert.ToInt32(sortByEventString));
            		PlayerPrefs.SetString("Custom Video Filename Prefix", customVideoFilenamePrefix);
            		PlayerPrefs.SetString("Auto Events List", autoEventsList);
            		PlayerPrefs.SetInt("Start Index", Convert.ToInt32(startIndex));
            		PlayerPrefs.SetString("Video Capture DB", captureDB);
            		PlayerPrefs.SetString("Video Output Directory", videoOutputDir);
            		PlayerPrefs.SetInt("Make Voxemes Editable", Convert.ToInt32(editableVoxemes));

                    userPrefs.MakeLogs = makeLogs;
                    userPrefs.LogsPrefix = logsPrefix;
                    userPrefs.ActionOnlyLogs = actionsOnly;
                    userPrefs.FullStateInfo = fullState;
                    userPrefs.LogTimestamps = logTimestamps;

                    for (int i = 0; i < numUrls; i++) {
                        VoxSimSocket socket = new VoxSimSocket();
                        socket.Name = urlLabels[i];
                        socket.Type = urlTypes[i];
                        socket.URL = urls[i];
                        socket.Enabled = urlActiveStatuses[i];
                        userPrefs.SocketConfig.Sockets.Add(socket);
                    }

                    userPrefs.CapturePrefs.CaptureVideo = captureVideo;
                    userPrefs.CapturePrefs.CaptureParams = captureParams;
                    userPrefs.CapturePrefs.VideoCaptureMode = videoCaptureMode.ToString();
                    userPrefs.CapturePrefs.ResetBetweenEvents = resetScene;
                    userPrefs.CapturePrefs.EventResetCounter = Convert.ToInt32(eventResetCounter);
                    userPrefs.CapturePrefs.VideoCaptureFilenameType = videoCaptureFilenameType.ToString();
                    userPrefs.CapturePrefs.SortByEventString = captureVideo;
                    userPrefs.CapturePrefs.CustomVideoFilenamePrefix = customVideoFilenamePrefix;
                    userPrefs.CapturePrefs.AutoEventsList = autoEventsList;
                    userPrefs.CapturePrefs.StartIndex = Convert.ToInt32(startIndex);
                    userPrefs.CapturePrefs.VideoCaptureDB = captureDB;
                    userPrefs.CapturePrefs.VideoOutputDirectory = videoOutputDir;

                    userPrefs.MakeVoxemesEditable = editableVoxemes;

                    return userPrefs;
            	}
            }
        }
    }
}