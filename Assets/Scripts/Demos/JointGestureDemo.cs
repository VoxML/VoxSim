using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

using MajorAxes;
using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Agent.SyntheticVision;
using VoxSimPlatform.Core;
using VoxSimPlatform.Episteme;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;
using VoxSimPlatform.Logging;
using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.SpatialReasoning.QSR;
using VoxSimPlatform.Vox;

public class SelectionEventArgs : EventArgs {
	public object Content;

	public SelectionEventArgs(object content) {
		Content = content;
	}
}

public class JointGestureDemo : SingleAgentInteraction {
	FusionSocket fusionSocket;
    KSIMSocket ksimSocket;
    NLURestClient nluRestClient;
    ADESocket adeSocket;
    EventManager eventManager;
	ObjectSelector objSelector;
	CommunicationsBridge commBridge;
	RelationTracker relationTracker;
	Predicates predicates;

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

	bool setLeftHandTarget = false;
	bool setRightHandTarget = false;
	bool setHeadTarget = false;

	Vector3 leftTargetDefault, leftTargetStored;
	Vector3 rightTargetDefault, rightTargetStored;
	Vector3 headTargetDefault, headTargetStored;

	public bool callUserByName = false;

	public InteractionPrefsModalWindow interactionPrefs;
	public AvatarGestureController gestureController;
	public VisualMemory dianaMemory;


    public GameObject demoSurface;
	public BoxCollider demoSurfaceCollider;
	public List<GameObject> availableObjs;
	public GameObject indicatedObj = null;
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
	public Vector2 knownScreenSize = new Vector2(.9146f, .53f); //m
	public Vector2 windowScaleFactor;
	public float kinectToSurfaceHeight = .63f; //m

	public bool transformToScreenPointing = false; // false = assume table in demo space and use its coords to mirror table coords

	public Vector2 receivedPointingCoord = Vector2.zero;
	public Vector2 receivedPointingVariance = Vector2.zero;
	public Vector2 screenPoint = Vector2.zero;
	public Vector3 varianceScaleFactor = Vector2.zero;
	public Vector2 leftArmScreenPointingBias = Vector2.zero;
	public Vector2 rightArmScreenPointingBias = Vector2.zero;

	public bool allowDeixisByClick = false;

	public float servoSpeed = .05f;

	public Region leftRegion;
	public Region rightRegion;
	public Region frontRegion;
	public Region backRegion;

	Timer gestureResumeTimer;
	public double gestureResumeTime;
	bool gestureResume = false;

	GenericLogger logger;
	int logIndex;

	bool logActionsOnly;
	bool logFullState;
	bool useTimestamps;

	List<Pair<string, string>> receivedMessages = new List<Pair<string, string>>();

	Dictionary<Region, string> regionLabels = new Dictionary<Region, string>();
	Dictionary<string, string> directionPreds = new Dictionary<string, string>();
	Dictionary<string, string> directionLabels = new Dictionary<string, string>();
	Dictionary<string, Vector3> directionVectors = new Dictionary<string, Vector3>();
	Dictionary<string, string> oppositeDir = new Dictionary<string, string>();
	Dictionary<string, string> relativeDir = new Dictionary<string, string>();

	GameObject regionHighlight;
	GameObject radiusHighlight;

	GameObject leftRegionHighlight;
	GameObject rightRegionHighlight;
	GameObject frontRegionHighlight;
	GameObject backRegionHighlight;

	List<string> knownDysfluencies = new List<string>(new string[] {
		"uh",
		//"uhm", 
		//"um", 
		//"em",
		//"ah", 
		//"y",
		//"oh", 
		"hmm"
	});

	List<string> knownPreables = new List<string>(new string[] {
		"now", // connective, not preamble
		"then",
		"and",
		"so",
		"diana", // begin actual preambles
		"could you",
		"would you",
		"can you",
		"tell me",
		"could_you",
		"would_you",
		"can_you",
		"tell_me"
	});

	public List<string> actionOptions = new List<string>();
	public string eventConfirmation = "";

	public List<string> suggestedActions = new List<string>();

	public List<GameObject> objectMatches = new List<GameObject>();
	public GameObject objectConfirmation = null;

	public bool useOrderingHeuristics;

	//public bool useSpeechGrammar;
	//public bool useServos;

	Dictionary<string, string> confirmationTexts = new Dictionary<string, string>();

	int sessionCounter = 0;

	public event EventHandler ObjectSelected;

	public void OnObjectSelected(object sender, EventArgs e) {
		if (ObjectSelected != null) {
			ObjectSelected(this, e);
		}
	}

	public event EventHandler PointSelected;

	public void OnPointSelected(object sender, EventArgs e) {
		if (PointSelected != null) {
			PointSelected(this, e);
		}
	}

	// Use this for initialization
	void Start() {
		windowScaleFactor.x = (float) Screen.width / (float) Screen.currentResolution.width;
		windowScaleFactor.y = (float) Screen.height / (float) Screen.currentResolution.height;

		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();

		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
		eventManager.EventComplete += ReturnToRest;
		eventManager.QueueEmpty += CompletedEventSequence;
		eventManager.EntityReferenced += ReferentIndicated;
		eventManager.NonexistentEntityError += NonexistentReferent;
		eventManager.DisambiguationError += Disambiguate;
        // Could set active agent here, but we'll pass it in directly after we set a Diana variable.
        //eventManager.setActiveAgent("Diana");

		relationTracker = GameObject.Find("BehaviorController").GetComponent<RelationTracker>();

		predicates = GameObject.Find("BehaviorController").GetComponent<Predicates>();

		interactionPrefs = gameObject.GetComponent<InteractionPrefsModalWindow>();

		logger = GetComponent<GenericLogger>();

		if (PlayerPrefs.GetInt("Make Logs") == 1) {
			logger.OpenLog(PlayerPrefs.GetString("Logs Prefix"));
		}

		logActionsOnly = (PlayerPrefs.GetInt("Actions Only Logs") == 1);
		logFullState = (PlayerPrefs.GetInt("Full State Info") == 1);

		logIndex = 0;

		Diana = GameObject.Find("Diana");
        eventManager.SetActiveAgent(Diana); // This would need called again any time we switch who we talk to
        UseTeaching = interactionPrefs.useTeachingAgent;
		epistemicModel = Diana.GetComponent<EpistemicModel>();
		interactionLogic = Diana.GetComponent<DianaInteractionLogic>();
		interactionLogic.AttentionShift += AttentionShift;

        if (GameObject.Find("DianaMemory") != null) {
			dianaMemory = GameObject.Find("DianaMemory").GetComponent<VisualMemory>();
		}

		fusionSocket = (FusionSocket)commBridge.GetComponent<CommunicationsBridge>().FindSocketConnectionByLabel("Fusion");
		//TODO: What if there is no CSUClient address assigned?
		if (fusionSocket != null) {
			fusionSocket.ConnectionMade += ConnectionMade;
			fusionSocket.FusionReceived += ReceivedFusion;
			fusionSocket.ConnectionLost += ConnectionLost;
		}

        ksimSocket = (KSIMSocket)commBridge.GetComponent<CommunicationsBridge>().FindSocketConnectionByLabel("KSIM");

        adeSocket = (ADESocket)commBridge.GetComponent<CommunicationsBridge>().FindSocketConnectionByLabel("ADE");

        // set up the parser we want to use in this scene
        nluRestClient = (NLURestClient)commBridge.GetComponent<CommunicationsBridge>().FindRestClientByLabel("NLTK");

        leftGrasper = Diana.GetComponent<FullBodyBipedIK>().references.leftHand.gameObject;
		rightGrasper = Diana.GetComponent<FullBodyBipedIK>().references.rightHand.gameObject;
		gestureController = Diana.GetComponent<AvatarGestureController>();
		ik = Diana.GetComponent<FullBodyBipedIK>();
		interactionSystem = Diana.GetComponent<InteractionSystem>();
		ikControl = Diana.GetComponent<IKControl>();

		InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);

		// store default positions at start
		leftTargetDefault = ikControl.leftHandObj.transform.position;
		rightTargetDefault = ikControl.rightHandObj.transform.position;
		headTargetDefault = ikControl.lookObj.transform.position;

		// store default positions at start
		leftTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		rightTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		headTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

		regionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
		regionHighlight.name = "Highlight";
		regionHighlight.transform.position = Vector3.zero;
		regionHighlight.transform.localScale =
			new Vector3(vectorConeRadius * .2f, vectorConeRadius * .2f, vectorConeRadius * .2f);
		regionHighlight.tag = "UnPhysic";
		regionHighlight.GetComponent<Renderer>().material = activeHighlightMaterial;
		regionHighlight.gameObject.layer = 5;

        //regionHighlight.GetComponent<Renderer> ().material.SetColor("_Color",new Color(1.0f,1.0f,1.0f,0.5f));
        //		regionHighlight.GetComponent<Renderer> ().enabled = false;
        Destroy(regionHighlight.GetComponent<Collider>());

		highlightTimeoutTimer = new Timer(highlightTimeoutTime);
		highlightTimeoutTimer.Enabled = false;
		highlightTimeoutTimer.Elapsed += DisableHighlight;

		gestureResumeTimer = new Timer(gestureResumeTime);
		gestureResumeTimer.Enabled = false;
		gestureResumeTimer.Elapsed += GestureResume;

		relativeDir.Add("left", "left");
		relativeDir.Add("right", "right");
		relativeDir.Add("front", "back");
		relativeDir.Add("back", "front");
		relativeDir.Add("up", "up");
		relativeDir.Add("down", "down");

		oppositeDir.Add("left", "right");
		oppositeDir.Add("right", "left");
		oppositeDir.Add("front", "back");
		oppositeDir.Add("back", "front");
		oppositeDir.Add("up", "down");
		oppositeDir.Add("down", "up");

		directionPreds.Add("left", "left");
		directionPreds.Add("right", "right");
		directionPreds.Add("front", "in_front");
		directionPreds.Add("back", "behind");

		directionLabels.Add("left", "left of");
		directionLabels.Add("right", "right of");
		directionLabels.Add("front", "in front of");
		directionLabels.Add("back", "behind");

		// TODO: read in from VoxML
		directionVectors.Add("left", Vector3.left);
		directionVectors.Add("right", Vector3.right);
		directionVectors.Add("front", Vector3.forward);
		directionVectors.Add("back", Vector3.back);
		directionVectors.Add("up", Vector3.up);
		directionVectors.Add("down", Vector3.down);
	}



	// Update is called once per frame
	void Update() {
        if ((nluRestClient != null) && (nluRestClient.isConnected)) {
            if ((commBridge.parser == null) || (commBridge.parser.GetType() != typeof(PythonJSONParser))) {
                // if this client is not connected,
                //  we should back off to the default parser,
                //  which is initialized in commBridge by, well, default
                commBridge.parser = new PythonJSONParser();
                commBridge.parser.InitParserService(nluRestClient, typeof(NLTKSyntax));
                nluRestClient.PostOkay += LookForNewParse;
            }
        }

        if (demoSurface != Helper.GetMostImmediateParentVoxeme(demoSurface)) {
			demoSurface = Helper.GetMostImmediateParentVoxeme(demoSurface);

			for (int i = 0; i < availableObjs.Count; i++) {
				availableObjs[i] = Helper.GetMostImmediateParentVoxeme(availableObjs[i]);
			}
		}

		if (leftRegion == null) {
			leftRegion = new Region(new Vector3(Helper.GetObjectWorldSize(demoSurface).center.x,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z + Constants.EPSILON),
				new Vector3(Helper.GetObjectWorldSize(demoSurface).max.x - Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z - Constants.EPSILON));
			Debug.Log(string.Format("{0}: {1},{2},{3}", leftRegion, leftRegion.center, leftRegion.min, leftRegion.max));
			leftRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			leftRegionHighlight.name = "LeftRegionHighlight";
			leftRegionHighlight.transform.position = leftRegion.center;
			leftRegionHighlight.transform.localScale = new Vector3(.1f * (leftRegion.max.x - leftRegion.min.x),
				1.0f, .1f * (leftRegion.max.z - leftRegion.min.z));
			leftRegionHighlight.SetActive(false);

			regionLabels.Add(leftRegion, "left");
		}

		if (rightRegion == null) {
			rightRegion = new Region(new Vector3(Helper.GetObjectWorldSize(demoSurface).min.x + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z + Constants.EPSILON),
				new Vector3(Helper.GetObjectWorldSize(demoSurface).center.x,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z - Constants.EPSILON));
			Debug.Log(string.Format("{0}: {1},{2},{3}", rightRegion, rightRegion.center, rightRegion.min,
				rightRegion.max));
			rightRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			rightRegionHighlight.name = "RightRegionHighlight";
			rightRegionHighlight.transform.position = rightRegion.center;
			rightRegionHighlight.transform.localScale = new Vector3(.1f * (rightRegion.max.x - rightRegion.min.x),
				1.0f, .1f * (rightRegion.max.z - rightRegion.min.z));
			rightRegionHighlight.SetActive(false);

			regionLabels.Add(rightRegion, "right");
		}

		if (frontRegion == null) {
			frontRegion = new Region(new Vector3(Helper.GetObjectWorldSize(demoSurface).min.x + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).min.z + Constants.EPSILON),
				new Vector3(Helper.GetObjectWorldSize(demoSurface).max.x + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).center.z));
			frontRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			frontRegionHighlight.name = "FrontRegionHighlight";
			frontRegionHighlight.transform.position = frontRegion.center;
			frontRegionHighlight.transform.localScale = new Vector3(.1f * (frontRegion.max.x - frontRegion.min.x),
				1.0f, .1f * (frontRegion.max.z - frontRegion.min.z));
			frontRegionHighlight.SetActive(false);

			regionLabels.Add(frontRegion, "front");
		}

		if (backRegion == null) {
			backRegion = new Region(new Vector3(Helper.GetObjectWorldSize(demoSurface).min.x + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).center.z),
				new Vector3(Helper.GetObjectWorldSize(demoSurface).max.x + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
					Helper.GetObjectWorldSize(demoSurface).max.z - Constants.EPSILON));
			backRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			backRegionHighlight.name = "BackRegionHighlight";
			backRegionHighlight.transform.position = backRegion.center;
			backRegionHighlight.transform.localScale = new Vector3(.1f * (backRegion.max.x - backRegion.min.x),
				1.0f, .1f * (backRegion.max.z - backRegion.min.z));
			backRegionHighlight.SetActive(false);

			regionLabels.Add(backRegion, "back");
		}

		UseTeaching = interactionPrefs.useTeachingAgent;
		transformToScreenPointing = (interactionPrefs.deixisMethod == InteractionPrefsModalWindow.DeixisMethod.Screen);

		// Vector pointing scaling
		if (transformToScreenPointing) {
			vectorScaleFactor.x = (float) DEFAULT_SCREEN_WIDTH / (knownScreenSize.x * windowScaleFactor.x);
			vectorScaleFactor.y = (float) DEFAULT_SCREEN_HEIGHT / (knownScreenSize.y * windowScaleFactor.y);

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
			vectorScaleFactor.x = tableSize.x / (float) DEFAULT_SCREEN_WIDTH;
		}

		if (disableHighlight) {
			regionHighlight.transform.position = Vector3.zero;
			regionHighlight.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f,
				(1.0f / ((regionHighlight.transform.position -
				          new Vector3(demoSurface.transform.position.x, Helper.GetObjectWorldSize(demoSurface).max.y,
					          demoSurface.transform.position.z)).magnitude + Constants.EPSILON)) *
				regionHighlight.transform.position.y);
//			Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.GetColor ("_Color").a);
			disableHighlight = false;
		}

