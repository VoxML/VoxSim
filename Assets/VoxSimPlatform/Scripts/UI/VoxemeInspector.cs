using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;
using VoxSimPlatform.UI.ModalWindow;

namespace UI {
    public class VoxemeInspector : MonoBehaviour {
    	public int inspectorWidth;
    	public int inspectorHeight;
    	public int inspectorMargin;

    	string inspectorTitle = "";

    	Vector2 scrollPosition;

    	public Vector2 ScrollPosition {
    		get { return scrollPosition; }
    		set { scrollPosition = value; }
    	}

    	Rect inspectorRect = new Rect(0, 0, 0, 0);

    	public Rect InspectorRect {
    		get { return inspectorRect; }
    		set { inspectorRect = value; }
    	}

    	Vector2 inspectorPosition;

    	public Vector2 InspectorPosition {
    		get { return inspectorPosition; }
    		set { inspectorPosition = value; }
    	}

    	float inspectorPositionAdjX;
    	float inspectorPositionAdjY;
    	GUIStyle inspectorStyle;
    	string[] inspectorMenuItems = {"Reify As...", "View/Edit Markup", "Modify", "Delete"};

    	int inspectorChoice = -1;

    	public int InspectorChoice {
    		get { return inspectorChoice; }
    		set { inspectorChoice = value; }
    	}

    	GameObject inspectorObject;

    	public GameObject InspectorObject {
    		get { return inspectorObject; }
    		set { inspectorObject = value; }
    	}

    	string newName = "";
    	string xScale = "1", yScale = "1", zScale = "1";

    	bool drawInspector;

    	public bool DrawInspector {
    		get { return drawInspector; }
    		set {
    			drawInspector = value;
    			if (!drawInspector) {
    				inspectorRect = new Rect(0, 0, 0, 0);
    				scrollPosition = new Vector2(0, 0);
    			}
    		}
    	}

    	GUIStyle listStyle = new GUIStyle();
    	Texture2D tex;
    	Color[] colors;

    	// Markup vars
    	// ENTITY
    	VoxEntity.EntityType mlEntityType = VoxEntity.EntityType.None;

    	// LEX
    	string mlPred = "";

    	string[] mlTypeOptions = new string[] {"physobj", "human", "artifact"};
    	List<int> mlTypeSelectVisible = new List<int>(new int[] {-1});
    	List<int> mlTypeSelected = new List<int>(new int[] {-1});
    	int mlAddType = -1;
    	List<int> mlRemoveType = new List<int>(new int[] {-1});
    	int mlTypeCount = 1;
    	List<string> mlTypes = new List<string>(new string[] {""});

    	// TYPE
    	string[] mlHeadOptions = new string[]
    		{"cylindroid", "ellipsoid", "rectangular_prism", "toroid", "pyramidoid", "sheet"};

    	int mlHeadSelectVisible = -1;
    	int mlHeadSelected = -1;
    	string mlHead = "";

    	int mlAddComponent = -1;
    	List<int> mlRemoveComponent = new List<int>();
    	int mlComponentCount = 0;
    	List<string> mlComponents = new List<string>();

    	string[] mlConcavityOptions = new string[] {"Concave", "Flat", "Convex"};
    	int mlConcavitySelectVisible = -1;
    	int mlConcavitySelected = -1;
    	string mlConcavity = "";

    	bool mlRotatSymX = false;
    	bool mlRotatSymY = false;
    	bool mlRotatSymZ = false;
    	bool mlReflSymXY = false;
    	bool mlReflSymXZ = false;
    	bool mlReflSymYZ = false;

    	int mlArgCount = 0;
    	List<string> mlArgs = new List<string>();

    	int mlSubeventCount = 0;
    	List<string> mlSubevents = new List<string>();

    	// HABITAT
    	int mlAddIntrHabitat = -1;
    	List<int> mlRemoveIntrHabitat = new List<int>();
    	int mlIntrHabitatCount = 0;
    	List<string> mlIntrHabitats = new List<string>();

