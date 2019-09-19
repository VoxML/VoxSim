using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;
using VoxSimPlatform.CogPhysics;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;
using VoxSimPlatform.UI.ModalWindow;
using VoxSimPlatform.Vox;

public class ModuleObjectCreation : ModalWindow {
	public int fontSize = 12;

	GUIStyle buttonStyle = new GUIStyle("Button");

	float fontSizeModifier;

	public float FontSizeModifier {
		get { return fontSizeModifier; }
		set { fontSizeModifier = value; }
	}

	string[] listItems;

	List<string> objects = new List<string>();

	public List<string> Objects {
		get { return objects; }
		set {
			objects = value;
			listItems = objects.ToArray();
		}
	}

	enum PlacementState {
		Add,
		Place,
		Delete
	};

	PlacementState placementState;

	enum ShaderType {
		Default,
		Highlight
	};

	Object[] prefabs;

	int selected = -1;
	GameObject selectedObject;

	string actionButtonText;

	public GameObject sandboxSurface;

	Dictionary<Renderer, Shader> defaultShaders;
	Shader highlightShader;

	ObjectSelector objSelector;
	VoxemeInit voxemeInit;
    VoxMLLibrary voxmlLibrary;
	Predicates preds;

	GhostFreeRoamCamera cameraControl;

	RaycastHit selectRayhit;
	float surfacePlacementOffset;

	// Use this for initialization
	void Start() {
		base.Start();

		actionButtonText = "Add Object";
		windowTitle = "Add Voxeme Object";
		persistent = true;

		buttonStyle = new GUIStyle("Button");

		fontSizeModifier = (int) (fontSize / defaultFontSize);
		buttonStyle.fontSize = fontSize;

		objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
		voxemeInit = GameObject.Find("VoxWorld").GetComponent<VoxemeInit>();
        voxmlLibrary = GameObject.Find("VoxWorld").GetComponent<VoxMLLibrary>();
		preds = GameObject.Find("BehaviorController").GetComponent<Predicates>();
		//windowManager = GameObject.Find ("VoxWorld").GetComponent<ModalWindowManager> ();

		cameraControl = Camera.main.GetComponent<GhostFreeRoamCamera>();

		prefabs = Resources.LoadAll("DemoObjects");
		foreach (Object prefab in prefabs) {
			Objects.Add(prefab.name);
		}

		defaultShaders = new Dictionary<Renderer, Shader>();
		highlightShader = Shader.Find("Legacy Shaders/Self-Illumin/Parallax Diffuse");

		listItems = Objects.ToArray();

		windowRect = new Rect(Screen.width - 215, Screen.height - (35 + (int) (20 * fontSizeModifier)) - 205, 200, 200);

		windowManager.NewModalWindow += NewInspector;
		windowManager.ActiveWindowSaved += VoxMLUpdated;
	}

