using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Agent;
using Global;
using Network;
using QSR;
using RCC;
using RootMotion.FinalIK;

public class JointGestureDemo : MonoBehaviour {

	CSUClient csuClient;
	EventManager eventManager;
	InteractionPrefsModalWindow interactionPrefs;

	GameObject Diana;
	GameObject leftGrasper;
	GameObject rightGrasper;

	SyntheticVision synVision;
	AvatarGestureController gestureController;
	FullBodyBipedIK ik;
	InteractionSystem interactionSystem;

	IKControl ikControl;
	IKTarget leftTarget;
	IKTarget rightTarget;
	IKTarget headTarget;

	Vector3 leftTargetDefault,leftTargetStored;
	Vector3 rightTargetDefault,rightTargetStored;
	Vector3 headTargetDefault,headTargetStored;

	public GameObject demoSurface;
	public List<GameObject> blocks;
	public GameObject indicatedObj = null;
	public GameObject indicatedObjObj = null;
	public GameObject graspedObj = null;

	public Region indicatedRegion = null;

	public Vector2 tableSize;
	public Vector2 vectorScaleFactor;
	public float vectorConeRadius;
	public Vector3 highlightCenter;
	public float highlightMoveSpeed;
	public float highlightTurnSpeed;
	public float highlightQuantum;
	public Material highlightMaterial;

	// highlight oscillation speed factor, upper limit scale, and lower limit scale
	public float highlightOscSpeed;
	public float highlightOscUpper;
	public float highlightOscLower;
	int highlightOscillateDirection = 1;

	const float DEFAULT_SCREEN_WIDTH = .9146f; // ≈ 36" = 3'
	public float knownScreenWidth = .3646f; //m
	public float windowScaleFactor;
	public bool transformToScreenPointing = false;	// false = assume table in demo space and use its coords to mirror table coords

	List<Pair<string,string>> receivedMessages = new List<Pair<string,string>>();

	Region leftRegion;
	Region rightRegion;
	Region frontRegion;
	Region backRegion;

	GameObject regionHighlight;
	GameObject radiusHighlight;

	GameObject leftRegionHighlight;
	GameObject rightRegionHighlight;
	GameObject frontRegionHighlight;
	GameObject backRegionHighlight;

	public List<string> actionOptions = new List<string> ();
	public string eventConfirmation = "";

	public List<GameObject> objectMatches = new List<GameObject> ();
	public GameObject objectConfirmation = null;

	Dictionary<string,string> confirmationTexts = new Dictionary<string, string>();

	int sessionCounter = 0;

	// Use this for initialization
	void Start () {
		windowScaleFactor = (float)Screen.width/(float)Screen.currentResolution.width;

		eventManager = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();
		eventManager.EventComplete += ReturnToRest;

		interactionPrefs = gameObject.GetComponent<InteractionPrefsModalWindow> ();

		Diana = GameObject.Find ("Diana");
		leftGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.leftHand.gameObject;
		rightGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.rightHand.gameObject;

		synVision = Diana.GetComponent<SyntheticVision> ();
		gestureController = Diana.GetComponent<AvatarGestureController> ();
		ik = Diana.GetComponent<FullBodyBipedIK> ();
		interactionSystem = Diana.GetComponent<InteractionSystem> ();

		ikControl = Diana.GetComponent<IKControl> ();
//		leftTarget = ikControl.leftHandObj.GetComponent<IKTarget> ();
//		rightTarget = ikControl.rightHandObj.GetComponent<IKTarget> ();
//		headTarget = ikControl.lookObj.GetComponent<IKTarget> ();

		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);

		// store default positions at start
		leftTargetDefault = ikControl.leftHandObj.transform.position;
		rightTargetDefault = ikControl.rightHandObj.transform.position;
		headTargetDefault = ikControl.lookObj.transform.position;

		regionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
		regionHighlight.name = "Highlight";
		regionHighlight.transform.position = Vector3.zero;
		regionHighlight.transform.localScale = new Vector3 (vectorConeRadius*.2f,vectorConeRadius*.2f,vectorConeRadius*.2f);
		regionHighlight.tag = "UnPhysic";
		regionHighlight.GetComponent<Renderer> ().material = highlightMaterial;
		regionHighlight.GetComponent<Renderer> ().enabled = false;
		Destroy (regionHighlight.GetComponent<Collider> ());
	}
	
	// Update is called once per frame
	void Update () {
		if (csuClient == null) {
			csuClient = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ().CSUClient;
			//TODO: What if there is no CSUClient address assigned?
			csuClient.GestureReceived += ReceivedFusion;
			csuClient.ConnectionLost += ConnectionLost;

			for (int i = 0; i < blocks.Count; i++) {
				blocks[i] = Helper.GetMostImmediateParentVoxeme (blocks [i]);
			}
		}

		if (demoSurface != Helper.GetMostImmediateParentVoxeme (demoSurface)) {
			demoSurface = Helper.GetMostImmediateParentVoxeme (demoSurface);
		}

		if (leftRegion == null) {
			leftRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).center.x,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z+Constants.EPSILON),
				new Vector3 (Helper.GetObjectWorldSize(demoSurface).max.x-Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z-Constants.EPSILON));
			leftRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			leftRegionHighlight.name = "LeftRegionHighlight";
			leftRegionHighlight.transform.position = leftRegion.center;
			leftRegionHighlight.transform.localScale = new Vector3 (.1f*(leftRegion.max.x - leftRegion.min.x),
				1.0f, .1f*(leftRegion.max.z - leftRegion.min.z));
			leftRegionHighlight.SetActive (false);
		}

		if (rightRegion == null) {
			rightRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z+Constants.EPSILON),
				new Vector3 (Helper.GetObjectWorldSize(demoSurface).center.x,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z-Constants.EPSILON));
			rightRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			rightRegionHighlight.name = "RightRegionHighlight";
			rightRegionHighlight.transform.position = rightRegion.center;
			rightRegionHighlight.transform.localScale = new Vector3 (.1f*(rightRegion.max.x - rightRegion.min.x),
				1.0f, .1f*(rightRegion.max.z - rightRegion.min.z));
			rightRegionHighlight.SetActive (false);
		}

		if (frontRegion == null) {
			frontRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z+Constants.EPSILON),
				new Vector3 (0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, 
					Helper.GetObjectWorldSize(demoSurface).center.z));
			frontRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			frontRegionHighlight.name = "FrontRegionHighlight";
			frontRegionHighlight.transform.position = frontRegion.center;
			frontRegionHighlight.transform.localScale = new Vector3 (.1f*(frontRegion.max.x - frontRegion.min.x),
				1.0f, .1f*(frontRegion.max.z - frontRegion.min.z));
			frontRegionHighlight.SetActive (false);
		}

		if (backRegion == null) {
			backRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).center.z),
				new Vector3 (0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z-Constants.EPSILON));
			backRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			backRegionHighlight.name = "BackRegionHighlight";
			backRegionHighlight.transform.position = backRegion.center;
			backRegionHighlight.transform.localScale = new Vector3 (.1f*(backRegion.max.x - backRegion.min.x),
				1.0f, .1f*(backRegion.max.z - backRegion.min.z));
			backRegionHighlight.SetActive (false);
		}

		// Vector pointing scaling
		if (transformToScreenPointing) {
			vectorScaleFactor.x = (float)DEFAULT_SCREEN_WIDTH/(knownScreenWidth * windowScaleFactor);

			// assume screen more or less directly under Kinect

			/*
			|```````#```````|
			|               |
			|               |
			|               |
			|  /------o=o---+--/
			| /		  o		. /
			|/	    -/-		./
			/  /`````````/  /
			|```````x```````|
			|				|
			|				|
			|				|
			|				|
			|				|
			|				|
			|,,,,,,,,,,,,,,,|
					H

				H points @ x (real space directly under Kinect on ["]table["] surface, aka "my" edge of virtual table) -> ~(0.0,0.0)
				H points @ # (far edge of virtual table) -> ~(0.0,-1.6)
			 */
		}

		if (regionHighlight.GetComponent<Renderer> ().enabled) {
			regionHighlight.transform.eulerAngles = new Vector3 (regionHighlight.transform.eulerAngles.x,
				regionHighlight.transform.eulerAngles.y + Time.deltaTime * highlightTurnSpeed, regionHighlight.transform.eulerAngles.z);

			if (highlightOscillateDirection == 1) { // grow
				regionHighlight.transform.localScale = new Vector3 (regionHighlight.transform.localScale.x + Time.deltaTime * highlightOscSpeed,
					regionHighlight.transform.localScale.y, regionHighlight.transform.localScale.z + Time.deltaTime * highlightOscSpeed);
					
				if (regionHighlight.transform.localScale.x >= (vectorConeRadius * .2f) * highlightOscUpper) {
					highlightOscillateDirection *= -1;	
				}
			}
			else if (highlightOscillateDirection == -1) { // shrink
				regionHighlight.transform.localScale = new Vector3 (regionHighlight.transform.localScale.x - Time.deltaTime * highlightOscSpeed,
					regionHighlight.transform.localScale.y, regionHighlight.transform.localScale.z - Time.deltaTime * highlightOscSpeed);

				if (regionHighlight.transform.localScale.x <= (vectorConeRadius * .2f) * highlightOscLower) {
					highlightOscillateDirection *= -1;	
				}
			}
		}

		// Synthetic vision mockup
		if (synVision != null) {
			if (synVision.enabled) {
				foreach (GameObject block in blocks) {	// limit to blocks only for now
					Voxeme blockVox = block.GetComponent<Voxeme> ();
					if (blockVox != null) {
						if (synVision.IsVisible (block)) {
							if (!synVision.visibleObjects.Contains (blockVox)) {
								synVision.visibleObjects.Add (blockVox);
								Debug.Log (string.Format ("JointGestureDemo.Start:{0}:{1}", block.name, synVision.IsVisible (block).ToString ()));
								synVision.NewInformation ();
							}
						}
					}
				}
			}
		}

		// Deixis by click
		if (Input.GetMouseButtonDown (0)) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			// Casts the ray and get the first game object hit
			Physics.Raycast (ray, out hit);

			if (hit.collider != null) {
				if (blocks.Contains (Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject))) {
					Deixis (Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject));
				}
			}
		}
	}

	void ReceivedFusion(object sender, EventArgs e) {
		string fusionMessage = ((GestureEventArgs)e).Content;
		//Debug.Log (fusionMessage);
		
		
		string[] splitMessage = ((GestureEventArgs)e).Content.Split (';');
		string messageType = splitMessage[0];
		string messageStr = splitMessage[1];
		string messageTime = splitMessage[2];

		receivedMessages.Add (new Pair<string,string> (messageTime, messageStr));

		if (messageType == "S") {	// speech message
			Debug.Log (fusionMessage);
			switch (messageStr.ToLower ()) {
			case "yes":
				Acknowledge (true);
				break;
			case "no":
				Acknowledge (false);
				break;
			case "left":
				if ((indicatedObj == null) && (graspedObj == null)) {
					if (indicatedRegion == leftRegion) {	// if ensemble with leftward point
						Deixis ("left");
					}
					else {	// if speech alone
						Deixis("right");
					}
				}
				else if (graspedObj == null) {
					if (indicatedRegion == leftRegion) {	// if ensemble with leftward push
						//Push ("left");
					}
					else {
						Push ("right");
					}
				}
				else if (indicatedObj == null) {
					if (indicatedRegion == leftRegion) {	// if ensemble with leftward carry
						//Move ("left");
					}
					else {
						Move ("right");
					}
				}
				break;
			case "right":
				if ((indicatedObj == null) && (graspedObj == null)) {
					if (indicatedRegion == rightRegion) {	// if ensemble with righttward point
						Deixis ("right");
					}
					else {	// if speech alone
						Deixis("left");
					}
				}
				else if (graspedObj == null) {
					if (indicatedRegion == rightRegion) {	// if ensemble with righttward push
						//Push ("right");
					}
					else {
						Push ("left");
					}
				}
				else if (indicatedObj == null) {
					if (indicatedRegion == rightRegion) {	// if ensemble with righttward carry
						//Move ("right");
					}
					else {
						Move ("left");
					}
				}
				break;
			case "red":
			case "green":
			case "yellow":
			case "orange":
			case "black":
			case "purple":
			case "white":
			case "pink":
				IndexByColor (messageStr);
				break;
			default:
				Debug.Log ("Cannot recognize the message: " + messageStr);
				break;
			}
		} 
		else if (messageType == "G") {	// gesture message
			Debug.Log (fusionMessage);
			string[] messageComponents = messageStr.Split ();
//			foreach (string c in messageComponents) {
//				Debug.Log (c);
//			}
			if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("start")) {	// start as trigger
				messageStr = RemoveGestureTriggers (messageStr);
				if (messageStr.StartsWith ("engage")) {
					if (GetGestureContent (messageStr, "engage") == "") {
						Engage (true);
					}
				} 
			}
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("high")) {	// high as trigger
				messageStr = RemoveGestureTriggers (messageStr);
				if (messageStr.StartsWith ("left point")) {
					Deixis (GetGestureVector (messageStr, "left point"));
				} 
				else if (messageStr.StartsWith ("right point")) {
					Deixis (GetGestureVector (messageStr, "right point"));
				} 
				else if (messageStr.StartsWith ("grab")) {
					if (GetGestureContent (messageStr, "grab") == "") {
						Grab (true);
					}
				}
				else if (messageStr.StartsWith ("posack")) {
					Acknowledge (true);
				}
				else if (messageStr.StartsWith ("negack")) {
					Acknowledge (false);
				}
			}
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("low")) {	// low as trigger
				messageStr = RemoveGestureTriggers (messageStr);
				if (messageStr.StartsWith ("left point")) {
					Suggest ("point");
				} 
				else if (messageStr.StartsWith ("right point")) {
					Suggest ("point");
				} 
				else if (messageStr.StartsWith ("grab")) {
					if (GetGestureContent (messageStr, "grab") == "") {
						Suggest ("grab");
					}
				} 
				else if (messageStr.StartsWith ("posack")) {
					Suggest ("posack");
				} 
				else if (messageStr.StartsWith ("negack")) {
					Suggest ("negack");
				}
			} 
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("stop")) {	// stop as trigger
				messageStr = RemoveGestureTriggers (messageStr);

				// stop pointing -> turn off highlight
				if (messageStr.StartsWith ("left point")) {
					regionHighlight.GetComponent<Renderer> ().enabled = false;
				} 
				else if (messageStr.StartsWith ("right point")) {
					regionHighlight.GetComponent<Renderer> ().enabled = false;
				}

				// otherwise process gesture over interval
				string startSignal = FindStartSignal (messageStr);

				if (messageStr.StartsWith ("engage")) {
					if (GetGestureContent (messageStr, "engage") == "") {
						Engage (false);
					}
				} 
				else if (messageStr.StartsWith ("push")) {
					if (startSignal.EndsWith ("high")) {
						if (GetGestureContent (messageStr, "push") == "left") {
							Push ("left");
						} 
						else if (GetGestureContent (messageStr, "push") == "right") {
							Push ("right");
						} 
						else if (GetGestureContent (messageStr, "push") == "front") {
							Push ("front");
						}
						else if (GetGestureContent (messageStr, "push") == "back") {
							Push ("back");
						}
					} 
					else if (startSignal.EndsWith ("low")) {
						if (GetGestureContent (messageStr, "push") == "left") {
							Suggest ("push left");
						} 
						else if (GetGestureContent (messageStr, "push") == "right") {
							Suggest ("push right");
						} 
						else if (GetGestureContent (messageStr, "push") == "front") {
							Suggest ("push front");
						} 
						else if (GetGestureContent (messageStr, "push") == "back") {
							Suggest ("push back");
						}
					}
				} 
				else if (messageStr.StartsWith ("grab move")) {
					if (startSignal.EndsWith ("high")) {
						if (GetGestureContent (messageStr, "grab move") == "left") {
							Move ("left");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right") {
							Move ("right");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "front") {
							Move ("front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "back") {
							Move ("back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "left front") {
							Move ("left front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right front") {
							Move ("right front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "left back") {
							Move ("left back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right back") {
							Move ("right back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "up") {
							Move ("up");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "down") {
							Move ("down");
						}
					}
					else if (startSignal.EndsWith ("low")) {
						if (GetGestureContent (messageStr, "grab move") == "left") {
							Suggest ("grab move left");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right") {
							Suggest ("grab move right");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "front") {
							Suggest ("grab move front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "back") {
							Suggest ("grab move back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "left front") {
							Suggest ("grab move left front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right front") {
							Suggest ("grab move right front");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "left back") {
							Suggest ("grab move left back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "right back") {
							Suggest ("grab move right back");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "up") {
							Suggest ("grab move up");
						} 
						else if (GetGestureContent (messageStr, "grab move") == "down") {
							Suggest ("grab move down");
						}
					}
				} 
				else if (messageStr.StartsWith ("grab")) {
					if (GetGestureContent (messageStr, "grab") == "") {
						Grab (false);
					}
				}
			}
		}
		else if (messageType == "P") {	// continuous pointing message
			if (messageStr.StartsWith ("l")) {
				TrackPointing (GetGestureVector (messageStr, "l"));
			} 
			else if (messageStr.StartsWith ("r")) {
				TrackPointing (GetGestureVector (messageStr, "r"));
			} 
		}
	}

	string FindStartSignal(string message) {
		string startSignal = "";

		foreach (Pair<string,string> m in receivedMessages.AsEnumerable().Reverse()) {
			if ((m.Item2.StartsWith (message)) && (!m.Item2.EndsWith ("stop"))) {
				startSignal = m.Item2;
				break;
			}
		}

		Debug.Log (startSignal);
		return startSignal;
	}

	string RemoveGestureTriggers(string receivedData) {
		return receivedData.Replace ("start", "").Replace ("stop", "").Replace ("high", "").Replace ("low", "").TrimStart(',');
	}

	string GetGestureContent(string receivedData, string gestureCode) {
		return receivedData.Replace (gestureCode, "").Split () [1];
	}

	List<float> GetGestureVector(string receivedData, string gestureCode) {
//		Debug.Log (receivedData);
//		Debug.Log (gestureCode);
		List<float> vector = new List<float> ();
		List<string> content = receivedData.Replace (gestureCode, "").Split (',').ToList();
		foreach (string c in content) {
			if (c.Trim() != string.Empty) {
//				Debug.Log (c);
				try {
					vector.Add (System.Convert.ToSingle (c));
				}
				catch (Exception e) {
				}
			}
		}

		return vector;
	}

	void Disambiguate(object content) {
		if (interactionPrefs.disambiguationStrategy == InteractionPrefsModalWindow.DisambiguationStrategy.Elimination) {
			if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<GameObject>))) {	// disambiguate objects
				if (((List<GameObject>)content).Equals (objectMatches)) {
					List<Voxeme> objVoxemes = new List<Voxeme> ();

					foreach (GameObject option in objectMatches) {
						if (option.GetComponent<Voxeme> () != null) {
							objVoxemes.Add (option.GetComponent<Voxeme> ());
						}

						List<object> uniqueAttrs = new List<object> ();
						for (int i = 0; i < objVoxemes.Count; i++) {
							List<object> newAttrs = Helper.DiffLists (uniqueAttrs, objVoxemes [i].voxml.Attributes.Attrs.Cast<object> ().ToList ());
							foreach (object attr in newAttrs) {
								uniqueAttrs.Add (attr);
							}
						}

						string attribute = ((Vox.VoxAttributesAttr)uniqueAttrs [0]).Value.ToString ();

						if (eventManager.events.Count == 0) {
							OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you pointing to the {0} block?", attribute));
							objectConfirmation = objVoxemes [0].gameObject;
						}
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Should I {0}?", confirmationTexts [actionOptions [0]]));
					eventConfirmation = actionOptions [0];
				}
			}
		}
		else if (interactionPrefs.disambiguationStrategy == InteractionPrefsModalWindow.DisambiguationStrategy.DeicticGestural) {
			// disambiguation strategy: deictic (gesture, use demonstratives)
			if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<GameObject>))) {	// disambiguate objects
				if (((List<GameObject>)content).Equals (objectMatches)) {
					List<Voxeme> objVoxemes = new List<Voxeme> ();

					foreach (GameObject option in objectMatches) {
						if (option.GetComponent<Voxeme> () != null) {
							objVoxemes.Add (option.GetComponent<Voxeme> ());
						}

						List<object> uniqueAttrs = new List<object> ();
						for (int i = 0; i < objVoxemes.Count; i++) {
							List<object> newAttrs = Helper.DiffLists (uniqueAttrs, objVoxemes [i].voxml.Attributes.Attrs.Cast<object> ().ToList ());
							foreach (object attr in newAttrs) {
								uniqueAttrs.Add (attr);
							}
						}

						//string attribute = ((Vox.VoxAttributesAttr)uniqueAttrs [0]).Value.ToString ();

						if (eventManager.events.Count == 0) {
							OutputHelper.PrintOutput (Role.Affector, "Which block?");
							//ReachFor (objVoxemes [0].gameObject);
							objectConfirmation = null;
						}
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				//if (actionOptions.Count 

				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Should I {0}?", confirmationTexts [actionOptions [0]]));
					eventConfirmation = actionOptions [0];
				}
			}
		}
	}

	void Suggest(string gesture) {
		AvatarGesture performGesture = null;
		if (gesture.StartsWith("grab move")) {
			string dir = GetGestureContent (gesture, "grab move");
			if (indicatedObj == null) {
				if (graspedObj == null) {
					if (dir == "left") {
						performGesture = AvatarGesture.RARM_CARRY_RIGHT;
					}
					else if (dir == "right") {
						performGesture = AvatarGesture.RARM_CARRY_LEFT;
					}
					else if (dir == "front") {
						performGesture = AvatarGesture.RARM_CARRY_FRONT;
					}
					else if (dir == "back") {
						performGesture = AvatarGesture.RARM_CARRY_BACK;
					}
					else if (dir == "up") {
						performGesture = AvatarGesture.RARM_CARRY_UP;
					}
					else if (dir == "down") {
						performGesture = AvatarGesture.RARM_CARRY_DOWN;
					}

					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move something this way?"));
						MoveToPerform ();
						gestureController.PerformGesture (performGesture);
					}
				}
				else {
					if (dir == "left") {
						performGesture = AvatarGesture.RARM_CARRY_RIGHT;
					}
					else if (dir == "right") {
						performGesture = AvatarGesture.RARM_CARRY_LEFT;
					}
					else if (dir == "front") {
						performGesture = AvatarGesture.RARM_CARRY_FRONT;
					}
					else if (dir == "back") {
						performGesture = AvatarGesture.RARM_CARRY_BACK;
					}
					else if (dir == "up") {
						performGesture = AvatarGesture.RARM_CARRY_UP;
					}
					else if (dir == "down") {
						performGesture = AvatarGesture.RARM_CARRY_DOWN;
					}

					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move this this way?"));
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (string.Format("ungrasp({0})",graspedObj.name), 1);
						MoveToPerform ();
						gestureController.PerformGesture (performGesture);
					}
				}
			}
			else {
				if (dir == "left") {
					performGesture = AvatarGesture.RARM_CARRY_RIGHT;
				}
				else if (dir == "right") {
					performGesture = AvatarGesture.RARM_CARRY_LEFT;
				}
				else if (dir == "front") {
					performGesture = AvatarGesture.RARM_CARRY_FRONT;
				}
				else if (dir == "back") {
					performGesture = AvatarGesture.RARM_CARRY_BACK;
				}
				else if (dir == "up") {
					performGesture = AvatarGesture.RARM_CARRY_UP;
				}
				else if (dir == "down") {
					performGesture = AvatarGesture.RARM_CARRY_DOWN;
				}

				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move this this way?"));
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
				}
			}
		}
		else if (gesture.StartsWith("grab")) {
			if (indicatedObj == null) {
				if (graspedObj == null) {
					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to grab something?"));
						MoveToPerform ();
						gestureController.PerformGesture (AvatarGesture.RARM_CARRY_STILL);
						actionOptions.Add("grasp({0})");
						Disambiguate (actionOptions);
					}
				}
				else {
				}
			}
			else {
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to grab this?"));
					MoveToPerform ();
					gestureController.PerformGesture (AvatarGesture.RARM_CARRY_STILL);
					actionOptions.Add(string.Format("grasp({0})",indicatedObj));
				}
			}
		}
		else if (gesture.StartsWith("push")) {
			string dir = GetGestureContent (gesture, "push");
			if ((indicatedObj == null) && (graspedObj == null)) {
				if (dir == "left") {
					performGesture = AvatarGesture.LARM_PUSH_RIGHT;
				}
				else if (dir == "right") {
					performGesture = AvatarGesture.RARM_PUSH_LEFT;
				}
				else if (dir == "front") {
					performGesture = AvatarGesture.RARM_PUSH_FRONT;
				}
				else if (dir == "back") {
					performGesture = AvatarGesture.RARM_PUSH_BACK;
				}

				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to push something this way?"));
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
				}
			}
			else {
				if (dir == "left") {
					performGesture = AvatarGesture.LARM_PUSH_RIGHT;
				}
				else if (dir == "right") {
					performGesture = AvatarGesture.RARM_PUSH_LEFT;
				}
				else if (dir == "front") {
					if (InteractionHelper.GetCloserHand (Diana, indicatedObj) == leftGrasper) {
						performGesture = AvatarGesture.LARM_PUSH_FRONT;
					}
					else if (InteractionHelper.GetCloserHand (Diana, indicatedObj) == rightGrasper) {
						performGesture = AvatarGesture.RARM_PUSH_FRONT;
					}
				}
				else if (dir == "back") {
					if (InteractionHelper.GetCloserHand (Diana, indicatedObj) == leftGrasper) {
						performGesture = AvatarGesture.LARM_PUSH_BACK;
					}
					else if (InteractionHelper.GetCloserHand (Diana, indicatedObj) == rightGrasper) {
						performGesture = AvatarGesture.RARM_PUSH_BACK;
					}
				}

				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to push something this way?"));
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
				}
			}
		}
		else if (gesture.StartsWith("left point")) { 
			string dir = GetGestureContent (gesture, "left point");
			if (dir == "left") {
				performGesture = AvatarGesture.LARM_POINT_RIGHT;
			}
			else if (dir == "right") {
				performGesture = AvatarGesture.LARM_POINT_LEFT;
			}
			else if (dir == "front") {
				performGesture = AvatarGesture.LARM_POINT_FRONT;
			}
			else if (dir == "back") {
				performGesture = AvatarGesture.LARM_POINT_BACK;
			}

			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you pointing to something over here?"));
				MoveToPerform ();
				gestureController.PerformGesture (performGesture);
			}
		}
		else if (gesture.StartsWith("right point")) { 
			string dir = GetGestureContent (gesture, "right point");
			if (dir == "left") {
				performGesture = AvatarGesture.RARM_POINT_RIGHT;
			}
			else if (dir == "right") {
				performGesture = AvatarGesture.RARM_POINT_LEFT;
			}
			else if (dir == "front") {
				performGesture = AvatarGesture.RARM_POINT_FRONT;
			}
			else if (dir == "back") {
				performGesture = AvatarGesture.RARM_POINT_BACK;
			}

			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you pointing to something over here?"));
				MoveToPerform ();
				gestureController.PerformGesture (performGesture);
			}
		}
		else if (gesture.StartsWith("posack")) {
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Was that a yes?"));
				MoveToPerform ();
				gestureController.PerformGesture (AvatarGesture.RARM_THUMBS_UP);
				gestureController.PerformGesture (AvatarGesture.HEAD_NOD);
			}
		}
		else if (gesture.StartsWith("negack")) { 
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Was that a no?"));
				MoveToPerform ();
				gestureController.PerformGesture (AvatarGesture.RARM_THUMBS_DOWN);
				gestureController.PerformGesture (AvatarGesture.HEAD_SHAKE);
			}
		}
	}
	
	void Acknowledge(bool yes) {
		LookForward ();
		if (!yes) {
			if (eventConfirmation != "") {
				if (actionOptions.Contains (eventConfirmation)) {
					actionOptions.Remove (eventConfirmation);
					confirmationTexts.Remove (eventConfirmation);
				}

				if (graspedObj == null) {
					ikControl.leftHandObj.position = leftTargetDefault;
					ikControl.rightHandObj.position = rightTargetDefault;
				}

				eventConfirmation = "";

				if (actionOptions.Count > 0) {
					Disambiguate (actionOptions);
				}
				else {
					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
					}
				}
			}
			else if (objectConfirmation != null) {
				if (objectMatches.Contains (objectConfirmation)) {
					objectMatches.Remove (objectConfirmation);
				}

				if (graspedObj == null) {
					ikControl.leftHandObj.position = leftTargetDefault;
					ikControl.rightHandObj.position = rightTargetDefault;
				}

				objectConfirmation = null;

				if (objectMatches.Count > 0) {
					Disambiguate (objectMatches);
				}
				else {
					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
					}
				}
			}