    	int mlAddExtrHabitat = -1;
    	List<int> mlRemoveExtrHabitat = new List<int>();
    	int mlExtrHabitatCount = 0;
    	List<string> mlExtrHabitats = new List<string>();

    	// AFFORD_STR
    	int mlAddAffordance = -1;
    	List<int> mlRemoveAffordance = new List<int>();
    	int mlAffordanceCount = 0;
    	List<string> mlAffordances = new List<string>();

    	// EMBODIMENT
    	string[] mlScaleOptions = new string[] {"<agent", "agent", ">agent"};
    	int mlScaleSelectVisible = -1;
    	int mlScaleSelected = -1;
    	string mlScale = "";

    	bool mlMovable = false;

    	bool markupCleared = false;
    	VoxML loadedObject = new VoxML();

    	// Use this for initialization
    	void Start() {
    		colors = new Color[] {Color.white, Color.white, Color.white, Color.white};
    		tex = new Texture2D(2, 2);

    		// Make a GUIStyle that has a solid white hover/onHover background to indicate highlighted items
    		listStyle.normal.textColor = Color.white;
    		tex.SetPixels(colors);
    		tex.Apply();
    		listStyle.hover.background = tex;
    		listStyle.onHover.background = tex;
    		listStyle.padding.left = listStyle.padding.right = listStyle.padding.top = listStyle.padding.bottom = 4;
    	}

    	// Update is called once per frame
    	void Update() {
    	}

    	public void OnGUI() {
    		if (DrawInspector) {
    			inspectorPositionAdjX = inspectorPosition.x;
    			inspectorPositionAdjY = inspectorPosition.y;
    			if (inspectorPosition.x + inspectorWidth > Screen.width) {
    				if (inspectorPosition.y > Screen.height - inspectorMargin) {
    					inspectorPositionAdjX = inspectorPosition.x - inspectorWidth;
    					inspectorPositionAdjY = inspectorPosition.y - inspectorHeight;
    					inspectorRect = new Rect(inspectorPosition.x - inspectorWidth,
    						inspectorPosition.y - inspectorHeight, inspectorWidth, inspectorHeight);
    				}
    				else if (inspectorPosition.y + inspectorHeight > Screen.height) {
    					inspectorPositionAdjX = inspectorPosition.x - inspectorWidth;
    					inspectorRect = new Rect(inspectorPosition.x - inspectorWidth, inspectorPosition.y, inspectorWidth,
    						Screen.height - inspectorPosition.y);
    				}
    				else {
    					inspectorPositionAdjX = inspectorPosition.x - inspectorWidth;
    					inspectorRect = new Rect(inspectorPosition.x - inspectorWidth, inspectorPosition.y, inspectorWidth,
    						inspectorHeight);
    				}
    			}
    			else if (inspectorPosition.y > Screen.height - inspectorMargin) {
    				inspectorPositionAdjY = inspectorPosition.y - inspectorHeight;
    				inspectorRect = new Rect(inspectorPosition.x, inspectorPosition.y - inspectorHeight, inspectorWidth,
    					inspectorHeight);
    			}
    			else if (inspectorPosition.y + inspectorHeight > Screen.height) {
    				inspectorRect = new Rect(inspectorPosition.x, inspectorPosition.y, inspectorWidth,
    					Screen.height - inspectorPosition.y);
    			}
    			else {
    				inspectorRect = new Rect(inspectorPosition.x, inspectorPosition.y, inspectorWidth, inspectorHeight);
    			}

    /*#if UNITY_EDITOR || UNITY_STANDALONE
    			if (File.Exists (inspectorObject.name + ".xml")) {
    				if (!ObjectLoaded (inspectorObject)) {
    					loadedObject = LoadMarkup (inspectorObject);
    					markupCleared = false;
    				}
    			}
    			else {
    				if (!markupCleared) {
    					InitNewMarkup ();
    					loadedObject = new VoxML ();
    				}
    			}
    #endif
    #if UNITY_WEBPLAYER*/
    			// Resources load here
    			//TextAsset markup = Resources.Load (inspectorObject.name) as TextAsset;
    			if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath,
    				string.Format("objects/{0}.xml", inspectorObject.name)))) {
    				using (StreamReader sr = new StreamReader(
    					string.Format("{0}/{1}", Data.voxmlDataPath,
    						string.Format("objects/{0}.xml", inspectorObject.name)))) {
    					String markup = sr.ReadToEnd();
    					//if (markup != null) {
    					if (!ObjectLoaded(markup, inspectorObject.name)) {
    						loadedObject = LoadMarkup(markup, inspectorObject.name);
    						inspectorTitle = inspectorObject.name;
    						markupCleared = false;
    					}

    					//}
    				}
    			}
    			else {
    				if (!markupCleared) {
    					InitNewMarkup();
    					loadedObject = new VoxML();
    				}
    			}
    //#endif

