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

	GUIStyle buttonStyle = new GUIStyle ("Button");

	float fontSizeModifier;	
	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string[] verbosityListItems = { "Everything", "Disambiguation only", "None" };
	string[] disambiguationListItems = { "Elimination", "Deictic/Gestural" };

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

	public VerbosityLevel verbosityLevel = VerbosityLevel.Disambiguation;
	public DisambiguationStrategy disambiguationStrategy = DisambiguationStrategy.DeicticGestural;

	string actionButtonText;

	// Use this for initialization
	void Start () {
		base.Start ();

		actionButtonText = "Interaction Prefs";
		windowTitle = "Gesture Interaction Prefs";
		persistent = true;

		buttonStyle = new GUIStyle ("Button");

		fontSizeModifier = (int)(fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		windowRect = new Rect (Screen.width - 215, 15 + (int)(20 * fontSizeModifier), 200, 200);
	}

	// Update is called once per frame
	void Update () {

	}	

	protected override void OnGUI () {

		if (GUI.Button (new Rect (Screen.width - (15 + (int)(110 * fontSizeModifier / 3)) + 38 * fontSizeModifier - (GUI.skin.label.CalcSize (new GUIContent (actionButtonText)).x + 10),
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
		GUILayout.BeginVertical(GUI.skin.box);
		GUILayout.Label ("Confirmation Verbosity:");
		GUILayout.BeginVertical(GUI.skin.box);
		verbosityLevel = (VerbosityLevel)GUILayout.SelectionGrid((int)verbosityLevel, verbosityListItems, 1, new GUIStyle ("Toggle"), GUILayout.ExpandWidth(true));
		GUILayout.EndVertical();
		GUILayout.EndVertical();

		GUILayout.BeginVertical(GUI.skin.box);
		GUILayout.Label ("Disambiguation Strategy:");
		GUILayout.BeginVertical(GUI.skin.box);
		disambiguationStrategy = (DisambiguationStrategy)GUILayout.SelectionGrid((int)disambiguationStrategy, disambiguationListItems, 1, new GUIStyle ("Toggle"), GUILayout.ExpandWidth(true));
		GUILayout.EndVertical();
		GUILayout.EndVertical();
		GUILayout.EndScrollView ();
	}
}