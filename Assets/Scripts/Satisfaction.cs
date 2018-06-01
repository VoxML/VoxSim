using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Agent;
using Global;
using MajorAxes;
using RCC;
using RootMotion.FinalIK;

namespace Satisfaction {
	public static class SatisfactionTest {
		public static bool IsSatisfied (String test) {
			bool satisfied = false;
			Hashtable predArgs = Helper.ParsePredicate (test);
			String predString = "";
			String[] argsStrings = null;

			PhysicsPrimitives physicsManager = GameObject.Find ("BehaviorController").GetComponent<PhysicsPrimitives> ();
			Predicates preds = GameObject.Find ("BehaviorController").GetComponent<Predicates> ();
			EventManager em = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();

			bool isMacroEvent = false;

//			if (em.isInitiatePhase) {
//				return false;
//			}

			foreach (DictionaryEntry entry in predArgs) {
				predString = (String)entry.Key;
				argsStrings = ((String)entry.Value).Split (new char[] {','});
			}

			//Debug.Log (test);

			if (predString == "put") {	// satisfy put
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					//Debug.Log(Helper.VectorToParsable(theme.transform.position) + " " + (String)argsStrings[1]);
					//Debug.Log(obj.transform.position);
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					//Debug.Log (voxComponent);
					Vector3 testLocation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.position : theme.transform.position;

					if (Helper.CloseEnough (testLocation, Helper.ParsableToVector ((String)argsStrings [1]))) {
						if (voxComponent.isGrasped) {
							//preds.UNGRASP (new object[]{ theme, true });
							//em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
							theme.transform.position = Helper.ParsableToVector ((String)argsStrings [1]);
							theme.transform.rotation = Quaternion.identity;
						}
						satisfied = true;
						//obj.GetComponent<Rigging> ().ActivatePhysics (true);
						//ReevaluateRelationships (predString, theme);	// we need to talk (do physics reactivation in here?)
//						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
					}
				}
			}
			else if (predString == "slide") {	// satisfy slide
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					//Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
					//Debug.Log(obj.transform.position);
					//Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					Vector3 testLocation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.position : theme.transform.position;

					if (Helper.CloseEnough (testLocation, Helper.ParsableToVector ((String)argsStrings [1]))) {
						if (voxComponent.isGrasped) {
							//preds.UNGRASP (new object[]{ theme, true });
							//em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
							theme.transform.position = Helper.ParsableToVector ((String)argsStrings [1]);
							theme.transform.rotation = Quaternion.identity;
						}
						satisfied = true;
//						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
						//theme.GetComponent<Rigging> ().ActivatePhysics (true);
					}
				}
			}
			else if (predString == "roll") {	// satisfy roll
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					//Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
					//Debug.Log(obj.transform.position);
					//Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					Vector3 testLocation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.position : theme.transform.position;

					if (argsStrings.Length > 1) {
						if (Helper.CloseEnough (testLocation, Helper.ParsableToVector ((String)argsStrings [1]))) {
							if (voxComponent.isGrasped) {
								//preds.UNGRASP (new object[]{ theme, true });
								//em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
								theme.transform.position = Helper.ParsableToVector ((String)argsStrings [1]);
								theme.transform.rotation = Quaternion.identity;
							}
							satisfied = true;
//							ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
							//theme.GetComponent<Rigging> ().ActivatePhysics (true);
						}
					}
				}
			}
			else if (predString == "turn") {	// satisfy turn
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					//Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
					//Debug.Log(obj.transform.position);
					//Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					Vector3 testRotation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.eulerAngles : theme.transform.eulerAngles;

					//Debug.DrawRay(theme.transform.position, theme.transform.up * 5, Color.blue, 0.01f);
					//Debug.Log(Vector3.Angle (theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1]), Helper.ParsableToVector ((String)argsStrings [2])));
					//Debug.Log(Helper.VectorToParsable(theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1])));
					//Debug.Log(Helper.ParsableToVector ((String)argsStrings [2]));
					if (Mathf.Deg2Rad * Vector3.Angle (theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1]), Helper.ParsableToVector ((String)argsStrings [2])) < Constants.EPSILON) {
						if (voxComponent.isGrasped) {
							//theme.transform.rotation = Quaternion.Euler(Helper.ParsableToVector ((String)argsStrings [1]));
							//theme.transform.rotation = Quaternion.identity;
						}
						satisfied = true;
						//Debug.Break ();

						//bar;
						// ROLL once - roll again - voxeme object satisfied TURN but rigidbody subobjects have moved under physics 
//						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
						//theme.GetComponent<Rigging> ().ActivatePhysics (true);
					}
				}
			}
			/*else if (predString == "spin") {	// satisfy spin
				/GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					Vector3 testRotation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.eulerAngles : theme.transform.eulerAngles;

					Debug.Log (Vector3.Angle (theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1]), Helper.ParsableToVector ((String)argsStrings [2])));
					//Debug.Log (Helper.VectorToParsable(theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1])));
					//Debug.Log ((String)argsStrings [2]);
					//Debug.Break ();
					if (Mathf.Deg2Rad * Vector3.Angle (theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1]), Helper.ParsableToVector ((String)argsStrings [2])) < Constants.EPSILON) {
						if (voxComponent.isGrasped) {
							//theme.transform.rotation = Quaternion.Euler(Helper.ParsableToVector ((String)argsStrings [1]));
							//theme.transform.rotation = Quaternion.identity;
						}
						satisfied = true;

						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
					}
				}
			}*/
