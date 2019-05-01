using UnityEngine;

using VoxSimPlatform.Core;
using VoxSimPlatform.UI;
using VoxSimPlatform.UI.UIButtons;

public class ClearEvents : FontManager {
	public int fontSize = 12;

	protected GUIStyle buttonStyle = new GUIStyle("Button");

	protected ExitToMenuUIButton exitToMenu;

	EventManager eventManager;

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	// Use this for initialization
	protected void Start() {
		exitToMenu = GameObject.Find("VoxWorld").GetComponent<ExitToMenuUIButton>();

		buttonStyle = new GUIStyle("Button");

		fontSizeModifier = (int) (fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
	}

	// Update is called once per frame
	protected void Update() {
	}

	protected virtual void OnGUI() {
		if (GUI.Button(new Rect(10,
			Screen.height - ((10 + (int) (45 * exitToMenu.FontSizeModifier)) + (5 + (int) (20 * fontSizeModifier))),
			100 * fontSizeModifier, 20 * fontSizeModifier), "Clear Events", buttonStyle)) {
			eventManager.SendMessage("ClearEvents");
			return;
		}
	}
}