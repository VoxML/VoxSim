using UnityEngine;
using System;

using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.Vox;

public class SaveAsInfo {
	public string FileName;
	public VoxEntity.EntityType EntityType;

	public SaveAsInfo(string fileName, VoxEntity.EntityType entityType) {
		this.FileName = fileName;
		this.EntityType = entityType;
	}
}

public class SaveAsModalWindow : ModalWindow {
	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle("Button");

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string newFile = string.Empty;
	public VoxEntity.EntityType entityType;

	public event EventHandler SaveAsEvent;

	public void OnSaveAsEvent(object sender, EventArgs e) {
		if (SaveAsEvent != null) {
			SaveAsEvent(this, e);
		}
	}

	// Use this for initialization
	void Start() {
		base.Start();

		windowTitle = "Save As...";
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
		newFile = GUILayout.TextField(newFile, 25, GUILayout.Width(100));
		if (GUILayout.Button("Save", GUILayout.Width(50))) {
			OnSaveAsEvent(this, new ModalWindowEventArgs(windowID, new SaveAsInfo(newFile, entityType)));
		}

		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
	}
}