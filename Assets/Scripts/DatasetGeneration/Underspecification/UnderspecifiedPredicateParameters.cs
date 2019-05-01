using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

public class ParamsEventArgs : EventArgs {
	public KeyValuePair<string, string> KeyValue { get; set; }

	public ParamsEventArgs(string key, string value) {
		this.KeyValue = new KeyValuePair<string, string>(key, value);
	}
}

public class PredicateParametersJSON {
	public string MotionSpeed;
	public string MotionManner;
	public string TranslocSpeed;
	public string TranslocDir;
	public string RotSpeed;
	public string RotAngle;
	public string RotAxis;
	public string RotDir;
	public string SymmetryAxis;
	public string PlacementOrder;
	public string RelOrientation;
	public string RelOffset;

	public PredicateParametersJSON(Dictionary<string, string> dict) {
		foreach (string key in dict.Keys) {
			switch (key) {
				case "MotionSpeed":
					MotionSpeed = dict[key];
					break;

				case "MotionManner":
					MotionManner = dict[key];
					break;

				case "TranslocSpeed":
					TranslocSpeed = dict[key];
					break;

				case "TranslocDir":
					TranslocDir = dict[key];
					break;

				case "RotSpeed":
					RotSpeed = dict[key];
					break;

				case "RotAngle":
					RotAngle = dict[key];
					break;

				case "RotAxis":
					RotAxis = dict[key];
					break;

				case "RotDir":
					RotDir = dict[key];
					break;

				case "SymmetryAxis":
					SymmetryAxis = dict[key];
					break;

				case "PlacementOrder":
					PlacementOrder = dict[key];
					break;

				case "RelOrientation":
					RelOrientation = dict[key];
					break;

				case "RelOffset":
					RelOffset = dict[key];
					break;

				default:
					break;
			}
		}
	}
}

public static class UnderspecifiedPredicateParameters {
	static string[] parameterNames = new string[] {
		"MotionSpeed",
		"MotionManner",
		"TranslocSpeed",
		"TranslocDir",
		"RotSpeed",
		"RotAngle",
		"RotAxis",
		"RotDir",
		"SymmetryAxis",
		"PlacementOrder",
		"RelOrientation",
		"RelOffset"
	};

	static Dictionary<string, List<string>> underspecifiedParams = new Dictionary<string, List<string>> {
		{
			"grasp", new List<string>() {
				"MotionSpeed"
			}
		}, {
			"hold", new List<string>() {
				"MotionManner"
			}
		}, {
			"touch", new List<string>() {
				"MotionManner"
			}
		}, {
			"move", new List<string>() {
				"MotionManner"
			}
		}, {
			"turn", new List<string>() {
				"RotSpeed",
				"RotAxis",
				"RotAngle",
				"RotDir",
				"MotionManner"
			}
		}, {
			"roll", new List<string>() {
				"TranslocDir"
			}
		}, {
			"slide", new List<string>() {
				"TranslocSpeed",
				"TranslocDir"
			}
		}, {
			"spin", new List<string>() {
				"RotSpeed",
				"RotAxis",
				"RotAngle",
				"RotDir",
				"MotionManner"
			}
		}, {
			"lift", new List<string>() {
				"TranslocSpeed",
				"TranslocDir"
			}
		}, {
			"stack", new List<string>() {
				"PlacementOrder"
			}
		}, {
			"put", new List<string>() {
				"TranslocSpeed",
				"TranslocDir",
				"RelOrientation"
			}
		}, {
			"lean", new List<string>() {
				"RotAngle"
			}
		}, {
			"flip", new List<string>() {
				"RotAxis",
				"SymmetryAxis"
			}
		}, {
			"close", new List<string>() {
				"MotionSpeed"
			}
		}, {
			"open", new List<string>() {
				"MotionSpeed",
				"TranslocDir",
				"RotAngle"
			}
		}
	};

	// key: predicate:
	// value:
	//	pair of:
	//		list of:
	//			alternate predicate, adjunct (optional), arity
	//		whether or not to always choose an alterate predicate
	static Dictionary<string, Pair<List<Triple<string, string, int>>, bool>> underspecifiedMannerPredicates =
		new Dictionary<string, Pair<List<Triple<string, string, int>>, bool>> {
			{
				"hold", new Pair<List<Triple<string, string, int>>, bool>(
					new List<Triple<string, string, int>>() {
						new Triple<string, string, int>("grasp", "", 1)
					},
					false)
			}, {
				"touch", new Pair<List<Triple<string, string, int>>, bool>(
					new List<Triple<string, string, int>>() {
						new Triple<string, string, int>("grasp", "", 1),
						new Triple<string, string, int>("hold", "", 1)
					},
					true)
			}, {
				"move", new Pair<List<Triple<string, string, int>>, bool>(
					new List<Triple<string, string, int>>() {
						new Triple<string, string, int>("turn", "", 1),
						//new Triple<string, string, int>("roll", "", 1),
						new Triple<string, string, int>("slide", "", 1),
						new Triple<string, string, int>("spin", "", 1),
						new Triple<string, string, int>("lift", "", 1),
						new Triple<string, string, int>("stack", "", 2), // binary w/ no adjunct
						new Triple<string, string, int>("put", "on", 2), // binary w/ adjunct: insert in order
						new Triple<string, string, int>("put", "in", 2),
						new Triple<string, string, int>("put", "near", 2),
						new Triple<string, string, int>("lean", "on", 2),
						new Triple<string, string, int>("lean", "against", 2),
						new Triple<string, string, int>("flip", "edge", 1), // unary w/ adjunct: insert same object
						new Triple<string, string, int>("flip", "center", 1)
					},
					true)
			}, {
				"turn", new Pair<List<Triple<string, string, int>>, bool>(
					new List<Triple<string, string, int>>() {
						new Triple<string, string, int>("roll", "", 1),
						new Triple<string, string, int>("spin", "", 1),
						new Triple<string, string, int>("lean", "on", 2),
						new Triple<string, string, int>("lean", "against", 2),
						new Triple<string, string, int>("flip", "edge", 1),
						new Triple<string, string, int>("flip", "center", 1)
					},
					false)
			}, {
				"spin", new Pair<List<Triple<string, string, int>>, bool>(
					new List<Triple<string, string, int>>() {
						new Triple<string, string, int>("roll", "", 1)
					},
					false)
			}
		};


