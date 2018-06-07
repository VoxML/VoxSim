using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

using Episteme;
using Global;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Agent
{
	public class StackSymbolContent : IEquatable<System.Object>
	{
		public object IndicatedObj{
			get;
			set;
		}

		public object GraspedObj{
			get;
			set;
		}

		public object IndicatedRegion{
			get;
			set;
		}

		public object ObjectOptions{
			get;
			set;
		}

		public object ActionOptions{
			get;
			set;
		}

		public object ActionSuggestions{
			get;
			set;
		}

		public StackSymbolContent(object indicatedObj, object graspedObj, object indicatedRegion,
			object objectOptions, object actionOptions, object actionSuggestions){
			this.IndicatedObj = indicatedObj;
			this.GraspedObj = graspedObj;
			this.IndicatedRegion = indicatedRegion;
			this.ObjectOptions = objectOptions;
			this.ActionOptions = actionOptions;
			this.ActionSuggestions = actionSuggestions;
		}

		public StackSymbolContent(StackSymbolContent clone){
			this.IndicatedObj = (GameObject)clone.IndicatedObj;
			this.GraspedObj = (GameObject)clone.GraspedObj;
			this.IndicatedRegion = (clone.IndicatedRegion != null) ? new Region((Region)clone.IndicatedRegion) : null;
			this.ObjectOptions = (clone.ObjectOptions != null) ? new List<GameObject>((List<GameObject>)clone.ObjectOptions) : null;
			this.ActionOptions = (clone.ActionOptions != null) ? new List<string>((List<string>)clone.ActionOptions) : null;
			this.ActionSuggestions = (clone.ActionSuggestions != null) ? new List<string>((List<string>)clone.ActionSuggestions) : null;
		}

		public override bool Equals(object obj) {
			if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
				return false;
			else {
				StackSymbolContent tuple = (StackSymbolContent)obj;
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					(GameObject)IndicatedObj,(GameObject)tuple.IndicatedObj,(GameObject)IndicatedObj == (GameObject)tuple.IndicatedObj));
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					(GameObject)GraspedObj,(GameObject)tuple.GraspedObj,(GameObject)GraspedObj == (GameObject)tuple.GraspedObj));
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					(Region)IndicatedRegion,(Region)tuple.IndicatedRegion,Helper.RegionsEqual((Region)IndicatedRegion, (Region)tuple.IndicatedRegion)));
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					string.Format ("[{0}]", String.Join (", ", ((List<GameObject>)ObjectOptions).Select (o => o.name).ToArray ())),
				//					string.Format ("[{0}]", String.Join (", ", ((List<GameObject>)tuple.ObjectOptions).Select (o => o.name).ToArray ())),
				//					((List<GameObject>)ObjectOptions).SequenceEqual((List<GameObject>)tuple.ObjectOptions)));
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					string.Format ("[{0}]", String.Join (", ", ((List<string>)ActionOptions).ToArray ())),
				//					string.Format ("[{0}]", String.Join (", ", ((List<string>)tuple.ActionOptions).ToArray ())),
				//					((List<string>)ActionOptions).SequenceEqual((List<string>)tuple.ActionOptions)));
				//				Debug.Log(string.Format("{0} == {1}:{2}",
				//					string.Format ("[{0}]", String.Join (", ", ((List<string>)ActionSuggestions).ToArray ())),
				//					string.Format ("[{0}]", String.Join (", ", ((List<string>)tuple.ActionSuggestions).ToArray ())),
				//					((List<string>)ActionSuggestions).SequenceEqual((List<string>)tuple.ActionSuggestions)));

				return (GameObject)IndicatedObj == (GameObject)tuple.IndicatedObj &&
					(GameObject)GraspedObj == (GameObject)tuple.GraspedObj &&
					Helper.RegionsEqual((Region)IndicatedRegion, (Region)tuple.IndicatedRegion) &&
					((List<GameObject>)ObjectOptions).SequenceEqual((List<GameObject>)tuple.ObjectOptions) &&
					((List<string>)ActionOptions).SequenceEqual((List<string>)tuple.ActionOptions) &&
					((List<string>)ActionSuggestions).SequenceEqual((List<string>)tuple.ActionSuggestions);
			}
		}

		public override int GetHashCode() {
			return IndicatedObj.GetHashCode() ^ GraspedObj.GetHashCode() ^
				IndicatedRegion.GetHashCode() ^ ObjectOptions.GetHashCode() ^
				ActionOptions.GetHashCode() ^ ActionSuggestions.GetHashCode();
		}

		public static bool operator == (StackSymbolContent tuple1, StackSymbolContent tuple2) {
			return tuple1.Equals (tuple2);
		}

		public static bool operator != (StackSymbolContent tuple1, StackSymbolContent tuple2) {
			return !tuple1.Equals (tuple2);
		}
	}

	public class StackSymbolConditions : IEquatable<System.Object>
	{
		public Expression<Predicate<GameObject>> IndicatedObjCondition{
			get;
			set;
		}

		public Expression<Predicate<GameObject>> GraspedObjCondition{
			get;
			set;
		}

		public Expression<Predicate<Region>> IndicatedRegionCondition{
			get;
			set;
		}

		public Expression<Predicate<List<GameObject>>> ObjectOptionsCondition{
			get;
			set;
		}

		public Expression<Predicate<List<string>>> ActionOptionsCondition{
			get;
			set;
		}

		public Expression<Predicate<List<string>>> ActionSuggestionsCondition{
			get;
			set;
		}

		public StackSymbolConditions(Expression<Predicate<GameObject>> indicatedObjCondition, Expression<Predicate<GameObject>> graspedObjCondition,
			Expression<Predicate<Region>> indicatedRegionCondition, Expression<Predicate<List<GameObject>>> objectOptionsCondition,
			Expression<Predicate<List<string>>> actionOptionsCondition, Expression<Predicate<List<string>>> actionSuggestionsCondition){
			this.IndicatedObjCondition = indicatedObjCondition;
			this.GraspedObjCondition = graspedObjCondition;
			this.IndicatedRegionCondition = indicatedRegionCondition;
			this.ObjectOptionsCondition = objectOptionsCondition;
			this.ActionOptionsCondition = actionOptionsCondition;
			this.ActionSuggestionsCondition = actionSuggestionsCondition;
		}

		public bool SatisfiedBy(object obj) {
			if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
				return false;
			else {
				StackSymbolContent tuple = (StackSymbolContent)obj;

				return ((IndicatedObjCondition == null) || (IndicatedObjCondition.Compile().Invoke((GameObject)tuple.IndicatedObj))) &&
					((GraspedObjCondition == null) || (GraspedObjCondition.Compile().Invoke((GameObject)tuple.GraspedObj))) &&
					((IndicatedRegionCondition == null) || (IndicatedRegionCondition.Compile().Invoke((Region)tuple.IndicatedRegion))) &&
					((ObjectOptionsCondition == null) || (ObjectOptionsCondition.Compile().Invoke((List<GameObject>)tuple.ObjectOptions))) &&
					((ActionOptionsCondition == null) || (ActionOptionsCondition.Compile().Invoke((List<string>)tuple.ActionOptions))) &&
					((ActionSuggestionsCondition == null) || (ActionSuggestionsCondition.Compile().Invoke((List<string>)tuple.ActionSuggestions)));
			}
		}

		public override bool Equals(object obj) {
			if (obj == null || (obj as StackSymbolConditions) == null) //if the object is null or the cast fails
				return false;
			else {				
				StackSymbolConditions tuple = (StackSymbolConditions)obj;

				//				Debug.Log (string.Format ("{0} == {1}?",
				//					IndicatedObjCondition == null ? "Null" : System.Convert.ToString(IndicatedObjCondition),
				//					tuple.IndicatedObjCondition == null ? "Null" : System.Convert.ToString(tuple.IndicatedObjCondition)));
				//				Debug.Log (string.Format ("{0} == {1}?",
				//					GraspedObjCondition == null ? "Null" : System.Convert.ToString(GraspedObjCondition),
				//					tuple.GraspedObjCondition == null ? "Null" : System.Convert.ToString(tuple.GraspedObjCondition)));
				//				Debug.Log (string.Format ("{0} == {1}?",
				//					IndicatedRegionCondition == null ? "Null" : System.Convert.ToString(IndicatedRegionCondition),
				//					tuple.IndicatedRegionCondition == null ? "Null" : System.Convert.ToString(tuple.IndicatedRegionCondition)));
				//				Debug.Log (string.Format ("{0} == {1}?",
				//					ObjectOptionsCondition == null ? "Null" : System.Convert.ToString(ObjectOptionsCondition),
				//					tuple.ObjectOptionsCondition == null ? "Null" : System.Convert.ToString(tuple.ObjectOptionsCondition)));
				//				Debug.Log (string.Format ("{0} == {1}?",
				//					ActionOptionsCondition == null ? "Null" : System.Convert.ToString(ActionOptionsCondition),
				//					tuple.ActionOptionsCondition == null ? "Null" : System.Convert.ToString(tuple.ActionOptionsCondition)));
				//				Debug.Log (string.Format ("{0} == {1}?",
				//					ActionSuggestionsCondition == null ? "Null" : System.Convert.ToString(ActionSuggestionsCondition),
				//					tuple.ActionSuggestionsCondition == null ? "Null" : System.Convert.ToString(tuple.ActionSuggestionsCondition)));

				bool equal = true;

				if ((IndicatedObjCondition == null) && (tuple.IndicatedObjCondition == null)) {
					equal &= true;
				}
				else if ((IndicatedObjCondition == null) && (tuple.IndicatedObjCondition != null)) {
					equal &= false;
				}
				else if ((IndicatedObjCondition != null) && (tuple.IndicatedObjCondition == null)) {
					equal &= false;
				}
				else {
					// loath to do this but it should work for now
					equal &= System.Convert.ToString(IndicatedObjCondition) == System.Convert.ToString(tuple.IndicatedObjCondition);
					//equal &= Expression.Lambda<Func<bool>>(Expression.Equal(IndicatedObjCondition, tuple.IndicatedObjCondition)).Compile()();
				}

				if ((GraspedObjCondition == null) && (tuple.GraspedObjCondition == null)) {
					equal &= true;
				}
				else if ((GraspedObjCondition == null) && (tuple.GraspedObjCondition != null)) {
					equal &= false;
				}
				else if ((GraspedObjCondition != null) && (tuple.GraspedObjCondition == null)) {
					equal &= false;
				}
				else {
					equal &= System.Convert.ToString(GraspedObjCondition) == System.Convert.ToString(tuple.GraspedObjCondition);
					//equal &= Expression.Lambda<Func<bool>>(Expression.Equal(GraspedObjCondition, tuple.GraspedObjCondition)).Compile()();
				}

				if ((IndicatedRegionCondition == null) && (tuple.IndicatedRegionCondition == null)) {
					equal &= true;
				}
				else if ((IndicatedRegionCondition == null) && (tuple.IndicatedRegionCondition != null)) {
					equal &= false;
				}
				else if ((IndicatedRegionCondition != null) && (tuple.IndicatedRegionCondition == null)) {
					equal &= false;
				}
				else {
					equal &= System.Convert.ToString(IndicatedRegionCondition) == System.Convert.ToString(tuple.IndicatedRegionCondition);
					//					equal &= Expression.Lambda<Func<bool>>(Expression.Equal(IndicatedRegionCondition, tuple.IndicatedRegionCondition)).Compile()();
				}

				if ((ObjectOptionsCondition == null) && (tuple.ObjectOptionsCondition == null)) {
					equal &= true;
				}
				else if ((ObjectOptionsCondition == null) && (tuple.ObjectOptionsCondition != null)) {
					equal &= false;
				}
				else if ((ObjectOptionsCondition != null) && (tuple.ObjectOptionsCondition == null)) {
					equal &= false;
				}
				else {
					equal &= System.Convert.ToString(ObjectOptionsCondition) == System.Convert.ToString(tuple.ObjectOptionsCondition);
					//					equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ObjectOptionsCondition, tuple.ObjectOptionsCondition)).Compile()();
				}

				if ((ActionOptionsCondition == null) && (tuple.ActionOptionsCondition == null)) {
					equal &= true;
				}
				else if ((ActionOptionsCondition == null) && (tuple.ActionOptionsCondition != null)) {
					equal &= false;
				}
				else if ((ActionOptionsCondition != null) && (tuple.ActionOptionsCondition == null)) {
					equal &= false;
				}
				else {
					equal &= System.Convert.ToString(ActionOptionsCondition) == System.Convert.ToString(tuple.ActionOptionsCondition);
					//					equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ActionOptionsCondition, tuple.ActionOptionsCondition)).Compile()();
				}

				if ((ActionSuggestionsCondition == null) && (tuple.ActionSuggestionsCondition == null)) {
					equal &= true;
				}
				else if ((ActionSuggestionsCondition == null) && (tuple.ActionSuggestionsCondition != null)) {
					equal &= false;
				}
				else if ((ActionSuggestionsCondition != null) && (tuple.ActionSuggestionsCondition == null)) {
					equal &= false;
				}
				else {
					equal &= System.Convert.ToString(ActionSuggestionsCondition) == System.Convert.ToString(tuple.ActionSuggestionsCondition);
					//					equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ActionSuggestionsCondition, tuple.ActionSuggestionsCondition)).Compile()();
				}

				Debug.Log (equal);
				return  equal;
			}
		}

		public override int GetHashCode() {
			return IndicatedObjCondition.GetHashCode() ^ GraspedObjCondition.GetHashCode() ^
				IndicatedRegionCondition.GetHashCode() ^ ObjectOptionsCondition.GetHashCode() ^
				ActionOptionsCondition.GetHashCode() ^ ActionSuggestionsCondition.GetHashCode();
		}

		public static bool operator == (StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
			return tuple1.Equals(tuple2);
		}

		public static bool operator != (StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
			return !tuple1.Equals(tuple2);
		}
	}

	public class DianaInteractionLogic : CharacterLogicAutomaton
	{
		public GameObject IndicatedObj {
			get { return GetCurrentStackSymbol() == null ? null : 
				(GameObject)((StackSymbolContent)GetCurrentStackSymbol ().Content).IndicatedObj; }
		}

		public GameObject GraspedObj {
			get { return GetCurrentStackSymbol() == null ? null : 
				(GameObject)((StackSymbolContent)GetCurrentStackSymbol ().Content).GraspedObj; }
		}

		public Region IndicatedRegion {
			get { return GetCurrentStackSymbol() == null ? null : 
				(Region)((StackSymbolContent)GetCurrentStackSymbol ().Content).IndicatedRegion; }
		}

		public List<GameObject> ObjectOptions {
			get { return GetCurrentStackSymbol() == null ? new List<GameObject>() : 
				(List<GameObject>)((StackSymbolContent)GetCurrentStackSymbol ().Content).ObjectOptions; }
		}

		public List<string> ActionOptions {
			get { return GetCurrentStackSymbol() == null ? new List<string>() : 
				(List<string>)((StackSymbolContent)GetCurrentStackSymbol ().Content).ActionOptions; }
		}

		public List<string> ActionSuggestions {
			get { return GetCurrentStackSymbol() == null ? new List<string>() : 
				(List<string>)((StackSymbolContent)GetCurrentStackSymbol ().Content).ActionSuggestions; }
		}

		public string eventConfirmation = "";

		public GameObject objectConfirmation = null;

		public bool useOrderingHeuristics;
		public bool humanRelativeDirections;
		public bool waveToStart;
		public bool useEpistemicModel;
		public bool repeatAfterWait;
		public double repeatTimerTime = 10000;

		public AgentInteraction interactionController;

#if UNITY_EDITOR
		[CustomEditor(typeof(DianaInteractionLogic))]
		public class DebugPreview : Editor {
			public override void OnInspectorGUI() {

				var bold = new GUIStyle(); 
				bold.fontStyle = FontStyle.Bold; 

				GUILayout.BeginHorizontal();
				GUILayout.Label("Use Ordering Heuristics", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).useOrderingHeuristics =
					GUILayout.Toggle (((DianaInteractionLogic)target).useOrderingHeuristics, "");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Human Relative Directions", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).humanRelativeDirections =
					GUILayout.Toggle (((DianaInteractionLogic)target).humanRelativeDirections,"");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Wave To Start", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).waveToStart =
					GUILayout.Toggle (((DianaInteractionLogic)target).waveToStart,"");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Use Epistemic Model", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).useEpistemicModel =
					GUILayout.Toggle (((DianaInteractionLogic)target).useEpistemicModel,"");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Repeat After Wait", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).repeatAfterWait =
					GUILayout.Toggle (((DianaInteractionLogic)target).repeatAfterWait,"");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Repeat Wait Time", bold, GUILayout.Width(150));
				((DianaInteractionLogic)target).repeatTimerTime = System.Convert.ToDouble(
					GUILayout.TextField (((DianaInteractionLogic)target).repeatTimerTime.ToString(), GUILayout.Width(50)));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Current State", bold, GUILayout.Width(150));
				GUILayout.Label(((DianaInteractionLogic)target).CurrentState == null ? 
					"Null" : ((DianaInteractionLogic)target).CurrentState.Name);
				GUILayout.EndHorizontal();

				// some styling for the header, this is optional
				GUILayout.Label("Stack", bold);

				// add a label for each item, you can add more properties
				// you can even access components inside each item and display them
				// for example if every item had a sprite we could easily show it 
				if (((DianaInteractionLogic)target).Stack != null) {
					foreach (PDASymbol item in ((DianaInteractionLogic)target).Stack) {
						GUILayout.Label (((DianaInteractionLogic)target).StackSymbolToString (item)); 
					}
				}

				GUILayout.Label("State History", bold);
				if (((DianaInteractionLogic)target).StateTransitionHistory != null) {
					foreach (Pair<PDASymbol,PDAState> item in ((DianaInteractionLogic)target).StateTransitionHistory) {
						GUILayout.BeginHorizontal();
						GUILayout.Label (item.Item1 == null ? "Null" : item.Item1.Name, GUILayout.Width(150));
						GUILayout.Label (item.Item2 == null ? "Null" : item.Item2.Name);
						GUILayout.EndHorizontal();
					}
				}
			}
		}
#endif

		EpistemicModel epistemicModel;
		enum CertaintyMode {
			Suggest,
			Act
		};
			
		Dictionary<PDASymbol,List<Concept>> symbolConceptMap;

		Timer repeatTimer;
		bool forceRepeat = false;

		protected GameObject GetIndicatedObj(object arg) {
			return IndicatedObj;
		}

		protected GameObject GetGraspedObj(object arg) {
			return GraspedObj;
		}

		protected Region GetIndicatedRegion(object arg) {
			return IndicatedRegion;
		}

		protected List<GameObject> GetObjectOptions(object arg) {
			return ObjectOptions;
		}

		protected List<string> GetActionOptions(object arg) {
			return ActionOptions;
		}

		protected List<string> GetActionSuggestions(object arg) {
			return ActionSuggestions;
		}

		protected string GetMostRecentInputSymbolName(object arg) {
			return GetLastInputSymbol ().Name;
		}

		protected List<string> GetMostRecentInputSymbolNameAsList(object arg) {
			return new List<string>(new string[]{GetLastInputSymbol ().Name});
		}

		public object NullObject(object arg) {
			return null;
		}

		public List<string> GetActionOptionsIfNull(object arg) {
			if (ActionSuggestions.Count == 0) {
				return ActionOptions;
			}
			else {
				return null;
			}
		}

		public List<PDASymbol> PushObjectOptions (object arg) {
			List<PDASymbol> symbolList = ((ObjectOptions.Count == 1) && (IndicatedRegion == null)) ? 
				Enumerable.Range (0, ObjectOptions.Count).Select (s => 
					GenerateStackSymbol ((IndicatedObj == null) ?
						(IndicatedRegion == null) ? ObjectOptions[s] : ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList() [s] : IndicatedObj,
						null, null,
						new List<GameObject>(),
						null, null)).ToList () :
				Enumerable.Range (0, ObjectOptions.Count).Select (s => 
					GenerateStackSymbol ((IndicatedObj == null) ?
						(IndicatedRegion == null) ? ObjectOptions[s] : ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList() [s] : IndicatedObj,
						null, null,
						(IndicatedRegion == null) ? ObjectOptions.GetRange(0,s+1) : ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList().GetRange(0,s+1),
						null, null)).ToList ();

			return symbolList;
		}

		public List<PDASymbol> PushPutOptions (object arg) {
			List<PDASymbol> symbolList = Enumerable.Range (0, ObjectOptions.Count).Select (s => 
				GenerateStackSymbol (IndicatedObj,
					null, null, 
					ObjectOptions.OrderByDescending(
						m => (m.transform.position - IndicatedRegion.center).magnitude).ToList().GetRange(0,s+1), 
					Enumerable.Range (0, s+1).Select(a => string.Format("put({0},on({1}))",(IndicatedObj != null) ? IndicatedObj.name : 
						(GraspedObj != null) ? GraspedObj.name : "{0}",
						ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()[a].name)).ToList(), 
					new List<string>())).ToList ();

			return symbolList;
		}

		public List<string> GenerateGraspCommand (object arg) {
			List<string> actionList = new List<string> (
				new string[]{ "grasp({0})" });

			return actionList;
		}

		public List<string> GeneratePutAtRegionCommand (object arg) {
			List<string> actionList = new List<string> (
				new string[]{ "put({0}" + string.Format (",{0})", Helper.VectorToParsable (IndicatedRegion.center)) });

			return actionList;
		}

		public List<string> GeneratePutObjectOnObjectCommand (object arg) {
			List<string> actionList = new List<string> (
				new string[]{ string.Format ("put({0},on({1}))",
					GraspedObj == null ? "{0}" : GraspedObj.name,
					IndicatedObj == null ? "{0}" : IndicatedObj.name) });

			return actionList;
		}

		public List<string> GenerateDirectedPutCommand (object arg) {
			List<string> actionList = new List<string> (
				new string[]{ "put({0}" + string.Format (",{0})", 
					GetGestureContent (
						RemoveInputSymbolType (
							RemoveGestureTrigger (
								ActionSuggestions[0], GetGestureTrigger (ActionSuggestions[0])),
							GetInputSymbolType (ActionSuggestions[0])),
						"grab move").ToLower()) });

			return actionList;
		}

		public List<string> GenerateDirectedSlideCommand (object arg) {
			List<string> actionList = (ActionSuggestions.Count > 0) ? 
				new List<string> (
					new string[]{ "slide({0}" + string.Format (",{0})", 
						GetGestureContent (
							RemoveInputSymbolType (
								RemoveGestureTrigger (
									ActionSuggestions[0], GetGestureTrigger (ActionSuggestions[0])),
								GetInputSymbolType (ActionSuggestions[0])),
							"push").ToLower()) }) :
				new List<string> (
					new string[]{ "slide({0}" + string.Format (",{0})", 
						GetGestureContent (
							RemoveInputSymbolType (
								RemoveGestureTrigger (
									ActionOptions[0], GetGestureTrigger (ActionOptions[0])),
								GetInputSymbolType (ActionOptions[0])),
							"push").ToLower()) });

			return actionList;
		}

		public override void Start() {
			// define the grammar
			/*
			  	// O: define the object
			// A: define the action
			// D: disambiguate
			// d: deixis (G)
			// v: direction (S)
			// c: color (S)
			// s: size (S,G)
			// y: posack (S,G)
			// n: negack (S,G)
			// a: action (S,G)
			S ::= OA|AO
				O ::= d|dD|v|vD|c|cD|s|sD
				A ::= a|aD|v|vD
				D ::= d|dD|v|vD|c|cD|s|sD|y|yD|n|nD
				*/

				// input symbols: received messages
				// stack symbols: array of state variables

			base.Start ();

			if ((repeatAfterWait) && (repeatTimerTime > 0)) {
				repeatTimer = new Timer (repeatTimerTime);
				repeatTimer.Enabled = false;
				repeatTimer.Elapsed += RepeatUtterance;
			}

			//interactionController.UseTeaching = (PlayerPrefs.GetInt("Use Teaching Agent") == 1);

			States.Add(new PDAState("StartState",null));
			States.Add(new PDAState("BeginInteraction",null));
			States.Add(new PDAState("Ready",null));
			States.Add(new PDAState("Suggest",null));

			States.Add(new PDAState("Confirm",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null))));

			States.Add(new PDAState("Wait",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null))));

			States.Add(new PDAState("ParseSentence",null));
			States.Add(new PDAState("ParseVP",null));
			States.Add(new PDAState("ParseNP",null));
			States.Add(new PDAState("ParsePP",null));
			States.Add(new PDAState("TrackPointing",null));
			States.Add(new PDAState("SituateDeixis",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
						new StackSymbolContent(null,null,null,null,null,null)))));

			States.Add(new PDAState("InterpretDeixis",null));
			States.Add(new PDAState("DisambiguateObject",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null))));

			States.Add(new PDAState("IndexByColor",null));
			States.Add(new PDAState("IndexBySize",null));
			States.Add(new PDAState("IndexByRegion",null));
			States.Add(new PDAState("RegionAsGoal",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null))));

			States.Add(new PDAState("StartGrab",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
						new StackSymbolContent(null,null,null,null,null,null)))));

			States.Add(new PDAState("StartGrabMove",null));
			States.Add(new PDAState("StopGrabMove",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
						new StackSymbolContent(null,null,null,null,null,new FunctionDelegate(GetActionOptions))))));

			States.Add(new PDAState("StopGrab",null));
			States.Add(new PDAState("StartPush",null));
			States.Add(new PDAState("StopPush",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
						new StackSymbolContent(null,null,null,null,null,new FunctionDelegate(GetActionOptions))))));

			States.Add(new PDAState("ConfirmObject",null));
			States.Add(new PDAState("RequestObject",
				new TransitionGate(
					new FunctionDelegate(EpistemicallyCertain),
					GetState("Suggest"),
					new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
						new StackSymbolContent(null,null,null,null,null,new FunctionDelegate(GetActionOptionsIfNull))))));

			States.Add(new PDAState("PlaceInRegion",null));
			States.Add(new PDAState("RequestAction",null));
			States.Add(new PDAState("ComposeObjectAndAction",null));
			States.Add(new PDAState("DisambiguateEvent",null));
			States.Add(new PDAState("ConfirmEvent",null));
			States.Add(new PDAState("ExecuteEvent",null));
			States.Add(new PDAState("AbortAction",null));
			States.Add(new PDAState("BlockUnavailable",null));
			States.Add(new PDAState("Confusion",null));
			States.Add(new PDAState("CleanUp",null));
			States.Add(new PDAState("EndState",null));

			InputSymbols.Add(new PDASymbol("G engage start"));
			InputSymbols.Add(new PDASymbol("G wave start"));
			InputSymbols.Add(new PDASymbol("G wave stop"));
			InputSymbols.Add(new PDASymbol("G left point start"));
			InputSymbols.Add(new PDASymbol("G right point start"));
			InputSymbols.Add(new PDASymbol("G left point high"));
			InputSymbols.Add(new PDASymbol("G right point high"));
			InputSymbols.Add(new PDASymbol("G left point low"));
			InputSymbols.Add(new PDASymbol("G right point low"));
			InputSymbols.Add(new PDASymbol("G left point stop"));
			InputSymbols.Add(new PDASymbol("G right point stop"));
			InputSymbols.Add(new PDASymbol("G posack start"));
			InputSymbols.Add(new PDASymbol("G negack start"));
			InputSymbols.Add(new PDASymbol("G posack high"));
			InputSymbols.Add(new PDASymbol("G negack high"));
			InputSymbols.Add(new PDASymbol("G posack low"));
			InputSymbols.Add(new PDASymbol("G negack low"));
			InputSymbols.Add(new PDASymbol("G posack stop"));
			InputSymbols.Add(new PDASymbol("G negack stop"));
			InputSymbols.Add(new PDASymbol("G grab start"));
			InputSymbols.Add(new PDASymbol("G grab high"));
			InputSymbols.Add(new PDASymbol("G grab low"));
			InputSymbols.Add(new PDASymbol("G grab move left start"));
			InputSymbols.Add(new PDASymbol("G grab move right start"));
			InputSymbols.Add(new PDASymbol("G grab move front start"));
			InputSymbols.Add(new PDASymbol("G grab move back start"));
			InputSymbols.Add(new PDASymbol("G grab move up start"));
			InputSymbols.Add(new PDASymbol("G grab move down start"));
			InputSymbols.Add(new PDASymbol("G grab move left high"));
			InputSymbols.Add(new PDASymbol("G grab move right high"));
			InputSymbols.Add(new PDASymbol("G grab move front high"));
			InputSymbols.Add(new PDASymbol("G grab move back high"));
			InputSymbols.Add(new PDASymbol("G grab move up high"));
			InputSymbols.Add(new PDASymbol("G grab move down high"));
			InputSymbols.Add(new PDASymbol("G grab move left low"));
			InputSymbols.Add(new PDASymbol("G grab move right low"));
			InputSymbols.Add(new PDASymbol("G grab move front low"));
			InputSymbols.Add(new PDASymbol("G grab move back low"));
			InputSymbols.Add(new PDASymbol("G grab move up low"));
			InputSymbols.Add(new PDASymbol("G grab move down low"));
			InputSymbols.Add(new PDASymbol("G grab stop"));
			InputSymbols.Add(new PDASymbol("G push left start"));
			InputSymbols.Add(new PDASymbol("G push right start"));
			InputSymbols.Add(new PDASymbol("G push front start"));
			InputSymbols.Add(new PDASymbol("G push back start"));
			InputSymbols.Add(new PDASymbol("G push left high"));
			InputSymbols.Add(new PDASymbol("G push right high"));
			InputSymbols.Add(new PDASymbol("G push front high"));
			InputSymbols.Add(new PDASymbol("G push back high"));
			InputSymbols.Add(new PDASymbol("G push left low"));
			InputSymbols.Add(new PDASymbol("G push right low"));
			InputSymbols.Add(new PDASymbol("G push front low"));
			InputSymbols.Add(new PDASymbol("G push back low"));
			InputSymbols.Add(new PDASymbol("G push left stop"));
			InputSymbols.Add(new PDASymbol("G push right stop"));
			InputSymbols.Add(new PDASymbol("G push front stop"));
			InputSymbols.Add(new PDASymbol("G push back stop"));
			InputSymbols.Add(new PDASymbol("G count one start"));
			InputSymbols.Add(new PDASymbol("G count two start"));
			InputSymbols.Add(new PDASymbol("G count three start"));
			InputSymbols.Add(new PDASymbol("G count four start"));
			InputSymbols.Add(new PDASymbol("G count five start"));
			InputSymbols.Add(new PDASymbol("G count one high"));
			InputSymbols.Add(new PDASymbol("G count two high"));
			InputSymbols.Add(new PDASymbol("G count three high"));
			InputSymbols.Add(new PDASymbol("G count four high"));
			InputSymbols.Add(new PDASymbol("G count five high"));
			InputSymbols.Add(new PDASymbol("G count one low"));
			InputSymbols.Add(new PDASymbol("G count two low"));
			InputSymbols.Add(new PDASymbol("G count three low"));
			InputSymbols.Add(new PDASymbol("G count four low"));
			InputSymbols.Add(new PDASymbol("G count five low"));
			InputSymbols.Add(new PDASymbol("G count one stop"));
			InputSymbols.Add(new PDASymbol("G count two stop"));
			InputSymbols.Add(new PDASymbol("G count three stop"));
			InputSymbols.Add(new PDASymbol("G count four stop"));
			InputSymbols.Add(new PDASymbol("G count five stop"));
			InputSymbols.Add(new PDASymbol("G nevermind start"));
			InputSymbols.Add(new PDASymbol("G nevermind stop"));
			InputSymbols.Add(new PDASymbol("G engage stop"));
			InputSymbols.Add(new PDASymbol("S YES"));
			InputSymbols.Add(new PDASymbol("S NO"));
			InputSymbols.Add(new PDASymbol("S THIS"));
			InputSymbols.Add(new PDASymbol("S THAT"));
			InputSymbols.Add(new PDASymbol("S THERE"));
			InputSymbols.Add(new PDASymbol("S GRAB"));
			InputSymbols.Add(new PDASymbol("S PUT"));
			InputSymbols.Add(new PDASymbol("S PUSH"));
			InputSymbols.Add(new PDASymbol("S RED"));
			InputSymbols.Add(new PDASymbol("S GREEN"));
			InputSymbols.Add(new PDASymbol("S YELLOW"));
			InputSymbols.Add(new PDASymbol("S ORANGE"));
			InputSymbols.Add(new PDASymbol("S BLACK"));
			InputSymbols.Add(new PDASymbol("S PURPLE"));
			InputSymbols.Add(new PDASymbol("S WHITE"));
			InputSymbols.Add(new PDASymbol("S PINK"));
			InputSymbols.Add(new PDASymbol("S BIG"));
			InputSymbols.Add(new PDASymbol("S SMALL"));
			InputSymbols.Add(new PDASymbol("S LEFT"));
			InputSymbols.Add(new PDASymbol("S RIGHT"));
			InputSymbols.Add(new PDASymbol("S FRONT"));
			InputSymbols.Add(new PDASymbol("S BACK"));
			InputSymbols.Add(new PDASymbol("S UP"));
			InputSymbols.Add(new PDASymbol("S DOWN"));
			InputSymbols.Add(new PDASymbol("S NOTHING"));
			InputSymbols.Add(new PDASymbol("S NEVERMIND"));
			InputSymbols.Add(new PDASymbol("S NP"));
			InputSymbols.Add(new PDASymbol("S PP"));
			InputSymbols.Add(new PDASymbol("S VP"));
			InputSymbols.Add(new PDASymbol("S S"));
			InputSymbols.Add(new PDASymbol("P l"));
			InputSymbols.Add(new PDASymbol("P r"));

			List<PDASymbol> colors = GetInputSymbolsByName (
				"S RED",
				"S GREEN",
				"S YELLOW",
				"S ORANGE",
				"S BLACK",
				"S PURPLE",
				"S PINK",
				"S WHITE"
			);

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartState"),
				GetInputSymbolsByName("G engage start"),
				GenerateStackSymbol(null, null, null, null, null, null),
				GetState("BeginInteraction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			if (waveToStart) {
				TransitionRelation.Add (new PDAInstruction (
					GetStates ("BeginInteraction"),
					GetInputSymbolsByName ("G wave start"),
					GenerateStackSymbol (null, null, null, null, null, null),
					GetState ("Ready"),
					new PDAStackOperation (PDAStackOperation.PDAStackOperationType.None, null)));
			}
			else {
				TransitionRelation.Add (new PDAInstruction (
					GetStates ("BeginInteraction"),
					null,
					GenerateStackSymbol (null, null, null, null, null, null),
					GetState ("Wait"),
					new PDAStackOperation (PDAStackOperation.PDAStackOperationType.None, null)));
			}

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Ready"),
				null,
				GenerateStackSymbol(null, null, null, null, null, null),
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(											// instruction operated by input signal
				GetStates("Wait"),																// in this state
				GetInputSymbolsByName("G left point high","G right point high",
										"G left point start","G right point start"),			// when we get this message
				GenerateStackSymbol(null, null, null, null, null, null),						// and this is the top of the stack
				GetState("SituateDeixis"),														// go to this state
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,				// and do this to the stack
					new StackSymbolContent(null, null, new Region(), null, null, null))));		// with this symbol

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G left point high","G right point high","G left point start","G right point start",
					"S THIS","S THAT","S THERE"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null, null, null, null
				),
				GetState("SituateDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G left point high","G right point high","G left point start","G right point start",
					"S THIS","S THAT","S THERE"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null, null, null, null
				),
				GetState("SituateDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G left point high","G right point high","G left point start","G right point start",
					"S THIS","S THAT","S THERE"),
				GenerateStackSymbolFromConditions(
					null, null, null, null, 
					(a) => (a.Count > 0) && (a[0].Contains("{0}")), null
				),
				GetState("SituateDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				colors,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
				GetState("IndexByColor"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S LEFT","S RIGHT","S FRONT","S BACK"),
				GenerateStackSymbol(null, null, null, null, null, null),
				GetState("IndexByRegion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G grab high","G grab start","S GRAB"),
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, 
					null, null, null, null
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GenerateGraspCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G grab high","G grab start","S GRAB"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, (g) => g == null, 
					null, null, null, null
				),	
				GetState("StartGrab"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G grab move left high",
					"G grab move right high",
					"G grab move front high",
					"G grab move back high",
					"G grab move up high",
					"G grab move left start",
					"G grab move right start",
					"G grab move front start",
					"G grab move back start",
					"G grab move up start"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("StartGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

//			TransitionRelation.Add(new PDAInstruction(
//				GetStates("Wait"),
//				GetInputSymbolsByName("G grab stop"),
//				GenerateStackSymbolFromConditions(
//					null, (g) => g != null, 
//					null, null, null, null
//				),	
//				GetState("StopGrab"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S LEFT","S RIGHT","S FRONT","S BACK"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("StopGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GetMostRecentInputSymbolNameAsList), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("G push left high",
					"G push right high",
					"G push front high",
					"G push back high",
					"G push left start",
					"G push right start",
					"G push front start",
					"G push back start"),
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("StartPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S LEFT","S RIGHT","S FRONT","S BACK"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, 
					null, null, null, null
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GetMostRecentInputSymbolNameAsList), null))));

