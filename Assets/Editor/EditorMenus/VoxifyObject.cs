using UnityEditor;
using UnityEngine;
using System.Linq;

using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
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
            FixHandRotation lHandRot;
            FixHandRotation rHandRot;


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
                lHandRot = obj.AddComponent<FixHandRotation>();
                rHandRot = obj.AddComponent<FixHandRotation>();
                lHandRot.effectorType = FullBodyBipedEffector.LeftHand;
                rHandRot.effectorType = FullBodyBipedEffector.RightHand;
                lHandRot.localDirection.x = -lHandRot.localDirection.x; // Mirror

                if (agent != null) {
                    lHandRot.interactionSystem = agent.GetComponent<InteractionSystem>();
                    lHandRot.rootJoint = agent.GetComponent<FullBodyBipedIK>().references.leftUpperArm.gameObject;
                    lHandRot.overrideDirection = true;

                    rHandRot.interactionSystem = agent.GetComponent<InteractionSystem>();
                    rHandRot.rootJoint = agent.GetComponent<FullBodyBipedIK>().references.rightUpperArm.gameObject;
                    rHandRot.overrideDirection = true;
                }
            }
            
            // Interaction Object, rotate with me
            if (obj.GetComponent<InteractionObject>() != null) {
                obj.AddComponent<InteractionObject>();
            }
            if(obj.GetComponent<RotateWithMe>() != null) {
                RotateWithMe rotWithMe = obj.AddComponent<RotateWithMe>();
                rotWithMe.source = agent;
                rotWithMe.rotateAround = RotateWithMe.Axis.Y;
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
            return (Selection.activeGameObject != null) && (Selection.activeGameObject.activeSelf) &&
                   (Selection.activeGameObject.GetComponentsInChildren<MeshRenderer>().Where(
                       r => r.enabled).ToList().Count > 0);
        }
    }
}