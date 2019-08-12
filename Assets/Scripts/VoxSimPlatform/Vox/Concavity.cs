using UnityEngine;
using System.Collections.Generic;

using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning.QSR;

namespace VoxSimPlatform {
    namespace Vox {
        public static class Concavity {
        	public static bool IsEnabled(GameObject obj) {
        		bool enabled = true;

        		//Ray ray = new Ray (obj.transform.position, obj.transform.rotation * Vector3.up);		// => get concavity vector from VoxML structure
        		Vector3 rayStart = new Vector3(Helper.GetObjectWorldSize(obj).center.x,
        			Helper.GetObjectWorldSize(obj).max.y,
        			Helper.GetObjectWorldSize(obj).center.z);
        		//rayStart = Helper.RotatePointAroundPivot (rayStart, obj.transform.position, obj.transform.eulerAngles);
        		Ray ray = new Ray(rayStart, obj.transform.up);
        		RaycastHit hitInfo;
        		bool hit = Physics.Raycast(ray, out hitInfo);
        		if (hit) {
        			GameObject
        				hitObj = hitInfo.collider
        					.gameObject; // if there's an object in the direction of the concavity's opening
        			//Debug.Log ("Ray collide: " + hitObj);
        			while (hitObj.GetComponent<Rigging>() == null) {
        				// get first parent to have rigging component (= voxeme root)
        				if (hitObj.transform.parent != null) {
        					hitObj = Helper.GetMostImmediateParentVoxeme(hitObj).gameObject;
        				}
        				else {
        					hitObj = null;
        					break;
        				}
        			}

        			if (hitObj != null) {
        				//Debug.Log ("Ray collide: " + hitObj);
        				Bounds objBounds = Helper.GetObjectWorldSize(obj);
        				Bounds hitObjBounds = Helper.GetObjectWorldSize(hitObj);
        				if ((hitObj.transform.IsChildOf(obj.transform)) || (Helper.FitsIn(hitObjBounds, objBounds))) {
        					//Debug.Log (hitObj.name + " is child of " + obj.name);
        					//Debug.Break ();
        					Transform[] children = hitObj.GetComponentsInChildren<Transform>();
        					List<GameObject> toExclude = new List<GameObject>();
        					foreach (Transform transform in children) {
        						toExclude.Add(transform.gameObject);
        					}

        					objBounds = Helper.GetObjectWorldSize(obj, toExclude);
        				}
        				else {
        					objBounds = Helper.GetObjectWorldSize(obj);
        				}

        				//if (RCC8.EC (hitObjBounds, objBounds) || RCC8.PO (hitObjBounds, objBounds)) {
        				if (RCC8.EC(hitObjBounds, objBounds)) {
        					enabled = false;
        				}
        				else if ((RCC8.PO(hitObjBounds, objBounds)) && (!Helper.FitsIn(hitObjBounds, objBounds))) {
        					enabled = false;
        				}
        			}
        		}

        		return enabled;
        	}