//			TransitionRelation.Add(new PDAInstruction(
//				GetStates("Wait"),
//				GetInputSymbolsByName("G left point low","G right point low"),
//				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
//				GetState("Suggest"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
//					new StackSymbolContent(null,null,null,null,null,null))));

//			TransitionRelation.Add(new PDAInstruction(
//				GetStates("Wait"),
//				GetInputSymbolsByName("G grab low","G grab move left low", "G grab move right low",
//					"G grab move front low","G grab move back low","G grab move up low",
//					"G grab move down low","G push left low","G push right low",
//					"G push front low"/*,"G push back low"*/),
//				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
//				GetState("Suggest"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
//					new StackSymbolContent(null,null,null,null,null,new FunctionDelegate(GetActionOptions)))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("P l","P r"),
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
				GetState("TrackPointing"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S S"),
				GenerateStackSymbol(null, null, null, null, null, null),	
				GetState("ParseSentence"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S VP"),
				GenerateStackSymbol(null, null, null, null, null, null),	
				GetState("ParseVP"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S NP"),
				GenerateStackSymbol(null, null, null, null, null, null),	
				GetState("ParseNP"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Wait"),
				GetInputSymbolsByName("S PP"),
				GenerateStackSymbol(null, null, null, null, null, null),	
				GetState("ParsePP"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ParseVP"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => a.Count == 1, null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			// if items have value, check and see if sentence is consistent with them
//			TransitionRelation.Add(new PDAInstruction(
//				GetStates("Wait"),
//				GetInputSymbolsByName("S S"),
//				GenerateStackSymbol(null, null, null, null, null, null),	
//				GetState("InterpretSentence"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("TrackPointing"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				null,
				GenerateStackSymbol(
					null, null, null,
					null, null, null
				),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S YES","G posack high","G posack start"),
				GenerateStackSymbolFromConditions(											// condition set
					null, null, null,
					null, null, (s) => s.Count > 0											// condition: # suggestions > 0
				),	
				GetState("Confirm"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NO","G negack high","G negack start"),
				GenerateStackSymbolFromConditions(
					null, null, null,
					(m) => m.Count == 0, null, (s) => s.Count > 0
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
					GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));
			
			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NO","G negack high","G negack start"),
				GenerateStackSymbolFromConditions(
					null, null, null,
					(m) => m.Count > 1, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NO","G negack high","G negack start"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, (r) => r != null && r.max != r.min,
					(m) => m.Count == 1, null, null
				),	
				GetState("RegionAsGoal"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("S NO","G negack high","G negack start"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count == 1, null, (s) => s.Count > 0
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null,
					null, null, (s) => s[0].Contains("point")
				),	
				GetState("SituateDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					null, null, (s) => s[0].Contains("grab")
				),	
				GetState("StartGrab"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					null, null, (s) => s[0].Contains("grab")
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GenerateGraspCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					null, (a) => (a.Count > 0) && (a[0].Contains("grab move")),
					(s) => s[0].Contains("grab move")
				),	
				GetState("StopGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					null, (a) => a.Count == 0,
					(s) => s[0].Contains("grab move")
				),	
				GetState("StopGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					null, (a) => (a.Count > 0) && (a[0].Contains("grab move")),
					(s) => s[0].Contains("grab move")
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null,
						new FunctionDelegate(GetActionSuggestions), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					null, (a) => a.Count == 0,
					(s) => s[0].Contains("grab move")
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null,
						new FunctionDelegate(GetActionSuggestions), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					null, (a) => (a.Count > 0) && (a[0].Contains("push")),
					(s) => s[0].Contains("push")
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					null, (a) => (a.Count > 0) && (a[0].Contains("push")),
					(s) => s[0].Contains("push")
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					null, (a) => a.Count == 0,
					(s) => s[0].Contains("push")
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					null, (a) => a.Count == 0,
					(s) => s[0].Contains("push")
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					null, (a) => (a.Count > 0) && (a[0].Contains("push")),
					(s) => s[0].Contains("push")
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GenerateDirectedSlideCommand), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confirm"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					null, (a) => a.Count == 0,
					(s) => s[0].Contains("push")
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GetActionSuggestions), null))));

			TransitionRelation.Add(new PDAInstruction(										// instruction operated by stack rewrite
				GetStates("SituateDeixis"),
				null,																		// no input symbol
				GenerateStackSymbolFromConditions(											// condition set
					null, null,
					(r) => r != null && r.max != r.min,										// condition: region is indicated
					null, null, null
				),	
				GetState("InterpretDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));	

			TransitionRelation.Add(new PDAInstruction(
				GetStates("SituateDeixis"),
				null,								
				GenerateStackSymbolFromConditions(	
					(o) => o == null, (g) => g == null,
					(r) => r == null,
					null, null, null
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));	

			TransitionRelation.Add(new PDAInstruction(
				GetStates("SituateDeixis"),
				null,								
				GenerateStackSymbolFromConditions(	
					(o) => o != null, null,
					(r) => r == null,
					null, null, null
				),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));	

			TransitionRelation.Add(new PDAInstruction(
				GetStates("SituateDeixis"),
				null,								
				GenerateStackSymbolFromConditions(	
					null, (g) => g != null,
					(r) => r == null,
					null, null, null
				),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));	

			TransitionRelation.Add(new PDAInstruction(
				GetStates("InterpretDeixis"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, 
					(r) => r != null && r.max != r.min,
					(m) => m.Count == 0, null, null
				),	
				GetState("RegionAsGoal"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("InterpretDeixis"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					(m) => m.Count > 0, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushObjectOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("InterpretDeixis"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count > 0, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushPutOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("InterpretDeixis"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count > 0, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushPutOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null,
					(m) => m.Count > 1, null, null
				),
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushPutOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushPutOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("ConfirmObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushObjectOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count == 0, null, null
				),
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count == 0, null, null
				),
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByColor"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					(m) => m.Count == 0, null, null
				),
				GetState("BlockUnavailable"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexBySize"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushObjectOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexBySize"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("ConfirmObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushObjectOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexBySize"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, null,
					(m) => m.Count == 1, null, null
				),
				GetState("ConfirmObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new FunctionDelegate(PushObjectOptions))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByRegion"),
				null,
				GenerateStackSymbolFromConditions(
					null, null,
					(r) => r != null && r.max != r.min,
					null, null, null
				),	
				GetState("InterpretDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));	

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, (g) => g == null, null,
					(m) => m.Count > 0, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0)),
					null),	
				GetState("ConfirmObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,new FunctionDelegate(NullObject),
						new List<GameObject>(),null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count > 0, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0)),
					null),	
				GetState("ConfirmObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,new FunctionDelegate(NullObject),
						new List<GameObject>(),null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, (g) => g == null, null,
					(m) => m.Count > 0,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0),
					null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,new FunctionDelegate(NullObject),
						new List<GameObject>(),null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count > 0,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0),
					null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,new FunctionDelegate(NullObject),
						new List<GameObject>(),null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					(m) => m.Count > 1, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, null,
					(m) => m.Count > 1, null, null
				),	
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, (r) => r != null && r.max != r.min,
					(m) => m.Count == 1, null, null
				),	
				GetState("RegionAsGoal"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, (r) => r != null && r.max != r.min,
					(m) => m.Count == 1, null, null
				),	
				GetState("RegionAsGoal"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, (r) => r == null,
					(m) => m.Count == 1, null, null
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, (r) => r == null,
					(m) => m.Count == 1, null, null
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null ,null, null, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("put"))).ToList().Count == 0)), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null ,null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

