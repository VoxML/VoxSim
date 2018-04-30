using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using Global;
using SimpleFileBrowser.Scripts.GracesGames;

public class ExportPrefsUIButton : UIButton {

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
			textToSave = ExportPrefs ();
			OpenFileBrowser (FileBrowserMode.Save);
			return;
		}

		base.OnGUI ();
	}

	public override void DoUIButton(int buttonID){

		base.DoUIButton (buttonID);
	}

	string ExportPrefs() {
		
		launcher.SavePrefs ();
		if ((launcher.eventResetCounter == string.Empty) || (launcher.eventResetCounter == "0")) {
			launcher.eventResetCounter = "1";
		}

		if (launcher.startIndex == string.Empty) {
			launcher.startIndex = "0";
		}

		Dictionary<string, object> prefsDict = new Dictionary<string, object> ();
		prefsDict.Add ("Listener Port", PlayerPrefs.GetString ("Listener Port"));
		prefsDict.Add ("Make Logs", (PlayerPrefs.GetInt ("Make Logs") == 1));
		prefsDict.Add ("Logs Prefix", PlayerPrefs.GetString ("Logs Prefix"));

		string urlsString = string.Empty;
		for (int i = 0; i < launcher.numUrls; i++) {
			urlsString += string.Format ("{0}={1};", launcher.urlLabels[i], launcher.urls[i]);
		}
		prefsDict.Add ("URLs", urlsString);

//		prefsDict.Add ("CSU URL", PlayerPrefs.GetString ("CSU URL"));
//		prefsDict.Add ("EpiSim URL", PlayerPrefs.GetString ("EpiSim URL"));
//		prefsDict.Add ("SRI URL", PlayerPrefs.GetString ("SRI URL"));
//		prefsDict.Add ("Parser URL", PlayerPrefs.GetString ("Parser URL"));
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
//		prefsDict.Add ("Use Teaching Agent", (PlayerPrefs.GetInt("Use Teaching Agent") == 1));

		StringBuilder sb = new StringBuilder ();

		foreach (var entry in prefsDict) {
			sb.Append (string.Format ("{0},{1}\n", entry.Key, entry.Value));
		}

		return sb.ToString ();
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
			fileBrowserScript.SaveFilePanel(this, "SaveFileUsingPath", "NewPrefs", "txt");
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

	// Saves a file with the textToSave using a path
	private void SaveFileUsingPath(string path) {
		// Make sure path and _textToSave is not null or empty
		if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(textToSave)) {
			BinaryFormatter bFormatter = new BinaryFormatter();
			// Create a file using the path
			FileStream file = File.Create(path);
			// Serialize the data (textToSave)
			bFormatter.Serialize(file, textToSave);
			// Close the created file
			file.Close();
			launcher.Draw = true;
		}
		else {
			Debug.Log("Invalid path or empty file given");
		}
	}
}
