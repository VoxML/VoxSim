using UnityEngine;
using System;

using Object = UnityEngine.Object;
using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
using VoxSimPlatform.Core;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        public class VoxemeInit : MonoBehaviour {
        	Predicates preds;
        	ObjectSelector objSelector;

        	public bool usePhysicsRigging;

        	// Use this for initialization
        	void Start() {
        		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
        		Macros macros = GameObject.Find("BehaviorController").GetComponent<Macros>();

        		InitializeVoxemes();

        		objSelector.InitDisabledObjects();
        		macros.PopulateMacros();
        	}

        	public void InitializeVoxemes() {
        		preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();

        		/* MAKE GLOBAL OBJECT RUNTIME ALTERATIONS */

        		// get all objects
        		GameObject[] allObjects = FindObjectsOfType<GameObject>();
        		Voxeme voxeme;

        		foreach (GameObject go in allObjects) {
        			if (go.activeInHierarchy) {
        				// set up all objects to enable consistent manipulation
        				// (i.e.) flatten any pos/rot inconsistencies in modeling or prefab setup due to human error
        				voxeme = go.GetComponent<Voxeme>();
        				Rigging rigging = go.GetComponent<Rigging>();
        				if ((voxeme != null) && (voxeme.enabled) && (rigging == null)) {
        					// object has Voxeme component and no Rigging
        					GameObject container = new GameObject(go.name, typeof(Voxeme), typeof(Rigging));

        					if (go.transform.root != go.transform) {
        						// not a top-level object
        						container.transform.parent = go.transform.parent;
        					}

        					container.transform.position = go.transform.position;
        					container.transform.rotation = go.transform.rotation;
        					go.transform.parent = container.transform;
        					go.name += "*";
        					voxeme.enabled = false;
        					//container.GetComponent<Entity> ().enabled = false;

        					container.GetComponent<Voxeme>().density = voxeme.density;

        					// copy attribute set
        					AttributeSet newAttrSet = container.AddComponent<AttributeSet>();
        					AttributeSet attrSet = go.GetComponent<AttributeSet>();
        					if (attrSet != null) {
        						foreach (string s in attrSet.attributes) {
        							newAttrSet.attributes.Add(s);
        						}
        					}

        					Debug.Log(newAttrSet.attributes.Count);

        					// copy interaction object
        					InteractionObject interactionObject = go.GetComponent<InteractionObject>();
        					if (interactionObject != null) {
        						// Set the object inactive to avoid InteractionObject initializing before attributes are set
        						Boolean containerState = container.activeInHierarchy;
        						container.SetActive(false);

        						CopyComponent(interactionObject, container);

        						container.SetActive(containerState);
        					}

        					Destroy(interactionObject);

        					// copy interaction target(s)
        					InteractionTarget[] interactionTargets = go.GetComponentsInChildren<InteractionTarget>();
        					foreach (InteractionTarget interactionTarget in interactionTargets) {
        						interactionTarget.gameObject.transform.parent = container.transform;
        						container.GetComponent<Voxeme>().interactionTargets.Add(interactionTarget);
        					}

        					FixHandRotation[] fixHandRotations = go.GetComponents<FixHandRotation>();
        					foreach (FixHandRotation fixHandRotation in fixHandRotations) {
        //						Debug.Log (fixHandRotation);
        //						Debug.Log (fixHandRotation.rootJoint);
        						CopyComponent(fixHandRotation, container);
        						Destroy(fixHandRotation);
        					}

        					// set up for physics
        					// add box colliders and rigid bodies to all subobjects that have MeshFilters
        					Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        					foreach (Renderer renderer in renderers) {
        						GameObject subObj = renderer.gameObject;
        						if (subObj.GetComponent<MeshFilter>() != null) {
        							if (go.tag != "UnPhysic") {
        								if (subObj.GetComponent<BoxCollider>() == null) {
        									// may already have one -- goddamn overachieving scene artists
        									BoxCollider collider = subObj.AddComponent<BoxCollider>();
        									//Physics.IgnoreCollision (collider, GameObject.Find ("MainCamera").GetComponent<Collider> ());
        								}
        							}

        							if ((go.tag != "UnPhysic") && (go.tag != "Ground")) {
        								// Non-physics objects are either scene markers or, like the ground, cognitively immobile
        								Debug.Log(subObj.name);

        								if (container.GetComponent<Voxeme>().density == 0) {
        									container.GetComponent<Voxeme>().density = 1;
        								}

        								if (subObj.GetComponent<Rigidbody>() == null) {
        									// may already have one -- goddamn overachieving scene artists
        									Rigidbody rigidbody = subObj.AddComponent<Rigidbody>();
        									if (rigidbody != null) {
        										// assume mass is a volume of uniform density
        										// assumption: all objects have the same density
        										float x = Helper.GetObjectWorldSize(subObj).size.x;
        										float y = Helper.GetObjectWorldSize(subObj).size.y;
        										float z = Helper.GetObjectWorldSize(subObj).size.z;
        										rigidbody.mass = x * y * z * (container.GetComponent<Voxeme>().density);

        										// bunch of crap assumptions to calculate drag:
        										// air density: 1.225 kg/m^3
        										// flow velocity = parent voxeme moveSpeed
        										// use box collider surface area for reference area
        										// use Reynolds number for drag coefficient - assume 1
        										// https://en.wikipedia.org/wiki/Drag_coefficient
        										rigidbody.drag =
        											1.225f * voxeme.moveSpeed * ((2 * x * y) + (2 * y * z) + (2 * x * z)) *
        											1.0f;
        										//rigidbody.drag = 0f;
        										//rigidbody.angularDrag = 0f;

        										//rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        										// get subobject initial rotations
        										//Debug.Log(rigidbody.name);
        										//Debug.Log(rigidbody.rotation.eulerAngles);
        										//container.GetComponent<Voxeme> ().startEventRotations.Add(rigidbody.name,rigidbody.rotation.eulerAngles);
        									}

        									// log the orientational displacement of each rigidbody relative to the main body
        									// relativeDisplacement = rotation to get from main body rotation to rigidbody rotation
        									// = rigidbody rotation * (main body rotation)^-1
        									Vector3 displacement =
        										rigidbody.transform.localPosition; //-container.transform.position;
        									Vector3 rotationalDisplacement =
        										rigidbody.transform
        											.localEulerAngles; //(rigidbody.transform.localRotation * Quaternion.Inverse (container.transform.rotation)).eulerAngles;
        									//Debug.Log(rotationalDisplacement);
        									//Debug.Log(rigidbody.name);
        									container.GetComponent<Voxeme>().displacement
        										.Add(rigidbody.gameObject, displacement);
        									container.GetComponent<Voxeme>().rotationalDisplacement
        										.Add(rigidbody.gameObject, rotationalDisplacement);
        								}
        							}
        						}
        					}

        					if (!usePhysicsRigging) {
        						container.GetComponent<Rigging>().ActivatePhysics(false);
        					}

        					// add to master voxeme list
        					objSelector.allVoxemes.Add(container.GetComponent<Voxeme>());
        					Debug.Log(Helper.VectorToParsable(container.transform.position -
        					                                  Helper.GetObjectWorldSize(container).center));
        				}
        			}
        		}

        		// set joint links between all subobjects (Cartesian product)
        		foreach (GameObject go in allObjects) {
        			if (go.activeInHierarchy) {
        				// remove BoxCollider and Rigidbody on non-top level objects
        				if ((Helper.GetMostImmediateParentVoxeme(go).gameObject.transform.parent != null) &&
        				    (go.transform.root.tag != "Agent")) {
        					BoxCollider boxCollider = go.GetComponent<BoxCollider>();
        					if (boxCollider != null) {
        						Destroy(boxCollider);
        					}

        					Rigidbody rigidbody = go.GetComponent<Rigidbody>();
        					if (rigidbody != null) {
        						Destroy(rigidbody);
        					}
        				}

        				Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        				foreach (Renderer r1 in renderers) {
        					GameObject sub1 = r1.gameObject;
        					foreach (Renderer r2 in renderers) {
        						GameObject sub2 = r2.gameObject;
        						if (sub1 != sub2) {
        							// can't connect body to itself
        							// add connections between all bodies EXCEPT:
        							//  if the connectedBody is on a GameObject that has a Voxeme component AND IS NOT the top-level voxeme
        							Rigidbody connectedBody = sub2.GetComponent<Rigidbody>();

        							if (connectedBody != null) {
        								if ((Helper.GetMostImmediateParentVoxeme(sub1).gameObject.transform.parent == null) &&
        								    (Helper.GetMostImmediateParentVoxeme(connectedBody.gameObject).gameObject.transform
        									     .parent == null)) {
        									FixedJoint fixedJoint = sub1.AddComponent<FixedJoint>();
        									fixedJoint.connectedBody = connectedBody;
        								}
        							}
        						}
        					}
        				}
        			}
        		}
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	T CopyComponent<T>(T original, GameObject destination) where T : Component {
        		Type type = original.GetType();
        		//var dst = destination.GetComponent(type) as T;
        		//if (!dst) {
        		var dst = destination.AddComponent(type) as T;
        		//}


        		var fields = type.GetFields();
        		foreach (var field in fields) {
        			if (field.IsStatic) continue;
        			field.SetValue(dst, field.GetValue(original));
        		}

        		var props = type.GetProperties();
        		foreach (var prop in props) {
        			if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
        			prop.SetValue(dst, prop.GetValue(original, null), null);
        		}

        		return dst as T;
        	}
        }
    }
}