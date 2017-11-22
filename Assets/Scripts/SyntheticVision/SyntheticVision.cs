using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using Global;

namespace Agent
{
	public class VisionEventArgs : EventArgs {

		public GameObject Object { get; set; }

		public VisionEventArgs(GameObject obj)
		{
			this.Object = obj;
		}
	}

	public class SyntheticVision : MonoBehaviour {

		public event EventHandler React;

		public void OnReact(object sender, VisionEventArgs e)
		{
			if (React != null)
			{
				React(this, e);
			}
		}

		public GameObject sensor;
		public List<Voxeme> visibleObjects;
		public List<Voxeme> knownObjects;

		ObjectSelector objSelector = null;

		Timer reactionTimer;
		float reactionDelayInterval = 1000;

		void Start () {
			// Create reaction timer
			// Create a timer
//			reactionTimer = new Timer();
//			// Tell the timer what to do when it elapses
//			reactionTimer.Elapsed += CheckIfStillVisible;
//			// Set it to go off after interval
//			reactionTimer.Interval = reactionDelayInterval;
//			// Don't start it        
//			reactionTimer.Enabled = false;
		}

		// Update is called once per frame
		void Update () {
			//if (objSelector == null) {
			objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
				//Debug.Log (objSelector);
			foreach (Voxeme voxeme in objSelector.allVoxemes) {
				//Debug.Log (voxeme);
				if (IsVisible (voxeme.gameObject)) {
					if (!visibleObjects.Contains (voxeme)) {
						visibleObjects.Add (voxeme);
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
					}

					if (gameObject.GetComponent<EpistemicModel> ().engaged) {
						if (!knownObjects.Contains (voxeme)) {
							knownObjects.Add (voxeme);
						}
					}
				}
				else {
					if (visibleObjects.Contains (voxeme)) {
						visibleObjects.Remove (voxeme);
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
					}
				}
			}
		}

		public bool IsVisible(GameObject obj) {
			bool r = false;

			Bounds bounds = Helper.GetObjectWorldSize (obj);

			float c = 1.0f;

			List<Vector3> vertices = new List<Vector3> () {
				new Vector3 (bounds.center.x+((bounds.min.x-bounds.center.x)*c), bounds.center.y+((bounds.min.y-bounds.center.y)*c), bounds.center.z+((bounds.min.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.min.x-bounds.center.x)*c), bounds.center.y+((bounds.min.y-bounds.center.y)*c), bounds.center.z+((bounds.max.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.min.x-bounds.center.x)*c), bounds.center.y+((bounds.max.y-bounds.center.y)*c), bounds.center.z+((bounds.min.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.min.x-bounds.center.x)*c), bounds.center.y+((bounds.max.y-bounds.center.y)*c), bounds.center.z+((bounds.max.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.max.x-bounds.center.x)*c), bounds.center.y+((bounds.min.y-bounds.center.y)*c), bounds.center.z+((bounds.min.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.max.x-bounds.center.x)*c), bounds.center.y+((bounds.min.y-bounds.center.y)*c), bounds.center.z+((bounds.max.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.max.x-bounds.center.x)*c), bounds.center.y+((bounds.max.y-bounds.center.y)*c), bounds.center.z+((bounds.min.z-bounds.center.z)*c)),
				new Vector3 (bounds.center.x+((bounds.max.x-bounds.center.x)*c), bounds.center.y+((bounds.max.y-bounds.center.y)*c), bounds.center.z+((bounds.max.z-bounds.center.z)*c)),
			};

			int numHits = 0;
			foreach (Vector3 vertex in vertices) {
				RaycastHit hitInfo;
				bool hit = Physics.Raycast (vertex, Vector3.Normalize (sensor.transform.position - vertex), out hitInfo,
					Vector3.Magnitude (sensor.transform.position - vertex));
				if (hit) {
					if ((Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject) != obj) && (hitInfo.collider.gameObject != obj)) {
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}:{2}", obj.name, Helper.VectorToParsable (vertex), hitInfo.collider.name));
						numHits += System.Convert.ToInt32 (hit);
					}
				}
			}

			return (numHits < 2);
		}

		public void NewInformation() {
			OutputHelper.PrintOutput (Role.Affector, "Wow, I didn't see that before!");
		}

//		public void NewInformation(object content) {
//			reactionTimer.Enabled = true;
//		}
//
//		private void CheckIfStillVisible(object source, ElapsedEventArgs e) {
//			PrintResponse();
//			reactionTimer.Interval = reactionDelayInterval;
//			reactionTimer.Enabled = false;
//		}
//
//		void PrintResponse() {
//			OutputHelper.PrintOutput (Role.Affector, "Holy shit!");
//		}
	}
}

