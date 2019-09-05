#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;

using Object = System.Object;
using VoxSimPlatform.Agent;
using VoxSimPlatform.Agent.CharacterLogic;
using VoxSimPlatform.Episteme;
using VoxSimPlatform.Global;
using VoxSimPlatform.Interaction;

public class StackSymbolContent : IEquatable<Object> {
	public object IndicatedObj { get; set; }

	public object GraspedObj { get; set; }

	public object IndicatedRegion { get; set; }

	public object ObjectOptions { get; set; }

	public object ActionOptions { get; set; }

	public object ActionSuggestions { get; set; }

	public StackSymbolContent(object indicatedObj, object graspedObj, object indicatedRegion,
		object objectOptions, object actionOptions, object actionSuggestions) {
		this.IndicatedObj = indicatedObj;
		this.GraspedObj = graspedObj;
		this.IndicatedRegion = indicatedRegion;
		this.ObjectOptions = objectOptions;
		this.ActionOptions = actionOptions;
		this.ActionSuggestions = actionSuggestions;
	}

	public StackSymbolContent(StackSymbolContent clone) {
		this.IndicatedObj = (GameObject) clone.IndicatedObj;
		this.GraspedObj = (GameObject) clone.GraspedObj;
		this.IndicatedRegion = (clone.IndicatedRegion != null) ? new Region((Region) clone.IndicatedRegion) : null;
		this.ObjectOptions = (clone.ObjectOptions != null)
			? new List<GameObject>((List<GameObject>) clone.ObjectOptions)
			: null;
		this.ActionOptions = (clone.ActionOptions != null)
			? new List<string>((List<string>) clone.ActionOptions)
			: null;
		this.ActionSuggestions = (clone.ActionSuggestions != null)
			? new List<string>((List<string>) clone.ActionSuggestions)
			: null;
	}

	public override bool Equals(object obj) {
		if (obj == null || (obj as StackSymbolContent) == null) //if the object is null or the cast fails
			return false;
		else {
			StackSymbolContent tuple = (StackSymbolContent) obj;
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

			return (GameObject) IndicatedObj == (GameObject) tuple.IndicatedObj &&
			       (GameObject) GraspedObj == (GameObject) tuple.GraspedObj &&
			       Helper.RegionsEqual((Region) IndicatedRegion, (Region) tuple.IndicatedRegion) &&
			       ((List<GameObject>) ObjectOptions).SequenceEqual((List<GameObject>) tuple.ObjectOptions) &&
			       ((List<string>) ActionOptions).SequenceEqual((List<string>) tuple.ActionOptions) &&
			       ((List<string>) ActionSuggestions).SequenceEqual((List<string>) tuple.ActionSuggestions);
		}
	}

	public override int GetHashCode() {
		return IndicatedObj.GetHashCode() ^ GraspedObj.GetHashCode() ^
		       IndicatedRegion.GetHashCode() ^ ObjectOptions.GetHashCode() ^
		       ActionOptions.GetHashCode() ^ ActionSuggestions.GetHashCode();
	}

	public static bool operator == (StackSymbolContent tuple1, StackSymbolContent tuple2) {
		return tuple1.Equals(tuple2);
	}

	public static bool operator != (StackSymbolContent tuple1, StackSymbolContent tuple2) {
		return !tuple1.Equals(tuple2);
	}
}

public class StackSymbolConditions : IEquatable<Object> {
	public Expression<Predicate<GameObject>> IndicatedObjCondition { get; set; }

	public Expression<Predicate<GameObject>> GraspedObjCondition { get; set; }

	public Expression<Predicate<Region>> IndicatedRegionCondition { get; set; }

	public Expression<Predicate<List<GameObject>>> ObjectOptionsCondition { get; set; }

	public Expression<Predicate<List<string>>> ActionOptionsCondition { get; set; }

	public Expression<Predicate<List<string>>> ActionSuggestionsCondition { get; set; }

	public StackSymbolConditions(Expression<Predicate<GameObject>> indicatedObjCondition,
		Expression<Predicate<GameObject>> graspedObjCondition,
		Expression<Predicate<Region>> indicatedRegionCondition,
		Expression<Predicate<List<GameObject>>> objectOptionsCondition,
		Expression<Predicate<List<string>>> actionOptionsCondition,
		Expression<Predicate<List<string>>> actionSuggestionsCondition) {
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
			StackSymbolContent tuple = (StackSymbolContent) obj;

			return ((IndicatedObjCondition == null) ||
			        (IndicatedObjCondition.Compile().Invoke((GameObject) tuple.IndicatedObj))) &&
			       ((GraspedObjCondition == null) ||
			        (GraspedObjCondition.Compile().Invoke((GameObject) tuple.GraspedObj))) &&
			       ((IndicatedRegionCondition == null) ||
			        (IndicatedRegionCondition.Compile().Invoke((Region) tuple.IndicatedRegion))) &&
			       ((ObjectOptionsCondition == null) || (ObjectOptionsCondition.Compile()
				        .Invoke((List<GameObject>) tuple.ObjectOptions))) &&
			       ((ActionOptionsCondition == null) ||
			        (ActionOptionsCondition.Compile().Invoke((List<string>) tuple.ActionOptions))) &&
			       ((ActionSuggestionsCondition == null) || (ActionSuggestionsCondition.Compile()
				        .Invoke((List<string>) tuple.ActionSuggestions)));
		}
	}

	public override bool Equals(object obj) {
		if (obj == null || (obj as StackSymbolConditions) == null) //if the object is null or the cast fails
			return false;
		else {
			StackSymbolConditions tuple = (StackSymbolConditions) obj;

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
				equal &= Convert.ToString(IndicatedObjCondition) ==
				         Convert.ToString(tuple.IndicatedObjCondition);
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
				equal &= Convert.ToString(GraspedObjCondition) ==
				         Convert.ToString(tuple.GraspedObjCondition);
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
				equal &= Convert.ToString(IndicatedRegionCondition) ==
				         Convert.ToString(tuple.IndicatedRegionCondition);
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
				equal &= Convert.ToString(ObjectOptionsCondition) ==
				         Convert.ToString(tuple.ObjectOptionsCondition);
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
				equal &= Convert.ToString(ActionOptionsCondition) ==
				         Convert.ToString(tuple.ActionOptionsCondition);
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
				equal &= Convert.ToString(ActionSuggestionsCondition) ==
				         Convert.ToString(tuple.ActionSuggestionsCondition);
				//					equal &= Expression.Lambda<Func<bool>>(Expression.Equal(ActionSuggestionsCondition, tuple.ActionSuggestionsCondition)).Compile()();
			}

			Debug.Log(equal);
			return equal;
		}
	}

	public override int GetHashCode() {
		return IndicatedObjCondition.GetHashCode() ^ GraspedObjCondition.GetHashCode() ^
		       IndicatedRegionCondition.GetHashCode() ^ ObjectOptionsCondition.GetHashCode() ^
		       ActionOptionsCondition.GetHashCode() ^ ActionSuggestionsCondition.GetHashCode();
	}

	public static bool operator ==(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
		return tuple1.Equals(tuple2);
	}

	public static bool operator !=(StackSymbolConditions tuple1, StackSymbolConditions tuple2) {
		return !tuple1.Equals(tuple2);
	}
}

public class NewInstructionEventArgs : EventArgs {
	public string InstructionKey { get; set; }

	public NewInstructionEventArgs(string instructionKey) {
		this.InstructionKey = instructionKey;
	}
}

public class StateChangeEventArgs : EventArgs {
	public PDAState State { get; set; }

	public StateChangeEventArgs(PDAState state) {
		this.State = state;
	}
}

public class AttentionShiftEventArgs : EventArgs {
	public PDASymbol Symbol { get; set; }

	public AttentionShiftEventArgs(PDASymbol symbol) {
		this.Symbol = symbol;
	}
}

