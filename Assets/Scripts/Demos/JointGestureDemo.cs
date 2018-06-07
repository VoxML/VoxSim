using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

using Agent;
using Episteme;
using Global;
using Network;
using QSR;
using RCC;
using RootMotion.FinalIK;

public class SelectionEventArgs : EventArgs {
	public object Content;

	public SelectionEventArgs(object content) {
		Content = content;
	}
}

public class JointGestureDemo : AgentInteraction {

	CSUClient csuClient;
	EventManager eventManager;
	ObjectSelector objSelector;
	PluginImport commBridge;

	GameObject Diana;
	GameObject leftGrasper;
	GameObject rightGrasper;

	FullBodyBipedIK ik;
	InteractionSystem interactionSystem;

	DianaInteractionLogic interactionLogic;
	EpistemicModel epistemicModel;
	enum CertaintyMode {
		Suggest,
		Act
	};

	IKControl ikControl;
	IKTarget leftTarget;
	IKTarget rightTarget;
	IKTarget headTarget;

	Vector3 leftTargetDefault,leftTargetStored;
	Vector3 rightTargetDefault,rightTargetStored;
	Vector3 headTargetDefault,headTargetStored;

	public InteractionPrefsModalWindow interactionPrefs;
	public AvatarGestureController gestureController;
	public VisualMemory dianaMemory;

	public GameObject demoSurface;
	public BoxCollider demoSurfaceCollider;
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
	public Material activeHighlightMaterial;
	public Material inactiveHighlightMaterial;

	// highlight oscillation speed factor, upper limit scale, and lower limit scale
	public float highlightOscSpeed;
	public float highlightOscUpper;
	public float highlightOscLower;
	int highlightOscillateDirection = 1;

	Timer highlightTimeoutTimer;
	public double highlightTimeoutTime;
	bool disableHighlight = false;

	const float DEFAULT_SCREEN_WIDTH = .9146f; // ≈ 36" = 3'
	const float DEFAULT_SCREEN_HEIGHT = .53f;
	public Vector2 knownScreenSize = new Vector2(.9146f,.53f); //m
	public Vector2 windowScaleFactor;
	public float kinectToSurfaceHeight = .63f; //m
	public bool transformToScreenPointing = false;	// false = assume table in demo space and use its coords to mirror table coords
	public Vector2 receivedPointingCoord = Vector2.zero;
	public Vector2 receivedPointingVariance = Vector2.zero;
	public Vector2 screenPoint = Vector2.zero;
	public Vector3 varianceScaleFactor = Vector2.zero;

	public bool allowDeixisByClick = false;

	GenericLogger logger;
	int logIndex;

	List<Pair<string,string>> receivedMessages = new List<Pair<string,string>>();

	Region leftRegion;
	Region rightRegion;
	Region frontRegion;
	Region backRegion;

	Dictionary<Region,string> regionLabels = new Dictionary<Region, string> ();
	Dictionary<string,string> directionPreds = new Dictionary<string, string> ();
	Dictionary<string,string> directionLabels = new Dictionary<string, string> ();
	Dictionary<string,string> oppositeDir = new Dictionary<string, string> ();
	Dictionary<string,string> relativeDir = new Dictionary<string, string> ();

	GameObject regionHighlight;
	GameObject radiusHighlight;

	GameObject leftRegionHighlight;
	GameObject rightRegionHighlight;
	GameObject frontRegionHighlight;
	GameObject backRegionHighlight;

	public List<string> actionOptions = new List<string> ();
	public string eventConfirmation = "";

	public List<string> suggestedActions = new List<string> ();

	public List<GameObject> objectMatches = new List<GameObject> ();
	public GameObject objectConfirmation = null;

	public bool useOrderingHeuristics;

	Dictionary<string,string> confirmationTexts = new Dictionary<string, string>();

	int sessionCounter = 0;

	public event EventHandler ObjectSelected;

	public void OnObjectSelected(object sender, EventArgs e)
	{
		if (ObjectSelected != null)
		{
			ObjectSelected(this, e);
		}
	}

	public event EventHandler PointSelected;

	public void OnPointSelected(object sender, EventArgs e)
	{
		if (PointSelected != null)
		{
			PointSelected(this, e);
		}
	}

	// Use this for initialization
	void Start () {
		windowScaleFactor.x = (float)Screen.width/(float)Screen.currentResolution.width;
		windowScaleFactor.y = (float)Screen.height/(float)Screen.currentResolution.height;

		objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
		commBridge = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ();

		eventManager = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();
		eventManager.EventComplete += ReturnToRest;

		interactionPrefs = gameObject.GetComponent<InteractionPrefsModalWindow> ();

		logger = GetComponent<GenericLogger> ();

		if (PlayerPrefs.GetInt ("Make Logs") == 1) {
			logger.OpenLog (PlayerPrefs.GetString ("Logs Prefix"));
		}

		logIndex = 0;

		Diana = GameObject.Find ("Diana");
		dianaMemory = GameObject.Find("DianaMemory").GetComponent<VisualMemory>();
		UseTeaching = interactionPrefs.useTeachingAgent;
		epistemicModel = Diana.GetComponent<EpistemicModel> ();
		interactionLogic = Diana.GetComponent<DianaInteractionLogic> ();

		leftGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.leftHand.gameObject;
		rightGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.rightHand.gameObject;
		gestureController = Diana.GetComponent<AvatarGestureController> ();
		ik = Diana.GetComponent<FullBodyBipedIK> ();
		interactionSystem = Diana.GetComponent<InteractionSystem> ();
		ikControl = Diana.GetComponent<IKControl> ();

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
		regionHighlight.GetComponent<Renderer> ().material = activeHighlightMaterial;
		regionHighlight.gameObject.layer = 5;
		//regionHighlight.GetComponent<Renderer> ().material.SetColor("_Color",new Color(1.0f,1.0f,1.0f,0.5f));
//		regionHighlight.GetComponent<Renderer> ().enabled = false;
		Destroy (regionHighlight.GetComponent<Collider> ());

		highlightTimeoutTimer = new Timer (highlightTimeoutTime);
		highlightTimeoutTimer.Enabled = false;
		highlightTimeoutTimer.Elapsed += DisableHighlight;

		relativeDir.Add ("left", "left");
		relativeDir.Add ("right", "right");
		relativeDir.Add ("front", "back");
		relativeDir.Add ("back", "front");
		relativeDir.Add ("up", "up");
		relativeDir.Add ("down", "down");

		oppositeDir.Add ("left", "right");
		oppositeDir.Add ("right", "left");
		oppositeDir.Add ("front", "back");
		oppositeDir.Add ("back", "front");
		oppositeDir.Add ("up", "down");
		oppositeDir.Add ("down", "up");

		directionPreds.Add ("left", "left");
		directionPreds.Add ("right", "right");
		directionPreds.Add ("front", "in_front");
		directionPreds.Add ("back", "behind");

		directionLabels.Add ("left", "left of");
		directionLabels.Add ("right", "right of");
		directionLabels.Add ("front", "in front of");
		directionLabels.Add ("back", "behind");
	}

