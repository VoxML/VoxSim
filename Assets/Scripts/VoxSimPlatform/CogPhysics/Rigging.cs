using UnityEngine;
using System.Collections.Generic;

using VoxSimPlatform.Vox;
using VoxSimPlatform.Animation;
using VoxSimPlatform.SpatialReasoning;

namespace VoxSimPlatform {
    namespace CogPhysics {
        public enum PhysicsActivationSignal {
            Activate,
            Deactivate
        }

        public class Rigging : MonoBehaviour {
        	//[HideInInspector]
        	public bool usePhysicsRig = true;
        	RelationTracker relationTracker;
        	List<Voxeme> ignorePhysics; // ignore physics between this game object and listed objects

        	[HideInInspector] public Rigidbody[] rigidbodies;

        	// Use this for initialization
        	void Start() {
        		relationTracker = (RelationTracker) GameObject.Find("BehaviorController").GetComponent("RelationTracker");

        		ignorePhysics = new List<Voxeme>();
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	public void ActivatePhysics(bool active) {
        		if (!active) {
        			// make this object unaffected by default physics rigging
        			Debug.Log(gameObject.name + ": deactivating physics");
        			//Debug.Break ();

        			// disable colliders
        			BoxCollider[] colliders = gameObject.GetComponentsInChildren<BoxCollider>();
        			foreach (BoxCollider collider in colliders) {
        				if (collider.gameObject != gameObject) {
        					if (collider != null) {
        						collider.isTrigger = true;
        					}
        				}
        			}

        			// disable rigidbodies
        			Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
        			foreach (Rigidbody rigidbody in rigidbodies) {
        				if (rigidbody.gameObject != gameObject) {
        					if (rigidbody != null) {
        						rigidbody.useGravity = false;
        						rigidbody.isKinematic = true;
        					}
        				}
        			}

        			foreach (FixHandRotation handRot in gameObject.GetComponentsInChildren<FixHandRotation>()) {
        				handRot.enabled = true;
        			}

        			usePhysicsRig = false;
        		}
        		else {
        			// make this object affected by default physics rigging
        			Debug.Log(gameObject.name + ": activating physics");

        			// enable colliders
        			BoxCollider[] colliders = gameObject.GetComponentsInChildren<BoxCollider>();
        			foreach (BoxCollider collider in colliders) {
        				if (collider.gameObject != gameObject) {
        					// don't reactivate physics on rigged children
        					// if this object is concave
        					// and other physics special cases
        //					Debug.Log(collider.name);
        //					Debug.Log(collider.transform.IsChildOf (gameObject.transform));
        //					Debug.Log(collider.gameObject.GetComponent<Voxeme> ());
        //					Debug.Log(gameObject.GetComponent<Voxeme> ().voxml.Type.Concavity);
        					//Debug.Log ((collider.transform.IsChildOf (gameObject.transform) && collider.gameObject.GetComponent<Voxeme> () != null &&
        					//gameObject.GetComponent<Voxeme> ().voxml.Type.Concavity.Contains ("Concave")));
        					if ((!(collider.transform.IsChildOf(gameObject.transform) &&
        					       collider.gameObject.GetComponent<Voxeme>() != null &&
        					       gameObject.GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"))) ||
        					    (gameObject.GetComponent<Voxeme>().isGrasped)) {
        						//if (!(collider.transform.IsChildOf(gameObject.transform) && gameObject.GetComponent<Voxeme>().voxml.Type.Concavity == "Concave") &&
        						//	!RCC8.ProperPart(Helper.GetObjectWorldSize(collider.gameObject),Helper.GetObjectWorldSize(gameObject))) {
        						//if (!((collider.transform.IsChildOf(gameObject.transform) &&
        						//	gameObject.GetComponent<Voxeme>().voxml.Type.Concavity == "Concave" &&
        						//	relationTracker.relations[new List<GameObject>(new GameObject[]{gameObject,collider.gameObject})] == "contain"))) {
        						if (collider != null) {
        							collider.isTrigger = false;
        						}
        					}
        				}
        			}

        			// enable rigidbodies
        			Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
        			foreach (Rigidbody rigidbody in rigidbodies) {
        				if (rigidbody.gameObject != gameObject) {
        					// don't reactivate physics on rigged children
        					// if this object is concave
        					// and other physics special cases
        					if ((!(rigidbody.transform.IsChildOf(gameObject.transform) &&
        					       rigidbody.gameObject.GetComponent<Voxeme>() != null &&
        					       gameObject.GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"))) ||
        					    (gameObject.GetComponent<Voxeme>().isGrasped)) {
        						//if (!(rigidbody.transform.IsChildOf(gameObject.transform) && gameObject.GetComponent<Voxeme>().voxml.Type.Concavity == "Concave") &&
        						//	!RCC8.ProperPart(Helper.GetObjectWorldSize(rigidbody.gameObject),Helper.GetObjectWorldSize(gameObject))) {
        						//if (!((rigidbody.transform.IsChildOf(gameObject.transform) &&
        						//	gameObject.GetComponent<Voxeme>().voxml.Type.Concavity == "Concave" &&
        						//	relationTracker.relations[new List<GameObject>(new GameObject[]{gameObject,rigidbody.gameObject})] == "contain"))) {
        						if (rigidbody != null) {
        							rigidbody.useGravity = true;
        							rigidbody.isKinematic = false;
        						}
        					}
        				}
        			}

        			usePhysicsRig = true;
        		}
        	}
        }

        public static class RiggingHelper {
        	public static void RigTo(GameObject child, GameObject parent) {
        		// disable child voxeme component
        		Voxeme voxeme = child.GetComponent<Voxeme>();
        		if (voxeme != null) {
        			voxeme.enabled = false;
        		}

        		child.transform.parent = parent.transform;
        		//Debug.LogWarning(child.name + " rigged to " + parent.name);

        		if (!child.GetComponent<Rigging>().usePhysicsRig) {
        			if (parent.transform.Find(string.Format("{0}_collision_clone", child.name)) == null) {
        				GameObject childCollisionClone =
        					GameObject.Instantiate(child, child.transform.position, child.transform.rotation);
        				foreach (Renderer renderer in childCollisionClone.GetComponentsInChildren<Renderer>()) {
        					renderer.enabled = false;
        				}

        				childCollisionClone.transform.parent = parent.transform;
        				BoxCollider[] colliders = childCollisionClone.GetComponentsInChildren<BoxCollider>();
        				foreach (BoxCollider collider in colliders) {
        					if (collider.gameObject != childCollisionClone) {
        						if (collider != null) {
        							collider.isTrigger = false;
        						}
        					}
        				}

        				FixedJoint[] fixedJoints = childCollisionClone.GetComponentsInChildren<FixedJoint>();
        				foreach (FixedJoint fixedJoint in fixedJoints) {
        					Object.Destroy(fixedJoint);
        				}

        				Rigidbody[] rigidbodies = childCollisionClone.GetComponentsInChildren<Rigidbody>();
        				foreach (Rigidbody rigidbody in rigidbodies) {
        					Object.Destroy(rigidbody);
        				}

        				childCollisionClone.name = childCollisionClone.name.Replace("(Clone)", "_collision_clone");
        			}
        		}
        	}

        	public static void UnRig(GameObject child, GameObject parent) {
        		// disable child voxeme component
        		Voxeme voxeme = child.GetComponent<Voxeme>();
        		if (voxeme != null) {
        			voxeme.enabled = true;
        		}

        		child.transform.parent = null;

        		Transform childCollisionClone = parent.transform.Find(string.Format("{0}_collision_clone", child.name));
        		if (childCollisionClone != null) {
        			Object.Destroy(childCollisionClone.gameObject);
        		}
        	}
        }
    }
}