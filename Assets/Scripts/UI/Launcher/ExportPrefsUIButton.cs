using UnityEngine;
using System.Collections;

using Global;

public class ExportPrefsUIButton : UIButton {

	public int fontSize = 12;

	GUIStyle buttonStyle;

	// Use this for initialization
	void Start () {
		FontSizeModifier = (int)(fontSize / defaultFontSize);

		base.Start ();
	}

	// Update is called once per frame
	void Update () {

	}

	protected override void OnGUI () {
		buttonStyle = new GUIStyle ("Button");
		buttonStyle.fontSize = fontSize;

		if (GUI.Button (buttonRect, buttonText, buttonStyle)) {
			return;
		}

		base.OnGUI ();
	}

	public override void DoUIButton(int buttonID){

		base.DoUIButton (buttonID);
	}
}
