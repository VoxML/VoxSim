using UnityEngine;
using System;
using System.Collections.Generic;
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

		public Camera sensor;
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


		public bool IsVisible(GameObject obj)
		{

			if (objSelector.disabledObjects.Contains(obj))
			{
				return false;
			}

			return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(sensor), Helper.GetObjectWorldSize(obj))
			       && GetBlockedVetices(obj, sensor.transform.position) < 2;
		}


		private int GetBlockedVetices(GameObject obj, Vector3 origin)
		{
			Bounds bounds = Helper.GetObjectWorldSize(obj);
            float c = 0.99f;

			List<Vector3> vertices = new List<Vector3> {
				new Vector3(bounds.center.x - bounds.extents.x*c, bounds.center.y - bounds.extents.y*c, bounds.center.z - bounds.extents.z*c),
				new Vector3(bounds.center.x - bounds.extents.x*c, bounds.center.y - bounds.extents.y*c, bounds.center.z + bounds.extents.z*c),
				new Vector3(bounds.center.x - bounds.extents.x*c, bounds.center.y + bounds.extents.y*c, bounds.center.z - bounds.extents.z*c),
				new Vector3(bounds.center.x - bounds.extents.x*c, bounds.center.y + bounds.extents.y*c, bounds.center.z + bounds.extents.z*c),
				new Vector3(bounds.center.x + bounds.extents.x*c, bounds.center.y - bounds.extents.y*c, bounds.center.z - bounds.extents.z*c),
				new Vector3(bounds.center.x + bounds.extents.x*c, bounds.center.y - bounds.extents.y*c, bounds.center.z + bounds.extents.z*c),
				new Vector3(bounds.center.x + bounds.extents.x*c, bounds.center.y + bounds.extents.y*c, bounds.center.z - bounds.extents.z*c),
				new Vector3(bounds.center.x + bounds.extents.x*c, bounds.center.y + bounds.extents.y*c, bounds.center.z + bounds.extents.z*c),
			};

			int numBlocked = 0;
			foreach (Vector3 vertex in vertices) {
				RaycastHit hitInfo;
				bool hit = Physics.Raycast(
					vertex, Vector3.Normalize(origin - vertex),
					out hitInfo,
					Vector3.Magnitude (origin - vertex));
				if (hit && hitInfo.collider.gameObject != obj
					&& Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject) != obj)
				{
					//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}:{2}", obj.name, Helper.VectorToParsable (vertex), hitInfo.collider.name));
					numBlocked += 1;
				}
			}
			return numBlocked;
		}

		public bool IsKnown(GameObject obj) {
			return knownObjects.Contains (obj.GetComponent<Voxeme>());
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