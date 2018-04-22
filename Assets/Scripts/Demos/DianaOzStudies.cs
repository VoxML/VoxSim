﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using UnityEngine;

using Episteme;
using Global;
using Network;

public class DianaOzStudies : MonoBehaviour {

	class CommanderStatus {
		public CommanderStatus(string _input, string _question, string _utter, string _clicked) {
			input = _input;
			question = _question;
			utter = _utter;
			clicked = _clicked;
		}

		public string input;
		public string question;
		public string utter;
		public string clicked;
	}

	GameObject restClient;
	string cmdrUrl = string.Empty;
	string lastReceivedData;

	Timer getTimer;
	float getInterval = 100;
	bool get = false;

	JointGestureDemo world;
	Predicates preds;

	// Use this for initialization
	void Start () {
		restClient = new GameObject("RestClient");
		restClient.AddComponent<RestClient>();

		world = GameObject.Find ("JointGestureDemo").GetComponent<JointGestureDemo> ();
		preds = GameObject.Find ("BehaviorController").GetComponent<Predicates> ();

		if (PlayerPrefs.HasKey ("URLs")) {
			string cmdrUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Commander URL") {
					cmdrUrlString = url.Split ('=') [1];
					cmdrUrl = !cmdrUrlString.StartsWith ("http://") ? "http://" + cmdrUrlString : cmdrUrlString;
					Debug.Log (cmdrUrl);
					restClient.GetComponent<RestClient>().Post(cmdrUrl + "/init", "", "okay", "error");
					break;
				}
			}
		}

		restClient.GetComponent<RestClient>().GotData += ConsumeData;
		world.ObjectSelected += BlockClicked;
		world.PointSelected += PointClicked;

		// Create a timer
		getTimer = new Timer();
		// Tell the timer what to do when it elapses
		getTimer.Elapsed += new ElapsedEventHandler(PollCommandServer);
		// Set it to go off every second
		getTimer.Interval = getInterval;
		// And start it        
		getTimer.Enabled = true;
	}

	// Update is called once per frame
	void Update () {
		if (get) {
			StartCoroutine ("GetCommanderInput");
			get = false;
		}
	}

	IEnumerator GetCommanderInput() {
		restClient.GetComponent<RestClient>().Get(cmdrUrl + "/server","okay", "error");
		yield return null;
	}

	private void PollCommandServer(object source, ElapsedEventArgs e) {
		get = true;

		// Reset timer
		getTimer.Interval = getInterval;
		getTimer.Enabled = true;
	}

	void ConsumeData(object sender, EventArgs e) {
		if (((RestEventArgs)e).Content is string) {
			if ((((RestEventArgs)e).Content.ToString() != string.Empty) && (((RestEventArgs)e).Content.ToString() != lastReceivedData)) {
				Debug.Log (((RestEventArgs)e).Content);
				CommanderStatus dict = JsonUtility.FromJson<CommanderStatus>(((RestEventArgs)e).Content.ToString());
				if (dict != null) {
//					Debug.Log(string.Format("input: \"{0}\", question: \"{1}\", utter: \"{2}\"",dict.input,dict.question,dict.utter));
					lastReceivedData = ((RestEventArgs)e).Content.ToString ();

					if (dict.input != string.Empty) {
						((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).inputString = dict.input.Trim();
						((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).MessageReceived(dict.input.Trim());
					}
					else if (dict.question != string.Empty) {
						if (Regex.IsMatch (dict.question, @"The .+ block\?")) {
							string color = dict.question.Split () [1];
							MethodInfo methodToCall = preds.GetType ().GetMethod (color.ToUpper ());
							object obj = methodToCall.Invoke (preds, new object[]{ world.blocks.ToArray () });
							if (obj != null) {
								world.ReachFor (GameObject.Find (obj as string));
							}
							world.RespondAndUpdate (dict.question);
						}
						else if (Regex.IsMatch (dict.question, @".*<.+; .+; .+>\?")) {
							string coord = Regex.Match (dict.question, @"<.+; .+; .+>").Value;
							world.RespondAndUpdate (Regex.Replace(dict.question,@"<.+; .+; .+>","here"));
							world.ReachFor (Helper.ParsableToVector (coord));
						}
						else {
							world.RespondAndUpdate (dict.question);
						}
					}
					else if (dict.utter != string.Empty) {
						world.RespondAndUpdate (dict.utter);
					}
				}
			}
		}
	}

	void BlockClicked(object sender, EventArgs e) {
		string color = (((SelectionEventArgs)e).Content as GameObject).GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;

		restClient.GetComponent<RestClient>().Post(cmdrUrl + "/server",
			JsonUtility.ToJson(new CommanderStatus("","","",string.Format("the {0} block",color))),
				"okay", "error");
	}

	void PointClicked(object sender, EventArgs e) {
		restClient.GetComponent<RestClient>().Post(cmdrUrl + "/server",
			JsonUtility.ToJson(new CommanderStatus("","","",Helper.VectorToParsable((Vector3)((SelectionEventArgs)e).Content))),
				"okay", "error");
	}
}