//			else {
//				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
//			}
		}
		else {
			if (eventConfirmation != "") {
				Hashtable predArgs = Helper.ParsePredicate (eventConfirmation);
				String pred = Helper.GetTopPredicate (eventConfirmation);

				if (predArgs.Count > 0) {
					Queue<String> argsStrings = new Queue<String> (((String)predArgs [pred]).Split (new char[] { ',' }));

					while (argsStrings.Count > 0) {
						object arg = argsStrings.Dequeue ();

						if (Helper.v.IsMatch ((String)arg)) {	// if arg is vector form
							Vector3 target = Helper.ParsableToVector ((String)arg);
							TurnToAccess (target);
							break;
						}
						else if (arg is String) {	// if arg is String
							if (indicatedObj != null) {
								TurnToAccess (indicatedObj.transform.position);
							}
							else if (graspedObj != null) {
								TurnToAccess (graspedObj.transform.position);
							}
							break;
						}
					}
				}

				if (eventConfirmation.StartsWith ("grasp")) {
					graspedObj = indicatedObj;
					indicatedObj = null;
					indicatedRegion = null;
				}
				else if (eventConfirmation.StartsWith ("put")) {
					graspedObj = null;
					indicatedRegion = null;
				}
				else if (eventConfirmation.StartsWith ("slide")) {
					indicatedObj = null;
					indicatedRegion = null;
				}
				else if (eventConfirmation.StartsWith ("ungrasp")) {
					graspedObj = null;
					indicatedRegion = null;
				}

				if (eventManager.events.Count == 0) {
					eventManager.InsertEvent ("", 0);
					eventManager.InsertEvent (eventConfirmation, 1);
					eventConfirmation = "";
					actionOptions.Clear ();
					objectMatches.Clear ();
					confirmationTexts.Clear ();
					OutputHelper.PrintOutput (Role.Affector, "OK.");
				}
			}
			else if (objectConfirmation != null) {
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, "OK, go on.");
					indicatedObj = objectConfirmation;
					objectConfirmation = null;
					objectMatches.Clear ();
				}
			}