//			else if (predString == "flip") {	// satisfy flip
//				GameObject theme = GameObject.Find (argsStrings [0] as String);
//				if (theme != null) {
//					//Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
//					//Debug.Log(obj.transform.position);
//					//Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
//					if (Helper.CloseEnough(theme.transform.rotation, Quaternion.Euler (Helper.ParsableToVector ((String)argsStrings [1])))) {
//						satisfied = true;
//						ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
//						theme.GetComponent<Rigging> ().ActivatePhysics (true);
//					}
//				}
//			}
			else if (predString == "lift") {	// satisfy lift
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				if (theme != null) {
					//Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
					//Debug.Log(obj.transform.position);
					//Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
					Voxeme voxComponent = theme.GetComponent<Voxeme>();
					Vector3 testLocation = voxComponent.isGrasped ? voxComponent.graspTracker.transform.position : theme.transform.position;
					//Vector3 testLocation = theme.transform.position;

					if (voxComponent.isGrasped) {
						if (Helper.CloseEnough (testLocation, Helper.ParsableToVector ((String)argsStrings [1])+
							voxComponent.grasperCoord.root.gameObject.GetComponent<GraspScript> ().graspTrackerOffset)) {
							theme.transform.position = Helper.ParsableToVector ((String)argsStrings [1]);//+
							//voxComponent.grasperCoord.root.gameObject.GetComponent<GraspScript> ().graspTrackerOffset;
							theme.transform.rotation = Quaternion.identity;
						}
						satisfied = true;
						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
					}
					else if (Helper.CloseEnough (testLocation, Helper.ParsableToVector ((String)argsStrings [1]))) {
						satisfied = true;
//						ReasonFromAffordances (predString, voxComponent);	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
						//theme.GetComponent<Rigging> ().ActivatePhysics (true);
					}
				}
			}
			else if (predString == "bind") {	// satisfy bind
				satisfied = true;
			}
			else if (predString == "wait") {	// satisfy wait
				if (!preds.waitTimer.Enabled) {
					satisfied = true;
				}
			}
			else if (predString == "reach") {	// satisfy reach
				GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
				GraspScript graspController = agent.GetComponent<GraspScript> ();
				//Debug.Log (graspController.isGrasping);
				//if (graspController.isGrasping) {
				//	satisfied = true;
				//}
				//Debug.Log (string.Format ("Reach {0}", satisfied));
			}
			else if (predString == "grasp") {	// satisfy grasp
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				GameObject agent = GameObject.FindGameObjectWithTag ("Agent");

				if ((agent.GetComponent<InteractionSystem> ().IsPaused (FullBodyBipedEffector.LeftHand)) ||
					(agent.GetComponent<InteractionSystem> ().IsPaused (FullBodyBipedEffector.RightHand))) {
					foreach (FixHandRotation handRot in theme.GetComponentsInChildren<FixHandRotation>()) {
						handRot.enabled = false;
					}
					satisfied = true;
				}
//				if (theme != null) {
//					if (agent != null) {
//						if (theme.transform.IsChildOf (agent.transform)) {
//							satisfied = true;
//						}
//					}
//				}

			}
			else if (predString == "hold") {	// satisfy hold
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
				if (theme != null) {
					if (agent != null) {
						if (theme.transform.IsChildOf (agent.transform)) {
							satisfied = true;
						}
					}
				}
			}
			else if (predString == "ungrasp") {	// satisfy ungrasp
				GameObject theme = GameObject.Find (argsStrings [0] as String);
				GameObject agent = GameObject.FindGameObjectWithTag ("Agent");

				if ((!agent.GetComponent<InteractionSystem> ().IsPaused (FullBodyBipedEffector.LeftHand)) ||
					(!agent.GetComponent<InteractionSystem> ().IsPaused (FullBodyBipedEffector.RightHand))) {
					foreach (FixHandRotation handRot in theme.GetComponentsInChildren<FixHandRotation>()) {
						handRot.enabled = true;
					}
					satisfied = true;
//					ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
				}

//				GameObject theme = GameObject.Find (argsStrings [0] as String);
//				GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
//				GraspScript graspController = agent.GetComponent<GraspScript> ();
//				if (theme != null) {
//					if (agent != null) {
//						if (!theme.transform.IsChildOf (agent.transform)) {
//							if (!theme.GetComponent<Voxeme>().isGrasped) {
//								//Debug.Break ();
//								satisfied = true;
//								ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
//							}
//						}
//					}
//				}
			}
