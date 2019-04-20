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

public class EventReferentArgs : EventArgs {

    public object Referent { get; set; }

    public EventReferentArgs(object referent)
    {
        this.Referent = referent;
    }
}

public class EventDisambiguationArgs : EventArgs {

    public string Event { get; set; }
    public string AmbiguityStr { get; set; }
    public string AmbiguityVar { get; set; }
    public object[] Candidates { get; set; }

    public EventDisambiguationArgs(string eventStr, string ambiguityStr, string ambiguityVar, object[] candidates)
    {
        this.Event = eventStr;
        this.AmbiguityStr = ambiguityStr;
        this.AmbiguityVar = ambiguityVar;
        this.Candidates = candidates;
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

    public ReferentStore referents;   // TODO: make agent-specific
    //public List<object> antecedents = new List<object>();

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

    public event EventHandler EntityReferenced;

    public void OnEntityReferenced(object sender, EventArgs e)
    {
        if (EntityReferenced != null)
        {
            EntityReferenced(this, e);
        }
    }

    public event EventHandler NonexistentEntityError;

    public void OnNonexistentEntityError(object sender, EventArgs e)
    {
        if (NonexistentEntityError != null)
        {
            NonexistentEntityError(this, e);
        }
    }

    public event EventHandler DisambiguationError;

    public void OnDisambiguationError(object sender, EventArgs e)
    {
        if (DisambiguationError != null)
        {
            DisambiguationError(this, e);
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
		objSelector = GameObject.Find ("VoxWorld").GetComponent<ObjectSelector> ();
		inputController = GameObject.Find ("IOController").GetComponent<InputController> ();
        referents = gameObject.GetComponent<ReferentStore>();

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

	string completedEvent = "";

	// Update is called once per frame
	void Update () {
		if (stayExecution) {
			stayExecution = false;
			return;
		}

		if (events.Count > 0) {
            bool q = SatisfactionTest.IsSatisfied (events [0]);

			if (q) {
				GameObject.Find ("VoxWorld").GetComponent<AStarSearch> ().path.Clear ();
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
                        string pred = Helper.GetTopPredicate(completedEvent);
                        MethodInfo method = preds.GetType().GetMethod(pred.ToUpper());
                        if ((method != null) && (method.ReturnType == typeof(void))) {    // is a program
                            Debug.Log(string.Format("Completed {0}", completedEvent));
                            EventManagerArgs eventArgs = new EventManagerArgs(completedEvent);
                            OnEventComplete(this, eventArgs);
                        }
					}
				}
			}
		}
		else {
		}
	}

	public void RemoveEvent(int index) {
		Debug.Log (string.Format("Removing event@{0}: {1}",index,events[index]));
        EventManagerArgs lastEventArgs = null;

        //Debug.Log(evalOrig.Count);
        //if (evalOrig.Count > 0)
        //{
        //    Debug.Log(evalOrig.Keys.ToList()[0]);
        //}
        if (evalOrig.ContainsKey(events[index])) {
            lastEventArgs = new EventManagerArgs(events[index]);
            //Debug.Log(lastEventArgs.EventString);
        }

		events.RemoveAt (index);

		if (events.Count == 0) {
            OnQueueEmpty (this, lastEventArgs);
		}
	}

	public void InsertEvent(String commandString, int before) {
		//Debug.Break ();
		Debug.Log (string.Format("Inserting@{0}: {1}",before,commandString));
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

		if (SatisfactionTest.ComputeSatisfactionConditions (events [0])) {
			ExecuteCommand (events [0]);
		}
		else {
			RemoveEvent (0);
		}
    }

    public bool EvaluateCommand(String command) {
		ClearRDFTriples ();
		ClearSkolems ();
		ParseCommand (command);

        string globalsApplied = ApplyGlobals (command);

        FinishSkolemization();
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

	public List<object> ExtractObjects (String pred, String predArg)
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
                            Debug.Log(arg as String);
							List<GameObject> matches = new List<GameObject> ();
							foreach (Voxeme voxeme in objSelector.allVoxemes) {
								if (voxeme.voxml.Lex.Pred.Equals (arg as String)) {
                                    Debug.Log(voxeme.gameObject);
                                    matches.Add (voxeme.gameObject);
								}
							}
							if (matches.Count <= 1) {
                                Debug.Log(arg as String);
                                if (!(arg as String).Contains('('))
                                {
                                    GameObject go = GameObject.Find(arg as String);
                                    Debug.Log(go);
                                    if (go == null)
                                    {
                                        for (int j = 0; j < objSelector.disabledObjects.Count; j++)
                                        {
                                            if (objSelector.disabledObjects[j].name == (arg as String))
                                            {
                                                go = objSelector.disabledObjects[j];
                                                break;
                                            }
                                        }

                                        if (go == null)
                                        {
                                            //OutputHelper.PrintOutput(Role.Affector, string.Format("What is {0}?", (arg as String)));
                                            OnNonexistentEntityError(this, new EventReferentArgs(arg as String));
                                            return objs;
                                            //throw new ArgumentNullException("Couldn't resolve the object");
                                            // abort
                                        }
                                    }
                                    else {
                                        if (go is GameObject) {
                                            if ((go as GameObject).GetComponent<Voxeme>() != null) {
                                                if ((referents.stack.Count == 0) || (!referents.stack.Peek().Equals(((GameObject)go).name))) {
                                                    referents.stack.Push(((GameObject)go).name);
                                                }
                                                OnEntityReferenced(this, new EventReferentArgs(((GameObject)go).name));
                                            }
                                        }
                                    }
                                    objs.Add (go);
                                }
                                else {
                                    List<object> args = ExtractObjects(Helper.GetTopPredicate(arg as String),
                                        (String)Helper.ParsePredicate(arg as String)[
                                        Helper.GetTopPredicate(arg as String)]);
                                            
                                    foreach (object o in args) {
                                        if (o is GameObject) {
                                            if ((o as GameObject).GetComponent<Voxeme>() != null) {
                                                if ((referents.stack.Count == 0) || (!referents.stack.Peek().Equals(((GameObject)o).name))) {
                                                    referents.stack.Push(((GameObject)o).name);
                                                }
                                                OnEntityReferenced(this, new EventReferentArgs(((GameObject)o).name));
                                            }
                                        }
                                        objs.Add(o);
                                    }
                                }
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

                if (methodToCall != null) { // found a method
                    if (methodToCall.ReturnType == typeof(void)) { // is it a program?
                        foreach (var obj in objs) {
                            if (obj is GameObject) {
                                if ((obj as GameObject).GetComponent<Voxeme>() != null) {
                                    if ((referents.stack.Count == 0) || (!referents.stack.Peek().Equals(((GameObject)obj).name))) {
                                        referents.stack.Push(((GameObject)obj).name);
                                    }
                                    OnEntityReferenced(this, new EventReferentArgs(((GameObject)obj).name));
                                }
                            }
                        }
                   }
                }

				if (preds.rdfTriples.Count > 0) {
                    if (methodToCall != null) { // found a method
                        if (methodToCall.ReturnType == typeof(void)) { // is it a program?
                            Debug.Log("========================== ExecuteCommand ============================ " + evaluatedCommand);
                            Debug.Log("ExecuteCommand: invoke " + methodToCall.Name);
                            Debug.Log(string.Format("{0} : {1}", evaluatedCommand, Helper.VectorToParsable((objs[0] as GameObject).GetComponent<Voxeme>().targetPosition)));
                            object obj = methodToCall.Invoke(preds, new object[] { objs.ToArray() });
                            OnExecuteEvent(this, new EventManagerArgs(evaluatedCommand));
                        }
                        else {  // not a program
                            object obj = methodToCall.Invoke(preds, new object[] { objs.ToArray() });
                            Debug.Log(string.Format("{0}:{1}",obj.ToString(),obj.GetType().ToString()));
                            if (obj.ToString() == string.Empty) {
                                OnNonexistentEntityError(this,
                                    new EventReferentArgs(new Pair<string, List<object>>(pred, objs.GetRange(0,objs.Count-1))));
                            }
                            //else {
                            //    if ((referents.stack.Count == 0) || (!referents.stack.Peek().Equals(obj))) {
                            //        referents.stack.Push(obj);
                            //    }
                            //    OnEntityReferenced(this, new EventReferentArgs(obj));
                            //}
                        }
					}
					else {
						if (File.Exists (Data.voxmlDataPath + string.Format ("/programs/{0}.xml", pred))) {
							using (StreamReader sr = new StreamReader (Data.voxmlDataPath + string.Format ("/programs/{0}.xml", pred))) {
								preds.ComposeSubevents (VoxML.LoadFromText (sr.ReadToEnd ()), objs.ToArray ());
							}
						}
					}
				}
            }
            catch (ArgumentNullException e){
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
        evalOrig.Clear();
        evalResolved.Clear();
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
						if (((String)kkv.Value).Contains ((String)kv.Value) && 
                            ((((String)kkv.Value).Count(f => f == '(') + ((String)kkv.Value).Count(f => f == ')')) - 
                            (((String)kv.Value).Count(f => f == '(') + ((String)kv.Value).Count(f => f == ')')) == 2)) {
							Debug.Log ("FinishSkolemization: " + kv.Value + " found in " + kkv.Value);
							Debug.Log ("FinishSkolemization: " + kkv.Key + " : " + ((String)kkv.Value).Replace ((String)kv.Value, (String)kv.Key));
							temp [kkv.Key] = ((String)kkv.Value).Replace ((String)kv.Value, (String)kv.Key);
                            Debug.Log("FinishSkolemization: " + temp[kkv.Key]);
                        }
                    }
				}
			}
		}

		foreach (DictionaryEntry kv in temp) {
            Debug.Log("FinishSkolemization: " + temp[kv.Key]);
            skolems[kv.Key] = temp[kv.Key];
            Debug.Log("FinishSkolemization: " + skolems[kv.Key]);
        }

        Helper.PrintKeysAndValues(skolems);
	}

	public String Skolemize(String inString) {
		String outString = inString;
		String temp = inString;

		int parenCount = temp.Count(f => f == '(') + 
			temp.Count(f => f == ')');
		Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

        do
        {
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

                                    if (GameObject.Find(arg as String) != null) {
                                        matches.Add(GameObject.Find(arg as String));
                                    }
                                    else {
                                        foreach (Voxeme voxeme in objSelector.allVoxemes) {
                                            if (voxeme.voxml.Lex.Pred.Equals(arg)) {
                                                matches.Add(voxeme.gameObject);
                                            }
                                        }
                                    }
                                    Debug.Log(string.Format("{0} matches", matches.Count));

									if (matches.Count == 0) {
                                        Debug.Log(arg as String);
                                        Debug.Log(preds.GetType().GetMethod(pred.ToUpper()).ReturnType);
                                        //if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {	// if predicate not going to return string (as in "AS")
											GameObject go = GameObject.Find (arg as String);
                                            Debug.Log(go);
											if (go == null) {
												for (int i = 0; i < objSelector.disabledObjects.Count; i++) {
													if (objSelector.disabledObjects[i].name == (arg as String)) {
														go = objSelector.disabledObjects[i];
														break;
													}
												}

                                                Debug.Log(go);
                                                if (go == null) {
													//OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
                                                    OnNonexistentEntityError(this, new EventReferentArgs(arg as String));
													return false;	// abort
												}
											}
											objs.Add (go);
										//}
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
													//OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
                                                    OnNonexistentEntityError(this,
                                                        new EventReferentArgs(new Pair<string, List<GameObject>>(pred, matches)));
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
                                        // if predicate arity of enclosing predicate as encoded in VoxML != matches.Count
                                        VoxML predVoxeme = new VoxML();
                                        String path = string.Empty;
                                        Debug.Log(pred);
                                        if (File.Exists(Data.voxmlDataPath + string.Format("/programs/{0}.xml", pred))) {
                                            path = string.Format("/programs/{0}.xml", pred);
                                        }
                                        else if (File.Exists(Data.voxmlDataPath + string.Format("/relations/{0}.xml", pred))) {
                                            path = string.Format("/relations/{0}.xml", pred);
                                        }
                                        else if (File.Exists(Data.voxmlDataPath + string.Format("/functions/{0}.xml", pred))) {
                                            path = string.Format("/functions/{0}.xml", pred);
                                        }

                                        if (path != string.Empty) {
                                            using (StreamReader sr = new StreamReader(Data.voxmlDataPath + path)) {
                                                predVoxeme = VoxML.LoadFromText(sr.ReadToEnd());
                                            }

                                            Debug.Log(predVoxeme);
                                            if (path.Contains("functions")) {
                                                Debug.Log(predVoxeme.Type.Mapping);
                                                int arity;
                                                bool isInt = Int32.TryParse(predVoxeme.Type.Mapping.Split(':')[1],out arity);

                                                if (isInt) {
                                                    Debug.Log(string.Format("{0} : {1} : {2}", pred.ToUpper(), arity, matches.Count));

                                                    if (arity != matches.Count) {
                                                        OnDisambiguationError(this, new EventDisambiguationArgs(events[0], (String)kv.Value, "{0}",
                                                            matches.Select(o => o.GetComponent<Voxeme>()).ToArray()));
                                                        return false;   // abort
                                                    }
                                                }
                                            }
                                            else {
                                                int arity = predVoxeme.Type.Args.Count - 1;
                                                Debug.Log(string.Format("{0} : {1} : {2}", pred.ToUpper(), arity, matches.Count));

                                                if (arity != matches.Count) {
                                                    //Debug.Log(string.Format("Which {0}?", (arg as String)));
                                                    //OutputHelper.PrintOutput(Role.Affector, string.Format("Which {0}?", (arg as String)));
                                                    OnDisambiguationError(this, new EventDisambiguationArgs(events[0], (String)kv.Value,
                                                        ((String)kv.Value).Replace(arg as String, "{0}"),
                                                        matches.Select(o => o.GetComponent<Voxeme>()).ToArray()));
                                                    return false;   // abort
                                                }
                                            }
                                        }
                                        else {
										    foreach (GameObject match in matches) {
                                                //Debug.Log(match);
											    objs.Add (match);
    										}
    									}
									}
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

                                if (obj is String) {
                                    if ((obj as String).Length == 0) {
                                        OnNonexistentEntityError(this,
                                            new EventReferentArgs(new Pair<string, List<object>>(pred, objs)));
                                        return false;
                                    }

                                    //if ((referents.stack.Count == 0) || (!referents.stack.Peek().Equals(obj))) {
                                    //    referents.stack.Push(obj);
                                    //}
                                    //OnEntityReferenced(this, new EventReferentArgs(obj));
                                }
								temp [kv.Key] = obj;
							}
						}
						else if (pass == EvaluationPass.RelationsAndFunctions) {
                            if ((methodToCall.ReturnType == typeof(Vector3)) || (methodToCall.ReturnType == typeof(object))) {
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
				matchVal = @"DEADBEEF"; // dummy val
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
