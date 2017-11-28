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

	EpistemicModel epistemicModel;
	enum CertaintyMode {
		Suggest,
		Act
	};

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

	Timer highlightTimeoutTimer;
	public double highlightTimeoutTime;
	bool disableHighlight = false;

	const float DEFAULT_SCREEN_WIDTH = .9146f; // ≈ 36" = 3'
	public float knownScreenWidth = .3646f; //m
	public float windowScaleFactor;
	public bool transformToScreenPointing = false;	// false = assume table in demo space and use its coords to mirror table coords

	public bool allowDeixisByClick = false;

	GenericLogger logger;

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

	// Use this for initialization
	void Start () {
		windowScaleFactor = (float)Screen.width/(float)Screen.currentResolution.width;

		eventManager = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();
		eventManager.EventComplete += ReturnToRest;

		interactionPrefs = gameObject.GetComponent<InteractionPrefsModalWindow> ();

		logger = GetComponent<GenericLogger> ();

		if (PlayerPrefs.GetInt ("Make Logs") == 1) {
			logger.OpenLog (PlayerPrefs.GetString ("Logs Prefix"));
		}

		Diana = GameObject.Find ("Diana");
		leftGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.leftHand.gameObject;
		rightGrasper = Diana.GetComponent<FullBodyBipedIK> ().references.rightHand.gameObject;
		epistemicModel = Diana.GetComponent<EpistemicModel> ();
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

			regionLabels.Add (leftRegion, "left");
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

			regionLabels.Add (rightRegion, "right");
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

			regionLabels.Add (frontRegion, "front");
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

			regionLabels.Add (backRegion, "back");
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

		if (disableHighlight) {
			regionHighlight.GetComponent<Renderer> ().enabled = false;
			disableHighlight = false;
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

		if (epistemicModel.engaged) {
			Concept putConcept = epistemicModel.state.GetConcept ("PUT", ConceptType.ACTION, ConceptMode.L);
			Concept putG = epistemicModel.state.GetConcept ("move", ConceptType.ACTION, ConceptMode.G);
			putConcept.Certainty = -1.0;
			Concept pushConcept = epistemicModel.state.GetConcept ("PUSH", ConceptType.ACTION, ConceptMode.L);
			Concept pushG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
			pushConcept.Certainty = -1.0;
			var putRelation = epistemicModel.state.GetRelation (putConcept, putG);
			var pushRelation = epistemicModel.state.GetRelation (pushConcept, pushG);
			putRelation.Certainty = -1.0;
			pushRelation.Certainty = -1.0;
			epistemicModel.state.UpdateEpisim (new Concept[] { putConcept, pushConcept }, new Relation[]{ pushRelation, putRelation });

			foreach (GameObject block in blocks) {	// limit to blocks only for now
				Voxeme blockVox = block.GetComponent<Voxeme> ();
				if (blockVox != null) {
					if (synVision.knownObjects.Contains (blockVox)) {
						string color = string.Empty;
						color = blockVox.voxml.Attributes.Attrs [0].Value;	// just grab the first one for now

						Concept blockConcept = epistemicModel.state.GetConcept (block.name, ConceptType.OBJECT, ConceptMode.G);

						if (blockConcept.Certainty < 1.0) {
							blockConcept.Certainty = 1.0;
							epistemicModel.state.UpdateEpisim (new Concept[] { blockConcept }, new Relation[] { });
						}
					}
				}
			}
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
						epistemicModel.engaged = true;
						if (synVision != null) {
							if (synVision.enabled) {
								Debug.Log(string.Format("SyntheticVision.IsVisible({0}):{1}",hit.collider.gameObject,synVision.IsVisible (hit.collider.gameObject)));
								if (synVision.IsKnown (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject))) {
									Deixis (Helper.GetMostImmediateParentVoxeme (hit.collider.gameObject));
								}
							}
						}
					}
				}
			}
		}
	}

	void ReceivedFusion(object sender, EventArgs e) {
		string fusionMessage = ((GestureEventArgs)e).Content;
		//Debug.Log (fusionMessage);
		logger.OnLogEvent (this, new LoggerArgs (fusionMessage));

		string[] splitMessage = ((GestureEventArgs)e).Content.Split (';');
		string messageType = splitMessage[0];
		string messageStr = splitMessage[1];
		string messageTime = splitMessage[2];

		receivedMessages.Add (new Pair<string,string> (messageTime, messageStr));

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
				conceptL = epistemicModel.state.GetConcept ("YES", ConceptType.ACTION, ConceptMode.L);
				conceptG = epistemicModel.state.GetConcept ("posack", ConceptType.ACTION, ConceptMode.G);
				relation = epistemicModel.state.GetRelation (conceptG, conceptL);

				conceptL.Certainty = 1.0;

				if (conceptG.Certainty > 0.0) {
					relation.Certainty = 1.0;
				}

				epistemicModel.state.UpdateEpisim(new Concept[] { conceptL,conceptG }, new Relation[] { relation });

				Acknowledge (true);
				break;
			case "no":
				conceptL = epistemicModel.state.GetConcept("NO", ConceptType.ACTION, ConceptMode.L);
				conceptG = epistemicModel.state.GetConcept ("negack", ConceptType.ACTION, ConceptMode.G);
				relation = epistemicModel.state.GetRelation (conceptG, conceptL);

				conceptL.Certainty = 1.0;

				if (conceptG.Certainty > 0.0) {
					relation.Certainty = 1.0;
				}

				epistemicModel.state.UpdateEpisim(new Concept[] { conceptL,conceptG }, new Relation[] { relation });

				Acknowledge (false);
				break;
			case "grab":
				conceptL = epistemicModel.state.GetConcept ("GRAB", ConceptType.ACTION, ConceptMode.L);
				conceptG = epistemicModel.state.GetConcept ("grab", ConceptType.ACTION, ConceptMode.G);
				relation = epistemicModel.state.GetRelation (conceptG, conceptL);

				if (indicatedObj == null) {
					if (EpistemicCertainty (conceptL) < 0.5) {
						conceptG.Certainty = 0.5;
					}
					else {
						relation.Certainty = 1.0;
					}

					Suggest ("grab");
				}
				else {
					if (EpistemicCertainty(conceptL) < 0.5) {
						conceptG.Certainty = 0.5;

						Suggest ("grab");
					}
					else {
						relation.Certainty = 1.0;

						Grab (true);
					}
				}

				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptG, conceptL }, new Relation[] { relation });
				break;
			case "left":
				conceptL = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);

				if ((indicatedObj == null) && (graspedObj == null)) {
					if (indicatedRegion == leftRegion) {	// if ensemble with leftward point
						Deixis ("left");
					}
					else {	// if speech alone
						Deixis("right");
					}
				}