	// Update is called once per frame
	void Update () {
		if (csuClient == null) {
			csuClient = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ().CSUClient;
			//TODO: What if there is no CSUClient address assigned?
			if (csuClient != null) {
				csuClient.GestureReceived += ReceivedFusion;
				csuClient.ConnectionLost += ConnectionLost;
			}

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
			Debug.Log (string.Format ("{0}: {1},{2},{3}", leftRegion, leftRegion.center, leftRegion.min, leftRegion.max));
			leftRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			leftRegionHighlight.name = "LeftRegionHighlight";
			leftRegionHighlight.transform.position = leftRegion.center;
			leftRegionHighlight.transform.localScale = new Vector3 (.1f*(leftRegion.max.x - leftRegion.min.x),
				1.0f, .1f*(leftRegion.max.z - leftRegion.min.z));
			leftRegionHighlight.SetActive (false);

			regionLabels.Add (leftRegion, "left");
		}

		if (rightRegion == null) {
			rightRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).min.z+Constants.EPSILON),
				new Vector3 (Helper.GetObjectWorldSize(demoSurface).center.x,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z-Constants.EPSILON));
			Debug.Log (string.Format ("{0}: {1},{2},{3}", rightRegion, rightRegion.center, rightRegion.min, rightRegion.max));
			rightRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			rightRegionHighlight.name = "RightRegionHighlight";
			rightRegionHighlight.transform.position = rightRegion.center;
			rightRegionHighlight.transform.localScale = new Vector3 (.1f*(rightRegion.max.x - rightRegion.min.x),
				1.0f, .1f*(rightRegion.max.z - rightRegion.min.z));
			rightRegionHighlight.SetActive (false);

			regionLabels.Add (rightRegion, "right");
		}

		if (frontRegion == null) {
			frontRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).min.z+Constants.EPSILON),
				new Vector3 (Helper.GetObjectWorldSize(demoSurface).max.x+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, 
					Helper.GetObjectWorldSize(demoSurface).center.z));
			frontRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			frontRegionHighlight.name = "FrontRegionHighlight";
			frontRegionHighlight.transform.position = frontRegion.center;
			frontRegionHighlight.transform.localScale = new Vector3 (.1f*(frontRegion.max.x - frontRegion.min.x),
				1.0f, .1f*(frontRegion.max.z - frontRegion.min.z));
			frontRegionHighlight.SetActive (false);

			regionLabels.Add (frontRegion, "front");
		}

		if (backRegion == null) {
			backRegion = new Region (new Vector3 (Helper.GetObjectWorldSize(demoSurface).min.x+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
				Helper.GetObjectWorldSize(demoSurface).center.z),
				new Vector3 (Helper.GetObjectWorldSize(demoSurface).max.x+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z-Constants.EPSILON));
			backRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			backRegionHighlight.name = "BackRegionHighlight";
			backRegionHighlight.transform.position = backRegion.center;
			backRegionHighlight.transform.localScale = new Vector3 (.1f*(backRegion.max.x - backRegion.min.x),
				1.0f, .1f*(backRegion.max.z - backRegion.min.z));
			backRegionHighlight.SetActive (false);

			regionLabels.Add (backRegion, "back");
		}

		UseTeaching = interactionPrefs.useTeachingAgent;
		transformToScreenPointing = (interactionPrefs.deixisMethod == InteractionPrefsModalWindow.DeixisMethod.Screen);

		// Vector pointing scaling
		if (transformToScreenPointing) {
			vectorScaleFactor.x = (float)DEFAULT_SCREEN_WIDTH / (knownScreenSize.x * windowScaleFactor.x);
			vectorScaleFactor.y = (float)DEFAULT_SCREEN_HEIGHT / (knownScreenSize.y * windowScaleFactor.y);

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
		else {
			vectorScaleFactor.x = tableSize.x/(float)DEFAULT_SCREEN_WIDTH;
		}

		if (disableHighlight) {
			regionHighlight.transform.position = Vector3.zero;
			regionHighlight.GetComponent<Renderer> ().material.color = new Color(1.0f,1.0f,1.0f,
				(1.0f/((regionHighlight.transform.position-
					new Vector3(demoSurface.transform.position.x,Helper.GetObjectWorldSize(demoSurface).max.y,demoSurface.transform.position.z)).magnitude+Constants.EPSILON))*
				regionHighlight.transform.position.y);
//			Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.GetColor ("_Color").a);
			disableHighlight = false;
		}

//		Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.GetColor ("_Color").a);

		if (regionHighlight.GetComponent<Renderer> ().material.color.a > 0.0f) {
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
			
		if ((UseTeaching) && (interactionLogic.useEpistemicModel)) {
			//Concept putL = epistemicModel.state.GetConcept ("PUT", ConceptType.ACTION, ConceptMode.L);
			Concept putG = epistemicModel.state.GetConcept ("move", ConceptType.ACTION, ConceptMode.G);
			//Concept pushL = epistemicModel.state.GetConcept ("PUSH", ConceptType.ACTION, ConceptMode.L);
			Concept pushG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
			//var putRelation = epistemicModel.state.GetRelation (putL, putG);
			//var pushRelation = epistemicModel.state.GetRelation (pushL, pushG);

//			if ((putL.Certainty > -1.0) && (pushL.Certainty > -1.0) && 
//				(pushRelation.Certainty > -1.0) && (putRelation.Certainty > -1.0)) { 
//				putL.Certainty = -1.0;
//				pushL.Certainty = -1.0;
//				putRelation.Certainty = -1.0;
//				pushRelation.Certainty = -1.0;
//
//				epistemicModel.state.UpdateEpisim (new []{putL, pushL}, new []{pushRelation, putRelation});
//			}

//			foreach (GameObject block in blocks) {	// limit to blocks only for now
//				Voxeme blockVox = block.GetComponent<Voxeme> ();
//				if (blockVox != null) {
//					if (dianaMemory.IsKnown(blockVox)) {
//						string color = string.Empty;
//						color = blockVox.voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
//
//						Concept blockConcept = epistemicModel.state.GetConcept (block.name, ConceptType.OBJECT, ConceptMode.G);
//
//						if (blockConcept.Certainty < 1.0) {
//							blockConcept.Certainty = 1.0;
//							epistemicModel.state.UpdateEpisim (new Concept[] { blockConcept }, new Relation[] { });
//						}
//					}
//				}
//			}
		}


		// Deixis by click
		if (allowDeixisByClick) {
			if (Input.GetMouseButtonDown (0)) {
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit;
				// Casts the ray and get the first game object hit
				Physics.Raycast (ray, out hit);

				if (hit.collider != null) {
					if (blocks.Contains (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject))) {
						if (!epistemicModel.engaged) {
							epistemicModel.engaged = true;
						}
						if (dianaMemory != null && dianaMemory.enabled) {
							Debug.Log (string.Format ("Does Agent know {0}:{1}", hit.collider.gameObject, dianaMemory.IsKnown(hit.collider.gameObject.GetComponent<Voxeme>())));
							if (dianaMemory.IsKnown (Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject).GetComponent<Voxeme>())) {
								//Deixis (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject));
								OnObjectSelected (this, new SelectionEventArgs (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject)));
							}
						}
					}
					else if (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject) == demoSurface) {
						if (!epistemicModel.engaged) {
							epistemicModel.engaged = true;
						}
						OnPointSelected (this, new SelectionEventArgs (hit.point));
					}
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

		logger.OnLogEvent (this, new LoggerArgs (string.Format("{0}\t{1}\t{2}",
			(++logIndex).ToString(),
			string.Format("{0}{1}","H",messageType),
			messageStr)));

		receivedMessages.Add (new Pair<string,string> (messageTime, messageStr));

		OnCharacterLogicInput (this, new CharacterLogicEventArgs (string.Format ("{0} {1}", messageType, messageStr.Split (',') [0]),
			string.Format ("{0} {1}", messageType, messageStr)));

		if (!epistemicModel.engaged) {
			epistemicModel.engaged = true;
		}

		Concept conceptL = null;
		Concept conceptG = null;
		Relation relation = null;

		if (messageType == "S") {	// speech message
			Debug.Log (fusionMessage);
			switch (messageStr.ToLower ()) {
			case "yes":
				break;
			case "no":
				break;
			case "grab":
				break;
			case "left":
				break;
			case "right":
				break;
			case "this":
			case "that":
				break;
			case "red":
			case "green":
			case "yellow":
			case "orange":
			case "black":
			case "purple":
			case "white":
			case "pink":
				break;
			case "big":
				break;
			case "small":
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
				messageStr = interactionLogic.RemoveGestureTrigger (messageStr, interactionLogic.GetGestureTrigger(messageStr));
			}
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("high")) {	// high as trigger
				messageStr = interactionLogic.RemoveGestureTrigger (messageStr, interactionLogic.GetGestureTrigger(messageStr));
				if (messageStr.StartsWith ("grab")) {
				}
				else if (messageStr.StartsWith ("posack")) {
				}
				else if (messageStr.StartsWith ("negack")) {
				}
			}
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("low")) {	// low as trigger
			} 
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("stop")) {	// stop as trigger
				messageStr = interactionLogic.RemoveGestureTrigger (messageStr, interactionLogic.GetGestureTrigger(messageStr));
				string startSignal = FindStartSignal (messageStr);

				if (messageStr.StartsWith ("engage")) {
				} 
				else if (messageStr.StartsWith ("push")) {
				} 
				else if (messageStr.StartsWith ("grab")) {
				}
			}
		}
		else if (messageType == "P") {	// continuous pointing message
			if ((interactionLogic.CurrentState.Name != "Wait") && (interactionLogic.CurrentState.Name != "TrackPointing")) {
				regionHighlight.GetComponent<Renderer> ().material = inactiveHighlightMaterial;
			}
			highlightTimeoutTimer.Interval = highlightTimeoutTime;
			highlightTimeoutTimer.Enabled = true;

			if (messageStr.StartsWith ("l")) {
				if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
					Vector3 offset = MoveHighlight (TransformToSurface (GetGestureVector (messageStr, "l")),
						GetVectorVariance (GetGestureVector (messageStr, "l")));

					if (offset.sqrMagnitude <= Constants.EPSILON) {
						regionHighlight.transform.position = TransformToSurface (GetGestureVector (messageStr, "l"));
					}
				}
			}
			else if (messageStr.StartsWith ("r")) {
				if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
					Vector3 offset = MoveHighlight (TransformToSurface (GetGestureVector (messageStr, "r")),
						GetVectorVariance (GetGestureVector (messageStr, "r")));

					if (offset.sqrMagnitude <= Constants.EPSILON) {
						regionHighlight.transform.position = TransformToSurface (GetGestureVector (messageStr, "r"));
					}
				}
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

	string FindPreviousMatch(string message) {
		string prevMessage = "";
		List<Pair<string,string>> previousMessages = receivedMessages.AsEnumerable ().Reverse ().ToList ();
		previousMessages.RemoveAt (0);

		foreach (Pair<string,string> m in previousMessages) {
			if (m.Item2.Contains (message)) {
				prevMessage = m.Item2;
				break;
			}
		}

		Debug.Log (prevMessage);
		return prevMessage;
	}

	string GetSpeechString(string receivedData, string constituentTag) {
		//		Debug.Log (receivedData);
		//		Debug.Log (gestureCode);
		List<string> content = receivedData.Replace (constituentTag, "").Split (',').ToList();

		return content[content.Count-1];
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

	float GetVectorVariance(List<float> vector) {
		if (vector.Count == 4) {
			return Mathf.Max(vector[vector.Count - 1],vector[vector.Count - 2]);
		}
		else {
			if (transformToScreenPointing) {
				receivedPointingVariance = new Vector2 (RandomHelper.RandomFloat (0.0f, 0.2f), RandomHelper.RandomFloat (0.0f, 0.2f));
			}
			else {
				receivedPointingVariance = new Vector2 (RandomHelper.RandomFloat(0.0f,0.06f),RandomHelper.RandomFloat(0.0f,0.06f));
			}
			return Mathf.Max(receivedPointingVariance.x,receivedPointingVariance.y);
		}
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
						RespondAndUpdate(string.Format ("The {0} block?", attribute));
						objectConfirmation = objVoxemes [0].gameObject;
						LookAt(objectConfirmation);

						// not using languages for property for now (colors, sizes, ...  are in G row)
//						Concept attrConcept = epistemicModel.state.GetConcept (attribute.ToUpper(), ConceptType.PROPERTY, ConceptMode.L);
//						attrConcept.Certainty = (attrConcept.Certainty < 0.5) ? 0.5 : attrConcept.Certainty;
//						epistemicModel.state.UpdateEpisim (new Concept[]{ attrConcept }, new Relation[]{ });
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate(string.Format ("Should I {0}?", confirmationTexts [actionOptions [0]]));
					eventConfirmation = actionOptions [0];
				}
			}
		}
		else if (interactionPrefs.disambiguationStrategy == InteractionPrefsModalWindow.DisambiguationStrategy.DeicticGestural) {
			// disambiguation strategy: deictic (gesture, use demonstratives)
			if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<GameObject>))) {	// disambiguate objects
				if (((List<GameObject>)content).Equals (objectMatches)) {
					bool duplicateNominal = false;
					List<Voxeme> objVoxemes = new List<Voxeme> ();

					foreach (GameObject option in objectMatches) {
						if (option.GetComponent<Voxeme> () != null) {
							objVoxemes.Add (option.GetComponent<Voxeme> ());
						}
					}

					List<object> uniqueAttrs = new List<object> ();
					for (int i = 0; i < objVoxemes.Count; i++) {
						List<object> newAttrs = Helper.DiffLists (
							uniqueAttrs.Select (x => ((Vox.VoxAttributesAttr)x).Value).Cast<object> ().ToList (),
							objVoxemes [i].voxml.Attributes.Attrs.Cast<object> ().ToList ().Select (x => ((Vox.VoxAttributesAttr)x).Value).Cast<object> ().ToList ());

						if (newAttrs.Count > 0) {
							foreach (object attr in newAttrs) {
								Debug.Log (string.Format ("{0}:{1}", objVoxemes [i].name, attr.ToString ()));
								Vox.VoxAttributesAttr attrToAdd = new Vox.VoxAttributesAttr ();
								attrToAdd.Value = attr.ToString ();

								if (uniqueAttrs.Where (x => ((Vox.VoxAttributesAttr)x).Value == attrToAdd.Value).ToList ().Count == 0) {
									uniqueAttrs.Add (attrToAdd);
								}
							}
						}
						else {
							duplicateNominal = true;
						}
					}

					string attribute = ((Vox.VoxAttributesAttr)uniqueAttrs [0]).Value.ToString ();

					if (eventManager.events.Count == 0) {
						if (!duplicateNominal) {
							RespondAndUpdate(string.Format ("The {0} block?", attribute));
							ReachFor (objVoxemes [0].gameObject);
							objectConfirmation = objVoxemes [0].gameObject;
							LookAt (objectConfirmation);

						// not using languages for property for now (colors, sizes, ...  are in G row)
//							Concept attrConcept = epistemicModel.state.GetConcept (attribute.ToUpper(), ConceptType.PROPERTY, ConceptMode.L);
//							attrConcept.Certainty = (attrConcept.Certainty < 0.5) ? 0.5 : attrConcept.Certainty;
//							epistemicModel.state.UpdateEpisim (new Concept[]{ attrConcept }, new Relation[]{ });
						}
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				if (eventManager.events.Count == 0) {
					LookForward();

					if (confirmationTexts.ContainsKey (actionOptions [0])) {
						RespondAndUpdate(string.Format ("Should I {0}?", confirmationTexts [actionOptions [0]]));
						eventConfirmation = actionOptions [0];
					}
					else {
						actionOptions.RemoveAt (0);
						Disambiguate (actionOptions);
					}
				}
			}
		}
	}

	void Suggest(string gesture) {
		if ((eventConfirmation != "") && (gesture != "posack") && (gesture != "negack")) {
			return;
		}

		AvatarGesture performGesture = null;
		if (gesture.StartsWith("grab move")) {
			string dir = interactionLogic.GetGestureContent (gesture, "grab move");
			if (indicatedObj == null) {	// not indicating anything
				if (graspedObj == null) {	// not grasping anything
					if (dir == "left") {
						performGesture = AvatarGesture.RARM_CARRY_RIGHT;
					}
					else if (dir == "right") {
						performGesture = AvatarGesture.RARM_CARRY_LEFT;
					}
					else if (dir == "front") {
						performGesture = AvatarGesture.RARM_CARRY_BACK;
					}
					else if (dir == "back") {
						performGesture = AvatarGesture.RARM_CARRY_FRONT;
					}
					else if (dir == "up") {
						performGesture = AvatarGesture.RARM_CARRY_UP;
					}
					else if (dir == "down") {
						performGesture = AvatarGesture.RARM_CARRY_DOWN;
					}

					if (eventManager.events.Count == 0) {
						LookForward();
						RespondAndUpdate("Do you want me to move something this way?");
						MoveToPerform ();
						gestureController.PerformGesture (performGesture);
					}
				}
				else {	// grasping something
					if (dir == "left") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_RIGHT;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_RIGHT;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_RIGHT;
							}
						}
					}
					else if (dir == "right") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_LEFT;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_LEFT;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_LEFT;
							}
						}
					}
					else if (dir == "front") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_BACK;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_BACK;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_BACK;
							}
						}
					}
					else if (dir == "back") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_FRONT;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_FRONT;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_FRONT;
							}
						}
					}
					else if (dir == "up") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_UP;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_UP;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_UP;
							}
						}
					}
					else if (dir == "down") {
						if (graspedObj == null) {
							performGesture = AvatarGesture.RARM_CARRY_DOWN;
						}
						else {
							if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_CARRY_DOWN;
							}
							else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_CARRY_DOWN;
							}
						}
					}

					if (eventManager.events.Count == 0) {
						LookForward();
						RespondAndUpdate("Do you want me to move this this way?");
						MoveToPerform ();
						gestureController.PerformGesture (performGesture);
						PopulateMoveOptions (graspedObj, dir, CertaintyMode.Suggest);
					}
				}
			}
			else {	// indicating something
				if (dir == "left") {
					performGesture = AvatarGesture.RARM_CARRY_RIGHT;
				}
				else if (dir == "right") {
					performGesture = AvatarGesture.RARM_CARRY_LEFT;
				}
				else if (dir == "front") {
					performGesture = AvatarGesture.RARM_CARRY_BACK;
				}
				else if (dir == "back") {
					performGesture = AvatarGesture.RARM_CARRY_FRONT;
				}
				else if (dir == "up") {
					performGesture = AvatarGesture.RARM_CARRY_UP;
				}
				else if (dir == "down") {
					performGesture = AvatarGesture.RARM_CARRY_DOWN;
				}

				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("Do you want me to move this this way?");
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
					PopulateMoveOptions (indicatedObj, dir, CertaintyMode.Suggest);
				}
			}
		}
		else if (gesture.StartsWith("grab")) {
			if (indicatedObj == null) {	// not indicating anything
				if (graspedObj == null) {	// not grasping anything
					if (eventManager.events.Count == 0) {
						LookForward();
						RespondAndUpdate("Do you want me to grab something?");
						MoveToPerform ();
						gestureController.PerformGesture (AvatarGesture.RARM_CARRY_STILL);
						suggestedActions.Add("grasp({0})");
					}
				}	// already grasping something
				else {
					// ignore this
				}
			}
			else {	// indicating something
				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("Are you asking me to grab this?");
					MoveToPerform ();
					gestureController.PerformGesture (AvatarGesture.RARM_CARRY_STILL);
					PopulateGrabOptions (indicatedObj, CertaintyMode.Suggest);
				}
			}
		}
		else if (gesture.StartsWith("push")) {
			string dir = interactionLogic.GetGestureContent (gesture, "push");
			if ((indicatedObj == null) && (graspedObj == null)) {	// not indicating or grasping anything
				if (dir == "left") {
					performGesture = AvatarGesture.LARM_PUSH_RIGHT;
				}
				else if (dir == "right") {
					performGesture = AvatarGesture.RARM_PUSH_LEFT;
				}
				else if (dir == "front") {
					performGesture = AvatarGesture.RARM_PUSH_BACK;
				}
				else if (dir == "back") {
					performGesture = AvatarGesture.RARM_PUSH_FRONT;
				}

				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("Are you asking me to push something this way?");
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
					suggestedActions.Add("slide({0}"+string.Format(",{0})",dir));
				}
			}
			else {	// something indicated or grasped
				GameObject theme = null;
				if (indicatedObj != null) {
					theme = indicatedObj;
				}
				else if (graspedObj != null) {
					theme = graspedObj;
				}

				if (dir == "left") {
					if (graspedObj == null) {
						performGesture = AvatarGesture.LARM_PUSH_RIGHT;
					}
					else {
						if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
							performGesture = AvatarGesture.RARM_PUSH_RIGHT;
						}
						else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
							performGesture = AvatarGesture.LARM_PUSH_RIGHT;
						}
					}
				}
				else if (dir == "right") {
					if (graspedObj == null) {
						performGesture = AvatarGesture.RARM_PUSH_LEFT;
					}
					else {
						if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
							performGesture = AvatarGesture.RARM_PUSH_LEFT;
						}
						else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
							performGesture = AvatarGesture.LARM_PUSH_LEFT;
						}
					}
				}
				else if (dir == "front") {
					if (theme != null) {
						if (InteractionHelper.GetCloserHand (Diana, theme) == leftGrasper) {
							performGesture = AvatarGesture.LARM_PUSH_BACK;
						}
						else if (InteractionHelper.GetCloserHand (Diana, theme) == rightGrasper) {
							performGesture = AvatarGesture.RARM_PUSH_BACK;
						}
					}
					else if (InteractionHelper.GetCloserHand (Diana, theme) == rightGrasper) {
						performGesture = AvatarGesture.RARM_PUSH_BACK;
					}
				}
				else if (dir == "back") {
					if (theme != null) {
						if (InteractionHelper.GetCloserHand (Diana, theme) == leftGrasper) {
							performGesture = AvatarGesture.LARM_PUSH_FRONT;
						}
						else if (InteractionHelper.GetCloserHand (Diana, theme) == rightGrasper) {
							performGesture = AvatarGesture.RARM_PUSH_FRONT;
						}
					}
					else if (InteractionHelper.GetCloserHand (Diana, theme) == rightGrasper) {
						performGesture = AvatarGesture.RARM_PUSH_FRONT;
					}
				}

				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("Are you asking me to push this this way?");
					MoveToPerform ();
					gestureController.PerformGesture (performGesture);
					PopulatePushOptions (theme, dir, CertaintyMode.Suggest);
				}
			}
		}
		else if (gesture.StartsWith("left point")) { 
			//string dir = GetGestureContent (gesture, "left point");
			//			if (dir == "left") {
			//				performGesture = AvatarGesture.LARM_POINT_RIGHT;
			//			}
			//			else if (dir == "right") {
			//				performGesture = AvatarGesture.LARM_POINT_LEFT;
			//			}
			//			else if (dir == "front") {
			//				performGesture = AvatarGesture.LARM_POINT_FRONT;
			//			}
			//			else if (dir == "back") {
			//				performGesture = AvatarGesture.LARM_POINT_BACK;
			//			}

			performGesture = AvatarGesture.LARM_POINT_FRONT;

			if (eventManager.events.Count == 0) {
				RespondAndUpdate("It looks like you're pointing to something.");
				MoveToPerform ();
				gestureController.PerformGesture (performGesture);
			}
		}
		else if (gesture.StartsWith("right point")) { 
			//			string dir = GetGestureContent (gesture, "right point");
			//			if (dir == "left") {
			//				performGesture = AvatarGesture.RARM_POINT_RIGHT;
			//			}
			//			else if (dir == "right") {
			//				performGesture = AvatarGesture.RARM_POINT_LEFT;
			//			}
			//			else if (dir == "front") {
			//				performGesture = AvatarGesture.RARM_POINT_FRONT;
			//			}
			//			else if (dir == "back") {
			//				performGesture = AvatarGesture.RARM_POINT_BACK;
			//			}

			performGesture = AvatarGesture.RARM_POINT_FRONT;

			if (eventManager.events.Count == 0) {
				RespondAndUpdate("It looks like you're pointing to something.");
				MoveToPerform ();
				gestureController.PerformGesture (performGesture);
			}
		}
		else if (gesture.StartsWith("posack")) {
			if (eventManager.events.Count == 0) {
				RespondAndUpdate("Yes?");
				MoveToPerform ();
				AllowHeadMotion ();

				if (graspedObj == null) {
					gestureController.PerformGesture(AvatarGesture.RARM_THUMBS_UP);
				}
				else {
					if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
						gestureController.PerformGesture(AvatarGesture.RARM_THUMBS_UP);
					}
					else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
						gestureController.PerformGesture(AvatarGesture.LARM_THUMBS_UP);
					}
				}
				gestureController.PerformGesture (AvatarGesture.HEAD_NOD);
			}
		}
		else if (gesture.StartsWith("negack")) { 
			if (eventManager.events.Count == 0) {
				RespondAndUpdate("No?");
				MoveToPerform ();
				AllowHeadMotion ();

				if (graspedObj == null) {
					gestureController.PerformGesture(AvatarGesture.RARM_THUMBS_DOWN);
				}
				else {
					if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
						gestureController.PerformGesture(AvatarGesture.RARM_THUMBS_DOWN);
					}
					else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
						gestureController.PerformGesture(AvatarGesture.LARM_THUMBS_DOWN);
					}
				}
				gestureController.PerformGesture (AvatarGesture.HEAD_SHAKE);
				eventConfirmation = "negack";
			}
		}
	}

	void Acknowledge(bool yes) {
		LookForward ();
		if (!yes) {
			if (eventConfirmation == "forget") {	// forget about previously indicated block? no
				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("OK.");
					eventConfirmation = "";
					if (indicatedObj != null) {
						ReachFor (indicatedObj);
					}
				}
			} 
			else if (eventConfirmation == "negack") {	// no? no 
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("OK.");
					indicatedObj = null;
					eventConfirmation = "";

					if ((graspedObj == null) && (eventConfirmation == "")) {
						TurnForward ();
					}
					//objectMatches.Clear ();
					suggestedActions.Clear ();

					if (actionOptions.Count > 0) {
						confirmationTexts.Remove (actionOptions [0]);
						actionOptions.RemoveAt (0);
						Disambiguate (actionOptions);
					}
					else if (objectConfirmation != null) {
						if (objectMatches.Contains (objectConfirmation)) {
							objectMatches.Remove (objectConfirmation);
						}

						objectConfirmation = null;

						if (objectMatches.Count > 0) {
							Disambiguate (objectMatches);
						} 
					}

					//						if (eventManager.events.Count == 0) {
					//							indicatedObj = null;
					//							objectMatches.Clear ();
					//							OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
					//						}
					//					}
				}
			}
			else if ((eventConfirmation != "") || (indicatedObj != null)) {
				if (actionOptions.Contains (eventConfirmation)) {
					actionOptions.Remove (eventConfirmation);
					confirmationTexts.Remove (eventConfirmation);
				}

				if (graspedObj == null) {
					ikControl.leftHandObj.position = leftTargetDefault;
					ikControl.rightHandObj.position = rightTargetDefault;
					InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
					InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
				}

				eventConfirmation = "";

				if (actionOptions.Count > 0) {
					Disambiguate (actionOptions);
				} 
				else {
					if (eventManager.events.Count == 0) {
						indicatedObj = null;
						objectMatches.Clear ();
						LookForward();
						RespondAndUpdate("OK.");
						//OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
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
					InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
					InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
				}

				objectConfirmation = null;

				if (objectMatches.Count > 0) {
					Disambiguate (objectMatches);
				} 
				else {
					if (eventManager.events.Count == 0) {
						indicatedObj = null;
						indicatedRegion = null;
						RespondAndUpdate("Sorry, I don't know what you mean.");
					}
				}
			}
			else if (suggestedActions.Count > 0) {
				suggestedActions.Remove (suggestedActions [0]);

				if (suggestedActions.Count == 0) {
					LookForward();
					RespondAndUpdate("OK.");
				}

				//				eventConfirmation = "";
				//				suggestedActions.Clear ();
				//				actionOptions.Clear ();
				//				objectMatches.Clear ();
				//				confirmationTexts.Clear ();
				//				TurnForward ();
			}
			else if (indicatedObj != null) {
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("OK.");
					eventConfirmation = "";
					indicatedObj = null;
					TurnForward ();
					LookForward ();
				}
			}
		} 
		else {
			if (eventConfirmation == "forget") {	// forget about previously indicated block? yes
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("OK.");
					eventConfirmation = "";
					indicatedObj = null;
					TurnForward ();
					LookForward ();
				}
			}
			else if (eventConfirmation == "negack") {	// no? yes 
				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("OK.");
					indicatedObj = null;
					eventConfirmation = "";
					//objectMatches.Clear ();
					suggestedActions.Clear ();

					confirmationTexts.Remove (actionOptions [0]);
					actionOptions.RemoveAt (0);

					if (actionOptions.Count > 0) {
						Disambiguate (actionOptions);
					} 
					else if (objectConfirmation != null) {
						if (objectMatches.Contains (objectConfirmation)) {
							objectMatches.Remove (objectConfirmation);
						}

						objectConfirmation = null;

						if (objectMatches.Count > 0) {
							Disambiguate (objectMatches);
						} 
					}
					else {
						if (eventManager.events.Count == 0) {
							indicatedObj = null;
							objectMatches.Clear ();
							RespondAndUpdate("Sorry, I don't know what you mean.");
						}
					}
				}
			}
			else if (eventConfirmation != "") {
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
					graspedObj = null;
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
					suggestedActions.Clear ();
					actionOptions.Clear ();
					objectMatches.Clear ();
					confirmationTexts.Clear ();
					LookForward();
					RespondAndUpdate("OK.");
				}
			} 
			else if (objectConfirmation != null) {
				if (eventManager.events.Count == 0) {
					LookForward();
					RespondAndUpdate("OK, go on.");
					indicatedObj = objectConfirmation;
					ReachFor (indicatedObj);
					objectConfirmation = null;
					objectMatches.Clear ();
				}
			}
			else if (suggestedActions.Count > 0) {
				if (suggestedActions [0].Contains ("{0}")) {
					if (eventManager.events.Count == 0) {
						if ((graspedObj == null) && (indicatedObj == null) && (objectConfirmation == null)) {
							LookForward();
							RespondAndUpdate(string.Format ("What do you want me to {0}?", suggestedActions [0].Split ('(') [0]));
						}
					}
				} 
				else {
					//eventConfirmation = suggestedActions [0];
					actionOptions = new List<string>(suggestedActions);
					suggestedActions.Clear ();
					Disambiguate (actionOptions);
				}
			} 
			//			else {
			//				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
			//			}
		}
	}

	void IndexByColor(string color) {
		if (eventManager.events.Count == 0) {
			if ((indicatedObj == null) && (graspedObj == null)) {
				if (objectMatches.Count == 0) {	// if received color without existing disambiguation options
					foreach (GameObject block in blocks) {
						bool isKnown = true;

						if (dianaMemory != null && dianaMemory.enabled) {
							isKnown = dianaMemory.IsKnown (block.GetComponent<Voxeme>());
						}

						if ((block.activeInHierarchy) &&
							(block.GetComponent<AttributeSet> ().attributes.Contains (color.ToLower ())) && 
							(isKnown) && (SurfaceClear(block))) {
							if (!objectMatches.Contains (block)) {
								objectMatches.Add (block);
							}
						}
					}
					ResolveIndicatedObject ();
				}
				else {	// choose from restricted options based on color
					List<GameObject> toRemove = new List<GameObject>();
					foreach (GameObject match in objectMatches) {
						bool isKnown = true;

						if (dianaMemory != null && dianaMemory.enabled) {
							isKnown = dianaMemory.IsKnown (match.GetComponent<Voxeme>());
						}

						if ((match.activeInHierarchy) &&
							(!match.GetComponent<AttributeSet> ().attributes.Contains (color.ToLower ())) &&
							(isKnown)) {
							if (eventManager.events.Count == 0) {
								if (objectMatches.Contains (match)) {
									toRemove.Add (match);
								}
							}
						}
					}

					foreach (GameObject item in toRemove) {
						objectMatches.Remove (item);
					}
					ResolveIndicatedObject ();

				}

				if (indicatedObj == null) {
					if ((eventManager.events.Count == 0) && (objectMatches.Count > 0)) {
						LookForward();
						RespondAndUpdate(string.Format ("Which {0} block?", color.ToLower ()));
					}
					else if ((eventManager.events.Count == 0) && (objectConfirmation == null)) {
						LookForward();
						RespondAndUpdate(string.Format ("None of the blocks over here is {0}.", color.ToLower ()));
					}
				}
				else {
					if ((eventManager.events.Count == 0) && (eventConfirmation == "")) {
						LookForward();
						RespondAndUpdate("OK, go on.");
					}
				}
			}
			else {	// received color with object already indicated
				if (eventManager.events.Count == 0) {
					string attr = string.Empty;
					if (indicatedObj.GetComponent<Voxeme> () != null) {
						attr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}

					if (color.ToLower() != attr) {
						RespondAndUpdate(string.Format ("Should I forget about this {0} block?", attr));
						TurnForward ();
						LookAt (indicatedObj.transform.position);
						eventConfirmation = "forget";
					}
				}
			}
		} 
	}

	void IndexBySize(string size) {
		if (eventManager.events.Count == 0) {
			if (objectMatches.Count > 0) {
				GameObject obj = objectMatches[0];
				if (size.ToLower() == "big") {
					foreach (GameObject match in objectMatches) {
						Debug.Log (match);
						if ((Helper.GetObjectWorldSize (match).size.x *
							Helper.GetObjectWorldSize (match).size.y *
							Helper.GetObjectWorldSize (match).size.z) >
							(Helper.GetObjectWorldSize (obj).size.x *
								Helper.GetObjectWorldSize (obj).size.y *
								Helper.GetObjectWorldSize (obj).size.z)) {
							obj = match;
						}
					}
				}
				else if (size.ToLower() == "small") {
					foreach (GameObject match in objectMatches) {
						if ((Helper.GetObjectWorldSize (match).size.x *
							Helper.GetObjectWorldSize (match).size.y *
							Helper.GetObjectWorldSize (match).size.z) <
							(Helper.GetObjectWorldSize (obj).size.x *
								Helper.GetObjectWorldSize (obj).size.y *
								Helper.GetObjectWorldSize (obj).size.z)) {
							obj = match;
						}
					}
				}

				objectMatches.Clear ();
				objectMatches.Add (obj);
				ResolveIndicatedObject ();
			}
		}
	}

	void Engage(bool state) {
		if (state == true) {
			sessionCounter++;
			if (sessionCounter > 1) {
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("Hi again!");
				}
			} 
			else {
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("Hello.");
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
				RespondAndUpdate("Bye!");

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

		epistemicModel.engaged = state;
	}

	public void BeginInteraction(object[] content) {
		RespondAndUpdate ("Hello.");
		MoveToPerform ();
		gestureController.PerformGesture (AvatarGesture.RARM_WAVE);

		if (!interactionLogic.waveToStart) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));
		}
	}

	public void Ready(object[] content) {
		RespondAndUpdate ("I'm ready to go!");
		MoveToPerform ();
		gestureController.PerformGesture (AvatarGesture.RARM_THUMBS_UP);

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));
	}

	public void Wait(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break; 

		case 1:
			string message = null;

			if (content [0] != null) {
				message = (string)content [0];

				if ((message.StartsWith ("YES")) || (message.StartsWith ("posack"))) {
					RespondAndUpdate ("OK.");
					LookForward ();
				}
				else if ((message.StartsWith ("NO")) || (message.StartsWith ("negack"))) {
					RespondAndUpdate ("OK.");
					LookForward ();

					if ((interactionLogic.IndicatedObj == null) && (interactionLogic.IndicatedRegion == null)) {
						TurnForward ();
					}
				}
			}
			break;

		default:
			break;
		}
	}

	public void Suggest(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break; 

		case 1:
			string message = (string)content [0];
			Debug.Log (message);

			if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("left point")) {
				Vector3 highlightCenter = TransformToSurface (
					GetGestureVector (interactionLogic.RemoveInputSymbolType(
						message,interactionLogic.GetInputSymbolType(message)), "left point"));

				MoveHighlight (highlightCenter);
				regionHighlight.transform.position = highlightCenter;

				Bounds surfaceBounds = Helper.GetObjectWorldSize (demoSurface);
				Region surfaceRegion = new Region (new Vector3 (surfaceBounds.min.x, highlightCenter.y, surfaceBounds.min.z),
					new Vector3 (surfaceBounds.max.x, highlightCenter.y, surfaceBounds.max.z));
				
				if ((highlightCenter.y > 0) && (surfaceRegion.Contains (highlightCenter))) { // on table
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null, null,
								null, null, new List<string> (new string[]{ message }))));

					List<GameObject> blockOptions = FindBlocksInRegion (new Region (highlightCenter, vectorConeRadius * highlightOscUpper * 2));

					if (blockOptions.Count == 0) {
						RespondAndUpdate ("Are you pointing here?");
					}
					else {
						RespondAndUpdate ("Are you pointing at this?");
					}

					LookForward ();

					if (interactionLogic.GraspedObj == null) {
						PointAt (highlightCenter, rightGrasper);
					}
					else {
						if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
							PointAt (highlightCenter, rightGrasper);
						}
						else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
							PointAt (highlightCenter, leftGrasper);
						}
					}
				}
				else {
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null,
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								null, null, null)));
				}
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("right point")) {
				Vector3 highlightCenter = TransformToSurface (
					GetGestureVector (interactionLogic.RemoveInputSymbolType(
						message,interactionLogic.GetInputSymbolType(message)), "right point"));
				
				MoveHighlight (highlightCenter);
				regionHighlight.transform.position = highlightCenter;

				Bounds surfaceBounds = Helper.GetObjectWorldSize (demoSurface);
				Region surfaceRegion = new Region (new Vector3 (surfaceBounds.min.x, highlightCenter.y, surfaceBounds.min.z),
					new Vector3 (surfaceBounds.max.x, highlightCenter.y, surfaceBounds.max.z));

				if ((highlightCenter.y > 0) && (surfaceRegion.Contains (highlightCenter))) { // on table
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null, null,
								null, null, new List<string> (new string[]{ message }))));
										
					List<GameObject> blockOptions = FindBlocksInRegion (new Region (highlightCenter, vectorConeRadius * highlightOscUpper * 2));

					if (blockOptions.Count == 0) {
						RespondAndUpdate ("Are you pointing here?");
					}
					else {
						RespondAndUpdate ("Are you pointing at this?");
					}

					LookForward ();

					if (interactionLogic.GraspedObj == null) {
						PointAt (highlightCenter, rightGrasper);
					} 
					else {
						if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
							PointAt (highlightCenter, rightGrasper);
						} 
						else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
							PointAt (highlightCenter, leftGrasper);
						}
					}
				}
				else {
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null,
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								null, null, null)));
				}			
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("posack")) {
				RespondAndUpdate("Yes?");
				MoveToPerform ();
				AllowHeadMotion ();

				if (interactionLogic.GraspedObj == null) {
					PerformAndLogGesture(AvatarGesture.RARM_THUMBS_UP);
				}
				else {
					if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
						PerformAndLogGesture(AvatarGesture.RARM_THUMBS_UP);
					}
					else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
						PerformAndLogGesture(AvatarGesture.LARM_THUMBS_UP);
					}
				}
				PerformAndLogGesture (AvatarGesture.HEAD_NOD);
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("negack")) {
				RespondAndUpdate("No?");
				MoveToPerform ();
				AllowHeadMotion ();

				if (interactionLogic.GraspedObj == null) {
					PerformAndLogGesture(AvatarGesture.RARM_THUMBS_DOWN);
				}
				else {
					if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
						PerformAndLogGesture(AvatarGesture.RARM_THUMBS_DOWN);
					}
					else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
						PerformAndLogGesture(AvatarGesture.LARM_THUMBS_DOWN);
					}
				}
				PerformAndLogGesture (AvatarGesture.HEAD_SHAKE);
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("grab")) {
				LookForward();
				MoveToPerform ();

				AvatarGesture performGesture = null;

				if ((interactionLogic.ActionOptions.Count > 0) && (interactionLogic.ActionSuggestions.Count > 0)) {
					if (interactionLogic.ActionOptions[0] == interactionLogic.ActionSuggestions[0]) {
						if (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
							interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])).StartsWith ("grab move")) {
							interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));

							string dir = interactionLogic.GetGestureContent (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
								interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "grab move");

							if (interactionLogic.GraspedObj == null) {	// not grasping anything
								if (dir == "left") {
									performGesture = AvatarGesture.RARM_CARRY_RIGHT;
								} 
								else if (dir == "right") {
									performGesture = AvatarGesture.RARM_CARRY_LEFT;
								} 
								else if (dir == "front") {
									performGesture = AvatarGesture.RARM_CARRY_BACK;
								} 
								else if (dir == "back") {
									performGesture = AvatarGesture.RARM_CARRY_FRONT;
								} 
								else if (dir == "up") {
									performGesture = AvatarGesture.RARM_CARRY_UP;
								} 
								else if (dir == "down") {
									performGesture = AvatarGesture.RARM_CARRY_DOWN;
								}

								RespondAndUpdate ("Do you want me to move something this way?");
							} 
							else {	// grasping something
								if (dir == "left") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_RIGHT;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_RIGHT;
									}
								} 
								else if (dir == "right") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_LEFT;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_LEFT;
									}
								} 
								else if (dir == "front") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_BACK;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_BACK;
									}
								} 
								else if (dir == "back") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_FRONT;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_FRONT;
									}
								} 
								else if (dir == "up") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_UP;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_UP;
									}
								} 
								else if (dir == "down") {
									if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_DOWN;
									} 
									else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_DOWN;
									}
								}
								RespondAndUpdate ("Do you want me to move this this way?");
							}
						}
					}
					else {
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null, null,
									null, null, new List<string> (new string[]{ message }))));

						if (interactionLogic.IndicatedObj == null) {
							RespondAndUpdate ("Are you asking me to grab something?");
						} 
						else {
							RespondAndUpdate ("Are you asking me to grab this?");
						}
						performGesture = AvatarGesture.RARM_CARRY_STILL;
					}
				}
				else {
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null, null,
								null, null, new List<string> (new string[]{ message }))));

					if (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
						interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])).StartsWith ("grab move")) {
						interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));

						string dir = interactionLogic.GetGestureContent (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
							interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "grab move");

						if (interactionLogic.GraspedObj == null) {	// not grasping anything
							if (dir == "left") {
								performGesture = AvatarGesture.RARM_CARRY_RIGHT;
							} 
							else if (dir == "right") {
								performGesture = AvatarGesture.RARM_CARRY_LEFT;
							} 
							else if (dir == "front") {
								performGesture = AvatarGesture.RARM_CARRY_BACK;
							} 
							else if (dir == "back") {
								performGesture = AvatarGesture.RARM_CARRY_FRONT;
							} 
							else if (dir == "up") {
								performGesture = AvatarGesture.RARM_CARRY_UP;
							} 
							else if (dir == "down") {
								performGesture = AvatarGesture.RARM_CARRY_DOWN;
							}

							RespondAndUpdate ("Do you want me to move something this way?");
						} 
						else {	// grasping something
							if (dir == "left") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_RIGHT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_RIGHT;
								}
							} 
							else if (dir == "right") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_LEFT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_LEFT;
								}
							} 
							else if (dir == "front") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_BACK;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_BACK;
								}
							} 
							else if (dir == "back") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_FRONT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_FRONT;
								}
							} 
							else if (dir == "up") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_UP;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_UP;
								}
							} 
							else if (dir == "down") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_CARRY_DOWN;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_CARRY_DOWN;
								}
							}
							RespondAndUpdate ("Do you want me to move this this way?");
						}
					}
					else {
						if (interactionLogic.IndicatedObj == null) {
							RespondAndUpdate ("Are you asking me to grab something?");
						} 
						else {
							RespondAndUpdate ("Are you asking me to grab this?");
						}
						performGesture = AvatarGesture.RARM_CARRY_STILL;
					}
				}
				PerformAndLogGesture (performGesture);
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("push")) {
				LookForward();
				MoveToPerform ();

				AvatarGesture performGesture = null;
				if ((interactionLogic.ActionOptions.Count > 0) && (interactionLogic.ActionSuggestions.Count > 0)) {
					if (interactionLogic.ActionOptions[0] == interactionLogic.ActionSuggestions[0]) {
						interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));

						string dir = interactionLogic.GetGestureContent (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
							interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "push");

						if (interactionLogic.GraspedObj == null) {	// not grasping anything
							if (dir == "left") {
								performGesture = AvatarGesture.LARM_PUSH_RIGHT;
							} 
							else if (dir == "right") {
								performGesture = AvatarGesture.RARM_PUSH_LEFT;
							} 
							else if (dir == "front") {
								performGesture = AvatarGesture.RARM_PUSH_BACK;
							} 
							else if (dir == "back") {
								performGesture = AvatarGesture.RARM_PUSH_FRONT;
							} 

							if (interactionLogic.IndicatedObj == null) {
								RespondAndUpdate ("Do you want me to push something this way?");
							}
							else {
								RespondAndUpdate ("Do you want me to push this this way?");
							}
						} 
						else {	// grasping something
							if (dir == "left") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_RIGHT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_RIGHT;
								}
							} 
							else if (dir == "right") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_LEFT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_LEFT;
								}
							} 
							else if (dir == "front") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_BACK;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_BACK;
								}
							} 
							else if (dir == "back") {
								if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_FRONT;
								} 
								else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_FRONT;
								}
							} 
							RespondAndUpdate ("Do you want me to push this this way?");
						}
					} 
				}
				else {
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null, null,
								null, null, new List<string> (new string[]{ message }))));

					string dir = interactionLogic.GetGestureContent (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
						interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "push");

					if (interactionLogic.GraspedObj == null) {	// not grasping anything
						if (dir == "left") {
							performGesture = AvatarGesture.LARM_PUSH_RIGHT;
						} 
						else if (dir == "right") {
							performGesture = AvatarGesture.RARM_PUSH_LEFT;
						} 
						else if (dir == "front") {
							performGesture = AvatarGesture.RARM_PUSH_BACK;
						} 
						else if (dir == "back") {
							performGesture = AvatarGesture.RARM_PUSH_FRONT;
						} 

						if (interactionLogic.IndicatedObj == null) {
							RespondAndUpdate ("Do you want me to push something this way?");
						}
						else {
							RespondAndUpdate ("Do you want me to push this this way?");
						}
					} 
					else {	// grasping something
						if (dir == "left") {
							if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_PUSH_RIGHT;
							} 
							else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_PUSH_RIGHT;
							}
						} 
						else if (dir == "right") {
							if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_PUSH_LEFT;
							} 
							else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_PUSH_LEFT;
							}
						} 
						else if (dir == "front") {
							if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_PUSH_BACK;
							} 
							else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_PUSH_BACK;
							}
						} 
						else if (dir == "back") {
							if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
								performGesture = AvatarGesture.RARM_PUSH_FRONT;
							} 
							else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
								performGesture = AvatarGesture.LARM_PUSH_FRONT;
							}
						} 
						RespondAndUpdate ("Do you want me to push this this way?");
					}
				}
				PerformAndLogGesture (performGesture);
			}
			break;

		default:
			break;
		}
	}

	public void Confirm(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));

		// can we use this state to confirm objects or actions?
	}

	public void TrackPointing(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			regionHighlight.GetComponent<Renderer> ().material = activeHighlightMaterial;
			highlightTimeoutTimer.Interval = highlightTimeoutTime;
			highlightTimeoutTimer.Enabled = true;

			if (interactionLogic.RemoveInputSymbolType ((string)content [0],
				interactionLogic.GetInputSymbolType ((string)content [0])).StartsWith ("l")) {
				highlightCenter = TransformToSurface (GetGestureVector (interactionLogic.RemoveInputSymbolType (
					(string)content [0], interactionLogic.GetInputSymbolType ((string)content [0])), "l"));
				interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
			}
			else if (interactionLogic.RemoveInputSymbolType ((string)content [0],
				interactionLogic.GetInputSymbolType ((string)content [0])).StartsWith ("r")) {
				highlightCenter = TransformToSurface (GetGestureVector (interactionLogic.RemoveInputSymbolType (
					(string)content [0], interactionLogic.GetInputSymbolType ((string)content [0])), "r"));
				interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
			}

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
			break;

		default:
			break;
		}
	}

	public void ParseSentence(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] != null) {
				message = GetSpeechString(
					interactionLogic.RemoveInputSymbolType((string)content [0],interactionLogic.GetInputSymbolType((string)content [0])), "S");

			}
			Debug.Log (message);
			// do stuff here

			break;

		default:
			break;
		}
	}

	public void ParseVP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] != null) {
				message = GetSpeechString(
					interactionLogic.RemoveInputSymbolType((string)content [0],interactionLogic.GetInputSymbolType((string)content [0])), "S");

			}
			Debug.Log (message);
			// do stuff here

			interactionLogic.RewriteStack(new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
				interactionLogic.GenerateStackSymbol (null, null, null,
					null, new List<string>(new string[]{ commBridge.NLParse (message) }), null)));
			break;

		default:
			break;
		}
	}

	public void ParseNP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] != null) {
				message = GetSpeechString(
					interactionLogic.RemoveInputSymbolType((string)content [0],interactionLogic.GetInputSymbolType((string)content [0])), "S");

			}
			Debug.Log (message);
			// do stuff here

			break;

		default:
			break;
		}
	}

	public void ParsePP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] != null) {
				message = GetSpeechString(
					interactionLogic.RemoveInputSymbolType((string)content [0],interactionLogic.GetInputSymbolType((string)content [0])), "S");

			}
			Debug.Log (message);
			// do stuff here

			break;

		default:
			break;
		}
	}

	public void SituateDeixis(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] == null) {
				Debug.Log (interactionLogic.StackSymbolToString(interactionLogic.CurrentStackSymbol));
				if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions).Count > 0) {
					message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions)[0];
				}
				else {
					return;
				}
			}
			else {
				message = (string)content [0];
			}
			Debug.Log (message);

			if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("left point")) {
				Vector3 highlightCenter = TransformToSurface (
					GetGestureVector (interactionLogic.RemoveInputSymbolType(
						message,interactionLogic.GetInputSymbolType(message)), "left point"));

				MoveHighlight (highlightCenter);
				regionHighlight.transform.position = highlightCenter;
				highlightTimeoutTimer.Interval = highlightTimeoutTime;
				highlightTimeoutTimer.Enabled = true;

				Region testRegion = new Region (
					new Vector3 (Helper.GetObjectWorldSize (demoSurface).min.x, regionHighlight.transform.position.y, Helper.GetObjectWorldSize (demoSurface).min.z),
					new Vector3 (Helper.GetObjectWorldSize (demoSurface).max.x, regionHighlight.transform.position.y, Helper.GetObjectWorldSize (demoSurface).max.z));

				if (Helper.RegionsEqual(interactionLogic.IndicatedRegion,new Region())) {	// empty region
					if (testRegion.Contains(highlightCenter) && (highlightCenter.y > 0.0f)) { // enabled = on table
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null,
									new Region (highlightCenter, vectorConeRadius * highlightOscUpper * 2),
									null, null, null)));
					}
					else {
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
									null, null, null)));
					}
				}
			}
			else if (interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("right point")) {
				Vector3 highlightCenter = TransformToSurface (GetGestureVector (
					interactionLogic.RemoveInputSymbolType(
						message,interactionLogic.GetInputSymbolType(message)), "right point"));

				MoveHighlight (highlightCenter);
				regionHighlight.transform.position = highlightCenter;
				highlightTimeoutTimer.Interval = highlightTimeoutTime;
				highlightTimeoutTimer.Enabled = true;

				Region testRegion = new Region (
					new Vector3 (Helper.GetObjectWorldSize (demoSurface).min.x, regionHighlight.transform.position.y, Helper.GetObjectWorldSize (demoSurface).min.z),
					new Vector3 (Helper.GetObjectWorldSize (demoSurface).max.x, regionHighlight.transform.position.y, Helper.GetObjectWorldSize (demoSurface).max.z));

				if (Helper.RegionsEqual(interactionLogic.IndicatedRegion,new Region())) {	// empty region
					if (testRegion.Contains(highlightCenter) && (highlightCenter.y > 0.0f)) { // enabled = on table
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null,
									new Region (highlightCenter, vectorConeRadius * highlightOscUpper * 2),
									null, null, null)));
					}
					else {
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
									null, null, null)));
					}
				}
			}
			else if ((interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("THIS")) ||
				(interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("THAT")) ||
				(interactionLogic.RemoveInputSymbolType(message,interactionLogic.GetInputSymbolType(message)).StartsWith ("THERE"))) {
				if (regionHighlight.GetComponent<Renderer> ().material.color.a == 1.0f) {
					if (Helper.RegionsEqual (interactionLogic.IndicatedRegion, new Region ())) {	// empty region
						interactionLogic.RewriteStack (
							new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol (null, null,
									new Region (highlightCenter, vectorConeRadius * highlightOscUpper * 2),
									null, null, null)));
					}
				}
				else {
					interactionLogic.RewriteStack (
						new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol (null, null, new FunctionDelegate(interactionLogic.NullObject),
								null, null, null)));
				}
			}
			break;

		default:
			break;
		}
	}

	public void InterpretDeixis(object[] content) {
		// region or object?
		//interactionLogic.ObjectOptions.Clear();
		List<GameObject> objectOptions = FindBlocksInRegion(interactionLogic.IndicatedRegion);

		//		objectPlacements = objectPlacements.OrderByDescending (o => o.transform.position.y).
		//			ThenBy (o => (o.transform.position - theme.transform.position).magnitude).ToList ();
		objectOptions = objectOptions.OrderBy (o => (o.transform.position - highlightCenter).magnitude).ToList();

		if (objectOptions.Count > 0) {
			interactionLogic.RewriteStack (
				new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol (null, null, null, objectOptions, null, null)));
		}
		else {
			interactionLogic.RewriteStack (
				new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol (null, null, interactionLogic.IndicatedRegion, 
						new List<GameObject>(), null, null)));
		}
	}

	public void DisambiguateObject(object[] content) {
		bool duplicateNominalAttr = false;
		List<Voxeme> objVoxemes = new List<Voxeme> ();

		foreach (GameObject option in interactionLogic.ObjectOptions) {
			if (option.GetComponent<Voxeme> () != null) {
				objVoxemes.Add (option.GetComponent<Voxeme> ());
			}
		}

		List<object> uniqueAttrs = new List<object> ();
		for (int i = 0; i < objVoxemes.Count; i++) {
			List<object> newAttrs = Helper.DiffLists (
				uniqueAttrs.Select (x => ((Vox.VoxAttributesAttr)x).Value).Cast<object> ().ToList (),
				objVoxemes [i].voxml.Attributes.Attrs.Cast<object> ().ToList ().Select (x => ((Vox.VoxAttributesAttr)x).Value).Cast<object> ().ToList ());

			if (newAttrs.Count > 0) {
				foreach (object attr in newAttrs) {
					Debug.Log (string.Format ("{0}:{1}", objVoxemes [i].name, attr.ToString ()));
					Vox.VoxAttributesAttr attrToAdd = new Vox.VoxAttributesAttr ();
					attrToAdd.Value = attr.ToString ();

					if (uniqueAttrs.Where (x => ((Vox.VoxAttributesAttr)x).Value == attrToAdd.Value).ToList ().Count == 0) {
						uniqueAttrs.Add (attrToAdd);
					}
				}
			}
			else {
				duplicateNominalAttr = true;
			}
		}

//		Debug.Log (interactionLogic.ObjectOptions);
//		Debug.Log (interactionLogic.IndicatedObj.name);
		string attribute = ((Vox.VoxAttributesAttr)uniqueAttrs [uniqueAttrs.Count-1]).Value.ToString ();

		if (duplicateNominalAttr) {
			RespondAndUpdate (string.Format ("Which {0} block?", attribute));
			LookForward ();
		}
		else {
			if ((interactionLogic.GraspedObj == null) &&
			   (interactionLogic.ObjectOptions.Contains (interactionLogic.IndicatedObj))) {
				RespondAndUpdate (string.Format ("Do you mean the {0} one?", attribute));
				ReachFor (interactionLogic.IndicatedObj);
				LookForward ();
			}
			else if ((interactionLogic.IndicatedObj != null) &&
					(!interactionLogic.ObjectOptions.Contains (interactionLogic.IndicatedObj))) {
				RespondAndUpdate (string.Format ("Should I put the {0} block on the {1} block?",
					interactionLogic.IndicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value,
					attribute));
				LookForward ();
			}
			else if (interactionLogic.GraspedObj != null) {
				RespondAndUpdate (string.Format ("Should I put the {0} block on the {1} block?",
					interactionLogic.GraspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value,
					attribute));
				LookForward ();
			}
		}
	}

	public void IndexByColor(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			List<GameObject> objectOptions = new List<GameObject>();

			foreach (GameObject block in blocks) {
				bool isKnown = true;

				if (dianaMemory != null && dianaMemory.enabled) {
					isKnown = dianaMemory.IsKnown (block.GetComponent<Voxeme>());
				}

				if ((block.activeInHierarchy) || (objSelector.disabledObjects.Contains(block))) {
					if ((block.GetComponent<AttributeSet> ().attributes.Contains (
						interactionLogic.RemoveInputSymbolType(
							content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower ())) && 
						(isKnown) && (SurfaceClear(block)) && (block != interactionLogic.IndicatedObj) &&
						(block != interactionLogic.GraspedObj)){
						objectOptions.Add (block);
					}

					if ((objectOptions.Contains (block)) && (isKnown) && ((block == interactionLogic.IndicatedObj) ||
						(block == interactionLogic.GraspedObj))) {
						objectOptions.Remove (block);
					}
				}
				else {
					if ((objectOptions.Contains (block)) && (isKnown)) {
						objectOptions.Remove (block);
					}
				}
			}

			if (objectOptions.Count > 0) {
				interactionLogic.RewriteStack (
					new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol (null, null, null, objectOptions, null, null)));
			}
			else {
				interactionLogic.RewriteStack (
					new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol (null, null, null, 
							null, null, null)));
			}
			break;

		default:
			break;
		}
	}

	public void IndexBySize(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = interactionLogic.RemoveInputSymbolType(
				content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower ();
			GameObject obj = null;
			if (message == "big") {
				obj = interactionLogic.ObjectOptions.OrderByDescending (o => 
					Helper.GetObjectWorldSize (o).size.x *
					Helper.GetObjectWorldSize (o).size.y *
					Helper.GetObjectWorldSize (o).size.z).ToList () [0];
			}
			else if (message == "small") {
				obj = interactionLogic.ObjectOptions.OrderBy (o => 
					Helper.GetObjectWorldSize (o).size.x *
					Helper.GetObjectWorldSize (o).size.y *
					Helper.GetObjectWorldSize (o).size.z).ToList () [0];
			}

			interactionLogic.RewriteStack (
				new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol (null, null, null, new List<GameObject>(new GameObject[] { obj }), null, null)));
			break;

		default:
			break;
		}
	}

	public void IndexByRegion(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			Region region = null;
			if (interactionLogic.RemoveInputSymbolType(
				content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower () == "left") {
				region = leftRegion;
			}
			else if (interactionLogic.RemoveInputSymbolType(
				content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower () == "right") {
				region = rightRegion;
			}
			else if (interactionLogic.RemoveInputSymbolType(
				content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower () == "front") {
				region = frontRegion;
			}
			else if (interactionLogic.RemoveInputSymbolType(
				content[0].ToString(),interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower () == "back") {
				region = backRegion;
			}

			if (Helper.RegionsEqual(interactionLogic.IndicatedRegion,new Region())) {	// empty region
				interactionLogic.RewriteStack (
					new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol (null, null,
							region,
							null, null, null)));
			}
			break;

		default:
			break;
		}
	}

	public void RegionAsGoal(object[] content) {
		if ((interactionLogic.IndicatedObj != null) || (interactionLogic.GraspedObj != null)) {
			RespondAndUpdate ("Should I place this here?");
		}
		else {
			RespondAndUpdate ("Should I place something here?");
		}
		ReachFor (interactionLogic.IndicatedRegion.center);
		LookForward ();
	}

	public void ConfirmObject(object[] content) {
		if (interactionLogic.ActionOptions.Count == 0) {
			RespondAndUpdate ("OK, go on.");
		}
		else {
			RespondAndUpdate ("OK.");
		}

		if (interactionLogic.GraspedObj == null) {
			ReachFor (interactionLogic.IndicatedObj);
		}

		LookForward ();

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void RequestObject(object[] content) {
		if (interactionLogic.ActionOptions.Count > 0) {
			if ((new Regex(@"grasp\(\{0\}\)")).IsMatch (interactionLogic.ActionOptions [0])) {
				RespondAndUpdate ("What should I grab?");
			}
			else if ((new Regex(@"put\(\{0\},<.+,.+,.+>\)")).IsMatch (interactionLogic.ActionOptions [0])) {
				RespondAndUpdate ("What should I put there?");
			}
			else if (((new Regex(@"put\(\{0\},.+\)")).IsMatch (interactionLogic.ActionOptions [0])) ||
				(interactionLogic.RemoveInputSymbolType(interactionLogic.ActionOptions [0], 
					interactionLogic.GetInputSymbolType(interactionLogic.ActionOptions[0])).StartsWith("grab move"))) {
				RespondAndUpdate ("What should I move?");
			}
			else if (((new Regex(@"slide\(\{0\},.+\)")).IsMatch (interactionLogic.ActionOptions [0])) ||
				(interactionLogic.RemoveInputSymbolType(interactionLogic.ActionOptions [0], 
					interactionLogic.GetInputSymbolType(interactionLogic.ActionOptions[0])).StartsWith("push"))) {
				RespondAndUpdate ("What should I push?");
			}
		}
		else {
			RespondAndUpdate ("Which object do you want?");
		}
	}

	public void PlaceInRegion(object[] content) {
		RespondAndUpdate("OK.");

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol (null, null, null, null, 
				new List<string>(new string[]{string.Format("put({0},{1})",interactionLogic.IndicatedObj.name,
					Helper.VectorToParsable(interactionLogic.IndicatedRegion.center))}),
				null)));
	}

	public void DisambiguateEvent(object[] content) {
		LookForward();
		RespondAndUpdate(string.Format ("Should I {0}?", confirmationTexts [
			interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count-1]]));
	}

	public void ComposeObjectAndAction(object[] content) {
		if (interactionLogic.IndicatedObj != null) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol (null, null, null, null, 
					new List<string> (new string[]{ string.Format (interactionLogic.ActionOptions [0], interactionLogic.IndicatedObj.name) }),
					null)));
		}
		else if (interactionLogic.GraspedObj != null) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol (null, null, null, null, 
					new List<string> (new string[]{ string.Format (interactionLogic.ActionOptions [0], interactionLogic.GraspedObj.name) }),
					null)));
		}
	}

	public void ConfirmEvent(object[] content) {
		RespondAndUpdate ("OK.");
		PromptEvent (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1]);

		if (Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "grasp")) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol (new DelegateFactory (new FunctionDelegate (interactionLogic.NullObject)),
					interactionLogic.IndicatedObj, null, null, null, null)));
		}
		else if (Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
		}
		else {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol (null, new DelegateFactory(new FunctionDelegate (interactionLogic.NullObject)),
					null, null, null, null)));
		}
	}

	public void ExecuteEvent(object[] content) {
		if ((interactionLogic.ActionOptions.Count > 0) && 
			((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "grasp")) ||
				(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put")))) {
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
		}
	}

	public void StartGrab(object[] content) {
		RespondAndUpdate ("OK.");
		PromptEvent (string.Format ("grasp({0})", interactionLogic.IndicatedObj.name));

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
			interactionLogic.GenerateStackSymbol(new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				interactionLogic.IndicatedObj, null, null, null, null)));
	}

	public void StartGrabMove(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			List<string> actionOptions = new List<string> ();
			actionOptions.Add (content [0].ToString ());

			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol(null, null, null, null, 
					actionOptions, null)));
			break;

		default:
			break;
		}
	}

	public void StopGrabMove(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] == null) {
				Debug.Log (interactionLogic.StackSymbolToString (interactionLogic.CurrentStackSymbol));
				if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions).Count > 0) {
					message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions) [0];
				} 
				else {
					return;
				}
			}
			else {
				if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions).Count > 0) {
					message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions) [0];
				} 
				else {
					return;
				}
			}

			Debug.Log (message);
			string dir = string.Empty;

			if (interactionLogic.GetInputSymbolType (message) == 'G') {
				dir = interactionLogic.GetGestureContent (
					interactionLogic.RemoveInputSymbolType (
						interactionLogic.RemoveGestureTrigger (
							message, interactionLogic.GetGestureTrigger (message)),
						interactionLogic.GetInputSymbolType (message)),
					"grab move").ToLower();
			}
			else if (interactionLogic.GetInputSymbolType (message) == 'S') {
				dir = interactionLogic.RemoveInputSymbolType (message, interactionLogic.GetInputSymbolType (message)).ToLower ();
			}

			Debug.Log (dir);
			List<string> options = PopulateMoveOptions (interactionLogic.GraspedObj, dir);

