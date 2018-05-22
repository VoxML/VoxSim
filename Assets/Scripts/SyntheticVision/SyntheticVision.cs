using UnityEngine;
using System;
using System.Collections.Generic;
using System.Timers;

using Global;

namespace Agent
{
	public enum InconsistencyType
	{
		Missing,
		Present
	};


	public class VisionEventArgs : EventArgs {

		public Voxeme Voxeme { get; set; }
		public InconsistencyType Inconsistency { get; set; }

		public VisionEventArgs(Voxeme voxeme, InconsistencyType inconsistency)
		{
			Voxeme = voxeme;
			Inconsistency = inconsistency;
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

		private bool showFoV;
		public bool ShowFoV
		{
			get { return showFoV; }
			set { showFoV = value; }
		}

		public JointGestureDemo world;
		public GameObject agent;
		public EpistemicModel epistemicModel;
		public GameObject sensor;
		public List<Voxeme> visibleObjects;
		public List<Voxeme> knownObjects;
		public Dictionary<Voxeme,Bounds> knownObjectBounds = new Dictionary<Voxeme, Bounds>();

		ObjectSelector objSelector = null;
		InteractionPrefsModalWindow interactionPrefs;

		Timer reactionTimer;
		float reactionDelayInterval = 1000;

		bool surprise = false;
		VisionEventArgs surpriseArgs;

		bool initVision = true;

		void Start () {
			interactionPrefs = world.GetComponent<InteractionPrefsModalWindow> ();

			epistemicModel = agent.GetComponent<EpistemicModel> ();

			// Create reaction timer
			// Create a timer
			reactionTimer = new Timer();
			// Tell the timer what to do when it elapses
			reactionTimer.Elapsed += Surprise;
			// Set it to go off after interval
			reactionTimer.Interval = reactionDelayInterval;
			// Don't start it
			reactionTimer.Enabled = false;
		}

		// Update is called once per frame
		void Update () {
			if (agent == null) {
				return;
			}

			//if (objSelector == null) {
			objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
				//Debug.Log (objSelector);

			ShowFoV = interactionPrefs.showSyntheticVision;
			if (!ShowFoV) {
				gameObject.GetComponent<Camera>().enabled = false;
			}
			else {
				gameObject.GetComponent<Camera>().enabled = true;
			}

			foreach (Voxeme voxeme in objSelector.allVoxemes) {
				//Debug.Log (voxeme);
				if (IsVisible (voxeme.gameObject)) {
					if (!visibleObjects.Contains (voxeme)) {
						visibleObjects.Add (voxeme);
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
					}

					if (epistemicModel.engaged) {
						if (!knownObjects.Contains (voxeme)) {
							knownObjects.Add (voxeme);
							knownObjectBounds.Add (voxeme, Helper.GetObjectWorldSize(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject)));
							Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsKnown (voxeme.gameObject).ToString ()));

							if (!initVision) {	// don't do this when you initially populate knownObjects
								// but otherwise
								// surprise!
								surpriseArgs = new VisionEventArgs(voxeme,InconsistencyType.Present);
								Debug.Log(string.Format("{0} Surprise!",voxeme.ToString()));
								reactionTimer.Enabled = true;
							}
						}
						else {
							if (knownObjectBounds.ContainsKey (voxeme)) {
								knownObjectBounds [voxeme] = Helper.GetObjectWorldSize(Helper.GetMostImmediateParentVoxeme(voxeme.gameObject))	;
							}
						}
					}
				}
				else {
					if (visibleObjects.Contains (voxeme)) {
						visibleObjects.Remove (voxeme);
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}", voxeme.name, IsVisible (voxeme.gameObject).ToString ()));
					}
					else {	// if it's not visible
						if (knownObjects.Contains (voxeme)) {	// but I know about it
							if (IsVisible (knownObjectBounds [voxeme])) {	// and I know it should be here
								// surprise!
								knownObjects.Remove(voxeme);
								knownObjectBounds.Remove(voxeme);
								surpriseArgs = new VisionEventArgs(voxeme,InconsistencyType.Missing);
								Debug.Log(string.Format("{0} Surprise!",voxeme.ToString()));
								reactionTimer.Enabled = true;
							}
						}
					}
				}
			}

			if (surprise) {
				NewInformation (surpriseArgs);
				surprise = false;
			}

			if ((knownObjects.Count > 0) && (initVision)) {
				initVision = false;
			}
		}

		public bool IsVisible(Bounds bounds)
		{
			return GetVisibleVetices(bounds, sensor.transform.position) > 0;
		}

		public bool IsVisible(GameObject obj)
		{
			if (objSelector.disabledObjects.Contains(obj))
			{
				return false;
			}
			return IsVisible(Helper.GetObjectWorldSize(obj));
		}

		private int GetVisibleVetices(Bounds bounds, Vector3 origin)
		{
			float c = 1.0f;
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

			int numVisibleVertices = 0;
			foreach (Vector3 vertex in vertices) {
				RaycastHit hitInfo;
				bool visible = !Physics.Raycast (
					vertex, Vector3.Normalize (origin - vertex),
					out hitInfo,
					Vector3.Magnitude (origin - vertex));
				if (visible) {
//					if ((Helper.GetMostImmediateParentVoxeme (hitInfo.collider.gameObject) != obj) && (hitInfo.collider.gameObject != obj)) {
						//Debug.Log (string.Format ("SyntheticVision.Update:{0}:{1}:{2}", obj.name, Helper.VectorToParsable (vertex), hitInfo.collider.name));
						numVisibleVertices += Convert.ToInt32(visible);
//					}
				}
			}

			return numVisibleVertices;
		}

		public bool IsKnown(GameObject obj) {
			return knownObjects.Contains (obj.GetComponent<Voxeme>());
		}

		public void Surprise(object source, ElapsedEventArgs e) {
			reactionTimer.Interval = reactionDelayInterval;
			reactionTimer.Enabled = false;

			surprise = true;
		}

		public void NewInformation(VisionEventArgs e) {
			string color = e.Voxeme.voxml.Attributes.Attrs [0].Value;	// just grab the first one for now

			if (e.Inconsistency == InconsistencyType.Missing) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("Holy cow!  What happened to the {0} block?", color));
			}
			else if (e.Inconsistency == InconsistencyType.Present) {
				OutputHelper.PrintOutput (Role.Affector, string.Format ("I didn't know that {0} block was there!", color));
			}
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

