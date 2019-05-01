using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;

namespace VoxSimPlatform {
    namespace UI {
        namespace UIButtons {
            public class UIButtonEventArgs : EventArgs {
            	public int UIButtonID { get; set; }
            	public object Data { get; set; }

            	public UIButtonEventArgs(int buttonID, object data = null) {
            		this.UIButtonID = buttonID;
            		this.Data = data;
            	}
            }

            public class UIButtonManager : MonoBehaviour {
            	public Rect windowPort;

            	public Dictionary<int, UIButton> buttonManager = new Dictionary<int, UIButton>();

            	public event EventHandler NewUIButton;

            	public void OnNewUIButton(object sender, EventArgs e) {
            		if (NewUIButton != null) {
            			NewUIButton(this, e);
            		}
            	}

            	void Awake() {
            		windowPort = new Rect(0, 0, Screen.width, Screen.height);
            	}

            	// Use this for initialization
            	void Start() {
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	public void RegisterButton(UIButton button) {
            		Debug.Log(string.Format("Register {0}:{1}", this, button.id));
            		buttonManager.Add(button.id, button);
            	}

            	public void UnregisterButton(UIButton button) {
            		Debug.Log(string.Format("Unregister {0}:{1}", this, button.id));
            		buttonManager.Remove(button.id);
            	}

            	public int CountButtonsAtPosition(UIButtonPosition position) {
            		return buttonManager.Where(kv => kv.Value.position == position).ToList().Count;
            	}
            }
        }
    }
}