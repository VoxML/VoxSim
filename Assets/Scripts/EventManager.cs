using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Agent;
using Global;
using Satisfaction;
using Vox;
using RootMotion.Demos;
using RootMotion.FinalIK;

public class EventManagerArgs : EventArgs {

	public string EventString { get; set; }
	public bool MacroEvent { get; set; }

	public EventManagerArgs(string str, bool macroEvent = false)
	{
		this.EventString = str;
		this.MacroEvent = macroEvent;
	}
}

public class EventManager : MonoBehaviour {
	public FullBodyBipedIK bodyIk;
	public InteractionLookAt lookAt = new InteractionLookAt();
	public InteractionSystem interactionSystem;

	//public GameObject leftHandTarget;
	public InteractionObject interactionObject;

	public List<String> events = new List<String>();
	public OrderedDictionary eventsStatus = new OrderedDictionary();
	public ObjectSelector objSelector;
	public InputController inputController;
	public string lastParse = string.Empty;
	//public string lastObjectResolved = string.Empty;
	public Dictionary<String,String> evalOrig = new Dictionary<String, String>();
	public Dictionary<String,String> evalResolved = new Dictionary<String, String>();
	public Hashtable globalVars = new Hashtable();

	public double eventWaitTime = 2000.0;
	Timer eventWaitTimer;
	bool eventWaitCompleted = false;

	string skolemized, evaluated;
	MethodInfo methodToCall;
	public Predicates preds;
	String nextQueuedEvent = "";
	int argVarIndex = 0;
	Hashtable skolems = new Hashtable();
	string argVarPrefix = @"_ARG";
	Regex r = new Regex(@".*\(.*\)");
	String nextIncompleteEvent;
	bool stayExecution = false;

	public enum EvaluationPass {
		Attributes,
		RelationsAndFunctions
	}

	public bool immediateExecution = true;

	public event EventHandler ObjectsResolved;

	public void OnObjectsResolved(object sender, EventArgs e)
	{
		if (ObjectsResolved != null)
		{
			ObjectsResolved(this, e);
		}
	}

	public event EventHandler SatisfactionCalculated;

	public void OnSatisfactionCalculated(object sender, EventArgs e)
	{
		if (SatisfactionCalculated != null)
		{
			SatisfactionCalculated(this, e);
		}
	}

	public event EventHandler ExecuteEvent;

	public void OnExecuteEvent(object sender, EventArgs e)
	{
		if (ExecuteEvent != null)
		{
			ExecuteEvent(this, e);
		}
	}

	public event EventHandler EventComplete;

	public void OnEventComplete(object sender, EventArgs e)
	{
		if (EventComplete != null)
		{
			EventComplete(this, e);
		}
	}

	public event EventHandler QueueEmpty;

	public void OnQueueEmpty(object sender, EventArgs e)
	{
		if (QueueEmpty != null)
		{
			QueueEmpty(this, e);
		}
	}

	public event EventHandler ForceClear;

	public void OnForceClear(object sender, EventArgs e)
	{
		if (ForceClear != null)
		{
			ForceClear(this, e);
		}
	}

	// Use this for initialization
	void Start () {
		preds = gameObject.GetComponent<Predicates> ();
		objSelector = GameObject.Find ("BlocksWorld").GetComponent<ObjectSelector> ();
		inputController = GameObject.Find ("IOController").GetComponent<InputController> ();

		inputController.ParseComplete += StoreParse;
		inputController.ParseComplete += ClearGlobalVars;
		//inputController.InputReceived += StartEventWaitTimer;

		//eventWaitTimer = new Timer (eventWaitTime);
		//eventWaitTimer.Enabled = false;
		//eventWaitTimer.Elapsed += ExecuteNextEvent;
	}


	float initiatePhaseTime = 0f;
	public bool isInitiatePhase = false;

