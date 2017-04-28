using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Global;
using Network;
using QSR;
using RCC;

public class JointGestureDemo : MonoBehaviour {

	CSUClient csuClient;
	EventManager eventManager;
	InteractionPrefsModalWindow interactionPrefs;

	GameObject Diana;

	IKControl ikControl;
	IKTarget leftTarget;
	IKTarget rightTarget;
	IKTarget headTarget;

	Vector3 leftTargetDefault;
	Vector3 rightTargetDefault;
	Vector3 headTargetDefault;

	public GameObject demoSurface;
	public List<GameObject> blocks;
	public GameObject indicatedObj = null;
	public GameObject indicatedObjObj = null;
	public GameObject graspedObj = null;

	public Region indicatedRegion = null;

	Region leftRegion;
	Region rightRegion;
	Region frontRegion;
	Region backRegion;

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
		eventManager = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();
		interactionPrefs = gameObject.GetComponent<InteractionPrefsModalWindow> ();

		Diana = GameObject.Find ("Diana");

		ikControl = Diana.GetComponent<IKControl> ();
		leftTarget = ikControl.leftHandObj.GetComponent<IKTarget> ();
		rightTarget = ikControl.rightHandObj.GetComponent<IKTarget> ();
		headTarget = ikControl.lookObj.GetComponent<IKTarget> ();

		leftTargetDefault = ikControl.leftHandObj.transform.position;
		rightTargetDefault = ikControl.rightHandObj.transform.position;
		headTargetDefault = ikControl.lookObj.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		if (csuClient == null) {
			csuClient = GameObject.Find ("CommunicationsBridge").GetComponent<PluginImport> ().CSUClient;
			csuClient.GestureReceived += ReceivedGesture;
			csuClient.ConnectionLost += ConnectionLost;

			for (int i = 0; i < blocks.Count; i++) {
				blocks[i] = Helper.GetMostImmediateParentVoxeme (blocks [i]);
			}
		}

		if (demoSurface != Helper.GetMostImmediateParentVoxeme (demoSurface)) {
			demoSurface = Helper.GetMostImmediateParentVoxeme (demoSurface);
		}

		if (leftRegion == null) {
			leftRegion = new Region (new Vector3 (0.0f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, -0.7f),
				new Vector3 (0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, 0.1f));
			leftRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			leftRegionHighlight.name = "LeftRegionHighlight";
			leftRegionHighlight.transform.position = leftRegion.center;
			leftRegionHighlight.transform.localScale = new Vector3 (.1f*(leftRegion.max.x - leftRegion.min.x),
				1.0f, .1f*(leftRegion.max.z - leftRegion.min.z));
			leftRegionHighlight.SetActive (false);
		}

		if (rightRegion == null) {
			rightRegion = new Region (new Vector3 (-0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, -0.7f),
				new Vector3 (0.0f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, 0.1f));
			rightRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			rightRegionHighlight.name = "RightRegionHighlight";
			rightRegionHighlight.transform.position = rightRegion.center;
			rightRegionHighlight.transform.localScale = new Vector3 (.1f*(rightRegion.max.x - rightRegion.min.x),
				1.0f, .1f*(rightRegion.max.z - rightRegion.min.z));
			rightRegionHighlight.SetActive (false);
		}

		if (frontRegion == null) {
			frontRegion = new Region (new Vector3 (-0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, -0.7f),
				new Vector3 (0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, -0.3f));
			frontRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			frontRegionHighlight.name = "FrontRegionHighlight";
			frontRegionHighlight.transform.position = frontRegion.center;
			frontRegionHighlight.transform.localScale = new Vector3 (.1f*(frontRegion.max.x - frontRegion.min.x),
				1.0f, .1f*(frontRegion.max.z - frontRegion.min.z));
			frontRegionHighlight.SetActive (false);
		}

