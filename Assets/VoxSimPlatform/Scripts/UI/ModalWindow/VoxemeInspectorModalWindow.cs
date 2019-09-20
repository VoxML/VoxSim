using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using VoxSimPlatform.Global;
using VoxSimPlatform.Vox;

namespace VoxSimPlatform {
    namespace UI {
        namespace ModalWindow {
            public class VoxemeInspectorModalWindow : ModalWindow {
            	public int inspectorWidth = 230;
            	public int inspectorHeight = 300;
            	public int inspectorMargin = 150;

            	string inspectorTitle = "";

            	public string InspectorTitle {
            		get { return inspectorTitle; }
            		set { inspectorTitle = value; }
            	}

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

            	string inspectorVoxeme;

            	public string InspectorVoxeme {
            		get { return inspectorVoxeme; }
            		set { inspectorVoxeme = value; }
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

            	bool editable;

            	GUIStyle listStyle = new GUIStyle();
            	Texture2D tex;
            	Color[] colors;

            	// Markup vars
            	// ENTITY
            	VoxEntity.EntityType mlEntityType = VoxEntity.EntityType.None;

            	// LEX
            	string mlPred = "";

            	string[] mlObjectTypeOptions = new string[] {"physobj", "human", "artifact"};
            	string[] mlProgramTypeOptions = new string[] {"process", "transition_event"};
            	List<int> mlTypeSelectVisible = new List<int>(new int[] {-1});
            	List<int> mlTypeSelected = new List<int>(new int[] {-1});
            	int mlAddType = -1;
            	List<int> mlRemoveType = new List<int>(new int[] {-1});
            	int mlTypeCount = 1;
            	List<string> mlTypes = new List<string>(new string[] {""});

            	// TYPE
            	string[] mlObjectHeadOptions = new string[]
            		{"cylindroid", "ellipsoid", "rectangular_prism", "toroid", "pyramidoid", "sheet"};

            	string[] mlProgramHeadOptions = new string[] {"state", "process", "transition", "assignment", "test"};
            	int mlHeadSelectVisible = -1;
            	int mlHeadSelected = -1;
            	string mlHead = "";
            	string mlHeadReentrancy = "";

            	int mlAddComponent = -1;
            	List<int> mlRemoveComponent = new List<int>();
            	int mlComponentCount = 0;
            	List<string> mlComponents = new List<string>();
            	List<string> mlComponentReentrancies = new List<string>();

            	string[] mlConcavityOptions = new string[] {"Concave", "Flat", "Convex"};
            	int mlConcavitySelectVisible = -1;
            	int mlConcavitySelected = -1;
            	string mlConcavity = "";
            	string mlConcavityReentrancy = "";

            	bool mlRotatSymX = false;
            	bool mlRotatSymY = false;
            	bool mlRotatSymZ = false;
            	bool mlReflSymXY = false;
            	bool mlReflSymXZ = false;
            	bool mlReflSymYZ = false;

            	int mlAddArg = -1;
            	List<int> mlRemoveArg = new List<int>();
            	int mlArgCount = 0;
            	List<string> mlArgs = new List<string>();

            	int mlAddSubevent = -1;
            	List<int> mlRemoveSubevent = new List<int>();
            	int mlSubeventCount = 0;
            	List<string> mlSubevents = new List<string>();

            	string[] mlTypeScaleOptions = new string[] {"nominal", "binary", "ordinal", "interval", "rational"};
            	int mlTypeScaleSelectVisible = -1;
            	int mlTypeScaleSelected = -1;
            	string mlTypeScale = "";

            	string[] mlArityOptions = new string[] {"intransitive", "transitive"};
            	int mlAritySelectVisible = -1;
            	int mlAritySelected = -1;
            	string mlArity = "";

            	string[] mlClassOptions = new string[] {"config", "force_dynamic"};
            	int mlClassSelectVisible = -1;
            	int mlClassSelected = -1;
            	string mlClass = "";

            	string mlValue = "";
            	string mlConstr = "";

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
            	string[] mlEmbodimentScaleOptions = new string[] {"<agent", "agent", ">agent"};
            	int mlEmbodimentScaleSelectVisible = -1;
            	int mlEmbodimentScaleSelected = -1;
            	string mlEmbodimentScale = "";

            	bool mlMovable = false;

            	// ATTRIBUTES
            	int mlAddAttribute = -1;
            	List<int> mlRemoveAttribute = new List<int>();
            	int mlAttributeCount = 0;
            	List<string> mlAttributes = new List<string>();

            	bool markupCleared = false;
            	VoxML loadedObject = new VoxML();

            	public override bool Render {
            		get { return render; }
            		set { render = value; }
            	}

            	// Use this for initialization
            	protected override void Start() {
            		colors = new Color[] {Color.white, Color.white, Color.white, Color.white};
            		tex = new Texture2D(2, 2);

            		// Make a GUIStyle that has a solid white hover/onHover background to indicate highlighted items
            		listStyle.normal.textColor = Color.white;
            		tex.SetPixels(colors);
            		tex.Apply();
            		listStyle.hover.background = tex;
            		listStyle.onHover.background = tex;
            		listStyle.padding.left = listStyle.padding.right = listStyle.padding.top = listStyle.padding.bottom = 4;

            		//Render = true;

            		editable = (PlayerPrefs.GetInt("Make Voxemes Editable") == 1);

            		base.Start();

            		windowManager.OnNewModalWindow(this, new ModalWindowEventArgs(id));
            	}

            	// Update is called once per frame
            	void Update() {
            	}

            	protected override void OnGUI() {
            		if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("{0}.xml", InspectorVoxeme)))) {
            			using (StreamReader sr = new StreamReader(string.Format("{0}/{1}", Data.voxmlDataPath,
            				string.Format("{0}.xml", InspectorVoxeme)))) {
            				String markup = sr.ReadToEnd();
            				if (!ObjectLoaded(markup, InspectorVoxeme)) {
            					loadedObject = LoadMarkup(markup, InspectorVoxeme);
            					windowTitle = InspectorVoxeme.Substring(InspectorVoxeme.LastIndexOf('/') + 1);
            					markupCleared = false;
            				}
            			}
            		}
            		else {
            			if (!markupCleared) {
            				InitNewMarkup();
            				//loadedObject = new VoxML ();

            				switch (InspectorVoxeme.Remove(InspectorVoxeme.LastIndexOf('/'))) {
            					case "objects":
            						mlEntityType = VoxEntity.EntityType.Object;
            						break;

            					case "programs":
            						mlEntityType = VoxEntity.EntityType.Program;
            						break;

            					case "attributes":
            						mlEntityType = VoxEntity.EntityType.Attribute;
            						break;

            					case "relations":
            						mlEntityType = VoxEntity.EntityType.Relation;
            						break;

            					case "functions":
            						mlEntityType = VoxEntity.EntityType.Function;
            						break;

            					default:
            						break;
            				}

            				windowTitle = InspectorVoxeme.Substring(InspectorVoxeme.LastIndexOf('/') + 1);
            			}
            		}

            		base.OnGUI();
            	}

