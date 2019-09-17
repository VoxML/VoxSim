using UnityEngine;
using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Network;
using VoxSimPlatform.Vox;

public class DianaOzStudies : MonoBehaviour {
	void EventManager_EventComplete(object sender, EventArgs e) {
	}

	class CommanderStatus {
		public CommanderStatus(string _input, string _question, string _utter, string _anim, string _show, string _hide,
			string _clicked, string _state) {
			input = _input;
			question = _question;
			utter = _utter;
			anim = _anim;
			show = _show;
			hide = _hide;
			clicked = _clicked;
			state = _state;
		}

		public string input;
		public string question;
		public string utter;
		public string anim;
		public string show;
		public string hide;
		public string clicked;
		public string state;
	}

	GameObject restClient;
	string cmdrUrl = string.Empty;
	string lastReceivedData;

	Timer getTimer;
	float getInterval = 100;
	bool get = false;

	GameObject behaviorController;
	JointGestureDemo world;
	Predicates preds;
	ObjectSelector objSelector;
	EventManager eventManager;

	// Use this for initialization
	void Start() {
		restClient = new GameObject("RestClient");
		//restClient.AddComponent<RestClient>();

		behaviorController = GameObject.Find("BehaviorController");
		world = GameObject.Find("JointGestureDemo").GetComponent<JointGestureDemo>();
		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		preds = behaviorController.GetComponent<Predicates>();
		eventManager = behaviorController.GetComponent<EventManager>();

		if (PlayerPrefs.HasKey("URLs")) {
			string cmdrUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split('=')[0] == "Commander URL") {
					cmdrUrlString = url.Split('=')[1];
					cmdrUrl = !cmdrUrlString.StartsWith("http://") ? "http://" + cmdrUrlString : cmdrUrlString;
//					Debug.Log (cmdrUrl);
					//restClient.GetComponent<RestClient>().Post(cmdrUrl + "/init", "", "okay", "error");
					break;
				}
			}
		}

		restClient.GetComponent<RestClient>().GetOkay += ConsumeData;
		world.ObjectSelected += BlockClicked;
		world.PointSelected += PointClicked;
		eventManager.EventComplete += EventCompleted;

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
	void Update() {
		if (get) {
			StartCoroutine("GetCommanderInput");
			get = false;
		}
	}

	IEnumerator GetCommanderInput() {
		//restClient.GetComponent<RestClient>().Get(cmdrUrl + "/server", "okay", "error");
		yield return null;
	}

	private void PollCommandServer(object source, ElapsedEventArgs e) {
		get = true;

		// Reset timer
		getTimer.Interval = getInterval;
		getTimer.Enabled = true;
	}

	void ConsumeData(object sender, EventArgs e) {
		if (((RestEventArgs) e).Content is string) {
			if ((((RestEventArgs) e).Content.ToString() != string.Empty) &&
			    (((RestEventArgs) e).Content.ToString() != lastReceivedData)) {
				Debug.Log(((RestEventArgs) e).Content);
				CommanderStatus dict = JsonUtility.FromJson<CommanderStatus>(((RestEventArgs) e).Content.ToString());
				if (dict != null) {
//					Debug.Log(string.Format("input: \"{0}\", question: \"{1}\", utter: \"{2}\"",dict.input,dict.question,dict.utter));
					lastReceivedData = ((RestEventArgs) e).Content.ToString();

					if (dict.input != string.Empty) {
						((InputController) (GameObject.Find("IOController").GetComponent("InputController")))
							.inputString = dict.input.Trim();
						((InputController) (GameObject.Find("IOController").GetComponent("InputController")))
							.MessageReceived(dict.input.Trim());
					}
					else if (dict.question != string.Empty) {
						if (Regex.IsMatch(dict.question, @"The .+ block\?")) {
							string color = dict.question.Split()[1];
							MethodInfo methodToCall = preds.GetType().GetMethod(color.ToUpper());
							object obj = methodToCall.Invoke(preds, new object[] {world.availableObjs.ToArray()});
							if (obj != null) {
								world.ReachFor(GameObject.Find(obj as string));
							}

							world.RespondAndUpdate(dict.question);
						}
						else if (Regex.IsMatch(dict.question, @".*<.+; .+; .+>\?")) {
							string coord = Regex.Match(dict.question, @"<.+; .+; .+>").Value;
							world.RespondAndUpdate(Regex.Replace(dict.question, @"<.+; .+; .+>", "here"));
							world.ReachFor(Helper.ParsableToVector(coord));
						}
						else {
							world.RespondAndUpdate(dict.question);
						}
					}
					else if (dict.utter != string.Empty) {
						world.RespondAndUpdate(dict.utter);
					}
					else if (dict.hide != string.Empty) {
						GameObject obj = GameObject.Find(dict.hide);
						if (obj != null) {
							preds.DISABLE(new object[] {obj});
						}
					}
					else if (dict.show != string.Empty) {
						for (int i = 0; i < objSelector.disabledObjects.Count; i++) {
							if (objSelector.disabledObjects[i].name == dict.show) {
								preds.ENABLE(new object[] {objSelector.disabledObjects[i]});
							}
						}
					}
					else if (dict.anim != string.Empty) {
						world.MoveToPerform();
						world.gestureController.PerformGesture(dict.anim);
					}
				}
			}
		}
	}

	void BlockClicked(object sender, EventArgs e) {
		string color = (((SelectionEventArgs) e).Content as GameObject).GetComponent<Voxeme>().voxml.Attributes.Attrs[0]
			.Value;

		//restClient.GetComponent<RestClient>().Post(cmdrUrl + "/server",
			//JsonUtility.ToJson(new CommanderStatus("", "", "", "", "", "",
			//	string.Format("the {0} block", color), "")),
			//"okay", "error");
	}

	void PointClicked(object sender, EventArgs e) {
		//restClient.GetComponent<RestClient>().Post(cmdrUrl + "/server",
			//JsonUtility.ToJson(new CommanderStatus("", "", "", "", "", "",
			//	Helper.VectorToParsable((Vector3) ((SelectionEventArgs) e).Content), "")),
			//"okay", "error");
	}

	void EventCompleted(object sender, EventArgs e) {
		//restClient.GetComponent<RestClient>().Post(cmdrUrl + "/server",
			//JsonUtility.ToJson(new CommanderStatus("", "", "", "", "", "", "", "")),
			//"okay", "error");
	}
}