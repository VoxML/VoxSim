using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using SimpleFileBrowser.Scripts.GracesGames;
using Global;
using VideoCapture;

public class ImportPrefsUIButton : UIButton {

	public int fontSize = 12;

	GUIStyle buttonStyle;

	public GameObject FileBrowserPrefab;
	String textToSave = "";
	Launcher launcher;


	// Use this for initialization
	void Start () {
		FontSizeModifier = (int)(fontSize / defaultFontSize);
		launcher = gameObject.GetComponent<Launcher> ();

		base.Start ();
	}

	// Update is called once per frame
	void Update () {

	}

	protected override void OnGUI () {
		if (!Draw) {
			return;
		}

		buttonStyle = new GUIStyle ("Button");
		buttonStyle.fontSize = fontSize;

		if (GUI.Button (buttonRect, buttonText, buttonStyle)) {
			launcher.Draw = false;
			OpenFileBrowser (FileBrowserMode.Load);
			return;
		}

		base.OnGUI ();
	}

	public override void DoUIButton(int buttonID){

		base.DoUIButton (buttonID);
	}

	void ImportPrefs(string data) {
		string[] lines = data.Split('\n');
		foreach (string line in lines) {
			switch (line.Split (',') [0]) {
			case "Listener Port":
				launcher.inPort = line.Split (',') [1].Trim();
				break;

			case "Make Logs":
				launcher.makeLogs = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

			case "Logs Prefix":
				launcher.logsPrefix = line.Split (',') [1].Trim();
				break;

			case "URLs":
				launcher.urlLabels.Clear ();
				launcher.urls.Clear ();
				launcher.numUrls = 0;
				string urlsString = PlayerPrefs.GetString("URLs");
				foreach (string urlString in line.Split (',') [1].Trim().Split(';')) {
					if (urlString.Contains ("=")) {
						launcher.urlLabels.Add (urlString.Split ('=') [0]);
						launcher.urls.Add (urlString.Split ('=') [1]);
						launcher.numUrls++;
					}
				}
				break;

//			case "CSU URL":
//				launcher.csuUrl = line.Split (',') [1].Trim();
//				break;
//
//			case "EpiSim URL":
//				launcher.epiSimUrl = line.Split (',') [1].Trim();
//				break;
//
//			case "SRI URL":
//				launcher.sriUrl = line.Split (',') [1].Trim();
//				break;
//
//			case "Parser URL":
//				launcher.parserUrl = line.Split (',') [1].Trim();
//				break;

			case "Capture Video":
				launcher.captureVideo = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

			case "Capture Params":
				launcher.captureParams = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

			case "Video Capture Mode":
				launcher.videoCaptureMode = (VideoCaptureMode)System.Convert.ToInt32(line.Split (',') [1].Trim());
				break;

			case "Reset Between Events":
				launcher.resetScene = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

			case "Event Reset Counter":
				launcher.eventResetCounter = line.Split (',') [1].Trim();
				break;

			case "Video Capture Filename Type":
				launcher.videoCaptureFilenameType = (VideoCaptureFilenameType)System.Convert.ToInt32(line.Split (',') [1].Trim());
				break;

			case "Sort By Event String":
				launcher.sortByEventString = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

			case "Custom Video Filename Prefix":
				launcher.customVideoFilenamePrefix = line.Split (',') [1].Trim();
				break;

			case "Auto Events List":
				launcher.autoEventsList = line.Split (',') [1].Trim();
				break;

			case "Start Index":
				launcher.startIndex = line.Split (',') [1].Trim();
				break;

			case "Video Capture DB":
				launcher.captureDB = line.Split (',') [1].Trim();
				break;

			case "Video Output Directory":
				launcher.videoOutputDir = line.Split (',') [1].Trim();
				break;

			case "Make Voxemes Editable":
				launcher.editableVoxemes = System.Convert.ToBoolean(line.Split (',') [1].Trim());
				break;

//			case "Use Teaching Agent":
//				launcher.teachingAgent = System.Convert.ToBoolean(line.Split (',') [1].Trim());
//				break;

			default:
				break;
			}
		}
	}

	// Open a file browser to save and load files
	public void OpenFileBrowser(FileBrowserMode fileBrowserMode) {
		// Create the file browser and name it
		GameObject fileBrowserObject = Instantiate(FileBrowserPrefab, transform);
		fileBrowserObject.name = "FileBrowser";
		// Set the mode to save or load
		FileBrowser fileBrowserScript = fileBrowserObject.GetComponent<FileBrowser>();
		fileBrowserScript.SetupFileBrowser(ViewMode.Landscape);
		if (fileBrowserMode == FileBrowserMode.Load) {
			fileBrowserScript.OpenFilePanel(this, "LoadFileUsingPath", "txt");
		}

		GameObject uiObject = GameObject.Find (fileBrowserObject.name + "UI");
		uiObject.GetComponent<RectTransform> ().transform.localScale = new Vector3 (0.6f, 0.6f, 1.0f);

		GameObject directoryPanel = GameObject.Find (uiObject.name + "/FileBrowserPanel/DirectoryPanel");
		foreach (Text text in directoryPanel.GetComponentsInChildren<Text>()) {
			text.fontSize = 20;
		}

		GameObject filePanel = GameObject.Find (uiObject.name + "/FileBrowserPanel/FilePanel");
		foreach (Text text in filePanel.GetComponentsInChildren<Text>()) {
			text.fontSize = 20;
		}
	}

	// Loads a file using a path
	private void LoadFileUsingPath(string path) {
		if (path.Length != 0) {
			BinaryFormatter bFormatter = new BinaryFormatter();
			// Open the file using the path
			FileStream file = File.OpenRead(path);
			// Convert the file from a byte array into a string
			string fileData = bFormatter.Deserialize(file) as string;
			// We're done working with the file so we can close it
			file.Close();
			// Set the LoadedText with the value of the file
			ImportPrefs(fileData);
			launcher.Draw = true;
		}
		else {
			Debug.Log("Invalid path given");
		}
	}
}
