using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using Object = UnityEngine.Object;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Vox;

public class LogEventArgs : EventArgs {
	public string LogString { get; set; }

	public LogEventArgs(string str) {
		this.LogString = str;
	}
}

public class DemoScript : MonoBehaviour {
	[HideInInspector] public bool moveLogged;

	[HideInInspector] public float logTimer;

	protected EventManager eventManager;

	protected InputController inputController;

	protected bool log;
	StreamWriter logFile;

	public Dictionary<string, Vector3> defaultState = new Dictionary<string, Vector3>();

	public event EventHandler LogEvent;

	public void OnLogEvent(object sender, EventArgs e) {
		if (LogEvent != null) {
			LogEvent(this, e);
		}
	}

	// Use this for initialization
	public void Start() {
		log = (PlayerPrefs.GetInt("Make Logs") == 1);

		// log default state
		GameObject[] allObjects = FindObjectsOfType<GameObject>();
		foreach (GameObject o in allObjects) {
			if (o.GetComponent<Voxeme>() != null) {
				if (o.GetComponent<Voxeme>().enabled) {
					//Debug.Log (o.name);
					defaultState.Add(o.name, o.transform.position);
				}
			}
		}

		LogEvent += LogEventReceived;
	}

	// Update is called once per frame
	public void Update() {
		logTimer += Time.deltaTime;
	}

	protected void OpenLog(String name, OutputModality.Modality modality) {
		if (!log) {
			return;
		}

		if (!Directory.Exists("Logs")) {
			Directory.CreateDirectory("Logs");
		}


		if (!Directory.Exists(string.Format("Logs/{0}", name))) {
			Directory.CreateDirectory(string.Format("Logs/{0}", name));
		}

		string dateTime = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
		logFile = new StreamWriter(string.Format("Logs/{0}/{1}-{2}.txt", name, name, dateTime));

		logFile.WriteLine(string.Format("Structure: {0}", name));
		string modalityString = string.Empty;
		modalityString +=
			((int) (modality & OutputModality.Modality.Gestural) == (int) OutputModality.Modality.Gestural)
				? "Gestural"
				: string.Empty;
		modalityString += " ";
		modalityString +=
			((int) (modality & OutputModality.Modality.Linguistic) == (int) OutputModality.Modality.Linguistic)
				? "Linguistic"
				: string.Empty;
		modalityString = String.Join(", ", modalityString.Split());
		logFile.WriteLine(string.Format("Modality: {0}", modalityString));
	}

	protected string MakeLogString(params string[] strings) {
		string outStr = string.Empty;
		foreach (string str in strings) {
			outStr += str;
		}

		return outStr;
	}

	protected string FormatLogUtterance(string utterance) {
		return string.Format("\"{0}\"", utterance);
	}

	protected void Log(string content) {
		if (!log) {
			return;
		}

		if (!moveLogged) {
			logFile.WriteLine(string.Format("{0}\t{1}", logTimer.ToString(), content));
		}
	}

	protected void CloseLog() {
		if (!log) {
			return;
		}

		logFile.Close();
	}

	void LogEventReceived(object sender, EventArgs e) {
		Log(((LogEventArgs) e).LogString);
	}

	protected void EventsForceCleared(object sender, EventArgs e) {
		OnLogEvent(this, new LogEventArgs("Wizard: Force clear event queue"));
	}
}