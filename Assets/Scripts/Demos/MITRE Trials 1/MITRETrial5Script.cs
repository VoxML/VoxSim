using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using VoxSimPlatform;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.Vox;

public class MITRETrial5Script : DemoScript {
	enum ScriptStep {
		Step0,

		Step1A,
		Step1B,
		Step1C,

		Step2A,
		Step2B,
		Step2C,
		Step2D,

		Step3A,
		Step3B,
		Step3C,

		Step4A,
		Step4B,
		Step4C,

		Step5A,
		Step5B,
		Step5C,
		Step5D,

		Step6A,
		Step6B,
		Step6C,
		Step6D,

		Step7A,
		Step7B,
		Step7C,

		Step8A,
		Step8B,
		Step8C,

		Step9
	}

	enum WilsonState {
		Rest = 1,
		Point = (1 << 1),
		LookForward = (1 << 2),
		PushTogether = (1 << 3),
		Claw = (1 << 4),
		ThumbsUp = (1 << 5),
		HeadNod = (1 << 6),
		HeadShake = (1 << 7)
	}

	String demoName = "MITRE5";

	GameObject Wilson;
	GameObject Diana;
	Animator animator;

	ScriptStep currentStep;
	WilsonState wilsonState = 0;

	RelationTracker relationTracker;

	List<object> currentState;

	public double initialLeaderTime = 6000.0;
	Timer waitTimer;
	const double WAIT_TIME = 2000.0;

	bool humanMoveComplete;
	bool leftAtTarget, rightAtTarget;

	GameObject leftGrasper, rightGrasper;

	GraspScript graspController;

	IKControl ikControl;
	IKTarget leftTarget;
	IKTarget rightTarget;
	IKTarget headTarget;

	OutputModality outputModality;

	bool goBack;
	string mostRecentGesture = string.Empty;
	string lastReceivedInput = string.Empty;

	// Use this for initialization
	void Start() {
		base.Start();

		Wilson = GameObject.Find("Wilson");
		Diana = GameObject.Find("Diana");
		animator = Wilson.GetComponent<Animator>();
		relationTracker = GameObject.Find("BehaviorController").GetComponent<RelationTracker>();
		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();
		inputController = GameObject.Find("IOController").GetComponent<InputController>();

		leftGrasper = animator.GetBoneTransform(HumanBodyBones.LeftHand).transform.gameObject;
		rightGrasper = animator.GetBoneTransform(HumanBodyBones.RightHand).transform.gameObject;

		graspController = Wilson.GetComponent<GraspScript>();

		ikControl = Wilson.GetComponent<IKControl>();
		leftTarget = ikControl.leftHandObj.GetComponent<IKTarget>();
		rightTarget = ikControl.rightHandObj.GetComponent<IKTarget>();
		headTarget = ikControl.lookObj.GetComponent<IKTarget>();

		outputModality = GameObject.Find("OutputModality").GetComponent<OutputModality>();

		goBack = false;

		currentStep = ScriptStep.Step0;
		waitTimer = new Timer(WAIT_TIME);
		waitTimer.Enabled = false;
		waitTimer.Elapsed += Proceed;

		humanMoveComplete = false;
		leftAtTarget = false;
		rightAtTarget = false;
		inputController.InputReceived += HumanInputReceived;
		eventManager.QueueEmpty += HumanMoveComplete;
		eventManager.ForceClear += EventsForceCleared;
		leftTarget.AtTarget += LeftAtTarget;
		rightTarget.AtTarget += RightAtTarget;

		OpenLog(demoName, outputModality.modality);
	}

	void OnEnable() {
		// set default state
		foreach (string obj in defaultState.Keys) {
			GameObject.Find(obj).transform.position = defaultState[obj];
			GameObject.Find(obj).GetComponent<Voxeme>().targetPosition = defaultState[obj];
		}

		currentStep = ScriptStep.Step0;
	}

