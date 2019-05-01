using UnityEngine;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Agent {
        [RequireComponent(typeof(Animator))]
        public class IKControl : MonoBehaviour {
        	protected Animator animator;

        	public bool ikActive = false;
        	public Transform leftHandObj = null;
        	public Transform rightHandObj = null;
        	public Transform lookObj = null;

        	public Vector3 targetPosition;
        	public Vector3 targetRotation;

        	public float turnSpeed = 5.0f;

        	void Start() {
        		animator = GetComponentInChildren<Animator>();
        	}

        	void Update() {
        		if (!Helper.VectorIsNaN(targetRotation)) {
        			// has valid target
        			if (transform.rotation != Quaternion.Euler(targetRotation)) {
        				float offset = RotateToward(targetRotation);

        				if ((Mathf.Deg2Rad * offset) < 0.01f) {
        					transform.rotation = Quaternion.Euler(targetRotation);
        				}
        			}
        		}
        	}

        	//a callback for calculating IK
        	void OnAnimatorIK() {
        		if (animator) {
        			//if the IK is active, set the position and rotation directly to the goal. 
        			if (ikActive) {
        				// Set the look target position, if one has been assigned
        				if (lookObj != null) {
        					animator.SetLookAtWeight(1);
        					animator.SetLookAtPosition(lookObj.position);
        				}

        				// Set the left hand target position and rotation, if one has been assigned
        				if (leftHandObj != null) {
        					animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
        					//animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);  
        					animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
        					//animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandObj.rotation);
        				}

        				// Set the right hand target position and rotation, if one has been assigned
        				if (rightHandObj != null) {
        					animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
        					//animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);  
        					animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandObj.position);
        					//animator.SetIKRotation(AvatarIKGoal.RightHand,rightHandObj.rotation);
        				}
        			}

        			//if the IK is not active, set the position and rotation of the hand and head back to the original position
        			else {
        				animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
        				animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
        				animator.SetLookAtWeight(0);
        			}
        		}
        	}

        	float RotateToward(Vector3 target) {
        		float offset = 0.0f;
        		//if (!isGrasped) {
        		//Quaternion offset = Quaternion.FromToRotation (transform.eulerAngles, targetRotation);
        		//Vector3 normalizedOffset = Vector3.Normalize (offset);

        		float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(target));
        		float timeToComplete = angle / turnSpeed;
        		float donePercentage = Mathf.Min(1.0f, Time.deltaTime / timeToComplete);
        		Quaternion rot = Quaternion.Slerp(transform.rotation, Quaternion.Euler(target), donePercentage * 100.0f);
        		//Debug.Log (turnSpeed);
        		//Quaternion resolve = Quaternion.identity;

        //		if (rigging.usePhysicsRig) {
        //			float displacementAngle = 360.0f;
        //			Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody> ();
        //			foreach (Rigidbody rigidbody in rigidbodies) {
        //				rigidbody.MoveRotation (rot);
        //			}
        //		}

        		transform.rotation = rot;
        		//GameObject.Find ("ReachObject").transform.position = transform.position;

        		/*foreach (Voxeme child in children) {
        			if (child.isActiveAndEnabled) {
        				if (child.gameObject != gameObject) {
        					child.transform.localRotation = parentToChildRotationOffset [child.gameObject];
        					child.transform.rotation = gameObject.transform.rotation * child.transform.localRotation;
        					child.targetRotation = child.transform.rotation.eulerAngles;
        					child.transform.localPosition = Helper.RotatePointAroundPivot (parentToChildPositionOffset [child.gameObject],
        						Vector3.zero, gameObject.transform.eulerAngles);
        					child.transform.position = gameObject.transform.position + child.transform.localPosition;
        					child.targetPosition = child.transform.position;
        					//Debug.Log (child.name);
        					//Debug.Break ();
        					//Debug.Log (Helper.VectorToParsable(child.transform.localPosition));
        				}
        			}
        		}*/

        		offset = Quaternion.Angle(rot, Quaternion.Euler(target));
        		//Debug.Log (offset);

        		return offset;
        	}
        }
    }
}