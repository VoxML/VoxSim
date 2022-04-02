using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
//using RootMotion.FinalIK;
using VoxSimPlatform.Animation;
using VoxSimPlatform.Core;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        public class VoxemeInit : MonoBehaviour {
        	Predicates preds;
        	ObjectSelector objSelector;

            public List<Transform> topLevelObjectContainers; 
        	public bool usePhysicsRigging;

        	// Use this for initialization
        	void Start() {
        		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

        		InitializeVoxemes();

        		objSelector.InitDisabledObjects();
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
                            Debug.Log(string.Format("Initalizing object {0} as voxeme",go.name));
        					// object has Voxeme component and no Rigging
        					GameObject container = new GameObject(go.name, typeof(Voxeme), typeof(Rigging));

                            container.GetComponent<Voxeme>().defaultParent = go.transform.parent;

        					if (go.transform.root != go.transform) {
        						// not a top-level object
        						container.transform.parent = go.transform.parent;
        					}

                            container.transform.position = go.transform.position;
        					container.transform.rotation = go.transform.rotation;
        					go.transform.parent = container.transform;
        					go.name += "*";
                            voxeme.enabled = false;

                            container.GetComponent<Voxeme>().predicate = voxeme.predicate;
                            container.GetComponent<Voxeme>().density = voxeme.density;

        					// copy attribute set
        					AttributeSet newAttrSet = container.AddComponent<AttributeSet>();
        					AttributeSet attrSet = go.GetComponent<AttributeSet>();
        					if (attrSet != null) {
        						foreach (string s in attrSet.attributes) {
                                    Debug.Log(string.Format("Adding attribute {0} to object {1}", s, container.name));
        							newAttrSet.attributes.Add(s);
        						}
        					}

                            // copy interaction object
                            //InteractionObject interactionObject = go.GetComponent<InteractionObject>();
                            //if (interactionObject != null) {
                            //	// Set the object inactive to avoid InteractionObject initializing before attributes are set
                            //	Boolean containerState = container.activeInHierarchy;
                            //	container.SetActive(false);

                            //	CopyComponent(interactionObject, container);

                            //	container.SetActive(containerState);
                            //}

                            //Destroy(interactionObject);

                            //// copy interaction target(s)
                            //InteractionTarget[] interactionTargets = go.GetComponentsInChildren<InteractionTarget>();
                            //foreach (InteractionTarget interactionTarget in interactionTargets) {
                            //	interactionTarget.gameObject.transform.parent = container.transform;
                            //	container.GetComponent<Voxeme>().interactionTargets.Add(interactionTarget);
                            //}

                            //FixHandRotation[] fixHandRotations = go.GetComponents<FixHandRotation>();
                            //foreach (FixHandRotation fixHandRotation in fixHandRotations) {
                            //	CopyComponent(fixHandRotation, container);
                            //	Destroy(fixHandRotation);
                            //}

                            // set up for physics
                            //Debug.Log("Creating Rigidbody on " + container);
                            //Rigidbody voxRigidBody = container.AddComponent<Rigidbody>();
                            //voxRigidBody.mass = 0;
                            //voxRigidBody.drag = 0;

                            // add box colliders and rigid bodies to all subobjects that have MeshFilters
                            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        					foreach (Renderer renderer in renderers) {
        						GameObject subObj = renderer.gameObject;
        						if (subObj.GetComponent<MeshFilter>() != null) {
        							if (go.tag != "UnPhysic") {
        								if (subObj.GetComponent<Collider>() == null) {
        									// may already have a collider -- goddamn overachieving scene artists
        									BoxCollider collider = subObj.AddComponent<BoxCollider>();
        								}
        							}

        							if ((go.tag != "UnPhysic") && (go.tag != "Ground")) {
        								if (container.GetComponent<Voxeme>().density == 0) {
        									container.GetComponent<Voxeme>().density = 1;
        								}

        								if (subObj.GetComponent<Rigidbody>() == null) {
        									// may already have one -- goddamn overachieving scene artists
        									Rigidbody rb = subObj.AddComponent<Rigidbody>();
        									if (rb != null) {
        										// assume mass is a volume of uniform density
        										// assumption: all objects have the same density
        										float x = GlobalHelper.GetObjectWorldSize(subObj).size.x;
        										float y = GlobalHelper.GetObjectWorldSize(subObj).size.y;
        										float z = GlobalHelper.GetObjectWorldSize(subObj).size.z;
                                                rb.mass = x * y * z * (container.GetComponent<Voxeme>().density);
                                                //voxRigidBody.mass += rigidbody.mass;

                                                // bunch of crap assumptions to calculate drag:
                                                // air density: 1.225 kg/m^3
                                                // flow velocity = parent voxeme moveSpeed
                                                // use box collider surface area for reference area
                                                // use Reynolds number for drag coefficient - assume 1
                                                // https://en.wikipedia.org/wiki/Drag_coefficient
                                                rb.drag =
        											1.225f * voxeme.moveSpeed * ((2 * x * y) + (2 * y * z) + (2 * x * z)) *
        											1.0f;
                                                //voxRigidBody.drag += rigidbody.drag;
           									}

        									// log the orientational displacement of each rigidbody relative to the main body
        									// relativeDisplacement = rotation to get from main body rotation to rigidbody rotation
        									// = rigidbody rotation * (main body rotation)^-1
        									Vector3 displacement =
                                                rb.transform.localPosition; //-container.transform.position;
        									Vector3 rotationalDisplacement =
                                                rb.transform
        											.localEulerAngles; //(rigidbody.transform.localRotation * Quaternion.Inverse (container.transform.rotation)).eulerAngles;
        									//Debug.Log(rotationalDisplacement);
        									//Debug.Log(rigidbody.name);
        									container.GetComponent<Voxeme>().displacement
        										.Add(rb.gameObject, displacement);
        									container.GetComponent<Voxeme>().rotationalDisplacement
        										.Add(rb.gameObject, rotationalDisplacement);
                                        }
        							}
        						}
        					}

        					if (!usePhysicsRigging) {
        						container.GetComponent<Rigging>().ActivatePhysics(false);
        					}
                                
                            foreach(Transform t in go.transform) {
                                t.gameObject.tag = go.tag;
                            }
                            container.tag = go.tag;

        					// add to master voxeme list
        					objSelector.allVoxemes.Add(container.GetComponent<Voxeme>());
        					//Debug.Log(GlobalHelper.VectorToParsable(container.transform.position -
        					//                                  GlobalHelper.GetObjectWorldSize(container).center));
        				}
        			}
        		}


        		// set joint links between all subobjects (Cartesian product)
        		foreach (GameObject go in objSelector.allVoxemes.Select(v => v.gameObject).ToList()) {
                    if ((go.activeInHierarchy) && (go.GetComponent<Voxeme>() != null) &&
                        (go.GetComponent<Voxeme>().isActiveAndEnabled)) {
                        // remove BoxCollider and Rigidbody on non-top level objects
                        if ((GlobalHelper.GetMostImmediateParentVoxeme(go).gameObject.transform.parent != 
                            GlobalHelper.GetMostImmediateParentVoxeme(go).GetComponent<Voxeme>().defaultParent) &&
        				    (go.transform.root.tag != "Agent")) {
        					BoxCollider boxCollider = go.GetComponent<BoxCollider>();
        					if (boxCollider != null) {
        						Destroy(boxCollider);
        					}

        					Rigidbody rb = go.GetComponent<Rigidbody>();
        					if (rb != null) {
                                Debug.Log("Removing Rigidbody on " + go);
        						Destroy(rb);
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
                                        Transform subObjectParentContainer = GlobalHelper.GetMostImmediateParentVoxeme(sub1).gameObject.transform.parent;
                                        Transform connectedObjectParentContainer = GlobalHelper.GetMostImmediateParentVoxeme(connectedBody.gameObject).gameObject.transform.parent;
                                        if (((subObjectParentContainer == null) || (topLevelObjectContainers.Contains(subObjectParentContainer))) &&
        								    ((connectedObjectParentContainer == null) || (topLevelObjectContainers.Contains(connectedObjectParentContainer)))) {
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