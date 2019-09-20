using UnityEngine;

namespace VoxSimPlatform {
    namespace UI {
        namespace ModalWindow {
            public class ModalWindow : FontManager {
            	public Rect windowRect;
            	public Vector2 scrollPosition;
            	public bool isResizing = false;
            	public Rect resizeStart = new Rect();
            	public Vector2 minWindowSize;
            	public string windowTitle;
            	public bool persistent;
            	public int id;

            	public bool render = false;

            	public virtual bool Render {
            		get { return render; }
            		set { render = value; }
            	}

            	public bool autoOpen = false;

            	public virtual bool AutoOpen {
            		get { return autoOpen; }
            		set { autoOpen = value; }
            	}

            	public bool allowResize = true;

            	public virtual bool AllowResize {
            		get { return allowResize; }
            		set { allowResize = value; }
            	}

            	public bool allowDrag = true;

            	public virtual bool AllowDrag {
            		get { return allowDrag; }
            		set { allowDrag = value; }
            	}

            	public bool allowForceClose = true;

            	public virtual bool AllowForceClose {
            		get { return allowForceClose; }
            		set { allowForceClose = value; }
            	}

            	protected ModalWindowManager windowManager;

            	// Use this for initialization
            	protected virtual void Start() {
            		windowManager = GameObject.Find("VoxWorld").GetComponent<ModalWindowManager>();

            		id = windowManager.windowManager.Count;

            		if (!windowManager.windowManager.ContainsKey(id)) {
            			windowManager.RegisterWindow(this);
            		}
            		else {
            			Debug.Log("ModalWindow of id " + id + " already exists on this object!");
            			Destroy(this);
            		}

            		if (autoOpen) {
            			Render = true;
            		}
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	protected virtual void OnGUI() {
            		if (Render) {
            			//GUILayout automatically lays out the GUI window to contain all the text
            			windowRect = GUILayout.Window(id, windowRect, DoModalWindow, windowTitle);
            			//prevents GUI window from dragging off window screen
            			windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            			windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
            			//Resizing GUI window
            			if (allowResize) {
            				windowRect = ResizeWindow(windowRect, ref isResizing, ref resizeStart, minWindowSize);
            			}
            		}
            	}

            	public static Rect ResizeWindow(Rect windowRect, ref bool isResizing, ref Rect resizeStart, Vector2 minWindowSize) {
            		Vector2 mouse =
            			GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            		if (Event.current.type == EventType.MouseDown && windowRect.Contains(mouse)) {
            			isResizing = true;
            			resizeStart = new Rect(mouse.x, mouse.y, windowRect.width, windowRect.height);
            		}
            		else if (Event.current.type == EventType.MouseUp && isResizing) {
            			isResizing = false;
            		}
            		else if (!Input.GetMouseButton(0)) {
            			isResizing = false;
            		}
            		else if (isResizing) {
            			windowRect.width = Mathf.Max(minWindowSize.x, resizeStart.width + (mouse.x - resizeStart.x));
            			windowRect.height = Mathf.Max(minWindowSize.y, resizeStart.height + (mouse.y - resizeStart.y));
            			windowRect.xMax = Mathf.Min(Screen.width, windowRect.xMax);
            			windowRect.yMax = Mathf.Min(Screen.height, windowRect.yMax);
            		}

            		return windowRect;
            	}

            	public virtual void DoModalWindow(int windowID) {
            		//Debug.Log (windowID);
            		if ((allowForceClose) && (GUI.Button(new Rect(windowRect.width - 25, 2, 23, 16), "X"))) {
            			if (persistent) {
            				Render = false;
            			}
            			else {
            				DestroyWindow();
            			}
            		}

            		//makes GUI window draggable
            		if (allowDrag) {
            			GUI.DragWindow(new Rect(0, 0, 10000, 20));
            		}
            	}

            	public virtual void DestroyWindow() {
            		windowManager.UnregisterWindow(this);
            		Destroy(this);
            	}
            }
        }
    }
}