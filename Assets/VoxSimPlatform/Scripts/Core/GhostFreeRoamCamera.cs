using UnityEngine;
using System.Linq;

using VoxSimPlatform.Agent;
using VoxSimPlatform.Global;
using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.UI.UIButtons;

namespace VoxSimPlatform {
    namespace Core {
        [RequireComponent(typeof(Camera))]
        public class GhostFreeRoamCamera : MonoBehaviour {
        	public float initialSpeed = 10f;
        	public float increaseSpeed = 1.25f;

        	public bool allowMovement = true;
        	public bool allowRotation = true;

        	public KeyCode upButton = KeyCode.W;
        	public KeyCode downButton = KeyCode.S;
        	public KeyCode rightButton = KeyCode.D;
        	public KeyCode leftButton = KeyCode.A;
        	public KeyCode homeButton = KeyCode.H;

        	public float cursorSensitivity = 0.025f;
        	public bool cursorToggleAllowed = true;
        	public KeyCode cursorToggleButton = KeyCode.Escape;

        	private Vector3 cameraPosOrigin;
        	private Quaternion cameraRotOrigin;

        	public float panSpeed = 0.1f;
        	private Vector3 mouseOrigin; // Position of cursor when mouse dragging starts

        	float zoomAmount = 0;
        	float maxToClamp = 10f;
        	float zoomSpeed = 0.5f;

        	private float currentSpeed = 0f;
        	private bool moving = false;
        	private bool togglePressed = false;

        	private Rigidbody rb;
        	private Vector3 deltaPosition;

        	private float angle = 0;

        	HelpModalWindow help;
        	InputController inputController;
        	OutputController outputController;
        	ModalWindowManager windowManager;
        	UIButtonManager buttonManager;

        	private void OnEnable() {
        		help = GameObject.Find("Help").GetComponent<HelpModalWindow>();
        		inputController = GameObject.Find("IOController").GetComponent<InputController>();
        		outputController = GameObject.Find("IOController").GetComponent<OutputController>();
        		windowManager = GameObject.Find("VoxWorld").GetComponent<ModalWindowManager>();
        		buttonManager = GameObject.Find("VoxWorld").GetComponent<UIButtonManager>();

        		if (cursorToggleAllowed) {
        			Screen.lockCursor = false;
        			Cursor.visible = true;
        		}
        	}

        	private void FixedUpdate() {
        		if (inputController != null) {
        			if (!Helper.PointOutsideMaskedAreas(
        				new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        				new Rect[] {inputController.inputRect})) {
        				return;
        			}
        		}

        		if (outputController != null) {
        			if (!Helper.PointOutsideMaskedAreas(
        				new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        				new Rect[] {outputController.outputRect})) {
        				return;
        			}
        		}

        		if (help != null) {
        			if (!Helper.PointOutsideMaskedAreas(
        				    new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        				    new Rect[] {help.windowRect}) && (help.render)) {
        				return;
        			}
        		}

        		bool masked = false; // assume mouse not masked by some open modal window or some button
        		for (int i = 0; i < windowManager.windowManager.Count; i++) {
        			if (windowManager.windowManager.ContainsKey(i)) {
        				if (windowManager.windowManager[i] != null) {
        					if (!Helper.PointOutsideMaskedAreas(
        						    new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        						    new Rect[] {windowManager.windowManager[i].windowRect}) &&
        					    (windowManager.windowManager[i].Render)) {
        						masked = true;
        						break;
        					}
        				}
        			}
        		}

        		for (int i = 0; i < buttonManager.buttonManager.Count; i++) {
        			if (buttonManager.buttonManager.ContainsKey(i)) {
        				if (buttonManager.buttonManager[i] != null) {
        					if (!Helper.PointOutsideMaskedAreas(
        						new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        						new Rect[] {buttonManager.buttonManager[i].buttonRect})) {
        						masked = true;
        						break;
        					}
        				}
        			}
        		}

        		if (masked) {
        			return;
        		}
        		else {
        			// if a modal window is resizing
        			//  then mouse may be outside of masked area but we still don't want to move the camera
        			if (windowManager.windowManager.Values.Where(w => w.isResizing).ToList().Count > 0) {
        				return;
        			}
        		}

        		if (allowMovement) {
        			if (Input.GetKey(homeButton)) {
        				transform.position = cameraPosOrigin;
        				transform.rotation = cameraRotOrigin;
        			}

        			bool lastMoving = moving;
        			deltaPosition = Vector3.zero;

        			if (moving)
        				currentSpeed += increaseSpeed * Time.deltaTime;

        			moving = false;

        			CheckMove(upButton, transform.up);
        			CheckMove(downButton, -transform.up);
        			CheckMove(rightButton, transform.right);
        			CheckMove(leftButton, -transform.right);

        			//adding in zooming
        			if (Input.mousePosition.x >= 0 && Input.mousePosition.x <= Screen.width &&
        			    Input.mousePosition.y >= 0 && Input.mousePosition.y <= Screen.height) {
        //				zoomAmount += Input.GetAxis ("Mouse ScrollWheel");
        //				zoomAmount = Mathf.Clamp (zoomAmount, -maxToClamp, maxToClamp);
        //				var translate = Mathf.Min (Mathf.Abs (Input.GetAxis ("Mouse ScrollWheel")), maxToClamp - Mathf.Abs (zoomAmount));
        				gameObject.transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * zoomSpeed);
        			}

        			//adding in panning
        			if (Input.GetMouseButtonDown(2)) {
        				// Get mouse origin
        				mouseOrigin = Input.mousePosition;
        			}

        			if (Input.GetMouseButton(2)) {
        				Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - mouseOrigin);
        				Vector3 move = new Vector3(Mathf.Sign(pos.x) * panSpeed, Mathf.Sign(pos.y) * panSpeed, 0);
        				transform.Translate(move);
        			}

