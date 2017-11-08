using UnityEngine;
using System;
using System.Collections;
using System.Timers;

using Global;

public class PhysicsPrimitives : MonoBehaviour {

	bool resolveDiscrepancies;
	EventManager eventManager;

	const double PHYSICS_CATCHUP_TIME = 100.0;
	Timer catchupTimer;

	bool macroEventSatisfied;

	// Use this for initialization
	void Start () {
		eventManager = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();

		resolveDiscrepancies = false;

		catchupTimer = new Timer (PHYSICS_CATCHUP_TIME);
		catchupTimer.Enabled = false;
		catchupTimer.Elapsed += Resolve;

		eventManager.EventComplete += EventSatisfied;
	}
	
	// Update is called once per frame
	void Update () {
	}

	void LateUpdate() {
		//if (Input.GetKeyDown (KeyCode.R)) {
			if (resolveDiscrepancies) {
				//Debug.Log ("resolving");
				PhysicsHelper.ResolveAllPhysicsDiscrepancies (macroEventSatisfied);
				//Debug.Break ();
				if (eventManager.events.Count > 0) {
					catchupTimer.Interval = 1;
				}
			}
		//}
	}

	void EventSatisfied(object sender, EventArgs e) {
		Debug.Log ("Satisfaction received");
		resolveDiscrepancies = true;
		catchupTimer.Enabled = true;
		macroEventSatisfied = ((EventManagerArgs)e).MacroEvent;
	}

	void Resolve(object sender, ElapsedEventArgs e) {
		catchupTimer.Enabled = false;
		catchupTimer.Interval = PHYSICS_CATCHUP_TIME;
		resolveDiscrepancies = false;
	}
}
