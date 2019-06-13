using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

using GracesGames.SimpleFileBrowser.Scripts;
using VoxSimPlatform.UI.Launcher;

namespace VoxSimPlatform {
    namespace UI {
        namespace UIButtons {
            public class ExportPrefsUIButton : UIButton {
            	public int fontSize = 12;

            	GUIStyle buttonStyle;

            	public GameObject FileBrowserPrefab;
            	VoxSimUserPrefs prefsToSave;
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
            			prefsToSave = ExportPrefs();
            			OpenFileBrowser(FileBrowserMode.Save);
            			return;
            		}

            		base.OnGUI();
            	}

            	public override void DoUIButton(int buttonID) {
            		base.DoUIButton(buttonID);
            	}

            	VoxSimUserPrefs ExportPrefs() {
            		VoxSimUserPrefs prefsObject = launcher.SavePrefs();

                    if (prefsObject.CapturePrefs != null) {
                        if (prefsObject.CapturePrefs.EventResetCounter == 0) {
                            prefsObject.CapturePrefs.EventResetCounter = 1;
                        }
                    }

            		return prefsObject;
            	}

            	// Open a file browser to save and load files
            	public void OpenFileBrowser(FileBrowserMode fileBrowserMode) {
            		// Create the file browser and name it
            		GameObject fileBrowserObject = Instantiate(FileBrowserPrefab, transform);
            		fileBrowserObject.name = "FileBrowser";
            		// Set the mode to save or load
            		FileBrowser fileBrowserScript = fileBrowserObject.GetComponent<FileBrowser>();
            		fileBrowserScript.SetupFileBrowser(ViewMode.Landscape);
            		if (fileBrowserMode == FileBrowserMode.Save) {
            			fileBrowserScript.SaveFilePanel("NewPrefs", new string[] {"xml"});
            			fileBrowserScript.OnFileSelect += SaveFileUsingPath;
            		}

            		GameObject uiObject = GameObject.Find(fileBrowserObject.name + "UI");
            		uiObject.GetComponent<RectTransform>().transform.localScale = new Vector3(0.6f, 0.6f, 1.0f);

            		GameObject directoryPanel = GameObject.Find(uiObject.name + "/FileBrowserPanel/DirectoryPanel");
            		foreach (Text text in directoryPanel.GetComponentsInChildren<Text>()) {
            			text.fontSize = 20;
            		}

            		GameObject filePanel = GameObject.Find(uiObject.name + "/FileBrowserPanel/FilePanel");
            		foreach (Text text in filePanel.GetComponentsInChildren<Text>()) {
            			text.fontSize = 20;
            		}
            	}

            	// Saves a file with the textToSave using a path
            	private void SaveFileUsingPath(string path) {
            		// Make sure path and _textToSave is not null or empty
            		if (!String.IsNullOrEmpty(path) && prefsToSave != null) {
                        XmlSerializer serializer = new XmlSerializer(typeof(VoxSimUserPrefs));
                        using (var stream = new FileStream(path, FileMode.Create)) {
                            serializer.Serialize(stream, prefsToSave);
                        }

            			launcher.Draw = true;
            		}
            		else {
            			Debug.Log("Invalid path or empty file given");
            		}
            	}
            }
        }
    }
}