//			TransitionRelation.Add(new PDAInstruction(
//				GetStates("DisambiguateObject"),
//				GetInputSymbolsByName("G left point high","G right point high",
//					"S THIS","S THAT","S THERE"),
//				GenerateStackSymbolFromConditions(
//					null, (g) => g != null, null, null, null, null
//				),
//				GetState("SituateDeixis"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
//					new StackSymbolContent(null, null, new Region(), null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateObject"),
				GetInputSymbolsByName("S BIG","S SMALL"),
				GenerateStackSymbolFromConditions(
					(o) => o == null, null, null,
					(m) => m.Count > 0, null, null),	
				GetState("IndexBySize"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, null, null, null, null, null
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(
					null, null, null, null, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null
				),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(
					null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null
				),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null,
					(r) => r != null && r.max != r.min,
					null, null, null
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(), 
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, (r) => r != null && r.max != r.min,
					null, null, null
				),	
				GetState("ComposeObjectAndAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(), 
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, (r) => r != null && r.max != r.min,
					null, null, null
				),	
				GetState("ComposeObjectAndAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(), 
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RegionAsGoal"),
				colors,
				GenerateStackSymbolFromConditions(
					null, null, (r) => r != null && r.max != r.min,
					null, null, null
				),	
				GetState("IndexByColor"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(), 
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Suggest"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					null, null, (r) => r != null && r.max != r.min,
					null, null, (s) => s.Count == 0
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(), 
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RequestObject"),
				colors,
				GenerateStackSymbolFromConditions(
					null, null, null, null, null, null
				),	
				GetState("IndexByColor"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RequestObject"),
				GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RequestObject"),
				GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RequestObject"),
				GetInputSymbolsByName("P l","P r"),
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),
				GetState("TrackPointing"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("RequestObject"),
				GetInputSymbolsByName("G left point high","G right point high","G left point start","G right point start"),
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("SituateDeixis"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,new Region(),null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmObject"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, (g) => g == null, 
					(r) => r == null, null,
					(a) => a.Count == 0, null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmObject"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, (g) => g != null, 
					(r) => r == null, null,
					(a) => a.Count == 0, null),	
				GetState("ComposeObjectAndAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null,
						new FunctionDelegate(GeneratePutObjectOnObjectCommand), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmObject"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, null,
					(r) => r == null, null,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0),
					null
				),	
				GetState("ComposeObjectAndAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmObject"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, null,
					(r) => r == null, null, null,
					(s) => ((s.Count > 0) && (s[0].Contains("grab move")))
				),	
				GetState("StopGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ComposeObjectAndAction"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, (g) => g == null,
					null, null,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}") || 
							aa.Contains("slide"))).ToList().Count == 0),
					null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ComposeObjectAndAction"),
				null,
				GenerateStackSymbolFromConditions(null, (g) => g != null,
					null, null,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("{0}") || 
							aa.Contains("slide"))).ToList().Count == 0),
					null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ComposeObjectAndAction"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, null,
					null, null,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("slide"))).ToList().Count > 0),
					null),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,null,null,null,new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmObject"),
				null,
				GenerateStackSymbolFromConditions((o) => o != null, null, (r) => r != null, null, null, null),	
				GetState("PlaceInRegion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("PlaceInRegion"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, (a) => a.Count > 1, null),	
				GetState("DisambiguateEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(new FunctionDelegate(NullObject), null, 
						new FunctionDelegate(NullObject), new List<GameObject>(), null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("PlaceInRegion"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, (a) => a.Count == 1, null),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(new FunctionDelegate(NullObject), null, 
						new FunctionDelegate(NullObject), new List<GameObject>(), null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ConfirmEvent"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("ExecuteEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ExecuteEvent"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,null,null,null,new List<string>()))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("ExecuteEvent"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, 
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("lift"))).ToList().Count == 0), null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartGrab"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartGrabMove"),
				GetInputSymbolsByName("G grab move down high","G grab high","G grab move down start","G grab start","G grab stop"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("StopGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartGrabMove"),
				GetInputSymbolsByName("G grab move left high",
					"G grab move right high",
					"G grab move front high",
					"G grab move back high",
					"G grab move up high",
					"G grab move left start",
					"G grab move right start",
					"G grab move front start",
					"G grab move back start",
					"G grab move up start"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("StartGrabMove"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartPush"),
				GetInputSymbolsByName("G push left stop",
					"G push right stop",
					"G push front stop",
					"G push back stop"),
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, 
					null, null, null, null
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartPush"),
				GetInputSymbolsByName("G push left stop",
					"G push right stop",
					"G push front stop",
					"G push back stop"),
				GenerateStackSymbolFromConditions(
					null, (g) => g != null, 
					null, null, null, null
				),	
				GetState("StopPush"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, null, null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StartPush"),
				GetInputSymbolsByName("G push left stop",
					"G push right stop",
					"G push front stop",
					"G push back stop"),
				GenerateStackSymbolFromConditions(
					(o) => o == null, (g) => g == null, 
					null, null, null, null
				),	
				GetState("RequestObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null, null, null, null, 
						new FunctionDelegate(GenerateDirectedSlideCommand), null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StopGrab"),
				null,
				GenerateStackSymbolFromConditions(
					null, (g) => g == null, 
					null, null, null, null
				),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StopGrabMove"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null, null, (a) => a.Count > 1, null
				),	
				GetState("DisambiguateEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StopGrabMove"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null, null, (a) => a.Count == 1, null
				),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null,null,null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StopPush"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null, null, (a) => a.Count > 1, null
				),	
				GetState("DisambiguateEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("StopPush"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null, null, (a) => a.Count == 1, null
				),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, 
					new StackSymbolContent(null,null,null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateEvent"),
				GetInputSymbolsByName("G posack high","G posack start","S YES"),
				GenerateStackSymbolFromConditions(
					null, null, null,
					null, (a) => a.Count > 0, null
				),	
				GetState("ConfirmEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null,null,null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateEvent"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, null, null,
					null, (a) => a.Count > 1, null
				),	
				GetState("DisambiguateEvent"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateEvent"),
				GetInputSymbolsByName("G negack high","G negack start","S NO"),
				GenerateStackSymbolFromConditions(
					null, null, null,
					null, (a) => a.Count == 1, null
				),	
				GetState("Confusion"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop,null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateEvent"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null,null,null,null,
					(a) => ((a.Count == 0) || ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("put"))).ToList().Count == 0)),null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("DisambiguateEvent"),
				GetInputSymbolsByName("S NEVERMIND", "G nevermind start"),
				GenerateStackSymbolFromConditions(null,null,null,null,
					(a) => ((a.Count > 0) &&
						(a.Where(aa => aa.Contains("put"))).ToList().Count > 0),null),	
				GetState("AbortAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("AbortAction"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
					new StackSymbolContent(null,new FunctionDelegate(NullObject),null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("BlockUnavailable"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
					new StackSymbolContent(null,new FunctionDelegate(NullObject),null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction(
				GetStates("Confusion"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
					new StackSymbolContent(null,new FunctionDelegate(NullObject),null,null,null,null))));

			TransitionRelation.Add(new PDAInstruction (
				States,
				GetInputSymbolsByName("G engage stop"),
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("CleanUp"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

//			TransitionRelation.Add(new PDAInstruction (
//				States,
//				GetInputSymbolsByName("G engage stop"),
//				GenerateStackSymbolFromConditions(null, (g) => g != null, null, null, (a) => a.Count == 0, null),	
//				GetState("CleanUp"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));
//
//			TransitionRelation.Add(new PDAInstruction (
//				States,
//				GetInputSymbolsByName("G engage stop"),
//				GenerateStackSymbolFromConditions(null, (g) => g == null, null, null, (a) => a.Count > 0, null),	
//				GetState("CleanUp"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));
//
//			TransitionRelation.Add(new PDAInstruction (
//				States,
//				GetInputSymbolsByName("G engage stop"),
//				GenerateStackSymbolFromConditions(null, (g) => g != null, null, null, (a) => a.Count > 0, null),	
//				GetState("CleanUp"),
//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add(new PDAInstruction (
				GetStates("CleanUp"),
				null,
				GenerateStackSymbolFromConditions(null, null, null, null, null, null),	
				GetState("EndState"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,null)));

			TransitionRelation.Add (new PDAInstruction(
				GetStates("EndState"),
				GetInputSymbolsByName("G engage start"),
				GenerateStackSymbolFromConditions (null, null, null, null, null, null),	
				GetState("BeginInteraction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

			List<PDAInstruction> gateInstructions = new List<PDAInstruction> ();
			foreach (PDAInstruction instruction in TransitionRelation) {
				if (instruction.ToState.Content != null) {
					if (instruction.ToState.Content.GetType () == typeof(TransitionGate)) {
						//						Debug.Log (((TransitionGate)instruction.ToState.Content).RejectState.Name);
						//						Debug.Log (instruction.FromState.Name);
						//						Debug.Log (TransitionRelation.Where (i => (i.FromState == ((TransitionGate)instruction.ToState.Content).RejectState) &&
						//							(i.ToState == instruction.FromState)).ToList ().Count);
						if (instruction.InputSymbols != null) {
							//							Debug.Log (gateInstructions.Where (i => ((i.FromState == instruction.FromState) &&
							//							(i.InputSymbols == null) &&
							//							((i.StackSymbol.Content.GetType() == typeof(StackSymbolContent) && 
							//								(((StackSymbolContent)i.StackSymbol.Content).Equals ((StackSymbolContent)instruction.StackSymbol.Content))) ||
							//							(i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions) && 
							//								(((StackSymbolConditions)i.StackSymbol.Content).Equals ((StackSymbolConditions)instruction.StackSymbol.Content)))) &&
							//							(i.ToState == ((TransitionGate)instruction.ToState.Content).RejectState) &&
							//							(i.StackOperation.Type == PDAStackOperation.PDAStackOperationType.None) &&
							//							(i.StackOperation.Content == null))).ToList ().Count);
							if (gateInstructions.Where (i => ((i.FromStates == instruction.FromStates) &&
								(i.InputSymbols == null) &&
								((i.StackSymbol.Content.GetType() == typeof(StackSymbolContent) && 
									(((StackSymbolContent)i.StackSymbol.Content).Equals ((StackSymbolContent)instruction.StackSymbol.Content))) ||
									(i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions) && 
										(((StackSymbolConditions)i.StackSymbol.Content).Equals ((StackSymbolConditions)instruction.StackSymbol.Content)))) &&
								(i.ToState == ((TransitionGate)instruction.ToState.Content).RejectState) &&
								(i.StackOperation.Type == PDAStackOperation.PDAStackOperationType.None) &&
								(i.StackOperation.Content == null))).ToList ().Count == 0) {
								PDAInstruction newInstruction = new PDAInstruction (instruction.FromStates,
									instruction.InputSymbols,
									instruction.StackSymbol,
									((TransitionGate)instruction.ToState.Content).RejectState,
									new PDAStackOperation (PDAStackOperation.PDAStackOperationType.None, null));
								gateInstructions.Add (newInstruction);
								Debug.Log (string.Format ("Adding gate instruction {0} because {1} ToState {2} has TransitionGate to RejectState {3}",
									string.Format ("{0},{1},{2},{3},{4}",
										(newInstruction.FromStates == null) ? "Null" :
										String.Join (", ", ((List<PDAState>)newInstruction.FromStates).Select (s => s.Name).ToArray ()),
										(newInstruction.InputSymbols == null) ? "Null" :
										String.Join (", ", ((List<PDASymbol>)newInstruction.InputSymbols).Select (s => s.Content.ToString ()).ToArray ()),
										StackSymbolToString (newInstruction.StackSymbol),
										newInstruction.ToState.Name,
										string.Format ("[{0},{1}]",
											newInstruction.StackOperation.Type.ToString (),
											(newInstruction.StackOperation.Content == null) ? "Null" : StackSymbolToString (newInstruction.StackOperation.Content))),
									string.Format ("{0},{1},{2},{3},{4}", 
										(newInstruction.FromStates == null) ? "Null" :
										String.Join (", ", ((List<PDAState>)newInstruction.FromStates).Select (s => s.Name).ToArray ()),
										(instruction.InputSymbols == null) ? "Null" :
										String.Join (", ", ((List<PDASymbol>)instruction.InputSymbols).Select (s => s.Content.ToString ()).ToArray ()),
										StackSymbolToString (instruction.StackSymbol),
										instruction.ToState.Name,
										string.Format ("[{0},{1}]",
											instruction.StackOperation.Type.ToString (),
											(instruction.StackOperation.Content == null) ? "Null" : StackSymbolToString (instruction.StackOperation.Content))),
									instruction.ToState.Name,
									newInstruction.ToState.Name));
							}
						}
					}
				}
			}

			foreach (PDAInstruction instruction in gateInstructions) {
				TransitionRelation.Add (instruction);
			}

			epistemicModel = GetComponent<EpistemicModel> ();
			symbolConceptMap = MapInputSymbolsToConcepts (InputSymbols);

			MoveToState(GetState ("StartState"));
			Stack.Push (GenerateStackSymbol (null, null, null, null, null, null));
			ExecuteStateContent ();

			if (interactionController != null) {
				interactionController.CharacterLogicInput += ReadInputSymbol;
			}
		}

		public override void Update() {
			if (forceRepeat) {
				if ((OutputHelper.GetCurrentOutputString (Role.Affector) != "OK.") &&
					(OutputHelper.GetCurrentOutputString (Role.Affector) != "OK, never mind.") &&
					(OutputHelper.GetCurrentOutputString (Role.Affector) != "Bye!")) {
					OutputHelper.ForceRepeat (Role.Affector);
					forceRepeat = false;
				}
			}
		}

		public PDASymbol GenerateStackSymbol(
			object indicatedObj, object graspedObj, object indicatedRegion,
			object objectOptions, object actionOptions, object actionSuggestions,
			bool overwriteCurrentSymbol = false, string name = "New Stack Symbol") {

			if (actionSuggestions != null) {
				if (actionSuggestions.GetType () == typeof(DelegateFactory)) {
					Debug.Log (((DelegateFactory)actionSuggestions).Function);
				}
			}

			StackSymbolContent symbolContent =
				new StackSymbolContent (
					indicatedObj == null ? (GameObject)GetIndicatedObj (null) :
					indicatedObj.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)indicatedObj).Function : indicatedObj.GetType () == typeof(FunctionDelegate) ?
					(GameObject)((FunctionDelegate)indicatedObj).Invoke (null) : (GameObject)indicatedObj,
					graspedObj == null ? (GameObject)GetGraspedObj (null) :
					graspedObj.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)graspedObj).Function : graspedObj.GetType () == typeof(FunctionDelegate) ?
					(GameObject)((FunctionDelegate)graspedObj).Invoke (null) : (GameObject)graspedObj,
					indicatedRegion == null ? (Region)GetIndicatedRegion (null) :
					indicatedRegion.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)indicatedRegion).Function : indicatedRegion.GetType () == typeof(FunctionDelegate) ?
					(Region)((FunctionDelegate)indicatedRegion).Invoke (null) : (Region)indicatedRegion,
					objectOptions == null ? GetObjectOptions (null) :
					objectOptions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)objectOptions).Function : objectOptions.GetType () == typeof(FunctionDelegate) ?
					(List<GameObject>)((FunctionDelegate)objectOptions).Invoke (null) : (List<GameObject>)objectOptions,
					actionOptions == null ? GetActionOptions (null) :
					actionOptions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)actionOptions).Function : actionOptions.GetType () == typeof(FunctionDelegate) ?
					(List<string>)((FunctionDelegate)actionOptions).Invoke (null) : (List<string>)actionOptions,
					actionSuggestions == null ? GetActionSuggestions (null) :
					actionSuggestions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)actionSuggestions).Function : actionSuggestions.GetType () == typeof(FunctionDelegate) ?
					(List<string>)((FunctionDelegate)actionSuggestions).Invoke (null) : (List<string>)actionSuggestions
				);

			PDASymbol symbol = new PDASymbol (symbolContent);

			return symbol;
		}

		public PDASymbol GenerateStackSymbol(StackSymbolContent content, string name = "New Stack Symbol") {

			StackSymbolContent symbolContent =
				new StackSymbolContent (
					content.IndicatedObj == null ? (GameObject)GetIndicatedObj (null) :
					content.IndicatedObj.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.IndicatedObj).Function : content.IndicatedObj.GetType () == typeof(FunctionDelegate) ?
					(GameObject)((FunctionDelegate)content.IndicatedObj).Invoke (null) : (GameObject)content.IndicatedObj,
					content.GraspedObj == null ? (GameObject)GetGraspedObj (null) :
					content.GraspedObj.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.GraspedObj).Function : content.GraspedObj.GetType () == typeof(FunctionDelegate) ?
					(GameObject)((FunctionDelegate)content.GraspedObj).Invoke (null) : (GameObject)content.GraspedObj,
					content.IndicatedRegion == null ? (Region)GetIndicatedRegion (null) :
					content.IndicatedRegion.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.IndicatedRegion).Function : content.IndicatedRegion.GetType () == typeof(FunctionDelegate) ?
					(Region)((FunctionDelegate)content.IndicatedRegion).Invoke (null) : (Region)content.IndicatedRegion,
					content.ObjectOptions == null ? GetObjectOptions (null) :
					content.ObjectOptions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.ObjectOptions).Function : content.ObjectOptions.GetType () == typeof(FunctionDelegate) ?
					(List<GameObject>)((FunctionDelegate)content.ObjectOptions).Invoke (null) : (List<GameObject>)content.ObjectOptions,
					content.ActionOptions == null ? GetActionOptions (null) :
					content.ActionOptions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.ActionOptions).Function : content.ActionOptions.GetType () == typeof(FunctionDelegate) ?
					(List<string>)((FunctionDelegate)content.ActionOptions).Invoke (null) : (List<string>)content.ActionOptions,
					content.ActionSuggestions == null ? GetActionSuggestions (null) :
					content.ActionSuggestions.GetType () == typeof(DelegateFactory) ? 
					((DelegateFactory)content.ActionSuggestions).Function : content.ActionSuggestions.GetType () == typeof(FunctionDelegate) ?
					(List<string>)((FunctionDelegate)content.ActionSuggestions).Invoke (null) : (List<string>)content.ActionSuggestions
				);

			PDASymbol symbol = new PDASymbol (symbolContent);

			return symbol;
		}

		public PDASymbol GenerateStackSymbolFromConditions(
			Expression<Predicate<GameObject>> indicatedObjCondition, Expression<Predicate<GameObject>> graspedObjCondition,
			Expression<Predicate<Region>> indicatedRegionCondition, Expression<Predicate<List<GameObject>>> objectOptionsCondition,
			Expression<Predicate<List<string>>> actionOptionsCondition, Expression<Predicate<List<string>>> actionSuggestionsCondition,
			string name = "New Stack Symbol") {

			StackSymbolConditions symbolConditions =
				new StackSymbolConditions (null, null, null, null, null, null);

			symbolConditions.IndicatedObjCondition = indicatedObjCondition;
			symbolConditions.GraspedObjCondition = graspedObjCondition;
			symbolConditions.IndicatedRegionCondition = indicatedRegionCondition;
			symbolConditions.ObjectOptionsCondition = objectOptionsCondition;
			symbolConditions.ActionOptionsCondition = actionOptionsCondition;
			symbolConditions.ActionSuggestionsCondition = actionSuggestionsCondition;

			PDASymbol symbol = new PDASymbol (symbolConditions);

			return symbol;
		}

		public string StackSymbolToString(object stackSymbol) {
			//Debug.Log (stackSymbol.GetType ());
			if (stackSymbol == null) {
				return "[]";
			}
			else if (stackSymbol.GetType () == typeof(PDASymbol)) {
				PDASymbol symbol = (PDASymbol)stackSymbol;
				if (symbol.Content == null) {
					return "[]";
				} 
				else if (symbol.Content.GetType () == typeof(StackSymbolContent)) {
					StackSymbolContent content = (StackSymbolContent)symbol.Content;

					return string.Format ("[{0},{1},{2},{3},{4},{5}]",
						content.IndicatedObj == null ? "Null" : 
						content.IndicatedObj.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.IndicatedObj).ToString() :
						System.Convert.ToString (((GameObject)content.IndicatedObj).name),
						content.GraspedObj == null ? "Null" : 
						content.GraspedObj.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.GraspedObj).ToString() :
						System.Convert.ToString (((GameObject)content.GraspedObj).name),
						content.IndicatedRegion == null ? "Null" : 
						content.IndicatedRegion.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.IndicatedRegion).ToString() :
						Helper.RegionToString ((Region)content.IndicatedRegion),
						content.ObjectOptions == null ? "Null" : 
						content.ObjectOptions.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.ObjectOptions).ToString() :
						string.Format ("[{0}]", String.Join (", ", ((List<GameObject>)content.ObjectOptions).Select (o => o.name).ToArray ())),
						content.ActionOptions == null ? "Null" : 
						content.ActionOptions.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.ActionOptions).ToString() :
						string.Format ("[{0}]", String.Join (", ", ((List<string>)content.ActionOptions).ToArray ())),
						content.ActionSuggestions == null ? "Null" : 
						content.ActionSuggestions.GetType() == typeof(FunctionDelegate) ? 
						((FunctionDelegate)content.ActionSuggestions).ToString() :
						string.Format ("[{0}]", String.Join (", ", ((List<string>)content.ActionSuggestions).ToArray ())));
				} 
				else if (symbol.Content.GetType () == typeof(StackSymbolConditions)) {
					StackSymbolConditions content = (StackSymbolConditions)symbol.Content;

					return string.Format ("[{0},{1},{2},{3},{4},{5}]",
						content.IndicatedObjCondition == null ? "Null" : System.Convert.ToString (content.IndicatedObjCondition),
						content.GraspedObjCondition == null ? "Null" : System.Convert.ToString (content.GraspedObjCondition),
						content.IndicatedRegionCondition == null ? "Null" : System.Convert.ToString (content.IndicatedRegionCondition),
						content.ObjectOptionsCondition == null ? "Null" : System.Convert.ToString (content.ObjectOptionsCondition),
						content.ActionOptionsCondition == null ? "Null" : System.Convert.ToString (content.ActionOptionsCondition),
						content.ActionSuggestionsCondition == null ? "Null" : System.Convert.ToString (content.ActionSuggestionsCondition));
				}
			}
			else if (stackSymbol.GetType () == typeof(StackSymbolContent)) {
				StackSymbolContent content = (StackSymbolContent)stackSymbol;

				return string.Format ("[{0},{1},{2},{3},{4},{5}]",
					content.IndicatedObj == null ? "Null" : 
					content.IndicatedObj.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.IndicatedObj).ToString() :
					System.Convert.ToString (((GameObject)content.IndicatedObj).name),
					content.GraspedObj == null ? "Null" : 
					content.GraspedObj.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.GraspedObj).ToString() :
					System.Convert.ToString (((GameObject)content.GraspedObj).name),
					content.IndicatedRegion == null ? "Null" : 
					content.IndicatedRegion.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.IndicatedRegion).ToString() :
					Helper.RegionToString ((Region)content.IndicatedRegion),
					content.ObjectOptions == null ? "Null" : 
					content.ObjectOptions.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.ObjectOptions).ToString() :
					string.Format ("[{0}]", String.Join (", ", ((List<GameObject>)content.ObjectOptions).Select (o => o.name).ToArray ())),
					content.ActionOptions == null ? "Null" : 
					content.ActionOptions.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.ActionOptions).ToString() :
					string.Format ("[{0}]", String.Join (", ", ((List<string>)content.ActionOptions).ToArray ())),
					content.ActionSuggestions == null ? "Null" : 
					content.ActionSuggestions.GetType() == typeof(FunctionDelegate) ? 
					((FunctionDelegate)content.ActionSuggestions).ToString() :
					string.Format ("[{0}]", String.Join (", ", ((List<string>)content.ActionSuggestions).ToArray ())));
			}
			else if (stackSymbol.GetType () == typeof(FunctionDelegate)) {
				return string.Format(":{0}",((FunctionDelegate)stackSymbol).Method.Name);
			}

			return string.Empty;
		}

		public void RewriteStack(PDAStackOperation operation) {
			if (operation.Type != PDAStackOperation.PDAStackOperationType.Rewrite) {
				return;
			}
			else {
				if (operation.Content != null) {
					PDASymbol symbol = Stack.Pop ();
					//Stack.Push ((PDASymbol)operation.Content);
					PerformStackOperation(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,operation.Content));
				}

				Debug.Log (string.Format("RewriteStack: {0} result {1}", operation.Type,StackSymbolToString (GetCurrentStackSymbol ())));

				// handle state transitions on stack rewrite

				List<PDAInstruction> instructions = GetApplicableInstructions (CurrentState, null,
					GetCurrentStackSymbol ().Content);

				PDAInstruction instruction = null;

				if (instructions.Count > 1) {
					Debug.Log (string.Format("Multiple instruction condition ({0}).  Aborting.",instructions.Count));
					foreach (PDAInstruction inst in instructions) {
						Debug.Log(string.Format ("{0},{1},{2},{3},{4}", 
							(inst.FromStates == null) ? "Null" :
							string.Format("[{0}]", 
								String.Join (", ", ((List<PDAState>)inst.FromStates).Select (s => s.Name).ToArray ())),
							(inst.InputSymbols == null) ? "Null" :
							string.Format("[{0}]",
								String.Join (", ", ((List<PDASymbol>)inst.InputSymbols).Select (s => s.Content.ToString()).ToArray ())),
							StackSymbolToString (inst.StackSymbol),
							inst.ToState.Name,
							string.Format ("[{0},{1}]",
								inst.StackOperation.Type.ToString (),
								(inst.StackOperation.Content == null) ? "Null" : StackSymbolToString (inst.StackOperation.Content))));
					}
					return;
				}
				else if (instructions.Count == 1) {
					instruction = instructions [0];
					Debug.Log (string.Format ("{0},{1},{2},{3},{4}", 
						(instruction.FromStates == null) ? "Null" :
						string.Format("[{0}]", 
							String.Join (", ", ((List<PDAState>)instruction.FromStates).Select (s => s.Name).ToArray ())),
						(instruction.InputSymbols == null) ? "Null" :
						string.Format("[{0}]", 
							String.Join (", ", ((List<PDASymbol>)instruction.InputSymbols).Select (s => s.Content.ToString()).ToArray ())),
						StackSymbolToString (instruction.StackSymbol),
						instruction.ToState.Name,
						string.Format ("[{0},{1}]",
							instruction.StackOperation.Type.ToString (),
							(instruction.StackOperation.Content == null) ? "Null" : StackSymbolToString (instruction.StackOperation.Content))));
				}
				else if (instructions.Count < 1) {
					Debug.Log ("Zero instruction condition.  Aborting.");
					return;
				}

				if (instruction != null) {
					MoveToState (instruction.ToState);
					PerformStackOperation (instruction.StackOperation);
					ExecuteStateContent ();
				}
			}
		}

		public char GetInputSymbolType(string receivedData) {
			return receivedData.Split()[0].Trim ()[0];
		}

		public string RemoveInputSymbolType(string receivedData, char inputSymbolType) {
			return receivedData.TrimStart (inputSymbolType).Trim ();
		}

		public string GetGestureTrigger(string receivedData) {
			return receivedData.Split()[receivedData.Split().Length-1].Trim ();
		}

		public string RemoveGestureTrigger(string receivedData, string gestureTrigger) {
			return receivedData.Replace (gestureTrigger, "").TrimStart(',').Trim();
		}

		public string GetGestureContent(string receivedData, string gestureCode) {
			return receivedData.Replace (gestureCode, "").Split () [1];
		}

		string RemoveInputSymbolContent(string inputSymbol) {
			return inputSymbol.Split (',') [0];
		}

		void ReadInputSymbol (object sender, EventArgs e) {
			if (!((CharacterLogicEventArgs)e).InputSymbolName.StartsWith ("P")) {
				Debug.Log (((CharacterLogicEventArgs)e).InputSymbolName);
				Debug.Log (((CharacterLogicEventArgs)e).InputSymbolContent);
			}

			LastInputSymbol = GetInputSymbolByName (((CharacterLogicEventArgs)e).InputSymbolName);

			List<PDAInstruction> instructions =  GetApplicableInstructions (CurrentState,
				GetInputSymbolByName (((CharacterLogicEventArgs)e).InputSymbolName),
				GetCurrentStackSymbol ().Content);

			PDAInstruction instruction = null;

			if (instructions.Count > 1) {
				Debug.Log (string.Format("Multiple instruction condition ({0}).  Aborting.",instructions.Count));
				foreach (PDAInstruction inst in instructions) {
					Debug.Log(string.Format ("{0},{1},{2},{3},{4}", 
						(inst.FromStates == null) ? "Null" :
						string.Format("[{0}]", 
							String.Join (", ", ((List<PDAState>)inst.FromStates).Select (s => s.Name).ToArray ())),
						(inst.InputSymbols == null) ? "Null" :
						string.Format("[{0}]", 
							String.Join (", ", ((List<PDASymbol>)inst.InputSymbols).Select (s => s.Content.ToString()).ToArray ())),
						StackSymbolToString (inst.StackSymbol),
						inst.ToState.Name,
						string.Format ("[{0},{1}]",
							inst.StackOperation.Type.ToString (),
							(inst.StackOperation.Content == null) ? "Null" : StackSymbolToString (inst.StackOperation.Content))));
				}
				return;
			}
			else if (instructions.Count == 1) {
				instruction = instructions [0];
				Debug.Log (string.Format ("{0},{1},{2},{3},{4}", 
					(instruction.FromStates == null) ? "Null" :
					string.Format("[{0}]", 
						String.Join (", ", ((List<PDAState>)instruction.FromStates).Select (s => s.Name).ToArray ())),
					(instruction.InputSymbols == null) ? "Null" :
					string.Format("[{0}]", 
						String.Join (", ", ((List<PDASymbol>)instruction.InputSymbols).Select (s => s.Content.ToString()).ToArray ())),
					StackSymbolToString (instruction.StackSymbol),
					instruction.ToState.Name,
					string.Format ("[{0},{1}]",
						instruction.StackOperation.Type.ToString (),
						(instruction.StackOperation.Content == null) ? "Null" : StackSymbolToString (instruction.StackOperation.Content))));
			}
			else if (instructions.Count < 1) {
				Debug.Log ("Zero instruction condition.  Aborting.");
				return;
			}

			if (instruction != null) {
				// update epistemic model
				if ((interactionController.UseTeaching) && (useEpistemicModel)) {
					UpdateEpistemicModel (((CharacterLogicEventArgs)e).InputSymbolName, EpistemicCertaintyOperation.Increase);
				}

				Debug.Log (interactionController.UseTeaching);
				Debug.Log (useEpistemicModel);
				if ((interactionController.UseTeaching) && (instruction.ToState.Content != null)) {
					object stateContent = instruction.ToState.Content;

					if (stateContent.GetType () == typeof(TransitionGate)) {
						FunctionDelegate evaluateCondition = ((TransitionGate)stateContent).Condition;
						object result = ((ActionOptions.Count == 0) || (GetInputSymbolByName(ActionOptions[0]) == null) ||
							((ActionSuggestions.Count > 0) && (ActionOptions.Count > 0) && 
								(GetInputSymbolByName(ActionOptions[0]) == GetInputSymbolByName(ActionSuggestions[0])))) ? 
							evaluateCondition (((CharacterLogicEventArgs)e).InputSymbolName) :
							evaluateCondition (ActionOptions[0]);
						Debug.Log (result.GetType ());
						Debug.Log (result);

						if (!(bool)result) {
							MoveToState (((TransitionGate)stateContent).RejectState);
							PerformStackOperation (((TransitionGate)stateContent).RejectStackOperation);
						} 
						else {
							MoveToState (instruction.ToState);
							PerformStackOperation (instruction.StackOperation);
						}
					}
				}
				else {
					MoveToState (instruction.ToState);
					PerformStackOperation (instruction.StackOperation);
				}

				ExecuteStateContent (((CharacterLogicEventArgs)e).InputSymbolContent);
			}
		}

		Dictionary<PDASymbol,List<Concept>> MapInputSymbolsToConcepts(List<PDASymbol> symbols) {
			Dictionary<PDASymbol,List<Concept>> mapping = new Dictionary<PDASymbol,List<Concept>> ();

			mapping.Add(GetInputSymbolByName("G left point high"),
				new Concept[]{epistemicModel.state.GetConcept("point",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G right point high"),
				new Concept[]{epistemicModel.state.GetConcept("point",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G posack high"),
				new Concept[]{epistemicModel.state.GetConcept("posack",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G negack high"),
				new Concept[]{epistemicModel.state.GetConcept("negack",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab high"),
				new Concept[]{epistemicModel.state.GetConcept("grab",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move left high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move right high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move front high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move back high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move up high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("UP", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move down high"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("DOWN", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push left high"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push right high"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push front high"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push back high"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)}.ToList());

			mapping.Add(GetInputSymbolByName("G left point start"),
				new Concept[]{epistemicModel.state.GetConcept("point",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G right point start"),
				new Concept[]{epistemicModel.state.GetConcept("point",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G posack start"),
				new Concept[]{epistemicModel.state.GetConcept("posack",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G negack start"),
				new Concept[]{epistemicModel.state.GetConcept("negack",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab start"),
				new Concept[]{epistemicModel.state.GetConcept("grab",ConceptType.ACTION, ConceptMode.G)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move left start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move right start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move front start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move back start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move up start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("UP", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G grab move down start"),
				new Concept[]{epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("DOWN", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push left start"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push right start"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push front start"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G push back start"),
				new Concept[]{epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
					epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("G nevermind start"),
				new Concept[]{epistemicModel.state.GetConcept("NEVERMIND",ConceptType.ACTION, ConceptMode.L)}.ToList());

			mapping.Add(GetInputSymbolByName("S THIS"),
				new Concept[]{epistemicModel.state.GetConcept("THIS",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S THAT"),
				new Concept[]{epistemicModel.state.GetConcept("THAT",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S THERE"),
				new Concept[]{epistemicModel.state.GetConcept("THERE",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S YES"),
				new Concept[]{epistemicModel.state.GetConcept("YES",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S NO"),
				new Concept[]{epistemicModel.state.GetConcept("NO",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S NEVERMIND"),
				new Concept[]{epistemicModel.state.GetConcept("NEVERMIND",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S NOTHING"),
				new Concept[]{epistemicModel.state.GetConcept("NOTHING",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S GRAB"),
				new Concept[]{epistemicModel.state.GetConcept("GRAB",ConceptType.ACTION, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S RED"),
				new Concept[]{epistemicModel.state.GetConcept("RED",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S GREEN"),
				new Concept[]{epistemicModel.state.GetConcept("GREEN",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S YELLOW"),
				new Concept[]{epistemicModel.state.GetConcept("YELLOW",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S ORANGE"),
				new Concept[]{epistemicModel.state.GetConcept("ORANGE",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S BLACK"),
				new Concept[]{epistemicModel.state.GetConcept("BLACK",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S PURPLE"),
				new Concept[]{epistemicModel.state.GetConcept("PURPLE",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
//			mapping.Add(GetInputSymbolByName("S PINK"),
//				new Concept[]{epistemicModel.state.GetConcept("PINK",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
			mapping.Add(GetInputSymbolByName("S WHITE"),
				new Concept[]{epistemicModel.state.GetConcept("WHITE",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
//			mapping.Add(GetInputSymbolByName("S PUT"),
//				new Concept[]{epistemicModel.state.GetConcept("PUT", ConceptType.ACTION, ConceptMode.L)}.ToList());

			foreach (PDASymbol symbol in symbols) {
				if (!mapping.ContainsKey (symbol)) {
					Debug.Log (string.Format ("MapInputSymbolsToConcepts: no mapping for symbol \"{0}\"", symbol.Name));
				}
			}

			return mapping;
		}

		void PerformStackOperation(PDAStackOperation operation) {
			switch (operation.Type) {
			case PDAStackOperation.PDAStackOperationType.None:
				break;

			case PDAStackOperation.PDAStackOperationType.Pop:
				PDASymbol result = Stack.Pop ();
				break;

			case PDAStackOperation.PDAStackOperationType.Push:
				if (operation.Content.GetType () == typeof(FunctionDelegate)) {
					object content = ((FunctionDelegate)operation.Content).Invoke (null);
					Debug.Log (content.GetType ());
					foreach (PDASymbol symbol in (List<PDASymbol>)content) {
						Debug.Log (StackSymbolToString ((PDASymbol)symbol));
					}

					if (content.GetType () == typeof(PDASymbol)) {
						// When we push a new Stack symbol we should clone the CurrentStackSymbol and check the conditions below to adjust the values
						//PDASymbol pushSymbol = (PDASymbol)content;

						Stack.Push (GenerateStackSymbol ((StackSymbolContent)((PDASymbol)content).Content));
					}
					else if ((content is IList) && (content.GetType ().IsGenericType) &&
						(content.GetType ().IsAssignableFrom (typeof(List<PDASymbol>)))) {
						foreach (PDASymbol symbol in (List<PDASymbol>)content) {
							//Debug.Log (((StackSymbolContent)symbol.Content).IndicatedObj);
							Stack.Push (GenerateStackSymbol ((StackSymbolContent)((PDASymbol)symbol).Content));
						}
					}
				} 
				else if (operation.Content.GetType () == typeof(PDASymbol)) {
					Stack.Push (GenerateStackSymbol ((StackSymbolContent)((PDASymbol)operation.Content).Content));
				} 
				else if ((operation.Content is IList) && (operation.Content.GetType ().IsGenericType) &&
					(operation.Content.GetType ().IsAssignableFrom (typeof(List<PDASymbol>)))) {
					foreach (PDASymbol symbol in (List<PDASymbol>)operation.Content) {
						Stack.Push (GenerateStackSymbol ((StackSymbolContent)((PDASymbol)symbol).Content));
					}
				} 
				else if (operation.Content.GetType () == typeof(StackSymbolContent)) {
					Stack.Push (GenerateStackSymbol ((StackSymbolContent)operation.Content));
				}
				else if ((operation.Content is IList) && (operation.Content.GetType ().IsGenericType) &&
					(operation.Content.GetType ().IsAssignableFrom (typeof(List<StackSymbolContent>)))) {
					foreach (StackSymbolContent symbol in (List<StackSymbolContent>)operation.Content) {
						Stack.Push (GenerateStackSymbol ((StackSymbolContent)symbol));
					}
				}
				break;

			case PDAStackOperation.PDAStackOperationType.Rewrite:
				RewriteStack (new PDAStackOperation (PDAStackOperation.PDAStackOperationType.Rewrite, null));
				break;

			case PDAStackOperation.PDAStackOperationType.Flush:
				if (operation.Content != null) {
					if (operation.Content.GetType () == typeof(StackSymbolContent)) {
						Stack.Clear ();
						Stack.Push (GenerateStackSymbol ((StackSymbolContent)operation.Content));
					}
				}
				else {
					StackSymbolContent persistentContent = new StackSymbolContent (
						null, GraspedObj, null, null, null, null);	// keep GraspedObj because it is a physical state, not a mental one
					Stack.Clear ();
					Stack.Push (GenerateStackSymbol (persistentContent));
				}

				break;

			default:
				break;
			}

			Debug.Log (string.Format("PerformStackOperation: {0} result {1}", operation.Type,StackSymbolToString (GetCurrentStackSymbol ())));
		}

		List<PDAInstruction> GetApplicableInstructions(PDAState fromState, PDASymbol inputSymbol, object stackSymbol) {
			Debug.Log (fromState.Name);
			Debug.Log (inputSymbol == null ? "Null" : inputSymbol.Name);
			Debug.Log (StackSymbolToString (stackSymbol));
			foreach (PDASymbol element in Stack) {
				Debug.Log (StackSymbolToString (element));
			}

			List<PDAInstruction> instructions = TransitionRelation.Where (i => 
				(i.FromStates == null && fromState == null) || (i.FromStates != null && i.FromStates.Contains(fromState))).ToList();
			instructions = instructions.Where (i =>
				(i.InputSymbols == null && inputSymbol == null) || (i.InputSymbols != null && i.InputSymbols.Contains(inputSymbol))).ToList();

			Debug.Log (instructions.Count);

			if (stackSymbol.GetType () == typeof(StackSymbolContent)) {
				//instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolContent))).ToList();
				//Debug.Log (instructions.Count);
				instructions = instructions.Where (i =>
					((i.StackSymbol.Content.GetType() == typeof(StackSymbolContent)) &&
						(i.StackSymbol.Content as StackSymbolContent) == (stackSymbol as StackSymbolContent)) ||
					((i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions)) &&
						(i.StackSymbol.Content as StackSymbolConditions).SatisfiedBy(stackSymbol as StackSymbolContent))).ToList();
			}

			instructions = instructions.Where(i => !(instructions.Where (j => ((j.ToState.Content != null) &&
				(j.ToState.Content.GetType() == typeof(TransitionGate)))).Select(j => 
					((TransitionGate)j.ToState.Content).RejectState).ToList()).Contains(i.ToState)).ToList();
			//			else if (stackSymbol.GetType () == typeof(StackSymbolConditions)) {
			//				instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions))).ToList();
			//				instructions = instructions.Where (i => ((i.StackSymbol.Content as StackSymbolConditions) == (stackSymbol as StackSymbolConditions))).ToList();
			//			}

			//			Debug.Log (instructions.Count);

			return instructions;
		}

		void MoveToState(PDAState state) {
			Pair<PDASymbol, PDAState> symbolStatePair = new Pair<PDASymbol, PDAState> (GetLastInputSymbol (), state);

			if (CurrentState != null) {
				if (TransitionRelation.Where (i => (i.FromStates.Contains(CurrentState)) && (i.ToState == state)).ToList ().Count == 0) {
					Debug.Log (string.Format ("No transition arc between state {0} and state {1}.  Aborting.", CurrentState.Name, state.Name));
					return;
				}
					
				if (state.Name == "BeginInteraction") {
					epistemicModel.state.InitiateEpisim ();
					StateTransitionHistory.Push (symbolStatePair);
				}
				else if (state.Name == "Wait") {
					if (CurrentState.Name != "TrackPointing") {
						StateTransitionHistory.Push (symbolStatePair);
					}
				}
				else if (state.Name == "TrackPointing") {
					if (StateTransitionHistory.Peek ().Item2.Name != "TrackPointing") {
						StateTransitionHistory.Push (symbolStatePair);
					}
				}
				else if (state.Name == "EndState") {
					Debug.Log ("Disengaging EpiSim");
					epistemicModel.state.DisengageEpisim ();
					StateTransitionHistory.Push (symbolStatePair);
				}
				else {
					StateTransitionHistory.Push (symbolStatePair);
				}
			}
			else {
				StateTransitionHistory.Push (symbolStatePair);
			}

			CurrentState = state;

			if ((repeatAfterWait) && (repeatTimerTime > 0)) {
				repeatTimer.Interval = repeatTimerTime;
				repeatTimer.Enabled = true;
			}

			Debug.Log (string.Format("Entering state: {0}.  Stack symbol: {1}",CurrentState.Name,
				StackSymbolToString(GetCurrentStackSymbol())));
		}

		void ExecuteStateContent(object tempMessage = null) {
			MethodInfo methodToCall = interactionController.GetType ().GetMethod (CurrentState.Name);
			List<object> contentMessages = new List<object> ();

			contentMessages.Add (tempMessage);

			if (methodToCall != null) {
				Debug.Log ("MoveToState: invoke " + methodToCall.Name);
				object obj = methodToCall.Invoke (interactionController, new object[]{ contentMessages.ToArray() });
			}
			else {
				Debug.Log(string.Format("No method of name {0} on object {1}", CurrentState.Name, interactionController));
			}
		}

		void UpdateEpistemicModel(string inputSymbol, EpistemicCertaintyOperation certaintyOperation) {
			if (CurrentState == GetState ("StartState") || CurrentState == GetState ("BeginInteraction")) {
				return;
			}

			// if input symbol is negack/NO, state is Suggest, take the action suggestion and reduce its Certainty
			if ((CurrentState == GetState("Suggest")) && (ActionSuggestions.Count > 0)) {
				if (GetInputSymbolsByName ("G negack high", "S NO").Contains (GetInputSymbolByName (inputSymbol))) {
					UpdateEpistemicModel (RemoveInputSymbolContent (ActionSuggestions [0]), EpistemicCertaintyOperation.Decrease);
				}
				else if (GetInputSymbolsByName ("G posack high", "S YES").Contains (GetInputSymbolByName (inputSymbol))) {
					UpdateEpistemicModel (RemoveInputSymbolContent (ActionSuggestions [0]), EpistemicCertaintyOperation.Increase);
				}
			}

			if (GetInputSymbolByName (inputSymbol) != null) {
				if (symbolConceptMap.ContainsKey (GetInputSymbolByName (inputSymbol))) {
					List<Concept> concepts = symbolConceptMap [GetInputSymbolByName (inputSymbol)];

					List<Concept> conceptsToUpdate = new List<Concept>();
					List<Relation> relationsToUpdate = new List<Relation>();
					foreach (Concept concept in concepts) {
						if (GetInputSymbolType (inputSymbol) == 'G') {
							concept.Certainty = (certaintyOperation == EpistemicCertaintyOperation.Increase) ?
							(concept.Certainty < 0.5) ? 0.5 : 1.0 : 0.0;
						}
						else if (GetInputSymbolType (inputSymbol) == 'S') {
							concept.Certainty = (certaintyOperation == EpistemicCertaintyOperation.Increase) ? 1.0 : 0.0;
						}

						Debug.Log (string.Format ("Updating epistemic model: Concept {0} Certainty = {1}", concept.Name, concept.Certainty));
						conceptsToUpdate.Add(concept);

						foreach (Concept relatedConcept in epistemicModel.state.GetRelated(concept))
						{
							Relation relation = epistemicModel.state.GetRelation(concept, relatedConcept);
							double prevCertainty = relation.Certainty;
							double newCertainty = Math.Min(concept.Certainty, relatedConcept.Certainty);
							if (Math.Abs(prevCertainty - newCertainty) > 0.01)
							{
								relation.Certainty = newCertainty;
								relationsToUpdate.Add(relation);
							}
						}
					}
                    epistemicModel.state.UpdateEpisim (conceptsToUpdate.ToArray(), relationsToUpdate.ToArray());
				}
			}
		}

		object EpistemicallyCertain (object inputSignal) {
			if ((!interactionController.UseTeaching) || (!useEpistemicModel)) {
				return true;
			}
				
			if (inputSignal.GetType () != typeof(string)) {
				Debug.Log ("EpistemicCertainty: inputSignal not of type string.  Aborting.");
				return false;
			}

			double aggregateCertainty = 1.0;
			int conceptCount = 0;

			PDASymbol inputSymbol = GetInputSymbolByName ((string)inputSignal);
			if (inputSymbol != null) {
				if (symbolConceptMap.ContainsKey (inputSymbol)) {
					List<Concept> concepts = symbolConceptMap [inputSymbol];

					foreach (Concept concept in concepts) {
						Debug.Log (string.Format("{0}:{1}",concept.Name,concept.Certainty));
						conceptCount++;
						double conceptCertainty = concept.Certainty;

						foreach (Concept related in concept.Related) {
							if (related.Certainty > conceptCertainty) {
								conceptCertainty = related.Certainty;
							}
						}

						aggregateCertainty *= conceptCertainty;
					}
				}
			}

			return (aggregateCertainty > 0.5);
		}

		void RepeatUtterance(object sender, ElapsedEventArgs e) {
			if ((repeatAfterWait) && (repeatTimerTime > 0)) {
				repeatTimer.Interval = repeatTimerTime;
				forceRepeat = true;
				Debug.Log ("Repeating");
			}
		}
	}
}