	float recoverPhaseTime = 1f;
	public bool startRecoverPhase = false;

//	// Update is called once per frame
//	void Update () {
//		if (stayExecution) {
//			stayExecution = false;
//			return;
//		}
//
//		if (startRecoverPhase) {
//			if (recoverPhaseTime > 0) {
//				recoverPhaseTime -= Time.deltaTime * 1f;
//
//				bodyIk.solver.rightHandEffector.positionWeight = recoverPhaseTime;
//				bodyIk.solver.rightHandEffector.rotationWeight = recoverPhaseTime;
//				bodyIk.solver.rightHandEffector.position = rightHandTarget.transform.position;
//				bodyIk.solver.rightHandEffector.rotation = rightHandTarget.transform.rotation;
//
//				lookAtIk.solver.IKPosition = rightHandTarget.transform.position;
//				lookAtIk.solver.IKPositionWeight = recoverPhaseTime;
//			} else {
//				recoverPhaseTime = 1f;
//				startRecoverPhase = false;
//				Debug.Log ("======= startRecoverPhase false ========");
//			}
//		}
//
//		if (startInitiatePhase) {
//			if (initiatePhaseTime < 1) {
//				initiatePhaseTime += Time.deltaTime * 1f;
//
//				bodyIk.solver.rightHandEffector.positionWeight = initiatePhaseTime;
//				bodyIk.solver.rightHandEffector.rotationWeight = initiatePhaseTime;
//				bodyIk.solver.rightHandEffector.position = rightHandTarget.transform.position;
//				bodyIk.solver.rightHandEffector.rotation = rightHandTarget.transform.rotation;
//
//				lookAtIk.solver.IKPosition = rightHandTarget.transform.position;
//				lookAtIk.solver.IKPositionWeight = initiatePhaseTime;
//			} else {
//				initiatePhaseTime = 0f;
//				startInitiatePhase = false;
//				Debug.Log ("======= startInitiatePhase false ========");
//
//
//				if (events.Count > 0) {
//					if (SatisfactionTest.ComputeSatisfactionConditions (events [0])) {
//						ExecuteCommand (events [0]);
//					}
//					else {
//						RemoveEvent (0);
//					}
//				}
//			}
//		} else {
//			if (events.Count > 0) {
//				bool q = SatisfactionTest.IsSatisfied (events [0]);
//				Debug.Log ("q == " + q);
//
//				bodyIk.solver.rightHandEffector.positionWeight = 1f;
//				bodyIk.solver.rightHandEffector.rotationWeight = 1f;
//				bodyIk.solver.rightHandEffector.position = rightHandTarget.transform.position;
//				bodyIk.solver.rightHandEffector.rotation = rightHandTarget.transform.rotation;
//
//				lookAtIk.solver.IKPosition = rightHandTarget.transform.position;
//				lookAtIk.solver.IKPositionWeight = 1f;
//
//				if (q) {
//					GameObject.Find ("BlocksWorld").GetComponent<AStarSearch> ().path.Clear ();
//					Debug.Log ("Satisfied " + events [0]);
//
//					for (int i = 0; i < events.Count - 1; i++) {
//						events [i] = events [i + 1];
//					}
//					string completedEvent = events [events.Count - 1];
//					RemoveEvent (events.Count - 1);
//
//					// Move hand back to the original posture
//					startRecoverPhase = true;
//					Debug.Log ("======= startRecoverPhase true ========");
//
//					//Debug.Log (events.Count);
//
//					if (events.Count > 0) {
//						ExecuteNextCommand ();
//					}
//					else {
//						if (OutputHelper.GetCurrentOutputString (Role.Affector) != "I'm sorry, I can't do that.") {
//							//OutputHelper.PrintOutput (Role.Affector, "OK, I did it.");
//							EventManagerArgs eventArgs = new EventManagerArgs (completedEvent);
//							OnEventComplete (this, eventArgs);
//						}
//					}
//				}
//			}
//			else {
//			}
//		}
//	}

	string completedEvent = "";

