using UnityEngine;

using UnityEditor;

using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
using VoxSimPlatform.Global;

namespace EditorMenus {
    /// <summary>
    /// CreateSelectedHandPose takes a selected hand (currently attached to an unfortunate character model)
    /// and lops it off to be used as reference for the pose to pick up objects.
    /// Only enabled if a hand is currently selected (doesn't *need* to be on a person).
    ///  "Valid hand pose" is here defined as an object whose name begins with "[lr]Hand."
    /// </summary>

    public class CreateHandPose : MonoBehaviour {


        [MenuItem("VoxSim/Hand Poses/Create Hand Pose %#r")] // it was available
        static void CreateSelectedHandPose() {
            /// <summary>
            /// Removes the person from the hand and creates an InteractionTarget
            /// IN: none
            /// OUT: none
            /// </summary>

            // get the selected game object
            GameObject obj = Selection.activeGameObject;
            // Check object is, in fact, a hand.
            if (obj.name != "lHand" && obj.name != "rHand") {
                Debug.Log("Currently selected object is not a hand.");
                return;
            }
            GameObject clone = Instantiate(obj);

            //Find parent (e.g. Diana)
            GameObject root_person = obj.transform.root.gameObject;
            Debug.Log(root_person);

            // Make sure new hand is in the same place as old one
            clone.transform.position = obj.transform.position;
            clone.transform.rotation = obj.transform.rotation;

            // Move hand wireframe to be a top-level object
            clone.transform.parent = null;

            // Delete extraneous components
            foreach (Component comp in clone.GetComponents<MonoBehaviour>()) {
                if (!(comp is InteractionTarget || comp is Transform)) {
                    DestroyImmediate(comp);
                }
            }
            BoxCollider bc = clone.GetComponent<BoxCollider>(); // Only other component I've seen
            if (bc != null) {
                DestroyImmediate(bc);
            }

            // Set Interaction Target if it is not already there.
            InteractionTarget target = clone.GetComponent<InteractionTarget>();
            if (target == null) { // Only want to make one if it doesn't already exist.
                                  // Make a new one that says what hand to use and such
                InteractionTarget to_assign_target = clone.AddComponent<InteractionTarget>() as InteractionTarget;
                if (clone.name.StartsWith("r")) {
                    to_assign_target.effectorType = FullBodyBipedEffector.RightHand;
                }
                else if (clone.name.StartsWith("l")) {
                    to_assign_target.effectorType = FullBodyBipedEffector.LeftHand;
                }
                to_assign_target.twistAxis = new Vector3(0, 0, 1); // specs say z axis, for wrist.
            }

            // remove "(Clone)" from name
            clone.name = clone.name.Replace("(Clone)", "");

            // yeet the rest of the body
            if (clone.transform.root != obj.transform.root) { // edge case where you were already sans body
                DestroyImmediate(root_person);
            }

            // Set new handpose as the new selection (since we deleted the old one)
            Selection.objects = new GameObject[] { clone };
        }
        /// <summary>
        /// Verify that the selected object is a hand.  Enable CreateSelectedHandPose if true.
        /// </summary>
        // IN: none
        // OUT: bool
        [MenuItem("VoxSim/Hand Poses/Create Hand Pose %#r", true)]
        static bool ValidateSelectedHandPoseToMirror() {
            // Return false if no hand is selected, or if the selected transform does not contain
            //  an InteractionTarget component or does not begin with "[lr]Hand"
            return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
                   ((Selection.activeGameObject.name.StartsWith("lHand")) ||
                    (Selection.activeGameObject.name.StartsWith("rHand")));
        }
    }
}