#pragma mark MacroEvents
			else if (predString == "lean") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "flip") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "spin") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "switch") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "stack") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "close") {
				isMacroEvent = true;
				satisfied = true;
			}
			else if (predString == "open") {
				isMacroEvent = true;
				satisfied = true;
			}
			else {
				satisfied = true;
			}
				
			if (satisfied) {
				EventManagerArgs eventArgs = new EventManagerArgs (test, isMacroEvent);
				em.OnEventComplete(em, eventArgs);
//				ReasonFromAffordances (predString, GameObject.Find (argsStrings [0] as String).GetComponent<Voxeme>());	// we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
			}

			return satisfied;
		}

		public static bool ComputeSatisfactionConditions(String command) {
			Hashtable predArgs = Helper.ParsePredicate (command);
			String pred = Helper.GetTopPredicate (command);
			ObjectSelector objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
			EventManager em = GameObject.Find ("BehaviorController").GetComponent<EventManager> ();

			if (predArgs.Count > 0) {
				Queue<String> argsStrings = new Queue<String> (((String)predArgs [pred]).Split (new char[] { ',' }));
				List<object> objs = new List<object> ();
				MethodInfo methodToCall;
				Predicates preds = GameObject.Find ("BehaviorController").GetComponent<Predicates> ();

				while (argsStrings.Count > 0) {
					object arg = argsStrings.Dequeue ();
				
					if (Helper.v.IsMatch ((String)arg)) {	// if arg is vector form
						objs.Add (Helper.ParsableToVector ((String)arg));
					}
					else if (arg is String) {	// if arg is String
						if ((arg as String) != string.Empty) {
							Regex q = new Regex("[\'\"].*[\'\"]");
							int i;
							if ((q.IsMatch (arg as String)) || (int.TryParse(arg as String, out i))) {
								objs.Add (arg as String);
							}
							else {
								GameObject go = null;
								if ((arg as String).Count (f => f == '(') +
							    	(arg as String).Count (f => f == ')') == 0) {
									List<GameObject> matches = new List<GameObject> ();
									foreach (Voxeme voxeme in objSelector.allVoxemes) {
										if (voxeme.voxml.Lex.Pred.Equals(arg)) {
											matches.Add (voxeme.gameObject);
										}
									}

									Debug.Log (matches.Count);

									if (matches.Count == 0) {
										go = GameObject.Find (arg as String);
										if (go == null) {
											for (int j = 0; j < objSelector.disabledObjects.Count; j++) {
												if (objSelector.disabledObjects[j].name == (arg as String)) {
													go = objSelector.disabledObjects[j];
													break;
												}
											}

											if (go == null) {
												OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
												return false;	// abort
											}
										}
									}
									else if (matches.Count == 1) {
										go = matches[0];
										for (int j = 0; j < objSelector.disabledObjects.Count; i++) {
											if (objSelector.disabledObjects[j].name == (arg as String)) {
												go = objSelector.disabledObjects[j];
												break;
											}
										}

										if (go == null) {
											OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
											return false;	// abort
										}
									}
									else {
										//if (!em.evalOrig.ContainsKey(command)){
											Debug.Log (string.Format ("Which {0}?", (arg as String)));
											OutputHelper.PrintOutput (Role.Affector,string.Format("Which {0}?", (arg as String)));
											return false;	// abort
										//}
									}
								}
								objs.Add (go);
	//							objs.Add (GameObject.Find (arg as String));
							}
						}
					}
				}

				objs.Add (false);
				methodToCall = preds.GetType ().GetMethod (pred.ToUpper());

				if (methodToCall != null) {
					Debug.Log ("ComputeSatisfactionConditions: invoke " + methodToCall.Name);
					object obj = methodToCall.Invoke (preds, new object[]{ objs.ToArray () });
				}
				else {
					// no coded-behavior
					// see if a VoxML markup exists
					// if so, we might be able to figure this out,
					if (!File.Exists(Data.voxmlDataPath + string.Format("/programs/{0}.xml",pred))) {
						// otherwise return error
						OutputHelper.PrintOutput (Role.Affector,"Sorry, what does " + "\"" + pred + "\" mean?");
						return false;
					}
				}
			}
			else {
				OutputHelper.PrintOutput (Role.Affector,"Sorry, I don't understand \"" + command + ".\"");
				return false;
			}

			return true;
		}

		public static void ReasonFromAffordances(String program, Voxeme obj) {
			Regex reentrancyForm = new Regex (@"\[[0-9]+\]");
			Regex themeFirst = new Regex (@".*(\[[0-9]+\], .*x.*)");	// check the order of the arguments
			Regex themeSecond = new Regex (@".*(x, .*\[[0-9]+\].*)");
			List<string> supportedRelations = new List<string> (
				new string[]{	// list of supported relations
					@"on\(.*\)",	
					@"in\(.*\)",
					@"under\(.*\)"});	// TODO: move externally, draw from voxeme database
			List<string> genericRelations = new List<string> (
				new string[]{	// list of habitat-independent relations
					@"under\(.*\)",
					@"behind\(.*\)",	
					@"in_front\(.*\)",
					@"left\(.*\)",
					@"right\(.*\)",
					@"touching\(.*\)" });	// TODO: move externally, draw from voxeme database
		
			// get relation tracker
			RelationTracker relationTracker = (RelationTracker)GameObject.Find ("BehaviorController").GetComponent("RelationTracker");

			ObjectSelector objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();

			// get bounds of theme object of program
			Bounds objBounds = Helper.GetObjectWorldSize(obj.gameObject);

			// get list of all voxeme entities that are not components of other voxemes
//			Voxeme[] allVoxemes = objSelector.allVoxemes.Where(a => // where there does not exist another voxeme that has this voxeme as a component
//				objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a.gameObject).ToList().Count == 0)).ToArray();
			Voxeme[] allVoxemes = objSelector.allVoxemes.Where (a => 
				!objSelector.allVoxemes.SelectMany(
					(v, c) => v.opVox.Type.Components.Where(
						comp => comp.Item2 != v.gameObject).Select(comp => comp.Item2)).ToList().Contains (a.gameObject)).ToArray ();

			List<GameObject> components = objSelector.allVoxemes.SelectMany((v, c) => v.opVox.Type.Components.Where(comp => comp.Item2 != v.gameObject).Select(comp => comp.Item2)).ToList();

			foreach (GameObject go in components) {
				Debug.Log (go);
			}

			foreach (Voxeme v in allVoxemes) {
				Debug.Log (v);
			}
//			objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a.gameObject).ToList().Count == 0)
			
//				UnityEngine.Object.FindObjectsOfType<Voxeme>().Where(a => 
//				objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a)) 
//				a.isActiveAndEnabled).ToArray();

			// reactivate physics by default
			//PhysicsHelper.ResolvePhysicsDiscepancies(obj.gameObject);
			bool reactivatePhysics = true;
			//if (Helper.IsTopmostVoxemeInHierarchy(obj.gameObject)){
			//	obj.minYBound = objBounds.min.y;	//TODO: did removing this really fix the bug where
			//}										// a turned object would go through the supporting surface?
													// did that cause any other bugs?


			// check existing relations
			// if obj is in support or containment relation w/ concave obj
			//Debug.Log (relationTracker.relations.Count);
			foreach (DictionaryEntry relation in relationTracker.relations) {
				if (((String)relation.Value).Contains("support") || ((String)relation.Value).Contains("contain")){
//					Debug.Log (((List<GameObject>)relation.Key) [0]);
//					Debug.Log (((List<GameObject>)relation.Key) [1]);
//					Debug.Log (obj.gameObject);
//					Debug.Log (((List<GameObject>)relation.Key) [1] == obj.gameObject);
					if (((List<GameObject>)relation.Key)[0] == obj.gameObject) {
						if (TestRelation(((List<GameObject>)relation.Key)[1],"on",obj.gameObject) ||
							TestRelation(((List<GameObject>)relation.Key)[1],"in",obj.gameObject)) {
							reactivatePhysics = false;
							break;
						}
					}
				}
			}

			// reason new relations from affordances
			OperationalVox.OpAfford_Str affStr = obj.opVox.Affordance;
			string result;

			bool relationSatisfied = false;

			// relation-based reasoning from affordances
			foreach (int objHabitat in affStr.Affordances.Keys) {
				if (TestHabitat (obj.gameObject, objHabitat)) {
//					Debug.Log (objHabitat);
					foreach (Voxeme test in allVoxemes) {
						if (test.gameObject != obj.gameObject) {
							// foreach voxeme
							// get bounds of object being tested against
							Bounds testBounds = Helper.GetObjectWorldSize (test.gameObject);
							if (!test.gameObject.name.Contains ("*")) { // hacky fix to filter out unparented objects w/ disabled voxeme components

								// habitat-independent relation handling
								foreach (string rel in genericRelations) {
									string relation = rel.Split('\\')[0];	// not using relation as regex here

									//Debug.Log (string.Format ("Is {0} {1} {2}?", obj.gameObject.name, relation, test.gameObject.name));
									if (TestRelation (obj.gameObject, relation, test.gameObject)) {
										relationTracker.AddNewRelation (new List<GameObject>{ obj.gameObject, test.gameObject }, relation);
									}
									else {
										// remove if present
										relationTracker.RemoveRelation (new List<GameObject>{ obj.gameObject, test.gameObject }, relation);
									}

									if (TestRelation (test.gameObject, relation, obj.gameObject)) {
										relationTracker.AddNewRelation (new List<GameObject>{ test.gameObject, obj.gameObject }, relation);
									}
									else {
										// remove if present
										relationTracker.RemoveRelation (new List<GameObject>{ test.gameObject, obj.gameObject }, relation);
									}
								}

								//if (test.enabled) {	// if voxeme is active
//								Debug.Log(test);
								foreach (int testHabitat in test.opVox.Affordance.Affordances.Keys) {
									//if (TestHabitat (test.gameObject, testHabitat)) {	// test habitats
										for (int i = 0; i < test.opVox.Affordance.Affordances[testHabitat].Count; i++) {	// condition/event/result list for this habitat index
											string ev = test.opVox.Affordance.Affordances[testHabitat][i].Item2.Item1;
											Debug.Log (ev);
											if (ev.Contains (program) || ev.Contains ("put")) {	// TODO: resultant states should persist
//												Debug.Break ();
//												Debug.Log (ev);
//												Debug.Log (obj.name);
//												Debug.Log (test.name);
												//Debug.Log (test.opVox.Lex.Pred);
												//Debug.Log (program);

												foreach (string rel in supportedRelations) {
													Regex r = new Regex (rel);
													if (r.Match (ev).Length > 0) {	// found a relation that might apply between these objects
														string relation = r.Match(ev).Groups[0].Value.Split('(')[0];

														MatchCollection matches = reentrancyForm.Matches(ev);
														foreach (Match m in matches) {
															foreach (Group g in m.Groups) {
																int componentIndex = Helper.StringToInt (
																	                    g.Value.Replace (g.Value, g.Value.Trim (new char[]{ '[', ']' })));
																//Debug.Log (componentIndex);
																if (test.opVox.Type.Components.FindIndex (c => c.Item3 == componentIndex) != -1) {
																	Triple<string, GameObject, int> component = test.opVox.Type.Components.First (c => c.Item3 == componentIndex);
																	//Debug.Log (ev.Replace(g.Value,component.Item2.name));
																	//Debug.Log (string.Format ("Is {0} {1} {2} {3}?", obj.gameObject.name, relation, component.Item2.name, component.Item1));
																	
																	//bool relationSatisfied = false;	// this used to be here

																	//NOTE: These relations are used the *test* object as theme
																	if (themeFirst.Match (ev).Length > 0) {
																		relationSatisfied = TestRelation (test.gameObject, relation, obj.gameObject);
																		//Debug.Break ();
																		//Debug.Log (test);
																		//Debug.Log (obj);
																	}
																	else if (themeSecond.Match (ev).Length > 0) {
																		relationSatisfied = TestRelation (obj.gameObject, relation, test.gameObject);
																	}

																	if (relationSatisfied) {
																		Debug.Log (test.opVox.Affordance.Affordances [testHabitat] [i].Item2.Item1);
																		Debug.Log (test.opVox.Affordance.Affordances [testHabitat] [i].Item2.Item2);
																		result = test.opVox.Affordance.Affordances[testHabitat][i].Item2.Item2;

																		// things are getting a little ad hoc here
																		if (relation == "on") {
																			if (!((Helper.GetMostImmediateParentVoxeme (test.gameObject).GetComponent<Voxeme> ().voxml.Type.Concavity.Contains("Concave")) &&
																			   (Helper.FitsIn (objBounds, testBounds)))) {
																				//if (obj.enabled) {
																				//	obj.gameObject.GetComponent<Rigging> ().ActivatePhysics (true);
																				//}
																				obj.minYBound = testBounds.max.y;
//																				Debug.Log (test);
																			}
																		}
																		else if (relation == "in") {
																			reactivatePhysics = false;
																			obj.minYBound = objBounds.min.y;
																		}
//																		else if (relation == "under") {
//																			GameObject voxObj = Helper.GetMostImmediateParentVoxeme (test.gameObject);
//																			if ((voxObj.GetComponent<Voxeme> ().voxml.Type.Concavity == "Concave") &&	// this is a concave object
//																				(Concavity.IsEnabled (voxObj)) && (Mathf.Abs (Vector3.Dot (voxObj.transform.up, Vector3.up) + 1.0f) <= Constants.EPSILON)) { // TODO: Run this through habitat verification
//																				reactivatePhysics = false;
//																				obj.minYBound = objBounds.min.y;
//																			}
//																		}

																		// TODO: only instantiate a relation if it goes both ways (i.e. only if x can be contained AND y can contain something
																		if (result != "") {
																			result = result.Replace ("x", test.gameObject.name);
																			// any component reentrancy ultimately inherits from the parent voxeme itself
																			result = reentrancyForm.Replace (result, obj.gameObject.name);
																			result = Helper.GetTopPredicate (result);

																			// TODO: maybe switch object order here below => passivize relation?
																			if (themeFirst.Match (ev).Length > 0) {
																				relationTracker.AddNewRelation (new List<GameObject>{ obj.gameObject, test.gameObject }, result);
																				Debug.Log (string.Format ("{0}: {1} {2}.3sg {3}",
																					test.opVox.Affordance.Affordances [testHabitat] [i].Item2.Item1, obj.gameObject.name, result, test.gameObject.name));
																			}
																			else if (themeSecond.Match (ev).Length > 0) {
																				relationTracker.AddNewRelation (new List<GameObject>{ test.gameObject, obj.gameObject }, result);
																				Debug.Log (string.Format ("{0}: {1} {2}.3sg {3}",
																					test.opVox.Affordance.Affordances [testHabitat] [i].Item2.Item1, test.gameObject.name, result, obj.gameObject.name));
																			}

																			if (result == "support") {
																				if (themeFirst.Match (ev).Length > 0) {
																					RiggingHelper.RigTo (test.gameObject, obj.gameObject);
																				}
																				else if (themeSecond.Match (ev).Length > 0) {
																					RiggingHelper.RigTo (obj.gameObject, test.gameObject);
																				}
																			}
																			else if (result == "contain") {
																				if (themeFirst.Match (ev).Length > 0) {
																					RiggingHelper.RigTo (test.gameObject, obj.gameObject);
																				}
																				else if (themeSecond.Match (ev).Length > 0) {
																					RiggingHelper.RigTo (obj.gameObject, test.gameObject);
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									//}
								}
							}
						}
					}
				}
			}

			// non-relation-based reasoning from affordances
			foreach (int objHabitat in affStr.Affordances.Keys) {
				if (TestHabitat (obj.gameObject, objHabitat)) {	// test habitats
					for (int i = 0; i < affStr.Affordances [objHabitat].Count; i++) {	// condition/event/result list for this habitat index
						string ev = affStr.Affordances [objHabitat] [i].Item2.Item1;
//						Debug.Log (ev);
						if (ev.Contains (program)) {
							bool relationIndependent = true;
							foreach (string rel in supportedRelations) {
								Regex r = new Regex (rel);
								if (r.Match (ev).Length > 0) {
									relationIndependent = false;
								}
							}

							if (relationIndependent) {
//								Debug.Log (obj.opVox.Lex.Pred);
//								Debug.Log (program);

								result = affStr.Affordances [objHabitat] [i].Item2.Item2;
//								Debug.Log (result);

								if (result != "") {
									result = result.Replace ("x", obj.gameObject.name);
									// any component reentrancy ultimately inherits from the parent voxeme itself
									result = reentrancyForm.Replace (result, obj.gameObject.name);
									result = Helper.GetTopPredicate (result);
									Debug.Log (string.Format ("{0}: {1} {2}.pp",
										affStr.Affordances [objHabitat] [i].Item2.Item1, obj.gameObject.name, result));
									if (result != "release") {
										// TODO: maybe switch object order here below => passivize relation?
										relationTracker.AddNewRelation (new List<GameObject>{ obj.gameObject }, result);
									}
									//Debug.Break ();
									if (result == "hold") {
										reactivatePhysics = false;
									}
								}
							}
						}
					}
				}
			}

			if (reactivatePhysics) {
				if (obj.enabled) {
//					Debug.Log(obj.name);
//					Debug.Break ();
					Rigging rigging = obj.gameObject.GetComponent<Rigging> ();
					if (rigging != null) {
						//TODO:reenable
						rigging.ActivatePhysics (true);
					}
					//PhysicsHelper.ResolvePhysicsDiscepancies(obj.gameObject);
				}
			}
		}

		public static bool TestHabitat(GameObject obj, int habitatIndex) {
			HabitatSolver habitatSolver = GameObject.Find ("BehaviorController").GetComponent<HabitatSolver> ();

			MethodInfo methodToCall;
			bool r = true;

//			Debug.Log (string.Format ("H[{0}]", habitatIndex));
			if (habitatIndex != 0) {	// index 0 = affordance enabled in all habitats
				OperationalVox opVox = obj.GetComponent<Voxeme> ().opVox;
				if (opVox != null) {
					r = true;
					if (opVox.Habitat.IntrinsicHabitats.ContainsKey (habitatIndex)) {	// do intrinsic habitats first
						List<String> conditioningEnvs = opVox.Habitat.IntrinsicHabitats [habitatIndex];
						foreach (String env in conditioningEnvs) {
							string label = env.Split ('=') [0].Trim ();
							string formula = env.Split ('=') [1].Trim (new char[]{' ','{','}'});
							string methodName = formula.Split ('(') [0].Trim();
							string[] methodArgs = new string[]{string.Empty};

							if (formula.Split ('(').Length > 1) {
								methodArgs = formula.Split ('(') [1].Trim (')').Split (',');
							}

							List<object> args = new List<object> ();
							args.Add (obj);
							foreach (string arg in methodArgs) {
								args.Add (arg);
							}

							methodToCall = habitatSolver.GetType ().GetMethod (methodName);
							if (methodToCall != null) {
								object result = methodToCall.Invoke (habitatSolver, new object[]{ args.ToArray () });
								r &= (bool)result;
							}
						}
					}

					if (opVox.Habitat.ExtrinsicHabitats.ContainsKey (habitatIndex)) {	// then do extrinsic habitats
						List<String> conditioningEnvs = opVox.Habitat.ExtrinsicHabitats [habitatIndex];
						foreach (String env in conditioningEnvs) {
							string label = env.Split ('=') [0].Trim ();
							string formula = env.Split ('=') [1].Trim (new char[]{' ','{','}'});
							string methodName = formula.Split ('(') [0].Trim();
							string[] methodArgs = formula.Split ('(') [1].Trim(')').Split(',');

							List<object> args = new List<object> ();
							args.Add (obj);
							foreach (string arg in methodArgs) {
								args.Add (arg);
							}

							methodToCall = habitatSolver.GetType ().GetMethod (methodName);
							if (methodToCall != null) {
								object result = methodToCall.Invoke (habitatSolver, new object[]{ args.ToArray () });
								r &= (bool)result;
							}
						}
					}

					//flip(cup1);put(ball,under(cup1))
				}
			}
			Debug.Log (string.Format ("H[{0}]:{1}", habitatIndex,r));
			return r;
		}

		public static bool TestRelation(GameObject obj1, string relation, GameObject obj2) {
			bool r = false;
			Bounds bounds1 = Helper.GetObjectWorldSize (obj1);
			Bounds bounds2 = Helper.GetObjectWorldSize (obj2);

			Regex align = new Regex(@"align\(.+,.+\)");
			List<string> habitats = new List<string> ();
			foreach (int i in Helper.GetMostImmediateParentVoxeme(obj2).GetComponent<Voxeme>().opVox.Habitat.IntrinsicHabitats.Keys) {
				habitats.AddRange(Helper.GetMostImmediateParentVoxeme(obj2).GetComponent<Voxeme>().
					opVox.Habitat.IntrinsicHabitats [i].Where ((h => align.IsMatch (h))));
			}

			for (int i = 0; i < habitats.Count; i++) {
				habitats[i] = align.Match(habitats[i]).Value.Replace("align(","").Replace(")","").Split(',')[0];
			}

			// (default to Y-alignment if no encoding exists)
			if (habitats.Count == 0) {
				habitats.Add ("Y");
			}
				
			if (relation == "on") {	// TODO: needs to be fixed: PO, TPP(i), NTPP(i) for contacting regions along axis; relation satisfied only within EPSILON radius of ground obj position
				foreach (string axis in habitats) {
					//Debug.Break ();
//					Debug.Log (obj1);
//					Debug.Log (obj2);
//					Debug.Log (Concavity.IsEnabled(obj1));
//					Debug.Log (Helper.GetMostImmediateParentVoxeme (obj1.gameObject));
					if ((Helper.GetMostImmediateParentVoxeme (obj2.gameObject).GetComponent<Voxeme> ().voxml.Type.Concavity.Contains ("Concave")) &&
					    (Concavity.IsEnabled (obj2)) && (Helper.FitsIn (bounds1, bounds2))) {	// if ground object is concave and figure object would fit inside
						switch (axis) {
						case "X":
							r = (Vector3.Distance (
								new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						case "Y":
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							r &= (obj1.gameObject.transform.position.y > obj2.gameObject.transform.position.y);
							break;

						case "Z":
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						default:
							break;
						}
						r &= RCC8.PO (bounds1, bounds2);
					}
					else if ((Helper.GetMostImmediateParentVoxeme (obj1.gameObject).GetComponent<Voxeme> ().voxml.Type.Concavity.Contains ("Concave")) &&
						(!Concavity.IsEnabled (obj1)) && (Helper.FitsIn (bounds2, bounds1))) {
						switch (axis) {
						case "X":
							r = (Vector3.Distance (
								new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						case "Y":
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							r &= (obj1.gameObject.transform.position.y > obj2.gameObject.transform.position.y);
							break;

						case "Z":
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						default:
							break;
						}
						r &= RCC8.PO (bounds1, bounds2);
					}
					else {
						switch (axis) {
						case "X":
							r = (Vector3.Distance (
								new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						case "Y":
//							Debug.Log (obj1);
//							Debug.Log (obj2);
							Debug.Log (string.Format("{0}:{1}:{2}",obj1,obj2,Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position)));
							Debug.Log (string.Format("{0}:{1}:{2}",obj1,obj2,RCC8.EC (Helper.GetObjectOrientedSize (obj1, true), Helper.GetObjectOrientedSize (obj2, true))));
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						case "Z":
							r = (Vector3.Distance (
								new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
								obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
							break;

						default:
							break;
						}
						r &= RCC8.EC (Helper.GetObjectOrientedSize(obj1,true), Helper.GetObjectOrientedSize(obj2,true));
					}
				}
			}
			else if (relation == "in") {
				if ((Helper.GetMostImmediateParentVoxeme (obj2.gameObject).GetComponent<Voxeme> ().voxml.Type.Concavity.Contains("Concave")) &&
					(Concavity.IsEnabled(obj2))) {
					if (Helper.FitsIn (bounds1, bounds2)) {
						Debug.Log (obj1);
						Debug.Log (obj2);
						Debug.Log (bounds1);
						Debug.Log (bounds2);
						r = RCC8.PO (bounds1, bounds2) || RCC8.ProperPart (bounds1, bounds2);
					}
				}
				else {
					if (Helper.FitsIn (bounds1, bounds2)) {
						//Debug.Break ();
						Debug.Log (obj1);
						Debug.Log (obj2);
						Debug.Log (bounds1);
						Debug.Log (bounds2);
						r = RCC8.PO (bounds1, bounds2) || RCC8.ProperPart (bounds1, bounds2);
					}
				}
			}
			else if (relation == "under") {
				//Debug.Log (obj1.name);
				//Debug.Log (new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z));
				//Debug.Log (obj2.name);
				//Debug.Log (obj2.transform.position);
				float dist = Vector3.Distance (new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
					obj2.gameObject.transform.position);
				//Debug.Log (Vector3.Distance (
				//	new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
				//	obj2.gameObject.transform.position));
				r = (Vector3.Distance (
					new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
					obj2.gameObject.transform.position) <= Constants.EPSILON);
				r &= (obj1.gameObject.transform.position.y < obj2.gameObject.transform.position.y);
			}
			// add generic relations--left, right, etc.
			// TODO: must transform to camera perspective if relative persp is on
			else if (relation == "behind") {
				r = QSR.QSR.Behind(bounds1,bounds2) && 
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.X) || QSR.QSR.During(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X,true)) &&
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y,true));
//				r = (Vector3.Distance (
//					new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
//					obj2.gameObject.transform.position) <= Constants.EPSILON);
//				r &= (obj1.gameObject.transform.position.z > obj2.gameObject.transform.position.z);

			}
			else if (relation == "in_front") {
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.InFront(bounds1,bounds2)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X,true)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.X)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.X,true)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X,true)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X)));
//				Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X,true)));

				r = QSR.QSR.InFront(bounds1,bounds2) && 
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.X) || QSR.QSR.During(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.X,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.X,true)) &&
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y,true));
//				r = (Vector3.Distance (
//					new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
//					obj2.gameObject.transform.position) <= Constants.EPSILON);
//				r &= (obj1.gameObject.transform.position.z < obj2.gameObject.transform.position.z);

			}
			else if (relation == "left") {
//				Debug.Log(string.Format("{0} {3} of {1}:{2}",obj1,obj2,QSR.QSR.Left(bounds1,bounds2),relation));
//				Debug.Log(string.Format("{0} overlaps {1}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z)));
//				Debug.Log(string.Format("{1} overlaps {0}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z,true)));
//				Debug.Log(string.Format("{0} during {1}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.Z)));
//				Debug.Log(string.Format("{1} during {0}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.Z,true)));
//				Debug.Log(string.Format("{0} starts {1}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z)));
//				Debug.Log(string.Format("{1} starts {0}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z,true)));
//				Debug.Log(string.Format("{0} finishes {1}:{2}",obj1,obj2,QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z)));
//				Debug.Log(string.Format("{1} finishes {0}:{2}",obj1,obj2,QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z,true)));
//				Debug.Log(string.Format("{0} overlaps {1}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y)));
//				Debug.Log(string.Format("{1} overlaps {0}:{2}",obj1,obj2,QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true)));
//				Debug.Log(string.Format("{0} during {1}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.Y)));
//				Debug.Log(string.Format("{1} during {0}:{2}",obj1,obj2,QSR.QSR.During(bounds1,bounds2,MajorAxis.Y,true)));
//				Debug.Log(string.Format("{0} starts {1}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y)));
//				Debug.Log(string.Format("{1} starts {0}:{2}",obj1,obj2,QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y,true)));
//				Debug.Log(string.Format("{0} finishes {1}:{2}",obj1,obj2,QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y)));
//				Debug.Log (string.Format ("{1} finishes {0}:{2}", obj1, obj2, QSR.QSR.Finishes (bounds1, bounds2, MajorAxis.Y, true)));

				r = QSR.QSR.Left(bounds1,bounds2) && 
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z,true)) &&
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y,true));
					//(Vector3.Distance (
					//new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
					//obj2.gameObject.transform.position) <= Constants.EPSILON);
				//r &= (obj1.gameObject.transform.position.x < obj2.gameObject.transform.position.x);
			}
			else if (relation == "right") {
				r = QSR.QSR.Right(bounds1,bounds2) && 
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Z,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Z,true)) &&
					(QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.During(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.During(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Starts(bounds1,bounds2,MajorAxis.Y,true) ||
						QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y) || QSR.QSR.Finishes(bounds1,bounds2,MajorAxis.Y,true));
//				r = (Vector3.Distance (
//					new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
//					obj2.gameObject.transform.position) <= Constants.EPSILON);
//				r &= (obj1.gameObject.transform.position.x > obj2.gameObject.transform.position.x);
			}
			else if (relation == "touching") {
				r = RCC8.EC(Helper.GetObjectOrientedSize(obj1,true), Helper.GetObjectOrientedSize(obj2,true));
//				r = RCC8.EC(Helper.GetObjectOrientedSize(obj1), Helper.GetObjectOrientedSize(obj2));
			}
			else {
			}