	// Update is called once per frame
	void Update () {
		if (stayExecution) {
			stayExecution = false;
			return;
		}

		/*if (bodyIk != null) {
			if (interactionObject != null) {
				if (interactionSystem.IsPaused (FullBodyBipedEffector.RightHand)) {
					if (isInitiatePhase) {
						Debug.Log ("Done interaction, execute command");
						// Only execute command once
						isInitiatePhase = false;

						// These don't work
//				interactionSystem.manualResumeLookAt ();
//				// I need to reset the lookAt target because otherwise it would be automatically reset to null
//				interactionSystem.LookAtInteraction (FullBodyBipedEffector.RightHand, rightHandTarget);

						if (events.Count > 0) {
							if (SatisfactionTest.ComputeSatisfactionConditions (events [0])) {
								ExecuteCommand (events [0]);
							}
							else {
								RemoveEvent (0);
							}
						}
					} else {
						// Currently in movement
						lookAt.ik.solver.IKPosition = interactionObject.transform.position;
						lookAt.ik.solver.IKPositionWeight = 1f;
					}
				} else {
					lookAt.ik.solver.IKPosition = interactionObject.transform.position;
					if (interactionSystem.GetProgress (FullBodyBipedEffector.RightHand) <= 0.5) {
						lookAt.ik.solver.IKPositionWeight = interactionSystem.GetProgress (FullBodyBipedEffector.RightHand) * 2;
					} else if (interactionSystem.GetProgress (FullBodyBipedEffector.RightHand) < 1) {
						lookAt.ik.solver.IKPositionWeight = (1 - interactionSystem.GetProgress (FullBodyBipedEffector.RightHand)) * 2;
					} else {
						lookAt.ik.solver.IKPositionWeight = 0;
					}
				}
			}
		}*/
//		else {
//			if (events.Count > 0) {
//				if (SatisfactionTest.ComputeSatisfactionConditions (events [0])) {
//					ExecuteCommand (events [0]);
//				}
//				else {
//					RemoveEvent (0);
//				}
//			}
//		}

		if (events.Count > 0) {
			bool q = SatisfactionTest.IsSatisfied (events [0]);

			if (q) {
				GameObject.Find ("BlocksWorld").GetComponent<AStarSearch> ().path.Clear ();
				Debug.Log ("Satisfied " + events [0]);

				for (int i = 0; i < events.Count - 1; i++) {
					events [i] = events [i + 1];
				}
				completedEvent = events [events.Count - 1];
				RemoveEvent (events.Count - 1);

				//if (interactionSystem != null) {
					//interactionSystem.ResumeAll ();
					//startRecoverPhase = true;
				//}

				if (events.Count > 0) {
					ExecuteNextCommand ();
				}
				else {
					if (OutputHelper.GetCurrentOutputString (Role.Affector) != "I'm sorry, I can't do that.") {
						//OutputHelper.PrintOutput (Role.Affector, "OK, I did it.");
						EventManagerArgs eventArgs = new EventManagerArgs (completedEvent);
						OnEventComplete (this, eventArgs);
					}
				}
			}
		}
		else {
		}
	}

	public void RemoveEvent(int index) {
		Debug.Log (string.Format("Removing event@{0}: {1}",index,events[index]));
		events.RemoveAt (index);

		if (events.Count == 0) {
			OnQueueEmpty (this, null);
		}
	}

	public void InsertEvent(String commandString, int before) {
		//Debug.Break ();
		Debug.Log ("Inserting: " + commandString);
		events.Insert(before,commandString);
	}

	public void QueueEvent(String commandString) {
		// not using a Queue because I'm horrible
		events.Add(commandString);
	}

	public void StoreParse(object sender, EventArgs e) {
		lastParse = ((InputEventArgs)e).InputString;
	}

	public void ClearGlobalVars(object sender, EventArgs e) {
		globalVars.Clear ();
	}

	public void WaitComplete(object sender, EventArgs e) {
		((System.Timers.Timer)sender).Enabled = false;
//		RemoveEvent (0);
//		stayExecution = true;
	}

	public void PrintEvents() {
		foreach (String e in events) {
			Debug.Log (e);
		}
	}

	void StartEventWaitTimer(object sender, EventArgs e) {
		eventWaitTimer.Enabled = true;
	}

	void ExecuteNextEvent(object sender, ElapsedEventArgs e) {
		//Debug.Log ("Event wait complete");
		eventWaitCompleted = true;
	}

	public void ExecuteNextCommand() {
		//PhysicsHelper.ResolveAllPhysicsDiscrepancies (false);
		Debug.Log ("Next Command: " + events [0]);

		if (!EvaluateCommand (events [0])) {
			return;
		}


		Hashtable predArgs = Helper.ParsePredicate (events [0]);
		String pred = Helper.GetTopPredicate (events [0]);

//		if (bodyIk != null) {
//			if (predArgs.Count > 0) {
//				try {
//					// Resolve interactionObject
//					var objs = extractObjects (pred, (String)predArgs [pred]);
//					if (objs.Count > 0 && objs [0] is GameObject) {
//						interactionObject = ((GameObject)objs [0]).GetComponentInChildren<InteractionObject> ();
//					}
//					
//
////					if (interactionObject != null) {
////						// Execute interaction	
////						interactionSystem.StartInteraction (FullBodyBipedEffector.RightHand, interactionObject, true);
////
////						// TUAN
////						// Before Executing event
////						// Move hand to reach the target
////						isInitiatePhase = true;
////						Debug.Log ("======= isInitiatePhase true ========");
////					}
//				} catch (ArgumentNullException e) {
//					return;
//				}
//			}
//		}
//		else {
		/// NIKHIL: Instead of having isInitiatePhase here, all events w/ agent should insert a "grasp" precondition
		/// This should be handled automatically in a VoxML interpreter
			if (SatisfactionTest.ComputeSatisfactionConditions (events [0])) {
				ExecuteCommand (events [0]);
			}
			else {
				RemoveEvent (0);
			}
//		}
	}

