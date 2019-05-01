using UnityEngine;

using System;

namespace VoxSimPlatform {
    namespace Agent {
        public class GraspScript : MonoBehaviour {
        	private Animator anim;
        	public int grasper;
        	public Transform leftGrasperCoord, rightGrasperCoord;
        	public Transform leftFingerCoord, rightFingerCoord;
        	public bool isGrasping = false;
        	public Vector3 graspTrackerOffset; // = new Vector3(0.0f,0.0f,0.0f);
        	public Vector3 fingertipOffset;
        	public Vector3 leftDefaultPosition, rightDefaultPosition;
        	public Vector3 leftPerformPosition, rightPerformPosition;


        	// Use this for initialization
        	void Start() {
        		anim = GetComponentInChildren<Animator>();
        	}

        	// Update is called once per frame
        	void Update() {
        		/*if (Input.GetKeyDown (KeyCode.Alpha1)) {
        			grasper = 1;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Alpha2)) {
        			grasper = 2;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Alpha3)) {
        			grasper = 3;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Alpha4)) {
        			grasper = 4;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Alpha5)) {
        			grasper = 5;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Alpha6)) {
        			grasper = 6;
        			Debug.Log (grasper);
        		} else if (Input.GetKeyDown (KeyCode.Space)) {
        			grasper = 0;
        			Debug.Log (grasper);
        		}*/
        		//anim.SetInteger ("anim", grasper);
        	}

        	void UpdateGraspStatus(int complete) {
        		//if (isGrasping != System.Convert.ToBoolean(complete)) {
        		//	Debug.Log (string.Format("Setting to {0}", complete));
        		//	Debug.Break ();
        		//}
        		isGrasping = Convert.ToBoolean(complete);
        	}
        }
    }
}