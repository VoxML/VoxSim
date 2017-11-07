using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarGestureController : MonoBehaviour {

    [Tooltip("Optional. If an avatar is not specified, the component will assume it is attached to the avatar.")]
    public GameObject avatar;

    private Animator animator;
    private AvatarGestureStateBehavior agsBehavior;

    private const string ANIM_RARM_GESTUREID = "RArm_GestureId";
    private const string ANIM_RARM_TRIGGER = "RArm_Trigger";

    private const string ANIM_LARM_GESTUREID = "LArm_GestureId";
    private const string ANIM_LARM_TRIGGER = "LArm_Trigger";

    //
    //  Properties
    //

    public AvatarGesture CurrentGesture { get; private set; }

    public string CurrentGestureName { get { return CurrentGesture == null ? "" : CurrentGesture.Name; } }

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
    //  Initialization
    //
    
    void Start () {
		if (!avatar)
        {
            avatar = gameObject;
        }

        animator = avatar.GetComponent<Animator>();
        agsBehavior = animator.GetBehaviour<AvatarGestureStateBehavior>(); // TODO: Multiple layers may have different state behaviors!

        // Attach event handlers to state machine behavior (allows us to get animator events)
        agsBehavior.AnimationStart += StateMachineBehavior_AnimationStart;
        agsBehavior.AnimationEnd += StateMachineBehavior_AnimationEnd;
	}

    void Update () {
		
	}

    //
    //  Event Handlers
    //

    private void StateMachineBehavior_AnimationStart()
    {
        IsGesturing = true;
        if (GestureStart != null)
        {
            GestureStart(this, CurrentGesture);
        }
    }
    
    private void StateMachineBehavior_AnimationEnd()
    {
        IsGesturing = false;
        if (GestureEnd != null)
        {
            GestureEnd(this, CurrentGesture);
        }
    }

    //
    //  Methods
    //

    /// <summary>
    /// Editor-compatible API. Makes the avatar perform the specified gesture.
    /// </summary>
    /// <param name="gesture">Name of the gesture to perform</param>
    public void PerformGesture(string gesture)
    {
        PerformGesture(gesture, null);
    }

    /// <summary>
    /// Makes the avatar perform the specified gesture.
    /// </summary>
    /// <param name="gesture">Name of the gesture to perform</param>
    /// <param name="callback">Function to call when the gesture completes</param>
    public void PerformGesture(string gesture, Action<AvatarGesture> callback = null)
    {
        // Lookup gesture by name
        if (!AvatarGesture.ALL_GESTURES.ContainsKey(gesture.ToLower()))
        {
            Debug.LogError("Gesture \"" + gesture + "\" not found!");
            return;
        }

        AvatarGesture ag = AvatarGesture.ALL_GESTURES[gesture.ToLower()];
        PerformGesture(ag, callback);
    }

    /// <summary>
    /// Makes the avatar perform the specified gesture.
    /// </summary>
    /// <param name="gesture">A predefined AvatarGesture.</param>
    /// <param name="callback">Function to call when the gesture completes</param>
    public void PerformGesture(AvatarGesture gesture, Action<AvatarGesture> callback = null)
    {
        if (gesture == null)
        {
            Debug.LogError("No gesture specified!");
            return;
        }

        //////////////////////////////////////////////
        // Get trigger names based on handedness
        string anim_gestureid_name;
        string anim_trigger_name;

        if (gesture.BodyPart == AvatarGesture.Body.LeftArm)
        {
            anim_gestureid_name = ANIM_LARM_GESTUREID;
            anim_trigger_name = ANIM_LARM_TRIGGER;
        }
        else //if (gesture.BodyPart == AvatarGesture.Body.RightArm) // TODO: Right now, just left or right
        {
            anim_gestureid_name = ANIM_RARM_GESTUREID;
            anim_trigger_name = ANIM_RARM_TRIGGER;
        }

        //////////////////////////////////////////////
        // Trigger the gesture
        animator.SetInteger(anim_gestureid_name, gesture.Id);
        animator.SetTrigger(anim_trigger_name);

        CurrentGesture = gesture;

        //////////////////////////////////////////////
        // Callback upon completion
        if (callback != null)
        {
            // Create a one-shot event handler that calls the callback function
            AvatarGestureStateBehavior.StateMachineEventHandler eventHandler = null;
            eventHandler = delegate ()
            {
                agsBehavior.AnimationEnd -= eventHandler; // Unsubscribe; one-shot
                callback(gesture);
            };
            agsBehavior.AnimationEnd += eventHandler; // Subscribe
        }
    }
}