public class DianaInteractionLogic : CharacterLogicAutomaton {
	public GameObject IndicatedObj {
		get {
			return GetCurrentStackSymbol() == null
				? null
				: (GameObject) ((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedObj;
		}
	}

	public GameObject GraspedObj {
		get {
			return GetCurrentStackSymbol() == null
				? null
				: (GameObject) ((StackSymbolContent) GetCurrentStackSymbol().Content).GraspedObj;
		}
	}

	public Region IndicatedRegion {
		get {
			return GetCurrentStackSymbol() == null
				? null
				: (Region) ((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedRegion;
		}
	}

	public List<GameObject> ObjectOptions {
		get {
			return GetCurrentStackSymbol() == null
				? new List<GameObject>()
				: (List<GameObject>) ((StackSymbolContent) GetCurrentStackSymbol().Content).ObjectOptions;
		}
	}

	public List<string> ActionOptions {
		get {
			return GetCurrentStackSymbol() == null
				? new List<string>()
				: (List<string>) ((StackSymbolContent) GetCurrentStackSymbol().Content).ActionOptions;
		}
	}

	public List<string> ActionSuggestions {
		get {
			return GetCurrentStackSymbol() == null
				? new List<string>()
				: (List<string>) ((StackSymbolContent) GetCurrentStackSymbol().Content).ActionSuggestions;
		}
	}

	public string eventConfirmation = "";

	public GameObject objectConfirmation = null;

	public AttentionStatus attentionStatus;
	public bool useOrderingHeuristics;
	public bool humanRelativeDirections;
	public bool waveToStart;
	public bool useEpistemicModel;
	public bool repeatAfterWait;
	public double repeatTimerTime = 10000;
	public double servoWaitTimerTime = 500;
	public double servoLoopTimerTime = 10;

	public SingleAgentInteraction interactionController;

#if UNITY_EDITOR
	[CustomEditor(typeof(DianaInteractionLogic))]
	public class DebugPreview : Editor {
		public override void OnInspectorGUI() {
			var bold = new GUIStyle();
			bold.fontStyle = FontStyle.Bold;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Attention Status", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).attentionStatus =
				(AttentionStatus) GUILayout.SelectionGrid((int) ((DianaInteractionLogic) target).attentionStatus,
					new string[] {"Inattentive", "Attentive"}, 1, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Use Ordering Heuristics", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).useOrderingHeuristics =
				GUILayout.Toggle(((DianaInteractionLogic) target).useOrderingHeuristics, "");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Human Relative Directions", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).humanRelativeDirections =
				GUILayout.Toggle(((DianaInteractionLogic) target).humanRelativeDirections, "");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Wave To Start", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).waveToStart =
				GUILayout.Toggle(((DianaInteractionLogic) target).waveToStart, "");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Use Epistemic Model", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).useEpistemicModel =
				GUILayout.Toggle(((DianaInteractionLogic) target).useEpistemicModel, "");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Repeat After Wait", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).repeatAfterWait =
				GUILayout.Toggle(((DianaInteractionLogic) target).repeatAfterWait, "");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Repeat Wait Time", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).repeatTimerTime = Convert.ToDouble(
				GUILayout.TextField(((DianaInteractionLogic) target).repeatTimerTime.ToString(),
					GUILayout.Width(50)));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Servo Wait Time", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).servoWaitTimerTime = Convert.ToDouble(
				GUILayout.TextField(((DianaInteractionLogic) target).servoWaitTimerTime.ToString(),
					GUILayout.Width(50)));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Servo Loop Time", bold, GUILayout.Width(150));
			((DianaInteractionLogic) target).servoLoopTimerTime = Convert.ToDouble(
				GUILayout.TextField(((DianaInteractionLogic) target).servoLoopTimerTime.ToString(),
					GUILayout.Width(50)));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Current State", bold, GUILayout.Width(150));
			GUILayout.Label(((DianaInteractionLogic) target).CurrentState == null
				? "Null"
				: ((DianaInteractionLogic) target).CurrentState.Name);
			GUILayout.EndHorizontal();

			// some styling for the header, this is optional
			GUILayout.Label("Stack", bold);

			// add a label for each item, you can add more properties
			// you can even access components inside each item and display them
			// for example if every item had a sprite we could easily show it 
			if (((DianaInteractionLogic) target).Stack != null) {
				foreach (PDASymbol item in ((DianaInteractionLogic) target).Stack) {
					GUILayout.Label(((DianaInteractionLogic) target).StackSymbolToString(item));
				}
			}

			GUILayout.Label("State History", bold);
			if (((DianaInteractionLogic) target).StateTransitionHistory != null) {
				foreach (Triple<PDASymbol, PDAState, PDASymbol> item in ((DianaInteractionLogic) target)
					.StateTransitionHistory) {
					GUILayout.BeginHorizontal();
					GUILayout.Label(item.Item1 == null ? "Null" : item.Item1.Name, GUILayout.Width(150));
					GUILayout.Label(item.Item2 == null ? "Null" : item.Item2.Name, GUILayout.Width(150));
					GUILayout.Label(item.Item3 == null
						? "Null"
						: ((DianaInteractionLogic) target).StackSymbolToString(item.Item3));
					GUILayout.EndHorizontal();
				}
			}

			GUILayout.Label("Context Memory", bold);
			if (((DianaInteractionLogic) target).ContextualMemory != null) {
				foreach (Triple<PDASymbol, PDAState, PDASymbol> item in ((DianaInteractionLogic) target)
					.ContextualMemory) {
					GUILayout.BeginHorizontal();
					GUILayout.Label(item.Item1 == null ? "Null" : item.Item1.Name, GUILayout.Width(150));
					GUILayout.Label(item.Item2 == null ? "Null" : item.Item2.Name, GUILayout.Width(150));
					GUILayout.Label(item.Item3 == null
						? "Null"
						: ((DianaInteractionLogic) target).StackSymbolToString(item.Item3));
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

	Dictionary<PDASymbol, List<Concept>> symbolConceptMap;

	Timer servoWaitTimer;
	Timer servoLoopTimer;

	public bool inServoLoop = false;

	public bool forceChangeState = false;
	public PDAState forceMoveToState = null;

	Timer repeatTimer;
	bool forceRepeat = false;

	List<PDASymbol> attentionInputSymbols = new List<PDASymbol>();
	List<PDASymbol> inattentionInputSymbols = new List<PDASymbol>();

	public event EventHandler ChangeState;

	public void OnChangeState(object sender, EventArgs e) {
		if (ChangeState != null) {
			ChangeState(this, e);
		}
	}

	public event EventHandler AttentionShift;

	public void OnAttentionShift(object sender, EventArgs e) {
		if (AttentionShift != null) {
			AttentionShift(this, e);
		}
	}

	protected string GetLastInputSymbolName(object arg) {
		return GetLastInputSymbol().Name;
	}

	protected List<string> GetLastInputSymbolNameAsList(object arg) {
		return new List<string>(new string[] {GetLastInputSymbol().Name});
	}

	public object NullObject(object arg) {
		return null;
	}

	public object GetIndicatedObj(object arg) {
		return IndicatedObj;
	}

	public object GetGraspedObj(object arg) {
		return GraspedObj;
	}

	public object GetIndicatedRegion(object arg) {
		return IndicatedRegion;
	}

	public object GetObjectOptions(object arg) {
		return ObjectOptions;
	}

	public object GetActionOptions(object arg) {
		return ActionOptions;
	}

	public object GetActionSuggestions(object arg) {
		return ActionSuggestions;
	}

	public List<string> GetActionOptionsIfNull(object arg) {
		if (ActionSuggestions.Count == 0) {
			return ActionOptions;
		}
		else {
			return null;
		}
	}

	public List<PDASymbol> PushObjectOptions(object arg) {
		List<PDASymbol> symbolList = ((ObjectOptions.Count == 1) && (IndicatedRegion == null))
			? Enumerable.Range(0, ObjectOptions.Count).Select(s =>
				GenerateStackSymbol((IndicatedObj == null)
						? (IndicatedRegion == null) ? ObjectOptions[s] : ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()[s]
						: IndicatedObj,
					null, null,
					new List<GameObject>(),
					null, null)).ToList()
			: Enumerable.Range(0, ObjectOptions.Count).Select(s =>
				GenerateStackSymbol((IndicatedObj == null)
						? (IndicatedRegion == null) ? ObjectOptions[s] : ObjectOptions.OrderByDescending(
							m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()[s]
						: IndicatedObj,
					null, null,
					(IndicatedRegion == null)
						? ObjectOptions.GetRange(0, s + 1)
						: ObjectOptions.OrderByDescending(
								m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()
							.GetRange(0, s + 1),
					null, null)).ToList();

		return symbolList;
	}

	public List<PDASymbol> PushObjectTargetOptions(object arg) {
		List<PDASymbol> symbolList = ((ObjectOptions.Count == 1) && (IndicatedRegion == null))
			? Enumerable.Range(0, ObjectOptions.Count).Select(s =>
				GenerateStackSymbol(null, null, null,
					new List<GameObject>(),
					null, null)).ToList()
			: Enumerable.Range(0, ObjectOptions.Count).Select(s =>
				GenerateStackSymbol(null, null, null,
					(IndicatedRegion == null)
						? ObjectOptions.GetRange(0, s + 1)
						: ObjectOptions.OrderByDescending(
								m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()
							.GetRange(0, s + 1),
					(IndicatedRegion == null)
						? ObjectOptions.GetRange(0, s + 1).Select(m =>
							ActionOptions[0].Replace("{1}", string.Format("on({0})", m.name))).ToList()
						: ObjectOptions
							.OrderByDescending(m => (m.transform.position - IndicatedRegion.center).magnitude)
							.ToList().GetRange(0, s + 1).Select(m =>
								ActionOptions[0].Replace("{1}", string.Format("on({0})", m.name))).ToList(),
					null)).ToList();

		return symbolList;
	}

	public List<PDASymbol> PushPutOptions(object arg) {
		List<PDASymbol> symbolList = Enumerable.Range(0,
			ObjectOptions.Count).Select(s =>
			GenerateStackSymbol(IndicatedObj,
				null, null,
				(IndicatedRegion != null)
					? ObjectOptions.OrderByDescending(
						m => (m.transform.position - IndicatedRegion.center).magnitude).ToList().GetRange(0, s + 1)
					: ObjectOptions,
				Enumerable.Range(0, s + 1).Select(a => string.Format("put({0},on({1}))", (IndicatedObj != null)
						? IndicatedObj.name
						: (GraspedObj != null)
							? GraspedObj.name
							: "{0}",
					ObjectOptions.OrderByDescending(
						m => (m.transform.position - IndicatedRegion.center).magnitude).ToList()[a].name)).ToList(),
				new List<string>())).ToList();

		return symbolList;
	}

	public List<PDASymbol> PushGraspOptions(object arg) {
		List<PDASymbol> symbolList = Enumerable.Range(0, ActionOptions.Count).Select(s =>
			GenerateStackSymbol(IndicatedObj, null, null, null,
				ActionOptions.GetRange(ActionOptions.Count - 1 - s, s + 1),
				new List<string>())).ToList();

		return symbolList;
	}

	public List<PDASymbol> SwitchGraspedBackToIndicated(object arg) {
		List<PDASymbol> symbolList = new List<PDASymbol>() {
			GenerateStackSymbol(IndicatedObj, new FunctionDelegate(NullObject),
				null, null, null, null)
		};

		return symbolList;
	}

	public List<string> GenerateGraspCommand(object arg) {
		List<string> actionList = new List<string>(
			new string[] {"grasp({0})"});

		return actionList;
	}


	public List<string> GeneratePutAtRegionCommand(object arg) {
		List<string> actionList = new List<string>(
			new string[] {"put({0}" + string.Format(",{0})", Helper.VectorToParsable(IndicatedRegion.center))});

		return actionList;
	}

	public List<string> GeneratePutObjectOnObjectCommand(object arg) {
		List<string> actionList = new List<string>(
			new string[] {
				string.Format("put({0},on({1}))",
					GraspedObj == null ? "{0}" : GraspedObj.name,
					IndicatedObj == null ? "{0}" : IndicatedObj.name)
			});

		return actionList;
	}

	public List<string> GenerateDirectedPutCommand(object arg) {
		List<string> actionList = new List<string>(
			new string[] {
				"put({0}" + string.Format(",{0})",
					GetGestureContent(
						RemoveInputSymbolType(
							RemoveGestureTrigger(
								ActionSuggestions[0], GetGestureTrigger(ActionSuggestions[0])),
							GetInputSymbolType(ActionSuggestions[0])),
						"grab move").ToLower())
			});

		return actionList;
	}

	public List<string> GenerateDirectedSlideCommand(object arg) {
		List<string> actionList = (ActionSuggestions.Count > 0)
			? new List<string>(
				new string[] {
					"slide({0}" + string.Format(",{0})",
						GetGestureContent(
							RemoveInputSymbolType(
								RemoveGestureTrigger(
									ActionSuggestions[0], GetGestureTrigger(ActionSuggestions[0])),
								GetInputSymbolType(ActionSuggestions[0])),
							"push").ToLower())
				})
			: new List<string>(
				new string[] {
					"slide({0}" + string.Format(",{0})",
						GetGestureContent(
							RemoveInputSymbolType(
								RemoveGestureTrigger(
									ActionOptions[0], GetGestureTrigger(ActionOptions[0])),
								GetInputSymbolType(ActionOptions[0])),
							"push").ToLower())
				});

		return actionList;
	}

	public List<string> GenerateDirectedServoCommand(object arg) {
		List<string> actionList = (ActionSuggestions.Count > 0)
			? new List<string>(
				new string[] {
					"slidep({0}" + string.Format(",{0})",
						GetGestureContent(
							RemoveInputSymbolType(
								RemoveGestureTrigger(
									ActionSuggestions[0], GetGestureTrigger(ActionSuggestions[0])),
								GetInputSymbolType(ActionSuggestions[0])),
							"push servo").ToLower())
				})
			: new List<string>(
				new string[] {
					"slidep({0}" + string.Format(",{0})",
						GetGestureContent(
							RemoveInputSymbolType(
								RemoveGestureTrigger(
									ActionOptions[0], GetGestureTrigger(ActionOptions[0])),
								GetInputSymbolType(ActionOptions[0])),
							"push servo").ToLower())
				});

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

		base.Start();

		if ((repeatAfterWait) && (repeatTimerTime > 0)) {
			repeatTimer = new Timer(repeatTimerTime);
			repeatTimer.Enabled = false;
			repeatTimer.Elapsed += RepeatUtterance;
		}

		if (servoWaitTimerTime > 0) {
			servoWaitTimer = new Timer(servoWaitTimerTime);
			servoWaitTimer.Enabled = false;
			servoWaitTimer.Elapsed += MoveToServo;
		}

		if (servoLoopTimerTime > 0) {
			servoLoopTimer = new Timer(servoLoopTimerTime);
			servoLoopTimer.Enabled = false;
			servoLoopTimer.Elapsed += MoveToServo;
		}

		ChangeState += HandleStateChange;

		//interactionController.UseTeaching = (PlayerPrefs.GetInt("Use Teaching Agent") == 1);

		States.Add(new PDAState("StartState", null));
		States.Add(new PDAState("BeginInteraction", null));
		States.Add(new PDAState("Ready", null));
		States.Add(new PDAState("Suggest", null));

		States.Add(new PDAState("Confirm",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null))));

		States.Add(new PDAState("Wait",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null))));

		States.Add(new PDAState("ParseSentence", null));
		States.Add(new PDAState("ParseQuestion", null));
		States.Add(new PDAState("ParseVP", null));
		States.Add(new PDAState("ParseNP", null));
		States.Add(new PDAState("ParsePP", null));
		States.Add(new PDAState("TrackPointing", null));
		States.Add(new PDAState("SituateDeixis",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null)))));

		States.Add(new PDAState("InterpretDeixis", null));
		States.Add(new PDAState("DisambiguateObject",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null))));

		States.Add(new PDAState("IndexByColor", null));
		States.Add(new PDAState("IndexBySize", null));
		States.Add(new PDAState("IndexByRegion", null));
		States.Add(new PDAState("IndexByGesture", null));
		States.Add(new PDAState("RegionAsGoal",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null))));