	// Update is called once per frame
	void Update() {
		base.Update();
		if (currentStep == ScriptStep.Step0) {
			if ((int) (wilsonState & WilsonState.Rest) == 0) {
				waitTimer.Interval = WAIT_TIME + initialLeaderTime;
				waitTimer.Enabled = true;
				wilsonState |= (WilsonState.Rest | WilsonState.LookForward);
				Rest();
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Please help me build something!");
				}
			}
		}

		if (currentStep == ScriptStep.Step1A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block4";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step1B) {
			string objName = "block5";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put it in front of that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step1C) {
			string obj1Name = "block4";
			string obj2Name = "block5";
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find(obj1Name).transform.position,
					GameObject.Find(obj2Name).transform.position + (Vector3.forward * 0.5f));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture,
					                                  string.Format("in_front({0}, Persp = Wilson)", obj2Name))));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("behind") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step2A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block3";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step2B) {
			string objName = "block4";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step2C) {
			if ((int) (wilsonState & WilsonState.LookForward) == 0) {
				wilsonState |= WilsonState.LookForward;
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}

			leftTarget.targetPosition = new Vector3(1.0f, 2.5f, 0.0f);
			rightTarget.targetPosition = new Vector3(-1.0f, 2.5f, 0.0f);

			if (leftAtTarget && rightAtTarget) {
				currentStep = (ScriptStep) ((int) currentStep + 1);
			}
		}

		if (currentStep == ScriptStep.Step2D) {
			string obj1Name = "block3";
			string obj2Name = "block4";
			if ((int) (wilsonState & WilsonState.PushTogether) == 0) {
				wilsonState |= WilsonState.PushTogether;
				PushTogether();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put them together");
				}
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("right") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step3A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block8";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step3B) {
			string objName = "block4";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put it in front of that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step3C) {
			string obj1Name = "block8";
			string obj2Name = "block4";
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find(obj1Name).transform.position,
					GameObject.Find(obj2Name).transform.position + (Vector3.forward * 0.5f));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture,
					                                  string.Format("in_front({0}, Persp = Wilson)", obj2Name))));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("behind") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step4A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block2";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step4B) {
			string objName = "block8";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put it in front of that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step4C) {
			string obj1Name = "block4";
			string obj2Name = "block5";
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find(obj1Name).transform.position,
					GameObject.Find(obj2Name).transform.position + (Vector3.forward * 0.5f));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture,
					                                  string.Format("in_front({0}, Persp = Wilson)", obj2Name))));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("behind") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step5A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block1";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step5B) {
			string objName = "block3";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step5C) {
			if ((int) (wilsonState & WilsonState.LookForward) == 0) {
				wilsonState |= WilsonState.LookForward;
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}

			leftTarget.targetPosition = new Vector3(1.0f, 2.5f, 0.0f);
			rightTarget.targetPosition = new Vector3(-1.0f, 2.5f, 0.0f);

			if (leftAtTarget && rightAtTarget) {
				currentStep = (ScriptStep) ((int) currentStep + 1);
			}
		}

		if (currentStep == ScriptStep.Step5D) {
			string obj1Name = "block1";
			string obj2Name = "block3";
			if ((int) (wilsonState & WilsonState.PushTogether) == 0) {
				wilsonState |= WilsonState.PushTogether;
				PushTogether();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put them together");
				}
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("right") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step6A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block7";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step6B) {
			string objName = "block2";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step6C) {
			if ((int) (wilsonState & WilsonState.LookForward) == 0) {
				wilsonState |= WilsonState.LookForward;
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}

			leftTarget.targetPosition = new Vector3(1.0f, 2.5f, 0.0f);
			rightTarget.targetPosition = new Vector3(-1.0f, 2.5f, 0.0f);

			if (leftAtTarget && rightAtTarget) {
				currentStep = (ScriptStep) ((int) currentStep + 1);
			}
		}

		if (currentStep == ScriptStep.Step6D) {
			string obj1Name = "block7";
			string obj2Name = "block2";
			if ((int) (wilsonState & WilsonState.PushTogether) == 0) {
				wilsonState |= WilsonState.PushTogether;
				PushTogether();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put them together");
				}
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("right") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step7A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block6";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step7B) {
			string objName = "block1";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put it in front of that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step7C) {
			string obj1Name = "block6";
			string obj2Name = "block1";
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find(obj1Name).transform.position,
					GameObject.Find(obj2Name).transform.position + (Vector3.forward * 0.5f));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture,
					                                  string.Format("in_front({0}, Persp = Wilson)", obj2Name))));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("behind") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step8A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			string objName = "block9";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Take that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step8B) {
			string objName = "block6";
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find(objName));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, objName)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("And put it in front of that block");
				}
			}
		}

		if (currentStep == ScriptStep.Step8C) {
			string obj1Name = "block9";
			string obj2Name = "block6";
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find(obj1Name).transform.position,
					GameObject.Find(obj2Name).transform.position + (Vector3.forward * 0.5f));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture,
					                                  string.Format("in_front({0}, Persp = Wilson)", obj2Name))));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find(obj1Name),
						GameObject.Find(obj2Name)
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("behind") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					CheckAgreement(satisfied);
				}
			}
		}

		if (currentStep == ScriptStep.Step9) {
			if ((int) (wilsonState & WilsonState.Rest) == 0) {
				wilsonState |= WilsonState.Rest;
				Rest();
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("OK, we're done!");
				}

				CloseLog();
			}
		}
	}

	void OnDestroy() {
		CloseLog();
	}

	void OnApplicationQuit() {
		CloseLog();
	}

	void Rest() {
		Debug.Log("Enter Rest");

		graspController.grasper = (int) Gestures.HandPose.Neutral;

		if (ikControl != null) {
			leftTarget.targetPosition = graspController.leftDefaultPosition;
			rightTarget.targetPosition = graspController.rightDefaultPosition;
		}
	}

	void PointAt(GameObject obj) {
		Debug.Log("Enter Point");
		mostRecentGesture = "POINT_AT({0})";
		GameObject grasper;

		Bounds bounds = Helper.GetObjectWorldSize(obj);

		// which hand is closer?
		float leftToGoalDist = (leftGrasper.transform.position - bounds.ClosestPoint(leftGrasper.transform.position))
			.magnitude;
		float rightToGoalDist = (rightGrasper.transform.position - bounds.ClosestPoint(rightGrasper.transform.position))
			.magnitude;

		if (leftToGoalDist < rightToGoalDist) {
			grasper = leftGrasper;
			graspController.grasper = (int) Gestures.HandPose.LeftPoint;
		}
		else {
			grasper = rightGrasper;
			graspController.grasper = (int) Gestures.HandPose.RightPoint;
		}

		IKControl ikControl = Wilson.GetComponent<IKControl>();
		if (ikControl != null) {
			Vector3 target = new Vector3(bounds.center.x, bounds.center.y - 0.2f, bounds.center.z + 0.3f);
			if (grasper == leftGrasper) {
				leftTarget.targetPosition = target;
				headTarget.targetPosition = target;
			}
			else {
				rightTarget.GetComponent<IKTarget>().targetPosition = target;
				headTarget.GetComponent<IKTarget>().targetPosition = target;
			}
		}
	}

	void LookForward() {
		Debug.Log("Enter LookForward");
		mostRecentGesture = "LOOK_FORWARD";

		if (ikControl != null) {
			headTarget.targetPosition = Diana.GetComponent<IKControl>().lookObj.transform.position;
		}
	}

	void PushTogether() {
		Debug.Log("Enter PushTogether");
		mostRecentGesture = "PALM_CONVERGE";

		leftAtTarget = false;
		rightAtTarget = false;

		graspController.grasper = (int) Gestures.HandPose.Neutral;

		if (ikControl != null) {
			headTarget.targetPosition = Diana.GetComponent<IKControl>().lookObj.transform.position;

			leftTarget.targetPosition = new Vector3(0.1f, 2.5f, 0.0f);
			rightTarget.targetPosition = new Vector3(-0.1f, 2.5f, 0.0f);
		}
	}

	void Claw(Vector3 fromCoord, Vector3 toCoord) {
		Debug.Log("Enter Claw");
		mostRecentGesture = "CLAW; JUMP_TO({0})";

		GameObject grasper;

		// which hand is closer?
		float leftToGoalDist = (leftGrasper.transform.position - fromCoord).magnitude;
		float rightToGoalDist = (rightGrasper.transform.position - fromCoord).magnitude;

		if (leftToGoalDist < rightToGoalDist) {
			grasper = leftGrasper;
			graspController.grasper = (int) Gestures.HandPose.LeftClaw;
		}
		else {
			grasper = rightGrasper;
			graspController.grasper = (int) Gestures.HandPose.RightClaw;
		}

		if (ikControl != null) {
			if (grasper == leftGrasper) {
				leftTarget.interTargetPositions.Enqueue(fromCoord);
				leftTarget.interTargetPositions.Enqueue(new Vector3(fromCoord.x, fromCoord.y + 0.5f, fromCoord.z));
				leftTarget.interTargetPositions.Enqueue(new Vector3(toCoord.x, toCoord.y + 0.5f, toCoord.z));
				leftTarget.targetPosition = toCoord;
			}
			else {
				rightTarget.interTargetPositions.Enqueue(fromCoord);
				rightTarget.interTargetPositions.Enqueue(new Vector3(fromCoord.x, fromCoord.y + 0.5f, fromCoord.z));
				rightTarget.interTargetPositions.Enqueue(new Vector3(toCoord.x, toCoord.y + 0.5f, toCoord.z));
				rightTarget.targetPosition = toCoord;
			}
		}
	}

	void ThumbsUp() {
		Debug.Log("Enter ThumbsUp");
		mostRecentGesture = "THUMBS_UP";

		graspController.grasper = (int) Gestures.HandPose.RightThumbsUp;

		if (ikControl != null) {
			leftTarget.targetPosition = graspController.leftDefaultPosition;
			rightTarget.targetPosition = new Vector3(0.0f, 3.0f, 0.0f);
		}
	}

	void HeadNod() {
		Debug.Log("Enter HeadNod");
		mostRecentGesture = "HEAD_NOD";

		if (ikControl != null) {
			Vector3 headStartPos = headTarget.targetPosition;

			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x, headStartPos.y + 0.2f, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x, headStartPos.y - 0.2f, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x, headStartPos.y + 0.2f, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x, headStartPos.y - 0.2f, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x, headStartPos.y + 0.2f, headStartPos.z));
			headTarget.targetPosition = headStartPos;
		}
	}

	void HeadShake() {
		Debug.Log("Enter HeadShake");
		mostRecentGesture = "HEAD_SHAKE";

		if (ikControl != null) {
			Vector3 headStartPos = headTarget.targetPosition;

			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x + 0.2f, headStartPos.y, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x - 0.2f, headStartPos.y, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x + 0.2f, headStartPos.y, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x - 0.2f, headStartPos.y, headStartPos.z));
			headTarget.interTargetPositions.Enqueue(new Vector3(headStartPos.x + 0.2f, headStartPos.y, headStartPos.z));
			headTarget.targetPosition = headStartPos;
		}
	}

	void Repeat() {
		switch (currentStep) {
			case ScriptStep.Step1A:
			case ScriptStep.Step1B:
			case ScriptStep.Step1C:
				currentStep = ScriptStep.Step1A;
				break;

			case ScriptStep.Step2A:
			case ScriptStep.Step2B:
			case ScriptStep.Step2C:
			case ScriptStep.Step2D:
				currentStep = ScriptStep.Step2A;
				break;

			case ScriptStep.Step3A:
			case ScriptStep.Step3B:
			case ScriptStep.Step3C:
				currentStep = ScriptStep.Step3A;
				break;

			case ScriptStep.Step4A:
			case ScriptStep.Step4B:
			case ScriptStep.Step4C:
				currentStep = ScriptStep.Step4A;
				break;

			case ScriptStep.Step5A:
			case ScriptStep.Step5B:
			case ScriptStep.Step5C:
			case ScriptStep.Step5D:
				currentStep = ScriptStep.Step5A;
				break;

			case ScriptStep.Step6A:
			case ScriptStep.Step6B:
			case ScriptStep.Step6C:
			case ScriptStep.Step6D:
				currentStep = ScriptStep.Step6A;
				break;

			case ScriptStep.Step7A:
			case ScriptStep.Step7B:
			case ScriptStep.Step7C:
				currentStep = ScriptStep.Step7A;
				break;

			case ScriptStep.Step8A:
			case ScriptStep.Step8B:
			case ScriptStep.Step8C:
				currentStep = ScriptStep.Step8A;
				break;

			default:
				break;
		}
	}

	void CheckAgreement(bool satisfied) {
		List<object> diff = Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
		OnLogEvent(this, new LogEventArgs("Result: " + string.Join(";", diff.Cast<string>().ToArray())));
		if (satisfied) {
			OnLogEvent(this, new LogEventArgs("Response: Agreement"));
			if ((int) (wilsonState & (WilsonState.ThumbsUp | WilsonState.HeadNod)) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= (WilsonState.ThumbsUp | WilsonState.HeadNod);
				ThumbsUp();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				HeadNod();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("Great!");
				}
			}
		}
		else {
			OnLogEvent(this, new LogEventArgs("Response: Disagreement"));
			if ((int) (wilsonState & WilsonState.HeadShake) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.HeadShake;
				HeadShake();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					PrintAndLogLinguisticOutput("That's not quite what I had in mind.");
					goBack = true;
				}
			}
		}

		moveLogged = true;
	}

	void PrintAndLogLinguisticOutput(string output) {
		OutputHelper.PrintOutput(Role.Planner, output);
		OnLogEvent(this, new LogEventArgs(MakeLogString("Wilson: S = ", FormatLogUtterance(output))));
	}

	void Proceed(object sender, ElapsedEventArgs e) {
		waitTimer.Enabled = false;
		waitTimer.Interval = WAIT_TIME;

		humanMoveComplete = false;
		moveLogged = false;
		leftAtTarget = false;
		rightAtTarget = false;

		wilsonState = 0;
		if (goBack) {
			// try again
			if (currentStep < ScriptStep.Step2A) {
				currentStep = ScriptStep.Step1A;
			}
			else if (currentStep < ScriptStep.Step3A) {
				currentStep = ScriptStep.Step2A;
			}
			else if (currentStep < ScriptStep.Step4A) {
				currentStep = ScriptStep.Step3A;
			}
			else if (currentStep < ScriptStep.Step5A) {
				currentStep = ScriptStep.Step4A;
			}
			else if (currentStep < ScriptStep.Step6A) {
				currentStep = ScriptStep.Step5A;
			}
			else if (currentStep < ScriptStep.Step7A) {
				currentStep = ScriptStep.Step6A;
			}
			else if (currentStep < ScriptStep.Step8A) {
				currentStep = ScriptStep.Step7A;
			}
			else if (currentStep < ScriptStep.Step9) {
				currentStep = ScriptStep.Step8A;
			}
		}
		else {
			currentStep = (ScriptStep) ((int) currentStep + 1);
		}
	}

	void HumanInputReceived(object sender, EventArgs e) {
		lastReceivedInput = ((InputEventArgs) e).InputString;
		OnLogEvent(this, new LogEventArgs("User: S = " + lastReceivedInput));
	}

	void HumanMoveComplete(object sender, EventArgs e) {
		humanMoveComplete = true;
	}

	void LeftAtTarget(object sender, EventArgs e) {
		leftAtTarget = true;
	}

	void RightAtTarget(object sender, EventArgs e) {
		rightAtTarget = true;
	}
}