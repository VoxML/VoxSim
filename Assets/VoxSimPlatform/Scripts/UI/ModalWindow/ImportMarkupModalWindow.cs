using UnityEngine;
using System;
using System.IO;

using VoxSimPlatform.Global;
using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.Vox;

public class ImportMarkupEventArgs : EventArgs {
	public string ImportPath { get; set; }
	public bool MacroEvent { get; set; }

	public ImportMarkupEventArgs(string path) {
		this.ImportPath = path;
	}
}

public class ImportMarkupModalWindow : ModalWindow {
	public int fontSize = 12;

	GUIStyle buttonStyle;

	public int selected = -1;

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	public VoxEntity.EntityType entityType;

	string importPath;
	string[] importListItems;

	public event EventHandler ItemSelected;

	public void OnItemSelected(object sender, EventArgs e) {
		if (ItemSelected != null) {
			ItemSelected(this, e);
		}
	}

	void Start() {
		//persistent = true;
		fontSizeModifier = (int) (fontSize / defaultFontSize);

		base.Start();
	}

	// Update is called once per frame
	void Update() {
	}

	protected override void OnGUI() {
		buttonStyle = new GUIStyle("Button");
		buttonStyle.fontSize = fontSize;

		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		base.DoModalWindow(windowID);

		switch (entityType) {
			case VoxEntity.EntityType.Object:
				importPath = Data.voxmlDataPath + "/objects/";
				break;

			case VoxEntity.EntityType.Program:
				importPath = Data.voxmlDataPath + "/programs/";
				break;

			case VoxEntity.EntityType.Attribute:
				importPath = Data.voxmlDataPath + "/attributes/";
				break;

			case VoxEntity.EntityType.Relation:
				importPath = Data.voxmlDataPath + "/relations/";
				break;

			case VoxEntity.EntityType.Function:
				importPath = Data.voxmlDataPath + "/functions/";
				break;

			default:
				break;
		}

		importListItems = Directory.GetFiles(importPath, "*.xml");
		for (int i = 0; i < importListItems.Length; i++) {
			importListItems[i] = importListItems[i].Remove(0, importPath.Length).Replace(".xml", "");
		}

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		selected = GUILayout.SelectionGrid(selected, importListItems, 1, buttonStyle, GUILayout.ExpandWidth(true));
		GUILayout.EndScrollView();

		if (selected != -1) {
			OnItemSelected(this, new ImportMarkupEventArgs(importPath + importListItems[selected] + ".xml"));
			windowManager.UnregisterWindow(this);
			Destroy(this);
		}
	}
}