        	public static bool IsEnabled(GameObject obj, out GameObject disablingObject) {
        		bool enabled = true;
        		disablingObject = null;

        		Ray ray = new Ray(obj.transform.position,
        			obj.transform.rotation * Vector3.up); // => get concavity vector from VoxML structure
        		RaycastHit hitInfo;
        		bool hit = Physics.Raycast(ray, out hitInfo);
        		if (hit) {
        			GameObject
        				hitObj = hitInfo.collider
        					.gameObject; // if there's an object in the direction of the concavity's opening
        			//Debug.Log ("Ray collide: " + hitObj);
        			while (hitObj.GetComponent<Rigging>() == null) {
        				// get first parent to have rigging component (= voxeme root)
        				if (hitObj.transform.parent != null) {
        					hitObj = Helper.GetMostImmediateParentVoxeme(hitObj).gameObject;
        				}
        				else {
        					hitObj = null;
        					break;
        				}
        			}

        			if (hitObj != null) {
        				//Debug.Log ("Ray collide: " + hitObj);
        				Bounds objBounds = Helper.GetObjectWorldSize(obj);
        				Bounds hitObjBounds = Helper.GetObjectWorldSize(hitObj);
        				if ((hitObj.transform.IsChildOf(obj.transform)) || (Helper.FitsIn(hitObjBounds, objBounds))) {
        					Debug.Log(hitObj.name + " is child of " + obj.name);
        					Transform[] children = hitObj.GetComponentsInChildren<Transform>();
        					List<GameObject> toExclude = new List<GameObject>();
        					foreach (Transform transform in children) {
        						toExclude.Add(transform.gameObject);
        					}

        					objBounds = Helper.GetObjectWorldSize(obj, toExclude);
        				}
        				else {
        					objBounds = Helper.GetObjectWorldSize(obj);
        				}

        				//if (RCC8.EC (hitObjBounds, objBounds) || RCC8.PO (hitObjBounds, objBounds)) {
        				if (RCC8.EC(hitObjBounds, objBounds)) {
        					enabled = false;
        				}
        				else if ((RCC8.PO(hitObjBounds, objBounds)) && (!Helper.FitsIn(hitObjBounds, objBounds))) {
        					enabled = false;
        				}

        				if (hitObj.GetComponent<Voxeme>() != null) {
        					disablingObject = hitObj;
        				}
        			}
        		}

        		return enabled;
        	}

        	public static bool IsEnabled(GameObject obj, Vector3 position, out GameObject disablingObject) {
        		bool enabled = true;
        		disablingObject = null;

        		Ray ray = new Ray(position,
        			obj.transform.rotation * Vector3.up); // => get concavity vector from VoxML structure
        		RaycastHit hitInfo;
        		bool hit = Physics.Raycast(ray, out hitInfo);
        		if (hit) {
        			GameObject
        				hitObj = hitInfo.collider
        					.gameObject; // if there's an object in the direction of the concavity's opening
        			//Debug.Log ("Ray collide: " + hitObj);
        			while (hitObj.GetComponent<Rigging>() == null) {
        				// get first parent to have rigging component (= voxeme root)
        				if (hitObj.transform.parent != null) {
        					hitObj = Helper.GetMostImmediateParentVoxeme(hitObj).gameObject;
        				}
        				else {
        					hitObj = null;
        					break;
        				}
        			}

        			if (hitObj != null) {
        				//Debug.Log ("Ray collide: " + hitObj);
        				Bounds objBounds = Helper.GetObjectWorldSize(obj);
        				Bounds hitObjBounds = Helper.GetObjectWorldSize(hitObj);
        				if ((hitObj.transform.IsChildOf(obj.transform)) || (Helper.FitsIn(hitObjBounds, objBounds))) {
        					Debug.Log(hitObj.name + " is child of " + obj.name);
        					Transform[] children = hitObj.GetComponentsInChildren<Transform>();
        					List<GameObject> toExclude = new List<GameObject>();
        					foreach (Transform transform in children) {
        						toExclude.Add(transform.gameObject);
        					}

        					objBounds = Helper.GetObjectWorldSize(obj, toExclude);
        				}
        				else {
        					objBounds = Helper.GetObjectWorldSize(obj);
        				}

        				//if (RCC8.EC (hitObjBounds, objBounds) || RCC8.PO (hitObjBounds, objBounds)) {
        				if (RCC8.EC(hitObjBounds, objBounds)) {
        					enabled = false;
        				}
        				else if ((RCC8.PO(hitObjBounds, objBounds)) && (!Helper.FitsIn(hitObjBounds, objBounds))) {
        					enabled = false;
        				}

        				if (hitObj.GetComponent<Voxeme>() != null) {
        					disablingObject = hitObj;
        				}
        			}
        		}

        		return enabled;
        	}
        }
    }
}