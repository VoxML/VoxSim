﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Global {
	/// <summary>
	/// Triple class
	/// </summary>
	public class Triple<T1, T2, T3> : IEquatable<System.Object>{
		public T1 Item1{
			get;
			set;
		}
		
		public T2 Item2{
			get;
			set;
		}
		
		public T3 Item3{
			get;
			set;
		}
		
		public Triple(T1 Item1, T2 Item2, T3 Item3){
			this.Item1 = Item1;
			this.Item2 = Item2;
			this.Item3 = Item3;
		}
		
		public override bool Equals(object obj) {
			if ( obj == null || (obj as Triple<T1, T2, T3>) == null ) //if the object is null or the cast fails
				return false;
			else {
				Triple<T1,T2,T3> tuple = (Triple<T1, T2, T3>) obj;
				return Item1.Equals(tuple.Item1) && Item2.Equals(tuple.Item2) && Item3.Equals(tuple.Item3);
			}
		}
		
		public override int GetHashCode() {
			return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
		}
		
		public static bool operator == (Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
			return tuple1.Equals(tuple2);
		}
		
		public static bool operator != (Triple<T1, T2, T3> tuple1, Triple<T1, T2, T3> tuple2) {
			return !tuple1.Equals(tuple2);
		}
	}

	/// <summary>
	/// Helper class
	/// </summary>
	public static class Helper {
		public static Regex v = new Regex (@"<.*>");

		public static Hashtable ParsePredicate(String predicate) {
			Hashtable predArgs = new Hashtable ();
			
			Queue<String> split = new Queue<String>(predicate.Split (new char[] {'('},2,StringSplitOptions.None));
			String pred = split.ElementAt(0);
			String args = split.ElementAt(1);
			args = args.Remove(args.Length - 1);
			predArgs.Add (pred, args);
			
			return predArgs;
		}

		public static String GetTopPredicate(String formula) {
			Queue<String> split = new Queue<String>(formula.Split (new char[] {'('},2,StringSplitOptions.None));
			return split.ElementAt (0);
		}
		
		public static void PrintKeysAndValues(Hashtable ht)  {
			foreach (DictionaryEntry entry in ht)
				Debug.Log(entry.Key + " : " + entry.Value);
		}
		
		public static String ConvertVectorToParsable(Vector3 vector) {
			return ("<"+vector.x.ToString ()+"; "+
			       	vector.y.ToString ()+"; "+
			        vector.z.ToString ()+">");
		}
		
		public static Vector3 ConvertParsableToVector(String parsable) {
			List<String> components = new List<String> ((((String)parsable).Replace("<","").Replace(">","")).Split (new char[] {';'}));
			return new Vector3(System.Convert.ToSingle(components[0]),
			                   System.Convert.ToSingle(components[1]),
			                   System.Convert.ToSingle(components[2]));
		}

		public static Triple<String,String,String> MakeRDFTriples(String formula) {		// fix for multiple RDF triples
			Triple<String,String,String> triple = new Triple<String,String,String> ("","","");
			Debug.Log ("MakeRDFTriple: " + formula);
			String[] components = formula.Replace ('(', '/').Replace (')', '/').Replace (',','/').Split ('/');
			if (components.Length <= 3) {
				triple.Item1 = "NULL";
			}

			foreach (String s in components) {
				if (s.Length > 0) {
					//Debug.Log (s);
					GameObject obj = GameObject.Find (s);
					if (obj != null) {
						if (triple.Item1 == "") {
							triple.Item1 = s;
						} else if (triple.Item3 == "") {
							triple.Item3 = s;
						}
					} else {
						triple.Item2 = triple.Item2 + s + "_";
					}
				}
			}
			triple.Item2 = triple.Item2.Remove (triple.Item2.Length - 1, 1);
			//Debug.Log (triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
			return triple;
		}

		public static void PrintRDFTriples(List<Triple<String,String,String>> triples) {
			foreach (Triple<String,String,String> triple in triples) {
				Debug.Log (triple.Item1 + " " + triple.Item2 + " " + triple.Item3);
			}
		}
	}
}
