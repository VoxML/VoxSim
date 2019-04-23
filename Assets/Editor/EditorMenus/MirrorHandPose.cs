using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Global;
using RootMotion.FinalIK;

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

public class MirrorHandPose : MonoBehaviour
{
    [MenuItem("VoxSim/Hand Poses/Mirror Selected Hand Pose %#m")]
    static void MirrorSelectedHandPose() {
        //Debug.Log(Selection.activeGameObject.name);

        GameObject obj = Selection.activeGameObject;

        GameObject clone = Instantiate(obj);
        clone.transform.parent = obj.transform.parent;
        clone.transform.localScale = new Vector3(-obj.transform.localScale.x,
            obj.transform.localScale.y,obj.transform.localScale.z);
        clone.transform.localPosition = new Vector3(-obj.transform.localPosition.x,
            obj.transform.localPosition.y, obj.transform.localPosition.z);
        clone.transform.localEulerAngles = Quaternion.Euler(new Vector3(0.0f, 180.0f, 180.0f)) * obj.transform.localEulerAngles;

        clone.name = clone.name.Replace("(Clone)", "");

        InteractionTarget interactionTarget = clone.GetComponent<InteractionTarget>();

        Transform[] allChildren = clone.GetComponentsInChildren<Transform>();

        if (clone.name.StartsWith("r")) {
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

            if (interactionTarget.effectorType == FullBodyBipedEffector.RightHand) {
                interactionTarget.effectorType = FullBodyBipedEffector.LeftHand;
            }
            else if (interactionTarget.effectorType == FullBodyBipedEffector.RightShoulder) {
                interactionTarget.effectorType = FullBodyBipedEffector.LeftShoulder;
            }

            if (Mathf.Abs(interactionTarget.twistAxis.x) > Constants.EPSILON) {
                interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
                    interactionTarget.twistAxis.z);
            }
        }
        else if (clone.name.StartsWith("l")) {
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

            if (interactionTarget.effectorType == FullBodyBipedEffector.LeftHand) {
                interactionTarget.effectorType = FullBodyBipedEffector.RightHand;
            }
            else if (interactionTarget.effectorType == FullBodyBipedEffector.LeftShoulder) {
                interactionTarget.effectorType = FullBodyBipedEffector.RightShoulder;
            }

            if (Mathf.Abs(interactionTarget.twistAxis.x) > Constants.EPSILON) {
                interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
                    interactionTarget.twistAxis.z);
            }
        }
    }

    [MenuItem("VoxSim/Hand Poses/Mirror Selected Hand Pose %#m", true)]
    static bool ValidateSelectedHandPoseToMirror()
    {
        // Return false if no transform is selected.
        return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
            (((GameObject)Selection.activeGameObject).GetComponent<InteractionTarget>() != null) &&
            ((((GameObject)Selection.activeGameObject).name.StartsWith("lHand")) ||
                (((GameObject)Selection.activeGameObject).name.StartsWith("rHand")));
    }

    [MenuItem("VoxSim/Hand Poses/Flip Label Handedness %#h")]
    static void FlipLabelHandedness()
    {
        GameObject obj = Selection.activeGameObject;
        InteractionTarget interactionTarget = obj.GetComponent<InteractionTarget>();
        Transform[] allChildren = obj.GetComponentsInChildren<Transform>();

        if (obj.name.StartsWith("r")) {
            obj.name = "l" + obj.name.Remove(0, 1);

            foreach (Transform child in allChildren) {
                if (child.name.StartsWith("r")) {
                    child.name = "l" + child.name.Remove(0, 1);
                }

                if (child.name.StartsWith("Right")) {
                    child.name = "Left" + child.name.Remove(0, "Right".Length);
                }

                if (child.name.EndsWith("PointR")) {
                    child.name = child.name.Replace("PointR","PointL");
                }
            }

            if (interactionTarget.effectorType == FullBodyBipedEffector.RightHand) {
                interactionTarget.effectorType = FullBodyBipedEffector.LeftHand;
            }
            else if (interactionTarget.effectorType == FullBodyBipedEffector.RightShoulder) {
                interactionTarget.effectorType = FullBodyBipedEffector.LeftShoulder;
            }

            if (Mathf.Abs(interactionTarget.twistAxis.x) < Constants.EPSILON) {
                interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
                    interactionTarget.twistAxis.z);
            }
        }
        else if (obj.name.StartsWith("l")) {
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

            if (interactionTarget.effectorType == FullBodyBipedEffector.LeftHand) {
                interactionTarget.effectorType = FullBodyBipedEffector.RightHand;
            }
            else if (interactionTarget.effectorType == FullBodyBipedEffector.LeftShoulder) {
                interactionTarget.effectorType = FullBodyBipedEffector.RightShoulder;
            }

            if (Mathf.Abs(interactionTarget.twistAxis.x) < Constants.EPSILON) {
                interactionTarget.twistAxis = new Vector3(-interactionTarget.twistAxis.x, interactionTarget.twistAxis.y,
                    interactionTarget.twistAxis.z);
            }
        }
    }

    [MenuItem("VoxSim/Hand Poses/Flip Label Handedness %#h", true)]
    static bool ValidateSelectedHandPoseToRename()
    {
        // Return false if no transform is selected.
        return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
            (((GameObject)Selection.activeGameObject).GetComponent<InteractionTarget>() != null) &&
            ((((GameObject)Selection.activeGameObject).name.StartsWith("lHand")) || 
                (((GameObject)Selection.activeGameObject).name.StartsWith("rHand")));
    }
}
