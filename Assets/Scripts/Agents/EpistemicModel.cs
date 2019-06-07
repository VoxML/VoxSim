using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using VoxSimPlatform.Episteme;
using VoxSimPlatform.UI.ModalWindow;

public enum EpistemicCertaintyOperation {
	Increase,
	Decrease
}

public class EpistemicModel : MonoBehaviour {
	public EpistemicState state;

	public bool engaged;
	public bool reuseModel;

	UserNameModalWindow userNameModalWindow;

	bool idUser = false;
	public string userID = string.Empty;

	public static EpistemicState initModel() {
		EpistemicState state = new EpistemicState();
		// creating Concept instances (actions)
		// available types: ACTION, OBJECT, PROPERTY
		// available modes: L, G
		Concept pointG = new Concept("point", ConceptType.ACTION, ConceptMode.G);
		Concept deixis_thisL = new Concept("THIS", ConceptType.ACTION, ConceptMode.L);
		Concept deixis_thatL = new Concept("THAT", ConceptType.ACTION, ConceptMode.L);
		Concept deixis_thereL = new Concept("THERE", ConceptType.ACTION, ConceptMode.L);
		Concept grabG = new Concept("grab", ConceptType.ACTION, ConceptMode.G);
		Concept grabL = new Concept("GRAB", ConceptType.ACTION, ConceptMode.L);
		Concept moveG = new Concept("move", ConceptType.ACTION, ConceptMode.G);
		//Concept moveL = new Concept ("PUT", ConceptType.ACTION, ConceptMode.L);
		Concept pushG = new Concept("push", ConceptType.ACTION, ConceptMode.G);
		//Concept pushL = new Concept ("PUSH", ConceptType.ACTION, ConceptMode.L);

		Concept posackG = new Concept("posack", ConceptType.ACTION, ConceptMode.G);
		Concept posackL = new Concept("YES", ConceptType.ACTION, ConceptMode.L);
		Concept negackG = new Concept("negack", ConceptType.ACTION, ConceptMode.G);
		Concept negackL = new Concept("NO", ConceptType.ACTION, ConceptMode.L);

		// todo nevermind as "undo" and nothing as "cancel"
		Concept neverMindL = new Concept("NEVERMIND", ConceptType.ACTION, ConceptMode.L);
		Concept nothingL = new Concept("NOTHING", ConceptType.ACTION, ConceptMode.L);

		// add concepts to the epistemic model
		state.AddConcept(pointG);
		state.AddConcept(deixis_thisL);
		state.AddConcept(deixis_thatL);
		state.AddConcept(deixis_thereL);
		state.AddRelation(pointG, deixis_thisL, true);
		state.AddRelation(pointG, deixis_thatL, true);
		state.AddRelation(pointG, deixis_thereL, true);

		state.AddConcept(grabG);
		state.AddConcept(grabL);
		state.AddRelation(grabG, grabL, true);
		state.AddConcept(moveG);
//			state.AddConcept(moveL);
//			state.AddRelation(moveG, moveL, true);
		state.AddConcept(pushG);
//			state.AddConcept(pushL);
//			state.AddRelation(pushG, pushL, true);

		state.AddConcept(posackG);
		state.AddConcept(posackL);
		state.AddConcept(negackG);
		state.AddConcept(negackL);
		state.AddConcept(neverMindL);
		state.AddConcept(nothingL);
		// add relations between them, third boolean param is bidirectional
		state.AddRelation(posackG, posackL, true);
		state.AddRelation(negackG, negackL, true);

		state.AddPropertyGroup(new PropertyGroup("COLOR", PropertyType.Nominal));
		Concept red = new Concept("RED", ConceptType.PROPERTY, ConceptMode.L);
		Concept green = new Concept("GREEN", ConceptType.PROPERTY, ConceptMode.L);
		Concept yellow = new Concept("YELLOW", ConceptType.PROPERTY, ConceptMode.L);
		Concept orange = new Concept("ORANGE", ConceptType.PROPERTY, ConceptMode.L);
		Concept black = new Concept("BLACK", ConceptType.PROPERTY, ConceptMode.L);
		Concept purple = new Concept("PURPLE", ConceptType.PROPERTY, ConceptMode.L);
		Concept white = new Concept("WHITE", ConceptType.PROPERTY, ConceptMode.L);
		red.SubgroupName = "COLOR";
		green.SubgroupName = "COLOR";
		yellow.SubgroupName = "COLOR";
		orange.SubgroupName = "COLOR";
		black.SubgroupName = "COLOR";
		purple.SubgroupName = "COLOR";
		white.SubgroupName = "COLOR";
		state.AddConcept(red);
		state.AddConcept(green);
		state.AddConcept(yellow);
		state.AddConcept(orange);
		state.AddConcept(black);
		state.AddConcept(purple);
		state.AddConcept(white);

//			state.AddPropertyGroup(new PropertyGroup("SIZE", PropertyType.Ordinal));
//			Concept small = new Concept("SMALL", ConceptType.PROPERTY, ConceptMode.L);
//			Concept big = new Concept("BIG", ConceptType.PROPERTY, ConceptMode.L);
//			small.SubgroupName = "SIZE";
//			big.SubgroupName = "SIZE";
//			state.AddConcept(small);
//			state.AddConcept(big);

		state.AddPropertyGroup(new PropertyGroup("DIRECTION", PropertyType.Nominal));
		Concept left = new Concept("LEFT", ConceptType.PROPERTY, ConceptMode.L);
		Concept right = new Concept("RIGHT", ConceptType.PROPERTY, ConceptMode.L);
		Concept back = new Concept("BACK", ConceptType.PROPERTY, ConceptMode.L);
		Concept forward = new Concept("FRONT", ConceptType.PROPERTY, ConceptMode.L);
		Concept up = new Concept("UP", ConceptType.PROPERTY, ConceptMode.L);
		Concept down = new Concept("DOWN", ConceptType.PROPERTY, ConceptMode.L);
		left.SubgroupName = "DIRECTION";
		right.SubgroupName = "DIRECTION";
		back.SubgroupName = "DIRECTION";
		forward.SubgroupName = "DIRECTION";
		up.SubgroupName = "DIRECTION";
		down.SubgroupName = "DIRECTION";
		state.AddConcept(left);
		state.AddConcept(right);
		state.AddConcept(back);
		state.AddConcept(forward);
		state.AddConcept(up);
		state.AddConcept(down);

		Debug.Log(state);
		return state;
	}

