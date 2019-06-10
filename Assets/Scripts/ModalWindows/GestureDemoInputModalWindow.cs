using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Network;
using VoxSimPlatform.UI.ModalWindow;

public class GestureDemoInputModalWindow : ModalWindow {
	FusionSocket fusionSocket;

	public int fontSize = 12;

	GUIStyle buttonStyle;
	private bool showSpeech = true;
	private bool showGesture = true;

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string actionButtonText;
	List<string> inputs = new List<string>();

	// Use this for initialization
	void Start() {
		base.Start();

		actionButtonText = "View Input Symbols";
		windowTitle = "Input Symbol Log";
		persistent = true;

		fontSizeModifier = (int) (fontSize / defaultFontSize);

		windowRect = new Rect(15, 15 + (int) (20 * fontSizeModifier), 200, 200);
	}

	// Update is called once per frame
	void Update() {
		if (fusionSocket == null) {
			fusionSocket = (FusionSocket)GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>().
                FindSocketConnectionByLabel("Fusion");
			if (fusionSocket != null) {
				fusionSocket.FusionReceived += ReceivedGesture;
			}
		}
	}

	protected override void OnGUI() {
		buttonStyle = new GUIStyle("Button");
		buttonStyle.fontSize = fontSize;

		Rect buttonRect = new Rect(10, 10,
			GUI.skin.label.CalcSize(new GUIContent(actionButtonText)).x + 10,
			20 * fontSizeModifier);
		if (GUI.Button(buttonRect, actionButtonText, buttonStyle)) {
			render = true;
		}

		showSpeech = GUI.Toggle(new Rect(buttonRect.x * 2 + buttonRect.width, buttonRect.y, 25, buttonRect.height),
			showSpeech, "S");
		showGesture =
			GUI.Toggle(new Rect(buttonRect.x * 2 + buttonRect.width + 30, buttonRect.y, 25, buttonRect.height),
				showGesture, "G");

		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		base.DoModalWindow(windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.BeginVertical(GUI.skin.box);
		foreach (string input in inputs) {
			GUILayout.Label(input);
		}

		GUILayout.EndVertical();
		GUILayout.EndScrollView();
	}

	private bool IsInputSpeech(string msg) {
		return msg.StartsWith("S");
	}

	private bool IsInputGesture(string msg) {
		return msg.StartsWith("G");
	}

	private bool IsInputPointing(string msg) {
		return msg.StartsWith("P");
	}

	void ReceivedGesture(object sender, EventArgs e) {
		string msg = ((FusionEventArgs) e).Content;
		if (!IsInputPointing(msg)) {
			bool showInModal = IsInputSpeech(msg) ? showSpeech : showGesture;
			Debug.Log(string.Format("\"{0}\", shown in scene: {1}", msg, showInModal));
			if (showInModal) {
				inputs.Add(string.Format("{0} {1}", msg.Split(';')[0], msg.Split(';')[1]));
			}
		}

		scrollPosition.y = Mathf.Infinity; // scroll to bottom
	}
}