		States.Add(new PDAState("StartGrab",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null, null)))));

		States.Add(new PDAState("DisambiguateGrabPose", null));
		States.Add(new PDAState("PromptLearn", null));
		States.Add(new PDAState("StartLearn", null));
		States.Add(new PDAState("LearningSucceeded", null));
		States.Add(new PDAState("LearnNewInstruction", null));
		States.Add(new PDAState("LearningFailed", null));
		States.Add(new PDAState("RetryLearn", null));

		States.Add(new PDAState("StartGrabMove", null));
		States.Add(new PDAState("StopGrabMove",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null,
						new FunctionDelegate(GetActionOptions))))));

		States.Add(new PDAState("StopGrab", null));
		States.Add(new PDAState("StartPush", null));
		States.Add(new PDAState("StopPush",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null,
						new FunctionDelegate(GetActionOptions))))));

		States.Add(new PDAState("StartServo", null));
		States.Add(new PDAState("Servo",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null,
						new FunctionDelegate(GetActionOptions))))));
		States.Add(new PDAState("StopServo", null));

		States.Add(new PDAState("ConfirmObject", null));
		States.Add(new PDAState("RequestObject",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null,
						new FunctionDelegate(GetActionOptionsIfNull))))));

		States.Add(new PDAState("RequestLocation",
			new TransitionGate(
				new FunctionDelegate(EpistemicallyCertain),
				GetState("Suggest"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, null, null,
						new FunctionDelegate(GetActionOptionsIfNull))))));

		States.Add(new PDAState("PlaceInRegion", null));
		States.Add(new PDAState("RequestAction", null));
		States.Add(new PDAState("ComposeObjectAndAction", null));
		States.Add(new PDAState("DisambiguateEvent", null));
		States.Add(new PDAState("ConfirmEvent", null));
		States.Add(new PDAState("ExecuteEvent", null));
		States.Add(new PDAState("AbortAction", null));
		States.Add(new PDAState("ObjectUnavailable", null));
		States.Add(new PDAState("GrabPoseUnavailable", null));
		States.Add(new PDAState("Confusion", null));
		States.Add(new PDAState("CleanUp", null));
		States.Add(new PDAState("EndState", null));

		InputSymbols.Add(new PDASymbol("G engage start"));
		InputSymbols.Add(new PDASymbol("G wave start"));
		InputSymbols.Add(new PDASymbol("G wave stop"));
		InputSymbols.Add(new PDASymbol("G left point start"));
		InputSymbols.Add(new PDASymbol("G right point start"));
		InputSymbols.Add(new PDASymbol("G left point stop"));
		InputSymbols.Add(new PDASymbol("G right point stop"));
		InputSymbols.Add(new PDASymbol("G posack start"));
		InputSymbols.Add(new PDASymbol("G negack start"));
		InputSymbols.Add(new PDASymbol("G posack stop"));
		InputSymbols.Add(new PDASymbol("G negack stop"));
		InputSymbols.Add(new PDASymbol("G grab start"));
		InputSymbols.Add(new PDASymbol("G grab move left start"));
		InputSymbols.Add(new PDASymbol("G grab move right start"));
		InputSymbols.Add(new PDASymbol("G grab move front start"));
		InputSymbols.Add(new PDASymbol("G grab move back start"));
		InputSymbols.Add(new PDASymbol("G grab move up start"));
		InputSymbols.Add(new PDASymbol("G grab move down start"));
		InputSymbols.Add(new PDASymbol("G grab stop"));
		InputSymbols.Add(new PDASymbol("G push left start"));
		InputSymbols.Add(new PDASymbol("G push right start"));
		InputSymbols.Add(new PDASymbol("G push front start"));
		InputSymbols.Add(new PDASymbol("G push back start"));
		InputSymbols.Add(new PDASymbol("G push left stop"));
		InputSymbols.Add(new PDASymbol("G push right stop"));
		InputSymbols.Add(new PDASymbol("G push front stop"));
		InputSymbols.Add(new PDASymbol("G push back stop"));
		InputSymbols.Add(new PDASymbol("G servo left start"));
		InputSymbols.Add(new PDASymbol("G servo right start"));
		InputSymbols.Add(new PDASymbol("G servo front start"));
		InputSymbols.Add(new PDASymbol("G servo back start"));
		InputSymbols.Add(new PDASymbol("G servo left stop"));
		InputSymbols.Add(new PDASymbol("G servo right stop"));
		InputSymbols.Add(new PDASymbol("G servo front stop"));
		InputSymbols.Add(new PDASymbol("G servo back stop"));
		InputSymbols.Add(new PDASymbol("G count one start"));
		InputSymbols.Add(new PDASymbol("G count two start"));
		InputSymbols.Add(new PDASymbol("G count three start"));
		InputSymbols.Add(new PDASymbol("G count four start"));
		InputSymbols.Add(new PDASymbol("G count five start"));
		InputSymbols.Add(new PDASymbol("G count one stop"));
		InputSymbols.Add(new PDASymbol("G count two stop"));
		InputSymbols.Add(new PDASymbol("G count three stop"));
		InputSymbols.Add(new PDASymbol("G count four stop"));
		InputSymbols.Add(new PDASymbol("G count five stop"));
		InputSymbols.Add(new PDASymbol("G nevermind start"));
		InputSymbols.Add(new PDASymbol("G nevermind stop"));
		InputSymbols.Add(new PDASymbol("G teaching start"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 1"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 2"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 3"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 4"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 5"));
		InputSymbols.Add(new PDASymbol("G teaching succeeded 6"));
		InputSymbols.Add(new PDASymbol("G teaching failed"));
		InputSymbols.Add(new PDASymbol("G teaching failed"));
		InputSymbols.Add(new PDASymbol("G teaching failed"));
		InputSymbols.Add(new PDASymbol("G teaching failed"));
		InputSymbols.Add(new PDASymbol("G teaching stop"));
		InputSymbols.Add(new PDASymbol("G rh gesture 1 start"));
		InputSymbols.Add(new PDASymbol("G rh gesture 1 stop"));
		InputSymbols.Add(new PDASymbol("G rh gesture 2 start"));
		InputSymbols.Add(new PDASymbol("G rh gesture 2 stop"));
		InputSymbols.Add(new PDASymbol("G rh gesture 3 start"));
		InputSymbols.Add(new PDASymbol("G rh gesture 3 stop"));
		InputSymbols.Add(new PDASymbol("G lh gesture 4 start"));
		InputSymbols.Add(new PDASymbol("G lh gesture 4 stop"));
		InputSymbols.Add(new PDASymbol("G lh gesture 5 start"));
		InputSymbols.Add(new PDASymbol("G lh gesture 5 stop"));
		InputSymbols.Add(new PDASymbol("G lh gesture 6 start"));
		InputSymbols.Add(new PDASymbol("G lh gesture 6 stop"));
		InputSymbols.Add(new PDASymbol("G attentive start"));
		InputSymbols.Add(new PDASymbol("G inattentive left"));
		InputSymbols.Add(new PDASymbol("G inattentive right"));
		InputSymbols.Add(new PDASymbol("G attentive stop"));
		InputSymbols.Add(new PDASymbol("G engage stop"));

		InputSymbols.Add(new PDASymbol("S YES"));
		InputSymbols.Add(new PDASymbol("S NO"));
		InputSymbols.Add(new PDASymbol("S THIS"));
		InputSymbols.Add(new PDASymbol("S THAT"));
		InputSymbols.Add(new PDASymbol("S THERE"));

		// temp
		InputSymbols.Add(new PDASymbol("S S yes"));
		InputSymbols.Add(new PDASymbol("S S no"));
		InputSymbols.Add(new PDASymbol("S NP this"));
		InputSymbols.Add(new PDASymbol("S NP that"));
		InputSymbols.Add(new PDASymbol("S PP there"));
		InputSymbols.Add(new PDASymbol("S S nothing"));
		InputSymbols.Add(new PDASymbol("S S never mind"));
		InputSymbols.Add(new PDASymbol("S NP red"));
		InputSymbols.Add(new PDASymbol("S NP green"));
		InputSymbols.Add(new PDASymbol("S NP yellow"));
		InputSymbols.Add(new PDASymbol("S NP orange"));
		InputSymbols.Add(new PDASymbol("S NP black"));
		InputSymbols.Add(new PDASymbol("S NP purple"));
		InputSymbols.Add(new PDASymbol("S NP white"));
		InputSymbols.Add(new PDASymbol("S NP pink"));
		// end temp

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
		InputSymbols.Add(new PDASymbol("S SQ"));
		InputSymbols.Add(new PDASymbol("P l"));
		InputSymbols.Add(new PDASymbol("P r"));

		attentionInputSymbols = GetInputSymbolsByName(
			"G attentive start",
			"G attentive stop"
		);

		inattentionInputSymbols = GetInputSymbolsByName(
			"G inattentive left",
			"G inattentive right"
		);

		List<PDASymbol> colors = GetInputSymbolsByName(
			"S RED",
			"S GREEN",
			"S YELLOW",
			"S ORANGE",
			"S BLACK",
			"S PURPLE",
			"S PINK",
			"S WHITE",
			"S NP red",
			"S NP green",
			"S NP yellow",
			"S NP orange",
			"S NP black",
			"S NP purple",
			"S NP white",
			"S NP pink"
		);

		List<PDASymbol> learnedGesture = GetInputSymbolsByName(
			"G teaching succeeded 1",
			"G teaching succeeded 2",
			"G teaching succeeded 3",
			"G teaching succeeded 4",
			"G teaching succeeded 5",
			"G teaching succeeded 6"
		);

		List<PDASymbol> learnableSymbols = GetInputSymbolsByName(
			"G rh gesture 1 start",
			"G rh gesture 2 start",
			"G rh gesture 3 start",
			"G lh gesture 4 start",
			"G lh gesture 5 start",
			"G lh gesture 6 start"
		);

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartState"),
			GetInputSymbolsByName("G engage start"),
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("BeginInteraction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		if (waveToStart) {
			TransitionRelation.Add(new PDAInstruction(
				GetStates("BeginInteraction"),
				GetInputSymbolsByName("G wave start"),
				GenerateStackSymbol(null, null, null, null, null, null),
				GetState("Ready"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));
		}
		else {
			TransitionRelation.Add(new PDAInstruction(
				GetStates("BeginInteraction"),
				null,
				GenerateStackSymbol(null, null, null, null, null, null),
				GetState("Wait"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));
		}

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Ready"),
			null,
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction( // instruction operated by input signal
			GetStates("Wait"), // in this state
			GetInputSymbolsByName("G left point start", "G right point start"), // when we get this message
			GenerateStackSymbol(null, null, null, null, null, null), // and this is the top of the stack
			GetState("SituateDeixis"), // go to this state
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, // and do this to the stack
				new StackSymbolContent(null, null, new Region(), null, null, null)))); // with this symbol

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G left point start", "G right point start",
				"S THIS", "S THAT", "S THERE", "S NP this", "S NP that", "S PP there"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null, null,
				(a) => (a.Count == 0) || (!a[0].Contains("{0}") && !a[0].Contains("{1}")), null
			),
			GetState("SituateDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new Region(), null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G left point start", "G right point start",
				"S THIS", "S THAT", "S THERE", "S NP this", "S NP that", "S PP there"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null, null,
				(a) => (a.Count == 0) || (!a[0].Contains("{0}") && !a[0].Contains("{1}")), null
			),
			GetState("SituateDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new Region(), null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G left point start", "G right point start",
				"S THIS", "S THAT", "S THERE", "S NP this", "S NP that", "S PP there"),
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => (a.Count > 0) && (a[0].Contains("{0}") || a[0].Contains("{1}")), null
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
			GetInputSymbolsByName("S LEFT", "S RIGHT", "S FRONT", "S BACK"),
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("IndexByRegion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new Region(), null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0), null
			),
			GetState("RequestObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0), null
			),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G grab start", "S GRAB"),
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
			GetInputSymbolsByName("G grab start", "S GRAB"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, (g) => g == null,
				null, null, null, null
			),
			GetState("StartGrab"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G grab move left start",
				"G grab move right start",
				"G grab move front start",
				"G grab move back start",
				"G grab move up start"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null,
				null, null, null, null
			),
			GetState("StartGrabMove"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S LEFT", "S RIGHT", "S FRONT", "S BACK"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null,
				null, null, null, null
			),
			GetState("StopGrabMove"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null,
					new FunctionDelegate(GetLastInputSymbolNameAsList), null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G push left start",
				"G push right start",
				"G push front start",
				"G push back start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StartPush"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S LEFT", "S RIGHT", "S FRONT", "S BACK"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null,
				null, null, null, null
			),
			GetState("StopPush"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null,
					new FunctionDelegate(GetLastInputSymbolNameAsList), null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G servo left start",
				"G servo right start",
				"G servo front start",
				"G servo back start"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, (g) => g == null, null, null, null, null
			),
			GetState("StartServo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G servo left start",
				"G servo right start",
				"G servo front start",
				"G servo back start"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null, null, null, null, null
			),
			GetState("StartServo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		//TransitionRelation.Add(new PDAInstruction(  // check this
		//GetStates("Wait"),
		//GetInputSymbolsByName("G servo left start",
		//    "G servo right start",
		//    "G servo front start",
		//    "G servo back start"),
		//GenerateStackSymbolFromConditions(
		//    (o) => o == null, (g) => g == null,
		//    null, null, null, null
		//),
		//GetState("RequestObject"),
		//new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
		//new StackSymbolContent(null, null, null, null,
		//new FunctionDelegate(GenerateDirectedServoCommand), new List<string>()))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("P l", "P r"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("TrackPointing"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G nevermind start", "S NOTHING", "S NEVERMIND", "S S nothing", "S S never mind"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("G nevermind start", "S NOTHING", "S NEVERMIND", "S S nothing", "S S never mind"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S S"),
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("ParseSentence"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S SQ"),
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("ParseQuestion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S VP"),
			GenerateStackSymbol(null, null, null, null, null, null),
			GetState("ParseVP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S VP"),
			GenerateStackSymbolFromConditions((o) => o != null, null, null, null, null, null),
			GetState("ParseVP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S VP"),
			GenerateStackSymbolFromConditions(null, (g) => g != null, null, null, null, null),
			GetState("ParseVP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S NP"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("ParseNP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S PP"),
			GenerateStackSymbolFromConditions((o) => o != null, null, null, null, null, null),
			GetState("ParsePP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S PP"),
			GenerateStackSymbolFromConditions(null, (g) => g != null, null, null, null, null),
			GetState("ParsePP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Wait"),
			GetInputSymbolsByName("S PP"),
			GenerateStackSymbolFromConditions((o) => o == null, (g) => g == null, null, null, null, null),
			GetState("ParsePP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseVP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => (aa.Contains("grasp") ||
				                        aa.Contains("{0}") || aa.Contains("{1}"))).ToList().Count == 0)), null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseVP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("{0}")).ToList().Count > 0)), null),
			GetState("RequestObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseVP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("grasp")).ToList().Count > 0)), null),
			GetState("StartGrab"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseVP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("{1}")).ToList().Count > 0)), null),
			GetState("RequestLocation"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseNP"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, null,
				(r) => r != null && r.max != r.min,
				null, null, null
			),
			GetState("InterpretDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseNP"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, null,
				null, null, null, null
			),
			GetState("ConfirmObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseNP"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null,
				null, null, null, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseNP"),
			null,
			GenerateStackSymbolFromConditions(
				null, (g) => g != null,
				null, null, null, null
			),
			GetState("ComposeObjectAndAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParsePP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("{0}")).ToList().Count > 0)), null),
			GetState("RequestObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParsePP"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("{0}")).ToList().Count == 0)), null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		// if items have value, check and see if sentence is consistent with them
		//			TransitionRelation.Add(new PDAInstruction(
		//				GetStates("Wait"),
		//				GetInputSymbolsByName("S S"),
		//				GenerateStackSymbol(null, null, null, null, null, null),	
		//				GetState("InterpretSentence"),
		//				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None,null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseQuestion"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseQuestion"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseSentence"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseVP"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParseNP"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ParsePP"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

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
			GetInputSymbolsByName("S YES", "S S yes", "G posack start"),
			GenerateStackSymbolFromConditions( // condition set
				null, null, null,
				null, null, (s) => s.Count > 0 // condition: # suggestions > 0
			),
			GetState("Confirm"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NO", "S S no", "G negack start"),
			GenerateStackSymbolFromConditions(
				null, null, null,
				(m) => m.Count == 0, null, (s) => s.Count > 0
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NO", "S S no", "G negack start"),
			GenerateStackSymbolFromConditions(
				null, null, null,
				(m) => m.Count > 1, null, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NO", "S S no", "G negack start"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, (r) => r != null && r.max != r.min,
				(m) => m.Count == 1, null, null
			),
			GetState("RegionAsGoal"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Suggest"),
			GetInputSymbolsByName("S NO", "S S no", "G negack start"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null,
				(m) => m.Count == 1, null, (s) => s.Count > 0
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

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

		TransitionRelation.Add(new PDAInstruction( // instruction operated by stack rewrite
			GetStates("SituateDeixis"),
			null, // no input symbol
			GenerateStackSymbolFromConditions( // condition set
				null, null,
				(r) => r != null && r.max != r.min, // condition: region is indicated
				null, null, null
			),
			GetState("InterpretDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("SituateDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null,
				(r) => r == null,
				null, null, null
			),
			GetState("RequestObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("SituateDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, null,
				(r) => r == null,
				null, null, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("SituateDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				null, (g) => g != null,
				(r) => r == null,
				null, null, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				null, null,
				(r) => r != null && r.max != r.min,
				(m) => m.Count == 0, null, null
			),
			GetState("RegionAsGoal"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null, null,
				(m) => m.Count > 0, (a) => a.Count == 0, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushObjectOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null,
				(m) => m.Count > 0, (a) => a.Count == 0, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushPutOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 0, (a) => a.Count == 0, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushPutOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushObjectTargetOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("slide"))).ToList().Count > 0), null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushObjectTargetOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushObjectTargetOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("InterpretDeixis"),
			null,
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("slide"))).ToList().Count > 0), null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushObjectTargetOptions))));

		//TransitionRelation.Add(new PDAInstruction(
		//    GetStates("InterpretDeixis"),
		//    null,
		//    GenerateStackSymbolFromConditions(
		//        null, (g) => g != null, null,
		//        (m) => m.Count > 0,
		//        (a) => ((a.Count > 0) &&
		//            (a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null
		//    ),
		//    GetState("DisambiguateEvent"),
		//    new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
		//        new FunctionDelegate(PushPutOptions))));

		//TransitionRelation.Add(new PDAInstruction(
		//    GetStates("InterpretDeixis"),
		//    null,
		//    GenerateStackSymbolFromConditions(
		//        (o) => o != null, null, null,
		//        (m) => m.Count > 0,
		//        (a) => ((a.Count > 0) &&
		//            (a.Where(aa => aa.Contains("slide"))).ToList().Count > 0), null
		//    ),
		//    GetState("DisambiguateEvent"),
		//    new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, null)));

		//TransitionRelation.Add(new PDAInstruction(
		//GetStates("InterpretDeixis"),
		//null,
		//GenerateStackSymbolFromConditions(
		//    null, (g) => g != null, null,
		//    (m) => m.Count > 0,
		//    (a) => ((a.Count > 0) &&
		//        (a.Where(aa => aa.Contains("slide"))).ToList().Count > 0), null
		//),
		//GetState("DisambiguateEvent"),
		//new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("IndexByColor"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null,
				(m) => m.Count > 1, null, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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
			GetState("ObjectUnavailable"),
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
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, (g) => g == null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0)),
				null),
			GetState("ConfirmObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0)),
				null),
			GetState("ConfirmObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, (g) => g == null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0),
				null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0),
				null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null, null,
				(m) => m.Count > 0,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count == 0),
				null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
					new List<GameObject>(), null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, null,
				(m) => m.Count > 1, null, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, null,
				(m) => m.Count > 1, null, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null, null,
				(m) => m.Count > 1, null, null
			),
			GetState("DisambiguateObject"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, (r) => r != null && r.max != r.min,
				(m) => m.Count == 1, null, null
			),
			GetState("RegionAsGoal"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, (r) => r != null && r.max != r.min,
				(m) => m.Count == 1, null, null
			),
			GetState("RegionAsGoal"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null, (r) => r != null && r.max != r.min,
				(m) => m.Count == 1, null, null
			),
			GetState("RegionAsGoal"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o != null, null, (r) => r == null,
				(m) => m.Count == 1, null, null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null, (r) => r == null,
				(m) => m.Count == 1, null, null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null, (r) => r == null,
				(m) => m.Count == 1, null, null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("put"))).ToList().Count == 0)), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateObject"),
			GetInputSymbolsByName("S BIG", "S SMALL"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, null, null,
				(m) => m.Count > 0, null, null),
			GetState("IndexBySize"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RegionAsGoal"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, null, null, null, null, null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RegionAsGoal"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null
			),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RegionAsGoal"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null
			),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RegionAsGoal"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
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
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
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
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
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
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
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
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "S S nothing", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("lift"))).ToList().Count == 0)), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "S S nothing", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName("P l", "P r"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("TrackPointing"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName("G left point start", "G right point start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("SituateDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new Region(), null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName("S NP"),
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0), null),
			GetState("ParseNP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestLocation"),
			GetInputSymbolsByName("S NOTHING", "S NEVERMIND", "S S nothing", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => (a.Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestLocation"),
			GetInputSymbolsByName("P l", "P r"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("TrackPointing"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestLocation"),
			GetInputSymbolsByName("G left point start", "G right point start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("SituateDeixis"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, new Region(), null, null, null))));

		//TransitionRelation.Add(new PDAInstruction(
		//GetStates("RequestLocation"),
		//GetInputSymbolsByName("S NP"),
		//GenerateStackSymbolFromConditions(
		//        null, null, null, null,
		//        (a) => ((a.Count > 0) &&
		//            (a.Where(aa => aa.Contains("{1}"))).ToList().Count > 0), null),
		//GetState("ParseNP"),
		//new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestLocation"),
			GetInputSymbolsByName("S PP"),
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{1}"))).ToList().Count > 0), null),
			GetState("ParsePP"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ConfirmObject"),
			null,
			GenerateStackSymbolFromConditions((o) => o != null, (g) => g == null,
				(r) => r == null, null,
				(a) => a.Count == 0, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ConfirmObject"),
			null,
			GenerateStackSymbolFromConditions((o) => o != null, null,
				(r) => r == null, null, null,
				(s) => ((s.Count > 0) && (s[0].Contains("grab move")))
			),
			GetState("StopGrabMove"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ComposeObjectAndAction"),
			null,
			GenerateStackSymbolFromConditions((o) => o != null, (g) => g == null,
				null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}") ||
				                       (aa.Contains("slide")) || aa.Contains("grasp")).ToList().Count == 0)),
				null),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ComposeObjectAndAction"),
			null,
			GenerateStackSymbolFromConditions((o) => o != null, null,
				null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("grasp"))).ToList().Count > 0),
				null),
			GetState("StartGrab"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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
				new StackSymbolContent(null, null, null, null, null, new List<string>()))));

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
			GenerateStackSymbolFromConditions(
				(o) => o == null, null, null, null,
				(a) => a.Count == 0, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ConfirmEvent"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count == 1) &&
				        (a.Where(aa => aa.Contains("{0}")).ToList().Count > 0)), null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ConfirmEvent"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}")).ToList().Count == 0)), null
			),
			GetState("ExecuteEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ExecuteEvent"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count > 0), null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, new List<string>(), null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ExecuteEvent"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("lift"))).ToList().Count == 0), null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartGrab"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g == null,
				null, (a) => a.Count == 0, null, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartGrab"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, null, null
			),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartGrab"),
			null,
			GenerateStackSymbolFromConditions(
				(o) => o != null, (g) => g == null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => aa.Contains(",with("))).ToList().Count > 1)), null
			),
			GetState("DisambiguateGrabPose"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new FunctionDelegate(PushGraspOptions))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateGrabPose"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => (aa.Contains(",with(") &&
				                                     aa.Contains("0")))).ToList().Count == 0)), null
			),
			GetState("PromptLearn"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateGrabPose"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => (aa.Contains(",with(") &&
				                                     aa.Contains("0")))).ToList().Count > 0)), null
			),
			GetState("StartGrab"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateGrabPose"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => aa.Contains(",with("))).ToList().Count > 1)), null
			),
			GetState("DisambiguateGrabPose"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateGrabPose"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => aa.Contains(",with("))).ToList().Count == 1)), null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateGrabPose"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartGrabMove"),
			GetInputSymbolsByName("G grab move down start", "G grab start", "G grab stop"),
			GenerateStackSymbolFromConditions(
				null, (g) => g != null,
				null, null, null, null
			),
			GetState("StopGrabMove"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartGrabMove"),
			GetInputSymbolsByName("G grab move left start",
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
			GetStates("StartServo"),
			GetInputSymbolsByName("G push left start",
				"G push right start",
				"G push front start",
				"G push back start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StartPush"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartServo"),
			GetInputSymbolsByName(),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Servo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartServo"),
			GetInputSymbolsByName("G servo left stop",
				"G servo right stop",
				"G servo front stop",
				"G servo back stop",
				"G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StopServo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("PromptLearn"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StartLearn"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartLearn"),
			learnedGesture,
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => aa.Contains(",with("))).ToList().Count > 0)), null
			),
			GetState("LearningSucceeded"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartLearn"),
			GetInputSymbolsByName("G teaching stop", "G teaching failed"),
			GenerateStackSymbolFromConditions(
				(o) => o == null, (g) => g != null,
				null, null, (a) => ((a.Count > 0) &&
				                    ((a.Where(aa => aa.Contains(",with("))).ToList().Count > 0)), null
			),
			GetState("LearningFailed"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StartLearn"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Servo"),
			GetInputSymbolsByName("G servo left stop",
				"G servo right stop",
				"G servo front stop",
				"G servo back stop",
				"G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StopServo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Servo"),
			null,
			GenerateStackSymbolFromConditions(
				null, null, null, null,
				(a) => a.Count > 0, null
			),
			GetState("StartServo"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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
				new StackSymbolContent(null, null, null, null, null, null))));

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
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("StopServo"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("LearningSucceeded"),
			learnableSymbols,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("LearnNewInstruction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("LearnNewInstruction"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("LearningFailed"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => a.Count > 0, null),
			GetState("RetryLearn"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RetryLearn"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("StartLearn"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateEvent"),
			GetInputSymbolsByName("G posack start", "S YES", "S S yes"),
			GenerateStackSymbolFromConditions(
				null, null, null,
				null, (a) => a.Count > 0, null
			),
			GetState("ConfirmEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
				new StackSymbolContent(null, null, null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateEvent"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, null, null,
				null, (a) => a.Count > 1, null
			),
			GetState("DisambiguateEvent"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateEvent"),
			GetInputSymbolsByName("G negack start", "S NO", "S S no"),
			GenerateStackSymbolFromConditions(
				null, null, null,
				null, (a) => a.Count == 1, null
			),
			GetState("Confusion"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateEvent"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count == 0) || ((a.Count > 0) &&
				                           (a.Where(aa => aa.Contains("put"))).ToList().Count == 0)), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("DisambiguateEvent"),
			GetInputSymbolsByName("S NEVERMIND", "S S never mind", "G nevermind start"),
			GenerateStackSymbolFromConditions(null, null, null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("put"))).ToList().Count > 0), null),
			GetState("AbortAction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("AbortAction"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			//new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Pop, GetState("Wait"))));
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
				new StackSymbolContent(null, new FunctionDelegate(NullObject), null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("ObjectUnavailable"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
				new StackSymbolContent(null, new FunctionDelegate(NullObject), null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("Confusion"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("Wait"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush,
				new StackSymbolContent(null, new FunctionDelegate(NullObject), null, null, null, null))));

		TransitionRelation.Add(new PDAInstruction(
			States,
			GetInputSymbolsByName("G engage stop"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("CleanUp"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

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

		TransitionRelation.Add(new PDAInstruction(
			GetStates("CleanUp"),
			null,
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("EndState"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Flush, GetState("EndState"))));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("EndState"),
			GetInputSymbolsByName("G engage start"),
			GenerateStackSymbolFromConditions(null, null, null, null, null, null),
			GetState("BeginInteraction"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		List<PDAInstruction> gateInstructions = new List<PDAInstruction>();
		foreach (PDAInstruction instruction in TransitionRelation) {
			if (instruction.ToState.Content != null) {
				if (instruction.ToState.Content.GetType() == typeof(TransitionGate)) {
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
						if (gateInstructions.Where(i => ((i.FromStates == instruction.FromStates) &&
						                                 (i.InputSymbols == null) &&
						                                 ((i.StackSymbol.Content.GetType() ==
						                                   typeof(StackSymbolContent) &&
						                                   (((StackSymbolContent) i.StackSymbol.Content).Equals(
							                                   (StackSymbolContent) instruction.StackSymbol.Content)
						                                   )) ||
						                                  (i.StackSymbol.Content.GetType() ==
						                                   typeof(StackSymbolConditions) &&
						                                   (((StackSymbolConditions) i.StackSymbol.Content).Equals(
							                                   (StackSymbolConditions) instruction.StackSymbol
								                                   .Content)))) &&
						                                 (i.ToState ==
						                                  ((TransitionGate) instruction.ToState.Content)
						                                  .RejectState) &&
						                                 (i.StackOperation.Type ==
						                                  PDAStackOperation.PDAStackOperationType.None) &&
						                                 (i.StackOperation.Content == null))).ToList().Count == 0) {
							PDAInstruction newInstruction = new PDAInstruction(instruction.FromStates,
								instruction.InputSymbols,
								instruction.StackSymbol,
								((TransitionGate) instruction.ToState.Content).RejectState,
								new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null));
							gateInstructions.Add(newInstruction);
							Debug.Log(string.Format(
								"Adding gate instruction {0} because {1} ToState {2} has TransitionGate to RejectState {3}",
								string.Format("{0},{1},{2},{3},{4}",
									(newInstruction.FromStates == null)
										? "Null"
										: String.Join(", ",
											((List<PDAState>) newInstruction.FromStates).Select(s => s.Name)
											.ToArray()),
									(newInstruction.InputSymbols == null)
										? "Null"
										: String.Join(", ",
											((List<PDASymbol>) newInstruction.InputSymbols)
											.Select(s => s.Content.ToString()).ToArray()),
									StackSymbolToString(newInstruction.StackSymbol),
									newInstruction.ToState.Name,
									string.Format("[{0},{1}]",
										newInstruction.StackOperation.Type.ToString(),
										(newInstruction.StackOperation.Content == null)
											? "Null"
											: StackSymbolToString(newInstruction.StackOperation.Content))),
								string.Format("{0},{1},{2},{3},{4}",
									(newInstruction.FromStates == null)
										? "Null"
										: String.Join(", ",
											((List<PDAState>) newInstruction.FromStates).Select(s => s.Name)
											.ToArray()),
									(instruction.InputSymbols == null)
										? "Null"
										: String.Join(", ",
											((List<PDASymbol>) instruction.InputSymbols)
											.Select(s => s.Content.ToString()).ToArray()),
									StackSymbolToString(instruction.StackSymbol),
									instruction.ToState.Name,
									string.Format("[{0},{1}]",
										instruction.StackOperation.Type.ToString(),
										(instruction.StackOperation.Content == null)
											? "Null"
											: StackSymbolToString(instruction.StackOperation.Content))),
								instruction.ToState.Name,
								newInstruction.ToState.Name));
						}
					}
				}
			}
		}

		foreach (PDAInstruction instruction in gateInstructions) {
			TransitionRelation.Add(instruction);
		}

		LearnableInstructions.Add(GetInputSymbolsByName("G rh gesture 1 start"), null);
		LearnableInstructions.Add(GetInputSymbolsByName("G rh gesture 2 start"), null);
		LearnableInstructions.Add(GetInputSymbolsByName("G rh gesture 3 start"), null);
		LearnableInstructions.Add(GetInputSymbolsByName("G lh gesture 4 start"), null);
		LearnableInstructions.Add(GetInputSymbolsByName("G lh gesture 5 start"), null);
		LearnableInstructions.Add(GetInputSymbolsByName("G lh gesture 6 start"), null);
		LearnedNewInstruction += AddNewInstruction;

		epistemicModel = GetComponent<EpistemicModel>();
		Debug.Log(epistemicModel);
		Debug.Log(epistemicModel.state);

		symbolConceptMap = MapInputSymbolsToConcepts(InputSymbols);

		PerformStackOperation(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
			GenerateStackSymbol(null, null, null, null, null, null)));
		//StateTransitionHistory.Push(new Triple<PDASymbol, PDAState, PDASymbol>(null, GetState("StartState"),
		//GenerateStackSymbol(null, null, null, null, null, null)));
		MoveToState(GetState("StartState"));
		ExecuteStateContent();

		if (interactionController != null) {
			interactionController.CharacterLogicInput += ReadInputSymbol;
		}
	}

	public override void Update() {
		if (forceRepeat) {
			if ((OutputHelper.GetCurrentOutputString(Role.Affector) != "OK.") &&
			    (OutputHelper.GetCurrentOutputString(Role.Affector) != "OK, never mind.") &&
			    (OutputHelper.GetCurrentOutputString(Role.Affector) != "Bye!")) {
				Debug.Log("Repeating");
				OutputHelper.ForceRepeat(Role.Affector);
				forceRepeat = false;
			}
		}

		if ((forceChangeState) && (forceMoveToState != null)) {
			MoveToState(forceMoveToState);
			ExecuteStateContent();
			forceChangeState = false;
			forceMoveToState = null;
		}
	}

	public PDASymbol GenerateStackSymbol(
		object indicatedObj, object graspedObj, object indicatedRegion,
		object objectOptions, object actionOptions, object actionSuggestions,
		bool overwriteCurrentSymbol = false, string name = "New Stack Symbol") {
		if (actionSuggestions != null) {
			if (actionSuggestions.GetType() == typeof(DelegateFactory)) {
				Debug.Log(((DelegateFactory) actionSuggestions).Function);
			}
		}

		StackSymbolContent symbolContent =
			new StackSymbolContent(
				indicatedObj == null
					? (GameObject) GetIndicatedObj(null)
					: indicatedObj.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) indicatedObj).Function
						: indicatedObj.GetType() == typeof(FunctionDelegate)
							? (GameObject) ((FunctionDelegate) indicatedObj).Invoke(null)
							: (GameObject) indicatedObj,
				graspedObj == null
					? (GameObject) GetGraspedObj(null)
					: graspedObj.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) graspedObj).Function
						: graspedObj.GetType() == typeof(FunctionDelegate)
							? (GameObject) ((FunctionDelegate) graspedObj).Invoke(null)
							: (GameObject) graspedObj,
				indicatedRegion == null
					? (Region) GetIndicatedRegion(null)
					: indicatedRegion.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) indicatedRegion).Function
						: indicatedRegion.GetType() == typeof(FunctionDelegate)
							? (Region) ((FunctionDelegate) indicatedRegion).Invoke(null)
							: (Region) indicatedRegion,
				objectOptions == null
					? GetObjectOptions(null)
					: objectOptions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) objectOptions).Function
						: objectOptions.GetType() == typeof(FunctionDelegate)
							? (List<GameObject>) ((FunctionDelegate) objectOptions).Invoke(null)
							: (List<GameObject>) objectOptions,
				actionOptions == null
					? GetActionOptions(null)
					: actionOptions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) actionOptions).Function
						: actionOptions.GetType() == typeof(FunctionDelegate)
							? (List<string>) ((FunctionDelegate) actionOptions).Invoke(null)
							: (List<string>) actionOptions,
				actionSuggestions == null
					? GetActionSuggestions(null)
					: actionSuggestions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) actionSuggestions).Function
						: actionSuggestions.GetType() == typeof(FunctionDelegate)
							? (List<string>) ((FunctionDelegate) actionSuggestions).Invoke(null)
							: (List<string>) actionSuggestions
			);

		PDASymbol symbol = new PDASymbol(symbolContent);

		return symbol;
	}

	public PDASymbol GenerateStackSymbol(StackSymbolContent content, string name = "New Stack Symbol") {
		StackSymbolContent symbolContent =
			new StackSymbolContent(
				content.IndicatedObj == null
					? (GameObject) GetIndicatedObj(null)
					: content.IndicatedObj.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.IndicatedObj).Function
						: content.IndicatedObj.GetType() == typeof(FunctionDelegate)
							? (GameObject) ((FunctionDelegate) content.IndicatedObj).Invoke(null)
							: (GameObject) content.IndicatedObj,
				content.GraspedObj == null
					? (GameObject) GetGraspedObj(null)
					: content.GraspedObj.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.GraspedObj).Function
						: content.GraspedObj.GetType() == typeof(FunctionDelegate)
							? (GameObject) ((FunctionDelegate) content.GraspedObj).Invoke(null)
							: (GameObject) content.GraspedObj,
				content.IndicatedRegion == null
					? (Region) GetIndicatedRegion(null)
					: content.IndicatedRegion.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.IndicatedRegion).Function
						: content.IndicatedRegion.GetType() == typeof(FunctionDelegate)
							? (Region) ((FunctionDelegate) content.IndicatedRegion).Invoke(null)
							: (Region) content.IndicatedRegion,
				content.ObjectOptions == null
					? GetObjectOptions(null)
					: content.ObjectOptions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.ObjectOptions).Function
						: content.ObjectOptions.GetType() == typeof(FunctionDelegate)
							? (List<GameObject>) ((FunctionDelegate) content.ObjectOptions).Invoke(null)
							: (List<GameObject>) content.ObjectOptions,
				content.ActionOptions == null
					? GetActionOptions(null)
					: content.ActionOptions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.ActionOptions).Function
						: content.ActionOptions.GetType() == typeof(FunctionDelegate)
							? (List<string>) ((FunctionDelegate) content.ActionOptions).Invoke(null)
							: (List<string>) content.ActionOptions,
				content.ActionSuggestions == null
					? GetActionSuggestions(null)
					: content.ActionSuggestions.GetType() == typeof(DelegateFactory)
						? ((DelegateFactory) content.ActionSuggestions).Function
						: content.ActionSuggestions.GetType() == typeof(FunctionDelegate)
							? (List<string>) ((FunctionDelegate) content.ActionSuggestions).Invoke(null)
							: (List<string>) content.ActionSuggestions
			);

		PDASymbol symbol = new PDASymbol(symbolContent);

		return symbol;
	}

	public PDASymbol GenerateStackSymbolFromConditions(
		Expression<Predicate<GameObject>> indicatedObjCondition,
		Expression<Predicate<GameObject>> graspedObjCondition,
		Expression<Predicate<Region>> indicatedRegionCondition,
		Expression<Predicate<List<GameObject>>> objectOptionsCondition,
		Expression<Predicate<List<string>>> actionOptionsCondition,
		Expression<Predicate<List<string>>> actionSuggestionsCondition,
		string name = "New Stack Symbol") {
		StackSymbolConditions symbolConditions =
			new StackSymbolConditions(null, null, null, null, null, null);

		symbolConditions.IndicatedObjCondition = indicatedObjCondition;
		symbolConditions.GraspedObjCondition = graspedObjCondition;
		symbolConditions.IndicatedRegionCondition = indicatedRegionCondition;
		symbolConditions.ObjectOptionsCondition = objectOptionsCondition;
		symbolConditions.ActionOptionsCondition = actionOptionsCondition;
		symbolConditions.ActionSuggestionsCondition = actionSuggestionsCondition;

		PDASymbol symbol = new PDASymbol(symbolConditions);

		return symbol;
	}

	public string StackSymbolToString(object stackSymbol) {
		//Debug.Log (stackSymbol.GetType ());
		if (stackSymbol == null) {
			return "[]";
		}
		else if (stackSymbol.GetType() == typeof(PDASymbol)) {
			PDASymbol symbol = (PDASymbol) stackSymbol;
			if (symbol.Content == null) {
				return "[]";
			}
			else if (symbol.Content.GetType() == typeof(StackSymbolContent)) {
				StackSymbolContent content = (StackSymbolContent) symbol.Content;

				return string.Format("[{0},{1},{2},{3},{4},{5}]",
					content.IndicatedObj == null
						? "Null"
						: content.IndicatedObj.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.IndicatedObj).ToString()
							: Convert.ToString(((GameObject) content.IndicatedObj).name),
					content.GraspedObj == null
						? "Null"
						: content.GraspedObj.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.GraspedObj).ToString()
							: Convert.ToString(((GameObject) content.GraspedObj).name),
					content.IndicatedRegion == null
						? "Null"
						: content.IndicatedRegion.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.IndicatedRegion).ToString()
							: Helper.RegionToString((Region) content.IndicatedRegion),
					content.ObjectOptions == null
						? "Null"
						: content.ObjectOptions.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.ObjectOptions).ToString()
							: string.Format("[{0}]",
								String.Join(", ",
									((List<GameObject>) content.ObjectOptions).Select(o => o.name).ToArray())),
					content.ActionOptions == null
						? "Null"
						: content.ActionOptions.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.ActionOptions).ToString()
							: string.Format("[{0}]",
								String.Join(", ", ((List<string>) content.ActionOptions).ToArray())),
					content.ActionSuggestions == null
						? "Null"
						: content.ActionSuggestions.GetType() == typeof(FunctionDelegate)
							? ((FunctionDelegate) content.ActionSuggestions).ToString()
							: string.Format("[{0}]",
								String.Join(", ", ((List<string>) content.ActionSuggestions).ToArray())));
			}
			else if (symbol.Content.GetType() == typeof(StackSymbolConditions)) {
				StackSymbolConditions content = (StackSymbolConditions) symbol.Content;

				return string.Format("[{0},{1},{2},{3},{4},{5}]",
					content.IndicatedObjCondition == null
						? "Null"
						: Convert.ToString(content.IndicatedObjCondition),
					content.GraspedObjCondition == null
						? "Null"
						: Convert.ToString(content.GraspedObjCondition),
					content.IndicatedRegionCondition == null
						? "Null"
						: Convert.ToString(content.IndicatedRegionCondition),
					content.ObjectOptionsCondition == null
						? "Null"
						: Convert.ToString(content.ObjectOptionsCondition),
					content.ActionOptionsCondition == null
						? "Null"
						: Convert.ToString(content.ActionOptionsCondition),
					content.ActionSuggestionsCondition == null
						? "Null"
						: Convert.ToString(content.ActionSuggestionsCondition));
			}
			else if (symbol.Content.GetType() == typeof(PDAState)) {
				return ((PDAState) symbol.Content).Name;
			}
		}
		else if (stackSymbol.GetType() == typeof(StackSymbolContent)) {
			StackSymbolContent content = (StackSymbolContent) stackSymbol;

			return string.Format("[{0},{1},{2},{3},{4},{5}]",
				content.IndicatedObj == null
					? "Null"
					: content.IndicatedObj.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.IndicatedObj).ToString()
						: Convert.ToString(((GameObject) content.IndicatedObj).name),
				content.GraspedObj == null
					? "Null"
					: content.GraspedObj.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.GraspedObj).ToString()
						: Convert.ToString(((GameObject) content.GraspedObj).name),
				content.IndicatedRegion == null
					? "Null"
					: content.IndicatedRegion.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.IndicatedRegion).ToString()
						: Helper.RegionToString((Region) content.IndicatedRegion),
				content.ObjectOptions == null
					? "Null"
					: content.ObjectOptions.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.ObjectOptions).ToString()
						: string.Format("[{0}]",
							String.Join(", ",
								((List<GameObject>) content.ObjectOptions).Select(o => o.name).ToArray())),
				content.ActionOptions == null
					? "Null"
					: content.ActionOptions.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.ActionOptions).ToString()
						: string.Format("[{0}]", String.Join(", ", ((List<string>) content.ActionOptions).ToArray())),
				content.ActionSuggestions == null
					? "Null"
					: content.ActionSuggestions.GetType() == typeof(FunctionDelegate)
						? ((FunctionDelegate) content.ActionSuggestions).ToString()
						: string.Format("[{0}]",
							String.Join(", ", ((List<string>) content.ActionSuggestions).ToArray())));
		}
		else if (stackSymbol.GetType() == typeof(FunctionDelegate)) {
			return string.Format(":{0}", ((FunctionDelegate) stackSymbol).Method.Name);
		}

		return string.Empty;
	}

	public void RewriteStack(PDAStackOperation operation) {
		if (operation.Type != PDAStackOperation.PDAStackOperationType.Rewrite) {
			return;
		}
		else {
			if (operation.Content != null) {
				PDASymbol symbol = Stack.Pop();
				//Stack.Push ((PDASymbol)operation.Content);

				// if the symbol you just popped has a null parameter
				//  and the operation content has the same null parameter
				//  but the new stack symbol (before the rewrite -> push) does not have a null parameter there
				if (GetCurrentStackSymbol() != null) {
					if (operation.Content.GetType() == typeof(PDASymbol)) {
						if ((((StackSymbolContent) symbol.Content).IndicatedObj == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).IndicatedObj == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedObj != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).IndicatedObj =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).GraspedObj == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).GraspedObj == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).GraspedObj != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).GraspedObj =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).IndicatedRegion == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).IndicatedRegion ==
						     null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedRegion != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).IndicatedRegion =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).ObjectOptions == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).ObjectOptions ==
						     null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ObjectOptions != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).ObjectOptions =
								new List<string>();
						}

						if ((((StackSymbolContent) symbol.Content).ActionOptions == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).ActionOptions ==
						     null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ActionOptions != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).ActionOptions =
								new List<string>();
						}

						if ((((StackSymbolContent) symbol.Content).ActionSuggestions == null) &&
						    (((StackSymbolContent) ((PDASymbol) operation.Content).Content).ActionSuggestions ==
						     null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ActionSuggestions != null)) {
							((StackSymbolContent) ((PDASymbol) operation.Content).Content).ActionSuggestions =
								new List<string>();
						}
					}
					else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
					         (operation.Content.GetType().IsAssignableFrom(typeof(List<StackSymbolContent>)))) {
						if ((((StackSymbolContent) symbol.Content).IndicatedObj == null) &&
						    (((List<StackSymbolContent>) operation.Content)[0].IndicatedObj == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedObj != null)) {
							((List<StackSymbolContent>) operation.Content)[0].IndicatedObj =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).GraspedObj == null) &&
						    (((List<StackSymbolContent>) operation.Content)[0].GraspedObj == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).GraspedObj != null)) {
							((List<StackSymbolContent>) operation.Content)[0].GraspedObj =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).IndicatedRegion == null) &&
						    (((List<StackSymbolContent>) operation.Content)[0].IndicatedRegion == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).IndicatedRegion != null)) {
							((List<StackSymbolContent>) operation.Content)[0].IndicatedRegion =
								new FunctionDelegate(NullObject);
						}

						if ((((StackSymbolContent) symbol.Content).ObjectOptions == null) &&
						    (((List<StackSymbolContent>) ((PDASymbol) operation.Content).Content)[0]
						     .ObjectOptions == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ObjectOptions != null)) {
							((List<StackSymbolContent>) operation.Content)[0].ObjectOptions = new List<string>();
						}

						if ((((StackSymbolContent) symbol.Content).ActionOptions == null) &&
						    (((List<StackSymbolContent>) ((PDASymbol) operation.Content).Content)[0]
						     .ActionOptions == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ActionOptions != null)) {
							((List<StackSymbolContent>) operation.Content)[0].ActionOptions = new List<string>();
						}

						if ((((StackSymbolContent) symbol.Content).ActionSuggestions == null) &&
						    (((List<StackSymbolContent>) ((PDASymbol) operation.Content).Content)[0]
						     .ActionSuggestions == null) &&
						    (((StackSymbolContent) GetCurrentStackSymbol().Content).ActionSuggestions != null)) {
							((List<StackSymbolContent>) operation.Content)[0].ActionSuggestions =
								new List<string>();
						}
					}
				}

				PerformStackOperation(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					operation.Content));
			}

			Debug.Log(string.Format("RewriteStack: {0} result {1}", operation.Type,
				StackSymbolToString(GetCurrentStackSymbol())));

			// handle state transitions on stack rewrite

			List<PDAInstruction> instructions = GetApplicableInstructions(CurrentState, null,
				GetCurrentStackSymbol().Content);

			PDAInstruction instruction = null;

			if (instructions.Count > 1) {
				Debug.Log(string.Format("Multiple instruction condition ({0}).  Aborting.", instructions.Count));
				foreach (PDAInstruction inst in instructions) {
					Debug.Log(string.Format("{0},{1},{2},{3},{4}",
						(inst.FromStates == null)
							? "Null"
							: string.Format("[{0}]",
								String.Join(", ",
									((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
						(inst.InputSymbols == null)
							? "Null"
							: string.Format("[{0}]",
								String.Join(", ",
									((List<PDASymbol>) inst.InputSymbols).Select(s => s.Content.ToString())
									.ToArray())),
						StackSymbolToString(inst.StackSymbol),
						inst.ToState.Name,
						string.Format("[{0},{1}]",
							inst.StackOperation.Type.ToString(),
							(inst.StackOperation.Content == null)
								? "Null"
								: (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
									? StackSymbolToString(inst.StackOperation.Content)
									: (inst.StackOperation.Content.GetType() == typeof(PDAState))
										? ((PDAState) inst.StackOperation.Content).Name
										: (inst.StackOperation.Content.GetType() == typeof(FunctionDelegate))
											? ((FunctionDelegate) inst.StackOperation.Content).Method.Name
											: string.Empty)));
				}

				return;
			}
			else if (instructions.Count == 1) {
				instruction = instructions[0];
				Debug.Log(string.Format("{0},{1},{2},{3},{4}",
					(instruction.FromStates == null)
						? "Null"
						: string.Format("[{0}]",
							String.Join(", ",
								((List<PDAState>) instruction.FromStates).Select(s => s.Name).ToArray())),
					(instruction.InputSymbols == null)
						? "Null"
						: string.Format("[{0}]",
							String.Join(", ",
								((List<PDASymbol>) instruction.InputSymbols).Select(s => s.Content.ToString())
								.ToArray())),
					StackSymbolToString(instruction.StackSymbol),
					instruction.ToState.Name,
					string.Format("[{0},{1}]",
						instruction.StackOperation.Type.ToString(),
						(instruction.StackOperation.Content == null)
							? "Null"
							: (instruction.StackOperation.Content.GetType() == typeof(StackSymbolContent))
								? StackSymbolToString(instruction.StackOperation.Content)
								: (instruction.StackOperation.Content.GetType() == typeof(PDAState))
									? ((PDAState) instruction.StackOperation.Content).Name
									: (instruction.StackOperation.Content.GetType() == typeof(FunctionDelegate))
										? ((FunctionDelegate) instruction.StackOperation.Content).Method.Name
										: string.Empty)));
			}
			else if (instructions.Count < 1) {
				Debug.Log("Zero instruction condition.  Aborting.");
				return;
			}

			if (instruction != null) {
				MoveToState(instruction.ToState);
				PerformStackOperation(instruction.StackOperation);
				ExecuteStateContent();
			}
		}
	}

	public char GetInputSymbolType(string receivedData) {
		return receivedData.Split()[0].Trim()[0];
	}

	public string RemoveInputSymbolType(string receivedData, char inputSymbolType) {
		return receivedData.TrimStart(inputSymbolType).Trim();
	}

	public string GetGestureTrigger(string receivedData) {
		return receivedData.Split()[receivedData.Split().Length - 1].Trim();
	}

	public string RemoveGestureTrigger(string receivedData, string gestureTrigger) {
		return receivedData.Replace(gestureTrigger, "").TrimStart(',').Trim();
	}

	public string GetGestureContent(string receivedData, string gestureCode) {
		// TODO: GetGestureContent -> RemoveInputSymbolType + RemoveGestureTrigger, return result
		return receivedData.Replace(gestureCode, "").Split()[1];
	}

	string RemoveInputSymbolContent(string inputSymbol) {
		return inputSymbol.Split(',')[0];
	}

	void ReadInputSymbol(object sender, EventArgs e) {
		if (!((CharacterLogicEventArgs) e).InputSymbolName.StartsWith("P")) {
			Debug.Log(((CharacterLogicEventArgs) e).InputSymbolName);
			Debug.Log(((CharacterLogicEventArgs) e).InputSymbolContent);
		}

		LastInputSymbol = GetInputSymbolByName(((CharacterLogicEventArgs) e).InputSymbolName);

		if (inattentionInputSymbols.Contains(LastInputSymbol) || attentionInputSymbols.Contains(LastInputSymbol)) {
			OnAttentionShift(this, new AttentionShiftEventArgs(LastInputSymbol));
		}

		var curStackSymbol = GetCurrentStackSymbol();

		if (curStackSymbol == null)
			return;

		List<PDAInstruction> instructions = GetApplicableInstructions(CurrentState,
			GetInputSymbolByName(((CharacterLogicEventArgs) e).InputSymbolName),
			curStackSymbol.Content);

		PDAInstruction instruction = null;

		if (instructions.Count > 1) {
			Debug.Log(string.Format("Multiple instruction condition ({0}).  Aborting.", instructions.Count));
			foreach (PDAInstruction inst in instructions) {
				Debug.Log(string.Format("{0},{1},{2},{3},{4}",
					(inst.FromStates == null)
						? "Null"
						: string.Format("[{0}]",
							String.Join(", ", ((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
					(inst.InputSymbols == null)
						? "Null"
						: string.Format("[{0}]",
							String.Join(", ",
								((List<PDASymbol>) inst.InputSymbols).Select(s => s.Content.ToString()).ToArray())),
					StackSymbolToString(inst.StackSymbol),
					inst.ToState.Name,
					string.Format("[{0},{1}]",
						inst.StackOperation.Type.ToString(),
						(inst.StackOperation.Content == null)
							? "Null"
							: (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
								? StackSymbolToString(inst.StackOperation.Content)
								: (inst.StackOperation.Content.GetType() == typeof(PDAState))
									? ((PDAState) inst.StackOperation.Content).Name
									: string.Empty)));
			}

			return;
		}
		else if (instructions.Count == 1) {
			instruction = instructions[0];
			Debug.Log(string.Format("{0},{1},{2},{3},{4}",
				(instruction.FromStates == null)
					? "Null"
					: string.Format("[{0}]",
						String.Join(", ", ((List<PDAState>) instruction.FromStates).Select(s => s.Name).ToArray())),
				(instruction.InputSymbols == null)
					? "Null"
					: string.Format("[{0}]",
						String.Join(", ",
							((List<PDASymbol>) instruction.InputSymbols).Select(s => s.Content.ToString())
							.ToArray())),
				StackSymbolToString(instruction.StackSymbol),
				instruction.ToState.Name,
				string.Format("[{0},{1}]",
					instruction.StackOperation.Type.ToString(),
					(instruction.StackOperation.Content == null)
						? "Null"
						: (instruction.StackOperation.Content.GetType() == typeof(StackSymbolContent))
							? StackSymbolToString(instruction.StackOperation.Content)
							: (instruction.StackOperation.Content.GetType() == typeof(PDAState))
								? ((PDAState) instruction.StackOperation.Content).Name
								: string.Empty)));
		}
		else if (instructions.Count < 1) {
			Debug.Log("Zero instruction condition.  Aborting.");
			return;
		}

		if (instruction != null) {
			// update epistemic model
			if (useEpistemicModel) {
				UpdateEpistemicModel(((CharacterLogicEventArgs) e).InputSymbolName,
					((CharacterLogicEventArgs) e).InputSymbolContent as string,
					EpistemicCertaintyOperation.Increase);
			}

			Debug.Log(interactionController.UseTeaching);
			Debug.Log(useEpistemicModel);
			if ((interactionController.UseTeaching) && (instruction.ToState.Content != null)) {
				object stateContent = instruction.ToState.Content;

				if (stateContent.GetType() == typeof(TransitionGate)) {
					FunctionDelegate evaluateCondition = ((TransitionGate) stateContent).Condition;
					object result = ((ActionOptions.Count == 0) ||
					                 (GetInputSymbolByName(ActionOptions[0]) == null) ||
					                 ((ActionSuggestions.Count > 0) && (ActionOptions.Count > 0) &&
					                  (GetInputSymbolByName(ActionOptions[0]) ==
					                   GetInputSymbolByName(ActionSuggestions[0]))))
						? evaluateCondition(((CharacterLogicEventArgs) e).InputSymbolName)
						: evaluateCondition(ActionOptions[0]);
					Debug.Log(result.GetType());
					Debug.Log(result);

					if (!(bool) result) {
						MoveToState(((TransitionGate) stateContent).RejectState);
						PerformStackOperation(((TransitionGate) stateContent).RejectStackOperation);
					}
					else {
						MoveToState(instruction.ToState);
						PerformStackOperation(instruction.StackOperation);
					}
				}
			}
			else {
				MoveToState(instruction.ToState);
				PerformStackOperation(instruction.StackOperation);
			}

			ExecuteStateContent(((CharacterLogicEventArgs) e).InputSymbolContent);
		}
	}

	Dictionary<PDASymbol, List<Concept>> MapInputSymbolsToConcepts(List<PDASymbol> symbols) {
		Dictionary<PDASymbol, List<Concept>> mapping = new Dictionary<PDASymbol, List<Concept>>();

		Debug.Log(epistemicModel.state);

		mapping.Add(GetInputSymbolByName("G left point start"),
			new Concept[] {epistemicModel.state.GetConcept("point", ConceptType.ACTION, ConceptMode.G)}.ToList());
		mapping.Add(GetInputSymbolByName("G right point start"),
			new Concept[] {epistemicModel.state.GetConcept("point", ConceptType.ACTION, ConceptMode.G)}.ToList());
		mapping.Add(GetInputSymbolByName("G posack start"),
			new Concept[] {epistemicModel.state.GetConcept("posack", ConceptType.ACTION, ConceptMode.G)}.ToList());
		mapping.Add(GetInputSymbolByName("G negack start"),
			new Concept[] {epistemicModel.state.GetConcept("negack", ConceptType.ACTION, ConceptMode.G)}.ToList());
		mapping.Add(GetInputSymbolByName("G grab start"),
			new Concept[] {epistemicModel.state.GetConcept("grab", ConceptType.ACTION, ConceptMode.G)}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move left start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move right start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move front start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move back start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move up start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("UP", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G grab move down start"),
			new Concept[] {
				epistemicModel.state.GetConcept("move", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("DOWN", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G push left start"),
			new Concept[] {
				epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("LEFT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G push right start"),
			new Concept[] {
				epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("RIGHT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G push front start"),
			new Concept[] {
				epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("FRONT", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G push back start"),
			new Concept[] {
				epistemicModel.state.GetConcept("push", ConceptType.ACTION, ConceptMode.G),
				epistemicModel.state.GetConcept("BACK", ConceptType.PROPERTY, ConceptMode.L)
			}.ToList());
		mapping.Add(GetInputSymbolByName("G nevermind start"),
			new Concept[]
				{epistemicModel.state.GetConcept("NEVERMIND", ConceptType.ACTION, ConceptMode.L)}.ToList());

		mapping.Add(GetInputSymbolByName("S THIS"),
			new Concept[] {epistemicModel.state.GetConcept("THIS", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S THAT"),
			new Concept[] {epistemicModel.state.GetConcept("THAT", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S THERE"),
			new Concept[] {epistemicModel.state.GetConcept("THERE", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S YES"),
			new Concept[] {epistemicModel.state.GetConcept("YES", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S NO"),
			new Concept[] {epistemicModel.state.GetConcept("NO", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S NEVERMIND"),
			new Concept[]
				{epistemicModel.state.GetConcept("NEVERMIND", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S NOTHING"),
			new Concept[] {epistemicModel.state.GetConcept("NOTHING", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S GRAB"),
			new Concept[] {epistemicModel.state.GetConcept("GRAB", ConceptType.ACTION, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S RED"),
			new Concept[] {epistemicModel.state.GetConcept("RED", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S GREEN"),
			new Concept[] {epistemicModel.state.GetConcept("GREEN", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S YELLOW"),
			new Concept[]
				{epistemicModel.state.GetConcept("YELLOW", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S ORANGE"),
			new Concept[]
				{epistemicModel.state.GetConcept("ORANGE", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S BLACK"),
			new Concept[] {epistemicModel.state.GetConcept("BLACK", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S PURPLE"),
			new Concept[]
				{epistemicModel.state.GetConcept("PURPLE", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
//			mapping.Add(GetInputSymbolByName("S PINK"),
//				new Concept[]{epistemicModel.state.GetConcept("PINK",ConceptType.PROPERTY, ConceptMode.L)}.ToList());
		mapping.Add(GetInputSymbolByName("S WHITE"),
			new Concept[] {epistemicModel.state.GetConcept("WHITE", ConceptType.PROPERTY, ConceptMode.L)}.ToList());
//			mapping.Add(GetInputSymbolByName("S PUT"),
//				new Concept[]{epistemicModel.state.GetConcept("PUT", ConceptType.ACTION, ConceptMode.L)}.ToList());

		foreach (PDASymbol symbol in symbols) {
			if (!mapping.ContainsKey(symbol)) {
				Debug.Log(string.Format("MapInputSymbolsToConcepts: no mapping for symbol \"{0}\"", symbol.Name));
			}
		}

		return mapping;
	}

	void PerformStackOperation(PDAStackOperation operation) {
		switch (operation.Type) {
			case PDAStackOperation.PDAStackOperationType.None:
				break;

			case PDAStackOperation.PDAStackOperationType.Pop:
				if ((operation.Content == null) || (operation.Content.GetType() != typeof(PDAState))) {
					Stack.Pop();
					ContextualMemory.Pop();
				}
				else {
					PDASymbol popUntilSymbol = ContextualMemory.First().Item3;
					foreach (Triple<PDASymbol, PDAState, PDASymbol> symbolStateTriple in ContextualMemory.ToList()
						.GetRange(1, ContextualMemory.Count - 2)) {
						Debug.Log(string.Format("{0} {1}", symbolStateTriple.Item3.Name,
							StackSymbolToString(symbolStateTriple.Item3)));
						if ((symbolStateTriple.Item2 == (PDAState) operation.Content) &&
						    (symbolStateTriple.Item3 != GetCurrentStackSymbol())) {
							// if state == operation content && stack symbol != current stack symbol
							popUntilSymbol = symbolStateTriple.Item3;
							break;
						}
					}

					Debug.Log(string.Format(StackSymbolToString(popUntilSymbol)));
					while (Stack.Count > 1 &&
					       !((StackSymbolContent) GetCurrentStackSymbol().Content).Equals(
						       (StackSymbolContent) popUntilSymbol.Content)) {
						Debug.Log(string.Format("Popping {0} until {1}",
							StackSymbolToString(GetCurrentStackSymbol()), StackSymbolToString(popUntilSymbol)));
						Stack.Pop();
						ContextualMemory.Pop();
					}
				}

				break;

			case PDAStackOperation.PDAStackOperationType.Push:
				if (operation.Content.GetType() == typeof(FunctionDelegate)) {
					object content = ((FunctionDelegate) operation.Content).Invoke(null);
					Debug.Log(content.GetType());
					foreach (PDASymbol symbol in (List<PDASymbol>) content) {
						Debug.Log(StackSymbolToString((PDASymbol) symbol));
					}

					if (content.GetType() == typeof(PDASymbol)) {
						// When we push a new Stack symbol we should clone the CurrentStackSymbol and check the conditions below to adjust the values
						//PDASymbol pushSymbol = (PDASymbol)content;

						Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) content).Content));
					}
					else if ((content is IList) && (content.GetType().IsGenericType) &&
					         (content.GetType().IsAssignableFrom(typeof(List<PDASymbol>)))) {
						foreach (PDASymbol symbol in (List<PDASymbol>) content) {
							//Debug.Log (((StackSymbolContent)symbol.Content).IndicatedObj);
							Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) symbol).Content));
						}
					}
				}
				else if (operation.Content.GetType() == typeof(PDASymbol)) {
					Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) operation.Content).Content));
				}
				else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
				         (operation.Content.GetType().IsAssignableFrom(typeof(List<PDASymbol>)))) {
					foreach (PDASymbol symbol in (List<PDASymbol>) operation.Content) {
						Stack.Push(GenerateStackSymbol((StackSymbolContent) ((PDASymbol) symbol).Content));
					}
				}
				else if (operation.Content.GetType() == typeof(StackSymbolContent)) {
					Stack.Push(GenerateStackSymbol((StackSymbolContent) operation.Content));
				}
				else if ((operation.Content is IList) && (operation.Content.GetType().IsGenericType) &&
				         (operation.Content.GetType().IsAssignableFrom(typeof(List<StackSymbolContent>)))) {
					foreach (StackSymbolContent symbol in (List<StackSymbolContent>) operation.Content) {
						Stack.Push(GenerateStackSymbol((StackSymbolContent) symbol));
					}
				}

				break;

			case PDAStackOperation.PDAStackOperationType.Rewrite:
				RewriteStack(new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Rewrite, null));
				break;

			case PDAStackOperation.PDAStackOperationType.Flush:
				if (operation.Content != null) {
					if (operation.Content.GetType() == typeof(StackSymbolContent)) {
						Stack.Clear();
						Stack.Push(GenerateStackSymbol((StackSymbolContent) operation.Content));
						//ContextualMemory.Clear ();
						//ContextualMemory.Push(
						//new Triple<PDASymbol,PDAState,PDASymbol>(GetLastInputSymbol(),CurrentState,
						//GenerateStackSymbol((StackSymbolContent)operation.Content)));
					}
					else if (operation.Content.GetType() == typeof(PDAState)) {
						if (((PDAState) operation.Content) == GetState("EndState")) {
							Stack.Clear();
							Stack.Push(GenerateStackSymbol(null, null, null, null, null, null));
							ContextualMemory.Clear();
							ContextualMemory.Push(new Triple<PDASymbol, PDAState, PDASymbol>(null,
								GetState("StartState"),
								GenerateStackSymbol(null, null, null, null, null, null)));
							//GenerateStackSymbol((StackSymbolContent)operation.Content)));
						}
					}
				}
				else {
					StackSymbolContent persistentContent = new StackSymbolContent(
						null, GraspedObj, null, null, null,
						null); // keep GraspedObj because it is a physical state, not a mental one
					Stack.Clear();
					Stack.Push(GenerateStackSymbol(persistentContent));
					//ContextualMemory.Clear();
					//ContextualMemory.Push(
					//new Triple<PDASymbol, PDAState, PDASymbol>(GetLastInputSymbol(), CurrentState,
					//GenerateStackSymbol(persistentContent)));				
				}

				break;

			default:
				break;
		}

		Debug.Log(string.Format("PerformStackOperation: {0} result {1}", operation.Type,
			StackSymbolToString(GetCurrentStackSymbol())));
	}

	List<PDAInstruction> GetApplicableInstructions(PDAState fromState, PDASymbol inputSymbol, object stackSymbol) {
		Debug.Log(fromState.Name);
		Debug.Log(inputSymbol == null ? "Null" : inputSymbol.Name);
		Debug.Log(string.Format("Stack symbol: {0}", StackSymbolToString(stackSymbol)));
		foreach (PDASymbol element in Stack) {
			Debug.Log(StackSymbolToString(element));
		}

		List<PDAInstruction> instructions = TransitionRelation.Where(i =>
			(i.FromStates == null && fromState == null) ||
			(i.FromStates != null && i.FromStates.Contains(fromState))).ToList();
		instructions = instructions.Where(i =>
			(i.InputSymbols == null && inputSymbol == null) ||
			(i.InputSymbols != null && i.InputSymbols.Contains(inputSymbol))).ToList();

		Debug.Log(string.Format("{0} instructions from {1} with {2}", instructions.Count, fromState.Name,
			inputSymbol == null ? "Null" : inputSymbol.Name));

		foreach (PDAInstruction inst in instructions) {
			Debug.Log(string.Format("{0},{1},{2},{3},{4}",
				(inst.FromStates == null)
					? "Null"
					: string.Format("[{0}]",
						String.Join(", ", ((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
				(inst.InputSymbols == null)
					? "Null"
					: string.Format("[{0}]",
						String.Join(", ",
							((List<PDASymbol>) inst.InputSymbols).Select(s => s.Content.ToString()).ToArray())),
				StackSymbolToString(inst.StackSymbol),
				inst.ToState.Name,
				string.Format("[{0},{1}]",
					inst.StackOperation.Type.ToString(),
					(inst.StackOperation.Content == null)
						? "Null"
						: (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
							? StackSymbolToString(inst.StackOperation.Content)
							: (inst.StackOperation.Content.GetType() == typeof(PDAState))
								? ((PDAState) inst.StackOperation.Content).Name
								: string.Empty)));
		}

		Debug.Log(string.Format("{0} instructions before symbol + gate filtering", instructions.Count));
		Debug.Log(stackSymbol.GetType());

		if (stackSymbol.GetType() == typeof(StackSymbolContent)) {
			//instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolContent))).ToList();
			//Debug.Log (instructions.Count);
			instructions = instructions.Where(i =>
					((i.StackSymbol.Content.GetType() == typeof(StackSymbolContent)) &&
					 (i.StackSymbol.Content as StackSymbolContent) == (stackSymbol as StackSymbolContent)) ||
					((i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions)) &&
					 (i.StackSymbol.Content as StackSymbolConditions).SatisfiedBy(stackSymbol as StackSymbolContent)
					))
				.ToList();
		}

		instructions = instructions.Where(i => !(instructions.Where(j => ((j.ToState.Content != null) &&
		                                                                  (j.ToState.Content.GetType() ==
		                                                                   typeof(TransitionGate)))).Select(j =>
			((TransitionGate) j.ToState.Content).RejectState).ToList()).Contains(i.ToState)).ToList();
		//			else if (stackSymbol.GetType () == typeof(StackSymbolConditions)) {
		//				instructions = instructions.Where (i => (i.StackSymbol.Content.GetType() == typeof(StackSymbolConditions))).ToList();
		//				instructions = instructions.Where (i => ((i.StackSymbol.Content as StackSymbolConditions) == (stackSymbol as StackSymbolConditions))).ToList();
		//			}

		//			Debug.Log (instructions.Count);

		Debug.Log(string.Format("{0} instructions after symbol + gate filtering", instructions.Count));

		return instructions;
	}

	void AddNewInstruction(object sender, EventArgs e) {
		string instructionKey = ((NewInstructionEventArgs) e).InstructionKey;
		PDAStackOperation stackOperation = LearnableInstructions[GetLearnableInstructionKeyByName(instructionKey)];

		TransitionRelation.Add(new PDAInstruction( // instruction operated by input signal
			GetStates("Wait"), // in this state
			GetInputSymbolsByName(instructionKey), // when we get this message
			GenerateStackSymbolFromConditions(
				null, (g) => g == null, null, null, null, null // and this is the top of the stack
			),
			Regex.IsMatch(((List<string>) ((StackSymbolContent) stackOperation.Content).ActionOptions)[0], "grasp")
				? GetState("StartGrab")
				: GetState("ConfirmEvent"), // go to this state
			stackOperation)); // with this operation

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName(instructionKey),
			GenerateStackSymbolFromConditions(
				null, null, (r) => r == null, null,
				(a) => ((a.Count > 0) &&
				        (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0), null),
			GetState("StartGrab"),
			stackOperation));

		TransitionRelation.Add(new PDAInstruction(
			GetStates("RequestObject"),
			GetInputSymbolsByName(instructionKey),
			GenerateStackSymbolFromConditions(
				null, null, (r) => r != null, null,
				null, null),
			GetState("IndexByGesture"),
			new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));

		if (GetApplicableInstructions(GetState("IndexByGesture"), null,
			    GenerateStackSymbolFromConditions((o) => o != null, null, null,
				    null, null, null)).Count == 0) {
			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByGesture"),
				null,
				GenerateStackSymbolFromConditions(
					(o) => o != null, null, null,
					null, null, null
				),
				GetState("ComposeObjectAndAction"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
					new StackSymbolContent(null, null, null, new List<GameObject>(),
						new FunctionDelegate(GeneratePutAtRegionCommand), new List<string>()))));
		}

		if (GetApplicableInstructions(GetState("IndexByGesture"), null,
			    GenerateStackSymbolFromConditions(null, null, null,
				    (m) => m.Count > 1, null, null)).Count == 0) {
			TransitionRelation.Add(new PDAInstruction(
				GetStates("IndexByGesture"),
				null,
				GenerateStackSymbolFromConditions(
					null, null, null,
					(m) => m.Count > 1, null, null
				),
				GetState("DisambiguateObject"),
				new PDAStackOperation(PDAStackOperation.PDAStackOperationType.None, null)));
		}

		//TransitionRelation.Add(new PDAInstruction(
		//GetStates("IndexByGesture"),
		//null,
		//GenerateStackSymbolFromConditions(
		//    null, (g) => g != null, null,
		//    (m) => m.Count == 1,
		//    (a) => ((a.Count == 0) || ((a.Count > 0) &&
		//        (a.Where(aa => aa.Contains("{0}"))).ToList().Count > 0)),
		//    null),
		//GetState("ConfirmObject"),
		//new PDAStackOperation(PDAStackOperation.PDAStackOperationType.Push,
		//new StackSymbolContent(null, null, new FunctionDelegate(NullObject),
		//new List<GameObject>(), null, null))));

		// add new items to EpiSim
		epistemicModel.state.UpdateEpisimNewGesture(RemoveInputSymbolType(
				RemoveGestureTrigger(instructionKey, GetGestureTrigger(instructionKey)),
				GetInputSymbolType(instructionKey)),
			((List<string>) ((StackSymbolContent) stackOperation.Content).ActionOptions)[0].Replace(",", ", "));
	}

	void MoveToState(PDAState state) {
		Triple<PDASymbol, PDAState, PDASymbol> symbolStateTriple =
			new Triple<PDASymbol, PDAState, PDASymbol>(GetLastInputSymbol(), state, GetCurrentStackSymbol());

		if (CurrentState != null) {
			if (TransitionRelation.Where(i => (i.FromStates.Contains(CurrentState)) && (i.ToState == state))
				    .ToList().Count == 0) {
				Debug.Log(string.Format("No transition arc between state {0} and state {1}.  Aborting.",
					CurrentState.Name, state.Name));
				return;
			}

			if (state.Name == "BeginInteraction") {
				epistemicModel.state.InitiateEpisim();
				StateTransitionHistory.Push(symbolStateTriple);
				ContextualMemory.Push(symbolStateTriple);
			}
			else if (state.Name == "Wait") {
				if (CurrentState.Name != "TrackPointing") {
					StateTransitionHistory.Push(symbolStateTriple);
					ContextualMemory.Push(symbolStateTriple);
				}
			}
			else if (state.Name == "TrackPointing") {
				if (StateTransitionHistory.Peek().Item2.Name != "TrackPointing") {
					StateTransitionHistory.Push(symbolStateTriple);
					ContextualMemory.Push(symbolStateTriple);
				}
			}
			else if (state.Name == "EndState") {
				Debug.Log("Disengaging EpiSim");
				epistemicModel.state.DisengageEpisim();
				StateTransitionHistory.Push(symbolStateTriple);
				ContextualMemory.Push(symbolStateTriple);

				List<PDAInstruction> instructionsToRemove = GetApplicableInstructions(GetState("IndexByGesture"),
					null,
					GenerateStackSymbolFromConditions(null, null, null, null, null, null));

				foreach (List<PDASymbol> learnedInstructionKey in LearnableInstructions.Keys) {
					foreach (PDASymbol symbol in learnedInstructionKey) {
						instructionsToRemove = instructionsToRemove.Concat(GetApplicableInstructions(
							GetState("Wait"), symbol,
							GenerateStackSymbolFromConditions(null, null, null, null, null, null))).ToList();
						instructionsToRemove = instructionsToRemove.Concat(GetApplicableInstructions(
							GetState("RequestObject"), symbol,
							GenerateStackSymbolFromConditions(null, null, null, null, null, null))).ToList();
					}
				}

				foreach (PDAInstruction inst in instructionsToRemove) {
					Debug.Log("Removing " +
					          string.Format("{0},{1},{2},{3},{4}",
						          (inst.FromStates == null)
							          ? "Null"
							          : string.Format("[{0}]",
								          String.Join(", ",
									          ((List<PDAState>) inst.FromStates).Select(s => s.Name).ToArray())),
						          (inst.InputSymbols == null)
							          ? "Null"
							          : string.Format("[{0}]",
								          String.Join(", ",
									          ((List<PDASymbol>) inst.InputSymbols)
									          .Select(s => s.Content.ToString()).ToArray())),
						          StackSymbolToString(inst.StackSymbol),
						          inst.ToState.Name,
						          string.Format("[{0},{1}]",
							          inst.StackOperation.Type.ToString(),
							          (inst.StackOperation.Content == null)
								          ? "Null"
								          : (inst.StackOperation.Content.GetType() == typeof(StackSymbolContent))
									          ? StackSymbolToString(inst.StackOperation.Content)
									          : (inst.StackOperation.Content.GetType() == typeof(PDAState))
										          ? ((PDAState) inst.StackOperation.Content).Name
										          : string.Empty)));
					TransitionRelation.Remove(inst);
				}
			}
			else {
				StateTransitionHistory.Push(symbolStateTriple);
				ContextualMemory.Push(symbolStateTriple);
			}
		}
		else {
			StateTransitionHistory.Push(symbolStateTriple);
			ContextualMemory.Push(symbolStateTriple);
		}

		CurrentState = state;
		OnChangeState(this, new StateChangeEventArgs(CurrentState));

		if ((repeatAfterWait) && (repeatTimerTime > 0)) {
			repeatTimer.Interval = repeatTimerTime;
			repeatTimer.Enabled = true;
		}

		Debug.Log(string.Format("Entering state: {0}.  Stack symbol: {1}", CurrentState.Name,
			StackSymbolToString(GetCurrentStackSymbol())));
	}

	void ExecuteStateContent(object tempMessage = null) {
		Debug.Log(interactionController);
		Debug.Log(interactionController.GetType());
		Debug.Log(CurrentState.Name);
		Debug.Log(interactionController.GetType().GetMethod(CurrentState.Name));
		MethodInfo methodToCall = interactionController.GetType().GetMethod(CurrentState.Name);
		List<object> contentMessages = new List<object>();

		contentMessages.Add(tempMessage);

		if (methodToCall != null) {
			Debug.Log("MoveToState: invoke " + methodToCall.Name);
			object obj = methodToCall.Invoke(interactionController, new object[] {contentMessages.ToArray()});
		}
		else {
			Debug.Log(
				string.Format("No method of name {0} on object {1}", CurrentState.Name, interactionController));
		}
	}

	void UpdateEpistemicModel(string inputSymbol, string inputContent,
		EpistemicCertaintyOperation certaintyOperation) {
		if (CurrentState == GetState("StartState") || CurrentState == GetState("BeginInteraction")) {
			return;
		}

		// if input symbol is negack/NO, state is Suggest, take the action suggestion and reduce its Certainty
		if ((CurrentState == GetState("Suggest")) && (ActionSuggestions.Count > 0)) {
			if (GetInputSymbolsByName("G negack start", "S NO").Contains(GetInputSymbolByName(inputSymbol))) {
				UpdateEpistemicModel(RemoveInputSymbolContent(ActionSuggestions[0]), "",
					EpistemicCertaintyOperation.Decrease);
			}
			else if (GetInputSymbolsByName("G posack start", "S YES").Contains(GetInputSymbolByName(inputSymbol))) {
				UpdateEpistemicModel(RemoveInputSymbolContent(ActionSuggestions[0]), "",
					EpistemicCertaintyOperation.Increase);
			}
		}

		List<Concept> conceptsToUpdate = new List<Concept>();
		List<Relation> relationsToUpdate = new List<Relation>();

		List<Concepts> conceptsList = epistemicModel.state.GetAllConcepts();
		foreach (Concepts concepts in conceptsList) {
			if (concepts.GetConcepts().ContainsKey(ConceptMode.L)) {
				List<Concept> linguisticConcepts = concepts.GetConcepts()[ConceptMode.L];
				foreach (Concept concept in linguisticConcepts) {
//						Debug.Log (inputContent.ToLower ());
//						Debug.Log (concept.Name.ToLower ());
					if (inputContent.ToLower().Split(new char[] {',', ' '}).Contains(concept.Name.ToLower())) {
						Debug.Log(string.Format("{1} in {0}", inputContent.ToLower(), concept.Name.ToLower()));
						if (GetInputSymbolType(inputSymbol) == 'S') {
							Debug.Log(inputSymbol);
							concept.Certainty = (certaintyOperation == EpistemicCertaintyOperation.Increase)
								? 1.0
								: 0.0;
						}

						conceptsToUpdate.Add(concept);

						foreach (Concept relatedConcept in epistemicModel.state.GetRelated(concept)) {
							Relation relation = epistemicModel.state.GetRelation(concept, relatedConcept);
							double prevCertainty = relation.Certainty;
							double newCertainty = Math.Min(concept.Certainty, relatedConcept.Certainty);
							if (Math.Abs(prevCertainty - newCertainty) > 0.01) {
								relation.Certainty = newCertainty;
								relationsToUpdate.Add(relation);
							}
						}
					}
				}
			}
		}

		if (GetInputSymbolByName(inputSymbol) != null) {
			if (symbolConceptMap.ContainsKey(GetInputSymbolByName(inputSymbol))) {
				List<Concept> concepts = symbolConceptMap[GetInputSymbolByName(inputSymbol)];

				foreach (Concept concept in concepts) {
					if (GetInputSymbolType(inputSymbol) == 'G') {
						concept.Certainty = (certaintyOperation == EpistemicCertaintyOperation.Increase)
							? (interactionController.UseTeaching) ? (concept.Certainty < 0.5) ? 0.5 : 1.0 : 1.0
							: 0.0;
					}
					else if (GetInputSymbolType(inputSymbol) == 'S') {
						concept.Certainty =
							(certaintyOperation == EpistemicCertaintyOperation.Increase) ? 1.0 : 0.0;
					}

					Debug.Log(string.Format("Updating epistemic model: Concept {0} Certainty = {1}", concept.Name,
						concept.Certainty));
					conceptsToUpdate.Add(concept);

					foreach (Concept relatedConcept in epistemicModel.state.GetRelated(concept)) {
						Relation relation = epistemicModel.state.GetRelation(concept, relatedConcept);
						double prevCertainty = relation.Certainty;
						double newCertainty = Math.Min(concept.Certainty, relatedConcept.Certainty);
						if (Math.Abs(prevCertainty - newCertainty) > 0.01) {
							relation.Certainty = newCertainty;
							relationsToUpdate.Add(relation);
						}
					}
				}
			}
		}

		epistemicModel.state.UpdateEpisim(conceptsToUpdate.ToArray(), relationsToUpdate.ToArray());
	}

	object EpistemicallyCertain(object inputSignal) {
		if ((!interactionController.UseTeaching) || (!useEpistemicModel)) {
			return true;
		}

		if (inputSignal.GetType() != typeof(string)) {
			Debug.Log("EpistemicCertainty: inputSignal not of type string.  Aborting.");
			return false;
		}

		double aggregateCertainty = 1.0;
		int conceptCount = 0;

		PDASymbol inputSymbol = GetInputSymbolByName((string) inputSignal);
		if (inputSymbol != null) {
			if (symbolConceptMap.ContainsKey(inputSymbol)) {
				List<Concept> concepts = symbolConceptMap[inputSymbol];

				foreach (Concept concept in concepts) {
					Debug.Log(string.Format("{0}:{1}", concept.Name, concept.Certainty));
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
		}
	}

	void MoveToServo(object sender, ElapsedEventArgs e) {
		Debug.Log("MoveToServo");

		if (servoWaitTimerTime > 0) {
			servoWaitTimer.Interval = servoWaitTimerTime;
			servoWaitTimer.Enabled = false;
			forceMoveToState = GetState("Servo");
			forceChangeState = true;
			inServoLoop = true;
		}
	}

	void HandleStateChange(object sender, EventArgs e) {
		PDAState state = ((StateChangeEventArgs) e).State;

		if (state == GetState("StartServo")) {
			if (!inServoLoop) {
				servoWaitTimer.Interval = servoWaitTimerTime;
				servoWaitTimer.Enabled = true;
				Debug.Log(string.Format("Start Servo Loop Timer:{0}", servoLoopTimer.Interval));
			}
			else {
				servoLoopTimer.Interval = servoLoopTimerTime;
				servoLoopTimer.Enabled = true;
				Debug.Log(string.Format("Start Servo Wait Timer:{0}", servoWaitTimer.Interval));
			}
		}
		else if ((state == GetState("StopServo")) || (state == GetState("AbortAction"))) {
			servoWaitTimer.Interval = servoWaitTimerTime;
			servoWaitTimer.Enabled = false;

			servoLoopTimer.Interval = servoLoopTimerTime;
			servoLoopTimer.Enabled = false;

			inServoLoop = false;
		}
	}
}