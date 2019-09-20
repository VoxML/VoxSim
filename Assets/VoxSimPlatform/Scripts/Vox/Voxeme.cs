using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using MajorAxes;
using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        public class Voxeme : MonoBehaviour {
        	[HideInInspector] public VoxML voxml = new VoxML();

        	public OperationalVox opVox;

        	public float density;

        	// rotation information for each subobject's rigidbody
        	// (physics-resultant changes between the completion of one event and the start of the next must be brought into line)
        	//public Dictionary<string,Vector3> startEventRotations = new Dictionary<string, Vector3> ();
        	//public Dictionary<string,Vector3> endEventRotations = new Dictionary<string, Vector3> ();
        	public Dictionary<GameObject, Vector3> displacement = new Dictionary<GameObject, Vector3>();
        	public Dictionary<GameObject, Vector3> rotationalDisplacement = new Dictionary<GameObject, Vector3>();

        	Rigging rigging;

        	public GameObject graspConvention = null;

        	public List<InteractionTarget> interactionTargets = new List<InteractionTarget>();

            public LinkedList<Vector3> interTargetPositions = new LinkedList<Vector3>();
        	//public Queue<Vector3> interTargetPositions = new Queue<Vector3>();

            [SerializeField]
            private Vector3 _targetPosition;
        	public Vector3 targetPosition {
        		get { return _targetPosition; }
        		set {
                    if (_targetPosition != value) {
            			OnTargetPositionChanged(_targetPosition, value);
                    }
        			_targetPosition = value;
        		}
        	}

            public LinkedList<Vector3> interTargetRotations = new LinkedList<Vector3>();
        	//public Queue<Vector3> interTargetRotations = new Queue<Vector3>();

            [SerializeField]
            private Vector3 _targetRotation;
        	public Vector3 targetRotation {
                get { return _targetRotation; }
                set {
                    if (_targetRotation != value) {
                        OnTargetRotationChanged(_targetRotation, value);
                    }
                    _targetRotation = value;
                }
            }

            [SerializeField]
            private Vector3 _targetScale;
            public Vector3 targetScale {
                get { return _targetScale; }
                set {
                    if (_targetScale != value) {
                        OnTargetScaleChanged(_targetScale, value);
                    }
                    _targetScale = value;
                }
            }

        	public float moveSpeed = 1.0f;
        	public float turnSpeed = 5.0f;
        	public float defaultMoveSpeed = 0.0f;
        	public float defaultTurnSpeed = 0.0f;

        	public float minYBound;
        	//	public float minYBound {
        	//		get { return _minYBound; }
        	//		set {
        	//				if (value != _minYBound) {
        	//					Debug.Break ();
        	//				}
        	//				_minYBound = value;
        	//			}
        	//	}

        	public GameObject supportingSurface = null;
        //	public GameObject supportingSurface {
        //		get { return _supportingSurface; }
        //		set {
        //				if (value != _supportingSurface) {
        //					Debug.Break ();
        //				}
        //				_supportingSurface = value;
        //			}
        //	}

        	public bool isGrasped = false;
        	public Transform graspTracker = null;
        	public Transform grasperCoord = null;

        	public Voxeme[] children;
        	public Dictionary<GameObject, Vector3> parentToChildPositionOffset;
        	public Dictionary<GameObject, Quaternion> parentToChildRotationOffset;

        	public Vector3 startPosition;
        	public Vector3 startRotation;
        	public Vector3 startScale;

        	public event EventHandler VoxMLLoaded;

        	public void OnVoxMLLoaded(object sender, EventArgs e) {
        		if (VoxMLLoaded != null) {
        			VoxMLLoaded(this, e);
        		}
        	}

        	// Use this for initialization
        	void Start() {
        		// load in VoxML knowledge
        //		TextAsset markup = Resources.Load (gameObject.name) as TextAsset;
        //		if (markup != null) {
        //			voxml = VoxML.LoadFromText (markup.text);
        //		}

        //		WWW www = new WWW ("file:///" + Application.dataPath.Remove (Application.dataPath.LastIndexOf ('/') + 1) + 
        //			string.Format("Data/voxml/objects/{0}.xml",gameObject.name));
        //
        //		yield return www;

        //		voxml = VoxML.LoadFromText (www.text);

        		LoadVoxML();

        		// get movement blocking
        		minYBound = Helper.GetObjectWorldSize(gameObject).min.y;
        		//Debug.Log (minYBound);

        		// get rigging components
        		rigging = gameObject.GetComponent<Rigging>();
        		if (rigging != null) {
        			rigging.rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
        		}

        		startPosition = transform.position;
        		startRotation = transform.eulerAngles;
        		startScale = transform.localScale;

        		targetPosition = transform.position;
        		targetRotation = transform.eulerAngles;
        		targetScale = transform.localScale;

        //		moveSpeed = defaultMoveSpeed;
        //		turnSpeed = defaultTurnSpeed;

        		parentToChildPositionOffset = new Dictionary<GameObject, Vector3>();
        		parentToChildRotationOffset = new Dictionary<GameObject, Quaternion>();
        		children = GetComponentsInChildren<Voxeme>();
        		foreach (Voxeme child in children) {
        			if (child.isActiveAndEnabled) {
        				if (child.gameObject != gameObject) {
        					//Debug.Log (child.transform);
        					parentToChildPositionOffset[child.gameObject] = child.transform.localPosition;
        					parentToChildRotationOffset[child.gameObject] = child.transform.localRotation;
        					//Debug.Log (parentToChildPositionOffset [child.gameObject]);
        				}
        			}
        		}

        		Debug.Log(gameObject);
        		Debug.Log(Helper.VectorToParsable(Helper.GetObjectWorldSize(gameObject).min));
        		Debug.Log(Helper.VectorToParsable(Helper.GetObjectWorldSize(gameObject).max));
        	}

        	// Update is called once per frame
        	void Update() {
        		if (interTargetPositions.Count == 0) {
        			// no queued path
        			if (!Helper.VectorIsNaN(targetPosition)) {
        				// has valid destination
        				if (!isGrasped) {
        					if (transform.position != targetPosition) {
        						Vector3 offset = MoveToward(targetPosition);

        						//if (offset.sqrMagnitude <= Constants.EPSILON) {
        						//	transform.position = targetPosition;

        						//	foreach (Voxeme child in children) {
        						//		if (child.isActiveAndEnabled) {
        						//			if (child.gameObject != gameObject) {
        						//				child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
        						//				child.targetPosition = child.transform.position;
        						//			}
        						//		}
        						//	}
        						//}
        					}
        				}
        				else {
        					GraspScript graspController = grasperCoord.root.gameObject.GetComponent<GraspScript>();
        					if (graspTracker.transform.position != targetPosition + graspController.graspTrackerOffset) {
        						Vector3 offset = MoveToward(targetPosition + graspController.graspTrackerOffset);

        						//if (offset.sqrMagnitude <= Constants.EPSILON) {
        						//	graspTracker.transform.position = targetPosition + graspController.graspTrackerOffset;
        						//}
        					}
        				}
        			}
        			else {
        				// cannot execute motion
        				OutputHelper.PrintOutput(Role.Affector, "I'm sorry, I can't do that.");
        				GameObject.Find("BehaviorController").GetComponent<EventManager>().SendMessage("AbortEvent");
        				targetPosition = transform.position;
        			}
        		}
        		else {
        			Vector3 interimTarget = interTargetPositions.ElementAt(0);
        			if (!isGrasped) {
        				//if (transform.position != interimTarget) {
        				Vector3 offset = MoveToward(interimTarget);

        				if (offset.sqrMagnitude <= Constants.EPSILON) {
                            //transform.position = interimTarget;

                            //foreach (Voxeme child in children) {
                            //	if (child.isActiveAndEnabled) {
                            //		if (child.gameObject != gameObject) {
                            //			child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
                            //			child.targetPosition = child.transform.position;
                            //		}
                            //	}
                            //}

                            Debug.Log(string.Format("Removing {0} from {1}.interTargetPositions", Helper.VectorToParsable(interimTarget), gameObject.name));
        					interTargetPositions.RemoveFirst();
        				}

        				//}
        			}
        			else {
        				GraspScript graspController = grasperCoord.root.gameObject.GetComponent<GraspScript>();
        				//if (graspTracker.transform.position != interimTarget+graspController.graspTrackerOffset) {
        				Vector3 offset = MoveToward(interimTarget + graspController.graspTrackerOffset);

        				if (offset.sqrMagnitude <= Constants.EPSILON) {
        					//graspTracker.transform.position = interimTarget; //+graspController.graspTrackerOffset;
        					interTargetPositions.RemoveFirst();
        				}

        				//}
        			}
        		}

        		if (interTargetRotations.Count == 0) {
        			// no queued sequence
        			if (!Helper.VectorIsNaN(targetRotation)) {
        				// has valid target
        				if (!isGrasped) {
        					if (transform.rotation != Quaternion.Euler(targetRotation)) {
        						//Debug.Log (transform.eulerAngles);
        						float offset = RotateToward(targetRotation);

        						if ((Mathf.Deg2Rad * offset) < 0.01f) {
        							transform.rotation = Quaternion.Euler(targetRotation);

        //							foreach (Rigidbody rigidbody in rigging.rigidbodies) {
        //								if (rotationalDisplacement.ContainsKey (rigidbody.gameObject)) {
        //									rigidbody.transform.localEulerAngles = rotationalDisplacement [rigidbody.gameObject];
        //								}
        //							}
        						}
        					}
        				}
        				else {
        					// grasp tracking
        				}
        			}
        			else {
        				// cannot execute motion
        				OutputHelper.PrintOutput(Role.Affector, "I'm sorry, I can't do that.");
        				GameObject.Find("BehaviorController").GetComponent<EventManager>().SendMessage("AbortEvent");
        				targetRotation = transform.eulerAngles;
        			}
        		}
        		else {
        			Vector3 interimTarget = interTargetRotations.ElementAt(0);
        			if (!isGrasped) {
        				if (transform.rotation != Quaternion.Euler(interimTarget)) {
        					//Debug.Log (transform.rotation == Quaternion.Euler (targetRotation));
        					float offset = RotateToward(interimTarget);
        					//Debug.Log (offset);
        					//Debug.Log (Quaternion.Angle(transform.rotation,Quaternion.Euler (interimTarget)));
        					//if ((Mathf.Deg2Rad * Quaternion.Angle (transform.rotation, Quaternion.Euler (interimTarget))) < 0.01f) {
        					if ((Mathf.Deg2Rad * offset) < 0.01f) {
        						transform.rotation = Quaternion.Euler(interimTarget);

        						//Debug.Log (interimTarget);
        						interTargetRotations.RemoveFirst();
        						//Debug.Log (interTargetRotations.Peek ());
        					}
        				}
        			}
        			else {
        				// grasp tracking
        			}
        		}

        		if ((transform.localScale != targetScale) && (!isGrasped)) {
        			Vector3 offset = transform.localScale - targetScale;
        			Vector3 normalizedOffset = Vector3.Normalize(offset);

        			transform.localScale = new Vector3(transform.localScale.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        				transform.localScale.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        				transform.localScale.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

        			if (offset.sqrMagnitude <= 0.01f) {
        				transform.localScale = targetScale;
        			}
        		}

        		// Don't let the object sink below supporting surface
        		AdjustToSupportingSurface();

        		if (rigging != null) {
        			if (rigging.usePhysicsRig) {
        				return;
        			}
        		}

        		if (supportingSurface != null) {
        			//Debug.Log (supportingSurface.name);
        			// add check for SupportingSurface component
        			Bounds surfaceBounds = Helper.GetObjectWorldSize(supportingSurface);
        			Bounds objectBounds = Helper.GetObjectWorldSize(gameObject);
        //			Renderer[] renderers = supportingSurface.GetComponentsInChildren<Renderer> ();
        //			Bounds surfaceBounds = new Bounds ();
        //			foreach (Renderer renderer in renderers) {
        //				if (renderer.bounds.max.y > surfaceBounds.max.y) {
        //					surfaceBounds = renderer.bounds;
        //				}
        //			}

        //			Vector3 currentMin = gameObject.transform.position;
        //			renderers = gameObject.GetComponentsInChildren<Renderer> ();
        //			Bounds objectBounds = new Bounds ();
        //			foreach (Renderer renderer in renderers) {
        //				if (renderer.bounds.max.y > objectBounds.max.y) {
        //					objectBounds = renderer.bounds;
        //				}
        //
        //				if (renderer.bounds.min.y < currentMin.y) {
        //					currentMin = renderer.bounds.min;
        //				}
        //			}

        			if (Helper.IsTopmostVoxemeInHierarchy(gameObject)) {
        				if (objectBounds.min.y < minYBound) {
        					transform.position = new Vector3(transform.position.x,
        						transform.position.y + (minYBound - objectBounds.min.y),
        						transform.position.z);
        				}
        			}

        			/*if (supportingSurface.GetComponent<SupportingSurface> ().surfaceType == SupportingSurface.SupportingSurfaceType.Concave) {
        				/*if (objectBounds.min.y < surfaceBounds.min.y) {
        				transform.position = new Vector3 (transform.position.x,
        				                                  transform.position.y + (surfaceBounds.min.y - objectBounds.min.y),
        				                                  transform.position.z);
        			}*/
        			/*if (currentMin.y < surfaceBounds.min.y) {
        				transform.position = new Vector3 (transform.position.x,
        					transform.position.y + (surfaceBounds.min.y - currentMin.y),
        					transform.position.z);
        			}
        		} else {
        			/*if (objectBounds.min.y < surfaceBounds.max.y) {
        			transform.position = new Vector3 (transform.position.x,
        		                         transform.position.y + (surfaceBounds.max.y - objectBounds.min.y),
        		                         transform.position.z);
        		}*/
        			/*if (currentMin.y < surfaceBounds.max.y) {
        				transform.position = new Vector3 (transform.position.x,
        					transform.position.y + (surfaceBounds.max.y - currentMin.y),
        					transform.position.z);
        			}
        		}*/
        		}

        		// check relationships
        	}

        	void AdjustToSupportingSurface() {
        		Vector3 rayStartX = new Vector3(Helper.GetObjectWorldSize(gameObject).min.x - Constants.EPSILON,
        			Helper.GetObjectWorldSize(gameObject).min.y + Constants.EPSILON,
        			Helper.GetObjectWorldSize(gameObject).center.z);
        		Vector3 contactPointX = Helper.RayIntersectionPoint(rayStartX, Vector3.right);
        		//contactPointX = new Vector3 (contactPointX.x, transform.position.y, contactPointX.z);

        		Vector3 rayStartZ = new Vector3(Helper.GetObjectWorldSize(gameObject).center.x,
        			Helper.GetObjectWorldSize(gameObject).min.y + Constants.EPSILON,
        			Helper.GetObjectWorldSize(gameObject).min.z - Constants.EPSILON);
        		Vector3 contactPointZ = Helper.RayIntersectionPoint(rayStartZ, Vector3.forward);
        		//contactPointZ = new Vector3 (contactPointZ.x, transform.position.y, contactPointZ.z);

        		Vector3 contactPoint = (contactPointZ.y < contactPointX.y)
        			? new Vector3(contactPointZ.x, transform.position.y, contactPointZ.z)
        			: new Vector3(contactPointX.x, transform.position.y, contactPointX.z);

        		bool grasped = false;
        		InteractionObject interactionObject = gameObject.GetComponent<InteractionObject>();
        		if ((interactionObject != null) && (interactionObject.lastUsedInteractionSystem != null)) {
        			grasped = interactionObject.lastUsedInteractionSystem.IsPaused(FullBodyBipedEffector.LeftHand) ||
        			          interactionObject.lastUsedInteractionSystem.IsPaused(FullBodyBipedEffector.RightHand);
        		}

        		RaycastHit[] hits;

        		//		hits = Physics.RaycastAll (transform.position, AxisVector.negYAxis);
        		hits = Physics.RaycastAll(contactPoint, AxisVector.negYAxis);
        		List<RaycastHit> hitList = new List<RaycastHit>(hits);
        		hits = hitList.OrderBy(h => h.distance).ToArray();
        		foreach (RaycastHit hit in hits) {
        			if (hit.collider.gameObject.GetComponent<BoxCollider>() != null) {
        				if ((!hit.collider.gameObject.GetComponent<BoxCollider>().isTrigger) &&
        				    (!hit.collider.gameObject.transform.IsChildOf(gameObject.transform))) {
        					if (!Helper.FitsIn(Helper.GetObjectWorldSize(hit.collider.gameObject),
        						Helper.GetObjectWorldSize(gameObject), true)) {
        						supportingSurface = hit.collider.gameObject;

        						//if (!grasped) {
        						bool themeIsConcave = (Helper.GetMostImmediateParentVoxeme(gameObject)
        							.GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"));
        						bool themeIsUpright =
        							(Vector3.Dot(gameObject.transform.root.transform.up, Vector3.up) > 0.5f);
        						bool themeIsUpsideDown =
        							(Vector3.Dot(gameObject.transform.root.transform.up, Vector3.up) < -0.5f);

                                bool supportIsConcave = (Helper.GetMostImmediateParentVoxeme(supportingSurface)
        							.GetComponent<Voxeme>().voxml.Type.Concavity.Contains("Concave"));
        						bool supportIsUpright =
        							(Vector3.Dot(supportingSurface.transform.root.transform.up, Vector3.up) > 0.5f);
        						bool supportIsUpsideDown =
        							(Vector3.Dot(supportingSurface.transform.root.transform.up, Vector3.up) < -0.5f);

        						// if theme is concave, the concavity isn't enabled, and the object is on top of an object that fits inside of it
        						// e.g. cup on top of ball
        						if ((themeIsConcave) &&
        						    (Concavity.IsEnabled(Helper.GetMostImmediateParentVoxeme(gameObject))) &&
        						    (Helper.FitsIn(Helper.GetObjectWorldSize(supportingSurface.transform.root.gameObject),
        							    Helper.GetObjectWorldSize(gameObject)))) {
        							minYBound = Helper.GetObjectWorldSize(supportingSurface).min.y;
        							//flip the plate.  flip the cup.  put the plate under the cup
        							//flip the cup.  put the ball under the cup
        						}
        						else {
        							// otherwise
        							if (supportIsConcave) {
        								// if the object under this object is concave
        								if (Concavity.IsEnabled(Helper.GetMostImmediateParentVoxeme(supportingSurface))) {
        									// if the object under this object has its concavity enabled
        									minYBound = PhysicsHelper.GetConcavityMinimum(supportingSurface.transform.root
        										.gameObject);
        									//										Debug.Log (gameObject.name);
        									//										Debug.Log (supportingSurface.name);
        									//										Debug.Log (minYBound);
        									//Debug.Break ();
        								}
        								else {
        									// if the object under this object is not upright
        									//Debug.Break ();
        									minYBound = Helper.GetObjectWorldSize(supportingSurface).max.y;
        									//								Debug.Log (minYBound);
        									//Debug.Log (minYBound);
        								}
        							}
        							else {
        								// if the object under this object is not concave
        								minYBound = Helper.GetObjectWorldSize(supportingSurface).max.y;
        								//							Debug.Log (minYBound);
        								//Debug.Break ();
        							}

        							//**
        							//Bug list:
        							// put the plate under the cup jitter
        						}

        						break;
        					}
        					//}
        				}
        			}
        		}
        	}

        	public void Reset() {
        		if (gameObject.transform.parent != null) {
        			GameObject parent = gameObject.transform.parent.gameObject;
        			Voxeme parentVox = Helper.GetMostImmediateParentVoxeme(parent).GetComponent<Voxeme>();

        			// if this voxeme is not (intentionally) a subcomponent of another voxeme object
        			if (!(parentVox.opVox.Type.Components.Select(i => i.Item2).ToList()).Contains(gameObject)) {
        				RiggingHelper.UnRig(gameObject, gameObject.transform.parent.gameObject);
        			}
        		}

        		moveSpeed = defaultMoveSpeed;
        		turnSpeed = defaultTurnSpeed;

        		transform.position = startPosition;
        		transform.eulerAngles = startRotation;
        		transform.localScale = startScale;

        		targetPosition = startPosition;
        		targetRotation = startRotation;
        		targetScale = startScale;
        	}

        	public Vector3 MoveToward(Vector3 target) {
        		Debug.Log (string.Format("{0}: at {1}, moving toward {2}",gameObject.name,
                    Helper.VectorToParsable(transform.position),Helper.VectorToParsable(target)));
        		if (!isGrasped) {
        			Vector3 offset = transform.position - target;
        			Vector3 normalizedOffset = Vector3.Normalize(offset);

                    Debug.Log (string.Format("{0}: {1}, {2}",gameObject.name,
                        Helper.VectorToParsable(offset),Helper.VectorToParsable(normalizedOffset)));

        			if (rigging.usePhysicsRig) {
        				Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
        				foreach (Rigidbody rigidbody in rigidbodies) {
        					rigidbody.MovePosition(new Vector3(
        						transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        						transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        						transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
        				}
        			}

        			transform.position = new Vector3(transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        				transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        				transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

                    Debug.Log (string.Format("offset.sqrMagnitude({0}) = {1}",gameObject.name,
                        offset.sqrMagnitude));

                    if (offset.sqrMagnitude <= Constants.EPSILON) {
                        transform.position = target;
                    }

        			foreach (Voxeme child in children) {
        				if (child.isActiveAndEnabled) {
        					if (child.gameObject != gameObject) {
        						//Debug.Log ("Moving child: " + gameObject.name);
        						child.transform.localPosition = parentToChildPositionOffset[child.gameObject];
        						child.targetPosition = child.transform.position;
        					}
        				}
                    }

                    Debug.Log (string.Format("{0}: {1}, {2}",gameObject.name,
                        Helper.VectorToParsable(transform.position), Helper.VectorToParsable(target)));

        			return offset;
        		}
        		else {
        			Vector3 offset = graspTracker.transform.position - target;
        			Vector3 normalizedOffset = Vector3.Normalize(offset);

        			/*if (rigging.usePhysicsRig) {
        				Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody> ();
        				foreach (Rigidbody rigidbody in rigidbodies) {
        					rigidbody.MovePosition (new Vector3 (transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        						transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        						transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
        				}
        			}*/

        			graspTracker.transform.position = new Vector3(
        				graspTracker.transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        				graspTracker.transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        				graspTracker.transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

        			return offset;
        		}
        	}

        	public float RotateToward(Vector3 target) {
        		float offset = 0.0f;
        		if (!isGrasped) {
        			//Quaternion offset = Quaternion.FromToRotation (transform.eulerAngles, targetRotation);
        			//Vector3 normalizedOffset = Vector3.Normalize (offset);

        			float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(target));
        			float timeToComplete = angle / turnSpeed;
        			float donePercentage = Mathf.Min(1.0f, Time.deltaTime / timeToComplete);
        			Quaternion rot = Quaternion.Slerp(transform.rotation, Quaternion.Euler(target), donePercentage * 100.0f);
        			//Debug.Log (turnSpeed);
        			//Quaternion resolve = Quaternion.identity;

        			if (rigging.usePhysicsRig) {
        				float displacementAngle = 360.0f;
        				Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody>();
        				foreach (Rigidbody rigidbody in rigidbodies) {
        					rigidbody.MoveRotation(rot);
        				}
        			}

        			transform.rotation = rot;
        			//GameObject.Find ("ReachObject").transform.position = transform.position;

        			foreach (Voxeme child in children) {
        				if (child.isActiveAndEnabled) {
        					if (child.gameObject != gameObject) {
        						child.transform.localRotation = parentToChildRotationOffset[child.gameObject];
        						child.transform.rotation = gameObject.transform.rotation * child.transform.localRotation;
        						child.targetRotation = child.transform.rotation.eulerAngles;
        						child.transform.localPosition = Helper.RotatePointAroundPivot(
        							parentToChildPositionOffset[child.gameObject],
        							Vector3.zero, gameObject.transform.eulerAngles);
        						child.transform.position = gameObject.transform.position + child.transform.localPosition;
        						child.targetPosition = child.transform.position;
        						//Debug.Log (child.name);
        						//Debug.Break ();
        						//Debug.Log (Helper.VectorToParsable(child.transform.localPosition));
        					}
        				}
        			}

        			offset = Quaternion.Angle(rot, Quaternion.Euler(target));
        			//Debug.Log (offset);
        		}
        		else {
        			//float offset = Quaternion.FromToRotation (transform.eulerAngles, targetRotation);//graspTracker.transform.position - target;
        			//Vector3 normalizedOffset = Vector3.Normalize (offset);

        			/*if (rigging.usePhysicsRig) {
        					Rigidbody[] rigidbodies = gameObject.GetComponentsInChildren<Rigidbody> ();
        					foreach (Rigidbody rigidbody in rigidbodies) {
        						rigidbody.MovePosition (new Vector3 (transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        							transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        							transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed));
        					}
        				}*/

        			/*graspTracker.transform.position = new Vector3 (graspTracker.transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        				graspTracker.transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        				graspTracker.transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);*/
        		}

        		return offset;
        	}

            /// <summary>
            /// Triggered when the gameObject's target position changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetPosition
            //      newVal -- new or current value of gameObject's targetPosition
        	void OnTargetPositionChanged(Vector3 oldVal, Vector3 newVal) {
        		Debug.Log(string.Format("==================== Target position changed ==================== {0}: {1}->{2}",
        			gameObject.name, Helper.VectorToParsable(oldVal), Helper.VectorToParsable(newVal)));
        	}

            /// <summary>
            /// Triggered when the gameObject's target rotation changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetRotation
            //      newVal -- new or current value of gameObject's targetRotation
            void OnTargetRotationChanged(Vector3 oldVal, Vector3 newVal) {
                Debug.Log(string.Format("==================== Target rotation changed ==================== {0}: {1}->{2}",
                    gameObject.name, Helper.VectorToParsable(oldVal), Helper.VectorToParsable(newVal)));
            }

            /// <summary>
            /// Triggered when the gameObject's target scale changes
            /// </summary>
            // IN: oldVal -- previous value of gameObject's targetScale
            //      newVal -- new or current value of gameObject's targetScale
            void OnTargetScaleChanged(Vector3 oldVal, Vector3 newVal) {
                Debug.Log(string.Format("==================== Target scale changed ==================== {0}: {1}->{2}",
                    gameObject.name, Helper.VectorToParsable(oldVal), Helper.VectorToParsable(newVal)));
            }

        	void OnCollisionEnter(Collision other) {
        		if (other.gameObject.tag == "MainCamera") {
        			return;
        		}
        	}

        	public void LoadVoxML() {
        		try {
        			using (StreamReader sr = new StreamReader(
        				string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("objects/{0}.xml", gameObject.name)))) {
        				voxml = VoxML.LoadFromText(sr.ReadToEnd(), gameObject.name);
        			}
        		}
        		catch (FileNotFoundException ex) {
        			voxml = new VoxML();
        			voxml.Entity.Type = VoxEntity.EntityType.Object;
        		}


        		// populate operational voxeme structure
        		PopulateOperationalVoxeme();
        	}

        	void PopulateOperationalVoxeme() {
        		opVox = new OperationalVox();

        		// set entity type
        		opVox.VoxemeType = voxml.Entity.Type;

        		// set lex
        		opVox.Lex.Pred = voxml.Lex.Pred;
        		//Debug.Log (opVox.Lex.Pred);

        		// set type info

        		// find component objects
        		foreach (VoxTypeComponent c in voxml.Type.Components) {
        			Regex operators = new Regex(@"[\*\+]");
        			string oper = String.Empty;
        			//Debug.Log (c.Value);
        			if (operators.Match(c.Value).Length > 0) {
        				oper = operators.Match(c.Value).Groups[0].ToString();
        				//Debug.Log (oper);
        			}

        			string[] s;
        			if (oper != string.Empty) {
        				s = c.Value.Remove(c.Value.IndexOf(oper)).Split('[');
        			}
        			else {
        				s = c.Value.Split('[');
        			}

        			if (oper == String.Empty) {
        				Transform obj = null;
        				int index = -1;
        				obj = gameObject.transform.Find(gameObject.name + "*/" + s[0]);
        				if (s.Length > 1) {
        					index = Helper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
        				}

        				if (obj != null) {
        					//Debug.Log (s[0]);
        					//Debug.Log (obj);
        					//Debug.Log (index);
        					opVox.Type.Components.Add(new Triple<string, GameObject, int>(s[0], obj.gameObject, index));
        				}
        			}
        			else if (oper == "+" || oper == "*") {
        				Transform subVox = gameObject.transform.Find(gameObject.name + "*/");
        				if (subVox != null) {
        					foreach (Transform child in subVox) {
        						if (child.name == s[0]) {
        							//Debug.Log (child.name);
        							int index = -1;
        							if (s.Length > 1) {
        								index = Helper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
        							}

        							opVox.Type.Components.Add(
        								new Triple<string, GameObject, int>(s[0], child.gameObject, index));
        						}
        					}
        				}
        			}
        		}

        		// set component as semantic head
        		string[] str = voxml.Type.Head.Split('[');
        		if (str.Length > 1) {
        			int i = Helper.StringToInt(str[1].Remove(str[1].IndexOf(']')));
        			if (opVox.Type.Components.FindIndex(c => c.Item3 == i) != -1) {
        				opVox.Type.Head = opVox.Type.Components.First(c => c.Item3 == i);
        			}
        			// if none, add entire game object as semantic head for voxeme
        			else {
        				opVox.Type.Head = new Triple<string, GameObject, int>(gameObject.name, gameObject, i);
        				opVox.Type.Components.Add(new Triple<string, GameObject, int>(gameObject.name, gameObject, i));
        			}
        		}
        		else {
        			opVox.Type.Head = new Triple<string, GameObject, int>(gameObject.name, gameObject, -1);
        			opVox.Type.Components.Add(new Triple<string, GameObject, int>(gameObject.name, gameObject, -1));
        		}

        		// set concavity info
        		string[] concavity = voxml.Type.Concavity.Split('[');
        		// if reentrancy index given
        		if (concavity.Length > 1) {
        			int index = Helper.StringToInt(concavity[1].Remove(concavity[1].IndexOf(']')));
        			if (opVox.Type.Components.FindIndex(c => c.Item3 == index) != -1) {
        				GameObject obj = opVox.Type.Components.Find(c => c.Item3 == index).Item2;
        				opVox.Type.Concavity = new Triple<string, GameObject, int>(concavity[0], obj, index);
        			}
        			// if none, add entire game object as concavity segment
        			else {
        				opVox.Type.Concavity =
        					new Triple<string, GameObject, int>(concavity[0], gameObject, opVox.Type.Head.Item3);
        			}
        		}
        		else {
        			opVox.Type.Concavity = new Triple<string, GameObject, int>(concavity[0], gameObject, opVox.Type.Head.Item3);
        		}
        //		foreach (string sym in rotsym) {
        //			opVox.Type.RotatSym.Add (sym);
        //		}

        		// set symmetry info
        		string[] rotsym = voxml.Type.RotatSym.Split(',');
        		foreach (string sym in rotsym) {
        			opVox.Type.RotatSym.Add(sym);
        		}

        		string[] reflsym = voxml.Type.ReflSym.Split(',');
        		foreach (string sym in reflsym) {
        			opVox.Type.ReflSym.Add(sym);
        		}

        		// set habitat info
        		foreach (VoxHabitatIntr ih in voxml.Habitat.Intrinsic) {
        			string[] s = ih.Name.Split('[');
        			int index = Helper.StringToInt(s[1].Remove(s[1].IndexOf(']')));
        			//Debug.Log(index);
        			//Debug.Log (s[0] + " = {" + ih.Value + "}");

        			if (!opVox.Habitat.IntrinsicHabitats.ContainsKey(index)) {
        				opVox.Habitat.IntrinsicHabitats.Add(index, new List<string>() {s[0] + " = {" + ih.Value + "}"});
        			}
        			else {
        				opVox.Habitat.IntrinsicHabitats[index].Add(s[0] + " = {" + ih.Value + "}");
        			}
        		}

        		foreach (VoxHabitatExtr eh in voxml.Habitat.Extrinsic) {
        			string[] s = eh.Name.Split('[');
        			int index = Convert.ToInt16(s[1].Remove(s[1].IndexOf(']')));
        			//Debug.Log(index);
        			//Debug.Log (s[0] + " = {" + ih.Value + "}");

        			if (!opVox.Habitat.ExtrinsicHabitats.ContainsKey(index)) {
        				opVox.Habitat.ExtrinsicHabitats.Add(index, new List<string>() {s[0] + " = {" + eh.Value + "}"});
        			}
        			else {
        				opVox.Habitat.ExtrinsicHabitats[index].Add(s[0] + " = {" + eh.Value + "}");
        			}
        		}

        		/*foreach (KeyValuePair<int,List<string>> kv in opVox.Habitat.IntrinsicHabitats) {
        					Debug.Log (kv.Key);
        					foreach (string s in kv.Value) {
        						Debug.Log (s);
        					}
        				}*/

        		// set affordance info
        		foreach (VoxAffordAffordance a in voxml.Afford_Str.Affordances) {
        			//Debug.Log (a.Formula);
        			Regex reentrancyForm = new Regex(@"\[[0-9]+\]");
        			Regex numericalForm = new Regex(@"[0-9]+");
        			string[] s = a.Formula.Split(new string[] {"->"}, StringSplitOptions.None);
        			string[] conditions = s[0].Split(new char[] {','}, 2);
        			MatchCollection reentrancies = reentrancyForm.Matches(s[1]);
        			string aff = "";
        			string cHabitat = "";
        			string cFormula = "";
        			string events = "";
        			string result = "";
        			cHabitat = conditions[0]; // split into habitat and non-habitat condition (if any)
        			cFormula = conditions.Length > 1
        				? conditions[1]
        				: ""; // split into habitat and non-habitat condition (if any)
        			int index = (cHabitat.Split('[').Length > 1)
        				? Helper.StringToInt(cHabitat.Split('[')[1].Remove(cHabitat.Split('[')[1].IndexOf(']')))
        				: 0;

        			//Debug.Log ("Habitat index: " + index.ToString ());
        			foreach (Match match in reentrancies) {
        				GroupCollection groups = match.Groups;
        				foreach (Group group in groups) {
        					aff = s[1].Replace(group.Value, group.Value.Trim(new char[] {'[', ']'}));
        				}
        			}

        			//if (cFormula != "") {
        			//	Debug.Log ("Formula: " + cFormula);
        			//}
        			//Debug.Log ("Affordance: " + aff);

        			events = aff.Split(']')[0].Trim('[');
        			MatchCollection numerical = numericalForm.Matches(events);
        			foreach (Match match in numerical) {
        				GroupCollection groups = match.Groups;
        				foreach (Group group in groups) {
        					events = events.Replace(group.Value, '[' + group.Value + ']');
        				}
        			}
        			//Debug.Log ("Events: " + events);

        			result = aff.Split(']')[1];
        			numerical = numericalForm.Matches(result);
        			foreach (Match match in numerical) {
        				GroupCollection groups = match.Groups;
        				foreach (Group group in groups) {
        					result = result.Replace(group.Value, '[' + group.Value + ']');
        				}
        			}
        			//Debug.Log ("Result: " + result);

        			Pair<string, string> affordance = new Pair<string, string>(events, result);
        			if (!opVox.Affordance.Affordances.ContainsKey(index)) {
        				opVox.Affordance.Affordances.Add(index,
        					new List<Pair<string, Pair<string, string>>>()
        						{new Pair<string, Pair<string, string>>(cFormula, affordance)});
        			}
        			else {
        				opVox.Affordance.Affordances[index].Add(new Pair<string, Pair<string, string>>(cFormula, affordance));
        			}
        		}

        		opVox.Embodiment.Scale = voxml.Embodiment.Scale;
        		opVox.Embodiment.Movable = voxml.Embodiment.Movable;

        		if (voxml.Entity.Type == VoxEntity.EntityType.Object) {
        			AttributeSet attrSet = gameObject.GetComponent<AttributeSet>();
        			if (attrSet != null) {
        				attrSet.attributes.Clear();
        				for (int i = 0; i < voxml.Attributes.Attrs.Count; i++) {
        					attrSet.attributes.Add(voxml.Attributes.Attrs[i].Value);
        					Debug.Log(attrSet.attributes[i]);
        				}
        			}
        		}

        		OnVoxMLLoaded(this, new VoxMLEventArgs(gameObject, voxml));

        #if UNITY_EDITOR
        		using (StreamWriter file =
        			new StreamWriter(gameObject.name + @".txt")) {
        			file.WriteLine("PRED");
        			file.WriteLine("{0,-20}", opVox.Lex.Pred);
        			file.WriteLine("\n");
        			file.WriteLine("TYPE");
        			file.WriteLine("COMPONENTS");
        			foreach (Triple<string, GameObject, int> component in opVox.Type.Components) {
        				file.Write(String.Format("{0,-20}{1,-20}{2,-20}{3,-20}{4,-20}\n",
        					"Name: " + component.Item1,
        					"\t",
        					"GameObject name: " + component.Item2.name,
        					"\t",
        					"Index: " + component.Item3));
        			}

        			file.WriteLine("CONCAVITY");
        			file.Write(String.Format("{0,-20}{1,-20}{2,-20}{3,-20}{4,-20}\n",
        				"Name: " + opVox.Type.Concavity.Item1,
        				"\t",
        				"GameObject name: " + opVox.Type.Concavity.Item2.name,
        				"\t",
        				"Index: " + opVox.Type.Concavity.Item3));
        			file.WriteLine("SYMMETRY");
        			file.Write("ROT\t");
        			foreach (string s in opVox.Type.RotatSym) {
        				file.Write(String.Format("{0}\t", s));
        			}

        			file.Write("REFL\t");
        			foreach (string s in opVox.Type.ReflSym) {
        				file.Write(String.Format("{0}\t", s));
        			}

        			file.WriteLine("\n");
        			file.WriteLine("HABITATS");
        			file.WriteLine("INTRINSIC");
        			foreach (KeyValuePair<int, List<string>> kv in opVox.Habitat.IntrinsicHabitats) {
        				file.Write("Index: " + kv.Key);
        				foreach (string formula in kv.Value) {
        					file.Write("\t\tFormula: " + formula + "\n");
        				}
        			}

        			file.WriteLine("EXTRINSIC");
        			foreach (KeyValuePair<int, List<string>> kv in opVox.Habitat.ExtrinsicHabitats) {
        				file.Write("Index: " + kv.Key);
        				foreach (string formula in kv.Value) {
        					file.Write("\t\tFormula: " + formula + "\n");
        				}
        			}

        			file.WriteLine("\n");
        			file.WriteLine("AFFORDANCES");
        			foreach (KeyValuePair<int, List<Pair<string, Pair<string, string>>>> kv in opVox.Affordance.Affordances) {
        				file.Write("Habitat index: " + kv.Key);
        				foreach (Pair<string, Pair<string, string>> affordance in kv.Value) {
        					file.Write("\t\tCondition: " + ((affordance.Item1 != "") ? affordance.Item1 : "None") +
        					           "\t\tEvents: " + affordance.Item2.Item1 + "\t\tResult: " +
        					           ((affordance.Item2.Item2 != "") ? affordance.Item2.Item2 : "None") + "\n");
        				}
        			}
        		}
        #endif
        	}
        }
    }
}