        			if (moving) {
        				if (moving != lastMoving)
        					currentSpeed = initialSpeed;

        				transform.position += deltaPosition * currentSpeed * Time.deltaTime;
        			}
        			else currentSpeed = 0f;
        		}

        		if (allowRotation) {
        #if !UNITY_IOS
        			if (Input.GetMouseButton(0)) {
        				Vector3 eulerAngles = transform.eulerAngles;
        				eulerAngles.x += -Input.GetAxis("Mouse Y") * 359f * cursorSensitivity;
        				eulerAngles.y += Input.GetAxis("Mouse X") * 359f * cursorSensitivity;
        				transform.eulerAngles = eulerAngles;
        			}
        #else
        			if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) {
        				Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;
        				Vector3 eulerAngles = transform.eulerAngles;
        				eulerAngles.x += touchDeltaPosition.y * cursorSensitivity;
        				eulerAngles.y += -touchDeltaPosition.x * cursorSensitivity;
        				transform.eulerAngles = eulerAngles;
        			}
        #endif
        		}

        		if (cursorToggleAllowed) {
        			if (Input.GetKey(cursorToggleButton)) {
        				if (!togglePressed) {
        					togglePressed = true;
        					Screen.lockCursor = !Screen.lockCursor;
        					Cursor.visible = !Cursor.visible;
        				}
        			}
        			else togglePressed = false;
        		}
        		else {
        			togglePressed = false;
        			Cursor.visible = true;
        		}
        	}

        	private void Start() {
        		cameraPosOrigin = transform.position;
        		cameraRotOrigin = transform.rotation;
        		rb = GetComponent<Rigidbody>();

        		// ignore collisions with everything but the boundaries
        		int camLayer = LayerMask.NameToLayer("Camera");
        		for (int layer = 0; layer < 32; layer++) {
        			if (LayerMask.LayerToName(layer) != "Boundaries") {
        				Physics.IgnoreLayerCollision(camLayer, layer);
        			}
        		}
        	}

        	private void CheckMove(KeyCode keyCode, Vector3 directionVector) {
        		if (Input.GetKey(keyCode)) {
        			moving = true;
        			deltaPosition += directionVector;
        		}
        	}

        //	void OnCollisionEnter(Collision other) {
        //		if (other.gameObject.tag != "CameraBoundary") {
        //			Physics.IgnoreCollision (GetComponent<Collider> (), other.gameObject.GetComponent<Collider> ());
        //		}
        //	}
        //	
        //	void OnCollisionStay(Collision other) {
        //		if (other.gameObject.tag != "CameraBoundary") {
        //			Physics.IgnoreCollision(GetComponent<Collider>(), other.gameObject.GetComponent<Collider>());
        //		}
        //
        //	}
        }
    } 
}