	public bool EvaluateCommand(String command) {
		ClearRDFTriples ();
		ClearSkolems ();
		ParseCommand (command);

		string globalsApplied = ApplyGlobals (command);

		FinishSkolemization ();
		skolemized = Skolemize (globalsApplied);
		Debug.Log ("Skolemized command: " + skolemized);
		//EvaluateSkolemizedCommand(skolemized);

		if (!EvaluateSkolemConstants (EvaluationPass.Attributes)) {
			RemoveEvent (events.Count - 1);
			return false;
		}
		string objectResolved = ApplySkolems (skolemized);
//		Debug.Log (objectResolved);

		if (objectResolved != command) {
			OnObjectsResolved (this, new EventManagerArgs (objectResolved));
		}

		if (events.IndexOf (command) < 0) {
			return false;
		}

//		Triple<String,String,String> triple = Helper.MakeRDFTriples(objectResolved);
//		if (triple.Item1 != "" && triple.Item2 != "" && triple.Item3 != "") {
//			preds.rdfTriples.Add(triple);
//			Helper.PrintRDFTriples(preds.rdfTriples);
//		}
//		else {
//			Debug.Log ("Failed to make RDF triple");
//		}

		if (!EvaluateSkolemConstants (EvaluationPass.RelationsAndFunctions)) {
			RemoveEvent (events.Count - 1);
			return false;
		}

		evaluated = ApplySkolems (skolemized);
		Debug.Log (string.Format("Evaluated command@{0}: {1}", events.IndexOf(command), evaluated));
		if (!evalOrig.ContainsKey (evaluated)) {
			evalOrig.Add (evaluated, command);
		}

		if (!evalResolved.ContainsKey (evaluated)) {
			evalResolved.Add (evaluated, objectResolved);
		}
		events [events.IndexOf (command)] = evaluated;

		Triple<String,String,String> triple = Helper.MakeRDFTriples(evalResolved[evaluated]);
		Debug.Log(evalOrig[evaluated]);
		Debug.Log(evalResolved[evaluated]);
		Debug.Log (triple.Item1 + " " + triple.Item2 + " " + triple.Item3);

		if (triple.Item1 != "" && triple.Item2 != "" && triple.Item3 != "") {
			preds.rdfTriples.Add(triple);
			Helper.PrintRDFTriples(preds.rdfTriples);
		}
		else {
			Debug.Log ("Failed to make RDF triple");
		}

		//OnExecuteEvent (this, new EventManagerArgs (evaluated));

		return true;
	}

