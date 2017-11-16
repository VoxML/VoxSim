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
		private RestClient _restClient;
		private string _episimUrl;

		private static readonly string EpisimInitRouth = "init";
		private static readonly string EpisimUpdateRouth = "aware";
		
		public EpistemicState()
		{
			_episteme = new Dictionary<ConceptType, Concepts>();
			foreach (ConceptType type in Enum.GetValues(typeof(ConceptType)))
			{
				_episteme.Add(type, new Concepts(type));
			}
			var obj = new GameObject();
			_restClient = obj.AddComponent<RestClient>();
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
			if (!url.EndsWith("/"))
			{
				url += "/";
			}
			_episimUrl = url;
		}
		
		public void InitiateEpisim()
		{
			_restClient.Post(_episimUrl + EpisimInitRouth, Jsonifier.JsonifyEpistemicState(this),"okay", "error");
		}

		public void UpdateEpisim(Concept[] updatedConcepts, Relation[] updatedRelations)
		{
			_restClient.Post(_episimUrl + EpisimUpdateRouth, Jsonifier.JsonifyUpdates(this, updatedConcepts, updatedRelations),"okay", "error");
		}
	}
}