//		Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.GetColor ("_Color").a);

		if ((regionHighlight.GetComponent<Renderer>().material.color.a > 0.0f) &&
		    (regionHighlight.transform.position.y > 0.0f)) {
			regionHighlight.transform.eulerAngles = new Vector3(regionHighlight.transform.eulerAngles.x,
				regionHighlight.transform.eulerAngles.y + Time.deltaTime * highlightTurnSpeed,
				regionHighlight.transform.eulerAngles.z);

			if (highlightOscillateDirection == 1) {
				// grow
				regionHighlight.transform.localScale = new Vector3(
					regionHighlight.transform.localScale.x + Time.deltaTime * highlightOscSpeed,
					regionHighlight.transform.localScale.y,
					regionHighlight.transform.localScale.z + Time.deltaTime * highlightOscSpeed);

				if (regionHighlight.transform.localScale.x >= (vectorConeRadius * .2f) * highlightOscUpper) {
					highlightOscillateDirection *= -1;
				}
			}
			else if (highlightOscillateDirection == -1) {
				// shrink
				regionHighlight.transform.localScale = new Vector3(
					regionHighlight.transform.localScale.x - Time.deltaTime * highlightOscSpeed,
					regionHighlight.transform.localScale.y,
					regionHighlight.transform.localScale.z - Time.deltaTime * highlightOscSpeed);

				if (regionHighlight.transform.localScale.x <= (vectorConeRadius * .2f) * highlightOscLower) {
					highlightOscillateDirection *= -1;
				}
			}
		}

		if ((UseTeaching) && (interactionLogic.useEpistemicModel)) {
			//Concept putL = epistemicModel.state.GetConcept ("PUT", ConceptType.ACTION, ConceptMode.L);
			Concept putG = epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G);
			//Concept pushL = epistemicModel.state.GetConcept ("PUSH", ConceptType.ACTION, ConceptMode.L);
			Concept pushG = epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G);
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
			if (Input.GetMouseButtonDown(0)) {
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				RaycastHit hit;
				// Casts the ray and get the first game object hit
				Physics.Raycast(ray, out hit);

				if (hit.collider != null) {
					if (availableObjs.Contains(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject))) {
						if (!epistemicModel.engaged) {
							epistemicModel.engaged = true;
						}

						if (dianaMemory != null && dianaMemory.enabled) {
							Debug.Log(string.Format("Does Agent know {0}:{1}", hit.collider.gameObject,
								dianaMemory.IsKnown(hit.collider.gameObject.GetComponent<Voxeme>())));
							if (dianaMemory.IsKnown(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject)
								.GetComponent<Voxeme>())) {
								//Deixis (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject));
								OnObjectSelected(this,
									new SelectionEventArgs(
										Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject)));
							}
						}
					}
					else if (Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject) == demoSurface) {
						if (!epistemicModel.engaged) {
							epistemicModel.engaged = true;
						}

						OnPointSelected(this, new SelectionEventArgs(hit.point));
					}
				}
			}
		}

		if (gestureResume) {
			gestureController.OnGestureResume.Invoke();
			gestureResume = false;
		}
	}

	void ReceivedFusion(object sender, EventArgs e) {
		string fusionMessage = ((FusionEventArgs) e).Content;
		//Debug.Log (fusionMessage);

		string[] splitMessage = ((FusionEventArgs) e).Content.Split(';');
		string messageType = splitMessage[0];
		string messageStr = splitMessage[1];
		string messageTime = splitMessage[2];

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					string.Format("{0}{1}", "H", messageType),
					messageStr)));
		}

		receivedMessages.Add(new Pair<string, string>(messageTime, messageStr));

		if (interactionLogic.attentionStatus != CharacterLogicAutomaton.AttentionStatus.Attentive) {
			if (messageType == "S") {
				// ignore speech on inattention
				return;
			}
		}

		OnCharacterLogicInput(this, new CharacterLogicEventArgs(
			string.Format("{0} {1}", messageType, messageStr.Split(',')[0]),
			string.Format("{0} {1}", messageType, messageStr)));

		if (!epistemicModel.engaged) {
			epistemicModel.engaged = true;
		}

		Concept conceptL = null;
		Concept conceptG = null;
		Relation relation = null;

		if (messageType == "P") {
			// continuous pointing message
			if ((interactionLogic.CurrentState.Name != "Wait") &&
			    (interactionLogic.CurrentState.Name != "TrackPointing")) {
				regionHighlight.GetComponent<Renderer>().material = inactiveHighlightMaterial;
			}

			highlightTimeoutTimer.Interval = highlightTimeoutTime;
			highlightTimeoutTimer.Enabled = true;

			if (messageStr.StartsWith("l")) {
				if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
					Vector3 offset = MoveHighlight(
						TransformToSurface(GetGestureVector(messageStr, "l"), leftArmScreenPointingBias),
						GetVectorVariance(GetGestureVector(messageStr, "l")));

					if (offset.sqrMagnitude <= Constants.EPSILON) {
						regionHighlight.transform.position = TransformToSurface(GetGestureVector(messageStr, "l"),
							leftArmScreenPointingBias);
					}
				}
			}
			else if (messageStr.StartsWith("r")) {
				if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
					Vector3 offset = MoveHighlight(
						TransformToSurface(GetGestureVector(messageStr, "r"), rightArmScreenPointingBias),
						GetVectorVariance(GetGestureVector(messageStr, "r")));

					if (offset.sqrMagnitude <= Constants.EPSILON) {
						regionHighlight.transform.position = TransformToSurface(GetGestureVector(messageStr, "r"),
							rightArmScreenPointingBias);
					}
				}
			}
		}
	}

	string GetSpeechString(string receivedData, string constituentTag) {
		//		Debug.Log (receivedData);
		//		Debug.Log (gestureCode);
		List<string> content = receivedData.Replace(constituentTag, "").Split(',').ToList();

		return content[content.Count - 1];
	}

	List<float> GetGestureVector(string receivedData, string gestureCode) {
		//		Debug.Log (receivedData);
		//		Debug.Log (gestureCode);
		List<float> vector = new List<float>();
		List<string> content = receivedData.Replace(gestureCode, "").Split(',').ToList();
		foreach (string c in content) {
			if (c.Trim() != string.Empty) {
				//				Debug.Log (c);
				try {
					vector.Add(Convert.ToSingle(c));
				}
				catch (Exception e) {
				}
			}
		}

		return vector;
	}

	float GetVectorVariance(List<float> vector) {
		if (vector.Count == 4) {
			return Mathf.Max(vector[vector.Count - 1], vector[vector.Count - 2]);
		}
		else {
			if (transformToScreenPointing) {
				receivedPointingVariance = new Vector2(RandomHelper.RandomFloat(0.0f, 0.2f),
					RandomHelper.RandomFloat(0.0f, 0.2f));
			}
			else {
				receivedPointingVariance = new Vector2(RandomHelper.RandomFloat(0.0f, 0.06f),
					RandomHelper.RandomFloat(0.0f, 0.06f));
			}

			return Mathf.Max(receivedPointingVariance.x, receivedPointingVariance.y);
		}
	}

	public void StartState(object[] content) {
	}

	public void BeginInteraction(object[] content) {
		if ((epistemicModel.userID != string.Empty) && (callUserByName)) {
			interactionPrefs.userName = epistemicModel.userID;
		}

		sessionCounter++;

		if (sessionCounter == 1) {
			RespondAndUpdate(interactionPrefs.userName != ""
				? string.Format("Hello, {0}.", interactionPrefs.userName)
				: "Hello.");
		}
		else if (sessionCounter > 1) {
			RespondAndUpdate(interactionPrefs.userName != ""
				? string.Format("Welcome back, {0}.", interactionPrefs.userName)
				: "Welcome back.");
		}

		MoveToPerform();
		gestureController.PerformGesture(AvatarGesture.RARM_WAVE);

		if (logFullState) {
			foreach (Voxeme voxeme in objSelector.allVoxemes) {
				if ((voxeme.gameObject.activeInHierarchy) &&
				    (!objSelector.disabledObjects.Contains(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject)))) {
					logger.OnLogEvent(this, new LoggerArgs(
						string.Format("{0}\t{1}\t{2}",
							logIndex.ToString(),
							"", string.Format("{0}:{1},{2}", voxeme.gameObject.name,
								Helper.VectorToParsable(voxeme.gameObject.transform.position),
								Helper.VectorToParsable(voxeme.gameObject.transform.eulerAngles)))));
				}
			}
		}

		if (!interactionLogic.waveToStart) {
			interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
		}
	}

	public void Ready(object[] content) {
		RespondAndUpdate("I'm ready to go!");
		MoveToPerform();
		gestureController.PerformGesture(AvatarGesture.RARM_THUMBS_UP);

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void Wait(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = (string) content[0];

					if ((message.StartsWith("YES")) || (message.StartsWith("posack"))) {
						RespondAndUpdate("OK.");
						LookForward();
					}
					else if ((message.StartsWith("NO")) || (message.StartsWith("negack"))) {
						RespondAndUpdate("OK.");
						LookForward();

						if ((interactionLogic.IndicatedObj == null) && (interactionLogic.IndicatedRegion == null)) {
							TurnForward();
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
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = (string) content[0];
				Debug.Log(message);

				if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("left point")) {
					Vector3 highlightCenter = TransformToSurface(
						GetGestureVector(interactionLogic.RemoveInputSymbolType(
							message, interactionLogic.GetInputSymbolType(message)), "left point"),
						leftArmScreenPointingBias);

					MoveHighlight(highlightCenter);
					regionHighlight.transform.position = highlightCenter;

					Bounds surfaceBounds = Helper.GetObjectWorldSize(demoSurface);
					Region surfaceRegion = new Region(
						new Vector3(surfaceBounds.min.x, highlightCenter.y, surfaceBounds.min.z),
						new Vector3(surfaceBounds.max.x, highlightCenter.y, surfaceBounds.max.z));

					if ((highlightCenter.y > 0) && (surfaceRegion.Contains(highlightCenter))) {
						// on table
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null,
									null, null, new List<string>(new string[] {message}))));

						List<GameObject> objOptions = FindObjectsInRegion(new Region(highlightCenter,
							vectorConeRadius * highlightOscUpper * 2));

						if (objOptions.Count == 0) {
							RespondAndUpdate("Are you pointing here?");
						}
						else {
							RespondAndUpdate("Are you pointing at this?");
						}

						LookForward();

						if (interactionLogic.GraspedObj == null) {
							if (InteractionHelper.GetCloserHand(Diana, highlightCenter) == rightGrasper) {
								ReachAndPoint(highlightCenter, rightGrasper);
							}
							else if (InteractionHelper.GetCloserHand(Diana, highlightCenter) == leftGrasper) {
								ReachAndPoint(highlightCenter, leftGrasper);
							}
						}
						else {
							if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
								ReachAndPoint(highlightCenter, rightGrasper);
							}
							else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
							         rightGrasper) {
								ReachAndPoint(highlightCenter, leftGrasper);
							}
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
									null, null, null)));
					}
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("right point")) {
					Vector3 highlightCenter = TransformToSurface(
						GetGestureVector(interactionLogic.RemoveInputSymbolType(
							message, interactionLogic.GetInputSymbolType(message)), "right point"),
						rightArmScreenPointingBias);

					MoveHighlight(highlightCenter);
					regionHighlight.transform.position = highlightCenter;

					Bounds surfaceBounds = Helper.GetObjectWorldSize(demoSurface);
					Region surfaceRegion = new Region(
						new Vector3(surfaceBounds.min.x, highlightCenter.y, surfaceBounds.min.z),
						new Vector3(surfaceBounds.max.x, highlightCenter.y, surfaceBounds.max.z));

					if ((highlightCenter.y > 0) && (surfaceRegion.Contains(highlightCenter))) {
						// on table
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null,
									null, null, new List<string>(new string[] {message}))));

						List<GameObject> objOptions = FindObjectsInRegion(new Region(highlightCenter,
							vectorConeRadius * highlightOscUpper * 2));

						if (objOptions.Count == 0) {
							RespondAndUpdate("Are you pointing here?");
						}
						else {
							RespondAndUpdate("Are you pointing at this?");
						}

						LookForward();

						if (interactionLogic.GraspedObj == null) {
							if (InteractionHelper.GetCloserHand(Diana, highlightCenter) == rightGrasper) {
								ReachAndPoint(highlightCenter, rightGrasper);
							}
							else if (InteractionHelper.GetCloserHand(Diana, highlightCenter) == leftGrasper) {
								ReachAndPoint(highlightCenter, leftGrasper);
							}
						}
						else {
							if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
								ReachAndPoint(highlightCenter, rightGrasper);
							}
							else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
							         rightGrasper) {
								ReachAndPoint(highlightCenter, leftGrasper);
							}
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
									null, null, null)));
					}
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("posack")) {
					RespondAndUpdate("Yes?");
					MoveToPerform();
					AllowHeadMotion();

					if (interactionLogic.GraspedObj == null) {
						PerformAndLogGesture(AvatarGesture.RARM_THUMBS_UP);
					}
					else {
						if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
							PerformAndLogGesture(AvatarGesture.RARM_THUMBS_UP);
						}
						else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == rightGrasper) {
							PerformAndLogGesture(AvatarGesture.LARM_THUMBS_UP);
						}
					}

					PerformAndLogGesture(AvatarGesture.HEAD_NOD);
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("negack")) {
					RespondAndUpdate("No?");
					MoveToPerform();
					AllowHeadMotion();

					if (interactionLogic.GraspedObj == null) {
						PerformAndLogGesture(AvatarGesture.RARM_THUMBS_DOWN);
					}
					else {
						if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
							PerformAndLogGesture(AvatarGesture.RARM_THUMBS_DOWN);
						}
						else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == rightGrasper) {
							PerformAndLogGesture(AvatarGesture.LARM_THUMBS_DOWN);
						}
					}

					PerformAndLogGesture(AvatarGesture.HEAD_SHAKE);
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("grab")) {
					LookForward();
					MoveToPerform();

					AvatarGesture performGesture = null;

					if ((interactionLogic.ActionOptions.Count > 0) && (interactionLogic.ActionSuggestions.Count > 0)) {
						if (interactionLogic.ActionOptions[0] == interactionLogic.ActionSuggestions[0]) {
							if (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
									interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0]))
								.StartsWith("grab move")) {
								interactionLogic.RewriteStack(
									new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));

								string dir = interactionLogic.GetGestureContent(interactionLogic.RemoveInputSymbolType(
										interactionLogic.ActionSuggestions[0],
										interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])),
									"grab move");

								if (interactionLogic.GraspedObj == null) {
									// not grasping anything
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

									RespondAndUpdate("Do you want me to move something this way?");
								}
								else {
									// grasping something
									if (dir == "left") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_RIGHT;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_RIGHT;
										}
									}
									else if (dir == "right") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_LEFT;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_LEFT;
										}
									}
									else if (dir == "front") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_BACK;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_BACK;
										}
									}
									else if (dir == "back") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_FRONT;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_FRONT;
										}
									}
									else if (dir == "up") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_UP;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_UP;
										}
									}
									else if (dir == "down") {
										if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										    leftGrasper) {
											performGesture = AvatarGesture.RARM_CARRY_DOWN;
										}
										else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
										         rightGrasper) {
											performGesture = AvatarGesture.LARM_CARRY_DOWN;
										}
									}

									RespondAndUpdate("Do you want me to move this this way?");
								}
							}
						}
						else {
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null, null,
										null, null, new List<string>(new string[] {message}))));

							if (interactionLogic.IndicatedObj == null) {
								RespondAndUpdate("Are you asking me to grab something?");
							}
							else {
								RespondAndUpdate("Are you asking me to grab this?");
							}

							performGesture = AvatarGesture.RARM_CARRY_STILL;
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null,
									null, null, new List<string>(new string[] {message}))));

						if (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionSuggestions[0],
								interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0]))
							.StartsWith("grab move")) {
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));

							string dir = interactionLogic.GetGestureContent(interactionLogic.RemoveInputSymbolType(
									interactionLogic.ActionSuggestions[0],
									interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])),
								"grab move");

							if (interactionLogic.GraspedObj == null) {
								// not grasping anything
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

								RespondAndUpdate("Do you want me to move something this way?");
							}
							else {
								// grasping something
								if (dir == "left") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_RIGHT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_RIGHT;
									}
								}
								else if (dir == "right") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_LEFT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_LEFT;
									}
								}
								else if (dir == "front") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_BACK;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_BACK;
									}
								}
								else if (dir == "back") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_FRONT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_FRONT;
									}
								}
								else if (dir == "up") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_UP;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_UP;
									}
								}
								else if (dir == "down") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_CARRY_DOWN;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_CARRY_DOWN;
									}
								}

								RespondAndUpdate("Do you want me to move this this way?");
							}
						}
						else {
							if (interactionLogic.IndicatedObj == null) {
								RespondAndUpdate("Are you asking me to grab something?");
							}
							else {
								RespondAndUpdate("Are you asking me to grab this?");
							}

							performGesture = AvatarGesture.RARM_CARRY_STILL;
						}
					}

					PerformAndLogGesture(performGesture);
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("push")) {
					LookForward();
					MoveToPerform();

					AvatarGesture performGesture = null;
					if ((interactionLogic.ActionOptions.Count > 0) && (interactionLogic.ActionSuggestions.Count > 0)) {
						if (interactionLogic.ActionOptions[0] == interactionLogic.ActionSuggestions[0]) {
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));

							string dir = interactionLogic.GetGestureContent(interactionLogic.RemoveInputSymbolType(
								interactionLogic.ActionSuggestions[0],
								interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "push");

							if (interactionLogic.GraspedObj == null) {
								// not grasping anything
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
									RespondAndUpdate("Do you want me to push something this way?");
								}
								else {
									RespondAndUpdate("Do you want me to push this this way?");
								}
							}
							else {
								// grasping something
								if (dir == "left") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_PUSH_RIGHT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_PUSH_RIGHT;
									}
								}
								else if (dir == "right") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_PUSH_LEFT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_PUSH_LEFT;
									}
								}
								else if (dir == "front") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_PUSH_BACK;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_PUSH_BACK;
									}
								}
								else if (dir == "back") {
									if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									    leftGrasper) {
										performGesture = AvatarGesture.RARM_PUSH_FRONT;
									}
									else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
									         rightGrasper) {
										performGesture = AvatarGesture.LARM_PUSH_FRONT;
									}
								}

								RespondAndUpdate("Do you want me to push this this way?");
							}
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null,
									null, null, new List<string>(new string[] {message}))));

						string dir = interactionLogic.GetGestureContent(interactionLogic.RemoveInputSymbolType(
							interactionLogic.ActionSuggestions[0],
							interactionLogic.GetInputSymbolType(interactionLogic.ActionSuggestions[0])), "push");

						if (interactionLogic.GraspedObj == null) {
							// not grasping anything
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
								RespondAndUpdate("Do you want me to push something this way?");
							}
							else {
								RespondAndUpdate("Do you want me to push this this way?");
							}
						}
						else {
							// grasping something
							if (dir == "left") {
								if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								    leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_RIGHT;
								}
								else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								         rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_RIGHT;
								}
							}
							else if (dir == "right") {
								if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								    leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_LEFT;
								}
								else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								         rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_LEFT;
								}
							}
							else if (dir == "front") {
								if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								    leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_BACK;
								}
								else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								         rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_BACK;
								}
							}
							else if (dir == "back") {
								if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								    leftGrasper) {
									performGesture = AvatarGesture.RARM_PUSH_FRONT;
								}
								else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) ==
								         rightGrasper) {
									performGesture = AvatarGesture.LARM_PUSH_FRONT;
								}
							}

							RespondAndUpdate("Do you want me to push this this way?");
						}
					}

					PerformAndLogGesture(performGesture);
				}

				break;

			default:
				break;
		}
	}

	public void Confirm(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));

		// can we use this state to confirm objects or actions?
	}

	public void TrackPointing(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				regionHighlight.GetComponent<Renderer>().material = activeHighlightMaterial;
				highlightTimeoutTimer.Interval = highlightTimeoutTime;
				highlightTimeoutTimer.Enabled = true;

				if (interactionLogic.RemoveInputSymbolType((string) content[0],
					interactionLogic.GetInputSymbolType((string) content[0])).StartsWith("l")) {
					highlightCenter = TransformToSurface(GetGestureVector(interactionLogic.RemoveInputSymbolType(
							(string) content[0], interactionLogic.GetInputSymbolType((string) content[0])), "l"),
						leftArmScreenPointingBias);
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						null));
				}
				else if (interactionLogic.RemoveInputSymbolType((string) content[0],
					interactionLogic.GetInputSymbolType((string) content[0])).StartsWith("r")) {
					highlightCenter = TransformToSurface(GetGestureVector(interactionLogic.RemoveInputSymbolType(
							(string) content[0], interactionLogic.GetInputSymbolType((string) content[0])), "r"),
						rightArmScreenPointingBias);
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						null));
				}

				//		Debug.Log (string.Format("({0},{1};{2},{3})",vector[0],vector[1],vector[2],vector[4]));
				//Debug.Log (highlightCenter);

				// jump from origin on first update
				if (regionHighlight.transform.position.sqrMagnitude <= Constants.EPSILON) {
					MoveHighlight(highlightCenter);
					regionHighlight.transform.position = highlightCenter;
				}

				if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
					Vector3 offset = MoveHighlight(highlightCenter);

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
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = GetSpeechString(
						interactionLogic.RemoveInputSymbolType((string) content[0],
							interactionLogic.GetInputSymbolType((string) content[0])), "S");

					foreach (string preamble in knownPreables) {
						if (message.Contains(preamble)) {
							if (preamble.Contains(" ")) {
								message = message.Replace(preamble, preamble.Replace(" ", "_"));
							}
						}
					}

					List<string> splitMessage = message.Split()
						.Where(m => (!knownDysfluencies.Contains(m) && !knownPreables.Contains(m))).ToList();
					message = String.Join(" ", splitMessage.ToArray());
				}

				Debug.Log(message);
				// do stuff here

				if (message == "yes") {
//				interactionLogic.RewriteStack(new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
//					interactionLogic.GenerateStackSymbol (null, null, null,
//						null, new List<string>(new string[]{ commBridge.NLParse (message) }), null)));
				}
				else if (message == "no") {
				}

				break;

			default:
				break;
		}
	}

	public void ParseQuestion(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = GetSpeechString(
						interactionLogic.RemoveInputSymbolType((string) content[0],
							interactionLogic.GetInputSymbolType((string) content[0])), "S");

					foreach (string preamble in knownPreables) {
						if (message.Contains(preamble)) {
							if (preamble.Contains(" ")) {
								message = message.Replace(preamble, preamble.Replace(" ", "_"));
							}
						}
					}

					List<string> splitMessage = message.Split()
						.Where(m => (!knownDysfluencies.Contains(m) && !knownPreables.Contains(m))).ToList();
					message = String.Join(" ", splitMessage.ToArray());
				}

				Debug.Log(message);
				// do stuff here

				// demo hacks TODO: better than this
				if (message == "how many blocks are there") {
					if (dianaMemory != null && dianaMemory.enabled) {
						int knownCount = availableObjs.Where(b => dianaMemory.IsKnown(b.GetComponent<Voxeme>()))
							.ToList().Count;

						RespondAndUpdate(string.Format("There are {0} blocks on the table.", knownCount));
					}
				}
				else if (message == "how many blocks do you see") {
					int visibleCount = availableObjs.Where(b => dianaMemory._vision.IsVisible(b.GetComponent<Voxeme>()))
						.ToList().Count;

					RespondAndUpdate(string.Format("I can see {0} blocks.", visibleCount));
				}
				else if (message.StartsWith("where is the")) {
					string blockString = message.Replace("where is the", "").Trim();
					string attr = blockString.Replace("block", "").Trim();

					GameObject blockObj = null;

					foreach (GameObject block in availableObjs) {
						bool isKnown = true;

						if (dianaMemory != null && dianaMemory.enabled) {
							isKnown = dianaMemory.IsKnown(block.GetComponent<Voxeme>());
						}

						if (block.activeInHierarchy) {
							if ((block.GetComponent<AttributeSet>().attributes.Contains(attr)) &&
							    (isKnown)) {
								blockObj = block;
							}
						}
					}

					if (blockObj != null) {
						Debug.Log(blockObj);
						List<Pair<Pair<GameObject, GameObject>, string>> relationsInForce =
							new List<Pair<Pair<GameObject, GameObject>, string>>();

						foreach (DictionaryEntry relation in relationTracker.relations) {
                            // longest relation set == most relevant

                            if ((relation.Key as List<GameObject>).Contains(blockObj) && !(relation.Key as List<GameObject>).Contains(demoSurface)) {
                                relationsInForce.Add(new Pair<Pair<GameObject, GameObject>, string>(
									new Pair<GameObject, GameObject>((relation.Key as List<GameObject>)[0],
										(relation.Key as List<GameObject>)[1]),
									relation.Value as string));
							}
						}

						relationsInForce = relationsInForce.OrderByDescending(r => r.Item2.Split(',').ToList().Count)
							.ToList();
						foreach (Pair<Pair<GameObject, GameObject>, string> relation in relationsInForce) {
							if (relation.Item2.Contains("support")) {
								// use support as proxy for inverse of "on"
								relation.Item1 = relation.Item1.Reverse();
								relation.Item2 = "on";
							}
						}

						if (relationsInForce.Count > 0) {
							Debug.Log(string.Format("{0} {1} {2}", relationsInForce[0].Item1.Item1,
								relationsInForce[0].Item2, relationsInForce[0].Item1.Item2));

							RespondAndUpdate(string.Format("The {0} block is {1} the {2} block.",
								relationsInForce[0].Item1.Item1.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value,
								relationsInForce[0].Item2,
								relationsInForce[0].Item1.Item2.GetComponent<Voxeme>().voxml.Attributes.Attrs[0]
									.Value));
						}
						else {
							RespondAndUpdate(string.Format("Sorry, I don't know the answer to that."));
						}
					}
				}

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null,
						null, null, null)));

				break;

			default:
				break;
		}
	}

	public void ParseVP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = GetSpeechString(
						interactionLogic.RemoveInputSymbolType((string) content[0],
							interactionLogic.GetInputSymbolType((string) content[0])), "S");

					foreach (string preamble in knownPreables) {
						if (message.Contains(preamble)) {
							if (preamble.Contains(" ")) {
								message = message.Replace(preamble, preamble.Replace(" ", "_"));
							}
						}
					}

					List<string> splitMessage = message.Split()
						.Where(m => (!knownDysfluencies.Contains(m) && !knownPreables.Contains(m))).ToList();
					message = String.Join(" ", splitMessage.ToArray());
				}

				Debug.Log(message);
				// do stuff here

				// do verb mapping
				if (message.StartsWith("pick up")) {
					message = message.Replace("pick up", "lift");
				}
				else if (message.StartsWith("pick") && (message.EndsWith("up"))) {
					message = message.Replace("pick", "lift").Replace("up", "");
				}
				else if (message.StartsWith("grab")) {
					message = message.Replace("grab", "grasp");
				}
				else if (message.StartsWith("move")) {
					message = message.Replace("move", "put");
				}
				else if (message.StartsWith("push")) {
					message = message.Replace("push", "slide");
				}
				else if (message.StartsWith("pull")) {
					message = message.Replace("pull", "slide");
				}

				// do noun mapping
				if (message.Split().Contains("box")) {
					message = message.Replace("box", "block");
				}
				else if (message.Split().Contains("boxes")) {
					message = message.Replace("boxes", "blocks");
				}
				else if (message.Split().Contains("mug")) {
					message = message.Replace("mug", "cup");
				}
				else if (message.Split().Contains("mugs")) {
					message = message.Replace("mugs", "cups");
				}

				if (message.Split().Contains("these")) {
					message = message.Replace("these", "this");
				}
				else if (message.Split().Contains("those")) {
					message = message.Replace("those", "that");
				}

				// get rid of "on/to the" before PP
				// on the top of, on top of -> on
				// to back of, to the back of -> behind
				if ((message.Contains("to the left")) || (message.Contains("to the right"))) {
					message = message.Replace("to the", "");
				}
				else if (message.Contains("to back of")) {
					message = message.Replace("to back of", "behind");
				}
				else if (message.Contains("to the back of")) {
					message = message.Replace("to the back of", "behind");
				}
				else if (message.Contains("on top of")) {
					message = message.Replace("on top of", "on");
				}
				else if (message.Contains("on the top of")) {
					message = message.Replace("on the top of", "on");
				}

				// assume everything is a block
				if (message.Split().Contains("one")) {
					// for non-blocks world situations, we need anaphora resolution (cf. "it" handling)
					message = message.Replace("one", "block");
				}

				if (message.Split().Contains("it") || (message.Split().Contains("them"))) {
					if (eventManager.referents.stack.Count > 0) {
						object referent = eventManager.referents.stack.Peek();

						if (referent.GetType() == typeof(String)) {
							GameObject voxObj = GameObject.Find(referent as String);
							if (voxObj != null) {
								if (message.Split().Contains("it")) {
									message = message.Replace("it", voxObj.name);
								}
								else if (message.Split().Contains("them")) {
									message = message.Replace("them", voxObj.name);
								}
							}
							else {
								if (message.Split().Contains("it")) {
									message = message.Replace("it", "{0}");
								}
								else if (message.Split().Contains("them")) {
									message = message.Replace("them", "{0}");
								}
							}
						}
						else {
							if (message.Split().Contains("it")) {
								message = message.Replace("it", "{0}");
							}
							else if (message.Split().Contains("them")) {
								message = message.Replace("them", "{0}");
							}
						}
					}
					else {
						if (message.Split().Contains("it")) {
							message = message.Replace("it", "{0}");
						}
						else if (message.Split().Contains("them")) {
							message = message.Replace("them", "{0}");
						}
					}
				}

				Debug.Log(message);

				if (message.Contains("there")) {
					if ((regionHighlight.GetComponent<Renderer>().material == activeHighlightMaterial) &&
					    (regionHighlight.transform.position.y > 0.0f)) {
						if (!Helper.RegionsEqual(interactionLogic.IndicatedRegion, new Region())) {
							// non-empty region or null
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new Region(highlightCenter, vectorConeRadius * highlightOscUpper * 2),
										null,
										new List<string>(new string[]
											{commBridge.NLParse(message.Replace("there", "{1}"))}), null)));
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null,
									null,
									new List<string>(new string[]
										{commBridge.NLParse(message.Replace("there", "{1}"))}), null)));
					}
				}
				else {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, null, null,
							null, new List<string>(new string[] {commBridge.NLParse(message)}), null)));
				}

				break;

			default:
				break;
		}
	}

    /// <summary>
    /// After being notified by broadcast that there is a new parse
    /// from the external server, actually carry out the task.
    /// This code is copypasta'd from other places we add stuff to the stack
    /// because I don't actually know exactly what it does/how it works
    /// </summary>
    public void LookForNewParse(object sender, EventArgs e) {
        string parse = commBridge.GrabParse();
        PromptEvent(parse);
    }

	public void ParseNP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = GetSpeechString(
						interactionLogic.RemoveInputSymbolType((string) content[0],
							interactionLogic.GetInputSymbolType((string) content[0])), "S");

					foreach (string preamble in knownPreables) {
						if (message.Contains(preamble)) {
							if (preamble.Contains(" ")) {
								message = message.Replace(preamble, preamble.Replace(" ", "_"));
							}
						}
					}

					List<string> splitMessage = message.Split()
						.Where(m => (!knownDysfluencies.Contains(m) && !knownPreables.Contains(m))).ToList();
					message = String.Join(" ", splitMessage.ToArray());
				}

				Debug.Log(message);

				if (message.Split().Contains("these")) {
					message = message.Replace("these", "this");
				}
				else if (message.Split().Contains("those")) {
					message = message.Replace("those", "that");
				}

				if ((message.StartsWith("this")) || (message.StartsWith("that"))) {
					if ((regionHighlight.GetComponent<Renderer>().material == activeHighlightMaterial) &&
					    (regionHighlight.transform.position.y > 0.0f)) {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new Region(highlightCenter, vectorConeRadius * highlightOscUpper * 2),
									null, null, null)));
					}
					else {
						PromptEvent(commBridge.NLParse(message));
					}
				}
				else {
					// assume everything is a block
					if (message.EndsWith("one")) {
						// for non-blocks world situations, we need anaphora resolution (cf. "it" handling)
						message = message.Replace("one", "block");
					}

					PromptEvent(commBridge.NLParse(message));
				}

				break;

			default:
				break;
		}
	}

	public void ParsePP(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] != null) {
					message = GetSpeechString(
						interactionLogic.RemoveInputSymbolType((string) content[0],
							interactionLogic.GetInputSymbolType((string) content[0])), "S");

					foreach (string preamble in knownPreables) {
						if (message.Contains(preamble)) {
							if (preamble.Contains(" ")) {
								message = message.Replace(preamble, preamble.Replace(" ", "_"));
							}
						}
					}

					List<string> splitMessage = message.Split()
						.Where(m => (!knownDysfluencies.Contains(m) && !knownPreables.Contains(m))).ToList();
					message = String.Join(" ", splitMessage.ToArray());
				}

				Debug.Log(message);
				// do stuff here

				if (interactionLogic.IndicatedObj != null) {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, null, null, null,
							new List<string>(new string[] {
								commBridge.NLParse(string.Format("put {0} {1}", interactionLogic.IndicatedObj.name,
									message))
							}), null)));
				}
				else if (interactionLogic.GraspedObj != null) {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, null, null, null,
							new List<string>(new string[] {
								commBridge.NLParse(string.Format("put {0} {1}", interactionLogic.GraspedObj.name,
									message))
							}), null)));
				}
				else if (interactionLogic.ActionOptions.Count > 0) {
					if (interactionLogic.ActionOptions[0].Contains("{1}")) {
						Debug.Log(interactionLogic.ActionOptions[0].Replace("{1}", commBridge.NLParse(message)));
						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null, null, null,
								new List<string>(new string[]
									{interactionLogic.ActionOptions[0].Replace("{1}", commBridge.NLParse(message))}),
								null)));
					}
				}
				else {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, null, null, null,
							new List<string>(new string[]
								{commBridge.NLParse(string.Format("put {0} {1}", "{0}", message))}), null)));
				}

				break;

			default:
				break;
		}
	}

	public void SituateDeixis(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] == null) {
					Debug.Log(interactionLogic.StackSymbolToString(interactionLogic.CurrentStackSymbol));
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionSuggestions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionSuggestions)[0];
					}
					else {
						return;
					}
				}
				else {
					message = (string) content[0];
				}

				Debug.Log(message);

				if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("left point")) {
					Vector3 highlightCenter = TransformToSurface(
						GetGestureVector(interactionLogic.RemoveInputSymbolType(
							message, interactionLogic.GetInputSymbolType(message)), "left point"),
						leftArmScreenPointingBias);

					MoveHighlight(highlightCenter);
					regionHighlight.transform.position = highlightCenter;
					highlightTimeoutTimer.Interval = highlightTimeoutTime;
					highlightTimeoutTimer.Enabled = true;

					Region testRegion = new Region(
						new Vector3(Helper.GetObjectWorldSize(demoSurface).min.x, regionHighlight.transform.position.y,
							Helper.GetObjectWorldSize(demoSurface).min.z),
						new Vector3(Helper.GetObjectWorldSize(demoSurface).max.x, regionHighlight.transform.position.y,
							Helper.GetObjectWorldSize(demoSurface).max.z));

					if (Helper.RegionsEqual(interactionLogic.IndicatedRegion, new Region())) {
						// empty region
						if (testRegion.Contains(highlightCenter) && (highlightCenter.y > 0.0f)) {
							// enabled = on table
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new Region(highlightCenter, vectorConeRadius * highlightOscUpper * 2),
										null, null, null)));
						}
						else {
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
										null, null, null)));
						}
					}
				}
				else if (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					.StartsWith("right point")) {
					Vector3 highlightCenter = TransformToSurface(GetGestureVector(
							interactionLogic.RemoveInputSymbolType(
								message, interactionLogic.GetInputSymbolType(message)), "right point"),
						rightArmScreenPointingBias);

					MoveHighlight(highlightCenter);
					regionHighlight.transform.position = highlightCenter;
					highlightTimeoutTimer.Interval = highlightTimeoutTime;
					highlightTimeoutTimer.Enabled = true;

					Region testRegion = new Region(
						new Vector3(Helper.GetObjectWorldSize(demoSurface).min.x, regionHighlight.transform.position.y,
							Helper.GetObjectWorldSize(demoSurface).min.z),
						new Vector3(Helper.GetObjectWorldSize(demoSurface).max.x, regionHighlight.transform.position.y,
							Helper.GetObjectWorldSize(demoSurface).max.z));

					if (Helper.RegionsEqual(interactionLogic.IndicatedRegion, new Region())) {
						// empty region
						if (testRegion.Contains(highlightCenter) && (highlightCenter.y > 0.0f)) {
							// enabled = on table
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new Region(highlightCenter, vectorConeRadius * highlightOscUpper * 2),
										null, null, null)));
						}
						else {
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
										null, null, null)));
						}
					}
				}
				else if ((interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					         .StartsWith("THIS")) ||
				         (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					         .StartsWith("THAT")) ||
				         (interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
					         .StartsWith("THERE"))) {
					if ((regionHighlight.GetComponent<Renderer>().material == activeHighlightMaterial) &&
					    (regionHighlight.transform.position.y > 0.0f)) {
						if (Helper.RegionsEqual(interactionLogic.IndicatedRegion, new Region())) {
							// empty region
							interactionLogic.RewriteStack(
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, null,
										new Region(highlightCenter, vectorConeRadius * highlightOscUpper * 2),
										null, null, null)));
						}
					}
					else {
						interactionLogic.RewriteStack(
							new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new FunctionDelegate(interactionLogic.NullObject),
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
		List<GameObject> objectOptions = FindObjectsInRegion(interactionLogic.IndicatedRegion);

		//		objectPlacements = objectPlacements.OrderByDescending (o => o.transform.position.y).
		//			ThenBy (o => (o.transform.position - theme.transform.position).magnitude).ToList ();
		objectOptions = objectOptions.OrderBy(o => (o.transform.position - highlightCenter).magnitude).ToList();

		if (objectOptions.Count > 0) {
			if (interactionLogic.IndicatedObj != null) {
				objectOptions = objectOptions
					.Where(o => !o.transform.IsChildOf(interactionLogic.IndicatedObj.transform)).ToList();
			}
			else if (interactionLogic.GraspedObj != null) {
				objectOptions = objectOptions.Where(o => !o.transform.IsChildOf(interactionLogic.GraspedObj.transform))
					.ToList();
			}

			interactionLogic.RewriteStack(
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null, objectOptions, null, null)));
		}
		else {
			interactionLogic.RewriteStack(
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, interactionLogic.IndicatedRegion,
						new List<GameObject>(), null, null)));
		}
	}

	public void DisambiguateObject(object[] content) {
		bool duplicateNominalAttr = false;
		List<Voxeme> objVoxemes = new List<Voxeme>();

		foreach (GameObject option in interactionLogic.ObjectOptions) {
			if (option.GetComponent<Voxeme>() != null) {
				objVoxemes.Add(option.GetComponent<Voxeme>());
			}
		}

		List<object> uniqueAttrs = new List<object>();
		// if all voxemes in options have the same lexical predicate (i.e., same object type)
		bool allOptionsSameType = ((objVoxemes.Count > 1) &&
		                           objVoxemes.All(o => o.voxml.Lex.Pred == objVoxemes[0].voxml.Lex.Pred));
		// if any voxemes in the scene have the same lexical predicate (i.e., same object type) as target object
		bool anyGlobalSameType = ((objSelector.allVoxemes.Count > 1) &&
		                          objSelector.allVoxemes.Any(o =>
			                          (o != objVoxemes[0]) && (o.voxml.Lex.Pred == objVoxemes[0].voxml.Lex.Pred)));
		if (allOptionsSameType || anyGlobalSameType) {
			for (int i = 0; i < objVoxemes.Count; i++) {
				List<object> newAttrs = Helper.DiffLists(
					uniqueAttrs.Select(x => ((VoxAttributesAttr) x).Value).Cast<object>().ToList(),
					objVoxemes[i].voxml.Attributes.Attrs.Cast<object>().ToList()
						.Select(x => ((VoxAttributesAttr) x).Value).Cast<object>().ToList());

				if (newAttrs.Count > 0) {
					foreach (object attr in newAttrs) {
						Debug.Log(string.Format("{0}:{1}", objVoxemes[i].name, attr));
						VoxAttributesAttr attrToAdd = new VoxAttributesAttr();
						attrToAdd.Value = attr.ToString();

						if (uniqueAttrs.Where(x => ((VoxAttributesAttr) x).Value == attrToAdd.Value).ToList()
							    .Count == 0) {
							uniqueAttrs.Add(attrToAdd);
						}
					}
				}
				else {
					duplicateNominalAttr = true;
				}
			}
		}

//		Debug.Log (interactionLogic.ObjectOptions);
//		Debug.Log (interactionLogic.IndicatedObj.name);
		string attribute = string.Empty;
		if (uniqueAttrs.Count > 0) {
			attribute = ((VoxAttributesAttr) uniqueAttrs[uniqueAttrs.Count - 1]).Value;
		}

		if (duplicateNominalAttr) {
			RespondAndUpdate(string.Format("Which {0} {1}?", attribute, objVoxemes[0].voxml.Lex.Pred));
			LookForward();
		}
		else {
			if (interactionLogic.ActionOptions.Count == 0) {
				if ((interactionLogic.GraspedObj == null) &&
				    (interactionLogic.ObjectOptions.Contains(interactionLogic.IndicatedObj))) {
					RespondAndUpdate(string.Format("Do you mean {0}?", GenerateReferringExpression(
						interactionLogic.IndicatedObj,
						availableObjs.Cast<object>().ToList())));
					ReachFor(interactionLogic.IndicatedObj);
					LookForward();
				}
				else if ((interactionLogic.IndicatedObj != null) &&
				         (!interactionLogic.ObjectOptions.Contains(interactionLogic.IndicatedObj))) {
					RespondAndUpdate(string.Format("Should I put {0} on {1}?",
						GenerateReferringExpression(interactionLogic.IndicatedObj,
							interactionLogic.ObjectOptions
								.Concat(new List<GameObject>() {interactionLogic.IndicatedObj}).Cast<object>()
								.ToList()),
						GenerateReferringExpression(
							interactionLogic.ObjectOptions[interactionLogic.ObjectOptions.Count - 1],
							interactionLogic.ObjectOptions
								.Concat(new List<GameObject>() {interactionLogic.IndicatedObj}).Cast<object>()
								.ToList())));
					LookForward();
				}
				else if (interactionLogic.GraspedObj != null) {
					RespondAndUpdate(string.Format("Should I put {0} on {1}?",
						GenerateReferringExpression(interactionLogic.GraspedObj,
							interactionLogic.ObjectOptions
								.Concat(new List<GameObject>() {interactionLogic.IndicatedObj}).Cast<object>()
								.ToList()),
						GenerateReferringExpression(
							interactionLogic.ObjectOptions[interactionLogic.ObjectOptions.Count - 1],
							interactionLogic.ObjectOptions
								.Concat(new List<GameObject>() {interactionLogic.IndicatedObj}).Cast<object>()
								.ToList())));
					LookForward();
				}
			}
			else if (interactionLogic.ActionOptions[0].Contains("put")) {
				object theme = eventManager.ExtractObjects(Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
					(String) Helper.ParsePredicate(interactionLogic.ActionOptions[0])[
						Helper.GetTopPredicate(interactionLogic.ActionOptions[0])])[0];

				if (theme is GameObject) {
					RespondAndUpdate(string.Format("Should I put {0} on {1}?",
						GenerateReferringExpression((theme as GameObject),
							interactionLogic.ObjectOptions.Concat(new List<GameObject>() {(theme as GameObject)})
								.Cast<object>().ToList()),
						GenerateReferringExpression(
							interactionLogic.ObjectOptions[interactionLogic.ObjectOptions.Count - 1],
							interactionLogic.ObjectOptions.Concat(new List<GameObject>() {(theme as GameObject)})
								.Cast<object>().ToList())));
					LookForward();
				}
			}
		}
	}

	public void IndexByColor(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				List<GameObject> objectOptions = new List<GameObject>();

				foreach (GameObject block in availableObjs) {
					bool isKnown = true;

					if (dianaMemory != null && dianaMemory.enabled) {
						isKnown = dianaMemory.IsKnown(block.GetComponent<Voxeme>());
					}

					if ((block.activeInHierarchy) || (objSelector.disabledObjects.Contains(block))) {
						if ((block.GetComponent<AttributeSet>().attributes.Contains(
							    interactionLogic.RemoveInputSymbolType(
									    content[0].ToString(),
									    interactionLogic.GetInputSymbolType(content[0].ToString()))
								    .ToLower().Replace("np ", ""))) &&
						    (isKnown) && (SurfaceClear(block)) && (block != interactionLogic.IndicatedObj) &&
						    (block != interactionLogic.GraspedObj)) {
							objectOptions.Add(block);
						}

						if ((objectOptions.Contains(block)) && (isKnown) && ((block == interactionLogic.IndicatedObj) ||
						                                                     (block == interactionLogic.GraspedObj))) {
							objectOptions.Remove(block);
						}
					}
					else {
						if ((objectOptions.Contains(block)) && (isKnown)) {
							objectOptions.Remove(block);
						}
					}
				}

				if (objectOptions.Count > 0) {
					interactionLogic.RewriteStack(
						new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null, null, objectOptions, null, null)));
				}
				else {
					interactionLogic.RewriteStack(
						new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null, null,
								null, null, null)));
				}

				break;

			default:
				break;
		}
	}

	public void IndexBySize(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = interactionLogic.RemoveInputSymbolType(
					content[0].ToString(), interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower();
				GameObject obj = null;
				if (message == "big") {
					obj = interactionLogic.ObjectOptions.OrderByDescending(o =>
						Helper.GetObjectWorldSize(o).size.x *
						Helper.GetObjectWorldSize(o).size.y *
						Helper.GetObjectWorldSize(o).size.z).ToList()[0];
				}
				else if (message == "small") {
					obj = interactionLogic.ObjectOptions.OrderBy(o =>
						Helper.GetObjectWorldSize(o).size.x *
						Helper.GetObjectWorldSize(o).size.y *
						Helper.GetObjectWorldSize(o).size.z).ToList()[0];
				}

				interactionLogic.RewriteStack(
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, null, null,
							new List<GameObject>(new GameObject[] {obj}), null, null)));
				break;

			default:
				break;
		}
	}

	public void IndexByRegion(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				Region region = null;
				if (interactionLogic.RemoveInputSymbolType(
					    content[0].ToString(), interactionLogic.GetInputSymbolType(content[0].ToString())).ToLower() ==
				    "left") {
					region = leftRegion;
				}
				else if (interactionLogic.RemoveInputSymbolType(
						         content[0].ToString(), interactionLogic.GetInputSymbolType(content[0].ToString()))
					         .ToLower() == "right") {
					region = rightRegion;
				}
				else if (interactionLogic.RemoveInputSymbolType(
						         content[0].ToString(), interactionLogic.GetInputSymbolType(content[0].ToString()))
					         .ToLower() == "front") {
					region = frontRegion;
				}
				else if (interactionLogic.RemoveInputSymbolType(
						         content[0].ToString(), interactionLogic.GetInputSymbolType(content[0].ToString()))
					         .ToLower() == "back") {
					region = backRegion;
				}

				if (Helper.RegionsEqual(interactionLogic.IndicatedRegion, new Region())) {
					// empty region
					interactionLogic.RewriteStack(
						new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null,
								region,
								null, null, null)));
				}

				break;

			default:
				break;
		}
	}

	public void IndexByGesture(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = (string) content[0];
				Debug.Log(message);
				// find object associated with this conventional grasp pose
				GameObject obj = null;
				StackSymbolContent symbolContent = ((StackSymbolContent) interactionLogic.LearnableInstructions[
					interactionLogic.GetLearnableInstructionKeyByName(message)].Content);

				if (((List<string>) symbolContent.ActionOptions).Count > 0) {
					string instructionCmd = ((List<string>) ((StackSymbolContent) interactionLogic
						.LearnableInstructions[
							interactionLogic.GetLearnableInstructionKeyByName(message)].Content).ActionOptions)[0];

					obj = eventManager.ExtractObjects(Helper.GetTopPredicate(instructionCmd),
							(String) Helper.ParsePredicate(instructionCmd)[Helper.GetTopPredicate(instructionCmd)])[0]
						as
						GameObject;

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(obj, null, null, null, null, null)));
				}

				break;

			default:
				break;
		}
	}

	public void RegionAsGoal(object[] content) {
		if ((interactionLogic.IndicatedObj != null) || (interactionLogic.GraspedObj != null)) {
			RespondAndUpdate("Should I place this here?");
		}
		else {
			RespondAndUpdate("Should I place something here?");
		}

		ReachFor(interactionLogic.IndicatedRegion.center);
		LookForward();
	}

	public void ConfirmObject(object[] content) {
		if (interactionLogic.ActionOptions.Count == 0) {
			RespondAndUpdate("OK, go on.");
		}
		else {
			RespondAndUpdate("OK.");
		}

		if (interactionLogic.GraspedObj == null) {
			ReturnHandsToDefault();
			ReachFor(interactionLogic.IndicatedObj);
		}

		LookForward();

		if ((eventManager.referents.stack.Count == 0) ||
		    (!eventManager.referents.stack.Peek().Equals(interactionLogic.IndicatedObj.name))) {
			eventManager.referents.stack.Push(interactionLogic.IndicatedObj.name);
		}

		eventManager.OnEntityReferenced(this, new EventReferentArgs(interactionLogic.IndicatedObj.name));
		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void RequestObject(object[] content) {
		if (interactionLogic.ActionOptions.Count > 0) {
			if ((new Regex(@"grasp\(\{0\}\)")).IsMatch(interactionLogic.ActionOptions[0])) {
				RespondAndUpdate("What should I grab?");
			}
			else if ((new Regex(@"lift\(\{0\}\)")).IsMatch(interactionLogic.ActionOptions[0])) {
				RespondAndUpdate("What should I lift?");
			}
			else if ((new Regex(@"put\(\{0\},<.+,.+,.+>\)")).IsMatch(interactionLogic.ActionOptions[0])) {
				RespondAndUpdate("What should I put there?");
			}
            else if ((new Regex(@"put\(.+,\{0\}\)")).IsMatch(interactionLogic.ActionOptions[0])) {
                RespondAndUpdate("Where should I put that?");
            }
            else if (((new Regex(@"put\(\{0\},.+\)")).IsMatch(interactionLogic.ActionOptions[0])) ||
			         (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionOptions[0],
					         interactionLogic.GetInputSymbolType(interactionLogic.ActionOptions[0]))
				         .StartsWith("grab move"))) {
				RespondAndUpdate("What should I move?");
			}
			else if (((new Regex(@"slide\(\{0\},.+\)")).IsMatch(interactionLogic.ActionOptions[0])) ||
			         (interactionLogic.RemoveInputSymbolType(interactionLogic.ActionOptions[0],
				         interactionLogic.GetInputSymbolType(interactionLogic.ActionOptions[0])).StartsWith("push"))) {
				RespondAndUpdate("What should I push?");
			}
		}
		else {
			RespondAndUpdate("Which object do you want?");
		}
	}

	public void RequestLocation(object[] content) {
		object theme = eventManager.ExtractObjects(Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
			(String) Helper.ParsePredicate(interactionLogic.ActionOptions[0])[
				Helper.GetTopPredicate(interactionLogic.ActionOptions[0])])[0];

		if (theme is GameObject) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((new Regex(@"put\([a-z\(\)]+,\{1\}\)")).IsMatch(interactionLogic.ActionOptions[0])) {
					RespondAndUpdate(string.Format("Where should I put {0}?",
						GenerateReferringExpression(theme as GameObject, availableObjs.Cast<object>().ToList())));
				}
				else if ((new Regex(@"slide\([a-z\(\)]+,\{1\}\)")).IsMatch(interactionLogic.ActionOptions[0])) {
					RespondAndUpdate(string.Format("Where should I move {0}?",
						GenerateReferringExpression(theme as GameObject, availableObjs.Cast<object>().ToList())));
				}

				if ((interactionLogic.IndicatedObj == null) && (interactionLogic.GraspedObj != (theme as GameObject))) {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol((theme as GameObject), null, null, null, null, null)));
				}
			}
		}
	}

	public void PlaceInRegion(object[] content) {
		RespondAndUpdate("OK.");

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(null, null, null, null,
				new List<string>(new string[] {
					string.Format("put({0},{1})", interactionLogic.IndicatedObj.name,
						Helper.VectorToParsable(interactionLogic.IndicatedRegion.center))
				}),
				null)));
	}

	public void DisambiguateEvent(object[] content) {
		LookForward();
		RespondAndUpdate(string.Format("Should I {0}?", confirmationTexts[
			interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]]));
	}

	public void ComposeObjectAndAction(object[] content) {
		if (interactionLogic.IndicatedObj != null) {
			interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
				interactionLogic.GenerateStackSymbol(null, null, null, null,
					new List<string>(new string[]
						{string.Format(interactionLogic.ActionOptions[0], interactionLogic.IndicatedObj.name)}),
					null)));
		}
		else if (interactionLogic.GraspedObj != null) {
			interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
				interactionLogic.GenerateStackSymbol(null, null, null, null,
					new List<string>(new string[]
						{string.Format(interactionLogic.ActionOptions[0], interactionLogic.GraspedObj.name)}),
					null)));
		}
	}

	public void ConfirmEvent(object[] content) {
		RespondAndUpdate("OK.");
		PromptEvent(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]);

		if (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "grasp")) {
			if (interactionLogic.IndicatedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
						interactionLogic.IndicatedObj, null, null, null, null)));
			}
		}
		else if (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "lift")) {
			if (interactionLogic.GraspedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					null));
			}
		}
		else {
			if (interactionLogic.IndicatedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
						null, null, null, null, null)));
			}
			else if (interactionLogic.GraspedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null,
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
						null, null, null, null)));
			}
		}
	}

	public void ExecuteEvent(object[] content) {
		if ((interactionLogic.ActionOptions.Count > 0) &&
		    ((Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "grasp")) ||
		     (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "lift")) ||
		     (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "put")))) {
			interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
		}

		//if (interactionLogic.GraspedObj != null)
		//{
		//    ReturnHandsToDefault();
		//}
	}

	public void StartGrab(object[] content) {
		// see if this object has a learned conventional grasp pose associated with it
		List<object> objs = new List<object>();
		if (interactionLogic.ActionOptions.Count > 0) {
            string actionCmd = interactionLogic.ActionOptions[0];
            eventManager.ParseCommand(actionCmd);
            eventManager.FinishSkolemization();
            string skolemized = eventManager.Skolemize(actionCmd);
            if (eventManager.EvaluateSkolemConstants(EventManager.EvaluationPass.Attributes)) {
                actionCmd = eventManager.ApplySkolems(skolemized);
            }
            
            objs = eventManager.ExtractObjects(Helper.GetTopPredicate(actionCmd),
                (String)Helper.ParsePredicate(actionCmd)[Helper.GetTopPredicate(actionCmd)]);

            //if ((obj != null) && (interactionLogic.ActionOptions.Count > 0)) {
            //    if (InteractionHelper.GetCloserHand(Diana, obj) == leftGrasper) {
            //        grabStr = interactionLogic.ActionOptions[0].Replace("*", "lHand");
            //    }
            //    else if (InteractionHelper.GetCloserHand(Diana, obj) == rightGrasper) {
            //        grabStr = interactionLogic.ActionOptions[0].Replace("*", "rHand");
            //    }
            //}

            if ((interactionLogic.IndicatedObj == null) && (interactionLogic.GraspedObj == null)) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol((objs[0] as GameObject), null, null, null, null, null)));
			}

			if (objs.Count > 1) {
				// the second would be a grasp pose
				if (objs[1] is GameObject) {
					if ((objs[0] as GameObject).GetComponent<Voxeme>() != null) {
						(objs[0] as GameObject).GetComponent<Voxeme>().graspConvention = objs[1] as GameObject;
					}
				}
			}
		}

		List<PDAStackOperation> learnedActionSymbols = interactionLogic.LearnableInstructions.Values.Where(op =>
			((op != null) &&
			 ((((StackSymbolContent) op.Content).GraspedObj as GameObject ==
			   interactionLogic.IndicatedObj as GameObject) ||
			  ((objs.Count > 0) && (((StackSymbolContent) op.Content).GraspedObj as GameObject == objs[0]))))).ToList();

		//Debug.Log(eventManager.ExtractObjects(
		//Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
		//(String)Helper.ParsePredicate(interactionLogic.ActionOptions[0])[
		//Helper.GetTopPredicate(interactionLogic.ActionOptions[0])])[0]);
		Debug.Log(learnedActionSymbols.Count);

		if ((interactionLogic.IndicatedObj != null) && (learnedActionSymbols.Count == 0)) {
			List<GameObject> grabPoses = GetGrabPoses(interactionLogic.IndicatedObj,
				InteractionHelper.GetCloserHand(Diana, interactionLogic.IndicatedObj));

			if (interactionLogic.IndicatedObj.GetComponent<Voxeme>().graspConvention == null) {
				if (grabPoses.Count > 1) {
					// disambiguate grasp pose
					// are all poses available? -> for now, is the default pose (0) available?
					// default pose is available if indicatedObj has no children who !FitsIn(child,indicatedObj,threeDimensional=true)
					bool conventionAvailable = true;
					List<GameObject> excludeChildren = interactionLogic.IndicatedObj.GetComponentsInChildren<Renderer>()
						.Where(
							o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != interactionLogic.IndicatedObj))
						.Select(v => v.gameObject).ToList();
					foreach (GameObject child in excludeChildren) {
						//Debug.Log(child);
						//Debug.Log(Helper.VectorToParsable(Helper.GetObjectWorldSize(child.gameObject).size));
						//Debug.Log(Helper.VectorToParsable(
						//Helper.GetObjectWorldSize(interactionLogic.IndicatedObj, excludeChildren).size));
						if (!Helper.FitsIn(Helper.GetObjectWorldSize(child.gameObject),
							Helper.GetObjectWorldSize(interactionLogic.IndicatedObj, excludeChildren), true)) {
							//Debug.Log(child);
							conventionAvailable = false;
						}
					}

					Debug.Log(conventionAvailable);
					if (!conventionAvailable) {
						List<GameObject> toRemove = grabPoses.Where(p => p.name.EndsWith("0")).ToList();
						foreach (GameObject pose in toRemove) {
							//Debug.Log(pose.name);
							grabPoses.Remove(pose);
						}
					}

					Debug.Log(grabPoses.Count);

					if (grabPoses.Count > 1) {
						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null, null, null,
								Enumerable.Range(0, grabPoses.Count).Select(s => string.Format("grasp({0},with({1}))",
									interactionLogic.IndicatedObj.name,
									grabPoses[s].name)).ToList(), null)));
					}
					else {
						string graspCmd = string.Format("grasp({0},with({1}))", interactionLogic.IndicatedObj.name,
							grabPoses[0].name);
						RespondAndUpdate("OK.");
						PromptEvent(string.Format(graspCmd, interactionLogic.IndicatedObj.name));

						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								interactionLogic.IndicatedObj, null, null, null, null)));
					}
				}
				else {
					RespondAndUpdate("OK.");
					PromptEvent(string.Format("grasp({0})", interactionLogic.IndicatedObj.name));

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(
							new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
							interactionLogic.IndicatedObj, null, null, null, null)));
				}
			}
			else {
				// is the default pose (0) available?
				// default pose is available if indicatedObj has no children who !FitsIn(child,indicatedObj,threeDimensional=true)
				bool conventionAvailable = true;
				List<GameObject> excludeChildren = interactionLogic.IndicatedObj.GetComponentsInChildren<Renderer>()
					.Where(
						o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != interactionLogic.IndicatedObj))
					.Select(v => v.gameObject).ToList();
				foreach (GameObject child in excludeChildren) {
					//Debug.Log(child);
					//Debug.Log(Helper.VectorToParsable(Helper.GetObjectWorldSize(child.gameObject).size));
					//Debug.Log(Helper.VectorToParsable(
					//Helper.GetObjectWorldSize(interactionLogic.IndicatedObj, excludeChildren).size));
					if (!Helper.FitsIn(Helper.GetObjectWorldSize(child.gameObject),
						Helper.GetObjectWorldSize(interactionLogic.IndicatedObj, excludeChildren), true)) {
						//Debug.Log(child);
						conventionAvailable = false;
					}
				}

				Debug.Log(conventionAvailable);
				if (!conventionAvailable) {
					List<GameObject> toRemove = grabPoses.Where(p => p.name.EndsWith("0")).ToList();
					foreach (GameObject pose in toRemove) {
						//Debug.Log(pose.name);
						grabPoses.Remove(pose);
					}

					if (grabPoses.Count > 1) {
						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null, null, null, null,
								Enumerable.Range(0, grabPoses.Count).Select(s => string.Format("grasp({0},with({1}))",
									interactionLogic.IndicatedObj.name,
									grabPoses[s].name)).ToList(), null)));
					}
					else {
						string graspCmd = string.Format("grasp({0},with({1}))", interactionLogic.IndicatedObj.name,
							grabPoses[0].name);
						RespondAndUpdate("OK.");
						PromptEvent(string.Format(graspCmd, interactionLogic.IndicatedObj.name));

						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								interactionLogic.IndicatedObj, null, null, null, null)));
					}
				}
				else {
					string graspCmd = string.Format("grasp({0},with({1}))", interactionLogic.IndicatedObj.name,
						interactionLogic.IndicatedObj.GetComponent<Voxeme>().graspConvention.name);
					RespondAndUpdate("OK.");
					PromptEvent(string.Format(graspCmd, interactionLogic.IndicatedObj.name));

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(
							new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
							interactionLogic.IndicatedObj, null, null, null, null)));
				}
			}
		}
		else {
			if (interactionLogic.IndicatedObj != null) {
				//if ((objs.Count > 0) && (interactionLogic.IndicatedObj == objs[0])) {
				string graspCmd = string.Format("grasp({0})", interactionLogic.IndicatedObj.name);
				if (learnedActionSymbols.Count == 1) {
					// should only have one (for now)
					graspCmd = ((List<string>) ((StackSymbolContent) learnedActionSymbols[0].Content).ActionOptions)[0];
				}

				RespondAndUpdate("OK.");
				PromptEvent(graspCmd);

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
						interactionLogic.IndicatedObj, null, null, null, null)));
			}
			else if (learnedActionSymbols.Count == 1) {
				RespondAndUpdate("OK.");
				PromptEvent(((List<string>) ((StackSymbolContent) learnedActionSymbols[0].Content).ActionOptions)[0]);

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
						objs[0], null, null, null, null)));
			}
			else if (interactionLogic.ActionOptions.Count > 0) {
				string grabStr = string.Empty;
				List<object> args = eventManager.ExtractObjects(
					Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
					(String) Helper.ParsePredicate(interactionLogic.ActionOptions[0])[
						Helper.GetTopPredicate(interactionLogic.ActionOptions[0])]);

				if ((args.Count > 0) && (args[0] is GameObject)) {
					if (InteractionHelper.GetCloserHand(Diana, (args[0] as GameObject)) == leftGrasper) {
						grabStr = interactionLogic.ActionOptions[0].Replace("*", "lHand");
					}
					else if (InteractionHelper.GetCloserHand(Diana, (args[0] as GameObject)) == rightGrasper) {
						grabStr = interactionLogic.ActionOptions[0].Replace("*", "rHand");
					}

					RespondAndUpdate("OK.");
					PromptEvent(grabStr);

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, args[0] as GameObject, null, null,
							new List<string>(), null)));
				}
				else if (!(args[0] is bool)) {
					RespondAndUpdate("OK.");
					PromptEvent(interactionLogic.ActionOptions[0]);

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, args[0] as GameObject, null, null, null, null)));
				}
				else {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(null, args[0] as GameObject, null, null, null, null)));
				}
			}
		}
	}

	public void DisambiguateGrabPose(object[] content) {
		RespondAndUpdate("Should I grasp it like this?", true);

		String eventPrompt = interactionLogic.ActionOptions[0];

		if (InteractionHelper.GetCloserHand(Diana, interactionLogic.IndicatedObj) == leftGrasper) {
			eventPrompt = eventPrompt.Replace("*", "lHand");
		}
		else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.IndicatedObj) == rightGrasper) {
			eventPrompt = eventPrompt.Replace("*", "rHand");
		}

		PromptEvent(eventPrompt);

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				interactionLogic.IndicatedObj, null, null, null, null)));
	}

	public void StartGrabMove(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				List<string> actionOptions = new List<string>();
				actionOptions.Add(content[0].ToString());

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null, null,
						actionOptions, null)));
				break;

			default:
				break;
		}
	}

	public void StopGrabMove(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] == null) {
					Debug.Log(interactionLogic.StackSymbolToString(interactionLogic.CurrentStackSymbol));
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionSuggestions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionSuggestions)[0];
					}
					else {
						return;
					}
				}
				else {
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionOptions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionOptions)[0];
					}
					else {
						return;
					}
				}

				Debug.Log(message);
				string dir = string.Empty;

				if (interactionLogic.GetInputSymbolType(message) == 'G') {
					dir = interactionLogic.GetGestureContent(
						interactionLogic.RemoveInputSymbolType(
							interactionLogic.RemoveGestureTrigger(
								message, interactionLogic.GetGestureTrigger(message)),
							interactionLogic.GetInputSymbolType(message)),
						"grab move").ToLower();
				}
				else if (interactionLogic.GetInputSymbolType(message) == 'S') {
					dir = interactionLogic.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
						.ToLower();
				}

				Debug.Log(dir);
				List<string> options = PopulateMoveOptions(interactionLogic.GraspedObj, dir);

