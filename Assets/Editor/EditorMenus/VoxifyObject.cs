using UnityEditor;
using UnityEngine;

using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

namespace EditorMenus {


    /// <summary>
    /// Adds a number of components to the active game object
    /// to make it closer to fully voxxed.
    /// Select an agent as well (e.g. Diana) to fill in the hand rotations
    /// </summary>
    public class VoxifyObject : MonoBehaviour {


        [MenuItem("VoxSim/Voxify Object %#v")]
        static void Voxify() {
            // get the selected game object
            GameObject obj = Selection.activeGameObject;
            GameObject agent = null;
            GameObject[] selected = Selection.gameObjects;
            Voxeme vox;
            FixHandRotation hand_rot1;
            FixHandRotation hand_rot2;


            obj.layer = 10;//blocks=perceived layer

            if (selected.Length == 2) {
                // Kinda clunky, there's probably a more correct way to write this in c sharp
                if (selected[0] == obj && ((GameObject) selected[1]).GetComponents<InteractionSystem>().Length > 0) {
                    agent = selected[1];
                }
                else if (selected[1] == obj && ((GameObject)selected[0]).GetComponents<InteractionSystem>().Length > 0) {
                    agent = selected[0];
                }
            }

            // Add a number of properties
            // Check whether each exists beforehand.


            // Voxeme script
            if(obj.GetComponent<Voxeme>() == null) {
                GameObject temp = Instantiate(obj);
                vox = obj.AddComponent<Voxeme>(); // Deletes Transform attributes for some buggy reason
                                                  //Reclaim said transform attributes
                obj.transform.position = temp.transform.position;
                obj.transform.rotation = temp.transform.rotation;
                obj.transform.localScale = temp.transform.localScale;
                DestroyImmediate(temp, true);
            }
            
            // Rotate With Me
            // Fix Hand Rotation (one for each hand)
            if(obj.GetComponents<FixHandRotation>().Length == 0) {
                hand_rot1 = obj.AddComponent<FixHandRotation>();
                hand_rot2 = obj.AddComponent<FixHandRotation>();
                if (agent != null) {
                    hand_rot1.interactionSystem = agent.GetComponent<InteractionSystem>();
                    hand_rot1.rootJoint = agent.GetComponent<FullBodyBipedIK>().references.leftUpperArm.gameObject;
                    hand_rot1.effectorType = FullBodyBipedEffector.LeftHand;
                    //Local direction cannot be automated since it depends on the object :(
                    hand_rot1.overrideDirection = true;

                    hand_rot2.interactionSystem = agent.GetComponent<InteractionSystem>();
                    hand_rot2.rootJoint = agent.GetComponent<FullBodyBipedIK>().references.rightUpperArm.gameObject;
                    hand_rot2.effectorType = FullBodyBipedEffector.RightHand;
                    //Local direction cannot be automated since it depends on the object :(
                    hand_rot2.overrideDirection = true;
                }
            }
            
            // Interaction Object, rotate with me
            if (obj.GetComponent<InteractionObject>() != null) {
                obj.AddComponent<InteractionObject>();
            }
            if(obj.GetComponent<RotateWithMe>() != null) {
                RotateWithMe rotwme = obj.AddComponent<RotateWithMe>();
                rotwme.source = agent;
                rotwme.rotateAround = RotateWithMe.Axis.Y;
            }

            // v Unnecessary Components v
            // Box collider
            // Mesh renderer
        }

        /// <summary>
        /// Verify that the selected object contains a meshrenderer somewhere
        /// </summary>
        // IN: none
        // OUT: bool
        [MenuItem("VoxSim/Voxify Object %#v", true)]
        static bool ValidateVoxify() {
            // Return false if no transform is selected, or if the selected transform does not contain
            //  an InteractionTarget component or does not begin with "[lr]Hand"
            return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
                   (Selection.activeGameObject.GetComponentsInChildren<MeshRenderer>().Length > 0);
        }
    }
}