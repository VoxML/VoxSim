﻿using UnityEngine;
using System.Collections;

public class Help : ModalWindow {
	string helpText = "Available behaviors:\n" +
		"- put(x,f(y)), where f={on,in} -- places object x at position f(y)\n" +
		"- flip(x) -- flips object x over\n" +
		"- slide(x) -- slide object x in random direction\n\n" +
		"- grasp(x) -- agent grasps object x, if touching x\n" +
		"- drop(x) -- agent drops (releases) object x, if holding x\n\n" +
		"- disable(x) -- hide object x (use \"disable(human1)\" to hide agent)\n" +
		"- reset -- reload scene\n\n" +
		"Objects must be invoked by name.  " +
		"Right-click an object to inspect its semantic markup in VoxML.  " +
		"Object name displays in inspector title bar.\n\n" +
		"Click and drag to rotate camera.  Use arrow keys to move camera.  " +
		"Use S/W to raise/lower agent arm when visible.";

	// Use this for initialization
	void Start () {
		windowTitle = "Help";
		persistent = true;
	}
	
	// Update is called once per frame
	void Update () {
	}	

	protected override void OnGUI () {
		if (GUI.Button (new Rect (Screen.width-50, Screen.height-30, 38, 22), "Help"))
			render = true;

		base.OnGUI ();
	}

	public override void DoModalWindow(int windowID){
		base.DoModalWindow (windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		GUILayout.Label (helpText);
		GUILayout.EndScrollView ();
		//makes GUI window draggable
		GUI.DragWindow (new Rect (0, 0, 10000, 20));
	}
}