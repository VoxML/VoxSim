using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace CogPhysics {
        public class PhysicsPrimitives : MonoBehaviour {
        	bool _resolveDiscrepancies;
            public bool resolveDiscrepancies {
                get { return _resolveDiscrepancies; }
                set {
                    if (_resolveDiscrepancies != value) {
                        OnResolveDiscrepanciesChanged(_resolveDiscrepancies, value);
                    }
                    _resolveDiscrepancies = value;
                }
            }
        	EventManager eventManager;

        	const double PHYSICS_CATCHUP_TIME = 100.0;
        	Timer catchupTimer;

        	EventManagerArgs testSatisfied;

        	// Use this for initialization
        	void Start() {
        		eventManager = GameObject.Find("BehaviorController").GetComponent<EventManager>();

        		resolveDiscrepancies = false;

        		catchupTimer = new Timer(PHYSICS_CATCHUP_TIME);
        		catchupTimer.Enabled = false;
        		catchupTimer.Elapsed += Resolve;

        		eventManager.EventComplete += EventSatisfied;
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	void LateUpdate() {
        		//if (Input.GetKeyDown (KeyCode.R)) {
        		if (resolveDiscrepancies) {
        			Debug.Log ("Resolving discrepancies between physics bodies and voxemes");
        			PhysicsHelper.ResolveAllPhysicsDiscrepancies(testSatisfied.MacroEvent);
        			//Debug.Break ();
        			if (eventManager.events.Count > 0) {
        				catchupTimer.Interval = 1;
        			}

        			Hashtable predArgs = Helper.ParsePredicate(testSatisfied.EventString);
        			String predString = "";
        			String[] argsStrings = null;

        			foreach (DictionaryEntry entry in predArgs) {
        				predString = (String) entry.Key;
        				argsStrings = ((String) entry.Value).Split(new char[] {','});
        			}

                    // if any object in argsStrings VoxML contains affordances with event predString
                    //  reason the consequences of those affordances
                    foreach (String argString in argsStrings) {
                        // find the GameObject of this name
                        GameObject argObj = GameObject.Find(argString);
                        Debug.Log(string.Format("Found {0}game object {1}", (argObj == null) ? "no " : string.Empty, argString));
                        if (argObj != null) {
                            // get its Voxeme component
                            Voxeme argVox = argObj.GetComponent<Voxeme>();
                            if (argVox != null) {
                                // find all affordances in argVox.voxml.Afford_Str (that is, in the object's affordance structure)
                                //  that contain predString in the event E (not the result R) -- viz. affordance encoding format H->[E]R
                                // Regex matches [predString(...)]
                                Regex r = new Regex("\\["+predString+"\\(.+\\)\\]");
                                // if there's >0 affordances in argObj's affordance structure that match predString
                                // reason the consequences of those affordances
                                Debug.Log(string.Format("Completed event {0}",predString));
                                if (argVox.voxml.Afford_Str.Affordances.Where(a => r.IsMatch(a.Formula)).ToList().Count > 0) {
                                    Debug.Log(string.Format("Reasoning from affordances over {0}({1})", predString, argVox));
                                    SatisfactionTest.ReasonFromAffordances(eventManager, testSatisfied.VoxML, predString, argVox);
                                }
                            }
                        }
                    }

        			// TODO: better than this
        			// which predicates result in affordance-based consequence?
        			if ((predString == "lift") ||
        			    (predString == "turn") || (predString == "roll") ||
        			    (predString == "slide") || (predString == "put")) {
        				SatisfactionTest.ReasonFromAffordances(eventManager, null, predString,
        					GameObject.Find(argsStrings[0] as String)
        						.GetComponent<Voxeme>()); // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
        			}
        		}

        		//}
        	}

        	void EventSatisfied(object sender, EventArgs e) {
                testSatisfied = (EventManagerArgs)e;

                // if there are events in the event history and the last one is not a "while" predicate
                //  OR
                // if there is more than one event in the event queue and the next one up is not a "while" predicate
                if (((eventManager.eventHistory.Count > 0) &&
                    (Helper.GetTopPredicate(eventManager.eventHistory.Last()) != "while")) || 
                    ((eventManager.events.Count > 1) && (Helper.GetTopPredicate(eventManager.events[1]) != "while"))) {
            		resolveDiscrepancies = true;
                    catchupTimer.Enabled = true;
                }

                if (eventManager.events.Count > 0) {
                    eventManager.stayExecution = true;
                }
                else {
                    eventManager.stayExecution = false;
                }

                Debug.Log(string.Format("Satisfaction condition met for {0}specification {1}",
                    (testSatisfied.VoxML != null) ? testSatisfied.VoxML.Lex.Pred + " " : string.Empty,
                    testSatisfied.EventString));
        	}

        	void Resolve(object sender, ElapsedEventArgs e) {
                Debug.Log ("Catching up.  Next frame, event manager can run the full update loop again.");
        		catchupTimer.Enabled = false;
        		catchupTimer.Interval = PHYSICS_CATCHUP_TIME;
        		resolveDiscrepancies = false;
                //eventManager.stayExecution = false;
        	}

            /// <summary>
            /// Triggered when the resolveDiscrepancies flag changes
            /// </summary>
            // IN: oldVal -- previous value of resolveDiscrepancies flag
            //      newVal -- new or current value of resolveDiscrepancies flag
            void OnResolveDiscrepanciesChanged(bool oldVal, bool newVal) {
                Debug.Log(string.Format("==================== resolveDiscrepancies flag changed ==================== {0}->{1}",
                    oldVal, newVal));
            }
        }
    }
}