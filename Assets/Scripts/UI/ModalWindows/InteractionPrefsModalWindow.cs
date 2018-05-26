using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Global;
using Vox;

public class InteractionPrefsModalWindow : ModalWindow {

	public int fontSize = 12;

	GUIStyle buttonStyle;

	float fontSizeModifier;	
	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string[] verbosityListItems = { "Everything", "Disambiguation only", "None" };
	string[] disambiguationListItems = { "Elimination", "Deictic/Gestural" };
	string[] deixisListItems = { "Screen", "Table" };

	List<string> programs = new List<string>();
	public List<string> Programs {
		get { return programs; }
		set {
			programs = value;
			verbosityListItems = programs.ToArray ();
		}
	}

	public enum VerbosityLevel
	{
		Everything,
		Disambiguation,
		None
	};

	public enum DisambiguationStrategy
	{
		Elimination,
		DeicticGestural
	};

	public enum DeixisMethod
	{
		Screen,
		Table
	};

	public VerbosityLevel verbosityLevel = VerbosityLevel.Disambiguation;
	public DisambiguationStrategy disambiguationStrategy = DisambiguationStrategy.DeicticGestural;
	public DeixisMethod deixisMethod = DeixisMethod.Screen;

	public bool useTeachingAgent = false;
	public bool showSyntheticVision = false;
	public bool showVisualMemory = false;

	string actionButtonText;

	// Use this for initialization
	void Start () {
		base.Start ();

		actionButtonText = "Interaction Prefs";
		windowTitle = "Gesture Interaction Prefs";
		persistent = true;

		fontSizeModifier = (int)(fontSize / defaultFontSize);

		windowRect = new Rect (Screen.width - 215, 15 + (int)(20 * fontSizeModifier), 200, 200);
	}

	// Update is called once per frame
	void Update () {

	}	

	protected override void OnGUI () {
		buttonStyle = new GUIStyle ("Button");
		buttonStyle.fontSize = fontSize;

		if (GUI.Button (new Rect (Screen.width - (10 + (int)(110 * fontSizeModifier / 3)) + 38 * fontSizeModifier - (GUI.skin.label.CalcSize (new GUIContent (actionButtonText)).x + 10),
			10, GUI.skin.label.CalcSize (new GUIContent (actionButtonText)).x + 10, 20 * fontSizeModifier),
			actionButtonText, buttonStyle)) {
			render = true;
		}

		base.OnGUI ();
	}

	public override void DoModalWindow(int windowID){

		base.DoModalWindow (windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
//		GUILayout.BeginVertical(GUI.skin.box);
//		GUILayout.Label ("Confirmation Verbosity:");
//		GUILayout.BeginVertical(GUI.skin.box);
//		verbosityLevel = (VerbosityLevel)GUILayout.SelectionGrid((int)verbosityLevel, verbosityListItems, 1, new GUIStyle ("Toggle"), GUILayout.ExpandWidth(true));
//		GUILayout.EndVertical();
//		GUILayout.EndVertical();
//
//		GUILayout.BeginVertical(GUI.skin.box);
//		GUILayout.Label ("Disambiguation Strategy:");
//		GUILayout.BeginVertical(GUI.skin.box);
//		disambiguationStrategy = (DisambiguationStrategy)GUILayout.SelectionGrid((int)disambiguationStrategy, disambiguationListItems, 1, new GUIStyle ("Toggle"), GUILayout.ExpandWidth(true));
//		GUILayout.EndVertical();
//		GUILayout.EndVertical();
//		GUILayout.EndScrollView ();

		GUILayout.BeginVertical(GUI.skin.box);
		GUILayout.Label ("Deixis:");
		GUILayout.BeginVertical(GUI.skin.box);
		deixisMethod = (DeixisMethod)GUILayout.SelectionGrid((int)deixisMethod, deixisListItems, 1, new GUIStyle ("Toggle"), GUILayout.ExpandWidth(true));
		GUILayout.EndVertical();
		GUILayout.EndVertical();

		GUILayout.BeginHorizontal(GUI.skin.box);
		useTeachingAgent = GUILayout.Toggle (useTeachingAgent, "Use Teaching Agent", GUILayout.ExpandWidth (true));
		GUILayout.EndHorizontal();

		GUILayout.BeginVertical(GUI.skin.box);
		GUILayout.Label ("Agent Perception:");
		GUILayout.BeginVertical(GUI.skin.box);
		showSyntheticVision = GUILayout.Toggle (showSyntheticVision, "Show Synthetic Vision", GUILayout.ExpandWidth (true));
		showVisualMemory = GUILayout.Toggle (showVisualMemory, "Show Visual Memory", GUILayout.ExpandWidth (true));
		GUILayout.EndVertical();
		GUILayout.EndVertical();

		GUILayout.EndScrollView ();
	}
}