//			interactionLogic.RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite,
//				Enumerable.Range(0,options.Count).Select(s => interactionLogic.GenerateStackSymbol (null,
//					dir == "up" ? null : new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null, 
//					options.ToArray().Reverse().ToList().GetRange(0,s+1), new List<string>())).ToList()));
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					Enumerable.Range(0, options.Count).Select(s => interactionLogic.GenerateStackSymbol(null, null,
						null, null,
						options.ToArray().Reverse().ToList().GetRange(0, s + 1), new List<string>())).ToList()));
				break;

			default:
				break;
		}
	}

	public void StopGrab(object[] content) {
		RespondAndUpdate("OK.");
		PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(null, new FunctionDelegate(interactionLogic.NullObject),
				null, null, null, null)));
	}

	public void StartPush(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				List<string> actionOptions = new List<string>();
				actionOptions.Add(content[0].ToString());

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null, null,
						actionOptions, null)));
				break;

			default:
				break;
		}
	}

	public void StopPush(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] == null) {
					//Debug.Log (interactionLogic.StackSymbolToString (interactionLogic.CurrentStackSymbol));
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionSuggestions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionSuggestions)[0];
					}
				}
				else {
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionOptions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionOptions)[0];
					}
				}

				Debug.Log(message);
				string dir = string.Empty;

				if (message != null) {
					if (interactionLogic.GetInputSymbolType(message) == 'G') {
						dir = interactionLogic.GetGestureContent(
							interactionLogic.RemoveInputSymbolType(
								interactionLogic.RemoveGestureTrigger(
									message, interactionLogic.GetGestureTrigger(message)),
								interactionLogic.GetInputSymbolType(message)),
							"push").ToLower();
					}
					else if (interactionLogic.GetInputSymbolType(message) == 'S') {
						dir = interactionLogic.humanRelativeDirections
							? interactionLogic
								.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message)).ToLower()
							: oppositeDir[
								interactionLogic
									.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
									.ToLower()];
					}
				}
				else {
					dir = interactionLogic.ActionOptions[0].Split(',')[1].TrimEnd(')');
				}

				Debug.Log(dir);
				List<string> options = PopulatePushOptions(
					(interactionLogic.GraspedObj == null) ? interactionLogic.IndicatedObj : interactionLogic.GraspedObj,
					dir);

				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					Enumerable.Range(0, options.Count).Select(s => interactionLogic.GenerateStackSymbol(null,
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null,
						options.ToArray().Reverse().ToList().GetRange(0, s + 1), new List<string>())).ToList()));
				break;

			default:
				break;
		}
	}

	public void StartServo(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				if (content[0] != null) {
					List<string> actions = new List<string>();
					actions.Add(content[0].ToString());

					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						interactionLogic.GenerateStackSymbol(
							new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
							interactionLogic.IndicatedObj, null, null,
							null, actions)));
				}

				break;

			default:
				break;
		}
	}

	public void Servo(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = null;

				if (content[0] == null) {
					if (((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
						    .ActionSuggestions).Count > 0) {
						message = ((List<string>) ((StackSymbolContent) interactionLogic.CurrentStackSymbol.Content)
							.ActionSuggestions)[0];
					}
				}
				//else {
				//    if (((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions).Count > 0) {
				//        message = ((List<string>)((StackSymbolContent)interactionLogic.CurrentStackSymbol.Content).ActionOptions)[0];
				//    }
				//}

				Debug.Log(message);
				string dir = string.Empty;

				if (message != null) {
					if (interactionLogic.GetInputSymbolType(message) == 'G') {
						dir = interactionLogic.GetGestureContent(
							interactionLogic.RemoveInputSymbolType(
								interactionLogic.RemoveGestureTrigger(
									message, interactionLogic.GetGestureTrigger(message)),
								interactionLogic.GetInputSymbolType(message)),
							"push servo").ToLower();
					}
					else if (interactionLogic.GetInputSymbolType(message) == 'S') {
						dir = interactionLogic.humanRelativeDirections
							? interactionLogic
								.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message)).ToLower()
							: oppositeDir[
								interactionLogic
									.RemoveInputSymbolType(message, interactionLogic.GetInputSymbolType(message))
									.ToLower()];
					}
				}
				else {
					dir = interactionLogic.ActionOptions[0].Split(',')[1].TrimEnd(')');
				}

				Debug.Log(dir);

				GameObject obj = (interactionLogic.GraspedObj == null)
					? interactionLogic.IndicatedObj
					: interactionLogic.GraspedObj;

				Vector3 destCoord = obj.transform.position + (directionVectors[oppositeDir[dir]] * servoSpeed);
				Debug.Log(Helper.VectorToParsable(obj.transform.position));
				Debug.Log(Helper.VectorToParsable(destCoord));

				string eventStr = string.Format("slidep({0},{1})", obj.name, Helper.VectorToParsable(destCoord));

				//interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
				//interactionLogic.GenerateStackSymbol(null, null, new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null,
				//new List<string>() { eventStr }, null)));

				if (eventManager.events.Count == 0) {
					PromptEvent(eventStr);
				}
				else {
					eventManager.InsertEvent(eventStr, 1);
					eventManager.events.RemoveAt(1);
				}

				break;

			default:
				break;
		}
	}

	public void StopServo(object[] content) {
		RespondAndUpdate("OK.");
		eventManager.ClearEvents();

		if (interactionLogic.GraspedObj != null) {
			PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
		}

		LookForward();
		TurnForward();

		if (interactionLogic.GraspedObj != null) {
			ReturnHandsToDefault();
		}

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(null,
				new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				null, null, new List<string>(), new List<string>())));
	}

	public void PromptLearn(object[] content) {
		RespondAndUpdate("What's the gesture for that?");
		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void StartLearn(object[] content) {
		if ((ksimSocket != null) && (ksimSocket.IsConnected())) {
			string command = "learn";
			byte[] bytes = new byte[] {0x03}.Concat(new byte[] {0x01}).Concat(BitConverter.GetBytes(64 | 128))
				.Concat(BitConverter.GetBytes(command.Length)).Concat(Encoding.ASCII.GetBytes(command)).ToArray<byte>();
			ksimSocket.Write(BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray<byte>());
		}
	}

	public void LearningSucceeded(object[] content) {
		String eventPrompt = interactionLogic.ActionOptions[0];

		if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
			eventPrompt = eventPrompt.Replace("*", "lHand");
		}
		else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == rightGrasper) {
			eventPrompt = eventPrompt.Replace("*", "rHand");
		}

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(new StackSymbolContent(null, null, null, null,
				new List<string>() {eventPrompt}, null))));
	}

	public void LearnNewInstruction(object[] content) {
		// type check
		if (!Helper.CheckAllObjectsOfType(content, typeof(string))) {
			return;
		}

		switch (content.Length) {
			case 0:
				break;

			case 1:
				string message = content[0].ToString();

				Debug.Log(message);
				Debug.Log(interactionLogic.GetLearnableInstructionKeyByName(message));
				Debug.Log(string.Format("[{0}]",
					string.Join(",",
						interactionLogic.GetLearnableInstructionKeyByName(message).Select(k => k.Name).ToArray())));
				Debug.Log(interactionLogic.LearnableInstructions.ContainsKey(
					interactionLogic.GetLearnableInstructionKeyByName(message)));

				if (interactionLogic.LearnableInstructions.ContainsKey(
					interactionLogic.GetLearnableInstructionKeyByName(message))) {
					interactionLogic.LearnableInstructions[interactionLogic.GetLearnableInstructionKeyByName(message)] =
						new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
							new StackSymbolContent(null, interactionLogic.GraspedObj, null, null,
								new List<string>() {interactionLogic.ActionOptions[0]}, null));

					foreach (List<PDASymbol> key in interactionLogic.LearnableInstructions.Keys) {
						if (interactionLogic.LearnableInstructions[key] != null) {
							Debug.Log(string.Format("[{0}],{1}", string.Join(",", key.Select(k => k.Name).ToArray()),
								interactionLogic.StackSymbolToString(
									interactionLogic.LearnableInstructions[key].Content)));
						}
					}

					string learnedInstructionString = string.Format("{0} the {1}",
						Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
						((GameObject) eventManager.ExtractObjects(
							Helper.GetTopPredicate(interactionLogic.ActionOptions[0]),
							(String) Helper.ParsePredicate(interactionLogic.ActionOptions[0])[
								Helper.GetTopPredicate(interactionLogic.ActionOptions[0])])[0]).name);
					RespondAndUpdate(string.Format("Got it!  Do that with your {0} and I'll {1} like this.",
						message.Contains("lh") ? "left hand" : "right hand", learnedInstructionString));

					interactionLogic.OnLearnedNewInstruction(this, new NewInstructionEventArgs(message));
				}

				break;

			default:
				break;
		}

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void LearningFailed(object[] content) {
		//LookForward();
		//TurnForward();

		//if (interactionLogic.GraspedObj != null) {
		//    PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
		//    //ReturnHandsToDefault();
		//    ReachFor(interactionLogic.GraspedObj);
		//}

		String eventPrompt = interactionLogic.ActionOptions[0];

		RespondAndUpdate("Sorry, could you show me again?", true);

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(null, null,
				null, null, new List<string>() {eventPrompt}, null)));
	}

	public void RetryLearn(object[] content) {
		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void AbortAction(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			//if (interactionLogic.ActionOptions.Count > 0) {
			//if ((Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "lift")) ||
			//(Regex.IsMatch (interactionLogic.ActionOptions [interactionLogic.ActionOptions.Count - 1], "put"))) {
			RaycastHit hitInfo;

			if ((!Physics.Raycast(new Ray(interactionLogic.GraspedObj.transform.position, Vector3.down),
				    out hitInfo)) ||
			    (hitInfo.collider.gameObject == demoSurfaceCollider.gameObject)) {
				PromptEvent(string.Format("put({0},{1})",
					interactionLogic.GraspedObj.name,
					Helper.VectorToParsable(new Vector3(interactionLogic.GraspedObj.transform.position.x,
						Helper.GetObjectWorldSize(demoSurface).max.y +
						Helper.GetObjectWorldSize(interactionLogic.GraspedObj).extents.y,
						interactionLogic.GraspedObj.transform.position.z))));
			}
			else {
				PromptEvent(string.Format("put({0},on({1}))",
					interactionLogic.GraspedObj.name,
					Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject).name));
			}

			//}
			//else {
			//	PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			//                ReturnHandsToDefault();
			//}
			//}
			//else {
			//	PromptEvent (string.Format ("ungrasp({0})", interactionLogic.GraspedObj.name));
			//             ReturnHandsToDefault();
			//}
		}
		else {
			LookForward();
			TurnForward();
		}

		//if (interactionLogic.GraspedObj != null) {
		ReturnHandsToDefault();
		//}

		RespondAndUpdate("OK, never mind.");

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void ObjectUnavailable(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "lift")) ||
				    (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "put")) ||
				    (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1],
					    "grab move"))) {
					PromptEvent(string.Format("put({0},{1})",
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable(new Vector3(interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize(demoSurface).max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
				else {
					PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward();
			TurnForward();
		}

		RespondAndUpdate("Sorry, I can't find an object like that that I can use.");

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void Confusion(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (interactionLogic.ActionOptions.Count > 0) {
				if ((Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "lift")) ||
				    (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1], "put")) ||
				    (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1],
					    "grab move"))) {
					// why is this here? shouldn't all action options be in predicate form?
					PromptEvent(string.Format("put({0},{1})",
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable(new Vector3(interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize(demoSurface).max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
				else {
					PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
				}
			}
			else {
				PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			if (interactionLogic.IndicatedObj != null) {
				if ((interactionLogic.ActionOptions.Count > 0) &&
				    (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1],
					    "grasp"))) {
					PromptEvent(string.Format("ungrasp({0})", interactionLogic.IndicatedObj.name));
				}
			}

			LookForward();
			TurnForward();
		}

		int choice = RandomHelper.RandomInt(0, 3);

		switch (choice) {
			case 1:
				RespondAndUpdate("Sorry, I'm confused.");
				break;

			case 2:
				RespondAndUpdate("Sorry, I don't understand.");
				break;

			default:
				RespondAndUpdate("Sorry, I don't know what you mean.");
				break;
		}

		if (interactionLogic.GraspedObj == null) {
			ReturnHandsToDefault();
		}

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
	}

	public void CleanUp(object[] content) {
		if (interactionLogic.GraspedObj != null) {
			if (!RCC8.EC(Helper.GetObjectWorldSize(interactionLogic.GraspedObj),
				Helper.GetObjectWorldSize(interactionLogic.GraspedObj.GetComponent<Voxeme>().supportingSurface))) {
				RaycastHit hitInfo;

				if ((!Physics.Raycast(new Ray(interactionLogic.GraspedObj.transform.position, Vector3.down),
					    out hitInfo)) ||
				    (hitInfo.collider.gameObject == demoSurfaceCollider.gameObject)) {
					PromptEvent(string.Format("put({0},{1})",
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable(new Vector3(interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize(demoSurface).max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
				else {
					PromptEvent(string.Format("put({0},{1})",
						interactionLogic.GraspedObj.name,
						Helper.VectorToParsable(new Vector3(interactionLogic.GraspedObj.transform.position.x,
							Helper.GetObjectWorldSize(Helper.GetMostImmediateParentVoxeme(hitInfo.collider.gameObject))
								.max.y,
							interactionLogic.GraspedObj.transform.position.z))));
				}
			}
			else {
				PromptEvent(string.Format("ungrasp({0})", interactionLogic.GraspedObj.name));
			}
		}
		else {
			LookForward();
			TurnForward();
		}

		//eventManager.ClearEvents();

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(null,
				new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null,
				null, null, null)));
	}

	public void EndState(object[] content) {
		ReturnHandsToDefault();
		eventManager.referents.stack.Clear();

		Dictionary<List<PDASymbol>, PDAStackOperation> temp = new Dictionary<List<PDASymbol>, PDAStackOperation>();
		foreach (List<PDASymbol> key in interactionLogic.LearnableInstructions.Keys) {
			temp.Add(key, null);
		}

		interactionLogic.LearnableInstructions = temp;

		foreach (GameObject obj in availableObjs) {
			Voxeme voxeme = obj.GetComponent<Voxeme>();
			if (voxeme != null) {
				voxeme.graspConvention = null;
			}
		}

		epistemicModel.SaveUserModel(epistemicModel.userID);
		RespondAndUpdate("Bye!");

		interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
			interactionLogic.GenerateStackSymbol(new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
				new List<GameObject>(), new List<string>(), new List<string>())));
	}

	void AttentionShift(object sender, EventArgs e) {
		if (((AttentionShiftEventArgs) e).Symbol != null) {
			string symbol = ((AttentionShiftEventArgs) e).Symbol.Name;
			Debug.Log(string.Format("Attention symbol: {0}", symbol));
			symbol = interactionLogic.RemoveInputSymbolType(symbol, interactionLogic.GetInputSymbolType(symbol));
			if (symbol.StartsWith("inattentive")) {
				interactionLogic.attentionStatus = CharacterLogicAutomaton.AttentionStatus.Inattentive;
				if (symbol.EndsWith("left")) {
					Debug.Log("Looking left");
					LookAt(new Vector3(headTargetDefault.x + 2.0f, headTargetDefault.y, headTargetDefault.z));
				}
				else if (symbol.EndsWith("right")) {
					Debug.Log("Looking right");
					LookAt(new Vector3(headTargetDefault.x - 2.0f, headTargetDefault.y, headTargetDefault.z));
				}
			}
			else if (symbol.StartsWith("attentive")) {
				interactionLogic.attentionStatus = CharacterLogicAutomaton.AttentionStatus.Attentive;
				Debug.Log("Looking forward");
				if (interactionLogic.CurrentState.Name != "BeginInteraction") {
					RespondAndUpdate("");
				}

				LookForward();
			}
		}
	}

	public void MoveToPerform() {
		bool leftGrasping = false;
		bool rightGrasping = false;

		if (graspedObj != null) {
			if (InteractionHelper.GetCloserHand(Diana, graspedObj) == leftGrasper) {
				leftGrasping = true;
			}
			else if (InteractionHelper.GetCloserHand(Diana, graspedObj) == rightGrasper) {
				rightGrasping = true;
			}
		}

		if (!leftGrasping) {
			Diana.GetComponent<FullBodyBipedIK>().solver.GetEffector(FullBodyBipedEffector.LeftHand).positionWeight =
				0.0f;
			Diana.GetComponent<FullBodyBipedIK>().solver.GetEffector(FullBodyBipedEffector.LeftHand).rotationWeight =
				0.0f;
		}

		if (!rightGrasping) {
			Diana.GetComponent<FullBodyBipedIK>().solver.GetEffector(FullBodyBipedEffector.RightHand).positionWeight =
				0.0f;
			Diana.GetComponent<FullBodyBipedIK>().solver.GetEffector(FullBodyBipedEffector.RightHand).rotationWeight =
				0.0f;
		}

		LookForward();
	}

	public void TurnForward() {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
	}

	public void LookForward() {
		Diana.GetComponent<LookAtIK>().solver.target.position = headTargetDefault;
		Diana.GetComponent<LookAtIK>().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK>().solver.bodyWeight = 0.8f;
		Diana.GetComponent<LookAtIK>().solver.headWeight = 0.0f;
	}

	public void AllowHeadMotion() {
		Diana.GetComponent<LookAtIK>().solver.IKPositionWeight = 0.0f;
		Diana.GetComponent<LookAtIK>().solver.bodyWeight = 0.8f;
	}

	//void TrackPointing(List<float> vector) {
	//	highlightTimeoutTimer.Enabled = true;
	//	regionHighlight.GetComponent<Renderer> ().material.color = new Color (0.0f, 1.0f, 0.0f,
	//		regionHighlight.GetComponent<Renderer> ().material.color.a);

	//	if (eventManager.events.Count > 0) {
	//		return;
	//	}

	//	// TODO: output timeout timer
	//	//		if ((indicatedObj == null) && (graspedObj == null)) {
	//	//			OutputHelper.PrintOutput (Role.Affector, "");
	//	//		}

	//	Region region = null;

	//	highlightCenter = TransformToSurface (vector);

	//	//		Debug.Log (string.Format("({0},{1};{2},{3})",vector[0],vector[1],vector[2],vector[4]));
	//	//Debug.Log (highlightCenter);

	//	// jump from origin on first update
	//	if (regionHighlight.transform.position.sqrMagnitude <= Constants.EPSILON) {
	//		MoveHighlight (highlightCenter);
	//		regionHighlight.transform.position = highlightCenter;
	//	}

	//	if ((regionHighlight.transform.position - highlightCenter).magnitude > highlightQuantum) {
	//		Vector3 offset = MoveHighlight (highlightCenter);

	//		if (offset.sqrMagnitude <= Constants.EPSILON) {
	//			regionHighlight.transform.position = highlightCenter;
	//		}
	//	}

	//	//		Vector3 origin = new Vector3 (vector [0], Helper.GetObjectWorldSize (demoSurface).max.y, vector [1]);
	//	//		Ray ray = new Ray(origin,
	//	//				new Vector3(vector[2]*vectorScaleFactor.x,Camera.main.transform.position.y,vector[4])-origin);
	//	//
	//	//		//float height = 2.0 * Mathf.Tan(0.5 * Camera.main.fieldOfView * Mathf.Deg2Rad) * Camera.main.nearClipPlane;
	//	//		//float width = height * Screen.width / Screen.height;
	//	//		//Vector3 cameraOrigin = Camera.main.ScreenToWorldPoint (0.0f, 0.0f, Camera.main.nearClipPlane);
	//	//		Plane cameraPlane = new Plane(Camera.main.ScreenToWorldPoint (new Vector3(0.0f, 0.0f, Camera.main.nearClipPlane)),
	//	//			Camera.main.ScreenToWorldPoint (new Vector3(0.0f, Screen.height, Camera.main.nearClipPlane)),
	//	//			Camera.main.ScreenToWorldPoint (new Vector3(Screen.width, Screen.height, Camera.main.nearClipPlane)));
	//	//
	//	//		float distance;
	//	//		if (cameraPlane.Raycast (ray, out distance)) {
	//	//			Vector3 screenPoint = Camera.main.WorldToScreenPoint (ray.GetPoint (distance));
	//	//			Debug.Log(string.Format("{0};{1}",ray.GetPoint (distance),screenPoint));
	//	//		}

	//	//TurnForward ();
	//	//LookAt (cube.transform.position);
	//}

	Vector3 MoveHighlight(Vector3 highlightCenter, float variance = 0.0f) {
		Vector3 offset = regionHighlight.transform.position - highlightCenter;
		Vector3 normalizedOffset = Vector3.Normalize(offset);

		regionHighlight.transform.position = new Vector3(
			regionHighlight.transform.position.x - normalizedOffset.x * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.y - normalizedOffset.y * Time.deltaTime * highlightMoveSpeed,
			regionHighlight.transform.position.z - normalizedOffset.z * Time.deltaTime * highlightMoveSpeed);

		Vector3 normalizedScaleOffset = Vector3.Normalize(new Vector3(
			regionHighlight.transform.localScale.x - vectorConeRadius * (.2f + 10.0f * variance),
			regionHighlight.transform.localScale.y - vectorConeRadius * (.2f + 10.0f * variance),
			regionHighlight.transform.localScale.z - vectorConeRadius * (.2f + 10.0f * variance)));

		varianceScaleFactor = regionHighlight.transform.localScale;

		regionHighlight.transform.localScale = new Vector3(
			regionHighlight.transform.localScale.x - normalizedScaleOffset.x * Time.deltaTime * highlightOscSpeed,
			regionHighlight.transform.localScale.y - normalizedScaleOffset.y * Time.deltaTime * highlightOscSpeed,
			regionHighlight.transform.localScale.z - normalizedScaleOffset.z * Time.deltaTime * highlightOscSpeed);

		if ((regionHighlight.transform.position.x + vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.x) ||
		    (regionHighlight.transform.position.x - vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.x) ||
		    (regionHighlight.transform.position.z + vectorConeRadius < Helper.GetObjectWorldSize(demoSurface).min.z) ||
		    (regionHighlight.transform.position.z - vectorConeRadius > Helper.GetObjectWorldSize(demoSurface).max.z)) {
			// hide region highlight
			regionHighlight.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f,
				(1.0f / ((regionHighlight.transform.position -
				          new Vector3(demoSurface.transform.position.x, Helper.GetObjectWorldSize(demoSurface).max.y,
					          demoSurface.transform.position.z)).magnitude + Constants.EPSILON)) *
				regionHighlight.transform.position.y);
			//Debug.Log ("=======" + regionHighlight.GetComponent<Renderer> ().material.color.a);
		}
		else {
			regionHighlight.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		}

		return offset;
	}

	void PopulateOptions(string program, GameObject theme, string dir) {
		switch (program) {
			case "grasp":
				if (graspedObj == null) {
					PopulateGrabOptions(theme);
				}

				break;

			case "put":
				PopulateMoveOptions(theme, dir);
				break;

			case "slide":
				PopulatePushOptions(theme, dir);
				break;

			default:
				break;
		}
	}

	void PopulateGrabOptions(GameObject theme, CertaintyMode certainty = CertaintyMode.Act) {
		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme>() != null) {
			themeAttr = theme.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value; // just grab the first one for now
		}

		if (certainty == CertaintyMode.Act) {
			if (!actionOptions.Contains(string.Format("grasp({0})", theme.name))) {
				actionOptions.Add(string.Format("grasp({0})", theme.name));
			}

			if (!confirmationTexts.ContainsKey(string.Format("grasp({0})", theme.name))) {
				confirmationTexts.Add(string.Format("grasp({0})", theme.name),
					string.Format("grab the {0} block", themeAttr));
			}
		}
		else if (certainty == CertaintyMode.Suggest) {
			if (!suggestedActions.Contains(string.Format("grasp({0})", theme.name))) {
				suggestedActions.Add(string.Format("grasp({0})", theme.name));
			}

			if (!confirmationTexts.ContainsKey(string.Format("grasp({0})", theme.name))) {
				confirmationTexts.Add(string.Format("grasp({0})", theme.name),
					string.Format("grab the {0} block", themeAttr));
			}
		}
	}

	List<GameObject> FindObjectsInRegion(Region region) {
		List<GameObject> objOptions = new List<GameObject>();

		foreach (GameObject obj in availableObjs) {
			bool isKnown = true;

			if (dianaMemory != null && dianaMemory.enabled) {
				isKnown = dianaMemory.IsKnown(obj.GetComponent<Voxeme>());
			}

			if ((obj.activeInHierarchy) || (objSelector.disabledObjects.Contains(obj))) {
				Vector3 point = Helper.GetObjectWorldSize(obj).ClosestPoint(highlightCenter);
				if (region.Contains(new Vector3(point.x, region.center.y, point.z))) {
					if ((!objOptions.Contains(obj)) && (SurfaceClear(obj)) && (isKnown) &&
					    (obj != interactionLogic.IndicatedObj) && (obj != interactionLogic.GraspedObj)) {
						objOptions.Add(obj);
					}
				}
				else {
					if ((objOptions.Contains(obj)) && (isKnown)) {
						objOptions.Remove(obj);
					}
				}
			}
		}

		return objOptions;
	}

	List<string> PopulateMoveOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<string> moveOptions = new List<string>();
		List<object> placementOptions = FindPlacementOptions(theme, dir);

		if (interactionLogic.useOrderingHeuristics) {
			List<GameObject> objectPlacements = placementOptions.OfType<GameObject>().ToList();

			objectPlacements = objectPlacements.OrderByDescending(o => o.transform.position.y)
				.ThenBy(o => (o.transform.position - theme.transform.position).magnitude).ToList();

			for (int i = 0; i < placementOptions.Count; i++) {
				if (placementOptions[i] is GameObject) {
					placementOptions[i] = objectPlacements[i];
				}
			}
		}

		List<Region> orthogonalRegions = new List<Region>();
		if (dir == "left") {
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
		}
		else if (dir == "right") {
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
		}
		else if (dir == "front") {
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
		}
		else if (dir == "back") {
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
		}

		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme>() != null) {
			themeAttr = theme.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value; // just grab the first one for now
		}

		foreach (object option in placementOptions) {
			if (option is GameObject) {
				GameObject obj = (option as GameObject);
				if (theme != obj) {
					if (SurfaceClear(obj)) {
						string objAttr = string.Empty;
						if (obj.GetComponent<Voxeme>() != null) {
							objAttr = obj.GetComponent<Voxeme>().voxml.Attributes.Attrs[0]
								.Value; // just grab the first one for now
						}

						if (certainty == CertaintyMode.Act) {
							if (!moveOptions.Contains(string.Format("put({0},on({1}))", theme.name, obj.name))) {
								moveOptions.Add(string.Format("put({0},on({1}))", theme.name, obj.name));
							}

							if (!confirmationTexts.ContainsKey(string.Format("put({0},on({1}))", theme.name, obj.name))
							) {
								//confirmationTexts.Add (string.Format ("put({0},on({1}))", theme.name, obj.name),
								//	string.Format ("put the {0} block on the {1} block", themeAttr, objAttr));
								confirmationTexts.Add(string.Format("put({0},on({1}))", theme.name, obj.name),
									string.Format("put {0} on {1}",
										GenerateReferringExpression(theme, new List<object>() {obj}),
										GenerateReferringExpression(obj, new List<object>() {theme})));
							}
						}
						else if (certainty == CertaintyMode.Suggest) {
							if (!suggestedActions.Contains(string.Format("put({0},on({1}))", theme.name, obj.name))) {
								suggestedActions.Add(string.Format("put({0},on({1}))", theme.name, obj.name));
							}

							if (!confirmationTexts.ContainsKey(string.Format("put({0},on({1}))", theme.name, obj.name))) {
								confirmationTexts.Add(string.Format("put({0},on({1}))", theme.name, obj.name),
									string.Format("put {0} on {1}",
										GenerateReferringExpression(theme, new List<object>() {obj}),
										GenerateReferringExpression(obj, new List<object>() {theme})));
							}
						}
					}
				}
			}
			else if (option is Vector3) {
				Vector3 target = (Vector3) option;

				if (certainty == CertaintyMode.Act) {
					if (!moveOptions.Contains(string.Format("put({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						moveOptions.Add(string.Format("put({0},{1})", theme.name,
							Helper.VectorToParsable(target)));
					}

					foreach (Region region in orthogonalRegions) {
						if (region.Contains(target)) {
							if (!confirmationTexts.ContainsKey(string.Format("put({0},{1})", theme.name,
								Helper.VectorToParsable(target)))) {
								confirmationTexts.Add(
									string.Format("put({0},{1})", theme.name, Helper.VectorToParsable(target)),
									string.Format("put {0} in the table's {1} {2} part",
										GenerateReferringExpression(theme, new List<object>()),
										regionLabels[region], dir));
							}
						}
					}
				}
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains(string.Format("put({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						suggestedActions.Add(string.Format("put({0},{1})", theme.name,
							Helper.VectorToParsable(target)));
					}

					foreach (Region region in orthogonalRegions) {
						if (region.Contains(target)) {
							if (!confirmationTexts.ContainsKey(string.Format("put({0},{1})", theme.name,
								Helper.VectorToParsable(target)))) {
								confirmationTexts.Add(
									string.Format("put({0},{1})", theme.name, Helper.VectorToParsable(target)),
									string.Format("put {0} in the table's {1} {2} part",
										GenerateReferringExpression(theme, new List<object>()),
										regionLabels[region], dir));
							}
						}
					}
				}
			}
		}

		if (dir == "up") {
			if (certainty == CertaintyMode.Act) {
				if (!moveOptions.Contains(string.Format("lift({0})", theme.name))) {
					moveOptions.Add(string.Format("lift({0})", theme.name));
				}

				if (!confirmationTexts.ContainsKey(string.Format("lift({0})", theme.name))) {
					confirmationTexts.Add(string.Format("lift({0})", theme.name),
						string.Format("lift {0}", GenerateReferringExpression(theme, new List<object>())));
				}
			}
			else if (certainty == CertaintyMode.Suggest) {
				if (!suggestedActions.Contains(string.Format("lift({0})", theme.name))) {
					suggestedActions.Add(string.Format("lift({0})", theme.name));
				}

				if (!confirmationTexts.ContainsKey(string.Format("lift({0})", theme.name))) {
					confirmationTexts.Add(string.Format("lift({0})", theme.name),
						string.Format("lift {0}", GenerateReferringExpression(theme, new List<object>())));
				}
			}
		}
		else if (dir == "down") {
			if (eventConfirmation == "") {
				Vector3 target = new Vector3(theme.transform.position.x,
					Helper.GetObjectWorldSize(demoSurface).max.y,
					theme.transform.position.z);

				if (certainty == CertaintyMode.Act) {
					if (!moveOptions.Contains(
						string.Format("put({0},{1})", theme.name, Helper.VectorToParsable(target)))) {
						moveOptions.Add(string.Format("put({0},{1})", theme.name,
							Helper.VectorToParsable(target)));
					}

					if (!confirmationTexts.ContainsKey(string.Format("put({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						confirmationTexts.Add(string.Format("put({0},{1})", theme.name,
								Helper.VectorToParsable(target)),
							string.Format("put {0} down", GenerateReferringExpression(theme, new List<object>())));
					}
				}
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains(string.Format("put({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						suggestedActions.Add(string.Format("put({0},{1})", theme.name,
							Helper.VectorToParsable(target)));
					}

					if (!confirmationTexts.ContainsKey(string.Format("put({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						confirmationTexts.Add(string.Format("put({0},{1})", theme.name,
								Helper.VectorToParsable(target)),
							string.Format("put {0} down", GenerateReferringExpression(theme, new List<object>())));
					}
				}
			}
		}

		return moveOptions;
	}

	List<string> PopulatePushOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<string> pushOptions = new List<string>();
		List<object> placementOptions = FindPlacementOptions(theme, dir);

		Debug.Log(string.Format("{0} placement options", placementOptions.Count));
		foreach (object po in placementOptions) {
			if (po.GetType() == typeof(GameObject)) {
				Debug.Log((po as GameObject));
			}
		}

		if (interactionLogic.useOrderingHeuristics) {
			List<GameObject> objectPlacements = placementOptions.OfType<GameObject>().ToList();

			objectPlacements = objectPlacements
				.OrderBy(o => (o.transform.position - theme.transform.position).magnitude).ToList();

			for (int i = 0; i < placementOptions.Count; i++) {
				if (placementOptions[i] is GameObject) {
					placementOptions[i] = objectPlacements[i];
				}
			}
		}

		List<Region> orthogonalRegions = new List<Region>();
		if (dir == "left") {
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
		}
		else if (dir == "right") {
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
		}
		else if (dir == "front") {
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
		}
		else if (dir == "back") {
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
		}

		string themeAttr = string.Empty;
		if (theme.GetComponent<Voxeme>() != null) {
			themeAttr = theme.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value; // just grab the first one for now
		}

		foreach (object option in placementOptions) {
			if (option is GameObject) {
				GameObject obj = (option as GameObject);
				if (theme != obj) {
					if (FitsTouching(theme, obj, directionPreds[relativeDir[dir]]) &&
					    (Helper.GetObjectWorldSize(theme).min.y >=
					     Helper.GetObjectWorldSize(obj).min.y - Constants.EPSILON)) {
						// must fit in target destination and be on the same surface
						string objAttr = string.Empty;
						if (obj.GetComponent<Voxeme>() != null) {
							objAttr = obj.GetComponent<Voxeme>().voxml.Attributes.Attrs[0]
								.Value; // just grab the first one for now
						}

						if (certainty == CertaintyMode.Act) {
							if (!pushOptions.Contains(string.Format("slide({0},{1}({2}))", theme.name,
								directionPreds[relativeDir[dir]], obj.name))) {
								pushOptions.Add(string.Format("slide({0},{1}({2}))", theme.name,
									directionPreds[relativeDir[dir]], obj.name));
							}

							if (!confirmationTexts.ContainsKey(string.Format("slide({0},{1}({2}))", theme.name,
								directionPreds[relativeDir[dir]], obj.name))) {
								confirmationTexts.Add(
									string.Format("slide({0},{1}({2}))", theme.name, directionPreds[relativeDir[dir]],
										obj.name),
									string.Format("push {0} {1} {2}",
										GenerateReferringExpression(theme, new List<object>() {obj}),
										directionLabels[relativeDir[dir]],
										GenerateReferringExpression(obj, new List<object>() {theme})));
							}
						}
						else if (certainty == CertaintyMode.Suggest) {
							if (!suggestedActions.Contains(string.Format("slide({0},{1}({2}))", theme.name,
								directionPreds[relativeDir[dir]], obj.name))) {
								suggestedActions.Add(string.Format("slide({0},{1}({2}))", theme.name,
									directionPreds[relativeDir[dir]], obj.name));
							}

							if (!confirmationTexts.ContainsKey(string.Format("slide({0},{1}({2}))", theme.name,
								directionPreds[relativeDir[dir]], obj.name))) {
								confirmationTexts.Add(
									string.Format("slide({0},{1}({2}))", theme.name, directionPreds[relativeDir[dir]],
										obj.name),
									string.Format("push {0} {1} {2}",
										GenerateReferringExpression(theme, new List<object>() {obj}),
										directionLabels[relativeDir[dir]],
										GenerateReferringExpression(obj, new List<object>() {theme})));
							}
						}
					}
					else {
						if (!FitsTouching(theme, obj, directionPreds[relativeDir[dir]])) {
							Debug.Log(string.Format("!FitsTouching({0},{1},{2}", theme, obj,
								directionPreds[relativeDir[dir]]));
						}
						else if (Helper.GetObjectWorldSize(theme).min.y < Helper.GetObjectWorldSize(obj).min.y) {
							Debug.Log(string.Format("{0}.min.y < {1}.min.y", theme, obj));
						}
					}
				}
			}
			else if (option is Vector3) {
				Vector3 target = (Vector3) option;

				if (certainty == CertaintyMode.Act) {
					if (!pushOptions.Contains(string.Format("slide({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						pushOptions.Add(string.Format("slide({0},{1})", theme.name,
							Helper.VectorToParsable(target)));

						foreach (Region region in orthogonalRegions) {
							if (region.Contains(target)) {
								confirmationTexts.Add(
									string.Format("slide({0},{1})", theme.name, Helper.VectorToParsable(target)),
									string.Format("push {0} to the table's {1} {2} part",
										GenerateReferringExpression(theme, new List<object>()), regionLabels[region],
										dir));
							}
						}
					}
				}
				else if (certainty == CertaintyMode.Suggest) {
					if (!suggestedActions.Contains(string.Format("slide({0},{1})", theme.name,
						Helper.VectorToParsable(target)))) {
						suggestedActions.Add(string.Format("slide({0},{1})", theme.name,
							Helper.VectorToParsable(target)));

						foreach (Region region in orthogonalRegions) {
							if (region.Contains(target)) {
								confirmationTexts.Add(
									string.Format("slide({0},{1})", theme.name, Helper.VectorToParsable(target)),
									string.Format("push {0} to the table's {1} {2} part",
										GenerateReferringExpression(theme, new List<object>()), regionLabels[region],
										dir));
							}
						}
					}
				}
			}
		}

		return pushOptions;
	}

	List<object> FindPlacementOptions(GameObject theme, string dir) {
		// returns objects theme can be placed relative to, or region

		// populate regions and get QSR function label
		Region thisRegion = null;
		List<Region> orthogonalRegions = new List<Region>();
		string qsr = "";
		if (dir == "left") {
			thisRegion = (leftRegion);
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
			qsr = "Left";
		}
		else if (dir == "right") {
			thisRegion = (rightRegion);
			orthogonalRegions.Add(frontRegion);
			orthogonalRegions.Add(backRegion);
			qsr = "Right";
		}
		else if (dir == "front") {
			thisRegion = (frontRegion);
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
			qsr = "InFront";
		}
		else if (dir == "back") {
			thisRegion = (backRegion);
			orthogonalRegions.Add(leftRegion);
			orthogonalRegions.Add(rightRegion);
			qsr = "Behind";
		}

		//object qsrClassInstance = Activator.CreateInstance (QSR);
		List<object> placementOptions = new List<object>();
		List<GameObject> objectMatches = new List<GameObject>();
		Bounds themeBounds = Helper.GetObjectWorldSize(theme);
		foreach (Region region in orthogonalRegions) {
			Debug.Log(string.Format("{0}:{1}", region.center, region.Contains(theme)));
			if (region.Contains(theme)) {
				Debug.Log(string.Format("{0} contains {1}", region, theme));
				foreach (GameObject block in availableObjs) {
					// find any objects in the direction relative to the grasped object
					if (block.activeInHierarchy) {
						if (block != theme) {
							// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
							if ((bool) (Type.GetType("QSR").GetMethod(qsr).Invoke(null, new object[] {
								    Helper.GetObjectWorldSize(block),
								    themeBounds
							    })) && // if it's to the left of the grasped block
							    (region.Contains(block))) {
								// and in the same region (orthogonal to dir of movement)
								if (!objectMatches.Contains(block)) {
									objectMatches.Add(block);
								}
							}
							else {
								if (!region.Contains(block)) {
									Debug.Log(string.Format("{0} not in region {1}", block.name, region));
								}
								else {
									Debug.Log(string.Format("{0} not {1} of {2}", block.name, qsr, theme.name));
								}
							}
						}
						else {
							Debug.Log(string.Format("{0} == theme", block.name));
						}
					}
					else {
						Debug.Log(string.Format("{0} inactive", block.name));
					}
				}
			}
		}

		Vector3 target = Vector3.zero;

		foreach (GameObject obj in objectMatches) {
			target = obj.transform.position;
			placementOptions.Add(obj);
		}

		// not moving on top of another object
		foreach (Region region in orthogonalRegions) {
			if (region.Contains(theme)) {
				// stay in this region
				target = Helper.FindClearRegion(demoSurface, new Region[] {thisRegion, region}, theme).center +
				         theme.transform.position - (Helper.GetObjectWorldSize(theme).center);
				placementOptions.Add(target);
			}
		}

		return placementOptions;
	}

	void LookAt(GameObject obj) {
		Vector3 target = new Vector3(obj.transform.position.x / 2.0f,
			(obj.transform.position.y + headTargetDefault.y) / 2.0f, obj.transform.position.z / 2.0f);
		Diana.GetComponent<LookAtIK>().solver.target.position = obj.transform.position;
		Diana.GetComponent<LookAtIK>().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK>().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK>().solver.headWeight = 1.0f;

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("look_at({0})", obj.name))));
		}
	}

	void LookAt(Vector3 point) {
		Vector3 target = new Vector3(point.x / 2.0f, (point.y + headTargetDefault.y) / 2.0f, point.z / 2.0f);
		Diana.GetComponent<LookAtIK>().solver.target.position = target;
		Diana.GetComponent<LookAtIK>().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK>().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK>().solver.headWeight = 1.0f;

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("look_at({0})", Helper.VectorToParsable(point)))));
		}
	}

	void TurnToward(GameObject obj) {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
	}

	void TurnToward(Vector3 point) {
		//Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;

		ikControl.leftHandObj.position = leftTargetDefault;
		ikControl.rightHandObj.position = rightTargetDefault;
		InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
		InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
	}

	void TurnToAccess(Vector3 point) {
		Vector3 leftGrasperCoord = Diana.GetComponent<FullBodyBipedIK>().solver
			.GetEffector(FullBodyBipedEffector.LeftHand).position;
		Vector3 rightGrasperCoord = Diana.GetComponent<FullBodyBipedIK>().solver
			.GetEffector(FullBodyBipedEffector.RightHand).position;
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

		Vector3 offset = point - new Vector3(grasperCoord.x, point.y, grasperCoord.z);
		offset = Quaternion.Euler(0.0f,
			         -45.0f * (point.x - Diana.transform.position.x) / Mathf.Abs(point.x - Diana.transform.position.x),
			         0.0f) * offset;
		Diana.GetComponent<IKControl>().targetRotation = Quaternion.LookRotation(offset, Vector3.up).eulerAngles;
	}

	Vector3 TransformToSurface(List<float> vector, Vector2 bias) {
		Vector3 coord = Vector3.zero;
		vector[0] += bias.x;
		vector[1] += bias.y;

		if (transformToScreenPointing) {
			screenPoint = new Vector3(
				(Screen.width * vector[0] * vectorScaleFactor.x / tableSize.x) + (Screen.width / 2.0f),
				(Screen.height * vector[1] / (kinectToSurfaceHeight * vectorScaleFactor.y)) + (Screen.height / 2.0f),
				0.0f);

			Ray ray = Camera.main.ScreenPointToRay(screenPoint);
			RaycastHit hit;
			// Casts the ray and get the first game object hit
			if (demoSurfaceCollider.Raycast(ray, out hit, 10.0f)) {
//				if (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject) == demoSurface) {
				coord = new Vector3(hit.point.x,
					Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
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
			coord = new Vector3(-vector[0] * vectorScaleFactor.x,
				Helper.GetObjectWorldSize(demoSurface).max.y + Constants.EPSILON,
				zCoord);
		}

		return coord;
	}

	bool SurfaceClear(GameObject block) {
		Debug.Log(block);
		bool surfaceClear = true;
		List<GameObject> excludeChildren = block.GetComponentsInChildren<Renderer>().Where(
			o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != block) ||
			     (o.gameObject.layer == LayerMask.NameToLayer("blocks-known"))).Select(o => o.gameObject).ToList();
		foreach (GameObject go in excludeChildren) {
			Debug.Log(go);
		}

		Bounds blockBounds = Helper.GetObjectWorldSize(block, excludeChildren);
		Debug.Log(blockBounds);
		Debug.Log(Helper.GetObjectWorldSize(block).max.y);
		Debug.Log(Helper.GetObjectWorldSize(block, excludeChildren).max.y);
		Debug.Log(blockBounds.max.y);
		foreach (GameObject otherBlock in availableObjs) {
			excludeChildren = otherBlock.GetComponentsInChildren<Renderer>().Where(
				o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != otherBlock) ||
				     (o.gameObject.layer == LayerMask.NameToLayer("blocks-known"))).Select(o => o.gameObject).ToList();
			foreach (GameObject go in excludeChildren) {
				Debug.Log(go);
			}

			Bounds otherBounds = Helper.GetObjectWorldSize(otherBlock, excludeChildren);
			Debug.Log(otherBlock);
			Debug.Log(otherBounds);
			Debug.Log(Helper.GetObjectWorldSize(otherBlock).min.y);
			Debug.Log(Helper.GetObjectWorldSize(otherBlock, excludeChildren).min.y);
			Debug.Log(otherBounds.min.y);
			Region blockMax = new Region(new Vector3(blockBounds.min.x, blockBounds.max.y, blockBounds.min.z),
				new Vector3(blockBounds.max.x, blockBounds.max.y, blockBounds.max.z));
			Region otherMin = new Region(new Vector3(otherBounds.min.x, blockBounds.max.y, otherBounds.min.z),
				new Vector3(otherBounds.max.x, blockBounds.max.y, otherBounds.max.z));
//			if ((QSR.Above (otherBounds, blockBounds)) && (!QSR.Left (otherBounds, blockBounds)) &&
//				(!QSR.Right (otherBounds, blockBounds)) && (RCC8.EC (otherBounds, blockBounds))) {
			Debug.Log(Helper.RegionToString(blockMax));
			Debug.Log(Helper.RegionToString(otherMin));
			Debug.Log(Helper.RegionToString(Helper.RegionOfIntersection(blockMax, otherMin, MajorAxis.Y)));
			Debug.Log(QSR.Above(otherBounds, blockBounds));
			Debug.Log(
				((Helper.RegionOfIntersection(blockMax, otherMin, MajorAxis.Y).Area() / blockMax.Area())));
			Debug.Log(RCC8.EC(otherBounds, blockBounds));
			if ((QSR.Above(otherBounds, blockBounds)) &&
			    ((Helper.RegionOfIntersection(blockMax, otherMin, MajorAxis.Y).Area() / blockMax.Area()) >
			     0.25f) &&
			    (RCC8.EC(otherBounds, blockBounds))) {
				surfaceClear = false;
				break;
			}
		}

		Debug.Log(surfaceClear);
		return surfaceClear;
	}

	bool FitsTouching(GameObject theme, GameObject obj, string dir) {
		bool fits = true;

		Bounds themeBounds = Helper.GetObjectWorldSize(theme);
		Bounds objBounds = Helper.GetObjectWorldSize(obj);

		foreach (GameObject test in availableObjs) {
			if ((test != theme) && (test != obj)) {
				if (dir == "left") {
					Bounds projectedBounds = new Bounds(
						new Vector3(objBounds.min.x - themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
						themeBounds.size);
					if (!RCC8.DC(projectedBounds, Helper.GetObjectWorldSize(test)) &&
					    !RCC8.EC(projectedBounds, Helper.GetObjectWorldSize(test))) {
						fits = false;
					}
				}
				else if (dir == "right") {
					Bounds projectedBounds = new Bounds(
						new Vector3(objBounds.max.x + themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
						themeBounds.size);
					if (!RCC8.DC(projectedBounds, Helper.GetObjectWorldSize(test)) &&
					    !RCC8.EC(projectedBounds, Helper.GetObjectWorldSize(test))) {
						fits = false;
					}
				}
				else if (dir == "in_front") {
					Bounds projectedBounds = new Bounds(
						new Vector3(objBounds.center.x, objBounds.center.y, objBounds.min.z - themeBounds.extents.z),
						themeBounds.size);
					if (!RCC8.DC(projectedBounds, Helper.GetObjectWorldSize(test)) &&
					    !RCC8.EC(projectedBounds, Helper.GetObjectWorldSize(test))) {
						fits = false;
					}
				}
				else if (dir == "behind") {
					Bounds projectedBounds = new Bounds(
						new Vector3(objBounds.center.x, objBounds.center.y, objBounds.max.z + themeBounds.extents.z),
						themeBounds.size);
					if (!RCC8.DC(projectedBounds, Helper.GetObjectWorldSize(test)) &&
					    !RCC8.EC(projectedBounds, Helper.GetObjectWorldSize(test))) {
						fits = false;
					}
				}
			}
		}

		return fits;
	}

	public List<GameObject> GetGrabPoses(GameObject obj, GameObject hand) {
		List<GameObject> poseList = new List<GameObject>();

		FullBodyBipedEffector effectorType = FullBodyBipedEffector.Body;
		if (hand == leftGrasper) {
			effectorType = FullBodyBipedEffector.LeftHand;
		}
		else if (hand == rightGrasper) {
			effectorType = FullBodyBipedEffector.RightHand;
		}

		InteractionTarget[] poses = obj.GetComponentsInChildren<InteractionTarget>();
		poseList = poses.ToList().Where(
			p => Helper.GetMostImmediateParentVoxeme(p.gameObject) == obj).ToList().Where(
			p => p.effectorType == effectorType).ToList().Select(p => p.gameObject).ToList();
		//        .Count ==
		//        poses.ToList().Where(p => p.effectorType == FullBodyBipedEffector.RightHand).ToList().Count) ?
		//poses.ToList().Where(p => p.effectorType == FullBodyBipedEffector.LeftHand).ToList() :
		//Math.Min(poses.ToList().Where(p => p.effectorType == FullBodyBipedEffector.LeftHand).ToList().Count,
		//poses.ToList().Where(p => p.effectorType == FullBodyBipedEffector.RightHand).ToList().Count);

		return poseList;
	}

	public void ReachFor(Vector3 coord) {
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
		//Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		//Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;

		if ((interactionLogic != null) && (interactionLogic.enabled) && (interactionLogic.GraspedObj != null)) {
			// grasping something
			if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == leftGrasper) {
				if (ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
					ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = coord + offset;
				}
				else {
					ikControl.rightHandObj.position = coord + offset;
				}

				setRightHandTarget = true;
				//InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
			}
			else if (InteractionHelper.GetCloserHand(Diana, interactionLogic.GraspedObj) == rightGrasper) {
				if (ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
					ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = coord + offset;
				}
				else {
					ikControl.leftHandObj.position = coord + offset;
				}

				setLeftHandTarget = true;
				//InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			}
		}
		else {
			// which region is coord in?
			if (leftRegion.Contains(new Vector3(coord.x,
				leftRegion.center.y, coord.z))) {
				if (ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
					ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = coord + offset;
				}
				else {
					ikControl.rightHandObj.position = coord + offset;
				}

				setRightHandTarget = true;
				//InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
			}
			else if (rightRegion.Contains(new Vector3(coord.x,
				rightRegion.center.y, coord.z))) {
				if (ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
					ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = coord + offset;
				}
				else {
					ikControl.leftHandObj.position = coord + offset;
				}

				setLeftHandTarget = true;
				//InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
			}
		}

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("reach({0})", Helper.VectorToParsable(coord)))));
		}

		LookForward();
	}

	public void ReachFor(GameObject obj) {
		Bounds bounds = Helper.GetObjectWorldSize(obj);
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;
		//		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		//		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;

		PhysicsHelper.ResolveAllPhysicsDiscrepancies(false);

		// which region is obj in?
		if (leftRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y, obj.transform.position.z))) {
			if (ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
				ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().targetPosition =
					obj.transform.position + offset;
			}
			else {
				ikControl.rightHandObj.position = new Vector3(Helper.GetObjectWorldSize(obj).center.x,
					                                  Helper.GetObjectWorldSize(obj).max.y,
					                                  Helper.GetObjectWorldSize(obj).center.z) + offset;
			}

			setRightHandTarget = true;
			//InteractionHelper.SetRightHandTarget (Diana, ikControl.rightHandObj);
		}
		else if (rightRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y, obj.transform.position.z))) {
			if (ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
				ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().targetPosition =
					obj.transform.position + offset;
			}
			else {
				ikControl.leftHandObj.position = new Vector3(Helper.GetObjectWorldSize(obj).center.x,
					                                 Helper.GetObjectWorldSize(obj).max.y,
					                                 Helper.GetObjectWorldSize(obj).center.z) + offset;
			}

			setLeftHandTarget = true;
			//InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		}

		//LookAt (obj);

		if ((adeSocket != null) && (adeSocket.IsConnected())) {
			string goalSemantics = string.Format("moveToObject(self, {0}(X)^{1}(X)",
				obj.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value,
				obj.GetComponent<Voxeme>().voxml.Lex.Pred);
			byte[] bytes = BitConverter.GetBytes(goalSemantics.Length).Concat(Encoding.ASCII.GetBytes(goalSemantics))
				.ToArray<byte>();
            adeSocket.Write(BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray<byte>());
		}

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("reach({0})", obj.name))));
		}
	}

	public void ReachAndPoint(Vector3 point, GameObject hand) {
		Vector3 target = new Vector3(point.x, point.y + 0.2f, point.z);
		AvatarGesture performGesture = null;

		MoveToPerform();

		if (hand == leftGrasper) {
			performGesture = AvatarGesture.LARM_POINT_FRONT;
		}
		else if (hand == rightGrasper) {
			performGesture = AvatarGesture.RARM_POINT_FRONT;
		}

		gestureController.PerformGesture(performGesture);

		if (hand == leftGrasper) {
			// TODO: I don't like this solution
			if (ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
				ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = target;
			}
			else {
				ikControl.leftHandObj.position = target;
			}

			InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
		}
		else if (hand == rightGrasper) {
			// TODO: I don't like this solution
			if (ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
				ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = target;
			}
			else {
				ikControl.rightHandObj.position = target;
			}

			InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
		}

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("point({0},{1})", hand.name, Helper.VectorToParsable(point)))));
		}
	}

	public void PointAt(Vector3 point, GameObject hand) {
		Vector3 target = new Vector3(point.x, point.y, point.z);
		AvatarGesture performGesture = null;

		MoveToPerform();

		if (hand == leftGrasper) {
			performGesture = AvatarGesture.LARM_POINT_FRONT;
		}
		else if (hand == rightGrasper) {
			performGesture = AvatarGesture.RARM_POINT_FRONT;
		}

		gestureController.PerformGesture(performGesture);

		if (hand == leftGrasper) {
			LimbIK leftArmIK = Diana.GetComponents<LimbIK>().Where(ik => ik.solver.target == ikControl.leftHandObj)
				.ToList()[0];
			leftArmIK.solver.target.gameObject.GetComponent<TransformTarget>().targetPosition = target;
			//InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj, 1.0f, 1.0f);
		}
		else if (hand == rightGrasper) {
			LimbIK rightArmIK = Diana.GetComponents<LimbIK>().Where(ik => ik.solver.target == ikControl.rightHandObj)
				.ToList()[0];
			rightArmIK.solver.target.gameObject.GetComponent<TransformTarget>().targetPosition = target;
			//InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj, 1.0f, 1.0f);
		}

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					string.Format("point({0},{1})", hand.name, Helper.VectorToParsable(point)))));
		}
	}

	public void GesturePause() {
		Debug.Log("Pausing");
		gestureResumeTimer.Enabled = true;
	}

	public void GestureResume(object sender, ElapsedEventArgs e) {
		Debug.Log("Resuming gesture");
		gestureResume = true;
		gestureResumeTimer.Enabled = false;
		gestureResumeTimer.Interval = gestureResumeTime;
	}

	public void StorePose() {
		//Debug.Break();
		bool leftGrasping = false;
		bool rightGrasping = false;

		if (graspedObj != null) {
			if (InteractionHelper.GetCloserHand(Diana, graspedObj) == leftGrasper) {
				leftGrasping = true;
			}
			else if (InteractionHelper.GetCloserHand(Diana, graspedObj) == rightGrasper) {
				rightGrasping = true;
			}
		}

		if (!leftGrasping) {
			if (leftTargetStored == new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)) {
				leftTargetStored = ikControl.leftHandObj.transform.position;
				Debug.Log(string.Format("Storing pose {0} {1} {2}",
					Helper.VectorToParsable(ikControl.leftHandObj.transform.position),
					Helper.VectorToParsable(ikControl.rightHandObj.transform.position),
					Helper.VectorToParsable(ikControl.lookObj.transform.position)));
			}
		}
		else {
			leftTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}

		if (!rightGrasping) {
			if (rightTargetStored == new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)) {
				rightTargetStored = ikControl.rightHandObj.transform.position;
				Debug.Log(string.Format("Storing pose {0} {1} {2}",
					Helper.VectorToParsable(ikControl.leftHandObj.transform.position),
					Helper.VectorToParsable(ikControl.rightHandObj.transform.position),
					Helper.VectorToParsable(ikControl.lookObj.transform.position)));
			}
		}
		else {
			rightTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}

		headTargetStored = ikControl.lookObj.transform.position;
	}

	public void ReturnToPose() {
		Debug.Log(string.Format("Returning to pose {0} {1} {2}",
			Helper.VectorToParsable(leftTargetStored),
			Helper.VectorToParsable(rightTargetStored),
			Helper.VectorToParsable(headTargetStored)));
		bool animPlaying = false;
		for (int i = 0; i < Diana.GetComponent<Animator>().layerCount; i++) {
			if (Diana.GetComponent<Animator>().GetCurrentAnimatorClipInfo(i)[0].clip != null) {
				//Debug.Log(string.Format("{0}: {1}", i, Diana.GetComponent<Animator>().GetCurrentAnimatorClipInfo(i)[0].clip.name));
				animPlaying = true;
			}
		}

		//		if (Diana.GetComponent<Animator> ().GetCurrentAnimatorClipInfo() != null) {
		//			animPlaying = true;
		//		}

		//if (!animPlaying) {
		if (leftTargetStored != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)) {
			List<LimbIK> leftArmIKs = Diana.GetComponents<LimbIK>()
				.Where(ik => ik.solver.target == ikControl.leftHandObj).ToList();
			if (leftArmIKs.Count > 0) {
				LimbIK leftArmIK = Diana.GetComponents<LimbIK>().Where(ik => ik.solver.target == ikControl.leftHandObj)
					.ToList()[0];
				leftArmIK.solver.target.gameObject.GetComponent<TransformTarget>().targetPosition = leftTargetStored;
			}
			else {
				if (!animPlaying) {
					ikControl.leftHandObj.transform.GetComponent<TransformTarget>().targetPosition = leftTargetStored;
					InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
				}
			}

			leftTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}

		if (rightTargetStored != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue)) {
			List<LimbIK> rightArmIKs = Diana.GetComponents<LimbIK>()
				.Where(ik => ik.solver.target == ikControl.rightHandObj).ToList();
			if (rightArmIKs.Count > 0) {
				LimbIK rightArmIK = Diana.GetComponents<LimbIK>()
					.Where(ik => ik.solver.target == ikControl.rightHandObj).ToList()[0];
				rightArmIK.solver.target.gameObject.GetComponent<TransformTarget>().targetPosition = rightTargetStored;
			}
			else {
				if (!animPlaying) {
					ikControl.rightHandObj.transform.position = rightTargetStored;
					ikControl.rightHandObj.transform.GetComponent<TransformTarget>().targetPosition = rightTargetStored;
					InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
				}
			}

			rightTargetStored = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		}

		ikControl.lookObj.transform.position = headTargetStored;
		InteractionHelper.SetHeadTarget(Diana, ikControl.lookObj);
		//		Debug.Log (string.Format("Returning to pose {0} {1} {2}",
		//			ikControl.leftHandObj.transform.position,ikControl.rightHandObj.transform.position,ikControl.lookObj.transform.position));
		//}
	}

	public void ReturnHandsToDefault() {
		if (ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
			ikControl.leftHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = leftTargetDefault;
		}
		else {
			ikControl.leftHandObj.position = leftTargetDefault;
		}

		if (ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().enabled) {
			ikControl.rightHandObj.gameObject.GetComponent<TransformTarget>().targetPosition = rightTargetDefault;
		}
		else {
			ikControl.rightHandObj.position = rightTargetDefault;
		}
	}

	double EpistemicCertainty(Concept concept) {
		double certainty = concept.Certainty;

		foreach (Concept related in concept.Related) {
			if (related.Certainty > certainty) {
				certainty = related.Certainty;
			}
		}

		return certainty;
	}

	public void RespondAndUpdate(string utterance, bool forceUtterance = false) {
		if (AgentOutputHelper.GetCurrentOutputString(Role.Affector, "Diana") != utterance) { //// add agent
			if (!logActionsOnly) {
				logger.OnLogEvent(this, new LoggerArgs(
					string.Format("{0}\t{1}\t{2}",
						(++logIndex).ToString(),
						"AS",
						string.Format("\"{0}\"", utterance))));
			}
		}

        AgentOutputHelper.SpeakOutput(Role.Affector, utterance, "Diana", forceUtterance);
        AgentOutputHelper.PrintOutput(Role.Affector, utterance, "Diana", forceUtterance);


        // get all linguistic concepts
        if ((!UseTeaching) || (!interactionLogic.useEpistemicModel)) {
			return;
		}

		List<Concepts> conceptsList = epistemicModel.state.GetAllConcepts();
		foreach (Concepts concepts in conceptsList) {
			if (concepts.GetConcepts().ContainsKey(ConceptMode.L)) {
				List<Concept> linguisticConcepts = concepts.GetConcepts()[ConceptMode.L];

				List<Concept> conceptsToUpdate = new List<Concept>();
				List<Relation> relationsToUpdate = new List<Relation>();
				// if mentioned, introduce if not used already
//				Debug.Log (utterance.ToLower ());
				foreach (Concept concept in linguisticConcepts) {
//					Debug.Log (concept.Name.ToLower ());
					if (utterance.ToLower().Contains(concept.Name.ToLower())) {
//						Debug.Log (string.Format("{0} certainty: {1}",concept.Name.ToLower (),concept.Certainty));
						concept.Certainty = concept.Certainty < 0.5 && concept.Certainty >= 0.0
							? 0.5
							: concept.Certainty;
//						Debug.Log (string.Format("{0} certainty: {1}",concept.Name.ToLower (),concept.Certainty));
						conceptsToUpdate.Add(concept);

						foreach (Concept relatedConcept in epistemicModel.state.GetRelated(concept)) {
							Relation relation = epistemicModel.state.GetRelation(concept, relatedConcept);
							double prevCertainty = relation.Certainty;
							double newCertainty = Math.Min(concept.Certainty, relatedConcept.Certainty);
							if (Math.Abs(prevCertainty - newCertainty) > 0.01) {
								relation.Certainty = newCertainty;
								relationsToUpdate.Add(relation);
							}
						}
					}
				}

				if (conceptsToUpdate.Count + relationsToUpdate.Count > 0) {
					epistemicModel.state.UpdateEpisim(conceptsToUpdate.ToArray(), relationsToUpdate.ToArray());
				}
			}
		}
	}

	String GenerateReferringExpression(GameObject targetObj, List<object> context) {
		String refExp = string.Empty;

		Voxeme targetVoxeme = targetObj.GetComponent<Voxeme>();
		List<Voxeme> contextVoxemes = new List<Voxeme>();

		if ((context != null) && (context.Count > 0)) {
			foreach (object contextObj in context) {
				if (contextObj is GameObject) {
					if ((contextObj as GameObject) != targetObj) {
						if ((contextObj as GameObject).GetComponent<Voxeme>() != null) {
							contextVoxemes.Add((contextObj as GameObject).GetComponent<Voxeme>());
						}
					}
				}
			}
		}

		List<object> uniqueAttrs = new List<object>();
		bool anySameType = ((contextVoxemes.Count > 0) &&
		                    contextVoxemes.Any(o => o.voxml.Lex.Pred == targetVoxeme.voxml.Lex.Pred));

		if (anySameType) {
			List<Voxeme> objVoxemes = new List<Voxeme>() {targetVoxeme};
			objVoxemes.Concat(contextVoxemes);
			for (int i = 0; i < objVoxemes.Count; i++) {
				List<object> newAttrs = Helper.DiffLists(
					uniqueAttrs.Select(x => ((VoxAttributesAttr) x).Value).Cast<object>().ToList(),
					objVoxemes[i].voxml.Attributes.Attrs.Cast<object>().ToList()
						.Select(x => ((VoxAttributesAttr) x).Value).Cast<object>().ToList());

				if (newAttrs.Count > 0) {
					foreach (object attr in newAttrs) {
						Debug.Log(string.Format("{0}:{1}", objVoxemes[i].name, attr));
						VoxAttributesAttr attrToAdd = new VoxAttributesAttr();
						attrToAdd.Value = attr.ToString();

						if (uniqueAttrs.Where(x => ((VoxAttributesAttr) x).Value == attrToAdd.Value).ToList()
							    .Count == 0) {
							uniqueAttrs.Add(attrToAdd);
						}
					}
				}
			}
		}

		string attribute = string.Empty;
		if (uniqueAttrs.Count > 0) {
			attribute = ((VoxAttributesAttr) uniqueAttrs[uniqueAttrs.Count - 1]).Value;
		}

		Debug.Log(anySameType);
		Debug.Log(targetVoxeme.voxml.Attributes.Attrs[0].Value);

		refExp = string.Format("the {0}{1}",
			anySameType ? string.Format("{0} ", targetVoxeme.voxml.Attributes.Attrs[0].Value) : "",
			targetVoxeme.voxml.Lex.Pred);
		return refExp;
	}

	void PromptEvent(string eventStr) {
		eventManager.InsertEvent("", 0);
		eventManager.InsertEvent(eventStr, 1);

		logger.OnLogEvent(this, new LoggerArgs(
			string.Format("{0}\t{1}\t{2}",
				(++logIndex).ToString(),
				"AA",
				eventStr)));

		if (logFullState) {
			foreach (Voxeme voxeme in objSelector.allVoxemes) {
				if ((voxeme.gameObject.activeInHierarchy) &&
				    (!objSelector.disabledObjects.Contains(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject)))) {
					logger.OnLogEvent(this, new LoggerArgs(
						string.Format("{0}\t{1}\t{2}",
							logIndex.ToString(),
							"", string.Format("{0}:{1},{2}", voxeme.gameObject.name,
								Helper.VectorToParsable(voxeme.gameObject.transform.position),
								Helper.VectorToParsable(voxeme.gameObject.transform.eulerAngles)))));
				}
			}
		}
	}

	void PerformAndLogGesture(AvatarGesture gesture) {
		gestureController.PerformGesture(gesture);

		if (!logActionsOnly) {
			logger.OnLogEvent(this, new LoggerArgs(
				string.Format("{0}\t{1}\t{2}",
					(++logIndex).ToString(),
					"AG",
					gesture.Name)));
		}
	}

	void ReturnToRest(object sender, EventArgs e) {
		if (((EventManagerArgs) e).EventString != string.Empty) {
			Debug.Log(string.Format("Completed event: {0}", ((EventManagerArgs) e).EventString));
			if (!interactionSystem.IsPaused(FullBodyBipedEffector.LeftHand) &&
			    !interactionSystem.IsPaused(FullBodyBipedEffector.RightHand)) {
				TurnForward();
				LookForward();

				if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
					interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
						null));
				}
			}
			else {
				if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
					if ((interactionLogic.ActionOptions.Count > 0) &&
					    ((Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1],
						     "grasp")) ||
					     (Regex.IsMatch(interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1],
						     "lift")))) {
						GameObject graspedObject = null;
						//if (((EventManagerArgs)e).EventString.Contains("lift")) {
						string pred =
							Helper.GetTopPredicate(
								interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]);
						graspedObject = eventManager.ExtractObjects(pred,
								(String) Helper.ParsePredicate(
									interactionLogic.
                                        ActionOptions[interactionLogic.ActionOptions.Count - 1])[pred])[0] as GameObject;
						//}
						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								graspedObject, null, null, null, null)));
					}
				}
			}

			//		Debug.Log (interactionSystem.IsPaused (FullBodyBipedEffector.LeftHand));
			//		Debug.Log (interactionSystem.IsPaused (FullBodyBipedEffector.RightHand));

			ReturnHandsToDefault();
		}
	}

	void CompletedEventSequence(object sender, EventArgs e) {
		if (e != null) {
			string eventStr = ((EventManagerArgs) e).EventString;

			MethodInfo method = predicates.GetType().GetMethod(Helper.GetTopPredicate(eventStr).ToUpper());

			if (method != null) {
				if (method.ReturnType == typeof(void)) {
					// is event
					if ((!eventStr.Contains("grasp")) && (!eventStr.Contains("ungrasp"))) {
						if ((eventStr.Contains("slidep") && (!interactionLogic.inServoLoop))) {
							interactionLogic.RewriteStack(new PDAStackOperation(
								PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null,
									new List<string>(), null)));
						}
						else {
							interactionLogic.RewriteStack(new PDAStackOperation(
								PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null,
									new List<string>() {eventStr}, null)));
						}
					}
					else if (eventStr.Contains("ungrasp")) {
						interactionLogic.RewriteStack(new PDAStackOperation(
							PDAStackOperation.PDAStackOperationType.Rewrite,
							interactionLogic.GenerateStackSymbol(null,
								new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)),
								null, null, null, null)));
					}
				}
			}
		}
	}

	void ReferentIndicated(object sender, EventArgs e) {
		if (((EventReferentArgs) e).Referent is String) {
			// object
			GameObject obj = GameObject.Find(((string) ((EventReferentArgs) e).Referent));
			if (obj != null) {
				if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
					if ((interactionLogic.CurrentState.Name == "ParseNP") ||
					    (interactionLogic.CurrentState.Name == "ParseVP")) {
						if ((interactionLogic.IndicatedObj !=
						     GameObject.Find((string) eventManager.referents.stack.Peek())) &&
						    interactionLogic.GraspedObj != obj) {
							if ((interactionLogic.ActionOptions.Count > 0) &&
							    ((interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]
								      .StartsWith("lift") ||
							      (interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]
								      .StartsWith("grasp")))) &&
							    (!interactionLogic.ActionOptions[interactionLogic.ActionOptions.Count - 1]
								    .Contains("{0}"))) {
								interactionLogic.RewriteStack(new PDAStackOperation(
									PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(null, obj, null, null, null, null)));
							}
							else {
								interactionLogic.RewriteStack(new PDAStackOperation(
									PDAStackOperation.PDAStackOperationType.Rewrite,
									interactionLogic.GenerateStackSymbol(obj, null, null, null, null, null)));
							}
						}
					}
				}
			}
		}
		else if (((EventReferentArgs) e).Referent is Vector3) {
			// location
		}
	}

	void NonexistentReferent(object sender, EventArgs e) {
		Debug.Log(((EventReferentArgs) e).Referent is Pair<string, List<object>>);
		if (((EventReferentArgs) e).Referent is Pair<string, List<object>>) {
			// pair of predicate and object list 
			// (present type - common type of object list, of absent attribute - predicate)
			string pred = ((Pair<string, List<object>>) ((EventReferentArgs) e).Referent).Item1;
			List<object> objs = ((Pair<string, List<object>>) ((EventReferentArgs) e).Referent).Item2;
			Debug.Log(objs.Count);
			if (objs.Count > 0) {
				if (!objs.Any(o => (o == null) || (o.GetType() != typeof(GameObject)))) {
					// if all objects are game objects
					if ((interactionLogic != null) && (interactionLogic.isActiveAndEnabled)) {
						Debug.Log(string.Format("{0} {1} does not exist!", pred,
							(objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred));
						RespondAndUpdate(string.Format("There is no {0} {1} here.", pred,
							(objs[0] as GameObject).GetComponent<Voxeme>().voxml.Lex.Pred));
						if (interactionLogic.IndicatedObj != null) {
							interactionLogic.RewriteStack(new PDAStackOperation(
								PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null,
									null, new List<string>(), null)));
						}
						else if (interactionLogic.GraspedObj != null) {
							interactionLogic.RewriteStack(new PDAStackOperation(
								PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null,
									new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null,
									new List<string>(), null)));
						}
						else {
							interactionLogic.RewriteStack(new PDAStackOperation(
								PDAStackOperation.PDAStackOperationType.Rewrite,
								interactionLogic.GenerateStackSymbol(null, null, null, null, new List<string>(),
									null)));
						}
					}
				}
			}
		}
		else if (((EventReferentArgs) e).Referent is string) {
			// absent object type - string
			if (Regex.IsMatch(((EventReferentArgs) e).Referent as string, @"\{.\}")) {
				return;
			}

			RespondAndUpdate(string.Format("There is no {0} here.", ((EventReferentArgs) e).Referent as string));
			if (interactionLogic.IndicatedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null, null,
						new List<string>(), null)));
			}
			else if (interactionLogic.GraspedObj != null) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null,
						new DelegateFactory(new FunctionDelegate(interactionLogic.NullObject)), null, null,
						new List<string>(), null)));
			}
			else {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null, null, new List<string>(), null)));
			}
		}
	}

	void Disambiguate(object sender, EventArgs e) {
		List<String> commonFeatures = objSelector.ExtractCommonFeatureLabels(((EventDisambiguationArgs) e).Candidates);

		// check antecedents
		// does something in antecedent store match type and attribs in common features list?

		List<object> referentMatches = new List<object>();
		foreach (object referent in eventManager.referents.stack) {
			if (referent.GetType() == typeof(String)) {
				GameObject voxObj = GameObject.Find(referent as String);
				string pred = voxObj.GetComponent<Voxeme>().voxml.Lex.Pred;
				if (commonFeatures.Contains(pred)) {
					Debug.Log(voxObj);
					referentMatches.Add(voxObj);
					foreach (string feature in commonFeatures) {
						if ((feature != pred) && (!commonFeatures.Contains(feature))) {
							Debug.Log(voxObj);
							referentMatches.Remove(voxObj);
						}
					}
				}
			}
		}

		if ((eventManager.referents.stack.Count == 0) || (referentMatches.Count > 1)) {
			Debug.Log(string.Format("Referent(s) found: {0}",
				string.Join(", ", referentMatches.Cast<GameObject>().Select(g => g.name).ToArray())));

			Debug.Log(string.Format("Which {0}?", String.Join(" ", commonFeatures.ToArray())));
			RespondAndUpdate(string.Format("Which {0}?", String.Join(" ", commonFeatures.ToArray())));

			string ambiguityStr = ((EventDisambiguationArgs) e).AmbiguityStr;
			string ambiguityVar = ((EventDisambiguationArgs) e).AmbiguityVar;
			if ((ambiguityStr != string.Empty) && (ambiguityVar != string.Empty)) {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					interactionLogic.GenerateStackSymbol(null, null, null, null,
						new List<string>() {((EventDisambiguationArgs) e).Event.Replace(ambiguityStr, ambiguityVar)},
						null)));
			}
			else {
				interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
					null));
			}
		}
		else {
			Debug.Log(string.Format("Referent found: {0}", ((GameObject) referentMatches[0]).name));

			string ambiguityStr = ((EventDisambiguationArgs) e).AmbiguityStr;
			string ambiguityVar = ((EventDisambiguationArgs) e).AmbiguityVar;
			string eventStr = ((EventDisambiguationArgs) e).Event.Replace(ambiguityStr, ambiguityVar);
			interactionLogic.RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite,
				interactionLogic.GenerateStackSymbol(null, null, null, null,
					new List<string>() {string.Format(eventStr, ((GameObject) referentMatches[0]).name)}, null)));
		}

		//Debug.Log(interactionLogic.CurrentState.Name);
		//Debug.Log(interactionLogic.StackSymbolToString(interactionLogic.CurrentStackSymbol));
	}

	void ConnectionLost(object sender, EventArgs e) {
		LookForward();
		//Debug.Log("Connection Lost");

		if (interactionPrefs.connectionLostNotification) {
			if (eventManager.events.Count == 0) {
				if (interactionLogic.CurrentState.Name != "EndState") {
					fusionSocket.OnFusionReceived(this, new FusionEventArgs("G;engage stop;0.0"));
					if (sessionCounter >= 1) {
						RespondAndUpdate("Hey, where'd you go?");
					}
					else {
						RespondAndUpdate("Anyone there?");
					}
				}
			}
		}
	}

	void ConnectionMade(object sender, EventArgs e) {
		LookForward();
		RespondAndUpdate("");
	}

	void DisableHighlight(object sender, ElapsedEventArgs e) {
		highlightTimeoutTimer.Enabled = false;
		highlightTimeoutTimer.Interval = highlightTimeoutTime;

		disableHighlight = true;
	}

	void LateUpdate() {
		if (setLeftHandTarget) {
			InteractionHelper.SetLeftHandTarget(Diana, ikControl.leftHandObj);
			setLeftHandTarget = false;
		}

		if (setRightHandTarget) {
			InteractionHelper.SetRightHandTarget(Diana, ikControl.rightHandObj);
			setRightHandTarget = false;
		}

		if (setHeadTarget) {
			setHeadTarget = false;
		}
	}

	void OnDestroy() {
		epistemicModel.SaveUserModel(epistemicModel.userID);
		logger.CloseLog();
	}

	void OnApplicationQuit() {
		epistemicModel.SaveUserModel(epistemicModel.userID);
		logger.CloseLog();
	}
}