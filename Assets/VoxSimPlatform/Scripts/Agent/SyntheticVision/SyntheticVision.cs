using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Agent {
        namespace SyntheticVision {
        	public class SyntheticVision : MonoBehaviour {
        		private bool showFoV;

        		public bool ShowFoV {
        			get { return showFoV; }
        			set { showFoV = value; }
        		}

        		public JointGestureDemo world;
        		public GameObject agent;
        		public GameObject sensor;
        		public Transform attached;
        		public List<Voxeme> visibleObjects;

        		ObjectSelector objSelector;
        		InteractionPrefsModalWindow interactionPrefs;

        		Timer reactionTimer;
        		float reactionDelayInterval = 1000;

        		bool surprise = false;
        		VisionEventArgs surpriseArgs;

        		bool initVision = true;

        		public GameObject VisionCanvas;

        		void Start() {
        			gameObject.GetComponent<Camera>().targetTexture =
        				(RenderTexture) VisionCanvas.GetComponentInChildren<RawImage>().texture;
        			interactionPrefs = world.GetComponent<InteractionPrefsModalWindow>();
        			if (attached != null) {
        				gameObject.transform.SetParent(attached);
        			}

        			objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
        //			visibleObjects = new HashSet<Voxeme>();
        		}

        		void Update() {
        			if (agent == null) {
        				return;
        			}

        			ShowFoV = interactionPrefs.showSyntheticVision;
        			if (!ShowFoV) {
        				VisionCanvas.SetActive(false);
        			}
        			else {
        				VisionCanvas.SetActive(true);
        			}

        			foreach (Voxeme voxeme in objSelector.allVoxemes) {
        				//Debug.Log (voxeme);
        				if (IsVisible(voxeme.gameObject)) {
        					if (!visibleObjects.Contains(voxeme)) {
        						visibleObjects.Add(voxeme);
        						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
        					}
        				}
        				else {
        					if (visibleObjects.Contains(voxeme)) {
        						visibleObjects.Remove(voxeme);
        						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
        					}
        				}
        			}
        		}

        		public bool IsVisible(Voxeme voxeme) {
        			return visibleObjects.Contains(voxeme);
        		}

        		public bool IsVisible(GameObject obj) {
        			if (objSelector.disabledObjects.Contains(obj)) {
        				return false;
        			}

        			List<GameObject> excludeChildren = obj.GetComponentsInChildren<Renderer>().Where(
        				o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != obj)).Select(v => v.gameObject).ToList();
        			int visibility = GetVisibleVertices(Helper.GetObjectWorldSize(obj, excludeChildren), obj,
        				sensor.transform.position);
        			//Debug.Log(obj + "=============================================================== " + visibility);
        			return visibility > 0;
        		}

        		private int GetVisibleVertices(Bounds bounds, GameObject rotatedObj, Vector3 origin) {
        			float c = 1.0f;
        			List<Vector3> vertices = new List<Vector3> {
        				new Vector3(bounds.center.x - (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y - (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z - (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x - (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y - (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z + (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x - (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y + (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z - (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x - (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y + (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z + (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x + (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y - (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z - (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x + (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y - (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z + (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x + (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y + (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z - (bounds.extents.z + Constants.EPSILON) * c),
        				new Vector3(bounds.center.x + (bounds.extents.x + Constants.EPSILON) * c,
        					bounds.center.y + (bounds.extents.y + Constants.EPSILON) * c,
        					bounds.center.z + (bounds.extents.z + Constants.EPSILON) * c),
        			};

        			int numVisibleVertices = 0;
        			foreach (Vector3 vertex in vertices) {
        //            Quaternion rot = Helper.GetMostImmediateParentVoxeme(gameObject).transform.rotation;
        //				Vector3 rotatedVertex = Helper.GetMostImmediateParentVoxeme(rotatedObj).transform.rotation * vertex + rotatedObj.transform.position;
        				Vector3 rotatedVertex = vertex;
        				RaycastHit hitInfo;
        				bool hit = Physics.Raycast(
        					rotatedVertex, Vector3.Normalize(origin - rotatedVertex),
        					out hitInfo,
        					Vector3.Magnitude(origin - rotatedVertex));
        				bool visible = (!hit) || ((hitInfo.point - rotatedVertex).magnitude < Constants.EPSILON);
        //				if ((visible) || 
        //					(new Bounds(bounds.center,new Vector3(bounds.size.x+Constants.EPSILON,
        //						bounds.size.y+Constants.EPSILON,
        //						bounds.size.z+Constants.EPSILON)).Contains(hitInfo.point))) {
        				if (visible) {
        					//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}:{2}", obj.name, Helper.VectorToParsable (vertex), hitInfo.collider.name));
        					numVisibleVertices += Convert.ToInt32(visible);
        //					}
        				}
        				else {
        					//Debug.Log(string.Format("Ray from {0} collides with {1} at {2}",
        					//Helper.VectorToParsable(rotatedVertex),
        					//Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject),
        					//Helper.VectorToParsable(hitInfo.point)));
        				}
        			}

        			return numVisibleVertices;
        		}
        	}
        }
    }
}