//			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
//				Enumerable.Range(0,options.Count).Select(s => interactionLogic.GenerateStackSymbol (null,
//					dir == "up" ? null : new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null, 
//					options.ToArray().Reverse().ToList().GetRange(0,s+1), new List<string>())).ToList()));
			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
				Enumerable.Range(0,options.Count).Select(s => interactionLogic.GenerateStackSymbol (null, null, null, null, 
					options.ToArray().Reverse().ToList().GetRange(0,s+1), new List<string>())).ToList()));
			break;

		default:
			break;
		}
	}

	public void StopGrab(object[] content) {
		RespondAndUpdate ("OK.");
		PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
			interactionLogic.GenerateStackSymbol(null, new FunctionDelegate(interactionLogic.NullObject), 
				null, null, null, null)));
	}

	public void StartPush(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			List<string> actionOptions = new List<string> ();
			actionOptions.Add (content [0].ToString ());

			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, 
				interactionLogic.GenerateStackSymbol(null, null, null, null, 
					actionOptions, null)));
			break;

		default:
			break;
		}
	}

	public void StopPush(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content,typeof(string))) {
			return;
		}

		switch (content.Length) {
		case 0:
			break;

		case 1:
			string message = null;

			if (content [0] == null) {
				Debug.Log (interactionLogic.StackSymbolToString (interactionLogic.CurrentStackSymbol));
				if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions).Count > 0) {
					message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionSuggestions) [0];
				} 
			} else {
				if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions).Count > 0) {
					message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions) [0];
				} 
			}

			Debug.Log (message);
			string dir = string.Empty;

			if (message != null) {
				if (interactionLogic.GetInputSymbolType (message) == 'G') {
					dir = interactionLogic.GetGestureContent (
						interactionLogic.RemoveInputSymbolType (
							interactionLogic.RemoveGestureTrigger (
								message, interactionLogic.GetGestureTrigger (message)),
							interactionLogic.GetInputSymbolType (message)),
						"push").ToLower ();
				}
				else if (interactionLogic.GetInputSymbolType (message) == 'S') {
					dir = interactionLogic.humanRelativeDirections ?
						interactionLogic.RemoveInputSymbolType (message, interactionLogic.GetInputSymbolType (message)).ToLower () :
						oppositeDir[interactionLogic.RemoveInputSymbolType (message, interactionLogic.GetInputSymbolType (message)).ToLower ()];
				}
			}
			else {
				dir = interactionLogic.ActionOptions [0].Split (',') [1].TrimEnd (')');
			}

			Debug.Log (dir);
			List<string> options = PopulatePushOptions ((interactionLogic.GraspedObj == null) ?
				interactionLogic.IndicatedObj : interactionLogic.GraspedObj, dir);

			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
				Enumerable.Range(0,options.Count).Select(s => interactionLogic.GenerateStackSymbol (null,
					new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null, 
					options.ToArray().Reverse().ToList().GetRange(0,s+1), new List<string>())).ToList()));
			break;

		default:
			break;
		}
	}

	public void AbortAction(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put"))) {
					RaycastHit hitInfo;

					if ((!Physics.Raycast (new Ray (interactionLogic.GraspedObj.transform.position, Vector3.down), out hitInfo)) ||
						(hitInfo.collider.gameObject == demoSurfaceCollider.gameObject)){
						PromptEvent (string.Format ("put({0},{1})", 
							interactionLogic.GraspedObj.name,
							Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
								Helper.GetObjectWorldSize (demoSurface).max.y,
								interactionLogic.GraspedObj.transform.position.z))));
					}
					else {
						PromptEvent (string.Format ("put({0},{1})", 
							interactionLogic.GraspedObj.name,
							Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
								Helper.GetObjectWorldSize (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject)).max.y,
								interactionLogic.GraspedObj.transform.position.z))));
					}
				}
				else {
					PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward ();
			TurnForward ();
		}

		RespondAndUpdate ("OK, never mind.");

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));
	}

	public void BlockUnavailable(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "grab move"))) {
					PromptEvent (string.Format ("put({0},{1})", 
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize (demoSurface).max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
				else {
					PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward ();
			TurnForward ();
		}
			
		RespondAndUpdate ("Sorry, I can't find a block like that that I can use.");

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));
	}

	public void Confusion(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "grab move"))) {
					PromptEvent (string.Format ("put({0},{1})", 
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize (demoSurface).max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
				else {
					PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward ();
			TurnForward ();
		}

		int choice = RandomHelper.RandomInt (0, 3);

		switch (choice) {
		case 1:
			RespondAndUpdate ("Sorry, I'm confused.");
			break;

		case 2:
			RespondAndUpdate ("Sorry, I don't understand.");
			break;

		default:
			RespondAndUpdate ("Sorry, I don't know what you mean.");
			break;
		}

		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,null));
	}

	public void CleanUp(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) ||
					(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put"))) {
					RaycastHit hitInfo;

					if ((!Physics.Raycast (new Ray (interactionLogic.GraspedObj.transform.position, Vector3.down), out hitInfo)) ||
						(hitInfo.collider.gameObject == demoSurfaceCollider.gameObject)){
						PromptEvent (string.Format ("put({0},{1})", 
							interactionLogic.GraspedObj.name,
							Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
								Helper.GetObjectWorldSize (demoSurface).max.y,
								interactionLogic.GraspedObj.transform.position.z))));
					}
					else {
						PromptEvent (string.Format ("put({0},{1})", 
							interactionLogic.GraspedObj.name,
							Helper.VectorToParsable (new Vector3 (interactionLogic.GraspedObj.transform.position.x,
								Helper.GetObjectWorldSize (Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject)).max.y,
								interactionLogic.GraspedObj.transform.position.z))));
					}
				}
				else {
					PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward ();
			TurnForward ();
		}
	
		interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol (null, new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null,
				null, null, null)));
	}

	public void EndState(object[] content) {
		RespondAndUpdate ("Bye!");
	}

	public void MoveToPerform() {
		bool leftGrasping = false;
		bool rightGrasping = false;

		if (graspedObj != null) {
			if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
				leftGrasping = true;
			}
			else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
				rightGrasping = true;
			}
		}

		if (!leftGrasping) {
			Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).positionWeight = 0.0f;
			Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.LeftHand).rotationWeight = 0.0f;
		}

		if (!rightGrasping) {
			Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).positionWeight = 0.0f;
			Diana.GetComponent<FullBodyBipedIK> ().solver.GetEffector (FullBodyBipedEffector.RightHand).rotationWeight = 0.0f;
		}

		LookForward ();
	}

	public void TurnForward() {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
	}

	public void LookForward() {
		Diana.GetComponent<LookAtIK> ().solver.target.position = headTargetDefault;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.8f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 0.0f;
	}

	public void AllowHeadMotion() {
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.8f;
	}

	void Deixis(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		RespondAndUpdate("");
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
				RespondAndUpdate("Sorry, I don't know what you mean.");
				return;
			}

			TurnForward ();
			LookAt (region.center);

			foreach (GameObject block in blocks) {
				bool isKnown = true;

				if (dianaMemory != null && dianaMemory.enabled) {
					isKnown = dianaMemory.IsKnown (block.GetComponent<Voxeme>());
				}

				if (block.activeInHierarchy) {
					if (region.Contains (block)) {
						if ((!objectMatches.Contains (block)) && (SurfaceClear(block)) && (isKnown)) {
							objectMatches.Add (block);
						}
					} 
				}
			}

			if (useOrderingHeuristics) {
				objectMatches = objectMatches.OrderBy (o => (o.transform.position - Diana.transform.position).magnitude).ToList ();
			}

			if (objectMatches.Count > 0) {
				ResolveIndicatedObject ();
			} 
			else {	// indicating region
				indicatedRegion = region;
				RespondAndUpdate("Sorry, I don't know what you mean.");
			}
		}
	}

	void Deixis(GameObject obj) {
		bool isKnown = true;

		if (dianaMemory != null && dianaMemory.enabled) {
			isKnown = dianaMemory.IsKnown (obj.GetComponent<Voxeme>());
		}

		objectMatches.Clear ();

		if (obj.activeInHierarchy) {
			if ((!objectMatches.Contains (obj)) && (SurfaceClear(obj)) && (isKnown)) {
				objectMatches.Add (obj);
			}
		}

		if (objectMatches.Count > 0) {
			ResolveIndicatedObject ();
		}
	}

	void Deixis(Vector3 coord) {
		if (eventManager.events.Count > 0) {
			return;
		}

		highlightTimeoutTimer.Enabled = true;

		//OutputHelper.PrintOutput (Role.Affector, "");
		Region region = null;

		Vector3 highlightCenter = coord;

		Debug.Log (string.Format("Deixis: {0}",highlightCenter));

		MoveHighlight (highlightCenter);
		regionHighlight.transform.position = highlightCenter;

		//TurnForward ();
		//LookAt (cube.transform.position);

		foreach (GameObject block in blocks) {
			bool isKnown = true;

			if (dianaMemory != null && dianaMemory.enabled) {
				isKnown = dianaMemory.IsKnown (block.GetComponent<Voxeme>());
			}

			if (block.activeInHierarchy) {
				Vector3 point = Helper.GetObjectWorldSize(block).ClosestPoint(highlightCenter);
				//Debug.Log (string.Format("{0}:{1} {2} {3}",block,point,highlightCenter,(point-highlightCenter).magnitude));
				if ((point - highlightCenter).magnitude <= vectorConeRadius * highlightOscUpper) {
					//if (region.Contains (new Vector3 (block.transform.position.x,
					//	region.center.y, block.transform.position.z))) {
					if ((!objectMatches.Contains (block)) && (SurfaceClear (block)) && (isKnown)) {
						objectMatches.Add (block);
					}
				}
				else {
					if ((objectMatches.Contains (block)) && (isKnown)) {
						objectMatches.Remove (block);
					}
				}
			}
		}


		if ((indicatedObj == null) && (graspedObj == null)) {
			if (objectMatches.Count > 0) {
				TurnForward ();
				ReachFor (new Vector3 (highlightCenter.x, highlightCenter.y + Helper.GetObjectSize (objectMatches [0].gameObject).max.y,
					highlightCenter.z));
				ResolveIndicatedObject ();
			}
			else if (graspedObj == null) {	// indicating region
				indicatedRegion = new Region (new Vector3 (highlightCenter.x - vectorConeRadius, highlightCenter.y, highlightCenter.z - vectorConeRadius),
					new Vector3 (highlightCenter.x + vectorConeRadius, highlightCenter.y, highlightCenter.z + vectorConeRadius));
				//OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you're pointing at.");
			}
		}
		else {	// already indicated another object
			if (objectMatches.Count > 0) {
				if (eventManager.events.Count == 0) {
					string themeAttr = string.Empty;
					if (indicatedObj.GetComponent<Voxeme> () != null) {
						themeAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}

					string otherAttr = string.Empty;
					if (objectMatches[0].GetComponent<Voxeme> () != null) {
						otherAttr = indicatedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
					}

					if (themeAttr != otherAttr) {
						RespondAndUpdate(string.Format ("Should I forget about this {0} block?", themeAttr));
						TurnForward ();
						LookAt (indicatedObj.transform.position);
						eventConfirmation = "forget";
					}
				}
			}
		}
	}

	void TrackPointing(List<float> vector) {
		highlightTimeoutTimer.Enabled = true;
		regionHighlight.GetComponent<Renderer> ().material.color = new Color (0.0f, 1.0f, 0.0f,
			regionHighlight.GetComponent<Renderer> ().material.color.a);

		if (eventManager.events.Count > 0) {
			return;
		}

		// TODO: output timeout timer
		//		if ((indicatedObj == null) && (graspedObj == null)) {
		//			OutputHelper.PrintOutput (Role.Affector, "");
		//		}

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

	Vector3 MoveHighlight(Vector3 highlightCenter, float variance = 0.0f) {
		Vector3 offset = regionHighlight.transform.position - highlightCenter;
		Vector3 normalizedOffset = Vector3.Normalize (offset);

		regionHighlight.transform.position = new Vector3 (regionHighlight.transform.position.x - normalizedOffset.x * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.y - normalizedOffset.y * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.z - normalizedOffset.z * Time.deltaTime * highlightMoveSpeed);

		Vector3 normalizedScaleOffset = Vector3.Normalize (new Vector3(regionHighlight.transform.localScale.x - vectorConeRadius * (.2f + 10.0f*variance),
			regionHighlight.transform.localScale.y - vectorConeRadius * (.2f + 10.0f*variance),
			regionHighlight.transform.localScale.z - vectorConeRadius * (.2f + 10.0f*variance)));

		varianceScaleFactor = regionHighlight.transform.localScale;

		regionHighlight.transform.localScale = new Vector3 (regionHighlight.transform.localScale.x - normalizedScaleOffset.x * Time.deltaTime * highlightOscSpeed,
			regionHighlight.transform.localScale.y - normalizedScaleOffset.y * Time.deltaTime * highlightOscSpeed,
			regionHighlight.transform.localScale.z - normalizedScaleOffset.z * Time.deltaTime * highlightOscSpeed);

		if ((regionHighlight.transform.position.x+vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.x) ||
			(regionHighlight.transform.position.x-vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.x) ||
			(regionHighlight.transform.position.z+vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.z) ||
			(regionHighlight.transform.position.z-vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.z)) {
			// hide region highlight
			regionHighlight.GetComponent<Renderer> ().material.color = new Color(1.0f,1.0f,1.0f,
				(1.0f/((regionHighlight.transform.position-
					new Vector3(demoSurface.transform.position.x,Helper.GetObjectWorldSize(demoSurface).max.y,demoSurface.transform.position.z)).magnitude+Constants.EPSILON))*
				regionHighlight.transform.position.y);
			//Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.color.a);
		}
		else {
			regionHighlight.GetComponent<Renderer> ().material.color = new Color(1.0f,1.0f,1.0f,1.0f);
		}

		return offset;
	}

	void ResolveIndicatedObject() {
		Debug.Log (string.Format("Object matches: {0}",objectMatches.Count));
		if (objectMatches.Count == 1) {	// single object match
			if (interactionPrefs.verbosityLevel == InteractionPrefsModalWindow.VerbosityLevel.Everything) {
				Disambiguate (objectMatches);
			} 
			else {
				indicatedObj = objectMatches [0];
				objectMatches.Clear ();

				if (interactionPrefs.disambiguationStrategy == InteractionPrefsModalWindow.DisambiguationStrategy.DeicticGestural) {
					TurnForward();
					ReachFor (indicatedObj);
					RespondAndUpdate("OK, go on.");
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

		if (suggestedActions.Count > 0) {
			if (suggestedActions [0].Contains ("{0}")) {
				if (indicatedObj != null) {
					suggestedActions [0] = string.Format (suggestedActions [0], indicatedObj.name);
					PopulateOptions (suggestedActions [0].Split ('(')[0], indicatedObj,
						suggestedActions [0].Contains(',') ? suggestedActions [0].Split (',')[1].Replace(")","") : "");
					//actionOptions = new List<string> (suggestedActions);
					suggestedActions.Clear ();
					Disambiguate (actionOptions);
				}
			}
		}
	}

	void PopulateOptions(string program, GameObject theme, string dir) {
		switch (program) {
		case "grasp":
			if (graspedObj == null) {
				PopulateGrabOptions (theme);
			}
			break;

		case "put":
			PopulateMoveOptions (theme, dir);
			break;

		case "slide":
			PopulatePushOptions (theme, dir);
			break;

		default:
			break;
		}
	}

	void PopulateGrabOptions(GameObject theme, CertaintyMode certainty = CertaintyMode.Act) {
		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme> () != null) {
			themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
		}

		if (certainty == CertaintyMode.Act) {
			if (!actionOptions.Contains (string.Format ("grasp({0})", theme.name))) {
				actionOptions.Add (string.Format ("grasp({0})", theme.name));
			}

			if (!confirmationTexts.ContainsKey (string.Format ("grasp({0})", theme.name))) {
				confirmationTexts.Add (string.Format ("grasp({0})", theme.name),
					string.Format ("grab the {0} block", themeAttr));
			}
		}
		else if (certainty == CertaintyMode.Suggest) {
			if (!suggestedActions.Contains (string.Format ("grasp({0})", theme.name))) {
				suggestedActions.Add (string.Format ("grasp({0})", theme.name));
			}

			if (!confirmationTexts.ContainsKey (string.Format ("grasp({0})", theme.name))) {
				confirmationTexts.Add (string.Format ("grasp({0})", theme.name),
					string.Format ("grab the {0} block", themeAttr));
			}
		}
	}

	void Grab(bool state) {
		if (eventManager.events.Count > 0) {
			return;
		}

		if (eventConfirmation == "") {
			RespondAndUpdate("");
		}

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
					LookForward();
					graspedObj = indicatedObj;
					indicatedObj = null;
					indicatedRegion = null;
					suggestedActions.Clear ();
					actionOptions.Clear ();
					eventConfirmation = "";
					RespondAndUpdate("OK.");
				}
			} 
			else if ((graspedObj == null) && (indicatedObj == null) && (objectConfirmation == null)) {
				//OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("What do you want me to grab?");
					LookForward();
					if (!suggestedActions.Contains ("grasp({0})")) {
						suggestedActions.Add ("grasp({0})");
					}
				}
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

	List<GameObject> FindBlocksInRegion(Region region) {
		List<GameObject> blockOptions = new List<GameObject> ();

		foreach (GameObject block in blocks) {
			bool isKnown = true;

			if (dianaMemory != null && dianaMemory.enabled) {
				isKnown = dianaMemory.IsKnown (block.GetComponent<Voxeme>());
			}

			if ((block.activeInHierarchy) || (objSelector.disabledObjects.Contains(block))) {
				Vector3 point = Helper.GetObjectWorldSize(block).ClosestPoint(highlightCenter);
				if (region.Contains(new Vector3(point.x, region.center.y, point.z))) {
					if ((!blockOptions.Contains (block)) && (SurfaceClear (block)) && (isKnown) && 
						(block != interactionLogic.IndicatedObj) && (block != interactionLogic.GraspedObj)) {
						blockOptions.Add (block);
					}
				}
				else {
					if ((blockOptions.Contains (block)) && (isKnown)) {
						blockOptions.Remove (block);
					}
				}
			}
		}

		return blockOptions;
	}

	List<string> PopulateMoveOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<string> moveOptions = new List<string> ();
		List<object> placementOptions = FindPlacementOptions (theme, dir);

		if (interactionLogic.useOrderingHeuristics) {
			List<GameObject> objectPlacements = placementOptions.OfType<GameObject> ().ToList ();

			objectPlacements = objectPlacements.OrderByDescending (o => o.transform.position.y).
				ThenBy (o => (o.transform.position - theme.transform.position).magnitude).ToList ();

			for (int i = 0; i < placementOptions.Count; i++) {
				if (placementOptions [i] is GameObject) {
					placementOptions [i] = objectPlacements [i];
				}
			}
		}

		List<Region> orthogonalRegions = new List<Region> ();
		if (dir == "left") {
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
		}
		else if (dir == "right") {
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
		}
		else if (dir == "front") {
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
		}
		else if (dir == "back") {
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
		}

		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme> () != null) {
			themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
		}

		foreach (object option in placementOptions) {
			if (option is GameObject) {
				GameObject obj = (option as GameObject);
				if (theme != obj) {
					if (SurfaceClear (obj)) {
						string objAttr = string.Empty;
						if (obj.GetComponent<Voxeme> () != null) {
							objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
						}

						if (certainty == CertaintyMode.Act) {
							if (!moveOptions.Contains (string.Format ("put({0},on({1}))", theme.name, obj.name))) {
								moveOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
							}

							if (!confirmationTexts.ContainsKey (string.Format ("put({0},on({1}))", theme.name, obj.name))) {
								confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
									string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
							}
						}
						else if (certainty == CertaintyMode.Suggest) {
							if (!suggestedActions.Contains (string.Format ("put({0},on({1}))", theme.name, obj.name))) {
								suggestedActions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
							}

							if (!confirmationTexts.ContainsKey (string.Format ("put({0},on({1}))", theme.name, obj.name))) {
								confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
									string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
							}
						}
					}
				}
			}
			else if (option is Vector3) {
				Vector3 target = (Vector3)option;

				if (certainty == CertaintyMode.Act) {
					if (!moveOptions.Contains (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						moveOptions.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)));
					}

					foreach (Region region in orthogonalRegions) {
						if (region.Contains (target)) {
							if (!confirmationTexts.ContainsKey (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)))) {
								confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
									string.Format ("put the {0} block in the table's {1} {2} part", themeAttr, regionLabels [region], dir));
							}
						}
					}
				}
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						suggestedActions.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)));
					}

					foreach (Region region in orthogonalRegions) {
						if (region.Contains (target)) {
							if (!confirmationTexts.ContainsKey (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)))) {
								confirmationTexts.Add (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)),
									string.Format ("put the {0} block in the table's {1} {2} part", themeAttr, regionLabels [region], dir));
							}
						}
					}
				}
			}
		}

		if (dir == "up") {
			if (certainty == CertaintyMode.Act) {
				if (!moveOptions.Contains (string.Format ("lift({0})", theme.name))) {
					moveOptions.Add (string.Format ("lift({0})", theme.name));
				}

				if (!confirmationTexts.ContainsKey (string.Format ("lift({0})", theme.name))) {
					confirmationTexts.Add (string.Format ("lift({0})", theme.name), string.Format ("lift the {0} block", themeAttr));
				}
			}
			else if (certainty == CertaintyMode.Suggest) {
				if (!suggestedActions.Contains (string.Format ("lift({0})", theme.name))) {
					suggestedActions.Add (string.Format ("lift({0})", theme.name));
				}

				if (!confirmationTexts.ContainsKey (string.Format ("lift({0})", theme.name))) {
					confirmationTexts.Add (string.Format ("lift({0})", theme.name), string.Format ("lift the {0} block", themeAttr));
				}
			}
		} 
		else if (dir == "down") {
			if (eventConfirmation == "") {
				Vector3 target = new Vector3 (theme.transform.position.x,
					Helper.GetObjectWorldSize (demoSurface).max.y,
					theme.transform.position.z);

				if (certainty == CertaintyMode.Act) {
					if (!moveOptions.Contains (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)))) {
						moveOptions.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)));
					}

					if (!confirmationTexts.ContainsKey (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						confirmationTexts.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), string.Format ("put the {0} block down", themeAttr));
					}
				}
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)))) {
						suggestedActions.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)));
					}

					if (!confirmationTexts.ContainsKey (string.Format ("put({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						confirmationTexts.Add (string.Format ("put({0},{1})", theme.name,
							Helper.VectorToParsable (target)), string.Format ("put the {0} block down", themeAttr));
					}
				}
			}
		}

		return moveOptions;
	}

	void Move(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		if (eventConfirmation == "") {
			RespondAndUpdate("");
		}

		GameObject theme = null;
		if (indicatedObj != null) {
			theme = indicatedObj;
		}
		else if (graspedObj != null) {
			theme = graspedObj;
		}

		if (theme != null) {
			PopulateMoveOptions (theme, dir);

			//			if (dir == "up") {
			//				string objAttr = string.Empty;
			//				if (theme.GetComponent<Voxeme> () != null) {
			//					objAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
			//				}
			//
			//				actionOptions.Add (string.Format ("lift({0})", theme.name));
			//				confirmationTexts.Add (string.Format ("lift({0})", theme.name), string.Format ("lift the {0} block ", objAttr));
			//			} 
			//			else if (dir == "down") {
			//				if (eventConfirmation == "") {
			//					string objAttr = string.Empty;
			//					if (theme.GetComponent<Voxeme> () != null) {
			//						objAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
			//					}
			//
			//					Vector3 target = new Vector3 (theme.transform.position.x,
			//						                 Helper.GetObjectWorldSize (demoSurface).max.y,
			//						                 theme.transform.position.z);
			//					actionOptions.Add (string.Format ("put({0},{1})", theme.name,
			//						Helper.VectorToParsable (target)));
			//					confirmationTexts.Add (string.Format ("put({0},{1})", theme.name,
			//						Helper.VectorToParsable (target)), string.Format ("put the {0} block down", objAttr));
			//				}
			//			}

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
					//graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			} 
			else {
				eventManager.InsertEvent ("", 0);
				eventManager.InsertEvent (actionOptions [0], 1);
				indicatedObj = null;
				//graspedObj = null;
				actionOptions.Clear ();
				objectMatches.Clear ();
			}
		}
	}

	List<string> PopulatePushOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<string> pushOptions = new List<string> ();
		List<object> placementOptions = FindPlacementOptions (theme, dir);

		Debug.Log (string.Format ("{0} placement options", placementOptions.Count));
		foreach (object po in placementOptions) {
			if (po.GetType () == typeof(GameObject)) {
				Debug.Log ((po as GameObject));
			}
		}

		if (interactionLogic.useOrderingHeuristics) {
			List<GameObject> objectPlacements = placementOptions.OfType<GameObject> ().ToList ();

			objectPlacements = objectPlacements.OrderBy (o => (o.transform.position - theme.transform.position).magnitude).ToList ();

			for (int i = 0; i < placementOptions.Count; i++) {
				if (placementOptions [i] is GameObject) {
					placementOptions [i] = objectPlacements [i];
				}
			}
		}

		List<Region> orthogonalRegions = new List<Region> ();
		if (dir == "left") {
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
		}
		else if (dir == "right") {
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
		}
		else if (dir == "front") {
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
		}
		else if (dir == "back") {
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
		}

		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme> () != null) {
			themeAttr = theme.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
		}

		foreach (object option in placementOptions) {
			if (option is GameObject) {
				GameObject obj = (option as GameObject);
				if (theme != obj) {
					if (FitsTouching (theme, obj, directionPreds [relativeDir [dir]]) &&
						(Helper.GetObjectWorldSize (theme).min.y >= Helper.GetObjectWorldSize (obj).min.y-Constants.EPSILON)) {	// must fit in target destination and be on the same surface
						string objAttr = string.Empty;
						if (obj.GetComponent<Voxeme> () != null) {
							objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
						}

						if (certainty == CertaintyMode.Act) {
							if (!pushOptions.Contains (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name))) {
								pushOptions.Add (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name));
							}

							if (!confirmationTexts.ContainsKey (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name))) {
								confirmationTexts.Add (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name),
									string.Format ("push the {0} block {1} the {2} block", themeAttr, directionLabels [oppositeDir [relativeDir [dir]]], objAttr));
							}
						}
						else if (certainty == CertaintyMode.Suggest) {
							if (!suggestedActions.Contains (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name))) {
								suggestedActions.Add (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name));
							}

							if (!confirmationTexts.ContainsKey (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name))) {
								confirmationTexts.Add (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name),
									string.Format ("push the {0} block {1} the {2} block", themeAttr, directionLabels [oppositeDir [relativeDir [dir]]], objAttr));
							}
						}
					}
					else {
						if (!FitsTouching (theme, obj, directionPreds [relativeDir [dir]])) {
							Debug.Log (string.Format("!FitsTouching({0},{1},{2}",theme, obj, directionPreds [relativeDir [dir]]));
						}
						else if (Helper.GetObjectWorldSize (theme).min.y < Helper.GetObjectWorldSize (obj).min.y) {
							Debug.Log (string.Format("{0}.min.y < {1}.min.y",theme, obj));
						}

					}	
				}
			} 
			else if (option is Vector3) {
				Vector3 target = (Vector3)option;

				if (certainty == CertaintyMode.Act) {
					if (!pushOptions.Contains (string.Format ("slide({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						pushOptions.Add (string.Format ("slide({0},{1})", theme.name,
							Helper.VectorToParsable (target)));

						foreach (Region region in orthogonalRegions) {
							if (region.Contains (target)) {
								confirmationTexts.Add (string.Format ("slide({0},{1})", theme.name, Helper.VectorToParsable (target)),
									string.Format ("push the {0} block to the table's {1} {2} part", themeAttr, regionLabels [region], dir));
							}
						}
					}
				} 
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains (string.Format ("slide({0},{1})", theme.name,
						Helper.VectorToParsable (target)))) {
						suggestedActions.Add (string.Format ("slide({0},{1})", theme.name,
							Helper.VectorToParsable (target)));

						foreach (Region region in orthogonalRegions) {
							if (region.Contains (target)) {
								confirmationTexts.Add (string.Format ("slide({0},{1})", theme.name, Helper.VectorToParsable (target)),
									string.Format ("push the {0} block to the table's {1} {2} part", themeAttr, regionLabels [region], dir));
							}
						}
					}
				}
			}
		}

		return pushOptions;
	}

	void Push(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		if (eventConfirmation == "") {
			RespondAndUpdate("");
		}

		GameObject theme = null;
		if (indicatedObj != null) {
			theme = indicatedObj;
		}
		else if (graspedObj != null) {
			theme = graspedObj;
		}

		if (theme != null) {
			PopulatePushOptions (theme, dir);

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
				eventManager.InsertEvent (actionOptions [0], 1);
				indicatedObj = null;
				graspedObj = null;
				actionOptions.Clear ();
				objectMatches.Clear ();
			}
		}
		else {
			if (objectConfirmation == null) {
				if (eventManager.events.Count == 0) {
					RespondAndUpdate("What do you want me to push?");
					LookForward();
					if (!suggestedActions.Contains("slide({0}"+string.Format(",{0})",dir))) {
						suggestedActions.Add("slide({0}"+string.Format(",{0})",dir));
					}
				}
			}
		}
	}

	List<object> FindPlacementOptions(GameObject theme, string dir) {
		// returns objects theme can be placed relative to, or region

		// populate regions and get QSR function label
		Region thisRegion = null;
		List<Region> orthogonalRegions = new List<Region> ();
		string qsr = "";
		if (dir == "left") {
			thisRegion = (leftRegion);
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
			qsr = "Left";
		}
		else if (dir == "right") {
			thisRegion = (rightRegion);
			orthogonalRegions.Add (frontRegion);
			orthogonalRegions.Add (backRegion);
			qsr = "Right";
		}
		else if (dir == "front") {
			thisRegion = (frontRegion);
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
			qsr = "InFront";
		}
		else if (dir == "back") {
			thisRegion = (backRegion);
			orthogonalRegions.Add (leftRegion);
			orthogonalRegions.Add (rightRegion);
			qsr = "Behind";
		}
			
		//object qsrClassInstance = Activator.CreateInstance (QSR.QSR);
		List<object> placementOptions = new List<object>();
		List<GameObject> objectMatches = new List<GameObject> ();
		Bounds themeBounds = Helper.GetObjectWorldSize (theme);
		foreach (Region region in orthogonalRegions) {
			Debug.Log (string.Format ("{0}:{1}", region.center, region.Contains (theme)));
			if (region.Contains(theme)) {
				Debug.Log (string.Format ("{0} contains {1}", region, theme));
				foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
					if (block.activeInHierarchy) {
						if (block != theme) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
							if ((bool)(Type.GetType ("QSR.QSR").GetMethod (qsr).Invoke (null, new object[] {
								Helper.GetObjectWorldSize (block),
								themeBounds
							})) &&	// if it's to the left of the grasped block
								(region.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
								if (!objectMatches.Contains (block)) {
									objectMatches.Add (block);
								}
							}
							else {
								if (!region.Contains (block)) {
									Debug.Log (string.Format ("{0} not in region {1}", block.name, region));
								}
								else {
									Debug.Log (string.Format ("{0} not {1} of {2}", block.name, qsr, theme.name));
								}
							}
						}
						else {
							Debug.Log (string.Format ("{0} == theme", block.name));
						}
					}
					else {
						Debug.Log (string.Format ("{0} inactive", block.name));
					}
				}
			}
		}

		Vector3 target = Vector3.zero;

		foreach (GameObject obj in objectMatches) {
			target = obj.transform.position;
			placementOptions.Add (obj);
		}

		// not moving on top of another object
		foreach (Region region in orthogonalRegions) {
			if (region.Contains (theme)) {	// stay in this region
				target = Helper.FindClearRegion (demoSurface, new Region[]{ thisRegion, region }, theme).center;
				placementOptions.Add (target);
			}
		}

		return placementOptions;
	}

	void LookAt(GameObject obj) {
		Vector3 target = new Vector3 (obj.transform.position.x/2.0f,
			(obj.transform.position.y+headTargetDefault.y)/2.0f, obj.transform.position.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = obj.transform.position;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 1.0f;

		logger.OnLogEvent(this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				string.Format("look_at({0})",obj.name))));
	}

	void LookAt(Vector3 point) {
		Vector3 target = new Vector3 (point.x/2.0f, (point.y+headTargetDefault.y)/2.0f, point.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = target;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 1.0f;

		logger.OnLogEvent(this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				string.Format("look_at({0})",Helper.VectorToParsable(point)))));
	}

	void PointAt(Vector3 point, GameObject hand) {
		Vector3 target = new Vector3 (point.x, point.y+0.2f, point.z);
		AvatarGesture performGesture = null;

		MoveToPerform ();

		if (hand == leftGrasper) {
			ikControl.leftHandObj.position = target;
			InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			performGesture = AvatarGesture.LARM_POINT_FRONT;
		}
		else if (hand == rightGrasper) {
			ikControl.rightHandObj.position = target;
			InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
			performGesture = AvatarGesture.RARM_POINT_FRONT;
		}

		gestureController.PerformGesture (performGesture);
		logger.OnLogEvent(this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				string.Format("point({0},{1})",hand.name,Helper.VectorToParsable(point)))));
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

	Vector3 TransformToSurface(List<float> vector) {
		Vector3 coord = Vector3.zero;
		receivedPointingCoord = new Vector2 (vector [0], vector [1]);

		if (transformToScreenPointing) {
			screenPoint = new Vector3 (
				((Screen.width * vector [0] * vectorScaleFactor.x) / tableSize.x) + (Screen.width / 2.0f),
				((Screen.height * vector [1] / (kinectToSurfaceHeight*vectorScaleFactor.y)) + (Screen.height / 2.0f)),
				0.0f);

			Ray ray = Camera.main.ScreenPointToRay (screenPoint);
			RaycastHit hit;
			// Casts the ray and get the first game object hit
			if (demoSurfaceCollider.Raycast (ray, out hit, 10.0f)) {
//				if (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject) == demoSurface) {
					coord = new Vector3(hit.point.x,
						Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
						hit.point.z);
//				}
			}

//			float zCoord = vector[1];
//
//			// point at base of Kinect -> 0.0 -> .8 (my edge)
//			// point at far edge of virtual table -> -1.6 -> -.8 (Diana's edge)
//			zCoord = (vector[1] * vectorScaleFactor.y) + (tableSize.y / 2.0f);
//			coord = new Vector3 (-vector[0]*vectorScaleFactor.x,
//				Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
//				zCoord);
		}
		else {
			float zCoord = vector[1];

			// point at base of Kinect -> 0.0 -> -.8 (Diana's edge)
			// point down in front of me -> 1.6 -> .8 (my edge)
			zCoord = (vector[1] - (tableSize.y / 2.0f)) * vectorScaleFactor.y;
			coord = new Vector3 (-vector[0]*vectorScaleFactor.x,
				Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON,
				zCoord);
		}

		return coord;
	}

	bool SurfaceClear(GameObject block) {
		Debug.Log (block);
		bool surfaceClear = true;
		List<GameObject> excludeChildren = block.GetComponentsInChildren<Renderer>().Where(
			o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != block) || 
			(o.gameObject.layer == LayerMask.NameToLayer("blocks-known"))).Select(o => o.gameObject).ToList();
		foreach (GameObject go in excludeChildren) {
			Debug.Log (go);
		}
		Bounds blockBounds = Helper.GetObjectWorldSize (block, excludeChildren);
		Debug.Log (blockBounds);
		Debug.Log (Helper.GetObjectWorldSize (block).max.y);
		Debug.Log (Helper.GetObjectWorldSize (block,excludeChildren).max.y);
		Debug.Log (blockBounds.max.y);
		foreach (GameObject otherBlock in blocks) {
			excludeChildren = otherBlock.GetComponentsInChildren<Renderer>().Where(
				o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != otherBlock) || 
				(o.gameObject.layer == LayerMask.NameToLayer("blocks-known"))).Select(o => o.gameObject).ToList();
			foreach (GameObject go in excludeChildren) {
				Debug.Log (go);
			}
			Bounds otherBounds = Helper.GetObjectWorldSize (otherBlock,excludeChildren);
			Debug.Log (otherBlock);
			Debug.Log (otherBounds);
			Debug.Log (Helper.GetObjectWorldSize (otherBlock).min.y);
			Debug.Log (Helper.GetObjectWorldSize (otherBlock,excludeChildren).min.y);
			Debug.Log (otherBounds.min.y);
			Region blockMax = new Region (new Vector3 (blockBounds.min.x, blockBounds.max.y, blockBounds.min.z),
				new Vector3 (blockBounds.max.x, blockBounds.max.y, blockBounds.max.z));
			Region otherMin = new Region (new Vector3 (otherBounds.min.x, blockBounds.max.y, otherBounds.min.z),
				new Vector3 (otherBounds.max.x, blockBounds.max.y, otherBounds.max.z));
