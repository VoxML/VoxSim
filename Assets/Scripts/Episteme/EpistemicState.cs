using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using UnityEngine;

namespace Episteme
{
	public class EpistemicState 
	{
		private Dictionary<ConceptType, Concepts> _episteme;
		private GameObject _restClient;
		private string _episimUrl;

		private static readonly string EpisimInitRoute = "init";
		private static readonly string EpisimUpdateRoute = "aware";
		
		public EpistemicState()
		{
			_episteme = new Dictionary<ConceptType, Concepts>();
			foreach (ConceptType type in Enum.GetValues(typeof(ConceptType)))
			{
				_episteme.Add(type, new Concepts(type));
			}
		}

		public void AddPropertyGroup(PropertyGroup group)
		{
			_episteme[ConceptType.PROPERTY].AddSubgroup(group);
		}

		public Concept GetConcept(Concept c)
		{
			return _episteme[c.Type].GetConcept(c.Name, c.Mode);
		}

		public Concept GetConcept(string name, ConceptType type, ConceptMode mode)
		{
			return _episteme[type].GetConcept(name, mode);
		}

		public Relation GetRelation(Concept origin, Concept destination)
		{
			return _episteme[origin.Type].GetRelation(origin, destination);
		}

		public List<Concept> GetRelated(Concept origin)
		{
			return _episteme[origin.Type].GetRelated(origin);
		}
		
		public void AddConcept(Concept c)
		{
			_episteme[c.Type].Add(c);
		}

		public void AddRelation(Concept origin, Concept destination, bool bidirectional)
		{
			if (origin.Type == destination.Type)
			{
				if (bidirectional)
				{
					_episteme[origin.Type].MutualLink(origin, destination);
				}
				else
				{
					_episteme[origin.Type].Link(origin, destination);
				}
			}
		}
		
		public List<Concepts> GetAllConcepts()
		{
			return _episteme.Values.ToList();
		}
		
		public Concepts GetConcepts(ConceptType type)
		{
			return _episteme[type];
		}
		
		public void UpdateConcepts(Concepts concepts)
		{
			_episteme[concepts.Type()] = concepts;
		}

		public void SetEpisimUrl(string url)
		{	
			if (_restClient == null)
			{
				_restClient = new GameObject("RestClient");
				_restClient.AddComponent<RestClient>();
			}
			if (!url.EndsWith("/"))
			{
				url += "/";
			}
			_episimUrl = url;
		}
		
		public void InitiateEpisim()
		{
			_restClient.GetComponent<RestClient>().Post(_episimUrl + EpisimInitRoute, Jsonifier.JsonifyEpistemicStateInitiation(this),"okay", "error");
		}

		public void DisengageEpisim()
		{
			_restClient.GetComponent<RestClient>().Post(_episimUrl + EpisimUpdateRoute, "0","okay", "error");
		}


		public void UpdateEpisim(Concept[] updatedConcepts, Relation[] updatedRelations)
		{
			_restClient.GetComponent<RestClient>().Post(_episimUrl + EpisimUpdateRoute, Jsonifier.JsonifyUpdates(this, updatedConcepts, updatedRelations),"okay", "error");
		}
	}
}