//				else if (graspedObj == null) {
//					if (indicatedRegion == leftRegion) {	// if ensemble with leftward push
//						//Push ("left");
//					}
//					else {
//						Push ("right");
//					}
//				}
//				else if (indicatedObj == null) {
//					if (indicatedRegion == leftRegion) {	// if ensemble with leftward carry
//						//Move ("left");
//					}
//					else {
//						Move ("right");
//					}
//				}
				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });
				break;
			case "right":
				conceptL = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);

				if ((indicatedObj == null) && (graspedObj == null)) {
					if (indicatedRegion == rightRegion) {	// if ensemble with righttward point
						Deixis ("right");
					}
					else {	// if speech alone
						Deixis("left");
					}
				}
//				else if (graspedObj == null) {
//					if (indicatedRegion == rightRegion) {	// if ensemble with righttward push
//						//Push ("right");
//					}
//					else {
//						Push ("left");
//					}
//				}
//				else if (indicatedObj == null) {
//					if (indicatedRegion == rightRegion) {	// if ensemble with righttward carry
//						//Move ("right");
//					}
//					else {
//						Move ("left");
//					}
//				}
				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });
				break;
			case "this":
			case "that":
				conceptL = epistemicModel.state.GetConcept(messageStr, ConceptType.ACTION, ConceptMode.L);
				conceptL.Certainty = 1.0;

				if (regionHighlight.GetComponent<Renderer> ().enabled) {
					conceptG = epistemicModel.state.GetConcept("point", ConceptType.ACTION, ConceptMode.G);
					conceptG.Certainty = 1.0;
					relation = epistemicModel.state.GetRelation (conceptG, conceptL);
					relation.Certainty = 1.0;
					epistemicModel.state.UpdateEpisim(new Concept[] {conceptG}, new Relation[] {relation});

					Deixis (highlightCenter);
				}

				epistemicModel.state.UpdateEpisim(new Concept[] {conceptL}, new Relation[] {});


				break;
			case "red":
			case "green":
			case "yellow":
			case "orange":
			case "black":
			case "purple":
			case "white":
			case "pink":
				conceptL = epistemicModel.state.GetConcept(messageStr, ConceptType.PROPERTY, ConceptMode.L);
				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim(new Concept[] {conceptL}, new Relation[] {});

				IndexByColor (messageStr);
				break;
			case "big":
				conceptL = epistemicModel.state.GetConcept (messageStr, ConceptType.PROPERTY, ConceptMode.L);
				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });

				conceptL = epistemicModel.state.GetConcept ("SMALL", ConceptType.PROPERTY, ConceptMode.L);
				conceptL.Certainty = 0.5;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });

				IndexBySize (messageStr);
				break;
			case "small":
				conceptL = epistemicModel.state.GetConcept (messageStr, ConceptType.PROPERTY, ConceptMode.L);
				conceptL.Certainty = 1.0;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });

				conceptL = epistemicModel.state.GetConcept ("BIG", ConceptType.PROPERTY, ConceptMode.L);
				conceptL.Certainty = 0.5;
				epistemicModel.state.UpdateEpisim (new Concept[] { conceptL }, new Relation[] { });

				IndexBySize (messageStr);
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
					conceptG = epistemicModel.state.GetConcept("point", ConceptType.ACTION, ConceptMode.G);
					conceptG.Certainty = 1.0;
					epistemicModel.state.UpdateEpisim(new Concept[] {conceptG}, new Relation[] {});

					Deixis (TransformToSurface (GetGestureVector (messageStr, "left point")));
				} 
				else if (messageStr.StartsWith ("right point")) {
					conceptG = epistemicModel.state.GetConcept("point", ConceptType.ACTION, ConceptMode.G);
					conceptG.Certainty = 1.0;
					epistemicModel.state.UpdateEpisim(new Concept[] {conceptG}, new Relation[] {});

					Deixis (TransformToSurface (GetGestureVector (messageStr, "right point")));
				} 
				else if (messageStr.StartsWith ("grab")) {
					if ((graspedObj == null) && (eventConfirmation == "")) {
						if ((GetGestureContent (messageStr, "grab") == "") || (GetGestureContent (messageStr, "grab move") == "front")) {
							conceptG = epistemicModel.state.GetConcept ("grab", ConceptType.ACTION, ConceptMode.G);
							Debug.Log (EpistemicCertainty (conceptG));
							if (EpistemicCertainty (conceptG) < 0.5) {
								conceptG.Certainty = 0.5;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });

								Suggest ("grab");
							}
							else {
								conceptG.Certainty = 1.0;
								epistemicModel.state.UpdateEpisim(new Concept[] {conceptG}, new Relation[] {});

								Grab (true);
							}
						}
					}
					else {
						if ((graspedObj != null) || (indicatedObj != null)) {
							string prevInstruction = FindPreviousMatch ("grab");

							if (prevInstruction.StartsWith("grab move")) {
								HandleMoveSegment (prevInstruction);
							}
						}
					}
				}
				else if (messageStr.StartsWith ("posack")) {
					conceptG = epistemicModel.state.GetConcept("posack", ConceptType.ACTION, ConceptMode.G);
					conceptL = epistemicModel.state.GetConcept("YES", ConceptType.ACTION, ConceptMode.L);
					relation = epistemicModel.state.GetRelation(conceptG, conceptL);

					if (EpistemicCertainty(conceptG) < 0.5) {
						conceptG.Certainty = 0.5;

						Suggest ("posack");
					}
					else {
						conceptG.Certainty = 1.0;

						if (conceptL.Certainty > 0.0) {
							relation.Certainty = 1.0;
						}

						Acknowledge (true);
					}

					epistemicModel.state.UpdateEpisim(new Concept[] {conceptG, conceptL}, new Relation[] {relation});

				}
				else if (messageStr.StartsWith ("negack")) {
					conceptG = epistemicModel.state.GetConcept("negack", ConceptType.ACTION, ConceptMode.G);
					conceptL = epistemicModel.state.GetConcept("NO", ConceptType.ACTION, ConceptMode.L);
					relation = epistemicModel.state.GetRelation(conceptG, conceptL);

					if (EpistemicCertainty(conceptG) < 0.5) {
						conceptG.Certainty = 0.5;

						Suggest ("negack");
					}
					else {
						conceptG.Certainty = 1.0;

						if (conceptL.Certainty > 0.0) {
							relation.Certainty = 1.0;
						}

						Acknowledge (false);
					}

					epistemicModel.state.UpdateEpisim(new Concept[] {conceptG, conceptL}, new Relation[] {relation});
				}
			}
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("low")) {	// low as trigger
				messageStr = RemoveGestureTriggers (messageStr);
				if (messageStr.StartsWith ("left point")) {
					conceptG = epistemicModel.state.GetConcept ("point", ConceptType.ACTION, ConceptMode.G);
					if (EpistemicCertainty(conceptG) < 0.5) {
						conceptG.Certainty = 0.5;
						epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });
					}

					Suggest ("point");
				} 
				else if (messageStr.StartsWith ("right point")) {
					conceptG = epistemicModel.state.GetConcept ("point", ConceptType.ACTION, ConceptMode.G);
					if (EpistemicCertainty(conceptG) < 0.5) {
						conceptG.Certainty = 0.5;
						epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });
					}

					Suggest ("point");
				} 
				else if (messageStr.StartsWith ("grab")) {
					if (GetGestureContent (messageStr, "grab") == "") {
						conceptG = epistemicModel.state.GetConcept ("grab", ConceptType.ACTION, ConceptMode.G);
						if (EpistemicCertainty(conceptG) < 0.5) {
							conceptG.Certainty = 0.5;
							epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });
						}

						Suggest ("grab");
					}
				} 
				else if (messageStr.StartsWith ("posack")) {
					if (eventConfirmation != "") {
						conceptG = epistemicModel.state.GetConcept ("posack", ConceptType.ACTION, ConceptMode.G);
						if (EpistemicCertainty(conceptG) < 0.5) {
							conceptG.Certainty = 0.5;
							epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });
						}

						Suggest ("posack");
					}
				} 
				else if (messageStr.StartsWith ("negack")) {
					if (eventConfirmation != "") {
						conceptG = epistemicModel.state.GetConcept ("negack", ConceptType.ACTION, ConceptMode.G);
						if (EpistemicCertainty(conceptG) < 0.5) {
							conceptG.Certainty = 0.5;
							epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });
						}

						Suggest ("negack");
					}
				}
			} 
			else if (messageComponents[messageComponents.Length-1].Split(',')[0].EndsWith ("stop")) {	// stop as trigger
				messageStr = RemoveGestureTriggers (messageStr);
				string startSignal = FindStartSignal (messageStr);

				if (messageStr.StartsWith ("engage")) {
					if (GetGestureContent (messageStr, "engage") == "") {
						Engage (false);
					}
				} 
				else if (messageStr.StartsWith ("push")) {
					if (startSignal.EndsWith ("high")) {
						if (GetGestureContent (messageStr, "push") == "left") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });

								Suggest ("push left");
							}
							else {
								conceptG.Certainty = 1.0;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });

								Push ("left");
							}
						} 
						else if (GetGestureContent (messageStr, "push") == "right") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });

								Suggest ("push right");
							}
							else {
								conceptG.Certainty = 1.0;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });

								Push ("right");
							}
						} 
						else if (GetGestureContent (messageStr, "push") == "front") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });

								Suggest ("push front");
							}
							else {
								conceptG.Certainty = 1.0;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });

								Push ("front");
							}
						}
						else if (GetGestureContent (messageStr, "push") == "back") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });

								Suggest ("push back");
							}
							else {
								conceptG.Certainty = 1.0;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG }, new Relation[] { });

								Push ("back");
							}
						}
					} 
					else if (startSignal.EndsWith ("low")) {
						if (GetGestureContent (messageStr, "push") == "left") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });
							}

							Suggest ("push left");
						} 
						else if (GetGestureContent (messageStr, "push") == "right") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });
							}

							Suggest ("push right");
						} 
						else if (GetGestureContent (messageStr, "push") == "front") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });
							}

							Suggest ("push front");
						} 
						else if (GetGestureContent (messageStr, "push") == "back") {
							conceptG = epistemicModel.state.GetConcept ("push", ConceptType.ACTION, ConceptMode.G);
							conceptL = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
							if ((EpistemicCertainty(conceptG) < 0.5) || (EpistemicCertainty(conceptL) < 0.5)) {
								conceptG.Certainty = (conceptG.Certainty < 0.5) ? 0.5 : conceptG.Certainty;
								conceptL.Certainty = (conceptL.Certainty < 0.5) ? 0.5 : conceptL.Certainty;
								epistemicModel.state.UpdateEpisim (new Concept[] { conceptG,conceptL }, new Relation[] { });
							}

							Suggest ("push back");
						}
					}
				} 
				else if (messageStr.StartsWith ("grab")) {
					if (graspedObj != null) {
						string prevInstruction = FindPreviousMatch ("grab");

						if (prevInstruction.StartsWith("grab move")) {
							HandleMoveSegment (prevInstruction);
						}
						else if (GetGestureContent (messageStr, "grab") == "") {
							Grab (false);
						}
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("The {0} block?", attribute));
						objectConfirmation = objVoxemes [0].gameObject;
						LookAt(objectConfirmation);

						Concept attrConcept = epistemicModel.state.GetConcept (attribute.ToUpper(), ConceptType.PROPERTY, ConceptMode.L);
						attrConcept.Certainty = (attrConcept.Certainty < 0.5) ? 0.5 : attrConcept.Certainty;
						epistemicModel.state.UpdateEpisim (new Concept[]{ attrConcept }, new Relation[]{ });
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				if (eventManager.events.Count == 0) {
					LookForward();
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
							OutputHelper.PrintOutput (Role.Affector, string.Format ("The {0} block?", attribute));
							ReachFor (objVoxemes [0].gameObject);
							objectConfirmation = objVoxemes [0].gameObject;
							LookAt (objectConfirmation);

							Concept attrConcept = epistemicModel.state.GetConcept (attribute.ToUpper(), ConceptType.PROPERTY, ConceptMode.L);
							attrConcept.Certainty = (attrConcept.Certainty < 0.5) ? 0.5 : attrConcept.Certainty;
							epistemicModel.state.UpdateEpisim (new Concept[]{ attrConcept }, new Relation[]{ });
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Should I {0}?", confirmationTexts [actionOptions [0]]));
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
			string dir = GetGestureContent (gesture, "grab move");
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move something this way?"));
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move this this way?"));
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to move this this way?"));
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Do you want me to grab something?"));
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to grab this?"));
					MoveToPerform ();
					gestureController.PerformGesture (AvatarGesture.RARM_CARRY_STILL);
					PopulateGrabOptions (indicatedObj, CertaintyMode.Suggest);
				}
			}
		}
		else if (gesture.StartsWith("push")) {
			string dir = GetGestureContent (gesture, "push");
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to push something this way?"));
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("Are you asking me to push this this way?"));
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
				OutputHelper.PrintOutput (Role.Affector, string.Format ("It looks like you're pointing to something."));
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
				OutputHelper.PrintOutput (Role.Affector, string.Format ("It looks like you're pointing to something."));
				MoveToPerform ();
				gestureController.PerformGesture (performGesture);
			}
		}
		else if (gesture.StartsWith("posack")) {
			if (eventManager.events.Count == 0) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Yes?"));
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
				OutputHelper.PrintOutput (Role.Affector, string.Format ("No?"));
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
					OutputHelper.PrintOutput (Role.Affector, "OK.");
					eventConfirmation = "";
					if (indicatedObj != null) {
						ReachFor (indicatedObj);
					}
				}
			} 
			else if (eventConfirmation == "negack") {	// no? no 
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, "OK.");
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
						OutputHelper.PrintOutput (Role.Affector, "OK.");
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
						OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
					}
				}
			}
			else if (suggestedActions.Count > 0) {
				suggestedActions.Remove (suggestedActions [0]);

				if (suggestedActions.Count == 0) {
					LookForward();
					OutputHelper.PrintOutput (Role.Affector, "OK.");
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
					OutputHelper.PrintOutput (Role.Affector, "OK.");
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
					OutputHelper.PrintOutput (Role.Affector, "OK.");
					eventConfirmation = "";
					indicatedObj = null;
					TurnForward ();
					LookForward ();
				}
			}
			else if (eventConfirmation == "negack") {	// no? yes 
				if (eventManager.events.Count == 0) {
					LookForward();
					OutputHelper.PrintOutput (Role.Affector, "OK.");
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
							OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
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
					OutputHelper.PrintOutput (Role.Affector, "OK.");
				}
			} 
			else if (objectConfirmation != null) {
				if (eventManager.events.Count == 0) {
					LookForward();
					OutputHelper.PrintOutput (Role.Affector, "OK, go on.");
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
							OutputHelper.PrintOutput (Role.Affector, string.Format ("What do you want me to {0}?", suggestedActions [0].Split ('(') [0]));
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

						if (synVision != null) {
							if (synVision.enabled) {
								isKnown = synVision.IsKnown (block);
							}
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

						if (synVision != null) {
							if (synVision.enabled) {
								isKnown = synVision.IsKnown (match);
							}
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Which {0} block?", color.ToLower ()));
					}
					else if ((eventManager.events.Count == 0) && (objectConfirmation == null)) {
						LookForward();
						OutputHelper.PrintOutput (Role.Affector, string.Format ("None of the blocks over here is {0}.", color.ToLower ()));
					}
				}
				else {
					if ((eventManager.events.Count == 0) && (eventConfirmation == "")) {
						LookForward();
						OutputHelper.PrintOutput (Role.Affector, string.Format ("OK, go on."));
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Should I forget about this {0} block?", attr));
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

		epistemicModel.engaged = state;
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
				bool isKnown = true;

				if (synVision != null) {
					if (synVision.enabled) {
						isKnown = synVision.IsKnown (block);
					}
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
				OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
			}
		}
	}

	void Deixis(GameObject obj) {
		bool isKnown = true;

		if (synVision != null) {
			if (synVision.enabled) {
				isKnown = synVision.IsKnown (obj);
			}
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

			if (synVision != null) {
				if (synVision.enabled) {
					isKnown = synVision.IsKnown (block);
				}
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
						OutputHelper.PrintOutput (Role.Affector, string.Format ("Should I forget about this {0} block?", themeAttr));
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
					OutputHelper.PrintOutput (Role.Affector, "OK, go on.");
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
			OutputHelper.PrintOutput (Role.Affector, "");
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("OK."));
				}
			} 
			else if ((graspedObj == null) && (indicatedObj == null) && (objectConfirmation == null)) {
				//OutputHelper.PrintOutput (Role.Affector, "Sorry, I don't know what you mean.");
				if (eventManager.events.Count == 0) {
					OutputHelper.PrintOutput (Role.Affector, string.Format ("What do you want me to grab?"));
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

	void HandleMoveSegment(string instruction) {
		actionOptions.Clear ();
		suggestedActions.Clear ();

		Concept moveConcept = epistemicModel.state.GetConcept ("move", ConceptType.ACTION, ConceptMode.G);
		Concept grabConcept = epistemicModel.state.GetConcept ("grab", ConceptType.ACTION, ConceptMode.G);

		if (instruction.EndsWith ("high")) {
			if (GetGestureContent (instruction, "grab move") == "left") {
				Concept dirConcept = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					//moveConcept.Certainty = (moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty;
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move left");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("left");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "right") {
				Concept dirConcept = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move right");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("right");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "front") {
				Concept dirConcept = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move front");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("front");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "back") {
				Concept dirConcept = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move back");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("back");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "left front") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConceptX) < 0.5)
					|| (EpistemicCertainty(dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					dirConceptZ.Certainty = (dirConceptZ.Certainty < 0.5) ? 0.5 : dirConceptZ.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });

					Suggest ("grab move left front");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("left front");
				}
			} 
			else if (GetGestureContent (instruction, "grab move") == "right front") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConceptX) < 0.5)
					|| (EpistemicCertainty(dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					dirConceptZ.Certainty = (dirConceptZ.Certainty < 0.5) ? 0.5 : dirConceptZ.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });

					Suggest ("grab move right front");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("right front");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "left back") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConceptX) < 0.5)
					|| (EpistemicCertainty(dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					dirConceptZ.Certainty = (dirConceptZ.Certainty < 0.5) ? 0.5 : dirConceptZ.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });

					Suggest ("grab move left back");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("left back");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "right back") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConceptX) < 0.5)
					|| (EpistemicCertainty(dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					dirConceptZ.Certainty = (dirConceptZ.Certainty < 0.5) ? 0.5 : dirConceptZ.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });

					Suggest ("grab move right back");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("right back");
				}
			}
			else if (GetGestureContent (instruction, "grab move") == "up") {
				Concept dirConcept = epistemicModel.state.GetConcept ("UP", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move up");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("up");
				}
			} 
			else if (GetGestureContent (instruction, "grab move") == "down") {
				Concept dirConcept = epistemicModel.state.GetConcept ("DOWN", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty(moveConcept) < 0.5) || (EpistemicCertainty(dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : ((moveConcept.Certainty >= 0.5) ? 1.0 : moveConcept.Certainty);
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });

					Suggest ("grab move down");
				}
				else {
					moveConcept.Certainty = 1.0;

					Move ("down");
				}
			}
		} 
		else if (instruction.EndsWith ("low")) {
			if (GetGestureContent (instruction, "grab move") == "left") {
				Concept dirConcept = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move left");
			}
			else if (GetGestureContent (instruction, "grab move") == "right") {
				Concept dirConcept = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move right");
			}
			else if (GetGestureContent (instruction, "grab move") == "front") {
				Concept dirConcept = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move front");
			}
			else if (GetGestureContent (instruction, "grab move") == "back") {
				Concept dirConcept = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move back");
			} 
			else if (GetGestureContent (instruction, "grab move") == "left front") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConceptX) < 0.5) ||
					(EpistemicCertainty (dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });
				}

				Suggest ("grab move left front");
			} 
			else if (GetGestureContent (instruction, "grab move") == "right front") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("FRONT", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConceptX) < 0.5) ||
					(EpistemicCertainty (dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });
				}

				Suggest ("grab move right front");
			}
			else if (GetGestureContent (instruction, "grab move") == "left back") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("LEFT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConceptX) < 0.5) ||
					(EpistemicCertainty (dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });
				}

				Suggest ("grab move left back");
			}
			else if (GetGestureContent (instruction, "grab move") == "right back") {
				Concept dirConceptX = epistemicModel.state.GetConcept ("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
				Concept dirConceptZ = epistemicModel.state.GetConcept ("BACK", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConceptX) < 0.5) ||
					(EpistemicCertainty (dirConceptZ) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConceptX.Certainty = (dirConceptX.Certainty < 0.5) ? 0.5 : dirConceptX.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConceptX,dirConceptZ }, new Relation[] { });
				}

				Suggest ("grab move right back");
			}
			else if (GetGestureContent (instruction, "grab move") == "up") {
				Concept dirConcept = epistemicModel.state.GetConcept ("UP", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move up");
			} 
			else if (GetGestureContent (instruction, "grab move") == "down") {
				Concept dirConcept = epistemicModel.state.GetConcept ("DOWN", ConceptType.PROPERTY, ConceptMode.L);
				if ((EpistemicCertainty (moveConcept) < 0.5) || (EpistemicCertainty (dirConcept) < 0.5)) {
					moveConcept.Certainty = (moveConcept.Certainty < 0.5) ? 0.5 : moveConcept.Certainty;
					dirConcept.Certainty = (dirConcept.Certainty < 0.5) ? 0.5 : dirConcept.Certainty;
					epistemicModel.state.UpdateEpisim (new Concept[] { dirConcept }, new Relation[] { });
				}

				Suggest ("grab move down");
			}
		}

		grabConcept.Certainty = 1.0;
		epistemicModel.state.UpdateEpisim(new Concept[] {moveConcept,grabConcept}, new Relation[] {});
	}

	void PopulateMoveOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<object> placementOptions = FindPlacementOptions (theme, dir);

		if (useOrderingHeuristics) {
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
							if (!actionOptions.Contains (string.Format ("put({0},on({1}))", theme.name, obj.name))) {
								actionOptions.Add (string.Format ("put({0},on({1}))", theme.name, obj.name));
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
					if (!actionOptions.Contains (string.Format ("put({0},{1})", theme.name,
						    Helper.VectorToParsable (target)))) {
						actionOptions.Add (string.Format ("put({0},{1})", theme.name,
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
				if (!actionOptions.Contains (string.Format ("lift({0})", theme.name))) {
					actionOptions.Add (string.Format ("lift({0})", theme.name));
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
					if (!actionOptions.Contains (string.Format ("put({0},{1})", theme.name, Helper.VectorToParsable (target)))) {
						actionOptions.Add (string.Format ("put({0},{1})", theme.name,
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
	}

	void Move(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		if (eventConfirmation == "") {
			OutputHelper.PrintOutput (Role.Affector, "");
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

	void PopulatePushOptions(GameObject theme, string dir, CertaintyMode certainty = CertaintyMode.Act) {
		List<object> placementOptions = FindPlacementOptions (theme, dir);

		if (useOrderingHeuristics) {
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
					   (Helper.GetObjectWorldSize (theme).min.y >= Helper.GetObjectWorldSize (obj).min.y)) {	// must fit in target destination and be on the same surface
						string objAttr = string.Empty;
						if (obj.GetComponent<Voxeme> () != null) {
							objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs [0].Value;	// just grab the first one for now
						}

						if (certainty == CertaintyMode.Act) {
							if (!actionOptions.Contains (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name))) {
								actionOptions.Add (string.Format ("slide({0},{1}({2}))", theme.name, directionPreds [relativeDir [dir]], obj.name));
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
				}
			} 
			else if (option is Vector3) {
				Vector3 target = (Vector3)option;

				if (certainty == CertaintyMode.Act) {
					if (!actionOptions.Contains (string.Format ("slide({0},{1})", theme.name,
						    Helper.VectorToParsable (target)))) {
						actionOptions.Add (string.Format ("slide({0},{1})", theme.name,
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
	}

	void Push(string dir) {
		if (eventManager.events.Count > 0) {
			return;
		}

		if (eventConfirmation == "") {
			OutputHelper.PrintOutput (Role.Affector, "");
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
					OutputHelper.PrintOutput (Role.Affector, string.Format ("What do you want me to push?"));
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
		Bounds themeBounds = Helper.GetObjectWorldSize (theme);
		foreach (Region region in orthogonalRegions) {
			if (region.Contains(theme)) {
				foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
					if (block.activeInHierarchy) {
						if (block != theme) {	// if candidate block has clear surface and is not indicatedObj (?--shouldn't this be null at this point)
							if ((bool)(Type.GetType("QSR.QSR").GetMethod(qsr).Invoke(null, new object[]{ Helper.GetObjectWorldSize (block), themeBounds })) &&	// if it's to the left of the grasped block
								(region.Contains (block))) {	// and in the same region (orthogonal to dir of movement)
								if (!objectMatches.Contains (block)) {
									objectMatches.Add (block);
								}
							}
						}
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

	void MoveToPerform() {
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

	void LookForward() {
		Diana.GetComponent<LookAtIK> ().solver.target.position = headTargetDefault;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.8f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 0.0f;
	}

	void AllowHeadMotion() {
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.8f;
	}

	void LookAt(GameObject obj) {
		Vector3 target = new Vector3 (obj.transform.position.x/2.0f,
			(obj.transform.position.y+headTargetDefault.y)/2.0f, obj.transform.position.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = obj.transform.position;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 1.0f;
	}

	void LookAt(Vector3 point) {
		Vector3 target = new Vector3 (point.x/2.0f, (point.y+headTargetDefault.y)/2.0f, point.z/2.0f);
		Diana.GetComponent<LookAtIK> ().solver.target.position = target;
		Diana.GetComponent<LookAtIK> ().solver.IKPositionWeight = 1.0f;
		Diana.GetComponent<LookAtIK> ().solver.bodyWeight = 0.0f;
		Diana.GetComponent<LookAtIK> ().solver.headWeight = 1.0f;
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
			rightRegion.center.y,coord.z))) {
			ikControl.leftHandObj.transform.position = coord+offset;
			InteractionHelper.SetLeftHandTarget (Diana, ikControl.leftHandObj);
		}

		LookForward ();
	}

	void ReachFor(GameObject obj) {
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

	bool CanPrompt() {
		return ((eventManager.events.Count == 0) && (suggestedActions.Count == 0));
	}

	void ReturnToRest(object sender, EventArgs e) {
		if (!interactionSystem.IsPaused (FullBodyBipedEffector.LeftHand) &&
		    !interactionSystem.IsPaused (FullBodyBipedEffector.RightHand)) {
			TurnForward ();
		}
	}

	void ConnectionLost(object sender, EventArgs e) {
		LookForward();

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
