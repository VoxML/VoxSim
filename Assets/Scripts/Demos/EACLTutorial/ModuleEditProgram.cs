using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.UI.ModalWindow;

public class ModuleEditProgram : ModalWindow {
	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle("Button");

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string[] listItems;

	List<string> programs = new List<string>();

	public List<string> Programs {
		get { return programs; }
		set {
			programs = value;
			listItems = programs.ToArray();
		}
	}

	int selected = -1;

	string actionButtonText;

	GhostFreeRoamCamera cameraControl;

	RaycastHit selectRayhit;
	float surfacePlacementOffset;

	// Use this for initialization
	void Start() {
		base.Start();

		actionButtonText = "Edit Program";
		windowTitle = "Edit Voxeme Program";
		persistent = true;

		buttonStyle = new GUIStyle("Button");

		fontSizeModifier = (int) (fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		string[] fileEntries = Directory.GetFiles(Data.voxmlDataPath + "/programs/", "*.xml");
		foreach (string s in fileEntries) {
			string fileName = s.Remove(0, (Data.voxmlDataPath + "/programs/").Length).Replace(".xml", "");
			Programs.Add(fileName);
		}

		listItems = Programs.ToArray();

		windowRect = new Rect(Screen.width - 215, 15 + (int) (40 * fontSizeModifier), 200, 200);

		windowManager.NewModalWindow += NewInspector;
		windowManager.ActiveWindowSaved += VoxMLUpdated;
	}

	// Update is called once per frame
	void Update() {
	}

	protected override void OnGUI() {
		if (GUI.Button(new Rect(
				Screen.width - (15 + (int) (110 * fontSizeModifier / 3)) + 38 * fontSizeModifier -
				(GUI.skin.label.CalcSize(new GUIContent(actionButtonText)).x + 10),
				10 + (int) (20 * fontSizeModifier), GUI.skin.label.CalcSize(new GUIContent(actionButtonText)).x + 10,
				20 * fontSizeModifier),
			actionButtonText, buttonStyle)) {
			render = true;
		}

		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		base.DoModalWindow(windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		selected = GUILayout.SelectionGrid(selected, listItems, 1, buttonStyle, GUILayout.ExpandWidth(true));
		GUILayout.EndScrollView();

		if (selected != -1) {
			render = false;

			VoxemeInspectorModalWindow newInspector =
				GameObject.Find("VoxWorld").AddComponent<VoxemeInspectorModalWindow>();
			newInspector.InspectorPosition = new Vector2(25, 25);
			newInspector.windowRect = new Rect(newInspector.InspectorPosition.x, newInspector.InspectorPosition.y,
				newInspector.inspectorWidth, newInspector.inspectorHeight);
			newInspector.InspectorVoxeme = "programs/" + listItems[selected];
			newInspector.Render = true;

			selected = -1;
		}
	}

	void NewInspector(object sender, EventArgs e) {
	}

	void VoxMLUpdated(object sender, EventArgs e) {
	}
}