//			else {
//				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
//			}
		}
	}

	void IndexByColor(string color) {
		if (indicatedObj == null) {
			if (objectMatches.Count == 0) {	// if received color without existing disambiguation options
				foreach (GameObject block in blocks) {
					if (block.activeInHierarchy &&
					    block.GetComponent<AttributeSet> ().attributes.Contains (color)) {
						if (eventManager.events.Count == 0) {
							OutputHelper.PrintOutput (Role.Affector, "OK, go on.");
							indicatedObj = block;
							objectConfirmation = null;
							objectMatches.Clear ();
							ReachFor (indicatedObj);
							break;
						}
					}
				}
			}
			else {	// choose from restricted options based on color
				foreach (GameObject match in objectMatches) {
					if (match.activeInHierarchy &&
						match.GetComponent<AttributeSet> ().attributes.Contains (color.ToLower())) {
						if (eventManager.events.Count == 0) {
							OutputHelper.PrintOutput (Role.Affector, "OK, go on.");
							indicatedObj = match;
							objectConfirmation = null;
							objectMatches.Clear ();
							ReachFor (indicatedObj);
							break;
						}
					}
				}

				if (indicatedObj == null) {
					if (eventManager.events.Count == 0) {
						OutputHelper.PrintOutput (Role.Affector, string.Format ("None of the blocks over here is {0}.", color.ToLower ()));
					}
				}
			}
		}
		else {	// received color with object already indicated
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, "Should I forget about this other block?");
				LookAt (indicatedObj.transform.position);
			}
		}
	}

	void Engage(bool state) {
		if (state == true) {
			sessionCounter++;
			if (sessionCounter > 1) {
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, "Welcome back!");
				}
			} 
			else {
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, "Hello.");
				}
			}

			objectMatches.Clear ();
			objectConfirmation = null;

			actionOptions.Clear ();
			eventConfirmation = "";

			ikControl.leftHandObj.position = leftTargetDefault;
			ikControl.rightHandObj.position = rightTargetDefault;

			LookForward ();
			TurnForward ();

			indicatedObj = null;
			graspedObj = null;
		}
		else {
			if (graspedObj != null) {
				RiggingHelper.UnRig (graspedObj, graspedObj.transform.parent.gameObject);
				graspedObj.GetComponent<Voxeme>().targetPosition =
					graspedObj.transform.position = new Vector3 (graspedObj.transform.position.x,
						Helper.GetObjectWorldSize (demoSurface).max.y+Helper.GetObjectSize(graspedObj).extents.y,
					graspedObj.transform.position.z);
				graspedObj.GetComponent<Voxeme> ().targetRotation =
					graspedObj.transform.eulerAngles = Vector3.zero;

				graspedObj.GetComponent<Voxeme>().isGrasped = false;
				graspedObj.GetComponent<Voxeme>().graspTracker = null;
				graspedObj.GetComponent<Voxeme>().grasperCoord = null;
			}

			if (eventManager.events.Count == 0) {	// TODO: what if we disengage while Diana is performing an action?
				OutputHelper.PrintOutput (Role.Affector, "Bye!");

				objectMatches.Clear ();
				objectConfirmation = null;

				actionOptions.Clear ();
				eventConfirmation = "";

				ikControl.leftHandObj.position = leftTargetDefault;
				ikControl.rightHandObj.position = rightTargetDefault;

				LookForward ();
				TurnForward ();

				indicatedObj = null;
				graspedObj = null;
			}
		}
	}

	void Deixis(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		OutputHelper.PrintOutput (Role.Affector, "");
		Region region = null;

		if (dir == "left") {
			region = leftRegion;
		}
		else if (dir == "right") {
			region = rightRegion;
		}
		else if (dir == "front") {
			region = frontRegion;
		}
		else if (dir == "down") {
			region = backRegion;
		}

		if (region != null) {
			indicatedRegion = region;
			objectMatches.Clear ();
			objectConfirmation = null;
			indicatedObj = null;

			actionOptions.Clear ();
			eventConfirmation = "";

			if (graspedObj == null) {
				ikControl.leftHandObj.position = leftTargetDefault;
				ikControl.rightHandObj.position = rightTargetDefault;
			}
			else {
				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
				return;
			}

			TurnForward ();
			LookAt (region.center);

			foreach (GameObject block in blocks) {
				bool isVisible = true;

				if (synVision != null) {
					if (synVision.enabled) {
						isVisible = synVision.IsVisible (block);
					}
				}

				if (block.activeInHierarchy) {
					if (region.Contains (block)) {
						if ((!objectMatches.Contains (block)) && (SurfaceClear(block)) && (isVisible)) {
							objectMatches.Add (block);
						}
					} 
				}
			}

			if (objectMatches.Count > 0) {
				ResolveIndicatedObject ();
			} 
			else {	// indicating region
				indicatedRegion = region;
				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you're pointing at.");
			}
		}
	}

	void Deixis(GameObject obj) {
		bool isVisible = true;

		if (synVision != null) {
			if (synVision.enabled) {
				isVisible = synVision.IsVisible (obj);
			}
		}

		if (obj.activeInHierarchy) {
			if ((!objectMatches.Contains (obj)) && (SurfaceClear(obj)) && (isVisible)) {
				objectMatches.Add (obj);
			}
		}

		if (objectMatches.Count > 0) {
			ResolveIndicatedObject ();
		}
	}

	void Deixis(List<float> vector) {
		if (eventManager.events.Count > 0) {
			return;
		}

		OutputHelper.PrintOutput (Role.Affector, "");
		Region region = null;

		Vector3 highlightCenter = TransformToSurface (vector);

		Debug.Log (string.Format("Deixis: {0}",highlightCenter));

		MoveHighlight (highlightCenter);
		regionHighlight.transform.position = highlightCenter;

		//TurnForward ();
		//LookAt (cube.transform.position);

		foreach (GameObject block in blocks) {
			bool isVisible = true;

			if (synVision != null) {
				if (synVision.enabled) {
					isVisible = synVision.IsVisible (block);
				}
			}

			if (block.activeInHierarchy) {
				Vector3 point = Helper.GetObjectWorldSize(block).ClosestPoint(highlightCenter);
				//Debug.Log (string.Format("{0}:{1} {2} {3}",block,point,highlightCenter,(point-highlightCenter).magnitude));
				if ((point-highlightCenter).magnitude <= vectorConeRadius*highlightOscUpper) {
				//if (region.Contains (new Vector3 (block.transform.position.x,
				//	region.center.y, block.transform.position.z))) {
					if ((!objectMatches.Contains (block)) && (SurfaceClear(block)) && (isVisible)) {
						objectMatches.Add (block);
					}
				} 
			}
		}


		if (objectMatches.Count > 0) {
			ReachFor (new Vector3 (highlightCenter.x, highlightCenter.y + Helper.GetObjectSize (objectMatches [0].gameObject).max.y,
				highlightCenter.z));
			ResolveIndicatedObject ();
		} 
		else {	// indicating region
			indicatedRegion = new Region(new Vector3(highlightCenter.x-vectorConeRadius,highlightCenter.y,highlightCenter.z-vectorConeRadius),
				new Vector3(highlightCenter.x+vectorConeRadius,highlightCenter.y,highlightCenter.z+vectorConeRadius));
			OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you're pointing at.");
		}
	}

	void TrackPointing(List<float> vector) {
		if (eventManager.events.Count > 0) {
			return;
		}

		OutputHelper.PrintOutput (Role.Affector, "");
		Region region = null;

		highlightCenter = TransformToSurface (vector);

		//		Debug.Log (string.Format("({0},{1};{2},{3})",vector[0],vector[1],vector[2],vector[4]));
		//Debug.Log (highlightCenter);

		// jump from origin on first update
		if (regionHighlight.transform.position.sqrMagnitude <= Constants.EPSILON) {
			MoveHighlight (highlightCenter);
			regionHighlight.transform.position = highlightCenter;
		}

		if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
			Vector3 offset = MoveHighlight (highlightCenter);

			if (offset.sqrMagnitude <= Constants.EPSILON) {
				regionHighlight.transform.position = highlightCenter;
			}
		}

		//		Vector3 origin = new Vector3 (vector [0], Helper.GetObjectWorldSize (demoSurface).max.y, vector [1]);
		//		Ray ray = new Ray(origin,
		//				new Vector3(vector[2]*vectorScaleFactor.x,Camera.main.transform.position.y,vector[4])-origin);
		//
		//		//float height = 2.0 * Mathf.Tan(0.5 * Camera.main.fieldOfView * Mathf.Deg2Rad) * Camera.main.nearClipPlane;
		//		//float width = height * Screen.width / Screen.height;
		//		//Vector3 cameraOrigin = Camera.main.ScreenToWorldPoint (0.0f, 0.0f, Camera.main.nearClipPlane);
		//		Plane cameraPlane = new Plane(Camera.main.ScreenToWorldPoint (new Vector3(0.0f, 0.0f, Camera.main.nearClipPlane)),
		//			Camera.main.ScreenToWorldPoint (new Vector3(0.0f, Screen.height, Camera.main.nearClipPlane)),
		//			Camera.main.ScreenToWorldPoint (new Vector3(Screen.width, Screen.height, Camera.main.nearClipPlane)));
		//
		//		float distance;
		//		if (cameraPlane.Raycast (ray, out distance)) {
		//			Vector3 screenPoint = Camera.main.WorldToScreenPoint (ray.GetPoint (distance));
		//			Debug.Log(string.Format("{0};{1}",ray.GetPoint (distance),screenPoint));
		//		}

		//TurnForward ();
		//LookAt (cube.transform.position);
	}

	Vector3 MoveHighlight(Vector3 highlightCenter) {
		Vector3 offset = regionHighlight.transform.position - highlightCenter;
		Vector3 normalizedOffset = Vector3.Normalize (offset);

		regionHighlight.transform.position = new Vector3 (regionHighlight.transform.position.x - normalizedOffset.x * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.y - normalizedOffset.y * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.z - normalizedOffset.z * Time.deltaTime * highlightMoveSpeed);
		
		if ((regionHighlight.transform.position.x+vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.x) ||
			(regionHighlight.transform.position.x-vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.x) ||
			(regionHighlight.transform.position.z+vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.z) ||
			(regionHighlight.transform.position.z-vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.z)) {
			// hide region highlight
			regionHighlight.GetComponent<Renderer> ().enabled = false;
		}
		else {
			regionHighlight.GetComponent<Renderer> ().enabled = true;
		}

		return offset;
	}

	void ResolveIndicatedObject() {
		if (objectMatches.Count == 1) {	// single object match
			if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
				Disambiguate (objectMatches);
			} 
			else {
				indicatedObj = objectMatches [0];
				objectMatches.Clear ();

				if (interactionPrefs.disambiguationStrategy == InteractionPrefsModalWindow.DisambiguationStrategy.DeicticGestural) {
					ReachFor (indicatedObj);
				}
			}
		} 
		else {	// multiple objects matches: disambiguate
			if ((interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) ||
				(interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation)) {
				Disambiguate (objectMatches);
			} 
			else {	// just pick one

			}
		}
	}

	void Grab(bool state) {
		if (eventManager.events.Count > 0) {
			return;
		}

		OutputHelper.PrintOutput (Role.Affector, "");
		if (state == true) {
			if (indicatedObj != null) {
				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					actionOptions.Add (string.Format ("grasp({0})", indicatedObj.name));
					confirmationTexts.Add (string.Format ("grasp({0})", indicatedObj.name), string.Format ("grasp the {0}", indicatedObj.name));
					Disambiguate (actionOptions);
				} 
				else {
					eventManager.InsertEvent ("", 0);
					eventManager.InsertEvent (string.Format ("grasp({0})", indicatedObj.name), 1);
					LookAt (indicatedObj);
					graspedObj = indicatedObj;
					indicatedObj = null;
					indicatedRegion = null;
				}
			} 
			else {
				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
			}
		} 
		else {
			if (eventConfirmation == "") {
				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					if (graspedObj != null) {
						if (graspedObj.GetComponent<Voxeme> ().isGrasped) {
							actionOptions.Add (string.Format ("ungrasp({0})", graspedObj.name));
							confirmationTexts.Add (string.Format ("ungrasp({0})", graspedObj.name), string.Format ("release the {0}", graspedObj.name));
							Disambiguate (actionOptions);
						} 
						else {
							graspedObj = null;
						}
					}
				} 
				else {
					if (graspedObj != null) {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (string.Format ("ungrasp({0})", graspedObj.name), 1);
						LookForward ();
						actionOptions.Clear ();
						confirmationTexts.Clear ();
						graspedObj = null;
						//TurnForward ();
					}
				}
			}
		}
	}

	void Move(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		OutputHelper.PrintOutput (Role.Affector, "");
		GameObject theme = null;
		if (indicatedObj != null) {
			theme = indicatedObj;
		}
		else if (graspedObj != null) {
			theme = graspedObj;
		}

		if (theme != null) {
			Bounds graspedObjBounds = Helper.GetObjectWorldSize (theme);
			if (dir == "left") {	// going this direction
				if (frontRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear(block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the left of the grasped block
									(frontRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				} 
				else if (backRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear (block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the left of the grasped block
								    (backRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				}

				string themeAttr = string.Empty;
				if (theme.GetComponent<Voxeme> () != null) {
					themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", theme.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
						string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
				}

				// not moving on top of another object
				if (frontRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front {1} part", themeAttr, dir));
				}
				else if (backRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back {1} part", themeAttr, dir));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						//TurnToAccess (target);
						indicatedObj = null;
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					//TurnToAccess (target);
					indicatedObj = null;
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			} 
			else if (dir == "right") {
				if (frontRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear(block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the right of the grasped block
									(frontRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				} 
				else if (backRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear (block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the right of the grasped block
									(backRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				}

				string themeAttr = string.Empty;
				if (theme.GetComponent<Voxeme> () != null) {
					themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", theme.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
						string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
				}

				// not moving on top of another object
				if (frontRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, frontRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front {1} part", themeAttr, dir));
				}
				else if (backRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back {1} part", themeAttr, dir));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						//TurnToAccess (target);
						indicatedObj = null;
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, frontRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					//TurnToAccess (target);
					indicatedObj = null;
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			} 
			else if (dir == "front") {
				if (leftRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear(block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the front of the grasped block
									(leftRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				} 
				else if (rightRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear (block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the front of the grasped block
									(rightRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				}

				string themeAttr = string.Empty;
				if (theme.GetComponent<Voxeme> () != null) {
					themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", theme.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
						string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
				}

				// not moving on top of another object
				if (leftRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, leftRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's {1} left part", themeAttr, dir));
				}
				else if (rightRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, rightRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's {1} right part", themeAttr, dir));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						//TurnToAccess (target);
						indicatedObj = null;
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, leftRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, rightRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					//TurnToAccess (target);
					indicatedObj = null;
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			} 
			else if (dir == "back") {
				if (leftRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear(block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the back of the grasped block
									(leftRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				} 
				else if (rightRegion.Contains(theme)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != theme) && (SurfaceClear (block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), graspedObjBounds)) &&	// if it's to the back of the grasped block
									(rightRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				}

				string themeAttr = string.Empty;
				if (theme.GetComponent<Voxeme> () != null) {
					themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", theme.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
						string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
				}

				// not moving on top of another object
				if (leftRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, leftRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's {1} left part", themeAttr, dir));
				}
				else if (rightRegion.Contains (theme)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, rightRegion }, theme).center;
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's {1} right part", themeAttr, dir));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						//TurnToAccess (target);
						indicatedObj = null;
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, leftRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (theme)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, rightRegion }, theme).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), 1);
					}
					//TurnToAccess (target);
					indicatedObj = null;
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			} 
			else if (dir == "up") {
				string objAttr = string.Empty;
				if (theme.GetComponent<Voxeme> () != null) {
					objAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				actionOptions.Add (string.Format ("lift({0})", theme.name));
				confirmationTexts.Add (string.Format ("lift({0})", theme.name), string.Format ("lift the {0} block ", objAttr));
				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					eventManager.InsertEvent (string.Format ("lift({0})", theme.name), 1);
					objectMatches.Clear ();
				}
			} 
			else if (dir == "down") {
				if (eventConfirmation == "") {
					string objAttr = string.Empty;
					if (theme.GetComponent<Voxeme> () != null) {
						objAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					
					Vector3 target = new Vector3 (theme.transform.position.x,
						                Helper.GetObjectWorldSize (demoSurface).max.y,
										theme.transform.position.z);
					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)), string.Format ("put the {0} block down", objAttr));
					if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
						Disambiguate (actionOptions);
					} 
					else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
						if (actionOptions.Count > 1) {
							Disambiguate (actionOptions);
						} 
						else {
							eventManager.InsertEvent ("", 0);
							eventManager.InsertEvent (actionOptions [0], 1);
							indicatedObj = null;
							graspedObj = null;
							actionOptions.Clear ();
							objectMatches.Clear ();
						}
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (new Vector3 (theme.transform.position.x,
								Helper.GetObjectWorldSize (demoSurface).max.y,
								theme.transform.position.z))), 1);
						indicatedObj = null;
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
			}
		}
	}

	void Push(string dir) {
		OutputHelper.PrintOutput (Role.Affector, "");
		if (indicatedObj != null) {
			Bounds indicatedObjBounds = Helper.GetObjectWorldSize (indicatedObj);
			if (dir == "left") {
				if (frontRegion.Contains(indicatedObj)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != indicatedObj) && (SurfaceClear(block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), indicatedObjBounds)) &&	// if it's to the left of the grasped block
									(frontRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				} 
				else if (backRegion.Contains(indicatedObj)) {	// if the grasped block is in this region
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							if ((block != indicatedObj) && (SurfaceClear (block))) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), indicatedObjBounds)) &&	// if it's to the left of the grasped block
									(backRegion.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							}
						}
					}
				}

				string indicatedAttr = string.Empty;
				if (indicatedObj.GetComponent<Voxeme> () != null) {
					indicatedAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("slide({0},(left{1}))", indicatedObj.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name),
						string.Format ("push the {0} block to the left of the {1} block", indicatedAttr, objAttr));
				}

				// not moving on top of another object
				if (frontRegion.Contains (indicatedObj)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the table's front {1} part", indicatedAttr, dir));
				}
				else if (backRegion.Contains (indicatedObj)) {	// stay in this region
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the table's back {1} part", indicatedAttr, dir));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				} 
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					} 
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						//TurnToAccess (target);
						indicatedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				} 
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (indicatedObj)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("push({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (graspedObj)) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("push({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					//TurnToAccess (target);
					indicatedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
//				indicatedRegion = leftRegion;
//				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//					frontRegion.center.y, indicatedObj.transform.position.z))) {
//					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
//						if (block.activeInHierarchy) {
//							bool sideClear = true;
//							foreach (GameObject otherBlock in blocks) {
//								if ((QSR.QSR.Left(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
//									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
//									sideClear = false;
//								}
//							}
//
//							if ((block != indicatedObj) && (sideClear)) {
//								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
//								    (frontRegion.Contains (new Vector3 (block.transform.position.x,
//									    frontRegion.center.y, block.transform.position.z)))) {
//									if (!objectMatches.Contains (block)) {
//										objectMatches.Add (block);
//									}
//								}
//							} 
//						}
//					}
//				}
//				else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//					backRegion.center.y, indicatedObj.transform.position.z))) {
//					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
//						if (block.activeInHierarchy) {
//							bool sideClear = true;
//							foreach (GameObject otherBlock in blocks) {
//								if ((QSR.QSR.Left(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
//									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
//									sideClear = false;
//								}
//							}
//
//							if ((block != indicatedObj) && (sideClear)) {
//								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
//								    (backRegion.Contains (new Vector3 (block.transform.position.x,
//									    backRegion.center.y, block.transform.position.z)))) {
//									if (!objectMatches.Contains (block)) {
//										objectMatches.Add (block);
//									}
//								}
//							} 
//						}
//					}
//				}
//
//				string indAttr = string.Empty;
//				if (indicatedObj.GetComponent<Voxeme> () != null) {
//					indAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
//				}
//
//				Vector3 target = Vector3.zero;
//
//				foreach (GameObject obj in objectMatches) {
//					string objAttr = string.Empty;
//					if (obj.GetComponent<Voxeme> () != null) {
//						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
//					}
//					target = obj.transform.position;
//					actionOptions.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name));
//					confirmationTexts.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name),
//						string.Format ("push the {0} block to the right of the {1} block", indAttr, objAttr));
//				}
//
//				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//					frontRegion.center.y, indicatedObj.transform.position.z))) {
//					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, indicatedObj).center;
//					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
//						Helper.VectorToParsable (target)));
//					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
//						string.Format ("push the {0} block to the front left of the table", indAttr));
//				}
//				else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//					backRegion.center.y, indicatedObj.transform.position.z))) {
//					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
//					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
//						Helper.VectorToParsable (target)));
//					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
//						string.Format ("push the {0} block to the back left of the table", indAttr));
//				}
//
//				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
//					Disambiguate (actionOptions);
//				}
//				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
//					if (actionOptions.Count > 1) {
//						Disambiguate (actionOptions);
//					}
//					else {
//						eventManager.InsertEvent ("", 0);
//						eventManager.InsertEvent (actionOptions [0], 1);
//						TurnToAccess (target);
//						indicatedObj = null;
//						actionOptions.Clear ();
//						objectMatches.Clear ();
//					}
//				}
//				else {
//					eventManager.InsertEvent ("", 0);
//					if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//						frontRegion.center.y, indicatedObj.transform.position.z))) {
//						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
//						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
//							Helper.VectorToParsable (target)), 1);
//					}
//					else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
//						backRegion.center.y, indicatedObj.transform.position.z))) {
//						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
//						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
//							Helper.VectorToParsable (target)), 1);
//					}
//					TurnToAccess (target);
//					indicatedObj = null;
//					actionOptions.Clear ();
//					objectMatches.Clear ();
//				}
			}
			else if (dir == "right") {
				indicatedRegion = rightRegion;
				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					frontRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Right(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (frontRegion.Contains (new Vector3 (block.transform.position.x,
									    frontRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}
				else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					backRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Right(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (backRegion.Contains (new Vector3 (block.transform.position.x,
									    backRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}

				string indAttr = string.Empty;
				if (indicatedObj.GetComponent<Voxeme> () != null) {
					indAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					target = obj.transform.position;
					actionOptions.Add (string.Format ("slide({0},right({1}))", indicatedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("slide({0},right({1}))", indicatedObj.name, obj.name),
						string.Format ("push the {0} block to the left of the {1} block", indAttr, objAttr));
				}

				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					frontRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, frontRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the front right of the table", indAttr));
				}
				else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					backRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the back right of the table", indAttr));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				}
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					}
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						TurnToAccess (target);
						indicatedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						frontRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						backRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					indicatedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "front") {
				if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					leftRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.InFront(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (leftRegion.Contains (new Vector3 (block.transform.position.x,
									    leftRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}
				else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					rightRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.InFront(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (rightRegion.Contains (new Vector3 (block.transform.position.x,
									    rightRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}

				string indAttr = string.Empty;
				if (indicatedObj.GetComponent<Voxeme> () != null) {
					indAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					target = obj.transform.position;
					actionOptions.Add (string.Format ("slide({0},behind({1}))", indicatedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("slide({0},behind({1}))", indicatedObj.name, obj.name),
						string.Format ("push the {0} block in front of the {1} block", indAttr, objAttr));
				}

				if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					leftRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the front left of the table", indAttr));
				}
				else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					rightRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, frontRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the front right of the table", indAttr));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				}
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					}
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						TurnToAccess (target);
						indicatedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						leftRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, rightRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						rightRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, rightRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					indicatedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "back") {
				if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					leftRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Behind(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (leftRegion.Contains (new Vector3 (block.transform.position.x,
									    leftRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}
				else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					rightRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Behind(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
								    (rightRegion.Contains (new Vector3 (block.transform.position.x,
									    rightRegion.center.y, block.transform.position.z)))) {
									if (!objectMatches.Contains (block)) {
										objectMatches.Add (block);
									}
								}
							} 
						}
					}
				}

				string indAttr = string.Empty;
				if (indicatedObj.GetComponent<Voxeme> () != null) {
					indAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					target = obj.transform.position;
					actionOptions.Add (string.Format ("slide({0},in_front({1}))", indicatedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("slide({0},in_front({1}))", indicatedObj.name, obj.name),
						string.Format ("push the {0} block behind the {1} block", indAttr, objAttr));
				}

				if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					leftRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, leftRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the back left of the table", indAttr));
				}
				else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					rightRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, rightRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the back right of the table", indAttr));
				}

				if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
					Disambiguate (actionOptions);
				}
				else if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Disambiguation) {
					if (actionOptions.Count > 1) {
						Disambiguate (actionOptions);
					}
					else {
						eventManager.InsertEvent ("", 0);
						eventManager.InsertEvent (actionOptions [0], 1);
						TurnToAccess (target);
						indicatedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						leftRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, leftRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						rightRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, rightRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					indicatedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
		}
	}

	void MoveToPerform() {
//		Diana.GetComponent<IKControl> ().leftHandObj.position = leftTargetDefault;
//		Diana.GetComponent<IKControl> ().rightHandObj.position = rightTargetDefault;
//		Diana.GetComponent<IKControl> ().lookObj.position = headTargetDefault;
		//Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).target.position = leftTargetDefault;
		Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).positionWeight = 0.0f;
		Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).rotationWeight = 0.0f;
		//Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).target.position = rightTargetDefault;
		Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).positionWeight = 0.0f;
		Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).rotationWeight = 0.0f;
		LookForward ();
	}

	void LookForward() {
		Diana.GetComponent<LookAtIK> ().solver.target.position = headTargetDefault;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.8f;

	}

	void LookAt(GameObject obj) {
		Vector3 target = new Vector3 (obj.transform.position.x/2.0f,
			(obj.transform.position.y+headTargetDefault.y)/2.0f, obj.transform.position.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = obj.transform.position;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
	}

	void LookAt(Vector3 point) {
		Vector3 target = new Vector3 (point.x/2.0f, (point.y+headTargetDefault.y)/2.0f, point.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = target;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
	}

	void TurnToward(GameObject obj) {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
	}

	void TurnToward(Vector3 point) {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
	}

	void TurnForward() {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
	}

	void TurnToAccess(Vector3 point) {
		Vector3 leftGrasperCoord = Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).position;
		Vector3 rightGrasperCoord = Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).position;
		//Diana.GetComponent<Animator>().GetBoneTransform (HumanBodyBones.RightHand).transform.gameObject;
		Vector3 grasperCoord = Vector3.zero;

		// which hand is closer?
		float leftToGoalDist = (leftGrasperCoord - point).magnitude;
		float rightToGoalDist = (rightGrasperCoord - point).magnitude;

		if (leftToGoalDist < rightToGoalDist) {
			grasperCoord = leftGrasperCoord;
		}
		else {
			grasperCoord = rightGrasperCoord;
		}

		Vector3 offset = point - new Vector3 (grasperCoord.x, point.y, grasperCoord.z);
		offset = Quaternion.Euler (0.0f,
			-45.0f*(point.x-Diana.transform.position.x)/Mathf.Abs(point.x-Diana.transform.position.x),
			0.0f) * offset;
		Diana.GetComponent<IKControl>().targetRotation = Quaternion.LookRotation (offset,Vector3.up).eulerAngles;
	}

	void ReachFor(Vector3 coord) {
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
		//Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		//Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
	
		// which region is obj in?
		if (leftRegion.Contains(new Vector3(coord.x,
			leftRegion.center.y,coord.z))) {
			ikControl.rightHandObj.transform.position = coord+offset;
			InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);

		}
		else if (rightRegion.Contains(new Vector3(coord.x,
			leftRegion.center.y,coord.z))) {
			ikControl.leftHandObj.transform.position = coord+offset;
			InteractionHelper.SetRightHandTarget (Diana, ikControl.leftHandObj);
		}

		LookForward ();
	}

	void ReachFor(GameObject obj) {
		Bounds bounds = Helper.GetObjectWorldSize(obj);
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
//		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
//		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;

		// which region is obj in?
		if (leftRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y,obj.transform.position.z))) {
			ikControl.rightHandObj.transform.position = obj.transform.position+offset;
			InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);

		}
		else if (rightRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y,obj.transform.position.z))) {
			ikControl.leftHandObj.transform.position = obj.transform.position+offset;
			InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		}

		LookAt (obj);
	}

	Vector3 TransformToSurface(List<float> vector) {
		float zCoord = vector[1];

		if (transformToScreenPointing) {
			// point at base of Kinect -> 0.0 -> .8 (my edge)
			// point at far edge of virtual table -> -1.6 -> -.8 (Diana's edge)
			zCoord = (vector[1] * vectorScaleFactor.y) + (tableSize.y / 2.0f);
		}
		else {
			// point at base of Kinect -> 0.0 -> -.8 (Diana's edge)
			// point down in front of me -> 1.6 -> .8 (my edge)
			zCoord = (vector[1] - (tableSize.y / 2.0f)) * vectorScaleFactor.y;
		}

		Vector3 coord = new Vector3 (-vector[0]*vectorScaleFactor.x,
			Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
			zCoord);

		return coord;
	}

	bool SurfaceClear(GameObject block) {
		Debug.Log (block);
		bool surfaceClear = true;
		List<GameObject> excludeChildren = block.GetComponentsInChildren<Renderer>().Where(
			o => Helper.GetMostImmediateParentVoxeme(o.gameObject) != block).Select(o => o.gameObject).ToList();
		foreach (GameObject go in excludeChildren) {
			Debug.Log (go);
		}
		Bounds blockBounds = Helper.GetObjectWorldSize (block, excludeChildren);
		Debug.Log (blockBounds);
		foreach (GameObject otherBlock in blocks) {
			Bounds otherBounds = Helper.GetObjectWorldSize (otherBlock);
			Debug.Log (otherBlock);
			Debug.Log (otherBounds);
			if ((QSR.QSR.Above (otherBounds, blockBounds)) && (!QSR.QSR.Left (otherBounds, blockBounds)) &&
				(!QSR.QSR.Right (otherBounds, blockBounds)) && (RCC8.EC (otherBounds, blockBounds))) {
				surfaceClear = false;
				break;
			}
		}

		Debug.Log (surfaceClear);
		return surfaceClear;
	}

	public void StorePose() {
		Debug.Log (string.Format("Storing pose {0} {1} {2}",
			ikControl.leftHandObj.transform.position,ikControl.rightHandObj.transform.position,ikControl.lookObj.transform.position));
		leftTargetStored = ikControl.leftHandObj.transform.position;
		rightTargetStored = ikControl.rightHandObj.transform.position;
		headTargetStored = ikControl.lookObj.transform.position;
	}

	public void ReturnToPose() {
		ikControl.leftHandObj.transform.position = leftTargetStored;
		ikControl.rightHandObj.transform.position = rightTargetStored;
		ikControl.lookObj.transform.position = headTargetStored;
		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
		InteractionHelper.SetHeadTarget (Diana, ikControl.lookObj);
		Debug.Log (string.Format("Returning to pose {0} {1} {2}",
			ikControl.leftHandObj.transform.position,ikControl.rightHandObj.transform.position,ikControl.lookObj.transform.position));
	}

	void ReturnToRest(object sender, EventArgs e) {
		if (!interactionSystem.IsPaused (FullBodyBipedEffector.LeftHand) &&
		    !interactionSystem.IsPaused (FullBodyBipedEffector.RightHand)) {
			TurnForward ();
		}
	}

	void ConnectionLost(object sender, EventArgs e) {
		if (sessionCounter >= 1) {
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, "Hey, where'd you go?");
			}
		}
		else {
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, "Anyone there?");
			}
		}
	}
}