            	public override void DoModalWindow(int windowID) {
            		base.DoModalWindow(windowID);

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

            		if (editable) {
            			string saveText = "Save";

            			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
            				saveText = "Save As...";
            			}

            			if (GUILayout.Button(saveText)) {
            				if (saveText == "Save As...") {
            					SaveAsModalWindow saveAs = gameObject.AddComponent<SaveAsModalWindow>();
            					saveAs.windowRect = new Rect(InspectorRect.x + 25, InspectorRect.y + 25, 185, 60);
            					saveAs.entityType = mlEntityType;
            					saveAs.Render = true;
            					saveAs.AllowResize = false;
            					saveAs.SaveAsEvent += SaveMarkupAs;
            				}
            				else {
            					SaveMarkup(InspectorVoxeme, mlEntityType);
            					InspectorObject.GetComponent<Voxeme>().LoadVoxML();
            				}
            			}
            			else if (GUILayout.Button("Import")) {
            				ImportMarkup(InspectorVoxeme, mlEntityType);
            			}
            		}

            		Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(inspectorTitle));
            		GUI.Label(
            			new Rect(((2 * inspectorPosition.x + inspectorWidth) / 2) - textDimensions.x / 2, inspectorPosition.y,
            				textDimensions.x, 25), inspectorTitle);
            	}

            	void DisplayObjectMarkup() {
            		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            		inspectorStyle = GUI.skin.box;
            		inspectorStyle.wordWrap = true;
            		inspectorStyle.alignment = TextAnchor.MiddleLeft;
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("LEX");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Pred");

            		if (editable) {
            			mlPred = GUILayout.TextField(mlPred, 25, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Type");

            		GUILayout.BeginVertical();

            		if (editable) {
            			for (int i = 0; i < mlTypeCount; i++) {
            				GUILayout.BeginHorizontal();
            				if (mlTypeSelectVisible[i] == 0) {
            					GUILayout.BeginVertical(inspectorStyle);
            					mlTypeSelected[i] = -1;
            					mlTypeSelected[i] = GUILayout.SelectionGrid(mlTypeSelected[i], mlObjectTypeOptions, 1, listStyle,
            						GUILayout.Width(70), GUILayout.ExpandWidth(true));
            					if (mlTypeSelected[i] != -1) {
            						mlTypes[i] = mlObjectTypeOptions[mlTypeSelected[i]];
            						mlTypeSelectVisible[i] = -1;
            					}

            					GUILayout.EndVertical();
            				}
            				else {
            					mlTypeSelectVisible[i] = GUILayout.SelectionGrid(mlTypeSelectVisible[i], new string[] {mlTypes[i]},
            						1, GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				}

            				if (i != 0) {
            					// can't remove first type
            					mlRemoveType[i] = GUILayout.SelectionGrid(mlRemoveType[i], new string[] {"-"}, 1, GUI.skin.button,
            						GUILayout.ExpandWidth(true));
            				}

            				GUILayout.EndHorizontal();
            			}
            		}
            		else {
            			for (int i = 0; i < mlTypeCount; i++) {
            				GUILayout.BeginHorizontal();

            				GUILayout.Box(mlTypes[i], GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

            				GUILayout.EndHorizontal();
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.EndHorizontal();

            		if (editable) {
            			mlAddType = GUILayout.SelectionGrid(mlAddType, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddType == 0) {
            				// add new type
            				mlTypeCount++;
            				mlTypes.Add("");
            				mlTypeSelectVisible.Add(-1);
            				mlTypeSelected.Add(-1);
            				mlRemoveType.Add(-1);
            				mlAddType = -1;
            			}

            			for (int i = 0; i < mlTypeCount; i++) {
            				if (mlRemoveType[i] == 0) {
            					mlRemoveType[i] = -1;
            					mlTypes.RemoveAt(i);
            					mlRemoveType.RemoveAt(i);
            					mlTypeCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("TYPE");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Head");

            		GUILayout.BeginHorizontal(inspectorStyle);
            		if (editable) {
            			if (mlHeadSelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlHeadSelected = -1;
            				mlHeadSelected = GUILayout.SelectionGrid(mlHeadSelected, mlObjectHeadOptions, 1, listStyle,
            					GUILayout.Width(60), GUILayout.ExpandWidth(true));
            				if (mlHeadSelected != -1) {
            					mlHead = mlObjectHeadOptions[mlHeadSelected];
            					mlHeadSelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            				mlHeadReentrancy = GUILayout.TextField(mlHeadReentrancy, 25, GUILayout.Width(20));
            			}
            			else {
            				mlHeadSelectVisible = GUILayout.SelectionGrid(mlHeadSelectVisible, new string[] {mlHead}, 1,
            					GUI.skin.button, GUILayout.Width(60), GUILayout.ExpandWidth(true));
            				mlHeadReentrancy = GUILayout.TextField(mlHeadReentrancy, 25, GUILayout.Width(20));
            			}
            		}
            		else {
            			GUILayout.Box(mlHead, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            			GUILayout.Box(mlHeadReentrancy, GUILayout.Width(20), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.EndHorizontal();
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Components");

            		if (editable) {
            			mlAddComponent = GUILayout.SelectionGrid(mlAddComponent, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			if (mlAddComponent == 0) {
            				// add new component
            				mlComponentCount++;
            				mlComponents.Add("");
            				mlComponentReentrancies.Add("");
            				mlAddComponent = -1;
            				mlRemoveComponent.Add(-1);
            			}
            		}

            		GUILayout.BeginVertical(inspectorStyle);
            		for (int i = 0; i < mlComponentCount; i++) {
            			GUI.SetNextControlName(string.Format("component{0}", i.ToString()));
            			string componentName = mlComponents[i].Split(new char[] {'['})[0];
            			//TextAsset ml = Resources.Load (componentName) as TextAsset;
            			if ((File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath,
            				    string.Format("objects/{0}.xml", componentName)))) &&
            			    (GUI.GetNameOfFocusedControl() != string.Format("component{0}", i.ToString()))) {
            				using (StreamReader sr = new StreamReader(
            					string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("objects/{0}.xml", componentName)))) {
            					String ml = sr.ReadToEnd();
            					if (ml != null) {
            						float textSize = GUI.skin.label.CalcSize(new GUIContent(mlComponents[i])).x;
            						float padSize = GUI.skin.label.CalcSize(new GUIContent(" ")).x;
            						int padLength = (int) (((inspectorWidth - 85) - textSize) / (int) padSize);

            						GUILayout.BeginHorizontal(inspectorStyle);
            						bool componentButton =
            							GUILayout.Button(mlComponents[i].PadRight(padLength + mlComponents[i].Length - 3),
            								GUILayout.Width(inspectorWidth - 85));
            						mlComponentReentrancies[i] =
            							GUILayout.TextField(mlComponentReentrancies[i], 25, GUILayout.Width(20));
            						if (editable) {
            							mlRemoveComponent.Add(-1);
            							mlRemoveComponent[i] = GUILayout.SelectionGrid(mlRemoveComponent[i], new string[] {"-"}, 1,
            								GUI.skin.button, GUILayout.ExpandWidth(true));
            						}

            						GUILayout.EndHorizontal();

            						if (componentButton) {
            							VoxemeInspectorModalWindow newInspector =
            								gameObject.AddComponent<VoxemeInspectorModalWindow>();
            							newInspector.windowRect = new Rect(windowRect.x + 25, windowRect.y + 25, inspectorWidth,
            								inspectorHeight);
            							newInspector.InspectorVoxeme = "objects/" + mlComponents[i];
            							if (GameObject.Find(mlComponents[i]) != null) {
            								if (GameObject.Find(mlComponents[i]).GetComponent<Voxeme>() != null) {
            									newInspector.InspectorObject = GameObject.Find(mlComponents[i]);
            								}
            							}

            							newInspector.Render = true;
            						}
            					}
            					else {
            						if (editable) {
            							GUILayout.BeginHorizontal(inspectorStyle);
            							mlComponents[i] = GUILayout.TextField(mlComponents[i], 25,
            								GUILayout.Width(inspectorWidth - 85));
            							mlComponentReentrancies[i] = GUILayout.TextField(mlComponents[i], 25, GUILayout.Width(20));
            							mlRemoveComponent.Add(-1);
            							mlRemoveComponent[i] = GUILayout.SelectionGrid(mlRemoveComponent[i], new string[] {"-"}, 1,
            								GUI.skin.button, GUILayout.ExpandWidth(true));
            							GUILayout.EndHorizontal();
            						}
            						else {
            							GUILayout.BeginHorizontal(inspectorStyle);
            							GUILayout.Box(mlComponents[i], GUILayout.Width(inspectorWidth - 85));
            							GUILayout.Box(mlComponentReentrancies[i], GUILayout.Width(20), GUILayout.ExpandWidth(true));
            							GUILayout.EndHorizontal();
            						}
            					}
            				}
            			}
            			else {
            				if (editable) {
            					GUILayout.BeginHorizontal(inspectorStyle);
            					mlComponents[i] = GUILayout.TextField(mlComponents[i], 25, GUILayout.Width(inspectorWidth - 85));
            					mlComponentReentrancies[i] =
            						GUILayout.TextField(mlComponentReentrancies[i], 25, GUILayout.Width(20));
            					mlRemoveComponent.Add(-1);
            					mlRemoveComponent[i] = GUILayout.SelectionGrid(mlRemoveComponent[i], new string[] {"-"}, 1,
            						GUI.skin.button, GUILayout.ExpandWidth(true));
            					GUILayout.EndHorizontal();
            				}
            				else {
            					GUILayout.BeginHorizontal(inspectorStyle);
            					GUILayout.Box(mlComponents[i], GUILayout.Width(inspectorWidth - 85));
            					GUILayout.Box(mlComponentReentrancies[i], GUILayout.Width(20), GUILayout.ExpandWidth(true));
            					GUILayout.EndHorizontal();
            				}
            			}
            		}

            		GUILayout.EndVertical();

            		if (editable) {
            			for (int i = 0; i < mlComponentCount; i++) {
            				if (mlRemoveComponent[i] == 0) {
            					mlRemoveComponent[i] = -1;
            					mlComponents.RemoveAt(i);
            					mlComponentReentrancies.RemoveAt(i);
            					mlRemoveComponent.RemoveAt(i);
            					mlComponentCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Concavity");

            		if (editable) {
            			if (mlConcavitySelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlConcavitySelected = -1;
            				mlConcavitySelected = GUILayout.SelectionGrid(mlConcavitySelected, mlConcavityOptions, 1, listStyle,
            					GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				if (mlConcavitySelected != -1) {
            					mlConcavity = mlConcavityOptions[mlConcavitySelected];
            					mlConcavitySelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            				mlConcavityReentrancy = GUILayout.TextField(mlConcavityReentrancy, 25, GUILayout.Width(20));
            			}
            			else {
            				mlConcavitySelectVisible = GUILayout.SelectionGrid(mlConcavitySelectVisible, new string[] {mlConcavity},
            					1, GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				mlConcavityReentrancy = GUILayout.TextField(mlConcavityReentrancy, 25, GUILayout.Width(20));
            			}
            		}
            		else {
            			GUILayout.Box(mlConcavity, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            			GUILayout.Box(mlConcavityReentrancy, GUILayout.Width(20), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("Rotational Symmetry");
            		GUILayout.BeginHorizontal();

            		if (editable) {
            			mlRotatSymX = GUILayout.Toggle(mlRotatSymX, "X");
            			mlRotatSymY = GUILayout.Toggle(mlRotatSymY, "Y");
            			mlRotatSymZ = GUILayout.Toggle(mlRotatSymZ, "Z");
            		}
            		else {
            			GUILayout.Toggle(mlRotatSymX, "X");
            			GUILayout.Toggle(mlRotatSymY, "Y");
            			GUILayout.Toggle(mlRotatSymZ, "Z");
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("Reflectional Symmetry");
            		GUILayout.BeginHorizontal();

            		if (editable) {
            			mlReflSymXY = GUILayout.Toggle(mlReflSymXY, "XY");
            			mlReflSymXZ = GUILayout.Toggle(mlReflSymXZ, "XZ");
            			mlReflSymYZ = GUILayout.Toggle(mlReflSymYZ, "YZ");
            		}
            		else {
            			GUILayout.Toggle(mlReflSymXY, "XY");
            			GUILayout.Toggle(mlReflSymXZ, "XZ");
            			GUILayout.Toggle(mlReflSymYZ, "YZ");
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("HABITAT");
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Intrinsic");

            		if (editable) {
            			mlAddIntrHabitat = GUILayout.SelectionGrid(mlAddIntrHabitat, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddIntrHabitat == 0) {
            				// add new intrinsic habitat formula
            				mlIntrHabitatCount++;
            				mlIntrHabitats.Add("Name=Formula");
            				mlAddIntrHabitat = -1;
            			}
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			for (int i = 0; i < mlIntrHabitatCount; i++) {
            				GUILayout.BeginHorizontal();
            				mlIntrHabitats[i] =
            					GUILayout.TextField(mlIntrHabitats[i].Split(new char[] {'='}, 2)[0], 25, GUILayout.Width(50)) +
            					"=" +
            					GUILayout.TextField(mlIntrHabitats[i].Split(new char[] {'='}, 2)[1], 25, GUILayout.Width(60));
            				mlRemoveIntrHabitat.Add(-1);
            				mlRemoveIntrHabitat[i] = GUILayout.SelectionGrid(mlRemoveIntrHabitat[i], new string[] {"-"}, 1,
            					GUI.skin.button, GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}

            			for (int i = 0; i < mlIntrHabitatCount; i++) {
            				if (mlRemoveIntrHabitat[i] == 0) {
            					mlRemoveIntrHabitat[i] = -1;
            					mlIntrHabitats.RemoveAt(i);
            					mlRemoveIntrHabitat.RemoveAt(i);
            					mlIntrHabitatCount--;
            				}
            			}
            		}
            		else {
            			for (int i = 0; i < mlIntrHabitatCount; i++) {
            				GUILayout.BeginHorizontal();
            				GUILayout.Box(mlIntrHabitats[i].Split(new char[] {'='}, 2)[0], GUILayout.Width(inspectorWidth - 150));
            				GUILayout.Box(mlIntrHabitats[i].Split(new char[] {'='}, 2)[1], GUILayout.Width(inspectorWidth - 140));
            				GUILayout.EndHorizontal();
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Extrinsic");

            		if (editable) {
            			mlAddExtrHabitat = GUILayout.SelectionGrid(mlAddExtrHabitat, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddExtrHabitat == 0) {
            				// add new extrinsic habitat formula
            				mlExtrHabitatCount++;
            				mlExtrHabitats.Add("Name=Formula");
            				mlAddExtrHabitat = -1;
            			}
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			for (int i = 0; i < mlExtrHabitatCount; i++) {
            				GUILayout.BeginHorizontal();
            				mlExtrHabitats[i] =
            					GUILayout.TextField(mlExtrHabitats[i].Split(new char[] {'='}, 2)[0], 25, GUILayout.Width(50)) +
            					"=" +
            					GUILayout.TextField(mlExtrHabitats[i].Split(new char[] {'='}, 2)[1], 25, GUILayout.Width(60));
            				mlRemoveExtrHabitat.Add(-1);
            				mlRemoveExtrHabitat[i] = GUILayout.SelectionGrid(mlRemoveExtrHabitat[i], new string[] {"-"}, 1,
            					GUI.skin.button, GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}

            			for (int i = 0; i < mlExtrHabitatCount; i++) {
            				if (mlRemoveExtrHabitat[i] == 0) {
            					mlRemoveExtrHabitat[i] = -1;
            					mlExtrHabitats.RemoveAt(i);
            					mlRemoveExtrHabitat.RemoveAt(i);
            					mlExtrHabitatCount--;
            				}
            			}
            		}
            		else {
            			for (int i = 0; i < mlExtrHabitatCount; i++) {
            				GUILayout.BeginHorizontal();
            				GUILayout.Box(mlExtrHabitats[i].Split(new char[] {'='}, 2)[0], GUILayout.Width(inspectorWidth - 150));
            				GUILayout.Box(mlExtrHabitats[i].Split(new char[] {'='}, 2)[1], GUILayout.Width(inspectorWidth - 140));
            				GUILayout.EndHorizontal();
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("AFFORD_STR");

            		GUILayout.BeginVertical(inspectorStyle);

            		if (editable) {
            			for (int i = 0; i < mlAffordanceCount; i++) {
            				GUILayout.BeginHorizontal();
            				mlAffordances[i] = GUILayout.TextField(mlAffordances[i], 50, GUILayout.Width(115));
            				mlRemoveAffordance.Add(-1);
            				mlRemoveAffordance[i] = GUILayout.SelectionGrid(mlRemoveAffordance[i], new string[] {"-"}, 1,
            					GUI.skin.button, GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}

            			for (int i = 0; i < mlAffordanceCount; i++) {
            				if (mlRemoveAffordance[i] == 0) {
            					mlRemoveAffordance[i] = -1;
            					mlAffordances.RemoveAt(i);
            					mlRemoveAffordance.RemoveAt(i);
            					mlAffordanceCount--;
            				}
            			}

            			mlAddAffordance = GUILayout.SelectionGrid(mlAddAffordance, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddAffordance == 0) {
            				// add new affordance
            				mlAffordanceCount++;
            				mlAffordances.Add("");
            				mlAddAffordance = -1;
            			}
            		}
            		else {
            			for (int i = 0; i < mlAffordanceCount; i++) {
            				GUILayout.BeginHorizontal();
            				GUILayout.Box(mlAffordances[i], GUILayout.Width(inspectorWidth - 85));
            				GUILayout.EndHorizontal();
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("EMBODIMENT");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Scale");

            		if (editable) {
            			if (mlEmbodimentScaleSelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlEmbodimentScaleSelected = -1;
            				mlEmbodimentScaleSelected = GUILayout.SelectionGrid(mlEmbodimentScaleSelected, mlEmbodimentScaleOptions,
            					1, listStyle, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				if (mlEmbodimentScaleSelected != -1) {
            					mlEmbodimentScale = mlEmbodimentScaleOptions[mlEmbodimentScaleSelected];
            					mlEmbodimentScaleSelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            			}
            			else {
            				mlEmbodimentScaleSelectVisible = GUILayout.SelectionGrid(mlEmbodimentScaleSelectVisible,
            					new string[] {mlEmbodimentScale}, 1, GUI.skin.button, GUILayout.Width(70),
            					GUILayout.ExpandWidth(true));
            			}
            		}
            		else {
            			GUILayout.Box(mlEmbodimentScale, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Movable");

            		if (editable) {
            			mlMovable = GUILayout.Toggle(mlMovable, "");
            		}
            		else {
            			GUILayout.Toggle(mlMovable, "");
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("PARTICIPATION");
            		GUILayout.BeginVertical(inspectorStyle);
            		List<string> participations = new List<string>();

            		object[] programs = Directory.GetFiles(string.Format("{0}/programs", Data.voxmlDataPath));
            		//object[] assets = Resources.LoadAll ("Programs");
            		foreach (object program in programs) {
            			if (program != null) {
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
            			}
            		}

            		object[] relations = Directory.GetFiles(string.Format("{0}/relations", Data.voxmlDataPath));
            		//object[] assets = Resources.LoadAll ("Programs");
            		foreach (object relation in relations) {
            			if (relation != null) {
            				foreach (string affordance in mlAffordances) {
            					if (affordance.Contains(((string) relation).Substring(((string) relation).LastIndexOf('/') + 1)
            						.Split('.')[0])) {
            						if (!participations.Contains(((string) relation)
            							.Substring(((string) relation).LastIndexOf('/') + 1).Split('.')[0])) {
            							participations.Add(((string) relation).Substring(((string) relation).LastIndexOf('/') + 1)
            								.Split('.')[0]);
            						}
            					}
            				}
            			}
            		}

            		foreach (string p in participations) {
            			string filePath = string.Empty;
            			string dir = string.Empty;
            			if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", p)))) {
            				dir = "programs/";
            				filePath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", p));
            			}
            			else if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("relations/{0}.xml", p)))) {
            				dir = "relations/";
            				filePath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("relations/{0}.xml", p));
            			}

            			if (filePath != string.Empty) {
            				using (StreamReader sr = new StreamReader(filePath)) {
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
            								newInspector.windowRect = new Rect(windowRect.x + 25, windowRect.y + 25, inspectorWidth,
            									inspectorHeight);
            								//newInspector.InspectorTitle = mlComponents [i];
            								newInspector.InspectorVoxeme = dir + p;
            								newInspector.Render = true;
            							}
            							else {
            							}
            						}
            					}
            					else {
            						GUILayout.Box((string) p, GUILayout.Width(inspectorWidth - 85));
            					}
            				}
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("ATTRIBUTES");

            		GUILayout.BeginVertical(inspectorStyle);
            		if (InspectorObject != null) {
            			AttributeSet attrSet = InspectorObject.GetComponent<AttributeSet>();
            			if (attrSet != null) {
            				for (int i = 0; i < mlAttributeCount; i++) {
            					GUI.SetNextControlName(string.Format("attribute{0}", i.ToString()));
            					if ((File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath,
            						    string.Format("attributes/{0}.xml", mlAttributes[i])))) &&
            					    (GUI.GetNameOfFocusedControl() != string.Format("attribute{0}", i.ToString()))) {
            						using (StreamReader sr = new StreamReader(
            							string.Format("{0}/{1}", Data.voxmlDataPath,
            								string.Format("attributes/{0}.xml", mlAttributes[i])))) {
            							String ml = sr.ReadToEnd();
            							if (ml != null) {
            								float textSize = GUI.skin.label.CalcSize(new GUIContent(mlAttributes[i])).x;
            								float padSize = GUI.skin.label.CalcSize(new GUIContent(" ")).x;
            								int padLength = (int) (((inspectorWidth - 85) - textSize) / (int) padSize);

            								GUILayout.BeginHorizontal(inspectorStyle);
            								bool attributeButton = GUILayout.Button(
            									mlAttributes[i].PadRight(padLength + mlAttributes[i].Length - 3),
            									GUILayout.Width(inspectorWidth - 85));
            								if (editable) {
            									mlRemoveAttribute.Add(-1);
            									mlRemoveAttribute[i] = GUILayout.SelectionGrid(mlRemoveAttribute[i],
            										new string[] {"-"}, 1, GUI.skin.button, GUILayout.ExpandWidth(true));
            								}

            								GUILayout.EndHorizontal();

            								if (attributeButton) {
            									VoxemeInspectorModalWindow newInspector =
            										gameObject.AddComponent<VoxemeInspectorModalWindow>();
            									newInspector.windowRect = new Rect(windowRect.x + 25, windowRect.y + 25,
            										inspectorWidth, inspectorHeight);
            									newInspector.InspectorVoxeme = "attributes/" + mlAttributes[i];
            									newInspector.Render = true;
            								}
            							}
            							else {
            								if (editable) {
            									GUILayout.BeginHorizontal(inspectorStyle);
            									mlAttributes[i] = GUILayout.TextField(mlAttributes[i], 25,
            										GUILayout.Width(inspectorWidth - 85));
            									mlRemoveAttribute.Add(-1);
            									mlRemoveAttribute[i] = GUILayout.SelectionGrid(mlRemoveAttribute[i],
            										new string[] {"-"}, 1, GUI.skin.button, GUILayout.ExpandWidth(true));
            									GUILayout.EndHorizontal();
            								}
            								else {
            									GUILayout.BeginHorizontal(inspectorStyle);
            									GUILayout.Box(mlAttributes[i], GUILayout.Width(inspectorWidth - 85));
            									GUILayout.EndHorizontal();
            								}
            							}
            						}
            					}
            					else {
            						if (editable) {
            							GUILayout.BeginHorizontal(inspectorStyle);
            							mlAttributes[i] = GUILayout.TextField(mlAttributes[i], 25,
            								GUILayout.Width(inspectorWidth - 85));
            							mlRemoveAttribute.Add(-1);
            							mlRemoveAttribute[i] = GUILayout.SelectionGrid(mlRemoveAttribute[i], new string[] {"-"}, 1,
            								GUI.skin.button, GUILayout.ExpandWidth(true));
            							GUILayout.EndHorizontal();
            						}
            						else {
            							GUILayout.BeginHorizontal(inspectorStyle);
            							GUILayout.Box(mlAttributes[i], GUILayout.Width(inspectorWidth - 85));
            							GUILayout.EndHorizontal();
            						}
            					}
            				}
            			}
            		}

            		if (editable) {
            			mlAddAttribute = GUILayout.SelectionGrid(mlAddAttribute, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddAttribute == 0) {
            				// add new attribute
            				mlAttributeCount++;
            				mlAttributes.Add("");
            				mlAddAttribute = -1;
            				mlRemoveAttribute.Add(-1);
            			}

            			for (int i = 0; i < mlAttributeCount; i++) {
            				if (mlRemoveAttribute[i] == 0) {
            					mlRemoveAttribute[i] = -1;
            					mlAttributes.RemoveAt(i);
            					mlRemoveAttribute.RemoveAt(i);
            					mlAttributeCount--;
            				}
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

            		if (editable) {
            			mlPred = GUILayout.TextField(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Type");

            		GUILayout.BeginVertical();

            		if (editable) {
            			for (int i = 0; i < mlTypeCount; i++) {
            				GUILayout.BeginHorizontal();
            				if (mlTypeSelectVisible[i] == 0) {
            					GUILayout.BeginVertical(inspectorStyle);
            					mlTypeSelected[i] = -1;
            					mlTypeSelected[i] = GUILayout.SelectionGrid(mlTypeSelected[i], mlProgramTypeOptions, 1, listStyle,
            						GUILayout.Width(70), GUILayout.ExpandWidth(true));
            					if (mlTypeSelected[i] != -1) {
            						mlTypes[i] = mlProgramTypeOptions[mlTypeSelected[i]];
            						mlTypeSelectVisible[i] = -1;
            					}

            					GUILayout.EndVertical();
            				}
            				else {
            					mlTypeSelectVisible[i] = GUILayout.SelectionGrid(mlTypeSelectVisible[i], new string[] {mlTypes[i]},
            						1, GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				}

            				if (i != 0) {
            					// can't remove first type
            					mlRemoveType[i] = GUILayout.SelectionGrid(mlRemoveType[i], new string[] {"-"}, 1, GUI.skin.button,
            						GUILayout.ExpandWidth(true));
            				}

            				GUILayout.EndHorizontal();
            			}
            		}
            		else {
            			for (int i = 0; i < mlTypeCount; i++) {
            				GUILayout.BeginHorizontal();

            				GUILayout.Box(mlTypes[i], GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));

            				GUILayout.EndHorizontal();
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.EndHorizontal();

            		if (editable) {
            			mlAddType = GUILayout.SelectionGrid(mlAddType, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));

            			if (mlAddType == 0) {
            				// add new type
            				mlTypeCount++;
            				mlTypes.Add("");
            				mlTypeSelectVisible.Add(-1);
            				mlTypeSelected.Add(-1);
            				mlRemoveType.Add(-1);
            				mlAddType = -1;
            			}

            			for (int i = 0; i < mlTypeCount; i++) {
            				if (mlRemoveType[i] == 0) {
            					mlRemoveType[i] = -1;
            					mlTypes.RemoveAt(i);
            					mlRemoveType.RemoveAt(i);
            					mlTypeCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("TYPE");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Head");

            		GUILayout.BeginHorizontal(inspectorStyle);
            		if (editable) {
            			if (mlHeadSelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlHeadSelected = -1;
            				mlHeadSelected = GUILayout.SelectionGrid(mlHeadSelected, mlProgramHeadOptions, 1, listStyle,
            					GUILayout.Width(60), GUILayout.ExpandWidth(true));
            				if (mlHeadSelected != -1) {
            					mlHead = mlProgramHeadOptions[mlHeadSelected];
            					mlHeadSelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            			}
            			else {
            				mlHeadSelectVisible = GUILayout.SelectionGrid(mlHeadSelectVisible, new string[] {mlHead}, 1,
            					GUI.skin.button, GUILayout.Width(60), GUILayout.ExpandWidth(true));
            			}
            		}
            		else {
            			GUILayout.Box(mlHead, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.EndHorizontal();
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Args");

            		if (editable) {
            			mlAddArg = GUILayout.SelectionGrid(mlAddArg, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			if (mlAddArg == 0) {
            				// add new argument
            				mlArgCount++;
            				mlArgs.Add("");
            				mlAddArg = -1;
            				mlRemoveArg.Add(-1);
            			}
            		}

            		GUILayout.BeginVertical(inspectorStyle);
            		for (int i = 0; i < mlArgCount; i++) {
            			if (editable) {
            				GUILayout.BeginHorizontal(inspectorStyle);
            				mlArgs[i] = GUILayout.TextField(mlArgs[i], GUILayout.Width(inspectorWidth - 85));
            				mlRemoveArg.Add(-1);
            				mlRemoveArg[i] = GUILayout.SelectionGrid(mlRemoveArg[i], new string[] {"-"}, 1, GUI.skin.button,
            					GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}
            			else {
            				GUILayout.Box(mlArgs[i], GUILayout.Width(inspectorWidth - 85));
            			}
            		}

            		GUILayout.EndVertical();

            		if (editable) {
            			for (int i = 0; i < mlArgCount; i++) {
            				if (mlRemoveArg[i] == 0) {
            					mlRemoveArg[i] = -1;
            					mlArgs.RemoveAt(i);
            					mlRemoveArg.RemoveAt(i);
            					mlArgCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Body");

            		if (editable) {
            			mlAddSubevent = GUILayout.SelectionGrid(mlAddSubevent, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			if (mlAddSubevent == 0) {
            				// add new subevent
            				mlSubeventCount++;
            				mlSubevents.Add("");
            				mlAddSubevent = -1;
            				mlRemoveSubevent.Add(-1);
            			}
            		}

            		GUILayout.BeginVertical(inspectorStyle);
            		for (int i = 0; i < mlSubeventCount; i++) {
            			if (editable) {
            				GUILayout.BeginHorizontal(inspectorStyle);
            				mlSubevents[i] = GUILayout.TextField(mlSubevents[i], GUILayout.Width(inspectorWidth - 85));
            				mlRemoveSubevent.Add(-1);
            				mlRemoveSubevent[i] = GUILayout.SelectionGrid(mlRemoveSubevent[i], new string[] {"-"}, 1,
            					GUI.skin.button, GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}
            			else {
            				GUILayout.Box(mlSubevents[i], GUILayout.Width(inspectorWidth - 85));
            			}
            		}

            		GUILayout.EndVertical();

            		if (editable) {
            			for (int i = 0; i < mlSubeventCount; i++) {
            				if (mlRemoveSubevent[i] == 0) {
            					mlRemoveSubevent[i] = -1;
            					mlSubevents.RemoveAt(i);
            					mlRemoveSubevent.RemoveAt(i);
            					mlSubeventCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("PARTICIPATION");
            		GUILayout.BeginVertical(inspectorStyle);
            		List<string> participations = new List<string>();

            		object[] programs = Directory.GetFiles(string.Format("{0}/programs", Data.voxmlDataPath));
            		//object[] assets = Resources.LoadAll ("Programs");
            		foreach (object program in programs) {
            			if (program != null) {
            				foreach (string subevent in mlSubevents) {
            					if (subevent.Contains(((string) program).Substring(((string) program).LastIndexOf('/') + 1)
            						.Split('.')[0])) {
            						if (!participations.Contains(((string) program)
            							.Substring(((string) program).LastIndexOf('/') + 1).Split('.')[0])) {
            							participations.Add(((string) program).Substring(((string) program).LastIndexOf('/') + 1)
            								.Split('.')[0]);
            						}
            					}
            				}
            			}
            		}

            		object[] relations = Directory.GetFiles(string.Format("{0}/relations", Data.voxmlDataPath));
            		//object[] assets = Resources.LoadAll ("Programs");
            		foreach (object relation in relations) {
            			if (relation != null) {
            				foreach (string subevent in mlSubevents) {
            					if (subevent.Contains(((string) relation).Substring(((string) relation).LastIndexOf('/') + 1)
            						.Split('.')[0])) {
            						if (!participations.Contains(((string) relation)
            							.Substring(((string) relation).LastIndexOf('/') + 1).Split('.')[0])) {
            							participations.Add(((string) relation).Substring(((string) relation).LastIndexOf('/') + 1)
            								.Split('.')[0]);
            						}
            					}
            				}
            			}
            		}

            		foreach (string p in participations) {
            			string filePath = string.Empty;
            			string dir = string.Empty;
            			if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", p)))) {
            				dir = "programs/";
            				filePath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("programs/{0}.xml", p));
            			}
            			else if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("relations/{0}.xml", p)))) {
            				dir = "relations/";
            				filePath = string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("relations/{0}.xml", p));
            			}

            			if (filePath != string.Empty) {
            				using (StreamReader sr = new StreamReader(filePath)) {
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
            								newInspector.windowRect = new Rect(windowRect.x + 25, windowRect.y + 25, inspectorWidth,
            									inspectorHeight);
            								//newInspector.InspectorTitle = mlComponents [i];
            								newInspector.InspectorVoxeme = dir + p;
            								newInspector.Render = true;
            							}
            							else {
            							}
            						}
            					}
            					else {
            						GUILayout.Box((string) p, GUILayout.Width(inspectorWidth - 85));
            					}
            				}
            			}
            		}

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
            		GUILayout.Label("LEX");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Pred");

            		if (editable) {
            			mlPred = GUILayout.TextField(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("TYPE");

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Scale");
            		if (editable) {
            			if (mlTypeScaleSelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlTypeScaleSelected = -1;
            				mlTypeScaleSelected = GUILayout.SelectionGrid(mlTypeScaleSelected, mlTypeScaleOptions, 1, listStyle,
            					GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				if (mlTypeScaleSelected != -1) {
            					mlTypeScale = mlTypeScaleOptions[mlTypeScaleSelected];
            					mlTypeScaleSelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            			}
            			else {
            				mlTypeScaleSelectVisible = GUILayout.SelectionGrid(mlTypeScaleSelectVisible, new string[] {mlTypeScale},
            					1, GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            			}
            		}
            		else {
            			GUILayout.Box(mlTypeScale, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Arity");
            		if (editable) {
            			if (mlAritySelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlAritySelected = -1;
            				mlAritySelected = GUILayout.SelectionGrid(mlAritySelected, mlArityOptions, 1, listStyle,
            					GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				if (mlAritySelected != -1) {
            					mlArity = mlArityOptions[mlAritySelected];
            					mlAritySelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            			}
            			else {
            				mlAritySelectVisible = GUILayout.SelectionGrid(mlAritySelectVisible, new string[] {mlArity}, 1,
            					GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            			}
            		}
            		else {
            			GUILayout.Box(mlArity, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Arg");

            		if (editable) {
            			mlArgs[0] = GUILayout.TextField(mlArgs[0], GUILayout.Width(inspectorWidth - 85));
            		}
            		else {
            			GUILayout.Box(mlArgs[0], GUILayout.Width(inspectorWidth - 85));
            		}

            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();

            		GUILayout.EndVertical();

            		GUILayout.EndScrollView();
            	}

            	void DisplayRelationMarkup() {
            		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            		inspectorStyle = GUI.skin.box;
            		inspectorStyle.wordWrap = true;
            		inspectorStyle.alignment = TextAnchor.MiddleLeft;
            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("LEX");
            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Pred");

            		if (editable) {
            			mlPred = GUILayout.TextField(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlPred, GUILayout.Width(inspectorWidth - 100));
            		}


            		GUILayout.EndHorizontal();
            		GUILayout.EndVertical();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.Label("TYPE");

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Class");
            		if (editable) {
            			if (mlClassSelectVisible == 0) {
            				GUILayout.BeginVertical(inspectorStyle);
            				mlClassSelected = -1;
            				mlClassSelected = GUILayout.SelectionGrid(mlClassSelected, mlClassOptions, 1, listStyle,
            					GUILayout.Width(70), GUILayout.ExpandWidth(true));
            				if (mlClassSelected != -1) {
            					mlClass = mlClassOptions[mlClassSelected];
            					mlClassSelectVisible = -1;
            				}

            				GUILayout.EndVertical();
            			}
            			else {
            				mlClassSelectVisible = GUILayout.SelectionGrid(mlClassSelectVisible, new string[] {mlClass}, 1,
            					GUI.skin.button, GUILayout.Width(70), GUILayout.ExpandWidth(true));
            			}
            		}
            		else {
            			GUILayout.Box(mlClass, GUILayout.Width(inspectorWidth - 130), GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Value");

            		if (editable) {
            			mlValue = GUILayout.TextField(mlValue, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlValue, GUILayout.Width(inspectorWidth - 100));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.BeginVertical(inspectorStyle);
            		GUILayout.BeginHorizontal();
            		GUILayout.Label("Args");

            		if (editable) {
            			mlAddArg = GUILayout.SelectionGrid(mlAddArg, new string[] {"+"}, 1, GUI.skin.button,
            				GUILayout.ExpandWidth(true));
            		}

            		GUILayout.EndHorizontal();

            		if (editable) {
            			if (mlAddArg == 0) {
            				// add new argument
            				mlArgCount++;
            				mlArgs.Add("");
            				mlAddArg = -1;
            				mlRemoveArg.Add(-1);
            			}
            		}

            		GUILayout.BeginVertical(inspectorStyle);
            		for (int i = 0; i < mlArgCount; i++) {
            			if (editable) {
            				GUILayout.BeginHorizontal(inspectorStyle);
            				mlArgs[i] = GUILayout.TextField(mlArgs[i], GUILayout.Width(inspectorWidth - 85));
            				mlRemoveArg.Add(-1);
            				mlRemoveArg[i] = GUILayout.SelectionGrid(mlRemoveArg[i], new string[] {"-"}, 1, GUI.skin.button,
            					GUILayout.ExpandWidth(true));
            				GUILayout.EndHorizontal();
            			}
            			else {
            				GUILayout.Box(mlArgs[i], GUILayout.Width(inspectorWidth - 85));
            			}
            		}

            		GUILayout.EndVertical();

            		if (editable) {
            			for (int i = 0; i < mlArgCount; i++) {
            				if (mlRemoveArg[i] == 0) {
            					mlRemoveArg[i] = -1;
            					mlArgs.RemoveAt(i);
            					mlRemoveArg.RemoveAt(i);
            					mlArgCount--;
            				}
            			}
            		}

            		GUILayout.EndVertical();

            		GUILayout.BeginHorizontal(inspectorStyle);
            		GUILayout.Label("Constr");

            		if (editable) {
            			mlConstr = GUILayout.TextField(mlConstr, GUILayout.Width(inspectorWidth - 100));
            		}
            		else {
            			GUILayout.Box(mlConstr, GUILayout.Width(inspectorWidth - 100));
            		}

            		GUILayout.EndHorizontal();

            		GUILayout.EndVertical();

            		GUILayout.EndScrollView();
            	}

            	void DisplayFunctionMarkup() {
            		scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

            		inspectorStyle = GUI.skin.box;
            		inspectorStyle.wordWrap = true;
            		inspectorStyle.alignment = TextAnchor.MiddleLeft;
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

            		mlClass = "";
            		mlValue = "";
            		mlConstr = "";

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
            		mlEmbodimentScaleSelectVisible = -1;
            		mlEmbodimentScaleSelected = -1;
            		mlEmbodimentScale = "";

            		mlMovable = false;

            		markupCleared = true;
            	}

            	void ImportMarkup(string cloneToPath, VoxEntity.EntityType entityType) {
            		ImportMarkupModalWindow importWindow = gameObject.AddComponent<ImportMarkupModalWindow>();
            		//LoadMarkup (ml.text);
            		//newInspector.DrawInspector = true;
            		float xPos = windowRect.x + windowRect.width + 235 > Screen.width
            			? windowRect.x - 235
            			: windowRect.x + windowRect.width + 5;
            		importWindow.windowRect = new Rect(xPos, windowRect.y, 230,
            			(windowRect.y + 300 > Screen.height) ? Screen.height - windowRect.y : 300);
            		importWindow.windowTitle = "Import VoxML";
            		importWindow.entityType = entityType;
            		importWindow.ItemSelected += ImportVoxML;
            		importWindow.Render = true;
            	}

            	void ImportVoxML(object sender, EventArgs e) {
            		if (File.Exists(((ImportMarkupEventArgs) e).ImportPath)) {
            			using (StreamReader sr = new StreamReader(((ImportMarkupEventArgs) e).ImportPath)) {
            				String markup = sr.ReadToEnd();
            				//if (!ObjectLoaded (markup)) {
            				loadedObject = LoadMarkup(markup, Path.GetFileNameWithoutExtension(((ImportMarkupEventArgs)e).ImportPath));
            				//windowTitle = InspectorVoxeme.Substring (InspectorVoxeme.LastIndexOf ('/') + 1);
            				//}
            			}
            		}

            		if (mlEntityType == VoxEntity.EntityType.Object) {
            			if (InspectorObject != null) {
            				AttributeSet attrSet = InspectorObject.GetComponent<AttributeSet>();
            				if (attrSet != null) {
            					attrSet.attributes.Clear();
            					for (int i = 0; i < mlAttributeCount; i++) {
            						attrSet.attributes.Add(mlAttributes[i]);
            					}
            				}
            			}
            		}
            	}

            	void SaveMarkupAs(object sender, EventArgs e) {
            		if (((ModalWindowEventArgs) e).Data is SaveAsInfo) {
            			SaveAsInfo saveInfo = (SaveAsInfo) ((ModalWindowEventArgs) e).Data;

            			string dir = string.Empty;
            			switch (saveInfo.EntityType) {
            				case VoxEntity.EntityType.Object:
            					dir = "objects/";
            					break;

            				case VoxEntity.EntityType.Program:
            					dir = "programs/";
            					break;

            				case VoxEntity.EntityType.Attribute:
            					dir = "attributes/";
            					break;

            				case VoxEntity.EntityType.Relation:
            					dir = "relations/";
            					break;

            				case VoxEntity.EntityType.Function:
            					dir = "functions/";
            					break;

            				default:
            					break;
            			}

            			windowManager.windowManager[((ModalWindowEventArgs) e).WindowID].DestroyWindow();
            			SaveMarkup(dir + saveInfo.FileName, saveInfo.EntityType);
            			InspectorVoxeme = dir + saveInfo.FileName;
            			Debug.Log(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("{0}.xml", InspectorVoxeme)));
            			if (File.Exists(string.Format("{0}/{1}", Data.voxmlDataPath, string.Format("{0}.xml", InspectorVoxeme)))) {
            				using (StreamReader sr = new StreamReader(string.Format("{0}/{1}", Data.voxmlDataPath,
            					string.Format("{0}.xml", InspectorVoxeme)))) {
            					String markup = sr.ReadToEnd();
            					loadedObject = LoadMarkup(markup, InspectorVoxeme);
            					windowTitle = InspectorVoxeme.Substring(InspectorVoxeme.LastIndexOf('/') + 1);
            				}
            			}

            			InspectorObject.transform.Find(InspectorObject.name + "*").name = windowTitle + "*";
            			InspectorObject.name = windowTitle;
            		}
            	}

            	void SaveMarkup(string markupPath, VoxEntity.EntityType entityType) {
            		VoxML voxml = new VoxML();
            		voxml.Entity.Type = entityType;

            		// assign VoxML values
            		// PRED
            		voxml.Lex.Pred = mlPred;
            		voxml.Lex.Type = String.Join("*", mlTypes.ToArray());

            		// TYPE
            		voxml.Type.Head = (mlHeadReentrancy != string.Empty)
            			? mlHead + string.Format("[{0}]", mlHeadReentrancy)
            			: mlHead;
            		for (int i = 0; i < mlComponentCount; i++) {
            			voxml.Type.Components.Add(new VoxTypeComponent());
            			voxml.Type.Components[i].Value = (mlComponentReentrancies[i] != string.Empty)
            				? mlComponents[i] + string.Format("[{0}]", mlComponentReentrancies[i])
            				: mlComponents[i];
            		}

            		voxml.Type.Concavity = (mlConcavityReentrancy != string.Empty)
            			? mlConcavity + string.Format("[{0}]", mlConcavityReentrancy)
            			: mlConcavity;

            		List<string> rotatSyms = new List<string>();
            		if (mlRotatSymX) {
            			rotatSyms.Add("X");
            		}

            		if (mlRotatSymY) {
            			rotatSyms.Add("Y");
            		}

            		if (mlRotatSymZ) {
            			rotatSyms.Add("Z");
            		}

            		voxml.Type.RotatSym = String.Join(",", rotatSyms.ToArray());

            		List<string> reflSyms = new List<string>();
            		if (mlReflSymXY) {
            			reflSyms.Add("XY");
            		}

            		if (mlReflSymXZ) {
            			reflSyms.Add("XZ");
            		}

            		if (mlReflSymYZ) {
            			reflSyms.Add("YZ");
            		}

            		voxml.Type.ReflSym = String.Join(",", reflSyms.ToArray());

            		for (int i = 0; i < mlArgCount; i++) {
            			voxml.Type.Args.Add(new VoxTypeArg());
            			voxml.Type.Args[i].Value = mlArgs[i];
            		}

            		for (int i = 0; i < mlSubeventCount; i++) {
            			voxml.Type.Body.Add(new VoxTypeSubevent());
            			voxml.Type.Body[i].Value = mlSubevents[i];
            		}

            		voxml.Type.Scale = mlTypeScale;
            		voxml.Type.Arity = mlArity;

            		voxml.Type.Class = mlClass;
            		voxml.Type.Value = mlValue;
            		voxml.Type.Constr = mlConstr;

            		// HABITAT
            		for (int i = 0; i < mlIntrHabitatCount; i++) {
            			voxml.Habitat.Intrinsic.Add(new VoxHabitatIntr());
            			voxml.Habitat.Intrinsic[i].Name = mlIntrHabitats[i].Split(new char[] {'='}, 2)[0];
            			voxml.Habitat.Intrinsic[i].Value = mlIntrHabitats[i].Split(new char[] {'='}, 2)[1];
            		}

            		for (int i = 0; i < mlExtrHabitatCount; i++) {
            			voxml.Habitat.Extrinsic.Add(new VoxHabitatExtr());
            			voxml.Habitat.Extrinsic[i].Name = mlExtrHabitats[i].Split(new char[] {'='}, 2)[0];
            			voxml.Habitat.Extrinsic[i].Value = mlExtrHabitats[i].Split(new char[] {'='}, 2)[1];
            		}

            		// AFFORD_STR
            		for (int i = 0; i < mlAffordanceCount; i++) {
            			voxml.Afford_Str.Affordances.Add(new VoxAffordAffordance());
            			voxml.Afford_Str.Affordances[i].Formula = mlAffordances[i];
            		}

            		// EMBODIMENT
            		voxml.Embodiment.Scale = mlEmbodimentScale;
            		voxml.Embodiment.Movable = mlMovable;

            		// ATTRIBUTES
            		if (mlEntityType == VoxEntity.EntityType.Object) {
            			if (InspectorObject != null) {
            				AttributeSet attrSet = InspectorObject.GetComponent<AttributeSet>();
            				if (attrSet != null) {
            					attrSet.attributes.Clear();
            					for (int i = 0; i < mlAttributeCount; i++) {
            						attrSet.attributes.Add(mlAttributes[i]);
            					}
            				}
            			}

            			for (int i = 0; i < mlAttributeCount; i++) {
            				voxml.Attributes.Attrs.Add(new VoxAttributesAttr());
            				voxml.Attributes.Attrs[i].Value = mlAttributes[i];
            			}
            		}

            		voxml.Save(Data.voxmlDataPath + "/" + markupPath + ".xml");
            		//		voxml.SaveToServer (obj.name + ".xml");

            		windowManager.OnActiveWindowSaved(this, new VoxMLEventArgs(InspectorObject, voxml));
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
            		mlHead = voxml.Type.Head.Split('[')[0];
            		mlHeadReentrancy = voxml.Type.Head.Contains("[") ? voxml.Type.Head.Split('[')[1].Replace("]", "") : "";
            		mlComponents = new List<string>();
            		foreach (VoxTypeComponent c in voxml.Type.Components) {
            			mlComponents.Add(c.Value.Split('[')[0]);
            			mlComponentReentrancies.Add(c.Value.Contains("[") ? c.Value.Split('[')[1].Replace("]", "") : "");
            		}

            		mlComponentCount = mlComponents.Count;
            		mlConcavity = voxml.Type.Concavity.Split('[')[0];
            		mlConcavityReentrancy =
            			voxml.Type.Concavity.Contains("[") ? voxml.Type.Concavity.Split('[')[1].Replace("]", "") : "";

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

            		mlClass = voxml.Type.Class;
            		mlValue = voxml.Type.Value;
            		mlConstr = voxml.Type.Constr;

            		mlTypeScale = voxml.Type.Scale;
            		mlArity = voxml.Type.Arity;

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
            		mlEmbodimentScale = voxml.Embodiment.Scale;
            		mlMovable = voxml.Embodiment.Movable;

            		// ATTRIBUTES
            		mlAttributes = new List<string>();
            		foreach (VoxAttributesAttr a in voxml.Attributes.Attrs) {
            			Debug.Log(a.Value);
            			mlAttributes.Add(a.Value);
            		}

            		mlAttributeCount = mlAttributes.Count;

            		if (mlEntityType == VoxEntity.EntityType.Object) {
            			if (InspectorObject != null) {
            				AttributeSet attrSet = InspectorObject.GetComponent<AttributeSet>();
            				if (attrSet != null) {
            					attrSet.attributes.Clear();
            					for (int i = 0; i < mlAttributeCount; i++) {
            						attrSet.attributes.Add(mlAttributes[i]);
            					}
            				}
            			}
            		}
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
    }
}