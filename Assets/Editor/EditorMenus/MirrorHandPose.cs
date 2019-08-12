using UnityEditor;
using UnityEngine;

using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
using VoxSimPlatform.Global;

namespace EditorMenus {
	/// <summary>
	/// This class creates custom menu items (under VoxSim >> Hand Poses) in the Unity editor.  These items are
	///  used to clone or modify existing hand poses (usually "grasp poses") on an object for use with the opposite hand
	///  or to change labels appropriately so that the hand poses is used with the correct hand given its geometric
	///  layout.
	/// 
	/// Mirror Selected Hand Pose takes the selected hand pose, if any, and creates a clone of it
	///  with the scale and position inverted along the X-axis and rotated 180° around the Y- and Z- axes
	///  (hence flipping handedness or chirality of the hand pose).  In child objects of the newly-mirrored
	///  pose, label and effector types are switched (Left->Right/l->r or vice versa) and InteractionTarget-
	///  related parameters are inverted along the X-axis.
	///  
	/// Flip Label Handedness does the same on a hand pose where the orientation is correct but the labeling is wrong
	///  (such as if you created what you believe to be a left hand hand pose but it turns out its spatial properties
	///  actually make it a right hand hand pose that is mislabeled).
	/// 
	/// Both of these methods will only be enabled if a valid hand pose is currently selected.
	///  "Valid hand pose" is here defined as an object that contains an InteractionTarget component
	///   and whose name begins with "[lr]Hand."
	/// </summary>
	public class MirrorHandPose : MonoBehaviour {
		/// <summary>
		/// Clones and flips handedness of the selected hand pose.
		/// </summary>
		// IN: none
		// OUT: none
		[MenuItem("VoxSim/Hand Poses/Mirror Selected Hand Pose %#m")]
		static void MirrorSelectedHandPose() {

            // get the selected game object
            GameObject obj = Selection.activeGameObject;

            // if the pose already has a corresponding mirror, we don't need to do anything.
            if (obj.transform.parent.Find("l" + obj.name.Remove(0, 1)) && obj.transform.parent.Find("r" + obj.name.Remove(0, 1))) {
                Debug.Log("Preexisting Mirror Pose");
                return;
            }


            // clone it, parent it to the same object as the original, and apply the requisite transformations
            GameObject clone = Instantiate(obj);
			clone.transform.parent = obj.transform.parent;

            //Find rotations
            Component[] rotations = clone.transform.parent.GetComponents(typeof(FixHandRotation));
            bool rotRight = false;
            bool rotLeft = false;
            bool toMirrorRight = clone.name.StartsWith("r");
            FixHandRotation newRot = null; // For if there is a corresponding rotation
            GameObject targetJoint = null;
            FullBodyBipedEffector targetHand = FullBodyBipedEffector.RightHand;
            Vector3 targetDir = new Vector3(0,0,0);
            InteractionSystem targetInteractionSystem = null;
            foreach (FixHandRotation rot in rotations) {
                // Check if rotations exist for both sides, and keep track of mirrored data
                if (rot.rootJoint.ToString().StartsWith("r")) {
                    rotRight = true;
                    string targetJointName = "l" + rot.rootJoint.name.ToString().Substring(1);
                    targetInteractionSystem = rot.interactionSystem; // Interaction System
                    targetJoint = GameObject.Find(targetJointName); // Root Joint
                    targetHand = FullBodyBipedEffector.LeftHand; // Effector Type
                    targetDir = new Vector3(-rot.localDirection.x, rot.localDirection.y, rot.localDirection.z); // Local Direction
                    GameObject origJoint = rot.rootJoint; // The shoulder (or whatever else) we start with.
                    if (targetJoint == null || targetJoint.transform.root != origJoint.transform.root) {
                        Debug.Log("No mirror joint or wrong parent");
                    }
                }
                else if (rot.rootJoint.ToString().StartsWith("l")) {
                    // See above block
                    rotLeft = true;
                    string targetJointName = "r" + rot.rootJoint.name.ToString().Substring(1);
                    targetInteractionSystem = rot.interactionSystem;
                    targetJoint = GameObject.Find(targetJointName);
                    targetHand = FullBodyBipedEffector.RightHand;
                    targetDir = new Vector3(-rot.localDirection.x, rot.localDirection.y, rot.localDirection.z);
                    GameObject origJoint = rot.rootJoint;
                    if (targetJoint == null || targetJoint.transform.root != origJoint.transform.root) {
                        Debug.Log("No mirror joint or wrong parent");
                    }
                }
            }
            // Only need to make a new rotation if we're missing exactly one for the new function.
            // And only if old one matches with the one we're mirroring.
            if (rotRight ^ rotLeft && rotRight == toMirrorRight) {
                // Set parameters for new rotation that we now know we need.
                Debug.Log(targetJoint);
                newRot = clone.transform.parent.gameObject.AddComponent<FixHandRotation>();
                newRot.rootJoint = targetJoint;
                newRot.effectorType = targetHand;
                newRot.localDirection = targetDir;
                newRot.interactionSystem = targetInteractionSystem;
            }

            // mirror along the X-axis
            clone.transform.localScale = new Vector3(-obj.transform.localScale.x,
				obj.transform.localScale.y, obj.transform.localScale.z);
			// mirror its local position across the YZ-plane (along the X-axis)
			clone.transform.localPosition = new Vector3(-obj.transform.localPosition.x,
				obj.transform.localPosition.y, obj.transform.localPosition.z);
			// reverse its rotation around the Y- and Z-axes
			clone.transform.localEulerAngles = Quaternion.Euler(new Vector3(0.0f, 180.0f, 180.0f)) * obj.transform.localEulerAngles;

			// remove "(Clone)" from name
			clone.name = clone.name.Replace("(Clone)", "");

			// Get the InteractionTarget component and transforms of all children
			InteractionTarget interactionTarget = clone.GetComponent<InteractionTarget>();
			Transform[] allChildren = clone.GetComponentsInChildren<Transform>();

			if (clone.name.StartsWith("r")) {
				// replace right-hand labels with left-hand labels
				clone.name = "l" + clone.name.Remove(0, 1);

				foreach (Transform child in allChildren) {
					if (child.name.StartsWith("r")) {
						child.name = "l" + child.name.Remove(0, 1);
					}

					if (child.name.StartsWith("Right")) {
						child.name = "Left" + child.name.Remove(0, "Right".Length);
					}

					if (child.name.EndsWith("PointR")) {
						child.name = child.name.Replace("PointR", "PointL");
					}
				}

				// switch handedness of effector type
				if (interactionTarget.effectorType == FullBodyBipedEffector.RightHand) {
					interactionTarget.effectorType = FullBodyBipedEffector.LeftHand;
				}
				else if (interactionTarget.effectorType == FullBodyBipedEffector.RightShoulder) {
					interactionTarget.effectorType = FullBodyBipedEffector.LeftShoulder;
				}

				// invert the twist axis
				if (Mathf.Abs(interactionTarget.twistAxis.x) > Constants.EPSILON) {
					interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
						interactionTarget.twistAxis.z);
				}
			}
			else if (clone.name.StartsWith("l")) {
				// replace left-hand labels with right-hand labels
				clone.name = "r" + clone.name.Remove(0, 1);

				foreach (Transform child in allChildren) {
					if (child.name.StartsWith("l")) {
						child.name = "r" + child.name.Remove(0, 1);
					}

					if (child.name.StartsWith("Left")) {
						child.name = "Right" + child.name.Remove(0, "Left".Length);
					}

					if (child.name.EndsWith("PointL")) {
						child.name = child.name.Replace("PointL", "PointR");
					}
				}

				// switch handedness of effector type
				if (interactionTarget.effectorType == FullBodyBipedEffector.LeftHand) {
					interactionTarget.effectorType = FullBodyBipedEffector.RightHand;
				}
				else if (interactionTarget.effectorType == FullBodyBipedEffector.LeftShoulder) {
					interactionTarget.effectorType = FullBodyBipedEffector.RightShoulder;
				}

				// invert the twist axis
				if (Mathf.Abs(interactionTarget.twistAxis.x) > Constants.EPSILON) {
					interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
						interactionTarget.twistAxis.z);
				}
			}
		}

		/// <summary>
		/// Verify that the selected object is a hand pose.  Enable MirrorSelectedHandPose if true.
		/// </summary>
		// IN: none
		// OUT: bool
		[MenuItem("VoxSim/Hand Poses/Mirror Selected Hand Pose %#m", true)]
		static bool ValidateSelectedHandPoseToMirror() {
			// Return false if no transform is selected, or if the selected transform does not contain
			//  an InteractionTarget component or does not begin with "[lr]Hand"
			return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
			       (Selection.activeGameObject.GetComponent<InteractionTarget>() != null) &&
			       ((Selection.activeGameObject.name.StartsWith("lHand")) ||
			        (Selection.activeGameObject.name.StartsWith("rHand")));
		}

		/// <summary>
		/// Flips the handedness of labels on the selected hand pose.
		/// </summary>
		// IN: none
		// OUT: none
		[MenuItem("VoxSim/Hand Poses/Flip Label Handedness %#h")]
		static void FlipLabelHandedness() {
			// get the selected game object, its InteractionTarget and transforms of all children
			GameObject obj = Selection.activeGameObject;
			InteractionTarget interactionTarget = obj.GetComponent<InteractionTarget>();
			Transform[] allChildren = obj.GetComponentsInChildren<Transform>();

			if (obj.name.StartsWith("r")) {
				// replace right-hand labels with left-hand labels
				obj.name = "l" + obj.name.Remove(0, 1);

				foreach (Transform child in allChildren) {
					if (child.name.StartsWith("r")) {
						child.name = "l" + child.name.Remove(0, 1);
					}

					if (child.name.StartsWith("Right")) {
						child.name = "Left" + child.name.Remove(0, "Right".Length);
					}

					if (child.name.EndsWith("PointR")) {
						child.name = child.name.Replace("PointR", "PointL");
					}
				}

				// switch handedness of effector type
				if (interactionTarget.effectorType == FullBodyBipedEffector.RightHand) {
					interactionTarget.effectorType = FullBodyBipedEffector.LeftHand;
				}
				else if (interactionTarget.effectorType == FullBodyBipedEffector.RightShoulder) {
					interactionTarget.effectorType = FullBodyBipedEffector.LeftShoulder;
				}

				// invert the twist axis
				if (Mathf.Abs(interactionTarget.twistAxis.x) < Constants.EPSILON) {
					interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
						interactionTarget.twistAxis.z);
				}
			}
			else if (obj.name.StartsWith("l")) {
				// replace left-hand labels with right-hand labels
				obj.name = "r" + obj.name.Remove(0, 1);

				foreach (Transform child in allChildren) {
					if (child.name.StartsWith("l")) {
						child.name = "r" + child.name.Remove(0, 1);
					}

					if (child.name.StartsWith("Left")) {
						child.name = "Right" + child.name.Remove(0, "Left".Length);
					}

					if (child.name.EndsWith("PointL")) {
						child.name = child.name.Replace("PointL", "PointR");
					}
				}

				// switch handedness of effector type
				if (interactionTarget.effectorType == FullBodyBipedEffector.LeftHand) {
					interactionTarget.effectorType = FullBodyBipedEffector.RightHand;
				}
				else if (interactionTarget.effectorType == FullBodyBipedEffector.LeftShoulder) {
					interactionTarget.effectorType = FullBodyBipedEffector.RightShoulder;
				}

				// invert the twist axis
				if (Mathf.Abs(interactionTarget.twistAxis.x) < Constants.EPSILON) {
					interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
						interactionTarget.twistAxis.z);
				}
			}
		}

		/// <summary>
		/// Verify that the selected object is a hand pose.  Enable FlipLabelHandedness if true.
		/// </summary>
		// IN: none
		// OUT: bool
		[MenuItem("VoxSim/Hand Poses/Flip Label Handedness %#h", true)]
		static bool ValidateSelectedHandPoseToRename() {
			// Return false if no transform is selected, or if the selected transform does not contain
			//  an InteractionTarget component or does not begin with "[lr]Hand"
			return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
			       (Selection.activeGameObject.GetComponent<InteractionTarget>() != null) &&
			       ((Selection.activeGameObject.name.StartsWith("lHand")) ||
			        (Selection.activeGameObject.name.StartsWith("rHand")));
		}
	}
}