		if (backRegion == null) {
			backRegion = new Region (new Vector3 (-0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, -0.3f),
				new Vector3 (0.85f, Helper.GetObjectWorldSize(demoSurface).max.y+Constants.EPSILON, 0.1f));
			backRegionHighlight = GameObject.CreatePrimitive(PrimitiveType.Plane);
			backRegionHighlight.name = "BackRegionHighlight";
			backRegionHighlight.transform.position = backRegion.center;
			backRegionHighlight.transform.localScale = new Vector3 (.1f*(backRegion.max.x - backRegion.min.x),
				1.0f, .1f*(backRegion.max.z - backRegion.min.z));
			backRegionHighlight.SetActive (false);
		}
	}

	void ReceivedGesture(object sender, EventArgs e) {
		Debug.Log (((GestureEventArgs)e).Content);

		string gestureStr = ((GestureEventArgs)e).Content.Split (';') [0];

		if (gestureStr.EndsWith ("start")) {	// start as trigger
			gestureStr = RemoveGestureTriggers (gestureStr);
			if (gestureStr.StartsWith ("engage")) {
				if (GetGestureContent (gestureStr, "engage") == "") {
					Engage (true);
				}
			}
			else if (gestureStr.StartsWith ("point")) {
				if (GetGestureContent (gestureStr, "point") == "left") {
					Deixis ("left");
				}
				else if (GetGestureContent (gestureStr, "point") == "right") {
					Deixis ("right");
				}
				else if (GetGestureContent (gestureStr, "point") == "front") {
					Deixis ("front");
				}
				else if (GetGestureContent (gestureStr, "point") == "down") {
					Deixis ("down");
				}
			}
			else if (gestureStr.StartsWith ("grab")) {
				if (GetGestureContent (gestureStr, "grab") == "") {
					Grab (true);
				}
			}
			else if (gestureStr.StartsWith ("posack")) {
				Acknowledge (true);
			}
			else if (gestureStr.StartsWith ("negack")) {
				Acknowledge (false);
			}
		}
		else if (gestureStr.EndsWith("stop")) {	// stop as trigger
			gestureStr = RemoveGestureTriggers(gestureStr);
			if (gestureStr.StartsWith ("engage")) {
				if (GetGestureContent (gestureStr, "engage") == "") {
					Engage (false);
				}
			}
			else if (gestureStr.StartsWith ("push")) {
				if (GetGestureContent(gestureStr, "push") == "left") {
					Push ("left");
				}
				else if (GetGestureContent(gestureStr, "push") == "right") {
					Push ("right");
				}
				else if (GetGestureContent(gestureStr, "push") == "front") {
					Push ("front");
				}
				else if (GetGestureContent(gestureStr, "push") == "back") {
					Push ("back");
				}
			}
			else if (gestureStr.StartsWith ("grab move")) {
				if (GetGestureContent(gestureStr, "grab move") == "left") {
					Move ("left");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "right") {
					Move ("right");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "front") {
					Move ("front");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "back") {
					Move ("back");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "left front") {
					Move ("left front");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "right front") {
					Move ("right front");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "left back") {
					Move ("left back");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "right back") {
					Move ("right back");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "up") {
					Move ("up");
				}
				else if (GetGestureContent(gestureStr, "grab move") == "down") {
					Move ("down");
				}
			}
			else if (gestureStr.StartsWith ("grab")) {
				if (GetGestureContent(gestureStr, "grab") == "") {
					Grab (false);
				}
			}
		}
	}

	string RemoveGestureTriggers(string receivedData) {
		return receivedData.Replace ("start", "").Replace ("stop", "");
	}

	string GetGestureContent(string receivedData, string gestureCode) {
		return receivedData.Replace (gestureCode, "").Split () [1];
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
						OutputHelper.PrintOutput (OutputController.Role.Affector, string.Format ("Are you pointing to the {0} block?", attribute));
						objectConfirmation = objVoxemes [0].gameObject;
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				OutputHelper.PrintOutput (OutputController.Role.Affector, string.Format ("Should I {0}?", confirmationTexts[actionOptions [0]]));
				eventConfirmation = actionOptions [0];
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

						string attribute = ((Vox.VoxAttributesAttr)uniqueAttrs [0]).Value.ToString ();
						OutputHelper.PrintOutput (OutputController.Role.Affector, string.Format ("Are you pointing to the {0} block?", attribute));

						ReachFor (objVoxemes [0].gameObject);

						objectConfirmation = objVoxemes [0].gameObject;
					}
				}
			}
			else if ((content is IList) && (content.GetType().IsGenericType) && 
				content.GetType().IsAssignableFrom(typeof(List<string>))) {	// disambiguate events
				actionOptions = (List<string>)content;

				OutputHelper.PrintOutput (OutputController.Role.Affector, string.Format ("Should I {0}?", confirmationTexts[actionOptions [0]]));
				eventConfirmation = actionOptions [0];
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
					OutputHelper.PrintOutput (OutputController.Role.Affector, "Sorry, I don't know what you mean.");
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
					OutputHelper.PrintOutput (OutputController.Role.Affector, "Sorry, I don't know what you mean.");
				}
			}
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
				}
				else if (eventConfirmation.StartsWith ("put")) {
					graspedObj = null;
				}
				else if (eventConfirmation.StartsWith ("slide")) {
					indicatedObj = null;
				}
				else if (eventConfirmation.StartsWith ("ungrasp")) {
					graspedObj = null;
				}

				eventManager.InsertEvent ("", 0);
				eventManager.InsertEvent (eventConfirmation, 1);
				eventConfirmation = "";
				actionOptions.Clear ();
				objectMatches.Clear ();
				confirmationTexts.Clear ();

				OutputHelper.PrintOutput (OutputController.Role.Affector, "OK.");
			}
			else if (objectConfirmation != null) {
				indicatedObj = objectConfirmation;
				objectConfirmation = null;
				objectMatches.Clear ();

				OutputHelper.PrintOutput (OutputController.Role.Affector, "OK, go on.");
			}
		}
	}

	void Engage(bool state) {
		if (state == true) {
			sessionCounter++;
			if (sessionCounter > 1) {
				OutputHelper.PrintOutput (OutputController.Role.Affector, "Welcome back!");
			}
			else {
				OutputHelper.PrintOutput (OutputController.Role.Affector, "Hello.");
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

			OutputHelper.PrintOutput (OutputController.Role.Affector, "Bye!");

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

	void Deixis(string dir) {
		OutputHelper.PrintOutput (OutputController.Role.Affector, "");
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
				OutputHelper.PrintOutput (OutputController.Role.Affector, "Sorry, I don't know what you mean.");
				return;
			}

			TurnForward ();
			LookAt (region.center);

			foreach (GameObject block in blocks) {
				if (block.activeInHierarchy) {
					if (region.Contains (new Vector3 (block.transform.position.x,
						   region.center.y, block.transform.position.z))) {
						bool surfaceClear = true;
						foreach (GameObject otherBlock in blocks) {
							if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
								(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
								(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
								(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
								surfaceClear = false;
							}
						}
						if ((!objectMatches.Contains (block)) && (surfaceClear)) {
							objectMatches.Add (block);
						}
					} 
				}
			}

			if (objectMatches.Count > 0) {
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
			else {	// indicating region
				indicatedRegion = region;
				OutputHelper.PrintOutput (OutputController.Role.Affector, "Sorry, I don't know what you're pointing at.");
			}
		}
	}

	void Grab(bool state) {
		OutputHelper.PrintOutput (OutputController.Role.Affector, "");
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
					graspedObj = indicatedObj;
					indicatedObj = null;
				}
			}
			else {
				OutputHelper.PrintOutput (OutputController.Role.Affector, "Sorry, I don't know what you mean.");
			}
		}
		else {
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
					actionOptions.Clear ();
					confirmationTexts.Clear ();
					graspedObj = null;
				}
			}
		}
	}

	void Move(string dir) {
		OutputHelper.PrintOutput (OutputController.Role.Affector, "");
		if (graspedObj != null) {
			if (dir == "left") {
				if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					frontRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) && 
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
				else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						backRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) && 
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

				string graspedAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					graspedAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", graspedObj.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name),
						string.Format ("put the {0} block on the {1} block", graspedAttr, objAttr));
				}

				if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					frontRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front left part", graspedAttr));
				}
				else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					backRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back left part", graspedAttr));
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
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						frontRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						backRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "right") {
				if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					frontRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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
				else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					backRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Right (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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

				string graspedAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					graspedAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", graspedObj.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name),
						string.Format ("put the {0} block on the {1} block", graspedAttr, objAttr));
				}

				if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					frontRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{rightRegion, frontRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front right part", graspedAttr));
				}
				else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					backRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{rightRegion, backRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back right part", graspedAttr));
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
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (frontRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						frontRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, frontRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						backRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ rightRegion, backRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "front") {
				if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					leftRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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
				else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					rightRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.InFront (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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

				string graspedAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					graspedAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on{1}))", graspedObj.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name),
						string.Format ("put the {0} block on the {1} block", graspedAttr, objAttr));
				}

				if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					leftRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{frontRegion, leftRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (Helper.FindClearRegion (demoSurface, new Region[]{frontRegion, leftRegion}, graspedObj).center)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front left part", graspedAttr));
				}
				else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					rightRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{frontRegion, rightRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (Helper.FindClearRegion (demoSurface, new Region[]{frontRegion, rightRegion}, graspedObj).center)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's front right part", graspedAttr));
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
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						leftRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, leftRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						rightRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ frontRegion, rightRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "back") {
				if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					leftRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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
				else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					rightRegion.center.y, graspedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool surfaceClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Above(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) &&
									(!QSR.QSR.Left(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(!QSR.QSR.Right(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block))) && 
									(RCC8.EC(Helper.GetObjectWorldSize (otherBlock), Helper.GetObjectWorldSize (block)))) {
									Debug.Log (otherBlock);
									surfaceClear = false;
								}
							}

							if ((block != indicatedObj) && (surfaceClear)) {
								if ((QSR.QSR.Behind (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (graspedObj))) &&
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

				string graspedAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					graspedAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				Vector3 target = Vector3.zero;

				foreach (GameObject obj in objectMatches) {
					string objAttr = string.Empty;
					if (obj.GetComponent<Voxeme> () != null) {
						objAttr = obj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
					}
					Debug.Log (string.Format ("put({0},(on({1}))", graspedObj.name, obj.name));
					target = obj.transform.position;
					actionOptions.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("put({0},on({1}))", graspedObj.name, obj.name),
						string.Format ("put the {0} block on the {1} block", graspedAttr, objAttr));
				}

				if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					leftRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{backRegion, leftRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back left part", graspedAttr));
				}
				else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
					rightRegion.center.y, graspedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{backRegion, rightRegion}, graspedObj).center;
					actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name, Helper.VectorToParsable (target)),
						string.Format ("put the {0} block in the table's back right part", graspedAttr));
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
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					if (leftRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						leftRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, leftRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (rightRegion.Contains (new Vector3 (graspedObj.transform.position.x,
						rightRegion.center.y, graspedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ backRegion, rightRegion }, graspedObj).center;
						eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "up") {
				string objAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					objAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}

				actionOptions.Add (string.Format ("lift({0})", graspedObj.name));
				confirmationTexts.Add (string.Format ("lift({0})", graspedObj.name), string.Format ("lift the {0} block ", objAttr));
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
					eventManager.InsertEvent (string.Format ("lift({0})", graspedObj.name), 1);
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "down") {
				string objAttr = string.Empty;
				if (graspedObj.GetComponent<Voxeme> () != null) {
					objAttr = graspedObj.GetComponent<Voxeme> ().voxml.Attributes.Attrs[0].Value;	// just grab the first one for now
				}
					
				Vector3 target = new Vector3 (graspedObj.transform.position.x,
					Helper.GetObjectWorldSize (demoSurface).max.y,
					graspedObj.transform.position.z);
				actionOptions.Add (string.Format ("put({0},{1})", graspedObj.name,
					Helper.VectorToParsable (target)));
				confirmationTexts.Add (string.Format ("put({0},{1})", graspedObj.name,
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
						graspedObj = null;
						actionOptions.Clear ();
						objectMatches.Clear ();
					}
				}
				else {
					eventManager.InsertEvent ("", 0);
					eventManager.InsertEvent (string.Format ("put({0},{1})", graspedObj.name,
						Helper.VectorToParsable (new Vector3 (graspedObj.transform.position.x,
							Helper.GetObjectWorldSize (demoSurface).max.y,
							graspedObj.transform.position.z))), 1);
					graspedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
		}
	}

	void Push(string dir) {
		OutputHelper.PrintOutput (OutputController.Role.Affector, "");
		if (indicatedObj != null) {
			if (dir == "left") {
				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					frontRegion.center.y, indicatedObj.transform.position.z))) {
					foreach (GameObject block in blocks) {	// find any objects in the direction relative to the grasped object
						if (block.activeInHierarchy) {
							bool sideClear = true;
							foreach (GameObject otherBlock in blocks) {
								if ((QSR.QSR.Left(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
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
								if ((QSR.QSR.Left(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock))) &&
									(RCC8.EC(Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (otherBlock)))) {
									sideClear = false;
								}
							}

							if ((block != indicatedObj) && (sideClear)) {
								if ((QSR.QSR.Left (Helper.GetObjectWorldSize (block), Helper.GetObjectWorldSize (indicatedObj))) &&
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
					actionOptions.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name));
					confirmationTexts.Add (string.Format ("slide({0},left({1}))", indicatedObj.name, obj.name),
						string.Format ("push the {0} block to the right of the {1} block", indAttr, objAttr));
				}

				if (frontRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					frontRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, frontRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the front left of the table", indAttr));
				}
				else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
					backRegion.center.y, indicatedObj.transform.position.z))) {
					target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
					actionOptions.Add (string.Format ("slide({0},{1})", indicatedObj.name,
						Helper.VectorToParsable (target)));
					confirmationTexts.Add (string.Format ("slide({0},{1})", indicatedObj.name, Helper.VectorToParsable (target)),
						string.Format ("push the {0} block to the back left of the table", indAttr));
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
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					else if (backRegion.Contains (new Vector3 (indicatedObj.transform.position.x,
						backRegion.center.y, indicatedObj.transform.position.z))) {
						target = Helper.FindClearRegion (demoSurface, new Region[]{ leftRegion, backRegion }, indicatedObj).center;
						eventManager.InsertEvent (string.Format ("slide({0},{1})", indicatedObj.name,
							Helper.VectorToParsable (target)), 1);
					}
					TurnToAccess (target);
					indicatedObj = null;
					actionOptions.Clear ();
					objectMatches.Clear ();
				}
			}
			else if (dir == "right") {
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

	void LookAt(Vector3 point) {
		//GameObject grasper;

		//Bounds bounds = Helper.GetObjectWorldSize (obj);

		// which hand is closer?
//		float leftToGoalDist = (leftGrasper.transform.position - bounds.ClosestPoint (leftGrasper.transform.position)).magnitude;
//		float rightToGoalDist = (rightGrasper.transform.position - bounds.ClosestPoint (rightGrasper.transform.position)).magnitude;
//
//		if (leftToGoalDist < rightToGoalDist) {
//			grasper = leftGrasper;
//			graspController.grasper = (int)Gestures.HandPose.LeftPoint;
//		}
//		else {
//			grasper = rightGrasper;
//			graspController.grasper = (int)Gestures.HandPose.RightPoint;
//		}

		//IKControl ikControl = Wilson.GetComponent<IKControl> ();
		if (ikControl != null) {
			Vector3 target = new Vector3 (point.x/2.0f, (point.y+headTargetDefault.y)/2.0f, point.z/2.0f);
 			headTarget.targetPosition = target;
		}
	}

	void LookForward() {
		if (ikControl != null) {
			headTarget.targetPosition = headTargetDefault;
		}
	}

	void TurnToAccess(Vector3 point) {
		GameObject leftGrasper = Diana.GetComponent<Animator>().GetBoneTransform (HumanBodyBones.LeftHand).transform.gameObject;
		GameObject rightGrasper = Diana.GetComponent<Animator>().GetBoneTransform (HumanBodyBones.RightHand).transform.gameObject;
		GameObject grasper = null;

		// which hand is closer?
		float leftToGoalDist = (leftGrasper.transform.position - point).magnitude;
		float rightToGoalDist = (rightGrasper.transform.position - point).magnitude;

		if (leftToGoalDist < rightToGoalDist) {
			grasper = leftGrasper;
		}
		else {
			grasper = rightGrasper;
		}

		Vector3 offset = point - new Vector3 (grasper.transform.position.x, point.y, grasper.transform.position.z);
		offset = Quaternion.Euler (0.0f,
			-45.0f*(point.x-Diana.transform.position.x)/Mathf.Abs(point.x-Diana.transform.position.x),
			0.0f) * offset;
		Diana.GetComponent<IKControl>().targetRotation = Quaternion.LookRotation (offset,Vector3.up).eulerAngles;
	}

	void TurnForward() {
		Diana.GetComponent<IKControl> ().targetRotation = Vector3.zero;
	}

	void ReachFor(GameObject obj) {
		Bounds bounds = Helper.GetObjectWorldSize(obj);
		Vector3 offset = Diana.GetComponent<GraspScript>().graspTrackerOffset;

		// which region is obj in?
		if (leftRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y,obj.transform.position.z))) {
			ikControl.rightHandObj.transform.position = new Vector3(bounds.center.x,bounds.max.y+0.1f,bounds.min.z)+offset;
		}
		else if (rightRegion.Contains(new Vector3(obj.transform.position.x,
			leftRegion.center.y,obj.transform.position.z))) {
			ikControl.leftHandObj.transform.position = new Vector3(bounds.center.x,bounds.max.y+0.1f,bounds.min.z)+offset;
		}
	}

	void ConnectionLost(object sender, EventArgs e) {
		if (sessionCounter >= 1) {
			OutputHelper.PrintOutput (OutputController.Role.Affector, "Hey, where'd you go?");
		}
		else {
			OutputHelper.PrintOutput (OutputController.Role.Affector, "Anyone there?");
		}
	}
}