	public static Dictionary<string, string> InitPredicateParametersCollection() {
		Dictionary<string, string> collection = new Dictionary<string, string>();
		foreach (string parameterName in parameterNames) {
			collection.Add(parameterName, "");
		}

		return collection;
	}

	public static bool IsSpecificationOf(string hyponym, string hypernym) {
		bool r = false;

		if (underspecifiedMannerPredicates.ContainsKey(hypernym)) {
			r = (underspecifiedMannerPredicates[hypernym].Item1.FindIndex(t => t.Item1 == hyponym) != -1);
		}

		return r;
	}

	public static string FilterSpecifiedManner(string parse) {
		Hashtable predArgs = Helper.ParsePredicate(parse);
		string pred = Helper.GetTopPredicate(parse);

		string outValue = parse;

		if (!underspecifiedMannerPredicates.ContainsKey(pred)) {
			// manner already fully specified
			return outValue;
		}

		bool alwaysAlternate = underspecifiedMannerPredicates[pred].Item2;
		List<Triple<string, string, int>> alternateList = underspecifiedMannerPredicates[pred].Item1;

		if (alwaysAlternate) {
			outValue = SwitchPredicate(alternateList, predArgs, pred);
		}
		else {
			// if # of available alterante predicates > # other underspecified parameters
			//	2/3 chance of changing predicate
			// else
			//	1/3 chance of changing predicate

			bool switchPred = false;

			if (alternateList.Count > underspecifiedParams[pred].Count) {
				if (RandomHelper.RandomFloat(0.0f, 1.0f,
					    (int) (RandomHelper.RangeFlags.MinInclusive | RandomHelper.RangeFlags.MaxInclusive)) > 0.333f) {
					switchPred = true;
				}
			}
			else {
				if (RandomHelper.RandomFloat(0.0f, 1.0f,
					    (int) (RandomHelper.RangeFlags.MinInclusive | RandomHelper.RangeFlags.MaxInclusive)) > 0.667f) {
					switchPred = true;
				}
			}

			if (switchPred) {
				outValue = SwitchPredicate(alternateList, predArgs, pred);
			}
		}

		return outValue;
	}

	public static string SwitchPredicate(List<Triple<string, string, int>> alternateList, Hashtable predArgs,
		string pred) {
		UnderspecificationVideoAutoCapture vidCap = Camera.main.GetComponent<UnderspecificationVideoAutoCapture>();
		ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		string outValue = string.Empty;

		Triple<string, string, int> alternate = alternateList[RandomHelper.RandomInt(0, alternateList.Count,
			(int) RandomHelper.RangeFlags.MinInclusive)];

		string altPred = alternate.Item1;
		string adjunct = alternate.Item2;
		int arity = alternate.Item3;

		if (arity == 1) {
			string directObjName = predArgs[pred].ToString();
			if (adjunct == string.Empty) {
				outValue = string.Format("{0}({1})", altPred, directObjName);
			}
			else {
				outValue = string.Format("{0}({1},{2}({1}))", altPred, directObjName, adjunct);
			}
		}
		else if (arity == 2) {
			string directObjName = predArgs[pred].ToString();
			GameObject directObj = GameObject.Find(directObjName) != null
				? GameObject.Find(directObjName)
				: objSelector.disabledObjects.Find(o => o.name == directObjName);
			GameObject obj = RandomHelper.RandomVoxeme(vidCap.availableObjs, new List<GameObject>() {directObj});
//			while (Helper.GetMostImmediateParentVoxeme (obj).gameObject.transform.parent != null) {
//				obj = RandomHelper.RandomVoxeme (vidCap.availableObjs, new List<GameObject>(){ GameObject.Find (directObj) });
//				Debug.Log (obj.name);
//			}

			string indirectObjName = obj.name;
			if (adjunct == string.Empty) {
				outValue = string.Format("{0}({1},{2})", altPred, directObjName, indirectObjName);
			}
			else {
				outValue = string.Format("{0}({1},{2}({3}))", altPred, directObjName, adjunct, indirectObjName);
			}
		}

		return outValue;
	}
}