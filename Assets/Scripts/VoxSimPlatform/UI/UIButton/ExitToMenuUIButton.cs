using UnityEngine;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace UI {
        namespace UIButtons {
            public class ExitToMenuUIButton : UIButton {
            	public int fontSize = 12;

            	GUIStyle buttonStyle;

            	// Use this for initialization
            	void Start() {
            		FontSizeModifier = (int) (fontSize / defaultFontSize);

            		base.Start();
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	protected override void OnGUI() {
            		buttonStyle = new GUIStyle("Button");
            		buttonStyle.fontSize = fontSize;

            		if (GUI.Button(buttonRect, buttonText, buttonStyle)) {
            			StartCoroutine(SceneHelper.LoadScene("VoxSimMenu"));
            			return;
            		}

            		base.OnGUI();
            	}

            	public override void DoUIButton(int buttonID) {
            		base.DoUIButton(buttonID);
            	}
            }
        }
    }
}