	// Update is called once per frame
	void Update() {
		if (sandboxSurface != Helper.GetMostImmediateParentVoxeme(sandboxSurface)) {
			sandboxSurface = Helper.GetMostImmediateParentVoxeme(sandboxSurface);
		}

		if (placementState == PlacementState.Delete) {
			if (Input.GetMouseButtonDown(0)) {
				if (Helper.PointOutsideMaskedAreas(
					new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y),
					new Rect[] {
						new Rect(Screen.width - (15 + (int) (110 * fontSizeModifier / 3)) + 38 * fontSizeModifier - 60,
							Screen.height - (35 + (int) (20 * fontSizeModifier)),
							60, 20 * fontSizeModifier)
					})) {
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					// Casts the ray and get the first game object hit
					Physics.Raycast(ray, out selectRayhit);
					if (selectRayhit.collider != null) {
						if (selectRayhit.collider.gameObject.transform.root.gameObject == selectedObject) {
							DeleteVoxeme(selectedObject);
							actionButtonText = "Add Object";
							placementState = PlacementState.Add;
							selected = -1;
							cameraControl.allowRotation = true;
						}
					}
				}
			}
		}
		else if (placementState == PlacementState.Place) {
			if (Input.GetMouseButton(0)) {
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				// Casts the ray and get the first game object hit
				Physics.Raycast(ray, out selectRayhit);
				if (selectRayhit.collider != null) {
					if (selectRayhit.collider.gameObject.transform.root.gameObject == sandboxSurface) {
						Debug.Log(selectRayhit.point.y);
						if (Mathf.Abs(selectRayhit.point.y - ((Vector3)preds.ComposeRelation(voxmlLibrary.VoxMLObjectDict["on"], new object[] {sandboxSurface})).y) <=
						    Constants.EPSILON) {
							if ((Mathf.Abs(selectRayhit.point.x - Helper.GetObjectWorldSize(sandboxSurface).min.x) >=
							     Helper.GetObjectWorldSize(selectedObject).extents.x) &&
							    (Mathf.Abs(selectRayhit.point.x - Helper.GetObjectWorldSize(sandboxSurface).max.x) >=
							     Helper.GetObjectWorldSize(selectedObject).extents.x) &&
							    (Mathf.Abs(selectRayhit.point.z - Helper.GetObjectWorldSize(sandboxSurface).min.z) >=
							     Helper.GetObjectWorldSize(selectedObject).extents.z) &&
							    (Mathf.Abs(selectRayhit.point.z - Helper.GetObjectWorldSize(sandboxSurface).max.z) >=
							     Helper.GetObjectWorldSize(selectedObject).extents.z)) {
								selectedObject.transform.position = new Vector3(selectRayhit.point.x,
									((Vector3)preds.ComposeRelation(voxmlLibrary.VoxMLObjectDict["on"], new object[] {sandboxSurface})).y + surfacePlacementOffset,
									selectRayhit.point.z);
								Voxeme voxComponent = selectedObject.GetComponent<Voxeme>();
								voxComponent.targetPosition = selectedObject.transform.position;

								foreach (Voxeme child in voxComponent.children) {
									if (child.isActiveAndEnabled) {
										if (child.gameObject != voxComponent.gameObject) {
											child.transform.localPosition =
												voxComponent.parentToChildPositionOffset[child.gameObject];
											child.targetPosition = child.transform.position;
										}
									}
								}
							}
						}
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.Return)) {
				actionButtonText = "Add Object";
				placementState = PlacementState.Add;
				selected = -1;
				cameraControl.allowRotation = true;
				selectedObject.GetComponent<Rigging>().ActivatePhysics(true);
				SetShader(selectedObject, ShaderType.Default);
			}
		}
		else if (placementState == PlacementState.Add) {
			if (Input.GetMouseButton(0)) {
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					// Casts the ray and get the first game object hit
					Physics.Raycast(ray, out selectRayhit);
					if (selectRayhit.collider != null) {
						if (Helper.IsSupportedBy(selectRayhit.collider.gameObject.transform.root.gameObject,
							sandboxSurface)) {
							if (selectRayhit.collider.gameObject.transform.root.gameObject.GetComponent<Voxeme>() !=
							    null) {
								selectedObject = selectRayhit.collider.gameObject.transform.root.gameObject;
								surfacePlacementOffset =
									(Helper.GetObjectWorldSize(selectedObject.gameObject).center.y -
									 Helper.GetObjectWorldSize(selectedObject.gameObject).min.y) +
									(selectedObject.gameObject.transform.position.y -
									 Helper.GetObjectWorldSize(selectedObject.gameObject).center.y);
								SetShader(selectedObject, ShaderType.Highlight);
								actionButtonText = "Place";
								placementState = PlacementState.Place;
								cameraControl.allowRotation = false;

								if (selectedObject != null) {
									selectedObject.GetComponent<Rigging>().ActivatePhysics(false);
								}
							}
						}
					}
				}
			}
		}
	}

	protected override void OnGUI() {
		if (placementState == PlacementState.Place) {
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
				placementState = PlacementState.Delete;
				actionButtonText = "Delete";
			}
		}

		if (placementState == PlacementState.Delete) {
			if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) {
				placementState = PlacementState.Place;
				actionButtonText = "Place";
			}
		}

		if (GUI.Button(new Rect(
				Screen.width - (15 + (int) (110 * fontSizeModifier / 3)) + 38 * fontSizeModifier -
				(GUI.skin.label.CalcSize(new GUIContent(actionButtonText)).x + 10),
				Screen.height - (35 + (int) (20 * fontSizeModifier)),
				GUI.skin.label.CalcSize(new GUIContent(actionButtonText)).x + 10, 20 * fontSizeModifier),
			actionButtonText, buttonStyle)) {
			switch (actionButtonText) {
				case "Add Object":
					render = true;
					break;

				case "Place":
					actionButtonText = "Add Object";
					placementState = PlacementState.Add;
					selected = -1;
					cameraControl.allowRotation = true;
					selectedObject.GetComponent<Rigging>().ActivatePhysics(true);
					SetShader(selectedObject, ShaderType.Default);
					break;

				case "Delete":
					DeleteVoxeme(selectedObject);
					actionButtonText = "Add Object";
					placementState = PlacementState.Add;
					selected = -1;
					cameraControl.allowRotation = true;
					break;

				default:
					break;
			}
		}

		base.OnGUI();
	}

	public override void DoModalWindow(int windowID) {
		if (placementState != PlacementState.Add) {
			return;
		}

		base.DoModalWindow(windowID);

		//makes GUI window scrollable
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		selected = GUILayout.SelectionGrid(selected, listItems, 1, buttonStyle, GUILayout.ExpandWidth(true));
		GUILayout.EndScrollView();

		if (selected != -1) {
			render = false;

			GameObject go = (GameObject) Instantiate(prefabs[selected]);
			go.transform.position = Helper.FindClearRegion(sandboxSurface, go).center;
			Debug.Log(go.transform.position);
			go.SetActive(true);
			go.name = go.name.Replace("(Clone)", "");

			List<Voxeme> existingObjsOfType =
				objSelector.allVoxemes.FindAll(v => v.gameObject.name.StartsWith(go.name));
			List<int> objIndices = existingObjsOfType.Select(v => Convert.ToInt32(v.name.Replace(go.name, "0")))
				.ToList();
			for (int i = 0; i < objIndices.Count; i++) {
				if (objIndices[i] == 0) {
					objIndices[i] = 1;
				}
			}

			int j;
			for (j = 0; j < objIndices.Count; j++) {
				if (objIndices[j] != j + 1) {
					break;
				}
			}

			go.name = go.name + (j + 1);

			// store shaders
			foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>()) {
				defaultShaders[renderer] = renderer.material.shader;
			}

			voxemeInit.InitializeVoxemes();

//			Debug.Log (go);
//			foreach (Voxeme vox in objSelector.allVoxemes) {
//				Debug.Log (vox.gameObject);
//			}
			selectedObject = objSelector.allVoxemes.Find(v => v.gameObject.transform.Find(go.name) != null).gameObject;
			selectedObject.GetComponent<Voxeme>().VoxMLLoaded += VoxMLUpdated;

			surfacePlacementOffset = (Helper.GetObjectWorldSize(selectedObject.gameObject).center.y -
			                          Helper.GetObjectWorldSize(selectedObject.gameObject).min.y) +
			                         (selectedObject.gameObject.transform.position.y -
			                          Helper.GetObjectWorldSize(selectedObject.gameObject).center.y);
			selectedObject.transform.position = new Vector3(go.transform.position.x,
				((Vector3)preds.ComposeRelation(voxmlLibrary.VoxMLObjectDict["on"], new object[] {sandboxSurface})).y + surfacePlacementOffset,
				go.transform.position.z);
			SetShader(selectedObject, ShaderType.Highlight);
			actionButtonText = "Place";
			placementState = PlacementState.Place;
			cameraControl.allowRotation = false;
			selectedObject.GetComponent<Rigging>().ActivatePhysics(false);
		}
	}

	void SetShader(GameObject obj, ShaderType shaderType) {
		switch (shaderType) {
			case ShaderType.Default:
				foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
					renderer.material.shader = defaultShaders[renderer];
				}

				break;

			case ShaderType.Highlight:
				foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) {
					renderer.material.shader = highlightShader;
				}

				break;
		}
	}

	void DeleteVoxeme(GameObject obj) {
		foreach (Voxeme child in obj.GetComponentsInChildren<Voxeme>()) {
			objSelector.allVoxemes.Remove(objSelector.allVoxemes.Find(v => v == child));
		}

		Destroy(obj);
	}

	void NewInspector(object sender, EventArgs e) {
		if (placementState != PlacementState.Add) {
			Debug.Log(((ModalWindowEventArgs) e).WindowID);
			Debug.Log(sender);
			((ModalWindowManager) sender).windowManager[((ModalWindowEventArgs) e).WindowID].DestroyWindow();
		}
	}

	void VoxMLUpdated(object sender, EventArgs e) {
		GameObject voxeme = ((VoxMLEventArgs) e).Voxeme;
		VoxML voxml = ((VoxMLEventArgs) e).VoxML;

		if (voxeme != null) {
			if (voxeme.GetComponent<AttributeSet>() != null) {
				Debug.Log(voxeme.GetComponent<AttributeSet>().attributes.Count);
				foreach (string attr in voxeme.GetComponent<AttributeSet>().attributes) {
					Material newMat = Resources.Load(string.Format("DemoTextures/{0}", attr)) as Material;
					if (newMat != null) {
						Debug.Log(newMat);
						foreach (Renderer renderer in voxeme.GetComponentsInChildren<Renderer>()) {
							Shader shader = renderer.material.shader;
							renderer.material = newMat;
							renderer.material.shader = shader;
						}
					}
				}
			}
		}
	}
}