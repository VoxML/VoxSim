using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning;
using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace Core {
        public class ObjectSelector : MonoBehaviour {
        	public List<Voxeme> allVoxemes = new List<Voxeme>();

        	public List<GameObject> selectedObjects = new List<GameObject>();

        	//public List<GameObject> toDisable = new List<GameObject> ();
        	public List<GameObject> disabledObjects = new List<GameObject>();

        	//VoxemeInspector inspector;
        	RelationTracker relationTracker;
        	ModalWindowManager windowManager;

        	public int fontSize = 12;

        	float fontSizeModifier;

        	public float FontSizeModifier {
        		get { return fontSizeModifier; }
        		set { fontSizeModifier = value; }
        	}

        	bool editableVoxemes;

        	// Use this for initialization
        	void Start() {
        		//inspector = gameObject.GetComponent ("VoxemeInspector") as VoxemeInspector;
        		relationTracker = GameObject.Find("BehaviorController").GetComponent<RelationTracker>();
        		windowManager = gameObject.GetComponent<ModalWindowManager>();

        		editableVoxemes = (PlayerPrefs.GetInt("Make Voxemes Editable") == 1);
        	}

        	// Update is called once per frame
        	void Update() {
        		if (Input.GetMouseButtonDown(0)) {
        			if (Helper.PointOutsideMaskedAreas(
        				new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        				windowManager.windowManager.Values.Select(v => v.windowRect).ToArray())) {
        				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        				RaycastHit hit;
        				// Casts the ray and get the first game object hit
        				Physics.Raycast(ray, out hit);
        				if (hit.collider == null) {
        					//if (!Helper.PointInRect (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y), inspector.InspectorRect)) {
        					//inspector.InspectorObject = null;
        					selectedObjects.Clear();
        					//}

        //					if (!Helper.PointInRect (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y), inspector.InspectorRect)) {
        //						inspector.DrawInspector = false;
        //					}
        				}
        				else {
        					selectedObjects.Clear();
        					//selectedObjects.Add (hit.transform.root.gameObject);
        					//inspector.InspectorObject = hit.transform.root.gameObject;
        					//Debug.Log (selectedObjects.Count);
        				}

        //				if (!Helper.PointInRect (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y), inspector.InspectorRect)) {
        //					inspector.DrawInspector = false;
        //				}
        			}
        		}
        		else if (Input.GetMouseButtonDown(1)) {
        			if (Helper.PointOutsideMaskedAreas(
        				new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
        				windowManager.windowManager.Values.Select(v => v.windowRect).ToArray())) {
        				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        				RaycastHit hit;
        				// Casts the ray and get the first game object hit
        				Physics.Raycast(ray, out hit);
        //				if (hit.collider == null) {
        //					if (!Helper.PointInRect (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y), inspector.InspectorRect)) {
        //						inspector.DrawInspector = false;
        //					}
        //				}
        //				else {
        //					inspector.DrawInspector = true;
        //					inspector.ScrollPosition = new Vector2 (0, 0);
        //					inspector.InspectorChoice = -1;
        //					inspector.InspectorObject = Helper.GetMostImmediateParentVoxeme (hit.transform.gameObject);
        //					//inspector.InspectorObject = hit.transform.root.gameObject;
        //					inspector.InspectorPosition = new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        //				}

        				if (hit.collider != null) {
        					string objName = hit.collider.gameObject.transform.root.gameObject.name;
        					if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath,
        						string.Format("objects/{0}.xml", objName)))) {
        						using (StreamReader sr = new StreamReader(
        							string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("objects/{0}.xml", objName)))) {
        							String ml = sr.ReadToEnd();
        							if (ml != null) {
        //								float textSize = GUI.skin.label.CalcSize (new GUIContent (objName)).x;
        //								float padSize = GUI.skin.label.CalcSize (new GUIContent (" ")).x;
        //								int padLength = (int)(((inspectorWidth - 85) - textSize) / (int)padSize);
        //								if (GUILayout.Button (objName.PadRight (padLength + objName.Length - 3), GUILayout.Width (inspectorWidth - 85))) {
        //									if (ml != null) {
        								if (windowManager.windowManager.Values.Where(v =>
        									    v.GetType() == typeof(VoxemeInspectorModalWindow) &&
        									    ((VoxemeInspectorModalWindow) v).InspectorObject ==
        									    hit.collider.gameObject.transform.root.gameObject).ToList().Count == 0) {
        									VoxemeInspectorModalWindow newInspector =
        										gameObject.AddComponent<VoxemeInspectorModalWindow>();
        									//LoadMarkup (ml.text);
        									//newInspector.DrawInspector = true;
        									newInspector.InspectorPosition = new Vector2(Input.mousePosition.x,
        										Screen.height - Input.mousePosition.y);
        									newInspector.windowRect = new Rect(newInspector.InspectorPosition.x,
        										newInspector.InspectorPosition.y, newInspector.inspectorWidth,
        										newInspector.inspectorHeight);
        									newInspector.InspectorVoxeme = "objects/" + objName;
        									newInspector.InspectorObject = hit.collider.gameObject.transform.root.gameObject;
        									newInspector.Render = true;
        								}

        //									}
        //									else {
        //									}
        							}
        						}
        					}
        					else {
        						if (editableVoxemes) {
        							VoxemeInspectorModalWindow newInspector =
        								gameObject.AddComponent<VoxemeInspectorModalWindow>();
        							newInspector.InspectorPosition = new Vector2(Input.mousePosition.x,
        								Screen.height - Input.mousePosition.y);
        							newInspector.windowRect = new Rect(newInspector.InspectorPosition.x,
        								newInspector.InspectorPosition.y, newInspector.inspectorWidth,
        								newInspector.inspectorHeight);
        							newInspector.InspectorVoxeme = "objects/" + objName;
        							newInspector.InspectorObject = hit.collider.gameObject.transform.root.gameObject;
        							newInspector.Render = true;
        						}
        					}
        				}
        			}
        		}
        	}

        	public List<String> ExtractCommonFeatureLabels(object[] objects) {
        		List<String> common = new List<string>();

        		// are all objects nominally predicated the same?
        		// are any attributes shared by all objects?

        		if (objects[0].GetType() == typeof(Voxeme)) {
        			List<String> predicates = objects.Cast<Voxeme>().Select(o => o.voxml.Lex.Pred).ToList<String>();

        			if (predicates.All(p => p == predicates[0])) {
        				common.Add(predicates[0]);
        			}

        			foreach (String c in common) {
        				Debug.Log(c);
        			}
        		}

        		return common;
        	}

        	public void ResetScene() {
        		relationTracker.relations.Clear();
        		PhysicsHelper.ResolveAllPhysicsDiscrepancies(false);
        		foreach (Voxeme voxeme in allVoxemes) {
        			voxeme.Reset();
        		}
        	}

        	public void InitDisabledObjects() {
        		for (int i = 0; i < disabledObjects.Count; i++) {
        			disabledObjects[i] = Helper.GetMostImmediateParentVoxeme(disabledObjects[i]);
        			disabledObjects[i].SetActive(false);
        		}
        	}
        }
    }
}