using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using MajorAxes;
using Object = UnityEngine.Object;
using RootMotion.FinalIK;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Animation;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.GenLex;
using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.SpatialReasoning.QSR;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Core {
        public static class SatisfactionTest {
            public static bool IsSatisfied(String pred, List<object> args) {
                bool satisfied = false;

                EventManager em = GameObject.Find("BehaviorController").GetComponent<EventManager>();
                RelationTracker relationTracker = GameObject.Find("BehaviorController").GetComponent<RelationTracker>();

                if (em.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) {
                    if (em.voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs") {

                    }
                    else if (em.voxmlLibrary.VoxMLEntityTypeDict[pred] == "relations") {
                        foreach (List<object> key in relationTracker.relations.Keys.OfType<List<object>>().Where(k => k.SequenceEqual(args))) {
                            if (relationTracker.relations[key].ToString().Contains(pred)) {
                                satisfied = true;
                            }
                        }
                    }
                }

                return satisfied;
            }

            public static bool IsSatisfied(VoxML voxml, List<object> args) {
                bool satisfied = false;

                EventManager em = GameObject.Find("BehaviorController").GetComponent<EventManager>();
                RelationTracker relationTracker = GameObject.Find("BehaviorController").GetComponent<RelationTracker>();

                if (voxml.Entity.Type == VoxEntity.EntityType.Program) {
                    for (int i = 0; i < voxml.Type.Args.Count; i++) {
                        string argName = voxml.Type.Args[i].Value.Split(':')[0];
                        string[] argType = voxml.Type.Args[i].Value.Split(':')[1].Split('*');

                        if ((argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.Agent).ToList().Count > 0) ||
                            (argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.AgentList).ToList().Count > 0)) {
                            args.Insert(i, em.GetActiveAgent()); 
                        }
                    }

                    foreach (object arg in args) {
                        Debug.Log(arg.GetType());
                    }

                    // for now, a program is considered "satisfied" in this sense if a synonymous program exists in the relation tracker
                    //  at the moment, this effectively only works with "hold"
                    // TODO: something better
                    Debug.Log(relationTracker.relations.Keys.Count);
                    Debug.Log(relationTracker.relations.Keys.OfType<List<GameObject>>().ToList().Count);
                    Debug.Log(relationTracker.relations.Keys.OfType<List<GameObject>>().Where(k => k.SequenceEqual(args)).ToList().Count);
                    foreach (List<GameObject> key in relationTracker.relations.Keys.OfType<List<GameObject>>().Where(k => k.SequenceEqual(args))) {
                        if (relationTracker.relations[key].ToString().Contains(voxml.Lex.Pred)) {
                            satisfied = true;
                        }
                    }
                }
                else if (voxml.Entity.Type == VoxEntity.EntityType.Relation) {

                    string relStr = string.Empty;

                    switch (voxml.Type.Class) {
                        case "config":
                            relStr = voxml.Type.Value;
                            break;

                        case "force_dynamic":
                            relStr = voxml.Type.Value;
                            break;

                        default:
                            Debug.Log(string.Format("IsSatisfied: unknown relation class: {0}", voxml.Type.Class));
                            break;
                    }

                    if (relStr != string.Empty) {
                        for (int i = 0; i < voxml.Type.Args.Count; i++) {
                            string argName = voxml.Type.Args[i].Value.Split(':')[0];
                            string[] argType = voxml.Type.Args[i].Value.Split(':')[1].Split('*');
                            
                            if ((argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.Agent).ToList().Count > 0) ||
                                (argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.AgentList).ToList().Count > 0)) {
                                args.Insert(i, em.GetActiveAgent()); 
                            }
                        }

                        if (voxml.Type.Class == "config") {
                            // Get the Type for the calling class
                            //  class must be within namespace VoxSimPlatform.SpatialReasoning.QSR
                            String[] tryMethodPath = string.Format("VoxSimPlatform.SpatialReasoning.QSR.{0}", relStr).Split('.');
                            Type methodCallingType = Type.GetType(string.Join(".", tryMethodPath.ToList().GetRange(0, tryMethodPath.Length - 1)));
                            if (methodCallingType != null) {
                                try {
                                    MethodInfo method = methodCallingType.GetMethod(relStr.Split('.')[1], args.Select(a => a.GetType()).ToArray());
                                    if (method != null) {
                                        Debug.Log(string.Format("Testing predicate \"{0}\": found method {1}.{2}({3})", voxml.Lex.Pred,
                                            methodCallingType.Name, method.Name, string.Join(", ",method.GetParameters().Select(p => p.ParameterType))));
                                        object obj = method.Invoke(null, args.ToArray());
                                        satisfied = (bool)obj;
                                    }
                                    else {  // no method found
                                        // throw this to ComposeQSR
                                        method = Type.GetType("VoxSimPlatform.SpatialReasoning.QSR.QSR").GetMethod("ComposeQSR");
                                        Debug.Log(string.Format("Testing predicate \"{0}\": found method {1}.{2}({3})", voxml.Lex.Pred,
                                            methodCallingType.Name, method.Name, string.Join(", ",method.GetParameters().Select(p => p.ParameterType))));
                                        object obj = method.Invoke(null, args.ToArray());
                                        satisfied = (bool)obj;
                                    }
                                }
                                catch (Exception ex) {
                                    if (ex is AmbiguousMatchException) {
                                        Debug.LogError(string.Format("Ambiguous match found. Query was GetMethod(\"{0}\",[{1}]) in namespace {2}.",
                                            relStr.Split('.')[1], string.Join(", ",args.Select(a => a.GetType().ToString()).ToArray()),
                                            methodCallingType.ToString()));
                                    }
                                    else {
                                        Debug.LogError(ex);
                                    }
                                }
                            }
                            else {
                                Debug.Log(string.Format("IsSatisfied: No type {0} found!",
                                    string.Join(".", tryMethodPath.ToList().GetRange(0, tryMethodPath.Length - 1))));
                            }
                        }
                        else if (voxml.Type.Class == "force_dynamic") {
                            foreach (List<object> key in relationTracker.relations.Keys.OfType<List<object>>().Where(k => k.SequenceEqual(args))) {
                                if (relationTracker.relations[key].ToString().Contains(voxml.Lex.Pred)) {
                                    satisfied = true;
                                }
                            }
                        }
                    }
                }

                return satisfied;
            }

            public static bool IsSatisfied(String test) {
                bool satisfied = false;
                Hashtable predArgs = Helper.ParsePredicate(test);
                String predString = "";
                String[] argsStrings = null;

                PhysicsPrimitives physicsManager = GameObject.Find("BehaviorController").GetComponent<PhysicsPrimitives>();
                Predicates preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();
                EventManager em = GameObject.Find("BehaviorController").GetComponent<EventManager>();

                bool isMacroEvent = false;

    //            if (em.isInitiatePhase) {
    //                return false;
    //            }

                foreach (DictionaryEntry entry in predArgs) {
                    predString = (String) entry.Key;
                    argsStrings = ((String) entry.Value).Split(',');
                }

                //Debug.Log (test);

                // PRIMITIVE MOTIONS
                if (predString == "move") {
                    // satisfy move_1
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        Vector3 testLocation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.position
                            : theme.transform.position;

                        //Debug.Log(string.Format("{0} : {1}", Helper.VectorToParsable(testLocation),argsStrings[1]));
                        if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]))) {
                            if (voxComponent.isGrasped) {
                                theme.transform.position = Helper.ParsableToVector(argsStrings[1]);
                                theme.transform.rotation = Quaternion.identity;
                            }

                            Debug.Log(Helper.VectorToParsable(theme.transform.position));
                            satisfied = true;
                        }
                    }
                }
                // PRIMITIVE FUNCTIONAL OPERATORS
                // conditionals
                else if (predString == "if") {
                    string expression = (argsStrings[0] as String).Replace("^", " AND ").Replace("|", " OR ");
                    DataTable dt = new DataTable();
                    try {
                        // don't do anything with this
                        //  the "satisfaction" of a conditional is just a check
                        //  to see if the constituent conditions have been successfully evaluated
                        bool result = (bool)dt.Compute(expression, null);
                        satisfied = true;
                    }
                    catch (Exception ex) {
                        if (ex is EvaluateException) {
                            satisfied = false;
                        }
                    }
                }
                else if (predString == "while") {
                    string expression = (argsStrings[0] as String).Replace("^", " AND ").Replace("|", " OR ");
                    DataTable dt = new DataTable();
                    try {
                        // don't do anything with this
                        //  the "satisfaction" of a conditional is just a check
                        //  to see if the constituent conditions have been successfully evaluated
                        bool result = (bool)dt.Compute(expression, null);
                        satisfied = true;
                    }
                    catch (Exception ex) {
                        if (ex is EvaluateException) {
                            satisfied = false;
                        }
                    }
                }

                // COMPLEX MOTION (TODO: remove)
                else if (predString == "put_1") {
                    // satisfy put
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        //Debug.Log(Helper.VectorToParsable(theme.transform.position) + " " + (String)argsStrings[1]);
                        //Debug.Log(obj.transform.position);
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        //Debug.Log (voxComponent);
                        Vector3 testLocation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.position
                            : theme.transform.position;

                        if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]))) {
                            if (voxComponent.isGrasped) {
                                //preds.UNGRASP (new object[]{ theme, true });
                                //em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
                                theme.transform.position = Helper.ParsableToVector(argsStrings[1]);
                                theme.transform.rotation = Quaternion.identity;
                            }

                            satisfied = true;
                            //obj.GetComponent<Rigging> ().ActivatePhysics (true);
                            //ReevaluateRelationships (predString, theme);    // we need to talk (do physics reactivation in here?)
    //                        ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                        }
                    }
                }
                else if (predString == "slide") {
                    // satisfy slide
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        //Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
                        //Debug.Log(obj.transform.position);
                        //Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        Vector3 testLocation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.position
                            : theme.transform.position;

                        if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]))) {
                            if (voxComponent.isGrasped) {
                                //preds.UNGRASP (new object[]{ theme, true });
                                //em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
                                theme.transform.position = Helper.ParsableToVector(argsStrings[1]);
                                theme.transform.rotation = Quaternion.identity;
                            }

                            satisfied = true;
    //                        ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                            //theme.GetComponent<Rigging> ().ActivatePhysics (true);
                        }
                    }
                }
                else if (predString == "roll") {
                    // satisfy roll
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        //Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
                        //Debug.Log(obj.transform.position);
                        //Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        Vector3 testLocation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.position
                            : theme.transform.position;

                        if (argsStrings.Length > 1) {
                            if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]))) {
                                if (voxComponent.isGrasped) {
                                    //preds.UNGRASP (new object[]{ theme, true });
                                    //em.ExecuteCommand(string.Format("put({0},{1})",theme.name,(String)argsStrings [1]));
                                    theme.transform.position = Helper.ParsableToVector(argsStrings[1]);
                                    theme.transform.rotation = Quaternion.identity;
                                }

                                satisfied = true;
    //                            ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                                //theme.GetComponent<Rigging> ().ActivatePhysics (true);
                            }
                        }
                    }
                }
                else if (predString == "turn") {
                    // satisfy turn
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        //Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
                        //Debug.Log(obj.transform.position);
                        //Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        Vector3 testRotation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.eulerAngles
                            : theme.transform.eulerAngles;

                        //Debug.DrawRay(theme.transform.position, theme.transform.up * 5, Color.blue, 0.01f);
                        //Debug.Log(Vector3.Angle (theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1]), Helper.ParsableToVector ((String)argsStrings [2])));
                        //Debug.Log(Helper.VectorToParsable(theme.transform.rotation * Helper.ParsableToVector ((String)argsStrings [1])));
                        //Debug.Log(Helper.ParsableToVector ((String)argsStrings [2]));
                        if (Mathf.Deg2Rad *
                            Vector3.Angle(theme.transform.rotation * Helper.ParsableToVector(argsStrings[1]),
                                Helper.ParsableToVector(argsStrings[2])) < Constants.EPSILON) {
                            if (voxComponent.isGrasped) {
                                //theme.transform.rotation = Quaternion.Euler(Helper.ParsableToVector ((String)argsStrings [1]));
                                //theme.transform.rotation = Quaternion.identity;
                            }

                            satisfied = true;
                            //Debug.Break ();

                            //bar;
                            // ROLL once - roll again - voxeme object satisfied TURN but rigidbody subobjects have moved under physics 
    //                        ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                            //theme.GetComponent<Rigging> ().ActivatePhysics (true);
                        }
                    }
                }
                /*else if (predString == "spin") {    // satisfy spin
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

                            ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                        }
                    }
                }*/
    //            else if (predString == "flip") {    // satisfy flip
    //                GameObject theme = GameObject.Find (argsStrings [0] as String);
    //                if (theme != null) {
    //                    //Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
    //                    //Debug.Log(obj.transform.position);
    //                    //Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
    //                    if (Helper.CloseEnough(theme.transform.rotation, Quaternion.Euler (Helper.ParsableToVector ((String)argsStrings [1])))) {
    //                        satisfied = true;
    //                        ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
    //                        theme.GetComponent<Rigging> ().ActivatePhysics (true);
    //                    }
    //                }
    //            }
                else if (predString == "lift") {
                    // satisfy lift
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    if (theme != null) {
                        //Debug.Log(Helper.ConvertVectorToParsable(obj.transform.position) + " " + (String)argsStrings[1]);
                        //Debug.Log(obj.transform.position);
                        //Debug.Log (Quaternion.Angle(obj.transform.rotation,Quaternion.Euler(Helper.ParsableToVector((String)argsStrings[1]))));
                        Voxeme voxComponent = theme.GetComponent<Voxeme>();
                        Vector3 testLocation = voxComponent.isGrasped
                            ? voxComponent.graspTracker.transform.position
                            : theme.transform.position;
                        //Vector3 testLocation = theme.transform.position;

                        if (voxComponent.isGrasped) {
                            if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]) +
                                                                 voxComponent.grasperCoord.root.gameObject
                                                                     .GetComponent<GraspScript>().graspTrackerOffset)) {
                                theme.transform.position = Helper.ParsableToVector(argsStrings[1]); //+
                                //voxComponent.grasperCoord.root.gameObject.GetComponent<GraspScript> ().graspTrackerOffset;
                                theme.transform.rotation = Quaternion.identity;
                            }

                            satisfied = true;
                            ReasonFromAffordances(em, null, predString,
                                voxComponent); // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                        }
                        else if (Helper.CloseEnough(testLocation, Helper.ParsableToVector(argsStrings[1]))) {
                            satisfied = true;
    //                        ReasonFromAffordances (predString, voxComponent);    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                            //theme.GetComponent<Rigging> ().ActivatePhysics (true);
                        }
                    }
                }
                else if (predString == "bind") {
                    // satisfy bind
                    satisfied = true;
                }
                else if (predString == "wait") {
                    // satisfy wait
                    if (!preds.waitTimer.Enabled) {
                        satisfied = true;
                    }
                }
                else if (predString == "reach") {
                    // satisfy reach
                    GameObject agent = GameObject.FindGameObjectWithTag("Agent");
                    GraspScript graspController = agent.GetComponent<GraspScript>();
                    //Debug.Log (graspController.isGrasping);
                    //if (graspController.isGrasping) {
                    //    satisfied = true;
                    //}
                    //Debug.Log (string.Format ("Reach {0}", satisfied));
                }
                else if (predString == "grasp") {
                    // satisfy grasp
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    GameObject agent = GameObject.FindGameObjectWithTag("Agent");

                    if ((agent.GetComponent<InteractionSystem>().IsPaused(FullBodyBipedEffector.LeftHand)) ||
                        (agent.GetComponent<InteractionSystem>().IsPaused(FullBodyBipedEffector.RightHand))) {
                        foreach (FixHandRotation handRot in theme.GetComponentsInChildren<FixHandRotation>()) {
                            handRot.enabled = false;
                        }

                        satisfied = true;
                    }

    //                if (theme != null) {
    //                    if (agent != null) {
    //                        if (theme.transform.IsChildOf (agent.transform)) {
    //                            satisfied = true;
    //                        }
    //                    }
    //                }
                }
                else if (predString == "hold") {
                    // satisfy hold
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    GameObject agent = GameObject.FindGameObjectWithTag("Agent");
                    if (theme != null) {
                        if (agent != null) {
                            if (theme.transform.IsChildOf(agent.transform)) {
                                satisfied = true;
                            }
                        }
                    }
                }
                else if (predString == "ungrasp") {
                    // satisfy ungrasp
                    GameObject theme = GameObject.Find(argsStrings[0] as String);
                    GameObject agent = GameObject.FindGameObjectWithTag("Agent");

                    if ((!agent.GetComponent<InteractionSystem>().IsPaused(FullBodyBipedEffector.LeftHand)) ||
                        (!agent.GetComponent<InteractionSystem>().IsPaused(FullBodyBipedEffector.RightHand))) {
                        foreach (FixHandRotation handRot in theme.GetComponentsInChildren<FixHandRotation>()) {
                            handRot.enabled = true;
                        }

                        satisfied = true;
    //                    ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
                    }

    //                GameObject theme = GameObject.Find (argsStrings [0] as String);
    //                GameObject agent = GameObject.FindGameObjectWithTag ("Agent");
    //                GraspScript graspController = agent.GetComponent<GraspScript> ();
    //                if (theme != null) {
    //                    if (agent != null) {
    //                        if (!theme.transform.IsChildOf (agent.transform)) {
    //                            if (!theme.GetComponent<Voxeme>().isGrasped) {
    //                                //Debug.Break ();
    //                                satisfied = true;
    //                                ReasonFromAffordances (predString, theme.GetComponent<Voxeme>());    // we need to talk (do physics reactivation in here?) // replace ReevaluateRelationships
    //                            }
    //                        }
    //                    }
    //                }
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
                    MethodInfo method = preds.GetType().GetMethod(predString.ToUpper());
                    if ((method != null) && (method.ReturnType == typeof(void))) {
                        EventManagerArgs eventArgs = null;
                        // is a program
                        if (em.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(predString) && 
                            em.voxmlLibrary.VoxMLEntityTypeDict[predString] == "programs") {
                        //string testPath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", predString));
                        //if (File.Exists(testPath)) {
                            VoxML voxml = em.voxmlLibrary.VoxMLObjectDict[predString];
                            //using (StreamReader sr = new StreamReader(testPath)) {
                            //    voxml = VoxML.LoadFromText(sr.ReadToEnd(), predString);
                            //}
                            eventArgs = new EventManagerArgs(voxml, test);
                        }
                        else {
                           eventArgs = new EventManagerArgs(test, isMacroEvent);
                        }
                        em.OnEventComplete(em, eventArgs);
                    }
                }

                return satisfied;
            }

            public static bool ComputeSatisfactionConditions(String command) {
                Hashtable predArgs = Helper.ParsePredicate(command);
                String pred = Helper.GetTopPredicate(command);
                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
                EventManager em = GameObject.Find("BehaviorController").GetComponent<EventManager>();
                bool validPredExists = false;

                if (predArgs.Count > 0) {
                    Queue<String> argsStrings = new Queue<String>(((String) predArgs[pred]).Split(new char[] {','}));
                    List<object> objs = new List<object>();
                    Predicates preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();
                    MethodInfo methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                    VoxML voxml = null;

                    methodToCall = preds.GetType().GetMethod(pred.ToUpper());
                    if (methodToCall == null) {
                        if ((em.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) &&
                            (em.voxmlLibrary.VoxMLEntityTypeDict[pred] == "programs")) {
                            voxml = em.voxmlLibrary.VoxMLObjectDict[pred];
                            methodToCall = preds.GetType().GetMethod("ComposeProgram");
                        }
                    }

                    if (methodToCall.ReturnType == typeof(void)) {
                        while (argsStrings.Count > 0) {
                            object arg = argsStrings.Dequeue();

                            if (Helper.vec.IsMatch((String) arg)) {
                                if (Helper.listVec.IsMatch((String) arg)) {
                                    // if arg is list of vectors form
                                    List<Vector3> vecList = new List<Vector3>();

                                    foreach (string vecString in ((String) arg).Replace("[","").Replace("]","").Split(':')) {
                                        vecList.Add(Helper.ParsableToVector(vecString));
                                    }
                                    Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",vecList));
                                    objs.Add(vecList);
                                }
                                else {
                                    // if arg is vector form
                                    Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",Helper.ParsableToVector((String) arg)));
                                    objs.Add(Helper.ParsableToVector((String) arg));
                                }
                            }
                            else if (Helper.emptyList.IsMatch((String) arg)) {
                                objs.Add(new List<object>());
                            }
                            else if (arg is String) {
                                // if arg is String
                                if ((arg as String) != string.Empty) {
                                    Regex q = new Regex("[\'\"].*[\'\"]");
                                    int i;
                                    if (int.TryParse(arg as String, out i)) {
                                        Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",i));
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
                                                Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",method));
                                                objs.Add(method);
                                            }
                                            else {
                                                Debug.Log(string.Format("No method {0} found in class {1}!",tryMethodPath.Last(),methodCallingType.Name));
                                            }
                                        } 
                                        else {
                                            Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",arg as String));
                                            objs.Add(arg as String);
                                        }
                                    }
                                    else {
                                        GameObject go = null;
                                        if ((arg as String).Count(f => f == '(') +
                                            (arg as String).Count(f => f == ')') == 0) {
                                            List<GameObject> matches = new List<GameObject>();
                                            foreach (Voxeme voxeme in objSelector.allVoxemes) {
                                                if (voxeme.voxml.Lex.Pred.Equals(arg)) {
                                                    matches.Add(voxeme.gameObject);
                                                }
                                            }

                                            //Debug.Log(string.Format("# voxeme predicate matches to string {0}: {1}",(arg as String),matches.Count));

                                            if (matches.Count == 0) {
                                                go = GameObject.Find(arg as String);
                                                if (go == null) {
                                                    for (int j = 0; j < objSelector.disabledObjects.Count; j++) {
                                                        if (objSelector.disabledObjects[j].name == (arg as String)) {
                                                            go = objSelector.disabledObjects[j];
                                                            break;
                                                        }
                                                    }

                                                    if (go == null) {
                                                        //OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
                                                        em.OnNonexistentEntityError(null,
                                                            new EventReferentArgs((arg as String)));
                                                        Debug.Log(string.Format("ComputeSatisfactionConditions: no object named {0}",
                                                            (arg as String)));
                                                    }
                                                }
                                                else {
                                                    if (go.GetComponent<Voxeme>() != null) {
                                                        if ((em.referents.stack.Count == 0) ||
                                                            (!em.referents.stack.Peek().Equals(go.name))) {
                                                            em.referents.stack.Push(go.name);
                                                        }

                                                        em.OnEntityReferenced(null, new EventReferentArgs(go.name));
                                                    }
                                                }

                                                Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",go));
                                                objs.Add(go);
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
                                                    //OutputHelper.PrintOutput (Role.Affector, string.Format ("What is that?", (arg as String)));
                                                    em.OnNonexistentEntityError(null, new EventReferentArgs((arg as String)));
                                                    Debug.LogError(string.Format("ComputeSatisfactionConditions: Aborting {0}",
                                                        em.events[0]));
                                                    return false; // abort
                                                }

                                                Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",go));
                                                objs.Add(go);
                                            }
                                            else {
                                                if (methodToCall != null) {
                                                    // found a method
                                                    Debug.Log(pred);
                                                    if ((!em.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) ||
                                                        (em.voxmlLibrary.VoxMLEntityTypeDict[pred] != "attributes")) {
                                                        //if (methodToCall.ReturnType == typeof(void)) {
                                                        //if (!em.evalOrig.ContainsKey(command)){
                                                        Debug.Log(string.Format("Which {0}?", (arg as String)));
                                                        //OutputHelper.PrintOutput(Role.Affector, string.Format("Which {0}?", (arg as String)));
                                                        em.OnDisambiguationError(null, new EventDisambiguationArgs(command,
                                                            string.Empty, string.Empty,
                                                            matches.Select(o => o.GetComponent<Voxeme>()).ToArray()));
                                                        Debug.LogError(string.Format("ComputeSatisfactionConditions: Aborting {0}",
                                                            em.events[0]));
                                                        return false; // abort
                                                    }

                                                    //}
                                                    foreach (GameObject match in matches) {
                                                        Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",match));
                                                        objs.Add(match);
                                                    }
                                                }

                                                //}
                                            }
                                        }

                                        //                            objs.Add (GameObject.Find (arg as String));
                                    }
                                }
                            }
                        }
                    }
                    else if (methodToCall.ReturnType == typeof(bool)) {
                        while (argsStrings.Count > 0) {
                            object arg = argsStrings.Dequeue();

                            if (arg is String) {
                                Debug.Log(string.Format("ComputeSatisfactionConditions: adding {0} to objs",arg));
                                objs.Add(arg);
                            }
                        }
                    }
                    else {
                        // not a program or conditional
                        Debug.Log(string.Format("ComputeSatisfactionConditions: {0} is not a program or conditional! Returns {1}",
                            methodToCall.Name, methodToCall.ReturnType));
                    }

                    objs.Add(false);

                    if (methodToCall != null) {
                        // found a method
                        if (methodToCall.ReturnType == typeof(void)) {
                            // is it a program?
                            Debug.Log(string.Format("ComputeSatisfactionConditions: invoke {0} with {1}{2}",
                                methodToCall.Name, (voxml == null) ? string.Empty : "\"" + voxml.Lex.Pred + "\", ", objs));
                            object obj = null;
 
                            if (voxml == null) {
                                obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                            }
                            else {
                                obj = methodToCall.Invoke(preds, new object[] {voxml, objs.ToArray()});
                            } 
                        }
                        else if (methodToCall.ReturnType == typeof(bool)) {
                            // is it a conditional?
                            Debug.Log("ComputeSatisfactionConditions: invoke " + methodToCall.Name);
                            object obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                        }
                        else {
                            // not a program or conditional
                            Debug.Log(string.Format("ComputeSatisfactionConditions: {0} is not a program or conditional! Returns {1}",
                                methodToCall.Name, methodToCall.ReturnType));
                            object obj = methodToCall.Invoke(preds, new object[] {objs.ToArray()});
                            if (obj is String) {
                                Debug.Log(obj as String);
                                if (GameObject.Find(obj as String) == null) {
                                    em.OnNonexistentEntityError(null, new EventReferentArgs(
                                        new Pair<string, List<object>>(pred, objs.GetRange(0, objs.Count - 1))));
                                    Debug.LogError(string.Format("ComputeSatisfactionConditions: Aborting {0}",
                                        em.events[0]));
                                    return false;
                                }
                                else {
                                    if (GameObject.Find(obj as String).GetComponent<Voxeme>() != null) {
                                        if ((em.referents.stack.Count == 0) || (!em.referents.stack.Peek().Equals(obj))) {
                                            em.referents.stack.Push(obj);
                                        }

                                        em.OnEntityReferenced(null, new EventReferentArgs(obj));
                                    }
                                }
                            }
                        }
                    }
                    else {
                        // no coded-behavior
                        // see if a VoxML markup exists
                        // if so, we might be able to figure this out
                        if ((!em.voxmlLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) ||
                            (em.voxmlLibrary.VoxMLEntityTypeDict[pred] != "programs")) {
                            // otherwise return error
                            OutputHelper.PrintOutput(Role.Affector, "Sorry, what does " + "\"" + pred + "\" mean?");
                            Debug.LogError(string.Format("ComputeSatisfactionConditions: Aborting {0}",
                                em.events[0]));
                            return false;
                        }
                    }
                }
                else {
                    List<object> objs = em.ExtractObjects(string.Empty, pred);
                    if (objs.Count > 0) {
                        foreach (var obj in objs) {
                            if (obj is GameObject) {
                                if ((obj as GameObject).GetComponent<Voxeme>() != null) {
                                    if ((em.referents.stack.Count == 0) ||
                                        (!em.referents.stack.Peek().Equals(((GameObject) obj).name))) {
                                        em.referents.stack.Push(((GameObject) obj).name);
                                    }

                                    em.OnEntityReferenced(null, new EventReferentArgs(((GameObject) obj).name));
                                }
                            }
                        }
                    }
                    else {
                        em.OnNonexistentEntityError(null, new EventReferentArgs(pred));
                        //OutputHelper.PrintOutput (Role.Affector,"Sorry, I don't understand \"" + command + ".\"");
                        Debug.LogError(string.Format("ComputeSatisfactionConditions: Aborting {0}",
                            em.events[0]));
                        return false;
                    }
                }

                return true;
            }

            public static void ReasonFromAffordances(EventManager eventManager, VoxML program, String predString, Voxeme obj) {
                Regex reentrancyForm = new Regex(@"\[[0-9]+\]");
                Regex groundComponentFirst = new Regex(@".*(\[[0-9]+\], .*x.*)"); // check the order of the arguments
                Regex groundComponentSecond = new Regex(@".*(x, .*\[[0-9]+\].*)");
                List<string> supportedRelations = new List<string>(
                    new[] {
                        // list of supported relations
                        @"on\(.*\)",
                        @"in\(.*\)",
                        @"under\(.*\)"
                    }); // TODO: move externally, draw from voxeme database
                List<string> genericRelations = new List<string>(
                    new[] {
                        // list of habitat-independent relations
                        @"under\(.*\)",
                        @"behind\(.*\)",
                        @"in_front\(.*\)",
                        @"left\(.*\)",
                        @"right\(.*\)",
                        @"touching\(.*\)"
                    }); // TODO: move externally, draw from voxeme database

                // get relation tracker
                RelationTracker relationTracker =
                    (RelationTracker) GameObject.Find("BehaviorController").GetComponent("RelationTracker");

                ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();

                // get bounds of theme object of program
                List<GameObject> excludeChildren = obj.gameObject.GetComponentsInChildren<Renderer>().Where(
                        o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != obj.gameObject))
                    .Select(v => v.gameObject)
                    .ToList();
                Bounds objBounds = Helper.GetObjectWorldSize(obj.gameObject, excludeChildren);

                // get list of all voxeme entities that are not components of other voxemes
    //            Voxeme[] allVoxemes = objSelector.allVoxemes.Where(a => // where there does not exist another voxeme that has this voxeme as a component
    //                objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a.gameObject).ToList().Count == 0)).ToArray();
                Voxeme[] allVoxemes = objSelector.allVoxemes.Where(a =>
                        !objSelector.allVoxemes.SelectMany(
                                (v, c) => v.opVox.Type.Components.Where(
                                    comp => comp.Item2 != v.gameObject).Select(comp => comp.Item2)).ToList()
                            .Contains(a.gameObject))
                    .ToArray();

                List<GameObject> components = objSelector.allVoxemes.SelectMany((v, c) =>
                    v.opVox.Type.Components.Where(comp => comp.Item2 != v.gameObject).Select(comp => comp.Item2)).ToList();

                //foreach (GameObject go in components) {
                //    Debug.Log(go);
                //}

                //foreach (Voxeme v in allVoxemes) {
                //    Debug.Log(v);
                //}
    //            objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a.gameObject).ToList().Count == 0)

    //                UnityEngine.Object.FindObjectsOfType<Voxeme>().Where(a => 
    //                objSelector.allVoxemes.Where(v => v.opVox.Type.Components.Where(c => c.Item2 == a)) 
    //                a.isActiveAndEnabled).ToArray();

                // reactivate physics by default
                //PhysicsHelper.ResolvePhysicsDiscepancies(obj.gameObject);
                bool reactivatePhysics = true;
                //if (Helper.IsTopmostVoxemeInHierarchy(obj.gameObject)){
                //    obj.minYBound = objBounds.min.y;    //TODO: did removing this really fix the bug where
                //}                                        // a turned object would go through the supporting surface?
                // did that cause any other bugs?


                // check existing relations
                // if obj is in support or containment relation w/ concave obj
                //Debug.Log (relationTracker.relations.Count);
                foreach (DictionaryEntry relation in relationTracker.relations) {
                    if (((String) relation.Value).Contains("support") || ((String) relation.Value).Contains("contain")) {
                        Debug.Log(string.Format("==== {0} {1} {2} ====", ((List<GameObject>) relation.Key)[0],
                            ((String) relation.Value), ((List<GameObject>) relation.Key)[1]));
    //                    Debug.Log (((List<GameObject>)relation.Key) [1]);
    //                    Debug.Log (obj.gameObject);
    //                    Debug.Log (((List<GameObject>)relation.Key) [1] == obj.gameObject);
                        if (((List<GameObject>) relation.Key)[0] == obj.gameObject) {
                            if (TestRelation(((List<GameObject>) relation.Key)[1], "on", obj.gameObject) ||
                                TestRelation(((List<GameObject>) relation.Key)[1], "in", obj.gameObject)) {
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
                    if (TestHabitat(obj.gameObject, objHabitat)) {
    //                    Debug.Log (objHabitat);
                        foreach (Voxeme test in allVoxemes) {
                            if (test.gameObject != obj.gameObject) {
                                // foreach voxeme
                                // get bounds of object being tested against
                                Bounds testBounds = Helper.GetObjectWorldSize(test.gameObject);
                                if (!test.gameObject.name.Contains("*")) {
                                    // hacky fix to filter out unparented objects w/ disabled voxeme components

                                    // habitat-independent relation handling
                                    foreach (string rel in genericRelations) {
                                        string relation = rel.Split('\\')[0]; // not using relation as regex here

                                        //Debug.Log (string.Format ("Is {0} {1} {2}?", obj.gameObject.name, relation, test.gameObject.name));
                                        if (TestRelation(obj.gameObject, relation, test.gameObject)) {
                                            relationTracker.AddNewRelation(
                                                new List<GameObject> {obj.gameObject, test.gameObject}, relation);
                                        }
                                        else {
                                            // remove if present
                                            relationTracker.RemoveRelation(
                                                new List<GameObject> {obj.gameObject, test.gameObject}, relation);
                                        }

                                        if (TestRelation(test.gameObject, relation, obj.gameObject)) {
                                            relationTracker.AddNewRelation(
                                                new List<GameObject> {test.gameObject, obj.gameObject}, relation);
                                        }
                                        else {
                                            // remove if present
                                            relationTracker.RemoveRelation(
                                                new List<GameObject> {test.gameObject, obj.gameObject}, relation);
                                        }
                                    }

                                    //if (test.enabled) {    // if voxeme is active
    //                                Debug.Log(test);
                                    foreach (int testHabitat in test.opVox.Affordance.Affordances.Keys) {
                                        //if (TestHabitat (test.gameObject, testHabitat)) {    // test habitats
                                        for (int i = 0; i < test.opVox.Affordance.Affordances[testHabitat].Count; i++) {
                                            // condition/event/result list for this habitat index
                                            string ev = test.opVox.Affordance.Affordances[testHabitat][i].Item2.Item1;
                                            Debug.Log(ev);
                                            if (ev.Contains(predString) || ev.Contains("put")) {
                                                // TODO: resultant states should persist
    //                                                Debug.Break ();
    //                                                Debug.Log (ev);
    //                                                Debug.Log (obj.name);
    //                                                Debug.Log (test.name);
                                                //Debug.Log (test.opVox.Lex.Pred);
                                                //Debug.Log (program);

                                                foreach (string rel in supportedRelations) {
                                                    Regex r = new Regex(rel);
                                                    if (r.Match(ev).Length > 0) {
                                                        // found a relation that might apply between these objects
                                                        string relation = r.Match(ev).Groups[0].Value.Split('(')[0];
                                                        Debug.Log(relation);

                                                        MatchCollection matches = reentrancyForm.Matches(ev);
                                                        foreach (Match m in matches) {
                                                            foreach (Group g in m.Groups) {
                                                                int componentIndex = Helper.StringToInt(
                                                                    g.Value.Replace(g.Value,
                                                                        g.Value.Trim(new char[] {'[', ']'})));
                                                                //Debug.Log (componentIndex);
                                                                if (test.opVox.Type.Components.FindIndex(c =>
                                                                        c.Item3 == componentIndex) != -1) {
                                                                    Triple<string, GameObject, int> component =
                                                                        test.opVox.Type.Components.First(c =>
                                                                            c.Item3 == componentIndex);
                                                                    Debug.Log(ev.Replace(g.Value, component.Item2.name));
                                                                    Debug.Log(string.Format("Is {0} {1} {2}?",
                                                                        obj.gameObject.name, relation,
                                                                        component.Item2.name));

                                                                    //bool relationSatisfied = false;    // this used to be here

                                                                    //NOTE: These relations use the *test* object as theme
                                                                    if (groundComponentFirst.Match(ev).Length > 0) {
                                                                        relationSatisfied = TestRelation(test.gameObject,
                                                                            relation, obj.gameObject);
                                                                        //Debug.Break ();
                                                                        //Debug.Log (test);
                                                                        //Debug.Log (obj);
                                                                    }
                                                                    else if (groundComponentSecond.Match(ev).Length > 0) {
                                                                        relationSatisfied = TestRelation(obj.gameObject,
                                                                            relation, test.gameObject);
                                                                    }

                                                                    if (relationSatisfied) {
                                                                        Debug.Log(test.opVox.Affordance.Affordances[
                                                                            testHabitat][i].Item2.Item1);
                                                                        Debug.Log(test.opVox.Affordance.Affordances[
                                                                            testHabitat][i].Item2.Item2);
                                                                        result = test.opVox.Affordance.Affordances[
                                                                            testHabitat][i].Item2.Item2;

                                                                        // things are getting a little ad hoc here
                                                                        if (relation == "on") {
                                                                            Debug.Log(Helper
                                                                                .GetMostImmediateParentVoxeme(
                                                                                    test.gameObject).GetComponent<Voxeme>()
                                                                                .voxml.Type.Concavity.Contains("Concave"));
                                                                            Debug.Log(Helper.VectorToParsable(
                                                                                objBounds.size));
                                                                            Debug.Log(Helper.VectorToParsable(testBounds
                                                                                .size));
                                                                            Debug.Log(Helper.FitsIn(objBounds, testBounds));
                                                                            if (!((Helper
                                                                                      .GetMostImmediateParentVoxeme(
                                                                                          test.gameObject)
                                                                                      .GetComponent<Voxeme>().voxml.Type
                                                                                      .Concavity.Contains("Concave")) &&
                                                                                  (Helper.FitsIn(objBounds, testBounds)))) {
                                                                                //if (obj.enabled) {
                                                                                //    obj.gameObject.GetComponent<Rigging> ().ActivatePhysics (true);
                                                                                //}
                                                                                obj.minYBound = testBounds.max.y;
                                                                                //                                                                                Debug.Log (test);
                                                                            }
                                                                            else {
                                                                                reactivatePhysics = false;
                                                                                obj.minYBound = objBounds.min.y;
                                                                            }
                                                                        }
                                                                        else if (relation == "in") {
                                                                            reactivatePhysics = false;
                                                                            obj.minYBound = objBounds.min.y;
                                                                        }
    //                                                                        else if (relation == "under") {
    //                                                                            GameObject voxObj = Helper.GetMostImmediateParentVoxeme (test.gameObject);
    //                                                                            if ((voxObj.GetComponent<Voxeme> ().voxml.Type.Concavity == "Concave") &&    // this is a concave object
    //                                                                                (Concavity.IsEnabled (voxObj)) && (Mathf.Abs (Vector3.Dot (voxObj.transform.up, Vector3.up) + 1.0f) <= Constants.EPSILON)) { // TODO: Run this through habitat verification
    //                                                                                reactivatePhysics = false;
    //                                                                                obj.minYBound = objBounds.min.y;
    //                                                                            }
    //                                                                        }

                                                                        // TODO: only instantiate a relation if it goes both ways (i.e. only if x can be contained AND y can contain something
                                                                        if (result != "") {
                                                                            result = result.Replace("x",
                                                                                test.gameObject.name);
                                                                            // any component reentrancy ultimately inherits from the parent voxeme itself
                                                                            result = reentrancyForm.Replace(result,
                                                                                obj.gameObject.name);
                                                                            result = Helper.GetTopPredicate(result);

                                                                            // TODO: maybe switch object order here below => passivize relation?
                                                                            if (groundComponentFirst.Match(ev).Length > 0) {
                                                                                relationTracker.AddNewRelation(
                                                                                    new List<GameObject>
                                                                                        {obj.gameObject, test.gameObject},
                                                                                    result);
                                                                                Debug.Log(string.Format(
                                                                                    "{0}: {1} {2}.3sg {3}",
                                                                                    test.opVox.Affordance.Affordances[
                                                                                        testHabitat][i].Item2.Item1,
                                                                                    obj.gameObject.name, result,
                                                                                    test.gameObject.name));
                                                                            }
                                                                            else if (groundComponentSecond.Match(ev)
                                                                                         .Length > 0) {
                                                                                relationTracker.AddNewRelation(
                                                                                    new List<GameObject>
                                                                                        {test.gameObject, obj.gameObject},
                                                                                    result);
                                                                                Debug.Log(string.Format(
                                                                                    "{0}: {1} {2}.3sg {3}",
                                                                                    test.opVox.Affordance.Affordances[
                                                                                        testHabitat][i].Item2.Item1,
                                                                                    test.gameObject.name, result,
                                                                                    obj.gameObject.name));
                                                                            }

                                                                            if (result == "support") {
                                                                                if (groundComponentFirst.Match(ev).Length >
                                                                                    0) {
                                                                                    RiggingHelper.RigTo(test.gameObject,
                                                                                        obj.gameObject);
                                                                                }
                                                                                else if (groundComponentSecond.Match(ev)
                                                                                             .Length > 0) {
                                                                                    RiggingHelper.RigTo(obj.gameObject,
                                                                                        test.gameObject);
                                                                                }
                                                                            }
                                                                            else if (result == "contain") {
                                                                                if (groundComponentFirst.Match(ev).Length >
                                                                                    0) {
                                                                                    RiggingHelper.RigTo(test.gameObject,
                                                                                        obj.gameObject);
                                                                                }
                                                                                else if (groundComponentSecond.Match(ev)
                                                                                             .Length > 0) {
                                                                                    RiggingHelper.RigTo(obj.gameObject,
                                                                                        test.gameObject);
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
                Debug.Log(string.Format("{0} # habitat indices = {1}", obj.name, affStr.Affordances.Keys.Count));
                foreach (int objHabitat in affStr.Affordances.Keys) {
                    if (TestHabitat(obj.gameObject, objHabitat)) {
                        // test habitats
                        for (int i = 0; i < affStr.Affordances[objHabitat].Count; i++) {
                            // condition/event/result list for this habitat index
                            string ev = affStr.Affordances[objHabitat][i].Item2.Item1;
                            Debug.Log (string.Format("{0}: Testing {1}",obj.name, ev));
                            if (Helper.GetTopPredicate(ev) == predString) {
                                bool relationIndependent = true;
                                foreach (string rel in supportedRelations) {
                                    Regex r = new Regex(rel);
                                    if (r.Match(ev).Length > 0) {
                                        relationIndependent = false;
                                    }
                                }

                                if (relationIndependent) {
    //                                Debug.Log (obj.opVox.Lex.Pred);
                            
                                    result = affStr.Affordances[objHabitat][i].Item2.Item2.Replace(" ",string.Empty);
                                    //Debug.Log (result);

                                    if (result != "") {
                                        // look for agent in program VoxML arg structure
                                        string agentVar = string.Empty;
                                        //Debug.Log(program.Lex.Pred);
                                        try {
                                            foreach (VoxTypeArg arg in program.Type.Args) {
                                                string argName = arg.Value.Split(':')[0];
                                                string[] argType = arg.Value.Split(':')[1].Split('*');

                                                if ((argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.Agent).ToList().Count > 0) ||
                                                    (argType.Where(a => GenLex.GenLex.GetGLType(a) == GLType.AgentList).ToList().Count > 0)) {
                                                    agentVar = argName; 
                                                }
                                            }
                                        }
                                        catch (Exception ex) {
                                            if (ex is NullReferenceException) {
                                                if (program == null) {
                                                    Debug.LogWarning(string.Format("Check if VoxML encoding exists for \"{0}!\"",Helper.GetTopPredicate(ev)));
                                                }
                                            }
                                        }

                                        if (agentVar != string.Empty) {
                                            result = result.Replace(agentVar, eventManager.GetActiveAgent().name);
                                        }

                                        // any component reentrancy ultimately inherits from the parent voxeme itself
                                        result = reentrancyForm.Replace(result, obj.gameObject.name);
                                        Debug.Log(string.Format("Satisfied event: {0}; Result: {1}",
                                            affStr.Affordances[objHabitat][i].Item2.Item1, result));

                                        string resultPred = Helper.GetTopPredicate(result);

                                        Hashtable predArgs = Helper.ParsePredicate(result);
                                        var objs = eventManager.ExtractObjects(resultPred, (String)predArgs[resultPred]);
                                        List<GameObject> relationObjs = new List<GameObject>();
                                        foreach (object o in objs) {
                                            if (o is GameObject) {
                                                relationObjs.Add(o as GameObject);
                                            }
                                        }

                                        if (resultPred != "release") {
                                            // TODO: maybe switch object order here below => passivize relation?
                                            relationTracker.AddNewRelation(relationObjs, resultPred);
                                        }
                                        else {
                                            relationTracker.RemoveRelation(relationObjs, "hold");
                                        }

                                        //Debug.Break ();
                                        if (resultPred == "hold") {
                                            // unparent the held object from any other objects, unless its current parent is the one
                                            //  doing the holding
                                            if (obj.gameObject.transform.parent != null) {
                                                if (!relationObjs.Except(new List<GameObject>() { obj.gameObject }).ToList().
                                                    Contains(obj.gameObject.transform.parent.gameObject)) {
                                                    RiggingHelper.UnRig(obj.gameObject, obj.gameObject.transform.parent.gameObject);
                                                }
                                            }
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
    //                    Debug.Log(obj.name);
    //                    Debug.Break ();
                        Rigging rigging = obj.gameObject.GetComponent<Rigging>();
                        if (rigging != null) {
                            //TODO:reenable
                            rigging.ActivatePhysics(true);
                        }

                        //PhysicsHelper.ResolvePhysicsDiscepancies(obj.gameObject);
                    }
                }
            }

            public static bool TestHabitat(GameObject obj, int habitatIndex) {
                HabitatSolver habitatSolver = GameObject.Find("BehaviorController").GetComponent<HabitatSolver>();

                MethodInfo methodToCall;
                bool r = true;

    //            Debug.Log (string.Format ("H[{0}]", habitatIndex));
                if (habitatIndex != 0) {
                    // index 0 = affordance enabled in all habitats
                    OperationalVox opVox = obj.GetComponent<Voxeme>().opVox;
                    if (opVox != null) {
                        r = true;
                        if (opVox.Habitat.IntrinsicHabitats.ContainsKey(habitatIndex)) {
                            // do intrinsic habitats first
                            List<String> conditioningEnvs = opVox.Habitat.IntrinsicHabitats[habitatIndex];
                            foreach (String env in conditioningEnvs) {
                                string label = env.Split('=')[0].Trim();
                                string formula = env.Split('=')[1].Trim(new char[] {' ', '{', '}'});
                                string methodName = formula.Split('(')[0].Trim();
                                string[] methodArgs = new string[] {string.Empty};

                                if (formula.Split('(').Length > 1) {
                                    methodArgs = formula.Split('(')[1].Trim(')').Split(',');
                                }

                                List<object> args = new List<object>();
                                args.Add(obj);
                                foreach (string arg in methodArgs) {
                                    args.Add(arg);
                                }

                                methodToCall = habitatSolver.GetType().GetMethod(methodName);
                                if (methodToCall != null) {
                                    object result = methodToCall.Invoke(habitatSolver, new object[] {args.ToArray()});
                                    r &= (bool) result;
                                }
                            }
                        }

                        if (opVox.Habitat.ExtrinsicHabitats.ContainsKey(habitatIndex)) {
                            // then do extrinsic habitats
                            List<String> conditioningEnvs = opVox.Habitat.ExtrinsicHabitats[habitatIndex];
                            foreach (String env in conditioningEnvs) {
                                string label = env.Split('=')[0].Trim();
                                string formula = env.Split('=')[1].Trim(new char[] {' ', '{', '}'});
                                string methodName = formula.Split('(')[0].Trim();
                                string[] methodArgs = formula.Split('(')[1].Trim(')').Split(',');

                                List<object> args = new List<object>();
                                args.Add(obj);
                                foreach (string arg in methodArgs) {
                                    args.Add(arg);
                                }

                                methodToCall = habitatSolver.GetType().GetMethod(methodName);
                                if (methodToCall != null) {
                                    object result = methodToCall.Invoke(habitatSolver, new object[] {args.ToArray()});
                                    r &= (bool) result;
                                }
                            }
                        }

                        //flip(cup1);put(ball,under(cup1))
                    }
                }

                Debug.Log(string.Format("{0}: H[{1}]:{2}", obj.name, habitatIndex, r));
                return r;
            }

            public static bool TestRelation(GameObject obj1, string relation, GameObject obj2) {
                bool r = false;

                Bounds bounds1 = Helper.GetObjectWorldSize(obj1);
                Bounds bounds2 = Helper.GetObjectWorldSize(obj2);

                Regex align = new Regex(@"align\(.+,.+\)");
                List<string> habitats = new List<string>();
                foreach (int i in Helper.GetMostImmediateParentVoxeme(obj2).GetComponent<Voxeme>().opVox.Habitat
                    .IntrinsicHabitats.Keys) {
                    habitats.AddRange(Helper.GetMostImmediateParentVoxeme(obj2).GetComponent<Voxeme>().opVox.Habitat
                        .IntrinsicHabitats[i].Where((h => align.IsMatch(h))));
                }

                for (int i = 0; i < habitats.Count; i++) {
                    habitats[i] = align.Match(habitats[i]).Value.Replace("align(", "").Replace(")", "").Split(',')[0];
                }

                // (default to Y-alignment if no encoding exists)
                if (habitats.Count == 0) {
                    habitats.Add("Y");
                }

                if (relation == "on") {
                    // TODO: needs to be fixed: PO, TPP(i), NTPP(i) for contacting regions along axis; relation satisfied only within EPSILON radius of ground obj position
                    foreach (string axis in habitats) {
                        //Debug.Break ();
    //                    Debug.Log (obj1);
    //                    Debug.Log (obj2);
    //                    Debug.Log (Concavity.IsEnabled(obj1));
    //                    Debug.Log (Helper.GetMostImmediateParentVoxeme (obj1.gameObject));
                        List<GameObject> excludeChildren = obj1.GetComponentsInChildren<Renderer>().Where(
                                o => (Helper.GetMostImmediateParentVoxeme(o.gameObject) != obj1)).Select(v => v.gameObject)
                            .ToList();
                        Bounds adjustedBounds1 = Helper.GetObjectWorldSize(obj1, excludeChildren);
                        if ((Helper.GetMostImmediateParentVoxeme(obj2.gameObject).GetComponent<Voxeme>().voxml.Type
                                .Concavity.Contains("Concave")) &&
                            (Concavity.IsEnabled(obj2)) && (Helper.FitsIn(adjustedBounds1, bounds2))) {
                            // if ground object is concave and figure object would fit inside
                            switch (axis) {
                                case "X":
                                    r = (Vector3.Distance(
                                             new Vector3(obj2.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj1.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                case "Y":
                                    r = (Vector3.Distance(
                                             new Vector3(obj1.gameObject.transform.position.x,
                                                 obj2.gameObject.transform.position.y,
                                                 obj1.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    r &= (obj1.gameObject.transform.position.y > obj2.gameObject.transform.position.y);
                                    break;

                                case "Z":
                                    r = (Vector3.Distance(
                                             new Vector3(obj1.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj2.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                default:
                                    break;
                            }

                            r &= RCC8.PO(bounds1, bounds2);
                        }
                        else if ((Helper.GetMostImmediateParentVoxeme(obj1.gameObject).GetComponent<Voxeme>().voxml.Type
                                     .Concavity.Contains("Concave")) &&
                                 (!Concavity.IsEnabled(obj1)) && (Helper.FitsIn(bounds2, bounds1))) {
                            switch (axis) {
                                case "X":
                                    r = (Vector3.Distance(
                                             new Vector3(obj2.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj1.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                case "Y":
                                    r = (Vector3.Distance(
                                             new Vector3(obj1.gameObject.transform.position.x,
                                                 obj2.gameObject.transform.position.y,
                                                 obj1.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    r &= (obj1.gameObject.transform.position.y > obj2.gameObject.transform.position.y);
                                    break;

                                case "Z":
                                    r = (Vector3.Distance(
                                             new Vector3(obj1.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj2.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                default:
                                    break;
                            }

                            r &= RCC8.PO(bounds1, bounds2);
                        }
                        else {
                            switch (axis) {
                                case "X":
                                    r = (Vector3.Distance(
                                             new Vector3(obj2.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj1.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                case "Y":
                                    ObjBounds objBounds1 = Helper.GetObjectOrientedSize(obj1, true);
                                    ObjBounds objBounds2 = Helper.GetObjectOrientedSize(obj2, true);
                                    //Debug.Log(Helper.VectorToParsable(objBounds1.Center));
                                    //Debug.Log(Helper.VectorToParsable(objBounds2.Center));
                                    //Debug.Log (string.Format("XZ_off({0},{1}) = {2}",obj1,obj2,Vector3.Distance (
                                    //new Vector3 (objBounds1.Center.x, objBounds2.Center.y, objBounds1.Center.z),
                                    //objBounds2.Center)));
                                    Debug.Log(string.Format("EC_Y({0},{1}):{2}", obj1, obj2,
                                        RCC8.EC(objBounds1, objBounds2)));
                                    Bounds b1 = new Bounds(
                                        new Vector3(objBounds1.Center.x, objBounds2.Min(MajorAxis.Y).y,
                                            objBounds1.Center.z),
                                        new Vector3(objBounds1.Max(MajorAxis.X).x - objBounds1.Min(MajorAxis.X).x, 0.0f,
                                            objBounds1.Max(MajorAxis.Z).z - objBounds1.Min(MajorAxis.Z).z));
                                    Bounds b2 = new Bounds(
                                        new Vector3(objBounds1.Center.x, objBounds2.Min(MajorAxis.Y).y,
                                            objBounds1.Center.z),
                                        new Vector3(objBounds2.Max(MajorAxis.X).x - objBounds2.Min(MajorAxis.X).x, 0.0f,
                                            objBounds2.Max(MajorAxis.Z).z - objBounds2.Min(MajorAxis.Z).z));
                                    //Debug.Log(string.Format("{0} {1}", Helper.VectorToParsable(b1.center), Helper.VectorToParsable(b2.center)));
                                    //Debug.Log(string.Format("{0} {1}", Helper.VectorToParsable(b1.size), Helper.VectorToParsable(b2.size)));
                                    r = (b1.Intersects(b2) &&
                                         ((objBounds2.Max(MajorAxis.Y).y - objBounds1.Min(MajorAxis.Y).y) <=
                                          Constants.EPSILON));
                                    //r = b1.Intersects(b2);
                                    Debug.Log(r);
                                    //r = (Vector3.Distance (
                                    //new Vector3 (objBounds1.Center.x, objBounds2.Center.y, objBounds1.Center.z),
                                    //objBounds2.Center) <= Constants.EPSILON * 3); // works with 10
                                    break;

                                case "Z":
                                    r = (Vector3.Distance(
                                             new Vector3(obj1.gameObject.transform.position.x,
                                                 obj1.gameObject.transform.position.y,
                                                 obj2.gameObject.transform.position.z),
                                             obj2.gameObject.transform.position) <= Constants.EPSILON * 3);
                                    break;

                                default:
                                    break;
                            }

                            r &= RCC8.EC(Helper.GetObjectOrientedSize(obj1, true),
                                Helper.GetObjectOrientedSize(obj2, true));
                        }
                    }
                }
                else if (relation == "in") {
                    if ((Helper.GetMostImmediateParentVoxeme(obj2.gameObject).GetComponent<Voxeme>().voxml.Type.Concavity
                            .Contains("Concave")) &&
                        (Concavity.IsEnabled(obj2))) {
                        if (Helper.FitsIn(bounds1, bounds2)) {
                            Debug.Log(obj1);
                            Debug.Log(obj2);
                            Debug.Log(bounds1);
                            Debug.Log(bounds2);
                            r = RCC8.PO(bounds1, bounds2) || RCC8.ProperPart(bounds1, bounds2);
                        }
                    }
                    else {
                        if (Helper.FitsIn(bounds1, bounds2)) {
                            //Debug.Break ();
                            Debug.Log(obj1);
                            Debug.Log(obj2);
                            Debug.Log(bounds1);
                            Debug.Log(bounds2);
                            r = RCC8.PO(bounds1, bounds2) || RCC8.ProperPart(bounds1, bounds2);
                        }
                    }
                }
                else if (relation == "under") {
                    //Debug.Log (obj1.name);
                    //Debug.Log (new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z));
                    //Debug.Log (obj2.name);
                    //Debug.Log (obj2.transform.position);
                    float dist = Vector3.Distance(
                        new Vector3(obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y,
                            obj1.gameObject.transform.position.z),
                        obj2.gameObject.transform.position);
                    //Debug.Log (Vector3.Distance (
                    //    new Vector3 (obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
                    //    obj2.gameObject.transform.position));
                    r = (Vector3.Distance(
                             new Vector3(obj1.gameObject.transform.position.x, obj2.gameObject.transform.position.y,
                                 obj1.gameObject.transform.position.z),
                             obj2.gameObject.transform.position) <= Constants.EPSILON);
                    r &= (obj1.gameObject.transform.position.y < obj2.gameObject.transform.position.y);
                }
                // add generic relations--left, right, etc.
                // TODO: must transform to camera perspective if relative persp is on
                else if (relation == "behind") {
                    r = QSR.Behind(bounds1, bounds2) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.X, true)) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y, true));
    //                r = (Vector3.Distance (
    //                    new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
    //                    obj2.gameObject.transform.position) <= Constants.EPSILON);
    //                r &= (obj1.gameObject.transform.position.z > obj2.gameObject.transform.position.z);
                }
                else if (relation == "in_front") {
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.InFront(bounds1,bounds2)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.X)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.X,true)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.X)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.X,true)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.X)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.X,true)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Finishes(bounds1,bounds2,MajorAxis.X)));
    //                Debug.Log(string.Format("{0} {1}:{2}",obj1,obj2,QSR.Finishes(bounds1,bounds2,MajorAxis.X,true)));

                    r = QSR.InFront(bounds1, bounds2) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.X, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.X) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.X, true)) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y, true));
    //                r = (Vector3.Distance (
    //                    new Vector3 (obj1.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj2.gameObject.transform.position.z),
    //                    obj2.gameObject.transform.position) <= Constants.EPSILON);
    //                r &= (obj1.gameObject.transform.position.z < obj2.gameObject.transform.position.z);
                }
                else if (relation == "left") {
    //                Debug.Log(string.Format("{0} {3} of {1}:{2}",obj1,obj2,QSR.Left(bounds1,bounds2),relation));
    //                Debug.Log(string.Format("{0} overlaps {1}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.Z)));
    //                Debug.Log(string.Format("{1} overlaps {0}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.Z,true)));
    //                Debug.Log(string.Format("{0} during {1}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.Z)));
    //                Debug.Log(string.Format("{1} during {0}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.Z,true)));
    //                Debug.Log(string.Format("{0} starts {1}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.Z)));
    //                Debug.Log(string.Format("{1} starts {0}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.Z,true)));
    //                Debug.Log(string.Format("{0} finishes {1}:{2}",obj1,obj2,QSR.Finishes(bounds1,bounds2,MajorAxis.Z)));
    //                Debug.Log(string.Format("{1} finishes {0}:{2}",obj1,obj2,QSR.Finishes(bounds1,bounds2,MajorAxis.Z,true)));
    //                Debug.Log(string.Format("{0} overlaps {1}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.Y)));
    //                Debug.Log(string.Format("{1} overlaps {0}:{2}",obj1,obj2,QSR.Overlaps(bounds1,bounds2,MajorAxis.Y,true)));
    //                Debug.Log(string.Format("{0} during {1}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.Y)));
    //                Debug.Log(string.Format("{1} during {0}:{2}",obj1,obj2,QSR.During(bounds1,bounds2,MajorAxis.Y,true)));
    //                Debug.Log(string.Format("{0} starts {1}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.Y)));
    //                Debug.Log(string.Format("{1} starts {0}:{2}",obj1,obj2,QSR.Starts(bounds1,bounds2,MajorAxis.Y,true)));
    //                Debug.Log(string.Format("{0} finishes {1}:{2}",obj1,obj2,QSR.Finishes(bounds1,bounds2,MajorAxis.Y)));
    //                Debug.Log (string.Format ("{1} finishes {0}:{2}", obj1, obj2, QSR.Finishes (bounds1, bounds2, MajorAxis.Y, true)));

                    r = QSR.Left(bounds1, bounds2) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Z, true)) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y, true));
                    //(Vector3.Distance (
                    //new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
                    //obj2.gameObject.transform.position) <= Constants.EPSILON);
                    //r &= (obj1.gameObject.transform.position.x < obj2.gameObject.transform.position.x);
                }
                else if (relation == "right") {
                    r = QSR.Right(bounds1, bounds2) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Z, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Z) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Z, true)) &&
                        (RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Overlaps(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.During(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Starts(bounds1, bounds2, MajorAxis.Y, true) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y) ||
                         RectAlgebra.Finishes(bounds1, bounds2, MajorAxis.Y, true));
    //                r = (Vector3.Distance (
    //                    new Vector3 (obj2.gameObject.transform.position.x, obj1.gameObject.transform.position.y, obj1.gameObject.transform.position.z),
    //                    obj2.gameObject.transform.position) <= Constants.EPSILON);
    //                r &= (obj1.gameObject.transform.position.x > obj2.gameObject.transform.position.x);
                }
                else if (relation == "touching") {
                    r = RCC8.EC(Helper.GetObjectOrientedSize(obj1, true), Helper.GetObjectOrientedSize(obj2, true));
    //                r = RCC8.EC(Helper.GetObjectOrientedSize(obj1), Helper.GetObjectOrientedSize(obj2));
                }
                else {
                }

                Debug.Log(string.Format("{0}:{1}", relation, r));
                return r;
            }

            public static void ReevaluateRelationships(String program, GameObject obj) {
                // get object bounds
                Bounds objBounds = Helper.GetObjectWorldSize(obj);

                // get all objects
                GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();

                // reasoning from habitats
                // for each object
                // for each habitat in object
                // for each affordance by habitat

                // e.g. with object obj: H->[put(x, on([1]))]support([1], x)
                //    if (program == "put" && obj is on test) then test supports obj
                //    H[2]->[put(x, in([1]))]contain(y, x)
                // if obj is in configuration [2], if (program == "put" && obj is in test) then test contains obj

                if (program == "put") {
                    Bounds testBounds = new Bounds();
                    Voxeme[] voxemes;
                    RelationTracker relationTracker =
                        (RelationTracker) GameObject.Find("BehaviorController").GetComponent("RelationTracker");
                    foreach (GameObject test in allObjects) {
                        if (test != obj) {
                            voxemes = test.GetComponentsInChildren<Voxeme>();
                            foreach (Voxeme voxeme in voxemes) {
                                if (voxeme != null) {
                                    if (!voxeme.gameObject.name.Contains("*")) {
                                        // hacky fix to filter out unparented objects w/ disabled voxeme components
                                        testBounds = Helper.GetObjectWorldSize(test);
                                        // bunch of underspecified RCC relations
                                        if (voxeme.voxml.Afford_Str.Affordances.Any(p => p.Formula.Contains("support"))) {
                                            // **check for support configuration here
                                            if ((voxeme.voxml.Type.Concavity.Contains("Concave")) &&
                                                (Helper.FitsIn(objBounds, testBounds))) {
                                                // if test object is concave and placed object would fit inside
                                                if (RCC8.PO(objBounds, Helper.GetObjectWorldSize(test))) {
                                                    // interpenetration = support
                                                    RiggingHelper.RigTo(obj, test); // setup parent-child rig
                                                    relationTracker.AddNewRelation(new List<GameObject> {test, obj},
                                                        "support");
                                                    Debug.Log(test.name + " supports " + obj.name);
                                                }
                                            }
                                            else {
                                                if (RCC8.EC(objBounds, Helper.GetObjectWorldSize(test))) {
                                                    // otherwise EC = support
                                                    if (voxeme.enabled) {
                                                        obj.GetComponent<Rigging>().ActivatePhysics(true);
                                                    }

                                                    obj.GetComponent<Voxeme>().minYBound =
                                                        Helper.GetObjectWorldSize(test).max.y;
                                                    RiggingHelper.RigTo(obj, test); // setup parent-child rig
                                                    relationTracker.AddNewRelation(new List<GameObject> {test, obj},
                                                        "support");
                                                    Debug.Log(test.name + " supports " + obj.name);
                                                }
                                            }
                                        }

                                        if (voxeme.voxml.Afford_Str.Affordances.Any(p => p.Formula.Contains("contain"))) {
                                            if (Helper.FitsIn(objBounds, Helper.GetObjectWorldSize(test))) {
                                                if (RCC8.PO(objBounds, Helper.GetObjectWorldSize(test))) {
                                                    // interpenetration = containment
                                                    obj.GetComponent<Voxeme>().minYBound =
                                                        PhysicsHelper
                                                            .GetConcavityMinimum(
                                                                obj); //Helper.GetObjectWorldSize (obj).min.y;
                                                    RiggingHelper.RigTo(obj, test); // setup parent-child rig
                                                    relationTracker.AddNewRelation(new List<GameObject> {test, obj},
                                                        "contain");
                                                    Debug.Log(test.name + " contains " + obj.name);
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
}