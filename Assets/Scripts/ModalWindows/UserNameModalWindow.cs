using UnityEngine;
using System;

using VoxSimPlatform.UI.ModalWindow;

public class UserNameInfo {
	public string Username;

	public UserNameInfo(string username) {
		this.Username = username;
	}
}

public class UserNameModalWindow : ModalWindow {
	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle("Button");

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string username = string.Empty;

	public event EventHandler UserNameEvent;

	public void OnUserNameEvent(object sender, EventArgs e) {
		if (UserNameEvent != null) {
			UserNameEvent(this, e);
		}
	}

	// Use this for initialization
	void Start() {
		base.Start();

		windowTitle = "Enter User Name/ID";
		persistent = true;

		buttonStyle = new GUIStyle("Button");

		fontSizeModifier = (int) (fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;
	}

	// Update is called once per frame
	void Update() {
	}

	protected override void OnGUI() {
		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		base.DoModalWindow(windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		GUILayout.BeginHorizontal();
		username = GUILayout.TextField(username, 25, GUILayout.Width(100));
		GUI.enabled = (username != string.Empty);
		if (GUILayout.Button("OK", GUILayout.Width(50))) {
			OnUserNameEvent(this, new ModalWindowEventArgs(windowID, new UserNameInfo(username)));
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
	}

	public void CloseWindow(ModalWindowEventArgs e) {
		windowManager.windowManager[((ModalWindowEventArgs) e).WindowID].DestroyWindow();
	}
}