	List<object> ExtractObjects (String pred, String predArg)
	{
		List<object> objs = new List<object> ();
		Queue<String> argsStrings = new Queue<String> (predArg.Split (new char[] {
			','
		}));

		while (argsStrings.Count > 0) {
			object arg = argsStrings.Dequeue ();
			if (Helper.v.IsMatch ((String)arg)) {
				// if arg is vector form
				objs.Add (Helper.ParsableToVector ((String)arg));
			}
			else
				if (arg is String) {
					// if arg is String
					if ((arg as String) != string.Empty) {
						Regex q = new Regex ("[\'\"].*[\'\"]");
						int i;
						if ((q.IsMatch (arg as String)) || (int.TryParse (arg as String, out i))) {
							objs.Add (arg as String);
						}
						else {
							List<GameObject> matches = new List<GameObject> ();
							foreach (Voxeme voxeme in objSelector.allVoxemes) {
								if (voxeme.voxml.Lex.Pred.Equals (arg)) {
									matches.Add (voxeme.gameObject);
								}
							}
							if (matches.Count <= 1) {
								GameObject go = GameObject.Find (arg as String);
								if (go == null) {
									for (int j = 0; j < objSelector.disabledObjects.Count; j++) {
										if (objSelector.disabledObjects[j].name == (arg as String)) {
											go = objSelector.disabledObjects[j];
											break;
										}
									}

									if (go == null) {
										OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));

										throw new ArgumentNullException ("Couldn't resolve the object");
										// abort
									}
								}
								objs.Add (go);
							}
							else {
								//Debug.Log (string.Format ("Which {0}?", (arg as String)));
								//OutputHelper.PrintOutput (string.Format("Which {0}?", (arg as String)));
							}
						}
					}
				}
		}
		objs.Add (true);
		methodToCall = preds.GetType ().GetMethod (pred.ToUpper ());
		return objs;
	}

	public void ExecuteCommand(String evaluatedCommand) {
		Debug.Log("Execute command: " + evaluatedCommand);
		Hashtable predArgs = Helper.ParsePredicate (evaluatedCommand);
		String pred = Helper.GetTopPredicate (evaluatedCommand);

		if (predArgs.Count > 0) {
			try {
				var objs = ExtractObjects (pred, (String)predArgs [pred]);

				if (preds.rdfTriples.Count > 0) {
					if (methodToCall != null) {
						Debug.Log("========================== ExecuteCommand ============================");
						Debug.Log ("ExecuteCommand: invoke " + methodToCall.Name);
						object obj = methodToCall.Invoke (preds, new object[]{ objs.ToArray () });
						Debug.Log (evaluatedCommand);
						OnExecuteEvent (this, new EventManagerArgs (evaluatedCommand));
					}
					else {
						if (File.Exists (Data.voxmlDataPath + string.Format ("/programs/{0}.xml", pred))) {
							using (StreamReader sr = new StreamReader (Data.voxmlDataPath + string.Format ("/programs/{0}.xml", pred))) {
								preds.ComposeSubevents (VoxML.LoadFromText (sr.ReadToEnd ()), objs.ToArray ());
							}
						}
					}
				}
			} catch (ArgumentNullException e){
				return;
			}
		}
	}

	public void AbortEvent() {
		if (events.Count > 0) {
			//InsertEvent ("satisfy()", 0);
			InsertEvent ("", 0);
			RemoveEvent (1);
			//RemoveEvent (0);
		}
	}

	public void ClearEvents() {
		events.Clear ();
		OnForceClear (this, null);
	}

	String GetNextIncompleteEvent() {
		String[] keys = new String[eventsStatus.Keys.Count];
		bool[] values = new bool[eventsStatus.Keys.Count];

		eventsStatus.Keys.CopyTo (keys,0);
		eventsStatus.Values.CopyTo (values,0);

		String nextIncompleteEvent = "";
		for (int i = 0; i < keys.Length; i++) {
			if ((bool)eventsStatus[keys[i]] == false) {
				nextIncompleteEvent = (String)keys[i];
				if (i < events.Count-1) {
					SatisfactionTest.ComputeSatisfactionConditions(events[i+1]);
					eventsStatus.Keys.CopyTo (keys,0);
					eventsStatus.Values.CopyTo (values,0);
					nextQueuedEvent = (String)keys[i+1];
				}
				else {
					nextQueuedEvent = "";
				}
				break;
			}
		}

		return nextIncompleteEvent;
	}

	public void ClearSkolems() {
		argVarIndex = 0;
		skolems.Clear ();
	}

	public void ClearRDFTriples () {
		preds.rdfTriples.Clear ();
	}

	public void ParseCommand(String command) {
		Hashtable predArgs;
		String predString = null;
		Queue<String> argsStrings = null;

		if (r.IsMatch (command)) {	// if command matches predicate form
			//Debug.Log ("ParseCommand: " + command);
			// make RDF triples only after resolving attributives to atomics (but before evaluating relations and functions)
			/*Triple<String,String,String> triple = Helper.MakeRDFTriples(command);
			if (triple.Item1 != "" && triple.Item2 != "" && triple.Item3 != "") {
				preds.rdfTriples.Add(triple);
				Helper.PrintRDFTriples(preds.rdfTriples);
			}
			else {
				Debug.Log ("Failed to make RDF triple");
			}*/
			predArgs = Helper.ParsePredicate(command);
			foreach (DictionaryEntry entry in predArgs) {
				predString = (String)entry.Key;
				argsStrings = new Queue<String>(((String)entry.Value).Split (new char[] {','}));

				StringBuilder sb = new StringBuilder("[");
				foreach(String arg in argsStrings) {
					sb.Append (arg + ",");
				}
				sb.Remove(sb.Length-1,1);
				sb.Append("]");
				String argsList = sb.ToString();
				//Debug.Log(predString + " : " + argsList);

				for(int i = 0; i < argsStrings.Count; i++) {
					Debug.Log ("Input: " + argsStrings.ElementAt (i));
					if (r.IsMatch (argsStrings.ElementAt (i))) {
						String v = argVarPrefix+argVarIndex.ToString();
						skolems[v] = argsStrings.ElementAt (i);
						Debug.Log (v + " : " + skolems[v]);
						argVarIndex++;

						sb = new StringBuilder(sb.ToString());
						foreach(DictionaryEntry kv in skolems) {
							argsList = argsList.Replace((String)kv.Value, (String)kv.Key);
						}

					}
					ParseCommand (argsStrings.ElementAt (i));
				}
			}
		}
	}

	public void FinishSkolemization() {
		Hashtable temp = new Hashtable ();

		foreach (DictionaryEntry kv in skolems) {
			foreach (DictionaryEntry kkv in skolems) {
				if (kkv.Key != kv.Key) {
					//Debug.Log ("FinishSkolemization: "+kv.Key+ " " +kkv.Key);
					if (!temp.Contains (kkv.Key)) {
						if (((String)kkv.Value).Contains ((String)kv.Value)) {
							//Debug.Log ("FinishSkolemization: " + kv.Value + " found in " + kkv.Value);
							//Debug.Log ("FinishSkolemization: " + kkv.Key + " : " + ((String)kkv.Value).Replace ((String)kv.Value, (String)kv.Key));
							temp [kkv.Key] = ((String)kkv.Value).Replace ((String)kv.Value, (String)kv.Key);
						}
					}
				}
			}
		}

		foreach (DictionaryEntry kv in temp) {
			skolems[kv.Key] = temp[kv.Key];
		}

		Helper.PrintKeysAndValues(skolems);
	}

	public String Skolemize(String inString) {
		String outString = inString;
		String temp = inString;

		int parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

		do{
			foreach (DictionaryEntry kv in skolems) {
				outString = (String)outString.Replace((String)kv.Value,(String)kv.Key);
				//Debug.Log (outString);
			}
			temp = outString;
			parenCount = temp.Count(f => f == '(') + 
				temp.Count(f => f == ')');
			//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());
			//move(mug,from(edge(table)),to(edge(table)))
		}while(parenCount > 2);

		return outString;
	}

	public String ApplyGlobals(String inString) {
		String outString = inString;
		String temp = inString;

		int parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

		foreach (DictionaryEntry kv in globalVars) {
			if (kv.Value is Vector3) {
				outString = (String)outString.Replace ((String)kv.Key, Helper.VectorToParsable ((Vector3)kv.Value));
			}
			else if (kv.Value is GameObject) {
				outString = (String)outString.Replace ((String)kv.Key, ((GameObject)kv.Value).name);
				Dictionary<string,string> changeValues = skolems.Cast<DictionaryEntry> ().ToDictionary (kkv => (String)kkv.Key, kkv => (String)kkv.Value).
					Where (kkv => ((String)kkv.Value).Contains ((String)kv.Key)).ToDictionary (kkv => (String)kkv.Key, kkv => (String)kkv.Value);
				foreach (string key in changeValues.Keys) {
					skolems [key] = changeValues [key].Replace ((String)kv.Key, ((GameObject)kv.Value).name);
				}

				Helper.PrintKeysAndValues (skolems);
//				foreach (string key in changeValues.Keys) {
//					Debug.Log (key + ":" + changeValues [key]);
//				}
			}
			else if (kv.Value is List<GameObject>) {
				String list = String.Join (":", ((List<GameObject>)kv.Value).Select(go => go.name).ToArray());
				outString = (String)outString.Replace ((String)kv.Key, list);
				list = String.Join (",", ((List<GameObject>)kv.Value).Select(go => go.name).ToArray());
				Dictionary<string,string> changeValues = skolems.Cast<DictionaryEntry> ().ToDictionary (kkv => (String)kkv.Key, kkv => (String)kkv.Value).
					Where (kkv => ((String)kkv.Value).Contains ((String)kv.Key)).ToDictionary (kkv => (String)kkv.Key, kkv => (String)kkv.Value);
				foreach (string key in changeValues.Keys) {
					skolems [key] = changeValues [key].Replace ((String)kv.Key, list);
				}
			}
			else if (kv.Value is String) {
				outString = (String)outString.Replace ((String)kv.Key, (String)kv.Value);
			}
			else if (kv.Value is List<String>) {
				String list = String.Join (",", ((List<String>)kv.Value).ToArray());
				outString = (String)outString.Replace ((String)kv.Key, list);
			}
		}
		temp = outString;
		parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

		Debug.Log (outString);
		return outString;
	}

	public String ApplySkolems(String inString) {
		String outString = inString;
		String temp = inString;

		int parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

		foreach (DictionaryEntry kv in skolems) {
			if (kv.Value is Vector3) {
				outString = (String)outString.Replace ((String)kv.Key, Helper.VectorToParsable ((Vector3)kv.Value));
				//Debug.Log (outString);
			}
			else if (kv.Value is String) {
				outString = (String)outString.Replace ((String)kv.Key, (String)kv.Value);
			}
			else if (kv.Value is List<String>) {
				String list = String.Join (",", ((List<String>)kv.Value).ToArray());
				outString = (String)outString.Replace ((String)kv.Key, list);
			}
		}
		temp = outString;
		parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		//Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

		return outString;
	}

	public bool EvaluateSkolemConstants(EvaluationPass pass) {
		Hashtable temp = new Hashtable ();
		Regex regex = new Regex (argVarPrefix+@"[0-9]+");
		Match argsMatch;
		Hashtable predArgs;
		List<object> objs = new List<object>();
		Queue<String> argsStrings;
		bool doSkolemReplacement = false;
		Triple<String,String,String> replaceSkolems = null;

		foreach (DictionaryEntry kv in skolems) {
			Debug.Log (kv.Key + " : " + kv.Value); 
			objs.Clear ();
			if (kv.Value is String) {
				Debug.Log (kv.Value); 
				argsMatch = regex.Match ((String)kv.Value);
				Debug.Log (argsMatch); 
				if (argsMatch.Groups [0].Value.Length == 0) {	// matched an empty string = no match
					Debug.Log (kv.Value);
					predArgs = Helper.ParsePredicate ((String)kv.Value);
					String pred = Helper.GetTopPredicate ((String)kv.Value);
					if (((String)kv.Value).Count (f => f == '(') +	// make sure actually a predicate
						((String)kv.Value).Count (f => f == ')') >= 2) {
						argsStrings = new Queue<String> (((String)predArgs [pred]).Split (new char[] {','}));
						while (argsStrings.Count > 0) {
							object arg = argsStrings.Dequeue ();

							if (Helper.v.IsMatch ((String)arg)) {	// if arg is vector form
								objs.Add (Helper.ParsableToVector ((String)arg));
							}
							else if (arg is String) {	// if arg is String
								if ((arg as String).Count (f => f == '(') +	// not a predicate
								    (arg as String).Count (f => f == ')') == 0) {
									//if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {	// if predicate not going to return string (as in "AS")
									List<GameObject> matches = new List<GameObject> ();
									foreach (Voxeme voxeme in objSelector.allVoxemes) {
										if (voxeme.voxml.Lex.Pred.Equals(arg)) {
											matches.Add (voxeme.gameObject);
										}
									}

									if (matches.Count == 0) {
										if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {	// if predicate not going to return string (as in "AS")
											GameObject go = GameObject.Find (arg as String);
											if (go == null) {
												for (int i = 0; i < objSelector.disabledObjects.Count; i++) {
													if (objSelector.disabledObjects[i].name == (arg as String)) {
														go = objSelector.disabledObjects[i];
														break;
													}
												}

												if (go == null) {
													OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
													return false;	// abort
												}
											}
											objs.Add (go);
										}
									}
									else if (matches.Count == 1) {
										if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {	// if predicate not going to return string (as in "AS")
											GameObject go = matches [0];
											if (go == null) {
												for (int i = 0; i < objSelector.disabledObjects.Count; i++) {
													if (objSelector.disabledObjects[i].name == (arg as String)) {
														go = objSelector.disabledObjects[i];
														break;
													}
												}

												if (go == null) {
													OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
													return false;	// abort
												}
											}
											objs.Add (go);
											doSkolemReplacement = true;
											replaceSkolems = new Triple<String,String,String> (kv.Key as String, arg as String, go.name);
											//skolems[kv] = go.name;
										}
										else {
											objs.Add (matches[0]);
										}
									}
									else {
										//Debug.Log (string.Format ("Which {0}?", (arg as String)));
										//OutputHelper.PrintOutput (Role.Affector,string.Format("Which {0}?", (arg as String)));
										//return false;	// abort
										foreach (GameObject match in matches) {
											objs.Add (match);
										}
									}
									//}
								}

								if (objs.Count == 0) {
									Regex q = new Regex ("[\'\"].*[\'\"]");
									int i;
									if ((q.IsMatch (arg as String)) || (int.TryParse(arg as String, out i))) {
										objs.Add (arg as String);
									}
									else {
										objs.Add (GameObject.Find (arg as String));
									}
								}
							}
						}

						methodToCall = preds.GetType ().GetMethod (pred.ToUpper());

						if (methodToCall == null) {
							OutputHelper.PrintOutput (Role.Affector,"Sorry, what does " + "\"" + pred + "\" mean?");
							return false;
						}

						if (pass == EvaluationPass.Attributes) {
							if ((methodToCall.ReturnType == typeof(String)) ||  (methodToCall.ReturnType == typeof(List<String>))) {
								Debug.Log ("EvaluateSkolemConstants: invoke " + methodToCall.Name);
								object obj = methodToCall.Invoke (preds, new object[]{ objs.ToArray () });
								Debug.Log (obj);

								temp [kv.Key] = obj;
							}
						}
						else if (pass == EvaluationPass.RelationsAndFunctions) {
							if (methodToCall.ReturnType == typeof(Vector3)) {
								Debug.Log ("EvaluateSkolemConstants: invoke " + methodToCall.Name);
								object obj = methodToCall.Invoke (preds, new object[]{ objs.ToArray () });
								Debug.Log (obj);

								temp [kv.Key] = obj;
							}
						}
					}
				}
				else {
					temp [kv.Key] = kv.Value;
				}
			}
		}

		// replace improperly named arguments
		if (doSkolemReplacement) {
			skolems [replaceSkolems.Item1] = ((String)skolems [replaceSkolems.Item1]).Replace (replaceSkolems.Item2, replaceSkolems.Item3);
		}

		//Helper.PrintKeysAndValues(skolems);

//		for (int i = 0; i < temp.Count; i++) {
//			Debug.Log (temp [i]);
//		}

		foreach (DictionaryEntry kv in temp) {
		//for (int i = 0; i < temp.Count; i++) {
			//DictionaryEntry kv = (DictionaryEntry)temp [i];
			//Debug.Log (kv.Value);
			String matchVal = kv.Value as String;
			if (matchVal == null) {
				matchVal = @"DEADBEEF";
			}
			argsMatch = regex.Match (matchVal);
			if (argsMatch.Groups [0].Value.Length > 0) {
				Debug.Log (argsMatch.Groups [0]);
				if (temp.ContainsKey (argsMatch.Groups [0].Value)) {
					object replaceWith = temp [(String)argsMatch.Groups [0].Value];
					Debug.Log (replaceWith.GetType ());
					//String replaced = ((String)skolems [kv.Key]).Replace ((String)argsMatch.Groups [0].Value,
					//	replaceWith.ToString ().Replace (',', ';').Replace ('(', '<').Replace (')', '>'));
					if (regex.Match ((String)replaceWith).Length == 0) {
						String replaced = (String)argsMatch.Groups [0].Value;
						if (replaceWith is String) {
							replaced = ((String)skolems [kv.Key]).Replace ((String)argsMatch.Groups [0].Value, (String)replaceWith);
						} else if (replaceWith is Vector3) {
							replaced = ((String)skolems [kv.Key]).Replace ((String)argsMatch.Groups [0].Value, Helper.VectorToParsable ((Vector3)replaceWith));
						}
						Debug.Log (replaced);
						//if (replace is Vector3) {
						skolems [kv.Key] = replaced;
					}
				}
			}
			else {
				skolems [kv.Key] = temp [kv.Key];
			}
		}

		Helper.PrintKeysAndValues(skolems);

		int newEvaluations = 0;
		foreach (DictionaryEntry kv in skolems) {
			Debug.Log(kv.Key + " : " + kv.Value);
			if (kv.Value is String) {
				argsMatch = r.Match ((String)kv.Value);

				if (argsMatch.Groups [0].Value.Length > 0) {
					string pred = argsMatch.Groups [0].Value.Split ('(') [0];
					Debug.Log (pred);
					methodToCall = preds.GetType ().GetMethod (pred.ToUpper());
					Debug.Log (methodToCall);

					if (methodToCall != null) {
						if (((methodToCall.ReturnType == typeof(String)) || (methodToCall.ReturnType == typeof(List<String>))) &&
							(pass == EvaluationPass.Attributes)) {
							newEvaluations++;
						}
						if ((methodToCall.ReturnType == typeof(Vector3)) && (pass == EvaluationPass.RelationsAndFunctions)) {
							newEvaluations++;
						}
					}
				}
			}
		}

		//Debug.Log (newEvaluations);
		if (newEvaluations > 0) {
			EvaluateSkolemConstants (pass);
		}

		//Helper.PrintKeysAndValues(skolems);

		return true;
	}
}
