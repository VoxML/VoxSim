using System;
using UnityEngine;
using UnityEngine.Events;

public class AvatarGestureController : MonoBehaviour {
	[Tooltip("Optional. If an avatar is not specified, the component will assume it is attached to the avatar.")]
	public GameObject avatar;

	private Animator animator;
	private AvatarGestureStateBehavior rarmStateBehavior;
	private AvatarGestureStateBehavior larmStateBehavior;
	private AvatarGestureStateBehavior headStateBehavior;

	private AvatarGestureStateBehavior
		currentStateBehavior; // Intended behavior to receive events from, based on gesture body parts

	private const string ANIM_RARM_LAYERNAME = "RArm";
	private const string ANIM_RARM_GESTUREID = "RArm_GestureId";
	private const string ANIM_RARM_TRIGGER = "RArm_Trigger";

	private const string ANIM_LARM_LAYERNAME = "LArm";
	private const string ANIM_LARM_GESTUREID = "LArm_GestureId";
	private const string ANIM_LARM_TRIGGER = "LArm_Trigger";

	private const string ANIM_HEAD_LAYERNAME = "Head";
	private const string ANIM_HEAD_GESTUREID = "Head_GestureId";
	private const string ANIM_HEAD_TRIGGER = "Head_Trigger";

	// TODO Maybe make structs for these

	//
	//  Properties
	//

	public AvatarGesture CurrentGesture { get; private set; }

	public string CurrentGestureName {
		get { return CurrentGesture == null ? "" : CurrentGesture.Name; }
	}

	public bool IsGesturing { get; private set; }

	//
	//  Events
	//

	/// <summary>
	/// Event Handler for AvatarGestureController events.
	/// </summary>
	/// <param name="sender">Reference to the AvatarGestureController</param>
	/// <param name="gesture">The current gesture</param>
	public delegate void AvatarGestureEventHandler(object sender, AvatarGesture gesture);

	/// <summary>
	/// GestureStart is called when PerformGesture is called and the animation begins playing.
	/// </summary>
	public event AvatarGestureEventHandler GestureStart;

	/// <summary>
	/// GestureEnd is called after an animation is completed.
	/// </summary>
	public event AvatarGestureEventHandler GestureEnd;

	//
	//  Unity Events
	//

	public UnityEvent OnGestureStart;
	public UnityEvent OnGesturePause;
	public UnityEvent OnGestureResume;
	public UnityEvent OnGestureEnd;

	//
	//  Initialization
	//

	void Awake() {
		if (!avatar) {
			avatar = gameObject;
		}

		animator = avatar.GetComponent<Animator>();

		// Get animator state machine behaviors (allows us to get animator events)
		AvatarGestureStateBehavior[] behaviors = animator.GetBehaviours<AvatarGestureStateBehavior>();
		foreach (var behavior in behaviors) {
			if (behavior.layerName == ANIM_RARM_LAYERNAME) {
				rarmStateBehavior = behavior;
			}
			else if (behavior.layerName == ANIM_LARM_LAYERNAME) {
				larmStateBehavior = behavior;
			}
			else if (behavior.layerName == ANIM_HEAD_LAYERNAME) {
				headStateBehavior = behavior;
			}
		}

		Debug.Assert(rarmStateBehavior != null && larmStateBehavior != null);

		// Attach event handlers to state machine behavior
		rarmStateBehavior.AnimationStart += StateMachineBehavior_AnimationStart;
		rarmStateBehavior.AnimationEnd += StateMachineBehavior_AnimationEnd;

		larmStateBehavior.AnimationStart += StateMachineBehavior_AnimationStart;
		larmStateBehavior.AnimationEnd += StateMachineBehavior_AnimationEnd;

		headStateBehavior.AnimationStart += StateMachineBehavior_AnimationStart;
		headStateBehavior.AnimationEnd += StateMachineBehavior_AnimationEnd;

		// Default to RArm
		currentStateBehavior =
			rarmStateBehavior; // TODO -- could elect to dynamically subscribe/unsubscribe to behaviors as needed instead of checking them each time
	}

	void Update() {
	}

	//
	//  Event Handlers
	//

