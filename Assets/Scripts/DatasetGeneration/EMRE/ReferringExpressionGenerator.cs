using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.Vox;

public class ReferringExpressionGenerator : MonoBehaviour {
	GameObject behaviorController;
	Predicates preds;
	EventManager eventManager;

	Animator spriteAnimator;
	Timer focusTimeoutTimer;
	Timer referWaitTimer;

	Dictionary<string, string> predToString = new Dictionary<string, string>() {
		{"left", "right of"},
		{"right", "left of"},
		{"in_front", "in front of"},
		{"behind", "behind"},
		{"left,touching", "right of and touching"},
		{"right,touching", "left of and touching"},
		{"in_front,touching", "in front of and touching"},
		{"behind,touching", "behind and touching"},
		{"touching,left", "right of and touching"},
		{"touching,right", "left of and touching"},
		{"touching,in_front", "in front of and touching"},
		{"touching,behind", "behind and touching"},
		{"touching", "touching"}
	};

	bool itemsInited;
	bool resituateItems;
	bool timeoutFocus, refer;

	string descriptorString = string.Empty;

	public List<GameObject> landmarks;

	public int focusTimeoutTime;
	public int referWaitTime;

	public JointGestureDemo world;
	public GameObject agent;
	public Image focusCircle;
	public ObjectSelector objSelector;
	public RelationTracker relationTracker;
	public GameObject focusObj;
	public string fullDesc;
	public List<string> descriptors = new List<string>();
	public bool distanceDistinction;
	public bool relativeDistance;

	public event EventHandler PlaceObjects;

	public void OnPlaceObjects(object sender, EventArgs e) {
		if (PlaceObjects != null) {
			PlaceObjects(this, e);
		}
	}

	public event EventHandler ItemsSituated;

	public void OnItemsSituated(object sender, EventArgs e) {
		if (ItemsSituated != null) {
			ItemsSituated(this, e);
		}
	}

	public event EventHandler ObjectSelected;

	public void OnObjectSelected(object sender, EventArgs e) {
		if (ObjectSelected != null) {
			ObjectSelected(this, e);
		}
	}

	// Use this for initialization
	void Start() {
		focusCircle.enabled = false;
		spriteAnimator = focusCircle.GetComponent<Animator>();
		spriteAnimator.enabled = false;

		focusTimeoutTimer = new Timer();
		focusTimeoutTimer.Interval = focusTimeoutTime;
		focusTimeoutTimer.Enabled = false;
		focusTimeoutTimer.Elapsed += TimeoutFocus;
		timeoutFocus = false;

		referWaitTimer = new Timer();
		referWaitTimer.Interval = referWaitTime;
		referWaitTimer.Enabled = false;
		referWaitTimer.Elapsed += ReferToFocusedObject;
		timeoutFocus = false;

		behaviorController = GameObject.Find("BehaviorController");
		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		preds = behaviorController.GetComponent<Predicates>();
		eventManager = behaviorController.GetComponent<EventManager>();

		relationTracker = behaviorController.GetComponent<RelationTracker>();

		eventManager.EntityReferenced += ReferenceObject;

		PlaceObjects += PlaceObjectsRandomly;
		ObjectSelected += IndicateFocus;
	}

	// Update is called once per frame
	void Update() {
		if (!itemsInited) {
			for (int i = 0; i < landmarks.Count; i++) {
				landmarks[i] = landmarks[i] != Helper.GetMostImmediateParentVoxeme(landmarks[i])
					? Helper.GetMostImmediateParentVoxeme(landmarks[i])
					: landmarks[i];
			}

			for (int i = 0; i < world.availableObjs.Count; i++) {
				world.availableObjs[i] =
					world.availableObjs[i] != Helper.GetMostImmediateParentVoxeme(world.availableObjs[i])
						? Helper.GetMostImmediateParentVoxeme(world.availableObjs[i])
						: world.availableObjs[i];
			}

			itemsInited = true;
			resituateItems = true;
		}

		if (resituateItems) {
			PlaceRandomly(
				world.demoSurface != Helper.GetMostImmediateParentVoxeme(world.demoSurface)
					? Helper.GetMostImmediateParentVoxeme(world.demoSurface)
					: world.demoSurface,
				landmarks, world.availableObjs);
			behaviorController.GetComponent<RelationTracker>().SurveyRelations();
			resituateItems = false;
			OnItemsSituated(this, null);
		}

		if (Input.GetMouseButtonDown(0)) {
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			// Casts the ray and get the first game object hit
			Physics.Raycast(ray, out hit);

			if (hit.collider != null) {
				if (world.availableObjs.Contains(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject))) {
					OnObjectSelected(this,
						new SelectionEventArgs(Helper.GetMostImmediateParentVoxeme(hit.collider.gameObject)));
				}
			}
		}

