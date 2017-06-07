using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Agent;
using Global;

public class InputEventArgs : EventArgs {

	public string InputString { get; set; }

	public InputEventArgs(string str)
	{
		this.InputString = str;
	}
}

public class InputController : FontManager {
	public String inputLabel;
	public String inputString;
	public int fontSize = 12;
	public int inputHeight = 50;
	public Rect inputRect;

	public bool allowToggleAgent = true;

	String[] commands;
	EventManager eventManager;
	Macros macros;

	PluginImport commBridge;
	ObjectSelector objSelector;
	ExitToMenu exitToMenu;

	String disableEnable;

	GUIStyle textAreaStyle = new GUIStyle();

	GUIStyle labelStyle = new GUIStyle ("Label");
	GUIStyle textFieldStyle = new GUIStyle ("TextField");
	GUIStyle buttonStyle = new GUIStyle ("Button");

	float fontSizeModifier;

	TouchScreenKeyboard keyboard;

	public event EventHandler InputReceived;

	public void OnInputReceived(object sender, EventArgs e)
	{
		if (InputReceived != null)
		{
			InputReceived(this, e);
		}
	}

	public event EventHandler ParseComplete;

	public void OnParseComplete(object sender, EventArgs e)
	{
		if (ParseComplete != null)
		{
			ParseComplete(this, e);
		}
	}

	void Start() {
		GameObject bc = GameObject.Find ("BehaviorController");
		eventManager = bc.GetComponent<EventManager> ();
		macros = bc.GetComponent<Macros> ();

		objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
		exitToMenu = GameObject.Find ("BlocksWorld").GetComponent<ExitToMenu> ();

		commBridge = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ();

		labelStyle = new GUIStyle ("Label");
		textFieldStyle = new GUIStyle ("TextField");
		buttonStyle = new GUIStyle ("Button");
		labelStyle.fontSize = fontSize;
		textFieldStyle.fontSize = fontSize;
		buttonStyle.fontSize = fontSize;

		fontSizeModifier = (int)(fontSize / defaultFontSize);

		//inputRect = new Rect (5, 5, 50, 25);
		inputHeight = (int)(25*fontSizeModifier);

		inputRect = new Rect (5, 5, (int)(365*fontSizeModifier), inputHeight);
	}

	void Update() {
	}

