using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Agent {
        namespace CharacterLogic {
        	public class PDAInstruction {
        		List<PDAState> fromStates;

        		public List<PDAState> FromStates {
        			get { return fromStates; }
        			set { fromStates = value; }
        		}

        		PDAState toState;

        		public PDAState ToState {
        			get { return toState; }
        			set { toState = value; }
        		}

        		List<PDASymbol> inputSymbols;

        		public List<PDASymbol> InputSymbols {
        			get { return inputSymbols; }
        			set { inputSymbols = value; }
        		}

        		PDASymbol stackSymbol;

        		public PDASymbol StackSymbol {
        			get { return stackSymbol; }
        			set { stackSymbol = value; }
        		}

        		PDAStackOperation stackOperation;

        		public PDAStackOperation StackOperation {
        			get { return stackOperation; }
        			set { stackOperation = value; }
        		}

        		internal PDAInstruction(List<PDAState> _fromStates, List<PDASymbol> _inputSymbols, PDASymbol _stackSymbol,
        			PDAState _toState, PDAStackOperation _stackOperation) {
        			fromStates = _fromStates;
        			inputSymbols = _inputSymbols;
        			stackSymbol = _stackSymbol;
        			toState = _toState;
        			stackOperation = _stackOperation;
        		}
        	}

        	public class PDAState {
        		string name;

        		public string Name {
        			get { return name; }
        			set { name = value; }
        		}

        		object content;

        		public object Content {
        			get { return content; }
        			set { content = value; }
        		}

        		internal PDAState(string _name, object _content) {
        			name = _name;
        			content = _content;
        		}
        	}

        	public class PDASymbol {
        		string name;

        		public string Name {
        			get { return name; }
        			set { name = value; }
        		}

        		object content;

        		public object Content {
        			get { return content; }
        			set { content = value; }
        		}

        		internal PDASymbol(object _content) {
        			content = _content;
        			name = content.ToString();
        		}

        		internal PDASymbol(string _name, object _content) {
        			name = _name;
        			content = _content;
        		}
        	}

        	public class PDAStackOperation {
        		public enum PDAStackOperationType {
        			None,
        			Push,
        			Pop,
        			Rewrite,
        			Flush
        		}

        		PDAStackOperationType type;

        		public PDAStackOperationType Type {
        			get { return type; }
        			set { type = value; }
        		}

        		object content;

        		public object Content {
        			get { return content; }
        			set { content = value; }
        		}

        		internal PDAStackOperation(PDAStackOperationType _type, object _content) {
        			type = _type;
        			content = _content;
        		}
        	}

        	public delegate object FunctionDelegate(object args);

        	public class DelegateFactory {
        		object function;

        		public object Function {
        			get { return function; }
        			set { function = value; }
        		}

        		internal DelegateFactory(object _function) {
        			function = _function;
        		}
        	}

        	public class TransitionGate {
        		PDAState rejectState;

        		public PDAState RejectState {
        			get { return rejectState; }
        			set { rejectState = value; }
        		}

        		PDAStackOperation rejectStackOperation;

        		public PDAStackOperation RejectStackOperation {
        			get { return rejectStackOperation; }
        			set { rejectStackOperation = value; }
        		}

        		FunctionDelegate condition;

        		public FunctionDelegate Condition {
        			get { return condition; }
        			set { condition = value; }
        		}

        		internal TransitionGate(FunctionDelegate _condition, PDAState _rejectState,
        			PDAStackOperation _rejectStackOperation) {
        			condition = _condition;
        			rejectState = _rejectState;
        			rejectStackOperation = _rejectStackOperation;
        		}
        	}

        	public class CharacterLogicAutomaton : MonoBehaviour {
        		// a nondeterministic pushdown automaton

        		List<PDAState> states;

        		public List<PDAState> States {
        			get { return states; }
        			set { states = value; }
        		}

        		List<PDASymbol> inputSymbols;

        		public List<PDASymbol> InputSymbols {
        			get { return inputSymbols; }
        			set { inputSymbols = value; }
        		}

        		List<PDASymbol> stackSymbols;

        		public List<PDASymbol> StackSymbols {
        			get { return stackSymbols; }
        			set { stackSymbols = value; }
        		}

        		List<PDAInstruction> transitionRelation;

        		public List<PDAInstruction> TransitionRelation {
        			get { return transitionRelation; }
        			set { transitionRelation = value; }
        		}

        		PDAState currentState;

        		public PDAState CurrentState {
        			get { return currentState; }
        			set { currentState = value; }
        		}

        		PDASymbol currentStackSymbol;

        		public PDASymbol CurrentStackSymbol {
        			get { return GetCurrentStackSymbol(); }
        		}

        		PDASymbol lastInputSymbol;

        		public PDASymbol LastInputSymbol {
        			get { return lastInputSymbol; }
        			set { lastInputSymbol = value; }
        		}

        		Stack<PDASymbol> stack;

        		public Stack<PDASymbol> Stack {
        			get { return stack; }
        			set { stack = value; }
        		}

        		Stack<Triple<PDASymbol, PDAState, PDASymbol>> stateTransitionHistory;

        		public Stack<Triple<PDASymbol, PDAState, PDASymbol>> StateTransitionHistory {
        			get { return stateTransitionHistory; }
        			set { stateTransitionHistory = value; }
        		}

        		Stack<Triple<PDASymbol, PDAState, PDASymbol>> contextualMemory;

        		public Stack<Triple<PDASymbol, PDAState, PDASymbol>> ContextualMemory {
        			get { return contextualMemory; }
        			set { contextualMemory = value; }
        		}

        		Dictionary<List<PDASymbol>, PDAStackOperation>
        			learnableInstructions; // TODO: not sure sure if it should map to PDAInstruction or another type

        		public Dictionary<List<PDASymbol>, PDAStackOperation> LearnableInstructions {
        			get { return learnableInstructions; }
        			set { learnableInstructions = value; }
        		}

        		public List<PDASymbol> GetLearnableInstructionKeyByName(string name) {
        			List<PDASymbol> instructionKey = new List<PDASymbol>();

        			foreach (List<PDASymbol> key in learnableInstructions.Keys) {
        				if (key.Contains(GetInputSymbolByName(name))) {
        					instructionKey = key;
        					break;
        				}
        			}

        			return instructionKey;
        		}

        		public enum AttentionStatus {
        			Inattentive,
        			Attentive
        		}

        		public event EventHandler LearnedNewInstruction;

        		public void OnLearnedNewInstruction(object sender, EventArgs e) {
        			if (LearnedNewInstruction != null) {
        				LearnedNewInstruction(this, e);
        			}
        		}

        		public virtual void Start() {
        			States = new List<PDAState>();
        			InputSymbols = new List<PDASymbol>();
        			StackSymbols = new List<PDASymbol>();
        			TransitionRelation = new List<PDAInstruction>();
        			LearnableInstructions = new Dictionary<List<PDASymbol>, PDAStackOperation>();

        			Stack = new Stack<PDASymbol>();
        			StateTransitionHistory = new Stack<Triple<PDASymbol, PDAState, PDASymbol>>();
        			ContextualMemory = new Stack<Triple<PDASymbol, PDAState, PDASymbol>>();
        		}

        		public virtual void Update() {
        		}

        		protected PDAState GetState(string name) {
        			if (States.Where(s => s.Name == name).FirstOrDefault() == null) {
        				Debug.Log(string.Format("No state: {0}", name));
        			}

        			return States.Where(s => s.Name == name).FirstOrDefault();
        		}

        		protected List<PDAState> GetStates(params string[] names) {
        			return States.Where(s => names.Contains(s.Name)).ToList();
        		}

        		protected PDASymbol GetInputSymbolByName(string name) {
        			if (InputSymbols.Where(s => s.Name == name).FirstOrDefault() == null) {
        				Debug.Log(string.Format("No input symbol: {0}", name));
        			}

        			return InputSymbols.Where(s => s.Name == name).FirstOrDefault();
        		}

        		protected List<PDASymbol> GetInputSymbolsByName(params string[] names) {
        			return InputSymbols.Where(s => names.Contains(s.Name)).ToList();
        		}

        		protected PDASymbol GetInputSymbolByContent(object content) {
        			return InputSymbols.Where(s => (s.Content.GetType() == content.GetType()) &&
        			                               (s.Content == content)).FirstOrDefault();
        		}

        		protected PDASymbol GetStackSymbolByName(string name) {
        			return StackSymbols.Where(s => s.Name == name).FirstOrDefault();
        		}

        		protected PDASymbol GetStackSymbolByContent(object content) {
        			return StackSymbols.Where(s => (s.Content.GetType() == content.GetType()) &&
        			                               (s.Content == content)).FirstOrDefault();
        		}

        		protected PDASymbol GetCurrentStackSymbol() {
        			if (Stack.Count > 0) {
        				return Stack.Peek();
        			}
        			else {
        				return null;
        			}
        		}

        		protected PDASymbol GetLastInputSymbol() {
        			return LastInputSymbol;
        		}
        	}
        }
    }
}