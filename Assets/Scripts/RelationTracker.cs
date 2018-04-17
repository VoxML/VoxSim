using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Global;
using Vox;

public class RelationTracker : MonoBehaviour {

	public Hashtable relations = new Hashtable();
	public List<String> relStrings = new List<String>();

	// Use this for initialization
	void Start () {
		UpdateRelationStrings ();
	}
	
	// Update is called once per frame
	void Update () {
		// for each relation
		// assume they still hold
		// unless break condition is met
		Dictionary<List<GameObject>,string> toRemove = new Dictionary<List<GameObject>,string>();

		foreach (DictionaryEntry pair in relations)
		{
			if (!IsSatisfied((pair.Value as string),(pair.Key as List<GameObject>))) {
				toRemove.Add (pair.Key as List<GameObject>, pair.Value as string);
			}
		}

		foreach (object key in toRemove) {
			RemoveRelation (key as List<GameObject>, toRemove[key as List<GameObject>]);
		}
	}

	public void AddNewRelation (List<GameObject> objs, string relation, bool recurse = true) {
		VoxML voxml = null;
		try {
			using (StreamReader sr = new StreamReader (
				string.Format ("{0}/{1}", Data.voxmlDataPath, string.Format ("relations/{0}.xml", relation)))) {
				voxml = VoxML.LoadFromText (sr.ReadToEnd ());
			}
		}
		catch (Exception e) {
			Debug.Log (e.Message);
		}

		foreach (List<GameObject> key in relations.Keys) {
			if (key.SequenceEqual (objs)) {
				if (!relations [key].ToString ().Contains (relation)) {
					Debug.Log (string.Format("Adding {0} {1} {2}",relation,objs[0],objs[1]));
					relations [key] += string.Format (",{0}", relation);

					if (recurse) {
						if ((voxml != null) && (voxml.Type.Corresps.Where (c => c.Value == "reflexive").ToList ().Count > 0)) {
							AddNewRelation (Enumerable.Reverse (objs).ToList (), relation, false);
						}
					}
				}
				UpdateRelationStrings ();
				return;
			}
		}

		foreach (List<GameObject> key in relations.Keys) {
			if (key.SequenceEqual (objs.Reverse<GameObject>().ToList())) {
				if (relations [key].ToString ().Contains (relation)) {
					return;
				}
			}
		}

		Debug.Log (string.Format("Adding {0} {1} {2}",relation,objs[0],objs[1]));
		relations.Add(objs,relation);	// add key-val pair or modify value if key already exists

		if (recurse) {
			if ((voxml != null) && (voxml.Type.Corresps.Where (c => c.Value == "reflexive").ToList ().Count > 0)) {
				AddNewRelation (Enumerable.Reverse (objs).ToList (), relation, false);
			}
		}

		UpdateRelationStrings();
	}

	public void RemoveRelation (List<GameObject> objs, string relation, bool recurse = true) {
		VoxML voxml = null;
		try {
			using (StreamReader sr = new StreamReader (
				string.Format ("{0}/{1}", Data.voxmlDataPath, string.Format ("relations/{0}.xml", relation)))) {
				voxml = VoxML.LoadFromText (sr.ReadToEnd ());
			}
		}
		catch (Exception e) {
			Debug.Log (e.Message);
		}
			
		foreach (List<GameObject> key in relations.Keys) {
			if (key.SequenceEqual (objs)) {
				if (relations [key].ToString ().Contains (relation)) {
					Debug.Log (string.Format("Removing {0} {1} {2}",relation,objs[0],objs[1]));
					if (relations [key].ToString ().Contains (",")) {
						Debug.Log (relations [key]);
						relations [key] = Regex.Replace (relations [key].ToString (), string.Format ("{0},?", relation), "");
						if (relations [key].ToString ().EndsWith (",")) {
							relations [key] = relations [key].ToString ().Trim (new char[]{ ',' });
						}
						Debug.Log (relations [key]);

						if (recurse) {
							if ((voxml != null) && (voxml.Type.Corresps.Where (c => c.Value == "reflexive").ToList ().Count > 0)) {
								RemoveRelation (Enumerable.Reverse (objs).ToList (), relation, false);
							}
						}

						UpdateRelationStrings ();
						return;
					}
					else {
						relations.Remove(key);

						if (recurse) {
							if ((voxml != null) && (voxml.Type.Corresps.Where (c => c.Value == "reflexive").ToList ().Count > 0)) {
								RemoveRelation (Enumerable.Reverse (objs).ToList (), relation, false);
							}
						}

						UpdateRelationStrings();
						return;
					}
				}
			}
		}
	}

	void UpdateRelationStrings() {
		relStrings.Clear ();
		foreach (DictionaryEntry entry in relations) {
			String str = (String)entry.Value;
			foreach (GameObject go in (List<GameObject>)entry.Key) {
				str = str + " " + go.name;
			}
//			Debug.Log (str);
			relStrings.Add (str);
		}
	}

	bool IsSatisfied(string relation, List<GameObject> objs) {
		bool satisfied = true;

		if (relation == "support") {	// x support y - binary relation
			if (Vector3.Dot (objs [0].transform.up, Vector3.up) <= 0.0f) {	// --> get support axis info from habitat
				if (Vector3.Dot (objs [1].transform.up, Vector3.up) < 0.5f) {	// --> get support axis info from habitat
					// break relation
					objs [1].transform.parent = null;
					objs [1].GetComponent<Voxeme> ().enabled = true;
					objs [1].GetComponent<Voxeme> ().supportingSurface = null;
					objs [1].GetComponent<Rigging> ().ActivatePhysics (true);
					satisfied = false;
				}
			}
		}

		return satisfied;
	}
}
