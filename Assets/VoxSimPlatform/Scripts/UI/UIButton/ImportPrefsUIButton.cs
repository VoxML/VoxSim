using UnityEngine;
using UnityEngine.UI;using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

//using GracesGames.SimpleFileBrowser.Scripts;
using SFB; //StandaloneFileBroswer
#if !UNITY_WEBGL
using VoxSimPlatform.Network; 
#endif
using VoxSimPlatform.UI.Launcher;
using VoxSimPlatform.VideoCapture;

namespace VoxSimPlatform {
    namespace UI {
        namespace UIButtons {
            public class ImportPrefsUIButton : UIButton {
            	public int fontSize = 12;

            	GUIStyle buttonStyle;

            	public GameObject FileBrowserPrefab;
            	String textToSave = "";
            	LauncherMenu launcher;


            	// Use this for initialization
            	void Start() {
            		FontSizeModifier = (int) (fontSize / defaultFontSize);
            		launcher = gameObject.GetComponent<LauncherMenu>();

            		base.Start();
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	protected override void OnGUI() {
            		if (!Draw) {
            			return;
            		}

            		buttonStyle = new GUIStyle("Button");
            		buttonStyle.fontSize = fontSize;

            		if (GUI.Button(buttonRect, buttonText, buttonStyle)) {
            			launcher.Draw = false;
                        string[] path = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false); //Call to SFB to open
                        LoadFileUsingPath(path[0]);
                        return;
            		}

            		base.OnGUI();
            	}

            	public override void DoUIButton(int buttonID) {
            		base.DoUIButton(buttonID);
            	}

            	void ImportPrefs(VoxSimUserPrefs userPrefs) {
                    launcher.makeLogs = userPrefs.MakeLogs;
                    launcher.logsPrefix = userPrefs.LogsPrefix;
                    launcher.actionsOnly = userPrefs.ActionOnlyLogs;
                    launcher.fullState = userPrefs.FullStateInfo;
                    launcher.logTimestamps = userPrefs.LogTimestamps;

#if !UNITY_WEBGL
					launcher.numUrls = 0;
					launcher.urlLabels.Clear();
					launcher.urlTypes.Clear();
					launcher.urls.Clear();
					launcher.urlActiveStatuses.Clear();
					foreach (VoxSimSocket socket in userPrefs.SocketConfig.Sockets)
					{
						launcher.urlLabels.Add(socket.Name);
						launcher.urlTypes.Add(socket.Type);
						launcher.urls.Add(socket.URL);
						launcher.urlActiveStatuses.Add(socket.Enabled);
						launcher.numUrls++;
					} 
#endif

					launcher.captureVideo = userPrefs.CapturePrefs.CaptureVideo;
                    launcher.captureParams = userPrefs.CapturePrefs.CaptureParams;
                    Enum.TryParse<VideoCaptureMode>(userPrefs.CapturePrefs.VideoCaptureMode, out launcher.videoCaptureMode);
                    launcher.resetScene = userPrefs.CapturePrefs.ResetBetweenEvents;
                    launcher.eventResetCounter = userPrefs.CapturePrefs.EventResetCounter.ToString(); 
                    Enum.TryParse<VideoCaptureFilenameType>(userPrefs.CapturePrefs.VideoCaptureFilenameType, out launcher.videoCaptureFilenameType);
                    launcher.sortByEventString = userPrefs.CapturePrefs.SortByEventString;
                    launcher.customVideoFilenamePrefix = userPrefs.CapturePrefs.CustomVideoFilenamePrefix;
                    launcher.autoEventsList = userPrefs.CapturePrefs.AutoEventsList;
                    launcher.startIndex = userPrefs.CapturePrefs.StartIndex.ToString();
                    launcher.captureDB = userPrefs.CapturePrefs.VideoCaptureDB;
                    launcher.videoOutputDir = userPrefs.CapturePrefs.VideoOutputDirectory;
                    launcher.editableVoxemes = userPrefs.MakeVoxemesEditable;
            	}

            	// Loads a file using a path
            	private void LoadFileUsingPath(string path) {
            		if (path.Length != 0) {
                        XmlSerializer serializer = new XmlSerializer(typeof(VoxSimUserPrefs));
                        using (var stream = new FileStream(path, FileMode.Open)) {
                            VoxSimUserPrefs userPrefs = serializer.Deserialize(stream) as VoxSimUserPrefs;
                            ImportPrefs(userPrefs);
                        }

            			launcher.Draw = true;
            		}
            		else {
            			Debug.Log("Invalid path given");
            		}
            	}
            }
        } 
    }
}