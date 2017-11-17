﻿/* FixHandRotation.cs
 * USAGE: Attach component to InteractionObjects as needed. This script will automatically
 *        look for any child InteractionTargets that define hand poses. The interaction system
 *        and root joint need to be specified.
 *        
 *        This script will rotate the desired hand pose to point at the root joint (which should
 *        be a reference to the shoulder). This will prevent any contortion caused by impossible
 *        hand positioning.
 *        
 *        The hand direction is specified either as the local X-axis direction, or specified
 *        manually with localDirection. (For Diana, this needs to be overriden with the default
 *        localDirection.)
 */  

using RootMotion.FinalIK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixHandRotation : MonoBehaviour {
    [Tooltip("Required. Reference to the FinalIK interaction system.")]
    public InteractionSystem interactionSystem;

	[Tooltip("Required. Reference to the avatar's right shoulder joint.")]
	public GameObject leftRootJoint;

	[Tooltip("Required. Reference to the avatar's right shoulder joint.")]
    public GameObject rightRootJoint;

    [Tooltip("If set to true, will use the specified local direction vector instead of (transform.right).")]
    public bool overrideDirection;

    [Tooltip("Specifies the local direction vector of the left hand.")]
    public Vector3 leftLocalDirection = new Vector3(-0.8660254f, 0f, 0.5f); // Set to default for hand

	[Tooltip("Specifies the local direction vector of the right hand.")]
	public Vector3 rightLocalDirection = new Vector3(0.8660254f, 0f, 0.5f); // Set to default for hand

    private InteractionObject interactionObject; // FinalIK InteractionObject component for this object
    private InteractionTarget handTarget; // Child InteractionTarget representing the desired hand pose
    private FullBodyBipedEffector effectorType; // Effector type from hand target (could be left or right hand)

    private bool needObjectRotationReset; // When set to true, will reset the object rotation once released from grasp
    private Vector3 initialObjectRotation; // Cached rotation from before interaction

    // Use this for initialization
    void Start () {
        // Get FinalIK components
        interactionObject = GetComponent<InteractionObject>();
        handTarget = GetComponentInChildren<InteractionTarget>();
        if (handTarget)
        {
            effectorType = handTarget.effectorType;
        }
    }
	
	// Update is called once per frame
	void Update () {
		if (handTarget)
        {
            // Calculate rotation needed to keep the hand natural
            Vector3 handDirection = GetHandDirection().normalized;
			Vector3 objectDirection = Vector3.zero;

			if (transform.position.x < interactionSystem.gameObject.transform.position.x)
			{
				objectDirection = (transform.position - leftRootJoint.transform.position).normalized;
			}
			else if (transform.position.x > interactionSystem.gameObject.transform.position.x)
			{
				objectDirection = (transform.position - rightRootJoint.transform.position).normalized;
			}

            float delta = GetAngleBetween(Flatten(handDirection), Flatten(objectDirection));

            // Rotate the object if grabbing, else rotate the hand alone
            if (IsGrabbingObject())
            {
                if (!needObjectRotationReset)
                {
                    // Cache the rotation of the object
                    initialObjectRotation = transform.rotation.eulerAngles;
                    needObjectRotationReset = true;
                }

                // Rotate the object
                transform.Rotate(Vector3.up, delta);
            }
            else
            {
                if (needObjectRotationReset)
                {
                    // Re-apply cached rotation now that we are no longer grabbing it
                    transform.rotation = Quaternion.Euler(initialObjectRotation);
                    needObjectRotationReset = false;
                }

                // Rotate the hand around the object
                handTarget.transform.RotateAround(transform.position, Vector3.up, delta);
            }
        }
	}

    /// <summary>
    /// Returns the direction vector of the hand. By default, uses transform.right unless a local direction is specified.
    /// </summary>
    /// <returns>A vector representing the hand direction.</returns>
    private Vector3 GetHandDirection()
    {
		float invert = 1.0f;   
		if (transform.position.x < interactionSystem.gameObject.transform.position.x) {
			if (overrideDirection) {
				return handTarget.transform.TransformDirection (leftLocalDirection);
			}
			invert = -1.0f;
		} 
		else if (transform.position.x > interactionSystem.gameObject.transform.position.x) {
			if (overrideDirection) {
				return handTarget.transform.TransformDirection (rightLocalDirection);
			}
			invert = 1.0f;
		}

		return new Vector3(handTarget.transform.right.x * invert, handTarget.transform.right.y, handTarget.transform.right.z);
    }

    /// <summary>
    /// Checks if the system is currently interacting with/grabbing this object.
    /// </summary>
    /// <returns>bool</returns>
    private bool IsGrabbingObject()
    {
        // First check if interacting with this object
        if (interactionSystem.GetInteractionObject(effectorType) == interactionObject)
        {
            // Next check if we are grabbing the object (interaction is active but paused)
            if (interactionSystem.IsPaused(effectorType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Flattens the vector to X and Z components only.
    /// </summary>
    /// <param name="vector">Input vector</param>
    /// <returns>Output vector</returns>
    private static Vector2 Flatten(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    /// <summary>
    /// Returns the angle (-180 to +180) between two Vector2 instances.
    /// </summary>
    /// <param name="from">Source vector</param>
    /// <param name="to">Destination vector</param>
    /// <returns>The delta angle needed to rotate source to destination, in degrees.</returns>
    public static float GetAngleBetween(Vector2 from, Vector2 to)
    {
        // Note: For Unity, CW is positive and CCW is negative, the opposite of math angles
        float angle = (Mathf.Atan2(from.y, from.x) - Mathf.Atan2(to.y, to.x)) * Mathf.Rad2Deg;

        // Constrain within range -180 to 180
        if (angle > 180)
        {
            angle -= 360;
        }
        else if (angle < -180)
        {
            angle += 360;
        }

        return angle;
    }
}
