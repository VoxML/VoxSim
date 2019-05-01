using UnityEngine;

namespace VoxSimPlatform {
    namespace UI {
        namespace UIButtons {
            public enum UIButtonPosition {
            	TopLeft,
            	TopRight,
            	BottomLeft,
            	BottomRight
            };

            public class UIButton : FontManager {
            	public Rect buttonRect;
            	public string buttonText;
            	public int id;

            	public bool Draw {
            		get { return draw; }
            		set { draw = value; }
            	}

            	bool draw;

            	float fontSizeModifier;

            	[HideInInspector]
            	public float FontSizeModifier {
            		get { return fontSizeModifier; }
            		set { fontSizeModifier = value; }
            	}

            	public UIButtonPosition position;
            	public Vector2 offset, dimensions;

            	protected UIButtonManager buttonManager;

            	// Use this for initialization
            	protected virtual void Start() {
            		buttonManager = GameObject.Find("VoxWorld").GetComponent<UIButtonManager>();

            		id = buttonManager.buttonManager.Count;

            		draw = true;

            		if (!buttonManager.buttonManager.ContainsKey(id)) {
            			buttonManager.RegisterButton(this);
            		}
            		else {
            			Debug.Log("UIButton of id " + id + " already exists on this object!");
            			Destroy(this);
            		}

            		if (position == UIButtonPosition.TopLeft) {
            			//int count = buttonManager.CountButtonsAtPosition (UIButtonPosition.TopLeft);
            			buttonRect = new Rect(buttonManager.windowPort.x + 10 + offset.x,
            				buttonManager.windowPort.y + 10 + offset.y, dimensions.x, dimensions.y);
            		}
            		else if (position == UIButtonPosition.TopRight) {
            			//int count = buttonManager.CountButtonsAtPosition (UIButtonPosition.TopRight);
            			buttonRect = new Rect(
            				(buttonManager.windowPort.x + buttonManager.windowPort.width) - (10 + offset.x + dimensions.x),
            				buttonManager.windowPort.y + 10 + offset.y, dimensions.x, dimensions.y);
            		}
            		else if (position == UIButtonPosition.BottomLeft) {
            			//int count = buttonManager.CountButtonsAtPosition (UIButtonPosition.BottomLeft);
            			buttonRect = new Rect(buttonManager.windowPort.x + 10 + offset.x,
            				(buttonManager.windowPort.y + buttonManager.windowPort.height) - (10 + offset.y + dimensions.y),
            				dimensions.x, dimensions.y);
            		}
            		else if (position == UIButtonPosition.BottomRight) {
            			//int count = buttonManager.CountButtonsAtPosition (UIButtonPosition.BottomRight);
            			buttonRect = new Rect(
            				(buttonManager.windowPort.x + buttonManager.windowPort.width) - (10 + offset.x + dimensions.x),
            				(buttonManager.windowPort.y + buttonManager.windowPort.height) - (10 + offset.y + dimensions.y),
            				dimensions.x, dimensions.y);
            		}
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	protected virtual void OnGUI() {
            		if (!draw) {
            			return;
            		}

            		//GUILayout automatically lays out the GUI window to contain all the text
            		//GUI.Button(buttonRect, buttonText);
            	}

            	public virtual void DoUIButton(int buttonID) {
            		//Debug.Log (buttonID);
            	}

            	public virtual void DestroyButton() {
            		buttonManager.UnregisterButton(this);
            		Destroy(this);
            	}
            }
        }
    }
}