		if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && (Input.GetKeyDown(KeyCode.R))) {
			PlaceRandomly(
				world.demoSurface != Helper.GetMostImmediateParentVoxeme(world.demoSurface)
					? Helper.GetMostImmediateParentVoxeme(world.demoSurface)
					: world.demoSurface,
				landmarks, world.availableObjs);
			behaviorController.GetComponent<RelationTracker>().SurveyRelations();
			OnItemsSituated(this, null);
		}

		if (timeoutFocus) {
			timeoutFocus = false;
			focusCircle.enabled = false;
			spriteAnimator.enabled = false;
			referWaitTimer.Enabled = true;
		}

		if (refer) {
			refer = false;
			eventManager.OnEntityReferenced(this, new EventReferentArgs(focusObj));
		}
	}

	void PlaceRandomly(GameObject surface, List<GameObject> landmarkObjs, List<GameObject> focusObjs) {
		// place landmarks
		foreach (GameObject landmark in landmarkObjs) {
			Vector3 coord = Helper.FindClearRegion(surface, landmark).center;
			landmark.transform.position = new Vector3(coord.x,
				coord.y + Helper.GetObjectWorldSize(landmark).extents.y, coord.z);
			landmark.GetComponent<Voxeme>().targetPosition = landmark.transform.position;
		}

		// place focus objects
		foreach (GameObject obj in focusObjs) {
			Vector3 coord = Helper.FindClearRegion(surface, obj).center;
			obj.transform.position = new Vector3(coord.x,
				coord.y + Helper.GetObjectWorldSize(obj).extents.y, coord.z);
			obj.GetComponent<Voxeme>().targetPosition = obj.transform.position;
		}
	}

	void PlaceObjectsRandomly(object sender, EventArgs e) {
		resituateItems = true;
		Debug.Log("Reshuffle objects");
	}

	void IndicateFocus(object sender, EventArgs e) {
		world.RespondAndUpdate(string.Empty); // clear dialogue box

		focusObj = ((SelectionEventArgs) e).Content as GameObject;
		Debug.Log(string.Format("Focused on {0}, world @ {1} screen @ {2}", focusObj.name,
			Helper.VectorToParsable(focusObj.transform.position),
			Helper.VectorToParsable(Camera.main.WorldToScreenPoint(focusObj.transform.position))));

		focusCircle.enabled = true;
		focusCircle.transform.position = new Vector3(focusObj.transform.position.x,
			Helper.GetObjectWorldSize(focusObj).max.y,
			focusObj.transform.position.z);
		//Debug.Log(Helper.VectorToParsable(focusCircle.transform.position));
		focusTimeoutTimer.Interval = focusTimeoutTime;
		focusTimeoutTimer.Enabled = true;
		spriteAnimator.enabled = true;
		spriteAnimator.Play("circle_anim_test", 0, 0);
	}

	void ReferenceObject(object sender, EventArgs e) {
		Debug.Log(string.Format("Referring to {0}", focusObj.name));

		//List<string> descriptors = new List<string>();
		descriptorString = string.Empty;

		if (world.interactionPrefs.gesturalReference) {
			distanceDistinction = false;
			GameObject hand = InteractionHelper.GetCloserHand(agent, focusObj);
			world.PointAt(focusObj.transform.position, hand);

			if (world.interactionPrefs.linguisticReference) {
				// G + L
				// variables: bool proximal/distal distinction (this/that)
				//  int 0-3 relational descriptors
				distanceDistinction = Convert.ToBoolean(
					RandomHelper.RandomInt(0, 1,
						(int) (RandomHelper.RangeFlags.MinInclusive) | (int) (RandomHelper.RangeFlags.MaxInclusive)));
				relativeDistance = Convert.ToBoolean(
					RandomHelper.RandomInt(0, 1,
						(int) (RandomHelper.RangeFlags.MinInclusive) | (int) (RandomHelper.RangeFlags.MaxInclusive)));
				int relationalDescriptors = RandomHelper.RandomInt(0, 4);
				Debug.Log(string.Format(
					"Use distance distinction: {0}, relative distance: {1}, {2} relational descriptors",
					distanceDistinction, relativeDistance, relationalDescriptors));

				Voxeme objVox = focusObj.GetComponent<Voxeme>();

				string demonstrative = "That";
				if (distanceDistinction) {
					if (relativeDistance) {
						// !relative distance -> absolute distance (near region = this, far region = that)
						// is focusObj closer to the agent than the other block of the same color?
						string color = string.Empty;
						color = objVox.voxml.Attributes.Attrs[0].Value; // just grab the first one for now
						List<GameObject> otherObjs = world.availableObjs.Where(b =>
							b.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value == color &&
							b != focusObj).ToList();
						if (otherObjs.Count > 0) {
							if (Vector3.Distance(agent.transform.position, focusObj.transform.position) <
							    Vector3.Distance(agent.transform.position, otherObjs[0].transform.position)) {
								demonstrative = "This";
							}
						}
						else {
							// default to absolute distance
							if (world.frontRegion.Contains(new Vector3(
								focusObj.transform.position.x,
								world.frontRegion.center.y,
								focusObj.transform.position.z))) {
								demonstrative = "This";
							}
						}
					}
					else {
						if (world.frontRegion.Contains(new Vector3(
							focusObj.transform.position.x,
							world.frontRegion.center.y,
							focusObj.transform.position.z))) {
							demonstrative = "This";
						}
					}
				}

				descriptorString = FindFocusObjRelations(relationalDescriptors);

				if (objVox != null) {
					string color = string.Empty;
					color = objVox.voxml.Attributes.Attrs[0].Value; // just grab the first one for now
					fullDesc = string.Format("{0} {1} {2}{3}.", demonstrative, color, objVox.opVox.Lex.Pred,
						descriptorString);
					world.RespondAndUpdate(fullDesc);
				}
			}
		}
		else if (world.interactionPrefs.linguisticReference) {
			// variables: int 1-3 relational descriptors
			distanceDistinction = false;
			int relationalDescriptors = RandomHelper.RandomInt(1, 4);
			descriptorString = FindFocusObjRelations(relationalDescriptors);

			Voxeme objVox = focusObj.GetComponent<Voxeme>();

			if (objVox != null) {
				string color = string.Empty;
				color = objVox.voxml.Attributes.Attrs[0].Value; // just grab the first one for now
				fullDesc = string.Format("The {0} {1}{2}.", color, objVox.opVox.Lex.Pred, descriptorString);
				world.RespondAndUpdate(fullDesc);
			}
		}
	}

	string FindFocusObjRelations(int relationalDescriptors) {
		descriptors = new List<string>();
		descriptorString = string.Empty;

		// find relations involving focusObj
		List<Pair<GameObject, string>> focusObjRelations = new List<Pair<GameObject, string>>();
		foreach (DictionaryEntry dictEntry in relationTracker.relations) {
			if (((dictEntry.Key as List<GameObject>)[0] == focusObj) &&
			    (landmarks.Contains((dictEntry.Key as List<GameObject>)[1]))) {
				focusObjRelations.Add(new Pair<GameObject, string>(
					(dictEntry.Key as List<GameObject>).Where(o => o != focusObj).ToList()[0],
					dictEntry.Value as string));
			}
		}

		foreach (DictionaryEntry dictEntry in relationTracker.relations) {
			if (((dictEntry.Key as List<GameObject>)[0] == focusObj) &&
			    (!landmarks.Contains((dictEntry.Key as List<GameObject>)[1])) &&
			    ((dictEntry.Key as List<GameObject>)[1] != Helper.GetMostImmediateParentVoxeme(world.demoSurface))) {
				focusObjRelations.Add(new Pair<GameObject, string>(
					(dictEntry.Key as List<GameObject>).Where(o => o != focusObj).ToList()[0],
					dictEntry.Value as string));
			}
		}

		// shuffle relation list
		int count = focusObjRelations.Count;
		int last = count - 1;
		for (int i = 0; i < last; ++i) {
			int r = RandomHelper.RandomInt(i, count);
			var tmp = focusObjRelations[i];
			focusObjRelations[i] = focusObjRelations[r];
			focusObjRelations[r] = tmp;
		}

		foreach (Pair<GameObject, string> relation in focusObjRelations) {
			Debug.Log(string.Format("{0} {1} {2}", focusObj.name, relation.Item2, relation.Item1.name));
		}

		for (int i = 0; i < Math.Min(relationalDescriptors, focusObjRelations.Count); i++) {
			string color = string.Empty;
			Voxeme descriptorObjVox = (focusObjRelations[i].Item1 as GameObject).GetComponent<Voxeme>();
			color = (descriptorObjVox.voxml.Attributes.Attrs.Count > 0)
				? descriptorObjVox.voxml.Attributes.Attrs[0].Value
				: string.Empty; // just grab the first one for now
			color = (color == focusObj.GetComponent<Voxeme>().voxml.Attributes.Attrs[0].Value)
				? "other " + color
				: color;
			string descriptorObj = string.Format("{0}{1} {2}", "the",
				(color == string.Empty) ? string.Empty : " " + color,
				descriptorObjVox.opVox.Lex.Pred);

			descriptors.Add(string.Format("{0} {1}", predToString[focusObjRelations[i].Item2], descriptorObj));

			if (descriptors.Count > 1) {
				if (descriptors.Count > 2) {
					descriptorString = string.Format("{0} and {1}",
						string.Join(", ", descriptors.GetRange(0, descriptors.Count - 1).ToArray()),
						descriptors[descriptors.Count - 1]);
				}
				else {
					descriptorString = string.Join(" and ", descriptors.ToArray());
				}
			}
			else {
				descriptorString = descriptors[0];
			}
		}

		return ((descriptors.Count > 0) ? " " : "") + descriptorString;
	}

	void TimeoutFocus(object sender, ElapsedEventArgs e) {
		focusTimeoutTimer.Enabled = false;
		timeoutFocus = true;
	}

	void ReferToFocusedObject(object sender, ElapsedEventArgs e) {
		referWaitTimer.Interval = referWaitTime;
		referWaitTimer.Enabled = false;
		refer = true;
	}
}