using UnityEngine;
using System;
using System.Collections.Generic;

using VoxSimPlatform.Agent;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    public class IKTarget : MonoBehaviour {
    	// it's funny, it's like a voxeme but not
    	public Queue<Vector3> interTargetPositions = new Queue<Vector3>();
    	public Vector3 targetPosition;
    	public float moveSpeed = 2.0f;

    	public event EventHandler AtTarget;

    	public void OnAtTarget(object sender, EventArgs e) {
    		if (AtTarget != null) {
    			AtTarget(this, e);
    		}
    	}

    	// Use this for initialization
    	void Start() {
    		targetPosition = transform.position;
    	}

    	// Update is called once per frame
    	void Update() {
    		if (interTargetPositions.Count == 0) {
    			// no queued path
    			if (!Helper.VectorIsNaN(targetPosition)) {
    				// has valid destination
    				if (transform.position != targetPosition) {
    					Vector3 offset = MoveToward(targetPosition);

    					if (offset.sqrMagnitude <= 0.01f) {
    						transform.position = targetPosition;
    						OnAtTarget(this, EventArgs.Empty);
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
    			// has queued path
    			Vector3 interimTarget = interTargetPositions.Peek();
    			//Debug.Log (gameObject.name + " " + Helper.VectorToParsable(interimTarget));
    			if (transform.position != interimTarget) {
    				Vector3 offset = MoveToward(interimTarget);

    				if (offset.sqrMagnitude <= 0.001f) {
    					transform.position = interimTarget;
    					interTargetPositions.Dequeue();
    				}
    			}
    		}
    	}

    	Vector3 MoveToward(Vector3 target) {
    		Vector3 offset = transform.position - target;
    		Vector3 normalizedOffset = Vector3.Normalize(offset);

    		transform.position = new Vector3(transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
    			transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
    			transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

    		return offset;
    	}
    }
}