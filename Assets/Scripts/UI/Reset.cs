using UnityEngine;
using System.Collections;

using Global;

public class Reset : UIButton {

	public int fontSize = 12;

	protected GUIStyle buttonStyle = new GUIStyle ("Button");

	// Use this for initialization
	protected void Start () {
		buttonStyle = new GUIStyle ("Button");
		FontSizeModifier = (int)(fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		base.Start ();
	}

	// Update is called once per frame
	void Update () {

	}	

	protected virtual void OnGUI () {
		if (GUI.Button (buttonRect, buttonText, buttonStyle)) {
			StartCoroutine(SceneHelper.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name));
			return;
		}

		base.OnGUI ();
	}
}
