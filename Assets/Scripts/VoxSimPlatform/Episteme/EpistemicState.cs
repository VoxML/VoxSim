using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using SimpleJSON;
using VoxSimPlatform.Episteme;
using VoxSimPlatform.Network;

public class EpistemicState : RestClient {
	private Dictionary<ConceptType, Concepts> _episteme;
	//private GameObject _restClient;
	//private RestClient client;
	private string _episimUrl;

	private static readonly string EpisimInitRoute = "init";
	private static readonly string EpisimUpdateRoute = "aware";

	public EpistemicState() {
        clientType = typeof(EpiSimIOClient);

		_episteme = new Dictionary<ConceptType, Concepts>();
		foreach (ConceptType type in Enum.GetValues(typeof(ConceptType))) {
			_episteme.Add(type, new Concepts(type));
		}
	}

	public void AddPropertyGroup(PropertyGroup group) {
		_episteme[ConceptType.PROPERTY].AddSubgroup(group);
	}

	public Concept GetConcept(Concept c) {
		return _episteme[c.Type].GetConcept(c.Name, c.Mode);
	}

	public Concept GetConcept(string name, ConceptType type, ConceptMode mode) {
		return _episteme[type].GetConcept(name, mode);
	}

	public void SetCertainty(Concept c, double certainty) {
		GetConcept(c).Certainty = certainty;
	}

	public void SetCertainty(Concept origin, Concept destination, double certainty) {
		GetRelation(origin, destination).Certainty = certainty;
	}

	public Relation GetRelation(Concept origin, Concept destination) {
		return _episteme[origin.Type].GetRelation(origin, destination);
	}

	public List<Concept> GetRelated(Concept origin) {
		return _episteme[origin.Type].GetRelated(origin);
	}

	public void AddConcept(Concept c) {
		_episteme[c.Type].Add(c);
	}

	public void AddRelation(Concept origin, Concept destination, bool bidirectional) {
		if (origin.Type == destination.Type) {
			if (bidirectional) {
				_episteme[origin.Type].MutualLink(origin, destination);
			}
			else {
				_episteme[origin.Type].Link(origin, destination);
			}
		}
	}

	public List<Concepts> GetAllConcepts() {
		return _episteme.Values.ToList();
	}

	public Concepts GetConcepts(ConceptType type) {
		return _episteme[type];
	}

	public void UpdateConcepts(Concepts concepts) {
		_episteme[concepts.Type()] = concepts;
	}

	public void SetEpisimUrl(string url) {
		//if (_restClient == null) {
		//	_restClient = new GameObject("RestClient");
		//	_restClient.AddComponent<RestClient>();
		//	client = _restClient.GetComponent<RestClient>();
		//	client.PostError += ConnectionLost;
		//}

		if (!url.EndsWith("/")) {
			url += "/";
		}

		_episimUrl = url;
	}

	public void InitiateEpisim() {
		Post(EpisimInitRoute, Jsonifier.JsonifyEpistemicStateInitiation(this));
	}

	public void DisengageEpisim() {
		Post(EpisimUpdateRoute, "0");
	}

	public void UpdateEpisimNewGesture(string gestureId, string gestureLabel) {
		if (isConnected) {
			Post(EpisimUpdateRoute,
				string.Format("{{\"l\": [ {{ \"id\": \"{0}\", \"str\": \"{1}\" }} ] }}", gestureId, gestureLabel));
		}
	}

	public void UpdateEpisim(Concept[] updatedConcepts, Relation[] updatedRelations) {
		if (isConnected) {
			Post(EpisimUpdateRoute,
				Jsonifier.JsonifyUpdates(this, updatedConcepts, updatedRelations));
		}
	}

	public void SideloadCertaintyState(string certaintyJson) {
		UpdateEpisim(certaintyJson);
		var certainties = JSON.Parse(certaintyJson);
		JSONArray cCertainties = certainties["c"].AsArray;
		foreach (string cCertainty in cCertainties.Values) {
			string[] split1 = cCertainty.Split(Jsonifier.CertaintySep);
			string conceptString = split1[0];
			string certaintyString = split1[1];

			string[] split2 = conceptString.Split(Jsonifier.JsonRelationConnector);
			ConceptType type = (ConceptType) Int32.Parse(split2[0]);
			ConceptMode mode = (ConceptMode) Int32.Parse(split2[1]);
			int idx = Int32.Parse(split2[2]);

			GetConcepts(type).GetConcept(idx, mode).Certainty = Double.Parse(certaintyString);
		}

		JSONArray rCertainties = certainties["r"].AsArray;
		foreach (string rCertainty in rCertainties.Values) {
			string[] split1 = rCertainty.Split(Jsonifier.CertaintySep);
			string relationString = split1[0];
			string certaintyString = split1[1];

			string[] split2 = relationString.Split(Jsonifier.JsonRelationConnector);
			ConceptType type = (ConceptType) Int32.Parse(split2[0]);
			ConceptMode oMode = (ConceptMode) Int32.Parse(split2[1]);
			int oIdx = Int32.Parse(split2[2]);
			ConceptMode dMode = (ConceptMode) Int32.Parse(split2[3]);
			int dIdx = Int32.Parse(split2[4]);

			Concept origin = GetConcepts(type).GetConcept(oIdx, oMode);
			Concept destination = GetConcepts(type).GetConcept(dIdx, dMode);
			GetRelation(origin, destination).Certainty = Double.Parse(certaintyString);
		}
	}

	public void UpdateEpisim(string updateJsonString) {
		if (isConnected) {
			Post(EpisimUpdateRoute, updateJsonString);
		}
	}
}