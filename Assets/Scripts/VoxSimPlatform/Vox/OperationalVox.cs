using UnityEngine;
using System;
using System.Collections.Generic;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        /// <summary>
        /// OperationalVox
        /// </summary>
        public class OperationalVox {
        	/// <summary>
        	/// LEX
        	/// </summary>
        	public class OpLex {
        		public string Pred = "";
        		public string Type = "";
        	}

        	/// <summary>
        	/// TYPE
        	/// </summary>
        	public class OpTypeComponent {
        		public string Value { get; set; }
        	}

        	public class OpTypeArg {
        		public string Value { get; set; }
        	}

        	public class OpTypeSubevent {
        		public string Value { get; set; }
        	}

        	public class OpType {
        		public Triple<string, GameObject, int> Head;

        		// string: component name
        		// GameObject: component
        		// int: optional reentrancy index (-1 if none)
        		public List<Triple<string, GameObject, int>> Components = new List<Triple<string, GameObject, int>>();

        		public Triple<string, GameObject, int> Concavity = new Triple<string, GameObject, int>(String.Empty, null, -1);
        		public List<string> RotatSym = new List<string>();
        		public List<string> ReflSym = new List<string>();

        		public List<OpTypeArg> Args = new List<OpTypeArg>();

        		public List<OpTypeSubevent> Body = new List<OpTypeSubevent>();

        		public string Class = "";
        		public string Value = "";
        		public string Constr = "";
        	}

        	/// <summary>
        	/// HABITAT
        	/// </summary>
        	public class OpHabitat {
        		// int: habitat index
        		// List<string>: habitat formulae
        		public Dictionary<int, List<string>> IntrinsicHabitats = new Dictionary<int, List<string>>();
        		public Dictionary<int, List<string>> ExtrinsicHabitats = new Dictionary<int, List<string>>();
        	}

        	/// <summary>
        	/// AFFORD_STR
        	/// </summary>
        	public class OpAfford_Str {
        		// int: habitat index (0 for unindexed H)
        		// List<Pair<string, Pair<string, string>>>>: List of optional condition formulas, with [EVENT]RESULT pairs (RESULT may be empty)
        		// List Item1: condition on habitat
        		// List Item2: Event/Result pair
        		// List Item2 Item1: Event
        		// List Item2 Item2: Result
        		public Dictionary<int, List<Pair<string, Pair<string, string>>>> Affordances =
        			new Dictionary<int, List<Pair<string, Pair<string, string>>>>();
        	}

        	/// <summary>
        	/// EMBODIMENT
        	/// </summary>
        	public class OpEmbodiment {
        		public string Scale = "";
        		public bool Movable = false;
        	}

        	public VoxEntity.EntityType VoxemeType { get; set; }
        	public OpLex Lex = new OpLex();
        	public OpType Type = new OpType();
        	public OpHabitat Habitat = new OpHabitat();
        	public OpAfford_Str Affordance = new OpAfford_Str();
        	public OpEmbodiment Embodiment = new OpEmbodiment();
        }
    }
}