//			Debug.Log (r);
			return r;
		}

		public static void ReevaluateRelationships(String program, GameObject obj) {
			// get object bounds
			Bounds objBounds = Helper.GetObjectWorldSize(obj);

			// get all objects
			GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

			// reasoning from habitats
			// for each object
			// for each habitat in object
			// for each affordance by habitat

			// e.g. with object obj: H->[put(x, on([1]))]support([1], x)
			//	if (program == "put" && obj is on test) then test supports obj
			//	H[2]->[put(x, in([1]))]contain(y, x)
			// if obj is in configuration [2], if (program == "put" && obj is in test) then test contains obj

			if (program == "put") {
				Bounds testBounds = new Bounds ();
				Voxeme[] voxemes;
				RelationTracker relationTracker = (RelationTracker)GameObject.Find ("BehaviorController").GetComponent("RelationTracker");
				foreach (GameObject test in allObjects) {
					if (test != obj) {
						voxemes = test.GetComponentsInChildren<Voxeme> ();
						foreach (Voxeme voxeme in voxemes) {
							if (voxeme != null) {
								if (!voxeme.gameObject.name.Contains("*")) {	// hacky fix to filter out unparented objects w/ disabled voxeme components
									testBounds = Helper.GetObjectWorldSize (test);
									// bunch of underspecified RCC relations
									if (voxeme.voxml.Afford_Str.Affordances.Any (p => p.Formula.Contains("support"))) {
										// **check for support configuration here
										if ((voxeme.voxml.Type.Concavity.Contains("Concave")) &&
										   (Helper.FitsIn (objBounds, testBounds))) {	// if test object is concave and placed object would fit inside
											if (RCC8.PO (objBounds, Helper.GetObjectWorldSize (test))) {	// interpenetration = support
												RiggingHelper.RigTo (obj, test);	// setup parent-child rig
												relationTracker.AddNewRelation(new List<GameObject>{test,obj},"support");
												Debug.Log (test.name + " supports " + obj.name);
											}
										}
										else {
											if (RCC8.EC (objBounds, Helper.GetObjectWorldSize (test))) {	// otherwise EC = support
												if (voxeme.enabled) {
													obj.GetComponent<Rigging> ().ActivatePhysics (true);
												}
												obj.GetComponent<Voxeme>().minYBound = Helper.GetObjectWorldSize(test).max.y;
												RiggingHelper.RigTo (obj, test);	// setup parent-child rig
												relationTracker.AddNewRelation(new List<GameObject>{test,obj},"support");
												Debug.Log (test.name + " supports " + obj.name);
											}
										}
									}

									if (voxeme.voxml.Afford_Str.Affordances.Any (p => p.Formula.Contains("contain"))) {
										if (Helper.FitsIn (objBounds, Helper.GetObjectWorldSize (test))) {
											if (RCC8.PO (objBounds, Helper.GetObjectWorldSize (test))) {	// interpenetration = containment
												obj.GetComponent<Voxeme> ().minYBound = PhysicsHelper.GetConcavityMinimum(obj);//Helper.GetObjectWorldSize (obj).min.y;
												RiggingHelper.RigTo (obj, test);	// setup parent-child rig
												relationTracker.AddNewRelation (new List<GameObject>{test,obj}, "contain");
												Debug.Log (test.name + " contains " + obj.name);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
