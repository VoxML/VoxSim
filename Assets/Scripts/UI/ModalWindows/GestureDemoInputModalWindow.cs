using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Global;
using Network;

public class GestureDemoInputModalWindow : ModalWindow {

	CSUClient csuClient;

	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle ("Button");

	float fontSizeModifier;	
	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string actionButtonText;
	List<string> inputs = new List<string>();

	// Use this for initialization
	void Start () {
		base.Start ();

		actionButtonText = "View Gesture Input";
		windowTitle = "Gesture Input Log";
		persistent = true;

		buttonStyle = new GUIStyle ("Button");

		fontSizeModifier = (int)(fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		windowRect = new Rect (15, 15 + (int)(20 * fontSizeModifier), 200, 200);
	}

	// Update is called once per frame
	void Update () {
		if (csuClient == null) {
			csuClient = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ().CSUClient;
			csuClient.GestureReceived += ReceivedGesture;
		}
	}	

	protected override void OnGUI () {

		if (GUI.Button (new Rect (15, 10, GUI.skin.label.CalcSize (new GUIContent (actionButtonText)).x + 10, 20 * fontSizeModifier),
			actionButtonText, buttonStyle)) {
			render = true;
		}

		base.OnGUI ();
	}

	public override void DoModalWindow(int windowID){

		base.DoModalWindow (windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		GUILayout.BeginVertical(GUI.skin.box);
		foreach (string input in inputs) {
			GUILayout.Label (input);
		}
		GUILayout.EndVertical();
		GUILayout.EndScrollView ();
	}

	void ReceivedGesture(object sender, EventArgs e) {
		string msg = ((GestureEventArgs)e).Content;
		if (!msg.StartsWith ("P")) {
			inputs.Add (string.Format ("{0} {1}", msg.Split (';') [0], msg.Split (';') [1]));
		}
		scrollPosition.y = Mathf.Infinity;	// scroll to bottom
	}
}