	private void StateMachineBehavior_AnimationStart(object sender) {
		// Verify that the expected behavior is sending the event (only want events from one behavior)
		if (sender != currentStateBehavior) {
			return;
		}

		// Set flag and fire event
		IsGesturing = true;
		if (GestureStart != null) {
			GestureStart(this, CurrentGesture);
		}

		// Fire Unity events
		OnGestureStart.Invoke();
	}

	private void StateMachineBehavior_AnimationEnd(object sender) {
		// Verify that the expected behavior is sending the event (only want events from one behavior)
		if (sender != currentStateBehavior) {
			return;
		}

		// Clear flag and fire event
		IsGesturing = false;
		if (GestureEnd != null) {
			GestureEnd(this, CurrentGesture);
		}

		// Fire Unity events
		OnGestureEnd.Invoke();
	}

	//
	//  Methods
	//

	/// <summary>
	/// Editor-compatible API. Makes the avatar perform the specified gesture.
	/// </summary>
	/// <param name="gesture">Name of the gesture to perform</param>
	public void PerformGesture(string gesture) {
		PerformGesture(gesture, null);
	}

	/// <summary>
	/// Makes the avatar perform the specified gesture.
	/// </summary>
	/// <param name="gesture">Name of the gesture to perform</param>
	/// <param name="callback">Function to call when the gesture completes</param>
	public void PerformGesture(string gesture, Action<AvatarGesture> callback = null) {
		// Lookup gesture by name
		if (!AvatarGesture.AllGestures.ContainsKey(gesture.ToLower())) {
			Debug.LogError("Gesture \"" + gesture + "\" not found!");
			return;
		}

		AvatarGesture ag = AvatarGesture.AllGestures[gesture.ToLower()];
		PerformGesture(ag, callback);
	}

	/// <summary>
	/// Makes the avatar perform the specified gesture.
	/// </summary>
	/// <param name="gesture">A predefined AvatarGesture.</param>
	/// <param name="callback">Function to call when the gesture completes</param>
	public void PerformGesture(AvatarGesture gesture, Action<AvatarGesture> callback = null) {
		if (gesture == null) {
			Debug.LogError("No gesture specified!");
			return;
		}

		//////////////////////////////////////////////
		// Get trigger names based on handedness

		string anim_gestureid_name;
		string anim_trigger_name;

		if (gesture.BodyPart == AvatarGesture.Body.LeftArm) {
			anim_gestureid_name = ANIM_LARM_GESTUREID;
			anim_trigger_name = ANIM_LARM_TRIGGER;

			currentStateBehavior = larmStateBehavior;
		}
		else if (gesture.BodyPart == AvatarGesture.Body.RightArm) {
			anim_gestureid_name = ANIM_RARM_GESTUREID;
			anim_trigger_name = ANIM_RARM_TRIGGER;

			currentStateBehavior = rarmStateBehavior;
		}
		else {
			anim_gestureid_name = ANIM_HEAD_GESTUREID;
			anim_trigger_name = ANIM_HEAD_TRIGGER;

			currentStateBehavior = headStateBehavior;
		}

		//////////////////////////////////////////////
		// Trigger the gesture

		animator.SetInteger(anim_gestureid_name, gesture.Id);
		animator.SetTrigger(anim_trigger_name);

		CurrentGesture = gesture;

		//////////////////////////////////////////////
		// Callback upon completion

		if (callback != null) {
			// Cache currentStateBehavior
			AvatarGestureStateBehavior behavior = currentStateBehavior;

			// Create a one-shot event handler that calls the callback function
			AvatarGestureStateBehavior.StateMachineEventHandler eventHandler = null;
			eventHandler = delegate(object sender) {
				behavior.AnimationEnd -= eventHandler; // Unsubscribe; one-shot
				callback(gesture);
			};
			behavior.AnimationEnd += eventHandler; // Subscribe
		}
	}

	public void StorePose() {
		OnGestureStart.Invoke();
	}

	public void PauseGesture() {
		animator.speed = 0f;
		OnGesturePause.Invoke();
	}

	public void ResumeGesture() {
		animator.speed = 1f;
	}
}