    			GUILayout.BeginArea(inspectorRect, GUI.skin.window);

    			switch (mlEntityType) {
    				case VoxEntity.EntityType.Object:
    					DisplayObjectMarkup();
    					break;

    				case VoxEntity.EntityType.Program:
    					DisplayProgramMarkup();
    					break;

    				case VoxEntity.EntityType.Attribute:
    					DisplayAttributeMarkup();
    					break;

    				case VoxEntity.EntityType.Relation:
    					DisplayRelationMarkup();
    					break;

    				case VoxEntity.EntityType.Function:
    					DisplayFunctionMarkup();
    					break;

    				default:
    					break;
    			}

    			GUILayout.EndArea();

    			Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(inspectorTitle));
    			GUI.Label(
    				new Rect(((2 * inspectorPositionAdjX + inspectorWidth) / 2) - textDimensions.x / 2,
    					inspectorPositionAdjY, textDimensions.x, 25), inspectorTitle);
    		}
    	}

    	void DisplayObjectMarkup() {
    		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

    		inspectorStyle = GUI.skin.box;
    		inspectorStyle.alignment = TextAnchor.MiddleLeft;
    		inspectorStyle.stretchWidth = true;
    		inspectorStyle.wordWrap = true;
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("LEX");
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Pred");
    		GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
    		GUILayout.EndHorizontal();
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Type");

    		GUILayout.BeginVertical();
    		for (int i = 0; i < mlTypeCount; i++) {
    			GUILayout.BeginHorizontal();

    			GUILayout.Box(mlTypes[i], GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    			GUILayout.EndHorizontal();
    		}

    		GUILayout.EndVertical();

    		GUILayout.EndHorizontal();

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("TYPE");
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Head");

    		GUILayout.Box(mlHead, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    		GUILayout.EndHorizontal();
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Components");
    		GUILayout.EndHorizontal();

    		GUILayout.BeginVertical(inspectorStyle);
    		for (int i = 0; i < mlComponentCount; i++) {
    			string componentName = mlComponents[i].Split(new char[] {'['})[0];
    			//TextAsset ml = Resources.Load (componentName) as TextAsset;
    			if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath,
    				string.Format("objects/{0}.xml", componentName)))) {
    				using (StreamReader sr = new StreamReader(
    					string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("objects/{0}.xml", componentName)))) {
    					String ml = sr.ReadToEnd();
    					if (ml != null) {
    						float textSize = GUI.skin.label.CalcSize(new GUIContent(mlComponents[i])).x;
    						float padSize = GUI.skin.label.CalcSize(new GUIContent(" ")).x;
    						int padLength = (int) (((inspectorWidth - 85) - textSize) / (int) padSize);
    						if (GUILayout.Button(mlComponents[i].PadRight(padLength + mlComponents[i].Length - 3),
    							GUILayout.Width(inspectorWidth - 85))) {
    							if (ml != null) {
    								VoxemeInspectorModalWindow newInspector =
    									gameObject.AddComponent<VoxemeInspectorModalWindow>();
    								//LoadMarkup (ml.text);
    								//newInspector.DrawInspector = true;
    								newInspector.windowRect = new Rect(inspectorRect.x + 25, inspectorRect.y + 25,
    									inspectorWidth, inspectorHeight);
    								//newInspector.InspectorTitle = mlComponents [i];
    								newInspector.InspectorVoxeme = "objects/" + componentName;
    								newInspector.Render = true;
    							}
    							else {
    							}
    						}
    					}
    				}
    			}
    			else {
    				GUILayout.Box(mlComponents[i], GUILayout.Width(inspectorWidth - 85));
    			}
    		}

    		GUILayout.EndVertical();

    		GUILayout.EndVertical();

    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Concavity");

    		GUILayout.Box(mlConcavity, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    		GUILayout.EndHorizontal();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("Rotational Symmetry");
    		GUILayout.BeginHorizontal();
    		GUILayout.Toggle(mlRotatSymX, "X");
    		GUILayout.Toggle(mlRotatSymY, "Y");
    		GUILayout.Toggle(mlRotatSymZ, "Z");
    		GUILayout.EndHorizontal();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("Reflectional Symmetry");
    		GUILayout.BeginHorizontal();
    		GUILayout.Toggle(mlReflSymXY, "XY");
    		GUILayout.Toggle(mlReflSymXZ, "XZ");
    		GUILayout.Toggle(mlReflSymYZ, "YZ");
    		GUILayout.EndHorizontal();
    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("HABITAT");
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Intrinsic");

    		GUILayout.EndHorizontal();

    		for (int i = 0; i < mlIntrHabitatCount; i++) {
    			GUILayout.BeginHorizontal();
    			GUILayout.Box(mlIntrHabitats[i].Split(new char[] {'='}, 2)[0], GUILayout.Width(inspectorWidth - 150));
    			GUILayout.Box(mlIntrHabitats[i].Split(new char[] {'='}, 2)[1], GUILayout.Width(inspectorWidth - 140));
    			GUILayout.EndHorizontal();
    		}

    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Extrinsic");

    		GUILayout.EndHorizontal();

    		for (int i = 0; i < mlExtrHabitatCount; i++) {
    			GUILayout.BeginHorizontal();
    			GUILayout.Box(mlExtrHabitats[i].Split(new char[] {'='}, 2)[0], GUILayout.Width(inspectorWidth - 150));
    			GUILayout.Box(mlExtrHabitats[i].Split(new char[] {'='}, 2)[1], GUILayout.Width(inspectorWidth - 140));
    			GUILayout.EndHorizontal();
    		}

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("AFFORD_STR");

    		GUILayout.BeginVertical(inspectorStyle);

    		for (int i = 0; i < mlAffordanceCount; i++) {
    			GUILayout.BeginHorizontal();
    			GUILayout.Box(mlAffordances[i], GUILayout.Width(inspectorWidth - 85));
    			GUILayout.EndHorizontal();
    		}

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("EMBODIMENT");
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Scale");

    		GUILayout.Box(mlScale, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    		GUILayout.EndHorizontal();
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Movable");
    		GUILayout.Toggle(mlMovable, "");
    		GUILayout.EndHorizontal();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("PARTICIPATION");
    		GUILayout.BeginVertical(inspectorStyle);
    		object[] programs = Directory.GetFiles(string.Format("{0}/programs", Data.voxmlDataPath));
    		//object[] assets = Resources.LoadAll ("Programs");
    		foreach (object program in programs) {
    			if (program != null) {
    				List<string> participations = new List<string>();
    				foreach (string affordance in mlAffordances) {
    					if (affordance.Contains(((string) program).Substring(((string) program).LastIndexOf('/') + 1)
    						.Split('.')[0])) {
    						if (!participations.Contains(((string) program)
    							.Substring(((string) program).LastIndexOf('/') + 1).Split('.')[0])) {
    							participations.Add(((string) program).Substring(((string) program).LastIndexOf('/') + 1)
    								.Split('.')[0]);
    						}
    					}
    				}

    				foreach (string p in participations) {
    					using (StreamReader sr = new StreamReader(
    						string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", p)))) {
    						//TextAsset ml = Resources.Load ("Programs/" + p) as TextAsset;
    						String ml = sr.ReadToEnd();
    						if (ml != null) {
    							float textSize = GUI.skin.label.CalcSize(new GUIContent(p)).x;
    							float padSize = GUI.skin.label.CalcSize(new GUIContent(" ")).x;
    							int padLength = (int) (((inspectorWidth - 85) - textSize) / (int) padSize);
    							if (GUILayout.Button(p.PadRight(padLength + p.Length - 3),
    								GUILayout.Width(inspectorWidth - 85))) {
    								if (ml != null) {
    									VoxemeInspectorModalWindow newInspector =
    										gameObject.AddComponent<VoxemeInspectorModalWindow>();
    									//LoadMarkup (ml.text);
    									//newInspector.DrawInspector = true;
    									newInspector.windowRect = new Rect(inspectorRect.x + 25, inspectorRect.y + 25,
    										inspectorWidth, inspectorHeight);
    									//newInspector.InspectorTitle = mlComponents [i];
    									newInspector.InspectorVoxeme = "programs/" + p;
    									newInspector.Render = true;
    								}
    								else {
    								}
    							}
    						}
    						else {
    							GUILayout.Box((string) program, GUILayout.Width(inspectorWidth - 85));
    						}
    					}
    				}
    			}
    		}

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("ATTRIBUTES");
    		GUILayout.BeginVertical(inspectorStyle);
    		AttributeSet attrSet = inspectorObject.GetComponent<AttributeSet>();
    		if (attrSet != null) {
    			foreach (string s in attrSet.attributes) {
    				GUILayout.Box(s, GUILayout.Width(inspectorWidth - 85));
    			}
    		}

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.EndScrollView();
    	}

    	void DisplayProgramMarkup() {
    		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

    		inspectorStyle = GUI.skin.box;
    		inspectorStyle.wordWrap = true;
    		inspectorStyle.alignment = TextAnchor.MiddleLeft;
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("LEX");
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Pred");
    		GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
    		GUILayout.EndHorizontal();
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Type");

    		GUILayout.BeginVertical();
    		for (int i = 0; i < mlTypeCount; i++) {
    			GUILayout.BeginHorizontal();

    			GUILayout.Box(mlTypes[i], GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    			GUILayout.EndHorizontal();
    		}

    		GUILayout.EndVertical();

    		GUILayout.EndHorizontal();

    		GUILayout.EndVertical();
    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.Label("TYPE");
    		GUILayout.BeginHorizontal(inspectorStyle);
    		GUILayout.Label("Head");

    		GUILayout.Box(mlHead, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

    		GUILayout.EndHorizontal();
    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Args");
    		GUILayout.EndHorizontal();

    		GUILayout.BeginVertical(inspectorStyle);
    		for (int i = 0; i < mlArgCount; i++) {
    			GUILayout.Box(mlArgs[i], GUILayout.Width(inspectorWidth - 85));
    		}

    		GUILayout.EndVertical();

    		GUILayout.EndVertical();

    		GUILayout.BeginVertical(inspectorStyle);
    		GUILayout.BeginHorizontal();
    		GUILayout.Label("Body");
    		GUILayout.EndHorizontal();

    		GUILayout.BeginVertical(inspectorStyle);
    		for (int i = 0; i < mlSubeventCount; i++) {
    			GUILayout.Box(mlSubevents[i], GUILayout.Width(inspectorWidth - 85));
    		}

    		GUILayout.EndVertical();

    		GUILayout.EndVertical();

    		GUILayout.EndVertical();

    		GUILayout.EndScrollView();
    	}

    	void DisplayAttributeMarkup() {
    		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

    		inspectorStyle = GUI.skin.box;
    		inspectorStyle.wordWrap = true;
    		inspectorStyle.alignment = TextAnchor.MiddleLeft;
    		GUILayout.BeginVertical(inspectorStyle);

    		GUILayout.EndVertical();

    		GUILayout.EndScrollView();
    	}

    	void DisplayRelationMarkup() {
    		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

    		inspectorStyle = GUI.skin.box;
    		inspectorStyle.wordWrap = true;
    		inspectorStyle.alignment = TextAnchor.MiddleLeft;
    		GUILayout.BeginVertical(inspectorStyle);

    		GUILayout.EndVertical();

    		GUILayout.EndScrollView();
    	}

    	void DisplayFunctionMarkup() {
    		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

    		inspectorStyle = GUI.skin.box;
    		inspectorStyle.alignment = TextAnchor.MiddleLeft;
    		inspectorStyle.stretchWidth = true;
    		inspectorStyle.wordWrap = true;
    		GUILayout.BeginVertical(inspectorStyle);

    		GUILayout.EndVertical();

    		GUILayout.EndScrollView();
    	}

    	void InitNewMarkup() {
    		// ENTITY
    		mlEntityType = VoxEntity.EntityType.None;

    		// LEX
    		mlPred = "";

    		mlTypeSelectVisible = new List<int>(new int[] {-1});
    		mlTypeSelected = new List<int>(new int[] {-1});
    		mlAddType = -1;
    		mlRemoveType = new List<int>(new int[] {-1});
    		mlTypeCount = 1;
    		mlTypes = new List<string>(new string[] {""});

    		// TYPE
    		mlHeadSelectVisible = -1;
    		mlHeadSelected = -1;
    		mlHead = "";

    		mlAddComponent = -1;
    		mlRemoveComponent = new List<int>();
    		mlComponentCount = 0;
    		mlComponents = new List<string>();

    		mlConcavitySelectVisible = -1;
    		mlConcavitySelected = -1;
    		mlConcavity = "";

    		mlRotatSymX = false;
    		mlRotatSymY = false;
    		mlRotatSymZ = false;
    		mlReflSymXY = false;
    		mlReflSymXZ = false;
    		mlReflSymYZ = false;

    		mlArgCount = 0;
    		mlArgs = new List<string>();

    		mlSubeventCount = 0;
    		mlSubevents = new List<string>();

    		// HABITAT
    		mlAddIntrHabitat = -1;
    		mlRemoveIntrHabitat = new List<int>();
    		mlIntrHabitatCount = 0;
    		mlIntrHabitats = new List<string>();

    		mlAddExtrHabitat = -1;
    		mlRemoveExtrHabitat = new List<int>();
    		mlExtrHabitatCount = 0;
    		mlExtrHabitats = new List<string>();

    		// AFFORD_STR
    		mlAddAffordance = -1;
    		mlRemoveAffordance = new List<int>();
    		mlAffordanceCount = 0;
    		mlAffordances = new List<string>();

    		// EMBODIMENT
    		mlScaleSelectVisible = -1;
    		mlScaleSelected = -1;
    		mlScale = "";

    		mlMovable = false;

    		markupCleared = true;
    	}

    	VoxML LoadMarkup(GameObject obj) {
    		VoxML voxml = new VoxML();

    		try {
    			voxml = VoxML.Load(obj.name + ".xml");

    			AssignVoxMLValues(voxml);
    		}
    		catch (FileNotFoundException ex) {
    		}

    		return voxml;
    	}

    	VoxML LoadMarkup(VoxML v) {
    		VoxML voxml = new VoxML();

    		try {
    			voxml = v;

    			AssignVoxMLValues(voxml);
    		}
    		catch (FileNotFoundException ex) {
    		}

    		return voxml;
    	}

    	VoxML LoadMarkup(string text, string filename) {
    		VoxML voxml = new VoxML();

    		try {
    			voxml = VoxML.LoadFromText(text, filename);

    			AssignVoxMLValues(voxml);
    		}
    		catch (FileNotFoundException ex) {
    		}

    		return voxml;
    	}

    	void AssignVoxMLValues(VoxML voxml) {
    		// assign VoxML values
    		// ENTITY
    		mlEntityType = voxml.Entity.Type;

    		// PRED
    		mlPred = voxml.Lex.Pred;
    		mlTypes = new List<string>(voxml.Lex.Type.Split(new char[] {'*'}));
    		mlTypeCount = mlTypes.Count;
    		mlTypeSelectVisible = new List<int>(new int[] {-1});
    		mlTypeSelected = new List<int>(new int[] {-1});
    		mlRemoveType = new List<int>(new int[] {-1});
    		for (int i = 0; i < mlTypeCount; i++) {
    			mlTypeSelectVisible.Add(-1);
    			mlTypeSelected.Add(-1);
    			mlRemoveType.Add(-1);
    		}

    		// TYPE
    		mlHead = voxml.Type.Head;
    		mlComponents = new List<string>();
    		foreach (VoxTypeComponent c in voxml.Type.Components) {
    			mlComponents.Add(c.Value);
    		}

    		mlComponentCount = mlComponents.Count;
    		mlConcavity = voxml.Type.Concavity;

    		List<string> rotatSyms = new List<string>(voxml.Type.RotatSym.Split(new char[] {','}));
    		mlRotatSymX = (rotatSyms.Contains("X"));
    		mlRotatSymY = (rotatSyms.Contains("Y"));
    		mlRotatSymZ = (rotatSyms.Contains("Z"));

    		List<string> reflSyms = new List<string>(voxml.Type.ReflSym.Split(new char[] {','}));
    		mlReflSymXY = (reflSyms.Contains("XY"));
    		mlReflSymXZ = (reflSyms.Contains("XZ"));
    		mlReflSymYZ = (reflSyms.Contains("YZ"));

    		mlArgs = new List<string>();
    		foreach (VoxTypeArg a in voxml.Type.Args) {
    			mlArgs.Add(a.Value);
    		}

    		mlArgCount = mlArgs.Count;

    		mlSubevents = new List<string>();
    		foreach (VoxTypeSubevent e in voxml.Type.Body) {
    			mlSubevents.Add(e.Value);
    		}

    		mlSubeventCount = mlSubevents.Count;

    		// HABITAT
    		mlIntrHabitats = new List<string>();
    		foreach (VoxHabitatIntr i in voxml.Habitat.Intrinsic) {
    			mlIntrHabitats.Add(i.Name + "=" + i.Value);
    		}

    		mlIntrHabitatCount = mlIntrHabitats.Count;
    		mlExtrHabitats = new List<string>();
    		foreach (VoxHabitatExtr e in voxml.Habitat.Extrinsic) {
    			mlExtrHabitats.Add(e.Name + "=" + e.Value);
    		}

    		mlExtrHabitatCount = mlExtrHabitats.Count;

    		// AFFORD_STR
    		mlAffordances = new List<string>();
    		foreach (VoxAffordAffordance a in voxml.Afford_Str.Affordances) {
    			mlAffordances.Add(a.Formula);
    		}

    		mlAffordanceCount = mlAffordances.Count;

    		// EMBODIMENT
    		mlScale = voxml.Embodiment.Scale;
    		mlMovable = voxml.Embodiment.Movable;
    	}

    	bool ObjectLoaded(GameObject obj) {
    		bool r = false;

    		try {
    			r = ((VoxML.Load(obj.name + ".xml")).Lex.Pred == loadedObject.Lex.Pred);
    		}
    		catch (FileNotFoundException ex) {
    		}

    		return r;
    	}

    	bool ObjectLoaded(string text, string filename) {
    		bool r = false;

    		try {
    			r = ((VoxML.LoadFromText(text, filename)).Lex.Pred == loadedObject.Lex.Pred);
    		}
    		catch (FileNotFoundException ex) {
    		}

    		return r;
    	}
    }
}