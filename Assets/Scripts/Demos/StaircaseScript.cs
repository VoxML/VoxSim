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

public class StaircaseScript : DemoScript {
	enum ScriptStep {
		Step0,
		Step1A,
		Step1B,
		Step1C,
		Step1D,
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
		Step6
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

	String demoName = "staircase";

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
		eventManager.EventComplete += HumanMoveComplete;
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
					OutputHelper.PrintOutput(Role.Planner, "Let's build a staircase!");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Let's build a staircase!\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step1A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block4"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block4")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "Take that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Take that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step1B) {
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block3"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block3")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step1C) {
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

		if (currentStep == ScriptStep.Step1D) {
			if ((int) (wilsonState & WilsonState.PushTogether) == 0) {
				wilsonState |= WilsonState.PushTogether;
				PushTogether();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And put them together");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And put them together\""));
				}
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find("block4"),
						GameObject.Find("block3")
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("left") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					List<object> diff =
						Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
					OnLogEvent(this, new LogEventArgs("Result: " + string.Join(";", diff.Cast<string>().ToArray())));
					if (satisfied) {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Agreement"));
						if ((int) (wilsonState & (WilsonState.ThumbsUp | WilsonState.HeadNod)) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= (WilsonState.ThumbsUp | WilsonState.HeadNod);
							ThumbsUp();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							HeadNod();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "Great!");
								OnLogEvent(this, new LogEventArgs("Wilson: S = \"Great!\""));
							}
						}
					}
					else {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Disgreement"));
						if ((int) (wilsonState & WilsonState.HeadShake) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= WilsonState.HeadShake;
							HeadShake();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "That's not quite what I had in mind.");
								OnLogEvent(this,
									new LogEventArgs("Wilson: S = \"That's not quite what I had in mind.\""));
								goBack = true;
							}
						}
					}

					moveLogged = true;
				}
			}
		}

		if (currentStep == ScriptStep.Step2A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block6"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block6")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "Take that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Take that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step2B) {
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block3"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block3")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And that block\""));
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
			if ((int) (wilsonState & WilsonState.PushTogether) == 0) {
				wilsonState |= WilsonState.PushTogether;
				PushTogether();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And put them together");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And put them together\""));
				}
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find("block6"),
						GameObject.Find("block3")
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("right") && relations.Contains("touching")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					List<object> diff =
						Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
					OnLogEvent(this, new LogEventArgs("Result: " + string.Join(";", diff.Cast<string>().ToArray())));
					if (satisfied) {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Agreement"));
						if ((int) (wilsonState & (WilsonState.ThumbsUp | WilsonState.HeadNod)) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= (WilsonState.ThumbsUp | WilsonState.HeadNod);
							ThumbsUp();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							HeadNod();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "Great!");
								OnLogEvent(this, new LogEventArgs("Wilson: S = \"Great!\""));
							}
						}
					}
					else {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Disagreement"));
						if ((int) (wilsonState & WilsonState.HeadShake) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= WilsonState.HeadShake;
							HeadShake();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "That's not quite what I had in mind.");
								OnLogEvent(this,
									new LogEventArgs("Wilson: S = \"That's not quite what I had in mind.\""));
								goBack = true;
							}
						}
					}

					moveLogged = true;
				}
			}
		}

		if (currentStep == ScriptStep.Step3A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block5"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block5")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "Take that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Take that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step3B) {
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block6"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block6")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And put it on that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And put it on that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step3C) {
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find("block5").transform.position, GameObject.Find("block6").transform.position);
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block6")));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find("block6"),
						GameObject.Find("block5")
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("support")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					List<object> diff =
						Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
					OnLogEvent(this, new LogEventArgs("Result: " + string.Join(";", diff.Cast<string>().ToArray())));
					if (satisfied) {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Agreement"));
						if ((int) (wilsonState & (WilsonState.ThumbsUp | WilsonState.HeadNod)) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= (WilsonState.ThumbsUp | WilsonState.HeadNod);
							ThumbsUp();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							HeadNod();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "Great!");
								OnLogEvent(this, new LogEventArgs("Wilson: S = \"Great!\""));
							}
						}
					}
					else {
						OnLogEvent(this, new LogEventArgs("Wilson: Resp = Disagreement"));
						if ((int) (wilsonState & WilsonState.HeadShake) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= WilsonState.HeadShake;
							HeadShake();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "That's not quite what I had in mind.");
								OnLogEvent(this,
									new LogEventArgs("Wilson: S = \"That's not quite what I had in mind.\""));
								goBack = true;
							}
						}
					}

					moveLogged = true;
				}
			}
		}

		if (currentStep == ScriptStep.Step4A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block2"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block2")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "Take that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Take that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step4B) {
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block3"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block3")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And put it on that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And put it on that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step4C) {
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find("block2").transform.position, GameObject.Find("block3").transform.position);
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block3")));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find("block3"),
						GameObject.Find("block2")
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("support")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					List<object> diff =
						Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
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
								OutputHelper.PrintOutput(Role.Planner, "Great!");
								OnLogEvent(this, new LogEventArgs("Wilson: S = \"Great!\""));
							}
						}
					}
					else {
						Log("Response: Disagreement");
						if ((int) (wilsonState & WilsonState.HeadShake) == 0) {
							waitTimer.Enabled = true;
							wilsonState |= WilsonState.HeadShake;
							HeadShake();
							OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
							if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
								OutputHelper.PrintOutput(Role.Planner, "That's not quite what I had in mind.");
								OnLogEvent(this,
									new LogEventArgs("Wilson: S = \"That's not quite what I had in mind.\""));
								goBack = true;
							}
						}
					}

					moveLogged = true;
				}
			}
		}

		if (currentStep == ScriptStep.Step5A) {
			currentState = relationTracker.relStrings.Cast<object>().ToList();
			goBack = false;
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block1"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block1")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "Take that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"Take that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step5B) {
			if ((int) (wilsonState & WilsonState.Point) == 0) {
				waitTimer.Enabled = true;
				wilsonState |= WilsonState.Point;
				PointAt(GameObject.Find("block5"));
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block5")));
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "And put it on that block");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"And put it on that block\""));
				}
			}
		}

		if (currentStep == ScriptStep.Step5C) {
			if ((int) (wilsonState & WilsonState.Claw) == 0) {
				wilsonState |= (WilsonState.Claw | WilsonState.LookForward);
				Claw(GameObject.Find("block1").transform.position, GameObject.Find("block5").transform.position);
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture, "block5")));
				LookForward();
				OnLogEvent(this, new LogEventArgs("Wilson: G = " + string.Format(mostRecentGesture)));
			}
			else {
				bool satisfied = false;
				foreach (List<GameObject> key in relationTracker.relations.Keys) {
					if (key.SequenceEqual(new List<GameObject>(new GameObject[] {
						GameObject.Find("block5"),
						GameObject.Find("block1")
					}))) {
						string[] relations = relationTracker.relations[key].ToString().Split(',');
						if (relations.Contains("support")) {
							satisfied = true;
							break;
						}
					}
				}

				if (humanMoveComplete) {
					List<object> diff =
						Helper.DiffLists(currentState, relationTracker.relStrings.Cast<object>().ToList());
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
								OutputHelper.PrintOutput(Role.Planner, "Great!");
								OnLogEvent(this, new LogEventArgs("Wilson: S = \"Great!\""));
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
								OutputHelper.PrintOutput(Role.Planner, "That's not quite what I had in mind.");
								OnLogEvent(this,
									new LogEventArgs("Wilson: S = \"That's not quite what I had in mind.\""));
								goBack = true;
							}
						}
					}

					moveLogged = true;
				}
			}
		}

		if (currentStep == ScriptStep.Step6) {
			if ((int) (wilsonState & WilsonState.Rest) == 0) {
				wilsonState |= WilsonState.Rest;
				Rest();
				if ((int) (outputModality.modality & OutputModality.Modality.Linguistic) == 1) {
					OutputHelper.PrintOutput(Role.Planner, "OK, we're done!");
					OnLogEvent(this, new LogEventArgs("Wilson: S = \"OK, we're done!\""));
				}

				CloseLog();
			}
		}

		/*if (Input.GetKeyDown (KeyCode.Space)) {
			wilsonState = 0;
			currentStep = (DemoStep)((int)currentStep + 1);
		}*/
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
			case ScriptStep.Step1D:
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
				currentStep = ScriptStep.Step5A;
				break;

			default:
				break;
		}
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
			else if (currentStep < ScriptStep.Step6) {
				currentStep = ScriptStep.Step5A;
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