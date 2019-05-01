/* FixHandRotation.cs
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
 *        
 *        This script operates on a single InteractionTarget. To support two hands, add this
 *        component twice on the InteractionObject, one script with references to the left hand
 *        and shoulder, and the other with references to the right hand and shoulder.
 */

using UnityEngine;

using RootMotion.FinalIK;

namespace VoxSimPlatform {
    namespace Animation {
        public class FixHandRotation : MonoBehaviour {
        	[Tooltip("Required. Reference to the FinalIK interaction system (the Avatar).")]
        	public InteractionSystem interactionSystem;

        	[Tooltip(
        		"Required. Reference to the avatar's shoulder joint. Set this to the left shoulder for the left hand, and the right shoulder for the right hand.")]
        	public GameObject rootJoint;

        	public FullBodyBipedEffector effectorType = FullBodyBipedEffector.LeftHand;

        	[Tooltip("(Optional) Set this to override the interaction target for either the left or right hand.")]
        	public InteractionTarget handTarget; // Child InteractionTarget representing the desired hand pose

        	[Tooltip("If set to true, will use the specified local direction vector instead of (transform.right).")]
        	public bool overrideDirection;

        	[Tooltip("Specifies the local direction vector of the hand.")]
        	public Vector3 localDirection = new Vector3(0.8660254f, 0f, 0.5f); // Set to default for hand

        	private InteractionObject interactionObject; // FinalIK InteractionObject component for this object
        	//private FullBodyBipedEffector effectorType; // Effector type from hand target

        	private bool needObjectRotationReset; // When set to true, will reset the object rotation once released from grasp
        	private Vector3 initialObjectRotation; // Cached rotation from before interaction

        	// Use this for initialization
        	void Start() {
        		// Get FinalIK components
        		interactionObject = GetComponent<InteractionObject>();
        		//handTarget = GetComponentInChildren<InteractionTarget>();

        		if (!handTarget) {
        			// Get the left or right hand
        			foreach (var target in GetComponentsInChildren<InteractionTarget>()) {
        				if (target.effectorType == effectorType) {
        					handTarget = target;
        					break;
        				}
        			}
        		}
        		else {
        			// Get the type from the selected target
        			effectorType = handTarget.effectorType;
        		}
        	}

        	// Update is called once per frame
        	void Update() {
        		if (handTarget) {
        			// Calculate rotation needed to keep the hand natural
        			Vector3 handDirection = GetHandDirection().normalized;
        			Vector3 objectDirection = (transform.position - rootJoint.transform.position).normalized;
        			float delta = GetAngleBetween(Flatten(handDirection), Flatten(objectDirection));

        			// Rotate the object if grabbing, else rotate the hand alone
        			if (IsGrabbingObject()) {
        				if (!needObjectRotationReset) {
        					// Cache the rotation of the object
        					initialObjectRotation = transform.rotation.eulerAngles;
        					needObjectRotationReset = true;
        				}

        				// Rotate the object
        				transform.Rotate(Vector3.up, delta);
        			}
        			else {
        				if (needObjectRotationReset) {
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
        	private Vector3 GetHandDirection() {
        		if (overrideDirection) {
        			return handTarget.transform.TransformDirection(localDirection);
        		}

        		return handTarget.transform.right;
        	}

        	/// <summary>
        	/// Checks if the system is currently interacting with/grabbing this object.
        	/// </summary>
        	/// <returns>bool</returns>
        	private bool IsGrabbingObject() {
        		// First check if interacting with this object
        		if (interactionSystem.GetInteractionObject(effectorType) == interactionObject) {
        			// Next check if we are grabbing the object (interaction is active but paused)
        			if (interactionSystem.IsPaused(effectorType)) {
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
        	private static Vector2 Flatten(Vector3 vector) {
        		return new Vector2(vector.x, vector.z);
        	}

        	/// <summary>
        	/// Returns the angle (-180 to +180) between two Vector2 instances.
        	/// </summary>
        	/// <param name="from">Source vector</param>
        	/// <param name="to">Destination vector</param>
        	/// <returns>The delta angle needed to rotate source to destination, in degrees.</returns>
        	public static float GetAngleBetween(Vector2 from, Vector2 to) {
        		// Note: For Unity, CW is positive and CCW is negative, the opposite of math angles
        		float angle = (Mathf.Atan2(from.y, from.x) - Mathf.Atan2(to.y, to.x)) * Mathf.Rad2Deg;

        		// Constrain within range -180 to 180
        		if (angle > 180) {
        			angle -= 360;
        		}
        		else if (angle < -180) {
        			angle += 360;
        		}

        		return angle;
        	}
        }
    }
}