using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Logging {
        public class LoggerArgs : EventArgs {
        	public string LogString { get; set; }

        	public LoggerArgs(string str) {
        		this.LogString = str;
        	}
        }

        public class GenericLogger : MonoBehaviour {
        //	[HideInInspector]
        //	public bool moveLogged;

        	[HideInInspector] public float logTimer;

        //	protected EventManager eventManager;
        //
        //	protected InputController inputController;

        	protected bool logTimestamps;
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
        		logTimestamps = (PlayerPrefs.GetInt("Timestamps") == 1);
        		//
        //		// log default state
        //		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        //		foreach (GameObject o in allObjects) {
        //			if (o.GetComponent<Voxeme> () != null) {
        //				if (o.GetComponent<Voxeme> ().enabled) {
        //					//Debug.Log (o.name);
        //					defaultState.Add (o.name, o.transform.position);
        //				}
        //			}
        //		}

        		LogEvent += LogEventReceived;
        	}

        	// Update is called once per frame
        	public void Update() {
        		logTimer += Time.deltaTime;
        	}

        	public void OpenLog(String name) {
        //		if (!log) {
        //			return;
        //		}

        		if (name == String.Empty) {
        			name = "Temp";
        		}

        		if (!Directory.Exists("Logs")) {
        			Directory.CreateDirectory("Logs");
        		}


        		if (!Directory.Exists(string.Format("Logs/{0}", name))) {
        			Directory.CreateDirectory(string.Format("Logs/{0}", name));
        		}

        		string dateTime = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
        		logFile = new StreamWriter(string.Format("Logs/{0}/{1}-{2}.txt", name, name, dateTime));

        //		logFile.WriteLine (string.Format ("Structure: {0}", name));
        //		string modalityString = string.Empty;
        //		modalityString += ((int)(modality & OutputModality.Modality.Gestural) == (int)OutputModality.Modality.Gestural) ? "Gestural" : string.Empty;
        //		modalityString += " ";
        //		modalityString += ((int)(modality & OutputModality.Modality.Linguistic) == (int)OutputModality.Modality.Linguistic) ? "Linguistic" : string.Empty;
        //		modalityString = String.Join(", ", modalityString.Split ());
        //		logFile.WriteLine (string.Format ("Modality: {0}", modalityString));
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
        		if (PlayerPrefs.GetInt("Make Logs") == 1) {
        			logFile.WriteLine(string.Format("{0}\t{1}", content, logTimestamps ? logTimer.ToString() : string.Empty));
        		}
        	}

        	public void CloseLog() {
        		if (logFile == null)
        			return;

        		try {
        			logFile.Close();
        		}
        		catch (Exception e) {
        		}
        	}

        	void LogEventReceived(object sender, EventArgs e) {
        		Log(((LoggerArgs) e).LogString);
        	}
        }
    }
}