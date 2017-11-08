using UnityEngine;
using System.Collections;

using Global;

public class ExitToMenu : UIButton {

	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle ("Button");

	// Use this for initialization
	void Start () {
		buttonStyle = new GUIStyle ("Button");
		FontSizeModifier = (int)(fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		base.Start ();
	}
	
	// Update is called once per frame
	void Update () {

	}
		
	protected override void OnGUI () {
		if (GUI.Button (buttonRect, buttonText, buttonStyle)) {
			StartCoroutine(SceneHelper.LoadScene ("VoxSimMenu"));
			return;
		}

		base.OnGUI ();
	}

	public override void DoUIButton(int buttonID){

		base.DoUIButton (buttonID);
	}

}
