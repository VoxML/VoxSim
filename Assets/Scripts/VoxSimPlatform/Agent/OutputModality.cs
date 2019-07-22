using UnityEngine;

using VoxSimPlatform.UI;

// TODO: to be deprecated by end of refactor

namespace VoxSimPlatform {
    namespace Agent {
        public class OutputModality : FontManager {
        	public enum Modality {
        		Linguistic = 1,
        		Gestural = (1 << 1)
        	}

        	public Modality modality = (Modality.Linguistic | Modality.Gestural);

        	public int fontSize = 12;

        	GUIStyle buttonStyle;

        	float fontSizeModifier;

        	HelpModalWindow help;

        	// Use this for initialization
        	void Start() {
        		fontSizeModifier = (int) (fontSize / defaultFontSize);

        		help = GameObject.Find("Help").GetComponent<HelpModalWindow>();
        	}

        	// Update is called once per frame
        	void Update() {
        		if ((int) (modality & Modality.Linguistic) == 0) {
        			OutputHelper.PrintOutput(Role.Planner, "");
        		}
        	}

        	protected void OnGUI() {
        		buttonStyle = new GUIStyle("Button");
        		buttonStyle.fontSize = fontSize;

        		string buttonText = ((int) (modality & Modality.Linguistic) == 1) ? "Language Off" : "Language On";

        		float buttonWidth = buttonStyle.CalcSize(new GUIContent(buttonText)).x + (14 * fontSizeModifier);

        		if (GUI.Button(new Rect(Screen.width - buttonWidth - 12,
        			Screen.height - ((10 + (int) (20 * help.FontSizeModifier)) + (5 + (int) (20 * fontSizeModifier))),
        			buttonWidth, 20 * fontSizeModifier), buttonText, buttonStyle)) {
        			modality ^= Modality.Linguistic;
        		}
        	}
        }
    }
}