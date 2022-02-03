using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

//using GracesGames.SimpleFileBrowser.Scripts;
using SFB; //StandaloneFileBroswer
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
                        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "NewPrefs", "xml");//Call to SFB to save
                        Debug.Log(path);
                        if (!string.IsNullOrEmpty(path))
                        {
                            SaveFileUsingPath(path);
                        }
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