	// Use this for initialization
	void Start() {
		engaged = false;

		if (reuseModel) {
			idUser = true;
			userNameModalWindow = gameObject.AddComponent<UserNameModalWindow>();
			userNameModalWindow.windowRect =
				new Rect(Screen.width / 2 - 185 / 2, Screen.height / 2 - 60 / 2, 185, 60);
			userNameModalWindow.Render = true;
			userNameModalWindow.AllowDrag = false;
			userNameModalWindow.AllowResize = false;
			userNameModalWindow.AllowForceClose = false;
			userNameModalWindow.UserNameEvent += IdentifyUser;
		}

		if (state == null) {
			state = initModel();
			Debug.Log(state);
		}


		if (PlayerPrefs.HasKey("URLs")) {
			string epiSimUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split('=')[0] == "EpiSim URL") {
					epiSimUrlString = url.Split('=')[1];
					string epiSimUrl = !epiSimUrlString.StartsWith("http://")
						? "http://" + epiSimUrlString
						: epiSimUrlString;
					state.SetEpisimUrl(epiSimUrl);
					state.InitiateEpisim();
					break;
				}
			}
		}
	}

	// Update is called once per frame
	void Update() {
	}

	public void AddNewConcept(Concept concept) {
	}

	void LoadUserModel(string path) {
		string savedCertainties = File.ReadAllText(path);
		Debug.Log(savedCertainties);
		state.SideloadCertaintyState(savedCertainties);
	}

	public void SaveUserModel(string userID) {
		List<Concept> stateConcepts = new List<Concept>();
		List<Relation> stateRelations = new List<Relation>();

		List<Concept> gestureConcepts = new List<Concept>();
		List<Concept> linguisticConcepts = new List<Concept>();

		if (state != null) {
			foreach (Concepts conceptsByMode in state.GetAllConcepts()) {
				if (conceptsByMode.GetConcepts().ContainsKey(ConceptMode.G)) {
					gestureConcepts = conceptsByMode.GetConcepts()[ConceptMode.G];
				}

				if (conceptsByMode.GetConcepts().ContainsKey(ConceptMode.L)) {
					linguisticConcepts = conceptsByMode.GetConcepts()[ConceptMode.L];
				}

				foreach (Concept gestureConcept in gestureConcepts) {
					if (!stateConcepts.Contains(gestureConcept)) {
						stateConcepts.Add(gestureConcept);

						foreach (Concept relatedConcept in state.GetRelated(gestureConcept)) {
							Relation relation = state.GetRelation(gestureConcept, relatedConcept);
							if (!stateRelations.Contains(relation)) {
								stateRelations.Add(relation);
							}
						}
					}
				}

				foreach (Concept linguisticConcept in linguisticConcepts) {
					if (!stateConcepts.Contains(linguisticConcept)) {
						stateConcepts.Add(linguisticConcept);

						foreach (Concept relatedConcept in state.GetRelated(linguisticConcept)) {
							Relation relation = state.GetRelation(linguisticConcept, relatedConcept);
							if (!stateRelations.Contains(relation)) {
								stateRelations.Add(relation);
							}
						}
					}
				}
			}

			string jsonifiedCertaintyState =
				Jsonifier.JsonifyUpdates(state, stateConcepts.ToArray(), stateRelations.ToArray());
			Debug.Log(jsonifiedCertaintyState);


			using (StreamWriter sw = new StreamWriter(GetUserModelPath(userID))) {
				sw.Write(jsonifiedCertaintyState);
			}
		}
	}

	string GetUserModelPath(string username) {
		string userModelLocation = @"EpiSim/UserModels";
		if (!Directory.Exists(userModelLocation)) {
			Directory.CreateDirectory(userModelLocation);
		}

		return string.Format(@"{0}/user-{1}.json", userModelLocation, username);
	}

	void IdentifyUser(object sender, EventArgs e) {
		string username = ((UserNameInfo) ((ModalWindowEventArgs) e).Data).Username;
		userNameModalWindow.CloseWindow((ModalWindowEventArgs) e);
		userID = username;

		string userModelPath = GetUserModelPath(username);
		if (File.Exists(userModelPath)) {
			// load user model
			LoadUserModel(userModelPath);
		}
	}
}