	void OnGUI() {
#if !UNITY_IOS
		Event e = Event.current;
		if (e.keyCode == KeyCode.Return) {
			if (inputString != "") {

				MessageReceived (inputString);

				// warning: switching to TextArea here (and below) seems to cause crash
				GUILayout.BeginArea (inputRect);
				GUILayout.BeginHorizontal();
				GUILayout.Label(inputLabel+":", labelStyle);
				inputString = GUILayout.TextField("", textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
				GUILayout.EndHorizontal ();
				GUILayout.EndArea();

				//GUI.Label (inputRect, inputLabel+":");
				//inputString = GUI.TextField (inputRect, ""); 
			}
		}
		else {

			//GUI.Label (inputRect, inputLabel+":");
			//inputString = GUI.TextField (inputRect, inputString);
			GUILayout.BeginArea (inputRect);
			GUILayout.BeginHorizontal();
			GUILayout.Label(inputLabel+":", labelStyle);
			inputString = GUILayout.TextField(inputString, textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
			GUILayout.EndHorizontal ();
			GUILayout.EndArea();
		}
#else
		if (!TouchScreenKeyboard.visible) {
			if (inputString != "") {
				MessageReceived (inputString);

				GUILayout.BeginArea (inputRect);
				GUILayout.BeginHorizontal();
				GUILayout.Label(inputLabel+":", labelStyle);
				inputString = GUILayout.TextField("", textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
				GUILayout.EndHorizontal ();
				GUILayout.EndArea();
			}
			else {
				// warning: switching to TextArea here (and below) seems to cause crash
				GUILayout.BeginArea (inputRect);
				GUILayout.BeginHorizontal();
				GUILayout.Label(inputLabel+":", labelStyle);
				inputString = GUILayout.TextField("", textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
				GUILayout.EndHorizontal ();
				GUILayout.EndArea();

				//GUI.Label (inputRect, inputLabel+":");
				//inputString = GUI.TextField (inputRect, ""); 
			}
		}
		else {

			//GUI.Label (inputRect, inputLabel+":");
			//inputString = GUI.TextField (inputRect, inputString);
			GUILayout.BeginArea (inputRect);
			GUILayout.BeginHorizontal();
			GUILayout.Label(inputLabel+":", labelStyle);
			inputString = GUILayout.TextField(inputString, textFieldStyle, GUILayout.Width(300*fontSizeModifier), GUILayout.ExpandHeight (false));
			GUILayout.EndHorizontal ();
			GUILayout.EndArea();
		}
#endif


			/* DEBUG BUTTONS */

//		if (GUI.Button (new Rect (10, Screen.height - ((10 + (int)(20*exitToMenu.FontSizeModifier)) + (10 + (int)(40*fontSizeModifier))),
//			100*fontSizeModifier, 20*fontSizeModifier), "Reset", buttonStyle)) {
//			StartCoroutine(SceneHelper.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name));
//			return;
//		}

		if (GameObject.FindGameObjectWithTag ("Agent") != null) {
			if (allowToggleAgent) {
				disableEnable = (objSelector.disabledObjects.FirstOrDefault (t => t.tag == "Agent") == null) ? "Disable Agent" : "Enable Agent";
				if (GUI.Button (new Rect (10, Screen.height - ((10 + (int)(20 * exitToMenu.FontSizeModifier)) + (10 + (int)(40 * fontSizeModifier))),
					   100 * fontSizeModifier, 20 * fontSizeModifier), disableEnable, buttonStyle)) {
					GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
					if (agent != null) {
						if (agent.activeInHierarchy) {
							eventManager.preds.DISABLE (new object[]{ agent });
						}
					} else {
						agent = objSelector.disabledObjects.First (t => t.tag == "Agent");
						eventManager.preds.ENABLE (new object[]{ agent });
					}
					return;
				}
			}
		}
	}

	public void MessageReceived(String inputString) {
		Regex r = new Regex(@".*\(.*\)");
		string functionalCommand = "";

		if (inputString != "") {
			InputEventArgs inputArgs = new InputEventArgs (inputString);
			OnInputReceived (this, inputArgs);

			if (inputString == "reset") {
				StartCoroutine(SceneHelper.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name));
				return;
			}

			if (inputString == "repeat") {
				GameObject.Find ("BlocksWorld").GetComponent<ScenarioManager> ().scenarioScript.SendMessage("Repeat");
				return;
			}

			if (inputString == "that's all") {
				GameObject.Find ("BlocksWorld").GetComponent<ScenarioManager> ().scenarioScript.SendMessage("PlayGame");
				return;
			}

			Debug.Log ("User entered: " + inputString);
			if (!r.IsMatch (inputString)) { // is not already functional form
				// parse into functional form
				String[] inputs = inputString.Split(new char[]{'.',',','!'});
				List<String> commands = new List<String> ();
				foreach (String s in inputs) {
					if (s != String.Empty) {
						commands.Add(commBridge.NLParse(s.Trim().ToLower()));
					}
				}
				functionalCommand = String.Join (";", commands.ToArray());
			}
			else {
				functionalCommand = inputString;
			}

			if (functionalCommand.Count (x => x == '(') == functionalCommand.Count (x => x == ')')) {
				//eventManager.ClearEvents ();
				foreach (KeyValuePair<String,String> kv in macros.commandMacros) {	// if input is a macro
					if (functionalCommand == kv.Key) {									// sub in value
						functionalCommand = kv.Value;
						break;
					}
				}
				Debug.Log ("Parsed as: " + functionalCommand);
				InputEventArgs parseArgs = new InputEventArgs (functionalCommand);
				OnParseComplete (this, parseArgs);

				OutputHelper.PrintOutput (Role.Affector,"OK.");
				OutputHelper.PrintOutput (Role.Planner,"");
				commands = functionalCommand.Split (';');
				foreach (String commandString in commands) {
					// add to queue
					eventManager.QueueEvent (commandString);
				}

				if (eventManager.immediateExecution) {
					eventManager.ExecuteNextCommand ();
				}
			}
		}
	}
}