//			if ((QSR.QSR.Above (otherBounds, blockBounds)) && (!QSR.QSR.Left (otherBounds, blockBounds)) &&
//				(!QSR.QSR.Right (otherBounds, blockBounds)) && (RCC8.EC (otherBounds, blockBounds))) {
			Debug.Log(Helper.RegionToString(blockMax));
			Debug.Log(Helper.RegionToString(otherMin));
			Debug.Log(Helper.RegionToString(Helper.RegionOfIntersection(blockMax,otherMin,MajorAxes.MajorAxis.Y)));
			Debug.Log(QSR.QSR.Above (otherBounds, blockBounds));
			Debug.Log(((Helper.RegionOfIntersection(blockMax,otherMin,MajorAxes.MajorAxis.Y).Area()/blockMax.Area())));
			Debug.Log(RCC8.EC (otherBounds, blockBounds));
			if ((QSR.QSR.Above (otherBounds, blockBounds)) && 
				((Helper.RegionOfIntersection(blockMax,otherMin,MajorAxes.MajorAxis.Y).Area()/blockMax.Area()) > 0.25f) &&
				(RCC8.EC (otherBounds, blockBounds))) {
				surfaceClear = false;
				break;
			}
		}

		Debug.Log (surfaceClear);
		return surfaceClear;
	}

	bool FitsTouching (GameObject theme, GameObject obj, string dir) { 
		bool fits = true;

		Bounds themeBounds = Helper.GetObjectWorldSize (theme);
		Bounds objBounds = Helper.GetObjectWorldSize (obj);

		foreach (GameObject test in blocks) {
			if ((test != theme) && (test != obj)) {
				if (dir == "left") {
					Bounds projectedBounds = new Bounds (
						new Vector3 (objBounds.min.x - themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
						themeBounds.size);
					if (!RCC.RCC8.DC(projectedBounds, Helper.GetObjectWorldSize (test)) && 
						!RCC.RCC8.EC(projectedBounds, Helper.GetObjectWorldSize (test))) {
						fits = false;
					}
				}
				else if (dir == "right") {
					Bounds projectedBounds = new Bounds (
						new Vector3 (objBounds.max.x + themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
						themeBounds.size);
					if (!RCC.RCC8.DC(projectedBounds, Helper.GetObjectWorldSize (test)) && 
						!RCC.RCC8.EC(projectedBounds, Helper.GetObjectWorldSize (test))) {
						fits = false;
					}
				}
				else if (dir == "in_front") {
					Bounds projectedBounds = new Bounds (
						new Vector3 (objBounds.center.x, objBounds.center.y, objBounds.min.z - themeBounds.extents.z),
						themeBounds.size);
					if (!RCC.RCC8.DC(projectedBounds, Helper.GetObjectWorldSize (test)) && 
						!RCC.RCC8.EC(projectedBounds, Helper.GetObjectWorldSize (test))) {
						fits = false;
					}
				}
				else if (dir == "behind") {
					Bounds projectedBounds = new Bounds (
						new Vector3 (objBounds.center.x, objBounds.center.y, objBounds.max.z + themeBounds.extents.z),
						themeBounds.size);
					if (!RCC.RCC8.DC(projectedBounds, Helper.GetObjectWorldSize (test)) && 
						!RCC.RCC8.EC(projectedBounds, Helper.GetObjectWorldSize (test))) {
						fits = false;
					}
				}
			}
		}

		return fits;
	}

	public void ReachFor(Vector3 coord) {
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
		//Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		//Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;

		if ((interactionLogic != null) && (interactionLogic.enabled) && (interactionLogic.GraspedObj != null)) { // grasping something
			if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == leftGrasper) {
				ikControl.rightHandObj.transform.position = coord + offset;
				InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
			}
			else if (InteractionHelper.GetCloserHand (Diana, interactionLogic.GraspedObj) == rightGrasper) {
				ikControl.leftHandObj.transform.position = coord + offset;
				InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			}
		}
		else {
			// which region is coord in?
			if (leftRegion.Contains (new Vector3 (coord.x,
				leftRegion.center.y, coord.z))) {
				ikControl.rightHandObj.transform.position = coord + offset;
				InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);

			}
			else if (rightRegion.Contains (new Vector3 (coord.x,
				rightRegion.center.y, coord.z))) {
				ikControl.leftHandObj.transform.position = coord + offset;
				InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			}
		}

		logger.OnLogEvent(this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				string.Format("reach({0})",Helper.VectorToParsable(coord)))));

		LookForward ();
	}

	public void ReachFor(GameObject obj) {
		Bounds bounds = Helper.GetObjectWorldSize(obj);
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
		//		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		//		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;

		PhysicsHelper.ResolveAllPhysicsDiscrepancies (false);

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

		//LookAt (obj);

		logger.OnLogEvent(this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				string.Format("reach({0})",obj.name))));
	}

	public void StorePose() {
		//		Debug.Log (string.Format("Storing pose {0} {1} {2}",
		//			ikControl.leftHandObj.transform.position,ikControl.rightHandObj.transform.position,ikControl.lookObj.transform.position));
		bool leftGrasping = false;
		bool rightGrasping = false;

		if (graspedObj != null) {
			if (InteractionHelper.GetCloserHand (Diana, graspedObj) == leftGrasper) {
				leftGrasping = true;
			}
			else if (InteractionHelper.GetCloserHand (Diana, graspedObj) == rightGrasper) {
				rightGrasping = true;
			}
		}

		if (!leftGrasping) {
			leftTargetStored = ikControl.leftHandObj.transform.position;
		}
		else {
			leftTargetStored = new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue);
		}

		if (!rightGrasping) {
			rightTargetStored = ikControl.rightHandObj.transform.position;
		}
		else {
			rightTargetStored = new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue);
		}

		headTargetStored = ikControl.lookObj.transform.position;
	}

	public void ReturnToPose() {
		bool animPlaying = false;
		for (int i = 0; i < Diana.GetComponent<Animator> ().layerCount; i++) {
			if (Diana.GetComponent<Animator> ().GetCurrentAnimatorClipInfo(i)[0].clip != null) {
				animPlaying = true;
			}
		}

		//		if (Diana.GetComponent<Animator> ().GetCurrentAnimatorClipInfo() != null) {
		//			animPlaying = true;
		//		}

		if (!animPlaying) {
			if (leftTargetStored != new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue)) {
				ikControl.leftHandObj.transform.position = leftTargetStored;
				InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			}

			if (rightTargetStored != new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue)) {
				ikControl.rightHandObj.transform.position = rightTargetStored;
				InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
			}

			ikControl.lookObj.transform.position = headTargetStored;
			InteractionHelper.SetHeadTarget (Diana, ikControl.lookObj);
			//		Debug.Log (string.Format("Returning to pose {0} {1} {2}",
			//			ikControl.leftHandObj.transform.position,ikControl.rightHandObj.transform.position,ikControl.lookObj.transform.position));
		}
	}

	double EpistemicCertainty (Concept concept) {
		double certainty = concept.Certainty;

		foreach (Concept related in concept.Related) {
			if (related.Certainty > certainty) {
				certainty = related.Certainty;
			}
		}

		return certainty;
	}

	public void RespondAndUpdate(string utterance) {
		if (OutputHelper.GetCurrentOutputString(Role.Affector) != utterance) {
		logger.OnLogEvent (this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AS",
				string.Format("\"{0}\"",utterance))));
		}

		OutputHelper.PrintOutput (Role.Affector, utterance);

		// get all linguistic concepts
		if ((!UseTeaching) || (!interactionLogic.useEpistemicModel)) {
			return;
		}

		List<Concepts> conceptsList = epistemicModel.state.GetAllConcepts();
		foreach (Concepts concepts in conceptsList) {
			if (concepts.GetConcepts ().ContainsKey (ConceptMode.L)) {
				List<Concept> linguisticConcepts = concepts.GetConcepts () [ConceptMode.L];

				List<Concept> conceptsToUpdate = new List<Concept> ();
				List<Relation> relationsToUpdate = new List<Relation> ();
				// if mentioned, introduce if not used already
//				Debug.Log (utterance.ToLower ());
				foreach (Concept concept in linguisticConcepts) {
//					Debug.Log (concept.Name.ToLower ());
					if (utterance.ToLower ().Contains (concept.Name.ToLower ())) {
//						Debug.Log (string.Format("{0} certainty: {1}",concept.Name.ToLower (),concept.Certainty));
						concept.Certainty = concept.Certainty < 0.5 && concept.Certainty >= 0.0 ? 0.5 : concept.Certainty;
//						Debug.Log (string.Format("{0} certainty: {1}",concept.Name.ToLower (),concept.Certainty));
						conceptsToUpdate.Add (concept);

						foreach (Concept relatedConcept in epistemicModel.state.GetRelated(concept)) {
							Relation relation = epistemicModel.state.GetRelation (concept, relatedConcept);
							double prevCertainty = relation.Certainty;
							double newCertainty = Math.Min (concept.Certainty, relatedConcept.Certainty);
							if (Math.Abs (prevCertainty - newCertainty) > 0.01) {
								relation.Certainty = newCertainty;
								relationsToUpdate.Add (relation);
							}
						}
					}
				}

				if (conceptsToUpdate.Count + relationsToUpdate.Count > 0) {
					epistemicModel.state.UpdateEpisim (conceptsToUpdate.ToArray (), relationsToUpdate.ToArray ());
				}
			}
		}
	}

	void PromptEvent(string eventStr) {
		eventManager.InsertEvent ("", 0);
		eventManager.InsertEvent (eventStr, 1);

		logger.OnLogEvent (this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AA",
				eventStr)));
	}

	void PerformAndLogGesture(AvatarGesture gesture) {
		gestureController.PerformGesture (gesture);

		logger.OnLogEvent (this, new LoggerArgs (
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AG",
				gesture.Name)));
	}

	void ReturnToRest(object sender, EventArgs e) {
		if (((EventManagerArgs)e).EventString != string.Empty) {
			Debug.Log (string.Format ("Completed event: {0}", ((EventManagerArgs)e).EventString));
			if (!interactionSystem.IsPaused (FullBodyBipedEffector.LeftHand) &&
				!interactionSystem.IsPaused (FullBodyBipedEffector.RightHand)) {
				TurnForward ();
				LookForward ();

				if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
					interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
				}
			}
			else {
				if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
					if ((interactionLogic.ActionOptions.Count > 0) &&
					   (Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift"))) {
						interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
					}
				}
			}

	//		Debug.Log (interactionSystem.IsPaused (FullBodyBipedEffector.LeftHand));
	//		Debug.Log (interactionSystem.IsPaused (FullBodyBipedEffector.RightHand));
		}
	}

	void ConnectionLost(object sender, EventArgs e) {
		LookForward();

		if (sessionCounter >= 1) {
			if (eventManager.events.Count == 0) {
				RespondAndUpdate("Hey, where'd you go?");
			}
		}
		else {
			if (eventManager.events.Count == 0) {
				RespondAndUpdate("Anyone there?");
			}
		}
	}

	void DisableHighlight(object sender, ElapsedEventArgs e) {
		highlightTimeoutTimer.Enabled = false;
		highlightTimeoutTimer.Interval = highlightTimeoutTime;

		disableHighlight = true;
	}

	void OnDestroy() {
		logger.CloseLog ();
	}

	void OnApplicationQuit() {
		logger.CloseLog ();
	}
}
