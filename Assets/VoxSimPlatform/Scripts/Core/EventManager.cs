using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Global;
using VoxSimPlatform.Pathfinding;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Core {
        public class EventManagerArgs : EventArgs {
            // TODO: transition this over to take a VoxML encoding as the argument
            public VoxML VoxML { get; set; }
            public string EventString { get; set; }
            public bool MacroEvent { get; set; }

            public EventManagerArgs(string str, bool macroEvent = false) {
                this.EventString = str;
                this.MacroEvent = macroEvent;
            }

            public EventManagerArgs(VoxML voxml, string eventStr) {
                this.VoxML = voxml;
                this.EventString = eventStr;
                this.MacroEvent = (voxml.Type.Body.Count > 1);
            }
        }

        public class EventReferentArgs : EventArgs {
            public object Referent { get; set; }

            public EventReferentArgs(object referent) {
                this.Referent = referent;
            }
        }

        public class EventDisambiguationArgs : EventArgs {
            public string Event { get; set; }
            public string AmbiguityStr { get; set; }
            public string AmbiguityVar { get; set; }
            public object[] Candidates { get; set; }

            public EventDisambiguationArgs(string eventStr, string ambiguityStr, string ambiguityVar, object[] candidates) {
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

            public OrderedDictionary eventsStatus = new OrderedDictionary();
            public ObjectSelector objSelector;
            public VoxMLLibrary voxmlLibrary;
            public InputController inputController;

            public string lastParse = string.Empty;

            public ObservableCollection<String> events = new ObservableCollection<String>();
            public List<string> inspectableEventsList = new List<string>();
            public ObservableCollection<String> eventHistory = new ObservableCollection<String>();
            public List<string> inspectableEventHistory = new List<string>();

            // a dictionary containing
            //  Key: the evaluated form of an event string
            //  Value: the original event string resulting in that evaluated form
            public Dictionary<String, String> evalOrig = new Dictionary<String, String>();

            // a dictionary containing
            //  Key: an event string with all objects resolved to unique names (e.g. the(red(block)) -> block6)
            //  Value: the original event string resulting in that object-resolved form
            public Dictionary<String, String> evalResolved = new Dictionary<String, String>();

            // variables and their assigned values that should hold over an entire complex event or event sequence
            public Hashtable macroVars = new Hashtable();

            public ReferentStore referents;

            // activeAgent is used to swap around the referents in agent-specific way
            // TODO: make event-manager agent specific; this is just there for now to route commands correctly
            public GameObject activeAgent;

            public double eventWaitTime = 2000.0;
            Timer eventWaitTimer;
            bool eventWaitCompleted = false;

            string skolemized, evaluated;
            public Predicates preds;
            String nextQueuedEvent = "";
            int argVarIndex = 0;
            Hashtable skolems = new Hashtable();
            string argVarPrefix = @"_ARG";
            String nextIncompleteEvent;

            MethodInfo _methodToCall;
            public MethodInfo methodToCall {
                get { return _methodToCall; }
                set {
                    if (_methodToCall != value) {
                        OnMethodToCallChanged(_methodToCall, value);
                    }
                    _methodToCall = value;
                }
            }

            bool _stayExecution = false;
            public bool stayExecution {
                get { return _stayExecution; }
                set {
                    Debug.Log(string.Format("==================== stayExecution flag changed ==================== {0}: {1}->{2}",
                        (events.Count > 0) ? events[0] : "NULL", _stayExecution, value));
                    _stayExecution = value;
                }
            }

            public enum EvaluationPass {
                Attributes,
                RelationsAndFunctions
            }

            public bool immediateExecution = true;

            public event EventHandler ObjectsResolved;

            public void OnObjectsResolved(object sender, EventArgs e) {
                if (ObjectsResolved != null) {
                    ObjectsResolved(this, e);
                }
            }

            public event EventHandler EntityReferenced;

            public void OnEntityReferenced(object sender, EventArgs e) {
                if (EntityReferenced != null) {
                    EntityReferenced(this, e);
                }
            }

            public event EventHandler NonexistentEntityError;

            public void OnNonexistentEntityError(object sender, EventArgs e) {
                if (NonexistentEntityError != null) {
                    NonexistentEntityError(this, e);
                }
            }

            public event EventHandler DisambiguationError;

            public void OnDisambiguationError(object sender, EventArgs e) {
                if (DisambiguationError != null) {
                    DisambiguationError(this, e);
                }
            }

            public event EventHandler SatisfactionCalculated;

            public void OnSatisfactionCalculated(object sender, EventArgs e) {
                if (SatisfactionCalculated != null) {
                    SatisfactionCalculated(this, e);
                }
            }

            public event EventHandler ExecuteEvent;

            public void OnExecuteEvent(object sender, EventArgs e) {
                if (ExecuteEvent != null) {
                    ExecuteEvent(this, e);
                }
            }

            public event EventHandler EventComplete;

            public void OnEventComplete(object sender, EventArgs e) {
                if (EventComplete != null) {
                    EventComplete(this, e);
                }
            }

            public event EventHandler QueueEmpty;

            public void OnQueueEmpty(object sender, EventArgs e) {
                if (QueueEmpty != null) {
                    QueueEmpty(this, e);
                }
            }

            public event EventHandler ForceClear;

            public void OnForceClear(object sender, EventArgs e) {
                if (ForceClear != null) {
                    ForceClear(this, e);
                }
            }

            // Just getters/setters for the active agent
            public void SetActiveAgent(String name) {
                GameObject temp = GameObject.Find(name);
                if (temp != null) {
                    activeAgent = GameObject.Find(name);
                    referents = activeAgent.GetComponent<ReferentStore>();
                }
            }

            public void SetActiveAgent(GameObject agent) {
                if (agent != null) {
                    activeAgent = agent;
                    referents = activeAgent.GetComponent<ReferentStore>();
                }
            }

            public GameObject GetActiveAgent() {
                return activeAgent;
            }

            // Use this for initialization
            void Start() {
                preds = gameObject.GetComponent<Predicates>();
                objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
                voxmlLibrary = GameObject.Find("VoxWorld").GetComponent<VoxMLLibrary>();
                inputController = GameObject.Find("IOController").GetComponent<InputController>();

                // Deprecated. referents should be set from whatever is the activeAgent. But that only happens inf activeAgent exists
                //referents = gameObject.GetComponent<ReferentStore>(); 

                inputController.ParseComplete += StoreParse;
                inputController.ParseComplete += ClearGlobalVars;
                //inputController.InputReceived += StartEventWaitTimer;

                events.CollectionChanged += OnEventsListChanged;
                eventHistory.CollectionChanged += OnExecutedEventHistoryChanged;
                
                //eventWaitTimer = new Timer (eventWaitTime);
                //eventWaitTimer.Enabled = false;
                //eventWaitTimer.Elapsed += ExecuteNextEvent;
            }
                
            string completedEvent = "";

            // Update is called once per frame
            void Update() {
                if (events.Count > 0) {
                    bool q = SatisfactionTest.IsSatisfied(events[0]);

                    if (q) {
                        Debug.Log("Satisfied " + events[0]);

                        completedEvent = events[0];
                        eventHistory.Add(completedEvent);

                        for (int i = 0; i < events.Count - 1; i++) {
                            events[i] = events[i + 1];
                        }
                            
                        RemoveEvent(events.Count - 1);
                    
                        if (events.Count > 0) {
                            ExecuteNextCommand();
                        }
                        else {
                            if (OutputHelper.GetCurrentOutputString(Role.Affector) != "I'm sorry, I can't do that.") {
                                //OutputHelper.PrintOutput (Role.Affector, "OK, I did it.");
                                string pred = Helper.GetTopPredicate(completedEvent);
                                MethodInfo method = preds.GetType().GetMethod(pred.ToUpper());
                                if ((method != null) && (method.ReturnType == typeof(void))) {
                                    EventManagerArgs eventArgs = null;
                                    // is a program
                                    Debug.Log(string.Format("Completed {0}", completedEvent));
                                    if (voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) && 
                                        voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") {
                                    //string testPath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", pred));
                                    //if (File.Exists(testPath)) {
                                        VoxML voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                        //using (StreamReader sr = new StreamReader(testPath)) {
                                        //    voxml = VoxML.LoadFromText(sr.ReadToEnd(), pred);
                                        //}
                                        eventArgs = new EventManagerArgs(voxml, completedEvent);
                                    }
                                    else {
                                       eventArgs = new EventManagerArgs(completedEvent);
                                    }
                                    OnEventComplete(this, eventArgs);
                                    // is a program
                                    //Debug.Log(string.Format("Completed {0}", completedEvent));
                                    //EventManagerArgs eventArgs = new EventManagerArgs(completedEvent);
                                    //OnEventComplete(this, eventArgs);
                                }
                            }
                        }
                    }
                    else if (stayExecution) {
                        stayExecution = false;
                        if (events.Count > 0) {
                            ExecuteNextCommand();
                        }
                        else {
                            if (OutputHelper.GetCurrentOutputString(Role.Affector) != "I'm sorry, I can't do that.") {
                                //OutputHelper.PrintOutput (Role.Affector, "OK, I did it.");
                                string pred = Helper.GetTopPredicate(completedEvent);
                                MethodInfo method = preds.GetType().GetMethod(pred.ToUpper());
                                if ((method != null) && (method.ReturnType == typeof(void))) {
                                    EventManagerArgs eventArgs = null;
                                    // is a program
                                    Debug.Log(string.Format("Completed {0}", completedEvent));
                                    if (voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) && 
                                        voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") {
                                    //string testPath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", pred));
                                    //if (File.Exists(testPath)) {
                                        VoxML voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                        //using (StreamReader sr = new StreamReader(testPath)) {
                                        //    voxml = VoxML.LoadFromText(sr.ReadToEnd(), pred);
                                        //}
                                        eventArgs = new EventManagerArgs(voxml, completedEvent);
                                    }
                                    else {
                                       eventArgs = new EventManagerArgs(completedEvent);
                                    }
                                    OnEventComplete(this, eventArgs);
                                    // is a program
                                    //Debug.Log(string.Format("Completed {0}", completedEvent));
                                    //EventManagerArgs eventArgs = new EventManagerArgs(completedEvent);
                                    //OnEventComplete(this, eventArgs);
                                }
                            }
                        }
                    }
                }
                else {
                }
            }

            public void RemoveEvent(int index) {
                Debug.Log(string.Format("Removing event@{0}: {1}", index, events[index]));
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

                events.RemoveAt(index);

                if (events.Count == 0) {
                    OnQueueEmpty(this, lastEventArgs);
                }
            }

            public void InsertEvent(String commandString, int before) {
                //Debug.Break ();
                Debug.Log(string.Format("Inserting@{0}: {1}", before, commandString));
                events.Insert(before, commandString);
            }

            public void QueueEvent(String commandString) {
                // not using a Queue because I'm horrible
                Debug.Log(string.Format("Queueing@{0}: {1}", events.Count, commandString));
                events.Add(commandString);
            }

            public void StoreParse(object sender, EventArgs e) {
                lastParse = ((InputEventArgs) e).InputString;
            }

            public void ClearGlobalVars(object sender, EventArgs e) {
                Debug.Log("Clearing macroVars");
                macroVars.Clear();
            }

            public void WaitComplete(object sender, EventArgs e) {
                ((Timer) sender).Enabled = false;
        //        RemoveEvent (0);
        //        stayExecution = true;
            }

            public void PrintEvents() {
                Debug.Log(string.Format("Current events list: {0}", string.Join("\n", events.ToArray())));
            }

            void StartEventWaitTimer(object sender, EventArgs e) {
                eventWaitTimer.Enabled = true;
            }

            void ExecuteNextEvent(object sender, ElapsedEventArgs e) {
                //Debug.Log ("Event wait complete");
                eventWaitCompleted = true;
            }

            public void ExecuteNextCommand() {
                if (stayExecution) {
                    Debug.Log(string.Format("Deferring execution on {0}", events[0]));
                    return;
                }

                //PhysicsHelper.ResolveAllPhysicsDiscrepancies (false);
                Debug.Log("Next Command: " + events[0]);

                if (!EvaluateCommand(events[0])) {
                    return;
                }

                Hashtable predArgs = Helper.ParsePredicate(events[0]);
                String pred = Helper.GetTopPredicate(events[0]);

                if (SatisfactionTest.ComputeSatisfactionConditions(events[0])) {
                    ExecuteCommand(events[0]);
                }
                else {
                    RemoveEvent(0);
                }
            }

            public bool EvaluateCommand(String command) {
                ClearRDFTriples();
                ClearSkolems();

                ParseCommand(command);

                string globalsApplied = ApplyGlobals(command);
                Debug.Log("Command with global variables applied: " + globalsApplied);

                FinishSkolemization();
                skolemized = Skolemize(globalsApplied);
                Debug.Log("Skolemized command: " + skolemized);
                //EvaluateSkolemizedCommand(skolemized);

                if (!EvaluateSkolemConstants(EvaluationPass.Attributes)) {
                    RemoveEvent(events.Count - 1);
                    return false;
                }

                string objectResolved = ApplySkolems(skolemized);
        //        Debug.Log (objectResolved);

                if (objectResolved != command) {
                    OnObjectsResolved(this, new EventManagerArgs(objectResolved));
                }

                if (events.IndexOf(command) < 0) {
                    return false;
                }

                if (!EvaluateSkolemConstants(EvaluationPass.RelationsAndFunctions)) {
                    RemoveEvent(events.Count - 1);
                    return false;
                }

                Debug.Log(string.Format("Skolemized command@{0}: {1}", events.IndexOf(command), skolemized));
                evaluated = ApplySkolems(skolemized);
                Debug.Log(string.Format("Evaluated command@{0}: {1}", events.IndexOf(command), evaluated));
                if (!evalOrig.ContainsKey(evaluated)) {
                    evalOrig.Add(evaluated, command);
                }
                else {
                    evalOrig[evaluated] = command;
                }

                if (!evalResolved.ContainsKey(evaluated)) {
                    evalResolved.Add(evaluated, objectResolved);
                }
                else {
                    evalResolved[evaluated] = objectResolved;
                }

                events[events.IndexOf(command)] = evaluated;

                Triple<String, String, String> triple = Helper.MakeRDFTriples(evalResolved[evaluated]);
                Debug.Log(string.Format("Event string {0} with skolems resolved -> {1}",evalOrig[evaluated],evalResolved[evaluated]));
                Debug.Log(triple.Item1 + " " + triple.Item2 + " " + triple.Item3);

                if (triple.Item1 != "" && triple.Item2 != "" && triple.Item3 != "") {
                    preds.rdfTriples.Add(triple);
                    Helper.PrintRDFTriples(preds.rdfTriples);
                }
                else {
                    Debug.Log("Failed to make valid RDF triple");
                }

                //OnExecuteEvent (this, new EventManagerArgs (evaluated));

                return true;
            }

            public List<object> ExtractObjects(String pred, String predArg) {
                List<object> objs = new List<object>();
                Queue<String> argsStrings = new Queue<String>(predArg.Split(new char[] {
                    ','
                }));
                    
                // Match referent stack to whoever is being talked to
                if (GetActiveAgent() != null) {
                    referents = GetActiveAgent().GetComponent<ReferentStore>();
                }

                while (argsStrings.Count > 0) {
                    object arg = argsStrings.Dequeue();
                    if (Helper.vec.IsMatch((String) arg)) {
                        if (Helper.listVec.IsMatch((String) arg)) {
                            // if arg is list of vectors form
                            List<Vector3> vecList = new List<Vector3>();

                            foreach (string vecString in ((String) arg).Replace("[","").Replace("]","").Split(':')) {
                                vecList.Add(Helper.ParsableToVector(vecString));
                            }
                            Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,vecList));
                            objs.Add(vecList);
                        }
                        else {
                            // if arg is vector form
                            Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,(String) arg));
                            objs.Add(Helper.ParsableToVector((String) arg));
                        }
                    }
                    else if (Helper.emptyList.IsMatch((String) arg)) {
                        Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,new List<object>()));
                        objs.Add(new List<object>());
                    }
                    else if (arg is String) {
                        // if arg is String
                        if ((arg as String) != string.Empty) {
                            Regex q = new Regex("[\'\"].*[\'\"]");
                            int i;
                            if (int.TryParse(arg as String, out i)) {
                                Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,i));
                                objs.Add(i);
                            }
                            else if (q.IsMatch(arg as String)) {
                                String[] tryMethodPath = (arg as String).Replace("\'",string.Empty)
                                    .Replace("\"",string.Empty).Split('.');

                                // Get the Type for the class
                                Type methodCallingType = Type.GetType(String.Join(".", tryMethodPath.ToList().GetRange(0, tryMethodPath.Length - 1)));
                                if (methodCallingType != null) {
                                    MethodInfo method = methodCallingType.GetMethod(tryMethodPath.Last());
                                    if (method != null) {
                                        Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,method));
                                        objs.Add(method);
                                    }
                                    else {
                                        Debug.Log(string.Format("No method {0} found in class {1}!",tryMethodPath.Last(),methodCallingType.Name));
                                    }
                                } 
                                else {
                                    Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,arg as String));
                                    objs.Add(arg as String);
                                }
                            }
                            else {
                                //Debug.Log(arg as String);
                                List<GameObject> matches = new List<GameObject>();
                                foreach (Voxeme voxeme in objSelector.allVoxemes) {
                                    if (voxeme.voxml.Lex.Pred.Equals(arg as String)) {
                                        //Debug.Log(voxeme.gameObject);
                                        matches.Add(voxeme.gameObject);
                                    }
                                }

                                if (matches.Count <= 1) {
                                    //Debug.Log(arg as String);
                                    if (!(arg as String).Contains('(')) {
                                        GameObject go = GameObject.Find(arg as String);
                                        //Debug.Log(go);
                                        if (go == null) {
                                            for (int j = 0; j < objSelector.disabledObjects.Count; j++) {
                                                if (objSelector.disabledObjects[j].name == (arg as String)) {
                                                    go = objSelector.disabledObjects[j];
                                                    break;
                                                }
                                            }

                                            if (go == null) {
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
                                                    if ((referents.stack.Count == 0) ||
                                                        (!referents.stack.Peek().Equals(go.name))) {
                                                        referents.stack.Push(go.name);
                                                    }

                                                    OnEntityReferenced(this, new EventReferentArgs(go.name));
                                                }
                                            }
                                        }

                                        Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,go));
                                        objs.Add(go);
                                    }
                                    else {
                                        List<object> args = ExtractObjects(Helper.GetTopPredicate(arg as String),
                                            (String) Helper.ParsePredicate(arg as String)[
                                                Helper.GetTopPredicate(arg as String)]);

                                        foreach (object o in args) {
                                            if (o is GameObject) {
                                                if ((o as GameObject).GetComponent<Voxeme>() != null) {
                                                    if ((referents.stack.Count == 0) ||
                                                        (!referents.stack.Peek().Equals(((GameObject) o).name))) {
                                                        referents.stack.Push(((GameObject) o).name);
                                                    }

                                                    OnEntityReferenced(this, new EventReferentArgs(((GameObject) o).name));
                                                }
                                            }

                                            Debug.Log(string.Format("ExtractObjects (predicate = \"{0}\"): extracted {1}",pred,o));
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

                objs.Add(true);
                methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                return objs;
            }

            public void ExecuteCommand(String evaluatedCommand) {
                Debug.Log("Executing command: " + evaluatedCommand);
                Hashtable predArgs = Helper.ParsePredicate(evaluatedCommand);
                String pred = Helper.GetTopPredicate(evaluatedCommand);

                // Match referent stack to whoever is being talked to
                if (GetActiveAgent() != null) {
                    referents = GetActiveAgent().GetComponent<ReferentStore>();
                }

                if (predArgs.Count > 0) {
                    try {
                        List<object> objs = new List<object>();
                        // found a method
                        if ((methodToCall != null) ||
                            ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) &&
                            (voxmlLibrary.VoxMLEntityTypeDict[pred] != "objects"))) {
                            //Debug.Log(pred);
                            //if (methodToCall != null) {
                            //    Debug.Log(methodToCall.Name);
                            //    Debug.Log(methodToCall.ReturnType);
                            //}
                            objs = ExtractObjects(pred, (String) predArgs[pred]);
                        }
                        else {
                            // if methodToCall is still null at this point
                            //  we might have to look for a conditional predicate to evaluate
                            methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                            if (methodToCall.ReturnType != typeof(bool)) {
                                methodToCall = null;
                            }
                            else {
                                Queue<String> argsStrings = new Queue<String>(((String) predArgs[pred]).Split(new char[] {','}));
                                while (argsStrings.Count > 0) {
                                    object arg = argsStrings.Dequeue();

                                    if (arg is String) {
                                        Debug.Log(string.Format("ExecuteCommand: adding {0} to objs",(String) arg));
                                        objs.Add(arg);
                                    }
                                }
                                objs.Add(true);
                            }
                        }

                        // found a method
                        if (methodToCall != null) {
                            // is it a program?
                            if (methodToCall.ReturnType == typeof(void)) {
                                foreach (object obj in objs) {
                                    if (obj is GameObject) {
                                        if ((obj as GameObject).GetComponent<Voxeme>() != null) {
                                            if ((referents.stack.Count == 0) ||
                                                (!referents.stack.Peek().Equals(((GameObject) obj).name))) {
                                                referents.stack.Push(((GameObject) obj).name);
                                            }

                                            OnEntityReferenced(this, new EventReferentArgs(((GameObject) obj).name));
                                        }
                                    }
                                }
                            }
                        }

                        if (preds.rdfTriples.Count > 0) {
                            if (methodToCall != null) {
                                // found a method
                                if (methodToCall.ReturnType == typeof(void)) {
                                    // is it a program?
                                    Debug.Log("========================== ExecuteCommand ============================ " +
                                              evaluatedCommand);
                                    Debug.Log("ExecuteCommand: invoke " + methodToCall.Name);
                                    Debug.Log(string.Format("{0} : {1}", evaluatedCommand,
                                        Helper.VectorToParsable((objs[0] as GameObject).GetComponent<Voxeme>()
                                            .targetPosition)));
                                    object obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                    OnExecuteEvent(this, new EventManagerArgs(evaluatedCommand));
                                }
                                else {
                                    // not a program
                                    object obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                    Debug.Log(string.Format("{0}:{1}", obj, obj.GetType()));
                                    if (obj.ToString() == string.Empty) {
                                        OnNonexistentEntityError(this,
                                            new EventReferentArgs(
                                                new Pair<string, List<object>>(pred, objs.GetRange(0, objs.Count - 1))));
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
                                if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) && 
                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs")) {
                                //if (File.Exists(Data.voxmlDataPath + string.Format("/programs/{0}.xml", pred))) {
                                    //using (StreamReader sr =
                                        //new StreamReader(Data.voxmlDataPath + string.Format("/programs/{0}.xml", pred))) {
                                        VoxML voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                        Debug.Log(string.Format("Invoke ComposeProgram with {0}{1}",
                                           (voxml == null) ? string.Empty : "\"" + voxml.Lex.Pred + "\", ", objs));
                                        preds.ComposeProgram(voxml, objs.ToArray());
                                    //}
                                }
                            }
                        }
                        else {
                            // could be a conditional
                            if (methodToCall != null) {
                                // found a method
                                if (methodToCall.ReturnType == typeof(bool)) {
                                    // is it a condition?
                                    Debug.Log("========================== ExecuteCommand ============================ " +
                                              evaluatedCommand);
                                    Debug.Log("ExecuteCommand: invoke " + methodToCall.Name);
                                    object obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                    OnExecuteEvent(this, new EventManagerArgs(evaluatedCommand));
                                }
                            }
                        }
                    }
                    catch (ArgumentNullException e) {
                        return;
                    }
                }
            }

            public void AbortEvent() {
                if (events.Count > 0) {
                    //InsertEvent ("satisfy()", 0);
                    InsertEvent("", 0);
                    RemoveEvent(1);
                    //RemoveEvent (0);
                }
            }

            public void ClearEvents() {
                events.Clear();
                evalOrig.Clear();
                evalResolved.Clear();
                OnForceClear(this, null);
            }

            String GetNextIncompleteEvent() {
                String[] keys = new String[eventsStatus.Keys.Count];
                bool[] values = new bool[eventsStatus.Keys.Count];

                eventsStatus.Keys.CopyTo(keys, 0);
                eventsStatus.Values.CopyTo(values, 0);

                String nextIncompleteEvent = "";
                for (int i = 0; i < keys.Length; i++) {
                    if ((bool) eventsStatus[keys[i]] == false) {
                        nextIncompleteEvent = keys[i];
                        if (i < events.Count - 1) {
                            SatisfactionTest.ComputeSatisfactionConditions(events[i + 1]);
                            eventsStatus.Keys.CopyTo(keys, 0);
                            eventsStatus.Values.CopyTo(values, 0);
                            nextQueuedEvent = keys[i + 1];
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
                skolems.Clear();
            }

            public void ClearRDFTriples() {
                preds.rdfTriples.Clear();
            }

            public void ParseCommand(String command) {
                Hashtable predArgs;
                String predString = null;
                List<String> argsStrings = null;

                if (Helper.pred.IsMatch(command)) {   // if command matches predicate form
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

                    // get the main predicate and its argument
                    Debug.Log(string.Format("Parsing predicate: {0}", command));
                    predArgs = Helper.ParsePredicate(command);

                    // foreach key-value pair
                    foreach (DictionaryEntry entry in predArgs) {
                        // pred string is the key
                        predString = (String) entry.Key;

                        Debug.Log(string.Format("{0} : {1}", entry.Key, entry.Value));

                        // split the args at delimiters/operators, assuming they don't fall inside another subpredicate
                        // 1. check the VoxML entity type dictionary to see what entity type this predicate signifies
                        // 2. split appropriately

                        if (voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(Helper.GetTopPredicate((String)entry.Value))) {
                            // look in VoxML entity type dictionary
                            string predType = voxmlLibrary.VoxMLEntityTypeDict[Helper.GetTopPredicate((String)entry.Value)];
                            Debug.Log(string.Format("Voxmeme \"{0}\" is of type {1}", Helper.GetTopPredicate((String)entry.Value),
                                predType));
                            // if predicate is a relation
                            if (predType == "relations") {
                                argsStrings = new List<String>(Regex.Split(((String) entry.Value),
                                    @"(!|^\(|\((?=\()|(?<=(\n|\^)[^(]*\(?[^(]*),(?=[^)]*\)?[^)]*(\n|\^))|(?<=\)[^(]*)[,|^](?=[^)]*\())"));    // use for relational predicates
                            }
                            else {
                                argsStrings = new List<String>(Regex.Split(((String) entry.Value),
                                    @"(!|^\(|\((?=\()|(?<=(\n|^)[^(]*\(?[^(]*),|(?<=\)[^(]*)[,|^](?=[^)]*\())"));   // use for non-relational predicates
                            }
                        }
                        else {
                            // look for primitive predicate of this name
                            MethodInfo primitivePred = preds.GetType().GetMethod(Helper.GetTopPredicate((String)entry.Value).ToUpper());
                            if (primitivePred != null) {
                                argsStrings = new List<String>(Regex.Split(((String) entry.Value),                  // primitive predicates are all non-relational
                                    @"(!|^\(|\((?=\()|(?<=(\n|^)[^(]*\(?[^(]*),|(?<=\)[^(]*)[,|^](?=[^)]*\())"));   // use for non-relational predicate
                            }
                            else {
                                Debug.LogWarning(string.Format("VoxMLEntityTypeDict doesn't contain entry for \"{0}.\" " +
                                    "No primitive predicate \"{1}\" found.  Expect errors!",
                                    Helper.GetTopPredicate((String)entry.Value),Helper.GetTopPredicate((String)entry.Value).ToUpper()));
                                argsStrings = ((String)entry.Value).Split(',').ToList();
                            }
                        }

                        for (int i = 0; i < argsStrings.Count; i++) {   // get rid of any dangling close parens
                            int extraParens = argsStrings[i].Count(f => f == ')') -     //  that might be left over from an imperfect
                                argsStrings[i].Count(f => f == '(');                    //  regex split

                            for (int j = 0; j < extraParens; j++) {
                                argsStrings[i] = argsStrings[i].Remove(argsStrings[i].Length - 1);
                            }
                        }

                        // turn argsStrings in to another string representing a list of all args
                        StringBuilder sb = new StringBuilder("[");
                        foreach (String arg in argsStrings) {
                            sb.Append(arg + ",");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append("]");
                        String argsList = sb.ToString();

                        //Debug.Log(predString + " : " + argsList);

                        for (int i = 0; i < argsStrings.Count; i++) {
                            Debug.Log(string.Format("argsStrings@{0}: {1}", i, argsStrings.ElementAt(i)));
                            if (Helper.pred.IsMatch(argsStrings[i])) {
                                string symbol = argsStrings[i];

                                // if return type of top predicate of symbol is not void
                                //  add it as a skolem constant
                                //if (File.Exists(Data.voxmlDataPath + string.Format("/attributes/{0}.xml", Helper.GetTopPredicate(symbol))) ||
                                //    File.Exists(Data.voxmlDataPath + string.Format("/relations/{0}.xml", Helper.GetTopPredicate(symbol))) ||
                                //    File.Exists(Data.voxmlDataPath + string.Format("/functions/{0}.xml", Helper.GetTopPredicate(symbol)))) {
                                    String v = argVarPrefix + argVarIndex;
                                    skolems[v] = symbol;
                                    Debug.Log(string.Format("Adding skolem constant {0}: {1}", v, skolems[v]));
                                    argVarIndex++;
                                //}

                                argsStrings[i] = symbol;

                                sb = new StringBuilder(sb.ToString());
                                foreach (DictionaryEntry kv in skolems) {
                                    argsList = argsList.Replace((String) kv.Value, (String) kv.Key);
                                }
                            }

                            ParseCommand(argsStrings.ElementAt(i));
                        }
                    }
                }
            }

            public void FinishSkolemization() {
                Hashtable temp = new Hashtable();

                foreach (DictionaryEntry kv in skolems) {
                    foreach (DictionaryEntry kkv in skolems) {
                        if (kkv.Key != kv.Key) {
                            //Debug.Log ("FinishSkolemization: "+kv.Key+ " " +kkv.Key);
                            if (!temp.Contains(kkv.Key)) {
                                if (((String) kkv.Value).Contains((String) kv.Value) &&
                                    ((((String) kkv.Value).Count(f => f == '(') + ((String) kkv.Value).Count(f => f == ')')) -
                                     (((String) kv.Value).Count(f => f == '(') + ((String) kv.Value).Count(f => f == ')')) ==
                                     2)) {
                                    Debug.Log("FinishSkolemization: " + kv.Value + " found in " + kkv.Value);
                                    Debug.Log("FinishSkolemization: " + kkv.Key + " : " +
                                              ((String) kkv.Value).Replace((String) kv.Value, (String) kv.Key));
                                    temp[kkv.Key] = ((String) kkv.Value).Replace((String) kv.Value, (String) kv.Key);
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

                Helper.PrintKeysAndValues("skolems", skolems);
            }

            public String Skolemize(String inString) {
                String outString = inString;
                String temp = inString;

                int parenCount = temp.Count(f => f == '(') +
                                 temp.Count(f => f == ')');
                Debug.Log("Skolemize: parenCount = " + parenCount);

                //return outString;

                //do {
                    foreach (DictionaryEntry kv in skolems) {
                        outString = outString.Replace((String) kv.Value, (String) kv.Key);
                        //Debug.Log (outString);
                    }

                    //temp = outString;
                    //parenCount = temp.Count(f => f == '(') +
                    //             temp.Count(f => f == ')');
                    //Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());
                    //move(mug,from(edge(table)),to(edge(table)))
                //} while (parenCount > 2);

                return outString;
            }

            public String ApplyGlobals(String inString) {
                Helper.PrintKeysAndValues("Applying macroVars", macroVars);
                String outString = inString;
                String temp = inString;

                int parenCount = temp.Count(f => f == '(') +
                                 temp.Count(f => f == ')');
                //Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

                foreach (DictionaryEntry kv in macroVars) {
                    if (kv.Value is Vector3) {
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key,
                                Helper.VectorToParsable((Vector3) kv.Value));
                        }
                        // get the entries in "skolems" where the values contain the string equal to current key under question
                        Dictionary<string, string> changeValues = skolems.Cast<DictionaryEntry>()
                            .ToDictionary(kkv => kkv.Key, kkv => kkv.Value)
                            .Where(kkv => kkv.GetType() == typeof(String) && ((String)kkv.Value).Contains((String) kv.Key))
                            .ToDictionary(kkv => (String)kkv.Key, kkv => (String)kkv.Value);
                        foreach (string key in changeValues.Keys) {
                            skolems[key] = changeValues[key].Replace((String) kv.Key, Helper.VectorToParsable((Vector3) kv.Value));
                        }
                    }
                    else if (kv.Value is List<Vector3>) {
                        Debug.Log(kv.Value + " is List<Vector3>");
                        String list = string.Format("[{0}]",String.Join(":",
                            ((List<Vector3>) kv.Value).Select(v => Helper.VectorToParsable(v)).ToArray()));
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key, list);
                        }
                        list = string.Format("[{0}]",String.Join(",", ((List<Vector3>) kv.Value).Select(v => Helper.VectorToParsable(v)).ToArray()));
                        // get the entries in "skolems" where the values contain the string equal to current key under question
                        Dictionary<string, string> changeValues = skolems.Cast<DictionaryEntry>()
                            .ToDictionary(kkv => kkv.Key, kkv => kkv.Value)
                            .Where(kkv => kkv.GetType() == typeof(String) && ((String)kkv.Value).Contains((String) kv.Key))
                            .ToDictionary(kkv => (String)kkv.Key, kkv => (String)kkv.Value);
                        foreach (string key in changeValues.Keys) {
                            skolems[key] = changeValues[key].Replace((String) kv.Key, list);
                        }
                    }
                    else if (kv.Value is GameObject) {
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key, ((GameObject) kv.Value).name);
                        }
                        // get the entries in "skolems" where the values contain the string equal to current key under question
                        Dictionary<string, string> changeValues = skolems.Cast<DictionaryEntry>()
                            .ToDictionary(kkv => kkv.Key, kkv => kkv.Value)
                            .Where(kkv => kkv.GetType() == typeof(String) && ((String)kkv.Value).Contains((String) kv.Key))
                            .ToDictionary(kkv => (String)kkv.Key, kkv => (String)kkv.Value);
                        foreach (string key in changeValues.Keys) {
                            skolems[key] = changeValues[key].Replace((String) kv.Key, ((GameObject) kv.Value).name);
                        }
                    }
                    else if (kv.Value is List<GameObject>) {
                        String list = String.Join(":", ((List<GameObject>) kv.Value).Select(go => go.name).ToArray());
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key, list);
                        }
                        list = string.Format("[{0}]",String.Join(",", ((List<GameObject>) kv.Value).Select(go => go.name).ToArray()));
                        // get the entries in "skolems" where the values contain the string equal to current key under question
                        Dictionary<string, string> changeValues = skolems.Cast<DictionaryEntry>()
                            .ToDictionary(kkv => kkv.Key, kkv => kkv.Value)
                            .Where(kkv => kkv.GetType() == typeof(String) && ((String)kkv.Value).Contains((String) kv.Key))
                            .ToDictionary(kkv => (String)kkv.Key, kkv => (String)kkv.Value);
                        foreach (string key in changeValues.Keys) {
                            skolems[key] = changeValues[key].Replace((String) kv.Key, list);
                        }
                    }
                    else if (kv.Value is String) {
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key, (String) kv.Value);
                        }
                    }
                    else if (kv.Value is List<String>) {
                        String list = string.Format("[{0}]",String.Join(",", ((List<String>) kv.Value).ToArray()));
                        MatchCollection matches = Regex.Matches(outString, @"(?<!\'[^,]+)" + (String)kv.Key + @"(?![^,]+\')");
                        for (int i = 0; i < matches.Count; i++) {
                            outString = outString.ReplaceFirstStartingAt(matches[i].Index, (String) kv.Key, list);
                        }
                    }
                }

                temp = outString;
                parenCount = temp.Count(f => f == '(') +
                             temp.Count(f => f == ')');
                //Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

                Helper.PrintKeysAndValues("skolems", skolems);

                //Debug.Log(outString);
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
                        outString = outString.Replace((String) kv.Key, Helper.VectorToParsable((Vector3) kv.Value));
                        //Debug.Log (outString);
                    }
                    else if (kv.Value is String) {
                        outString = outString.Replace((String) kv.Key, (String) kv.Value);
                    }
                    else if (kv.Value is List<String>) {
                        String list = String.Join(",", ((List<String>) kv.Value).ToArray());
                        outString = outString.Replace((String) kv.Key, list);
                    }
                    else if (kv.Value is bool) {
                        Dictionary<string, string> toReplace = new Dictionary<string, string>();
                        List<int> indicesOfArg = outString.FindAllIndicesOf((String)kv.Key);
                        foreach (int index in indicesOfArg) {
                            //Debug.Log(string.Format("{0}:{1}:{2}", (String)kv.Key, index, outString[index-1]));
                            if ((index > 0) && 
                                (outString[index-1] == '!')) {
                                if (!toReplace.ContainsKey('!' + (String)kv.Key)) {
                                    toReplace.Add('!' + (String)kv.Key, (!(bool)kv.Value).ToString());
                                }
                            }
                            else {
                                if (!toReplace.ContainsKey((String)kv.Key)) {
                                    toReplace.Add((String)kv.Key, ((bool)kv.Value).ToString());
                                }
                            }
                        }
                            
                        foreach (KeyValuePair<string,string> kkv in toReplace.OrderByDescending(e => e.Key.Length)) {
                            outString = outString.Replace(kkv.Key, toReplace[kkv.Key]);
                        }
                    }
                }

                temp = outString;
                parenCount = temp.Count(f => f == '(') +
                             temp.Count(f => f == ')');
                //Debug.Log ("Skolemize: parenCount = " + parenCount.ToString ());

                return outString;
            }

            public bool EvaluateSkolemConstants(EvaluationPass pass) {
                Hashtable temp = new Hashtable();
                Regex regex = new Regex(argVarPrefix + @"[0-9]+");
                Match argsMatch;
                Hashtable predArgs;
                List<object> objs = new List<object>();
                Queue<String> argsStrings;
                bool doSkolemReplacement = false;
                Triple<String, String, String> replaceSkolems = null;
                bool validPredExists;
                VoxML voxml = null;

                methodToCall = null;

                foreach (DictionaryEntry kv in skolems) {
                    voxml = null;
                    Debug.Log(kv.Key + " : " + kv.Value);
                    objs.Clear();
                    if (kv.Value is String) {
                        Debug.Log(kv.Value);
                        argsMatch = regex.Match((String) kv.Value);
                        //Debug.Log(argsMatch);
                        if (argsMatch.Groups[0].Value.Length == 0) {
                            // matched an empty string = no match
                            Debug.Log(kv.Value);
                            predArgs = Helper.ParsePredicate((String) kv.Value);
                            String pred = Helper.GetTopPredicate((String) kv.Value);
                            if (((String) kv.Value).Count(f => f == '(') + // make sure actually a predicate
                                ((String) kv.Value).Count(f => f == ')') >= 2) {
                                argsStrings = new Queue<String>(((String) predArgs[pred]).Split(new char[] {','}));
                                while (argsStrings.Count > 0) {
                                    object arg = argsStrings.Dequeue();

                                    if (Helper.vec.IsMatch((String) arg)) {
                                        // if arg is vector form
                                        Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",Helper.ParsableToVector((String) arg)));
                                        objs.Add(Helper.ParsableToVector((String) arg));
                                    }
                                    else if (arg is String) {
                                        // if arg is String
                                        if ((arg as String).Count(f => f == '(') + // not a predicate
                                            (arg as String).Count(f => f == ')') == 0) {
                                            //if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {    // if predicate not going to return string (as in "AS")
                                            List<GameObject> matches = new List<GameObject>();

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

                                            Debug.Log(string.Format("{0} matches: [{1}]", matches.Count, string.Join(",",matches.Select(go => go.name).ToList())));

                                            if (matches.Count == 0) {
                                                Debug.Log(arg as String);
                                                Debug.Log(preds.GetType().GetMethod(pred.ToUpper()).ReturnType);
                                                //if (preds.GetType ().GetMethod (pred.ToUpper ()).ReturnType != typeof(String)) {    // if predicate not going to return string (as in "AS")
                                                GameObject go = GameObject.Find(arg as String);
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
                                                        return false; // abort
                                                    }
                                                }

                                                Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",go));
                                                objs.Add(go);
                                                //}
                                            }
                                            else if (matches.Count == 1) {
                                                // check if the predicate over this argument exists in our primitive list
                                                //  or exists in VoxML
                                                validPredExists = (((preds.GetType().GetMethod(pred.ToUpper()) != null) &&
                                                    (preds.GetType().GetMethod(pred.ToUpper()).ReturnType != typeof(String))) ||
                                                    ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations"))));
                                                    //(File.Exists(Data.voxmlDataPath + string.Format("/relations/{0}.xml", pred))));
                                                if (validPredExists) {
                                                    Debug.Log(string.Format("Predicate found: {0}", pred));
                                                    GameObject go = matches[0];
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
                                                                new EventReferentArgs(
                                                                    new Pair<string, List<GameObject>>(pred, matches)));
                                                            return false; // abort
                                                        }
                                                    }

                                                    Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",go));
                                                    objs.Add(go);
                                                    doSkolemReplacement = true;
                                                    replaceSkolems = new Triple<String, String, String>(kv.Key as String,
                                                        arg as String, go.name);
                                                    //skolems[kv] = go.name;
                                                }
                                                else {
                                                    Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",matches[0]));
                                                    objs.Add(matches[0]);
                                                }
                                            }
                                            else {
                                                // if predicate arity of enclosing predicate as encoded in VoxML != matches.Count
                                                VoxML predVoxeme = new VoxML();
                                                String path = string.Empty;
                                                Debug.Log(pred);
                                                if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) &&
                                                    ((voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") ||
                                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations") ||
                                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "functions"))) {
                                                    predVoxeme = voxmlLibrary.VoxMLObjectDict[pred];

                                                    Debug.Log(predVoxeme);
                                                    if (voxmlLibrary.VoxMLEntityTypeDict[pred] == "functions") {
                                                        Debug.Log(predVoxeme.Type.Mapping);
                                                        int arity;
                                                        bool isInt = Int32.TryParse(predVoxeme.Type.Mapping.Split(':')[1],
                                                            out arity);

                                                        if (isInt) {
                                                            Debug.Log(string.Format("{0} : {1} : {2}", pred.ToUpper(), arity,
                                                                matches.Count));

                                                            if (arity != matches.Count) {
                                                                OnDisambiguationError(this,
                                                                    new EventDisambiguationArgs(events[0], (String) kv.Value,
                                                                        "{0}",
                                                                        matches.Select(o => o.GetComponent<Voxeme>())
                                                                            .ToArray()));
                                                                return false; // abort
                                                            }
                                                        }
                                                    }
                                                    else {
                                                        int arity = predVoxeme.Type.Args.Count - 1;
                                                        Debug.Log(string.Format("{0} : {1} : {2}", pred.ToUpper(), arity,
                                                            matches.Count));

                                                        if (arity != matches.Count) {
                                                            //Debug.Log(string.Format("Which {0}?", (arg as String)));
                                                            //OutputHelper.PrintOutput(Role.Affector, string.Format("Which {0}?", (arg as String)));
                                                            OnDisambiguationError(this, new EventDisambiguationArgs(events[0],
                                                                (String) kv.Value,
                                                                ((String) kv.Value).Replace(arg as String, "{0}"),
                                                                matches.Select(o => o.GetComponent<Voxeme>()).ToArray()));
                                                            return false; // abort
                                                        }
                                                    }
                                                }
                                                else {
                                                    foreach (GameObject match in matches) {
                                                        //Debug.Log(match);
                                                        Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",match));
                                                        objs.Add(match);
                                                    }
                                                }
                                            }
                                        }

                                        if (objs.Count == 0) {
                                            Regex q = new Regex("[\'\"].*[\'\"]");
                                            int i;
                                            if (int.TryParse(arg as String, out i)) {
                                                Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",arg as String));
                                                objs.Add(arg as String);
                                            }
                                            else if (q.IsMatch(arg as String)) {
                                                String[] tryMethodPath = (arg as String).Replace("\'",string.Empty)
                                                    .Replace("\"",string.Empty).Split('.');

                                                // Get the Type for the class
                                                Type routineCallingType = Type.GetType(String.Join(".", tryMethodPath.ToList().GetRange(0, tryMethodPath.Length - 1)));
                                                if (routineCallingType != null) {
                                                    MethodInfo routineMethod = routineCallingType.GetMethod(tryMethodPath.Last());
                                                    if (routineMethod != null) {
                                                        Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",routineMethod));
                                                        objs.Add(routineMethod);
                                                    }
                                                    else {
                                                        Debug.Log(string.Format("No method {0} found in class {1}!",tryMethodPath.Last(),routineCallingType.Name));
                                                    }
                                                } 
                                                else {
                                                    Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",arg as String));
                                                    objs.Add(arg as String);
                                                }
                                            }
                                            else {
                                                GameObject go = GameObject.Find(arg as String);
                                                Debug.Log(string.Format("EvaluateSkolemConstants: adding {0} to objs",go));
                                                objs.Add(go);
                                            }
                                        }
                                    }
                                }

                                methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                                validPredExists = ((methodToCall != null) ||
                                    ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                                    ((voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") ||
                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations")))));

                                if (!validPredExists) {
                                    this.GetActiveAgent().GetComponent<AgentOutputController>().PromptOutput("Sorry, what does " + "\"" + pred + "\" mean?");
                                    OutputHelper.PrintOutput(Role.Affector, "Sorry, what does " + "\"" + pred + "\" mean?");
                                    return false;
                                }
                                else if (methodToCall == null) {
                                    if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                                        (voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs"))) {
                                        voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                        methodToCall = preds.GetType().GetMethod("ComposeProgram");
                                    }
                                    else if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                                        (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations"))) {
                                        voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                        methodToCall = preds.GetType().GetMethod("ComposeRelation");
                                    }
                                }

                                if (pass == EvaluationPass.Attributes) {
                                    //if ((methodToCall.ReturnType == typeof(String)) ||
                                    //    (methodToCall.ReturnType == typeof(List<String>))) {
                                    // non-void return type
                                    // (attribute, relation, function)
                                    if (methodToCall.ReturnType != typeof(void)) {
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): invoke {1} with {2}{3}",
                                            pass, methodToCall.Name, (voxml == null) ? string.Empty : "\"" + voxml.Lex.Pred + "\", ", objs));
                                        object obj = null;
                                        if (voxml == null) {
                                            obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                        }
                                        else {
                                            obj = methodToCall.Invoke(preds, new object[] {voxml, objs.ToArray()});
                                        } 
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): {1} returns {2} (typeof({3}))",
                                            pass, methodToCall.Name, obj, obj.GetType()));

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

                                        temp[kv.Key] = obj;
                                    }
                                    else {  // void return type: program
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): invoke IsSatisfied{1} with {2}{3}",
                                            pass, (voxml == null) ? string.Format("({0})",methodToCall.Name) : string.Empty,
                                                (voxml == null) ? string.Empty : string.Format("\"{0}\" ",voxml.Lex.Pred), objs));
                                        object obj = null;
                                        if (voxml == null) {
                                            obj = SatisfactionTest.IsSatisfied(methodToCall.Name, objs);
                                            //obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                        }
                                        else {
                                            obj = SatisfactionTest.IsSatisfied(voxml, objs);
                                            //obj = methodToCall.Invoke(preds, new object[] {voxml, objs.ToArray()});
                                        } 
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): IsSatisfied({1}) returns {2} (typeof({3}))",
                                            pass, (voxml == null) ? methodToCall.Name : string.Format("\"{0}\" ",voxml.Lex.Pred),
                                                obj, obj.GetType()));

                                        temp[kv.Key] = obj;
                                    }
                                }
                                else if (pass == EvaluationPass.RelationsAndFunctions) {
                                    if ((methodToCall.ReturnType == typeof(Vector3)) ||
                                        (methodToCall.ReturnType == typeof(object))) {
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): invoke {1} with {2}{3}",
                                            pass, methodToCall.Name, (voxml == null) ? string.Empty : "\"" + voxml.Lex.Pred + "\", ", objs));
                                        object obj = null;
                                        if (voxml == null) {
                                            obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                                        }
                                        else {
                                            obj = methodToCall.Invoke(preds, new object[] {voxml, objs.ToArray()});
                                        } 
                                        Debug.Log(string.Format("EvaluateSkolemConstants ({0}): {1} returns {2} (typeof({3}))",
                                            pass, methodToCall.Name, obj, obj.GetType()));

                                        temp[kv.Key] = obj;
                                    }
                                }
                            }
                        }
                        else {
                            temp[kv.Key] = kv.Value;
                        }
                    }
                }

                // replace improperly named arguments
                if (doSkolemReplacement) {
                    skolems[replaceSkolems.Item1] =
                        ((String) skolems[replaceSkolems.Item1]).Replace(replaceSkolems.Item2, replaceSkolems.Item3);
                }

                //Helper.PrintKeysAndValues(skolems);

        //        for (int i = 0; i < temp.Count; i++) {
        //            Debug.Log (temp [i]);
        //        }

                foreach (DictionaryEntry kv in temp) {
                    //for (int i = 0; i < temp.Count; i++) {
                    //DictionaryEntry kv = (DictionaryEntry)temp [i];
                    //Debug.Log (kv.Value);
                    String matchVal = kv.Value as String;
                    if (matchVal == null) {
                        matchVal = @"DEADBEEF"; // dummy val
                    }

                    argsMatch = regex.Match(matchVal);
                    if (argsMatch.Groups[0].Value.Length > 0) {
                        Debug.Log(argsMatch.Groups[0]);
                        if (temp.ContainsKey(argsMatch.Groups[0].Value)) {
                            object replaceWith = temp[argsMatch.Groups[0].Value];
                            Debug.Log(replaceWith.GetType());
                            //String replaced = ((String)skolems [kv.Key]).Replace ((String)argsMatch.Groups [0].Value,
                            //    replaceWith.ToString ().Replace (',', ';').Replace ('(', '<').Replace (')', '>'));
                            if (regex.Match((String) replaceWith).Length == 0) {
                                String replaced = argsMatch.Groups[0].Value;
                                if (replaceWith is String) {
                                    replaced = ((String) skolems[kv.Key]).Replace(argsMatch.Groups[0].Value,
                                        (String) replaceWith);
                                }
                                else if (replaceWith is Vector3) {
                                    replaced = ((String) skolems[kv.Key]).Replace((String) argsMatch.Groups[0].Value,
                                        Helper.VectorToParsable((Vector3) replaceWith));
                                }
                                else if (replaceWith is bool) {
                                    replaced = ((String) skolems[kv.Key]).Replace(argsMatch.Groups[0].Value,
                                        (String) replaceWith);
                                }

                                Debug.Log(string.Format("Replacing {0} with {1}", skolems[kv.Key], replaced));
                                skolems[kv.Key] = replaced;
                            }
                        }
                    }
                    else {
                        Debug.Log(string.Format("Replacing {0} with {1}", skolems[kv.Key], temp[kv.Key]));
                        skolems[kv.Key] = temp[kv.Key];
                    }
                }

                Helper.PrintKeysAndValues("skolems", skolems);

                int newEvaluations = 0;
                foreach (DictionaryEntry kv in skolems) {
                    Debug.Log(kv.Key + " : " + kv.Value);
                    if (kv.Value is String) {
                        argsMatch = Helper.pred.Match((String) kv.Value);

                        if (argsMatch.Groups[0].Value.Length > 0) {
                            string pred = argsMatch.Groups[0].Value.Split('(')[0];
                            methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                            validPredExists = ((methodToCall != null) ||
                                ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred) &&
                                (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations"))));

                            if (!validPredExists) {
                                this.GetActiveAgent().GetComponent<AgentOutputController>().PromptOutput("Sorry, what does " + "\"" + pred + "\" mean?");
                                OutputHelper.PrintOutput(Role.Affector, "Sorry, what does " + "\"" + pred + "\" mean?");
                                return false;
                            }
                            else if (methodToCall == null) {
                                if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) &&
                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs")) {
                                    voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                    methodToCall = preds.GetType().GetMethod("ComposeProgram");
                                }
                                else if ((voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) &&
                                    (voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations")) {
                                    voxml = voxmlLibrary.VoxMLObjectDict[pred];
                                    methodToCall = preds.GetType().GetMethod("ComposeRelation");
                                }
                            }

                            Debug.Log(string.Format("EvaluateSkolemConstants ({0}): queue new call to {1} (pred = \"{2}\")", pass, methodToCall.Name, pred));

                            if (methodToCall != null) {
                                if (((methodToCall.ReturnType == typeof(String)) ||
                                    (methodToCall.ReturnType == typeof(List<String>)) ||
                                    (methodToCall.Name == "ComposeAttribute")) &&
                                    (pass == EvaluationPass.Attributes)) {
                                    newEvaluations++;
                                }

                                if (((methodToCall.ReturnType == typeof(Vector3)) ||
                                    (methodToCall.Name == "ComposeRelation") || 
                                    (methodToCall.Name == "ComposeFunction")) &&
                                    (pass == EvaluationPass.RelationsAndFunctions)) {
                                    newEvaluations++;
                                }

                                if (((methodToCall.ReturnType == typeof(void)) ||
                                    ((methodToCall.Name == "ComposeProgram"))) &&
                                    (pass == EvaluationPass.Attributes)) {
                                    newEvaluations++;
                                }
                            }
                        }
                    }
                }

                //Debug.Log (newEvaluations);
                if (newEvaluations > 0) {
                    EvaluateSkolemConstants(pass);
                }

                //Helper.PrintKeysAndValues(skolems);

                return true;
            }

            /// <summary>
            /// Triggered when the methodToCall field changes
            /// </summary>
            // IN: oldVal -- previous methodToCall
            //      newVal -- new or current methodToCall
            void OnMethodToCallChanged(MethodInfo oldMethod, MethodInfo newMethod) {
                Debug.Log(string.Format("==================== EventManager Method to call changed ==================== {0}->{1}",
                    (oldMethod == null) ? "NULL" : oldMethod.Name,
                    (newMethod == null) ? "NULL" : newMethod.Name));
            }

            /// <summary>
            /// Triggered when the events list is modified or changes
            /// </summary>
            void OnEventsListChanged(object sender, NotifyCollectionChangedEventArgs e) {
                //Debug.Log(string.Format("==================== Events list changed ==================== {0}",
                //    (sender == null) ? "NULL" : string.Format("[{0}]",string.Join(",\n\t",((ObservableCollection<string>)sender).Cast<string>()))));
                inspectableEventsList = ((ObservableCollection<string>)sender).Cast<string>().ToList();
            }

            /// <summary>
            /// Triggered when the events list is modified or changes
            /// </summary>
            void OnExecutedEventHistoryChanged(object sender, NotifyCollectionChangedEventArgs e) {
                Debug.Log(string.Format("==================== Executed event history changed ==================== {0}",
                    (sender == null) ? "NULL" : string.Format("[{0}]",string.Join(",\n\t",((ObservableCollection<string>)sender).Cast<string>()))));
                inspectableEventHistory = ((ObservableCollection<string>)sender).Cast<string>().ToList();
            }
        }
    }
}