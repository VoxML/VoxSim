using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarGestureController : MonoBehaviour {

    [Tooltip("Optional. If an avatar is not specified, the component will assume it is attached to the avatar.")]
    public GameObject avatar;

    private Animator animator;

    private const string ANIM_RARM_GESTUREID = "RArm_GestureId";
    private const string ANIM_RARM_TRIGGER = "RArm_Trigger";

    private const string ANIM_LARM_GESTUREID = "LArm_GestureId";
    private const string ANIM_LARM_TRIGGER = "LArm_Trigger";

    // Use this for initialization
    void Start () {
		if (!avatar)
        {
            avatar = gameObject;
        }

        animator = avatar.GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

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

        // Get correct trigger names based on handedness
        string anim_gestureid_name;
        string anim_trigger_name;

        if (gesture.BodyPart == AvatarGesture.Body.LeftArm)
        {
            anim_gestureid_name = ANIM_LARM_GESTUREID;
            anim_trigger_name = ANIM_LARM_TRIGGER;
        }
        else
        {
            anim_gestureid_name = ANIM_RARM_GESTUREID;
            anim_trigger_name = ANIM_RARM_TRIGGER;
        }

        // Trigger the gesture
        animator.SetInteger(anim_gestureid_name, gesture.Id);
        animator.SetTrigger(anim_trigger_name);

        // Callback upon completion
        if (callback != null)
        {
            // TODO -- incorrect timing for callback
            callback(gesture);
        }
    }
}
