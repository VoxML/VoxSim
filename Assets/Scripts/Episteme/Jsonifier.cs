using System.Collections.Generic;
using System.Linq;

namespace Episteme
{
	public static class Jsonifier
	{
		private static readonly string CertaintySep = "::";
		private static readonly string ConceptIdSep = ":";
		private static readonly string JsonRelationSuffix = "-relations";
		private static readonly string JsonSubgroupSuffix = "-subgroups";
		private static readonly string JsonRelationConnector = "-";

		public static string JsonifyConceptDefinitions(Concepts collection)
		{
			var relationStrings = new List<string>();
			var conceptStrings = new List<string>();
			var concepts = collection.GetConcepts();
			foreach (var mode in concepts.Keys)
			{
				foreach (var c in concepts[mode])
				{
					var subgroupString = "";
                    if (collection.Type() == ConceptType.PROPERTY && !string.IsNullOrEmpty(c.SubgroupName))
					{
						subgroupString = string.Format(", \"subgroup\": \"{0}\"", c.SubgroupName);
					}
					var conceptString = string.Format("{{\"name\":\"{0}\", \"modality\": \"{1}\"{2}}}",
						c.Name,
						c.Mode.ToString(),
						subgroupString);
					
				
					conceptStrings.Add(conceptString);
				}
				foreach (var r in collection.GetRelations())
				{

					relationStrings.Add(string.Format("\"{0}{4}{1}{5}{2}{4}{3}\"",
						r.Origin.Mode.ToString(),
						collection.GetIndex(r.Origin),
						r.Destination.Mode.ToString(),
						collection.GetIndex(r.Destination),
						ConceptIdSep,
						JsonRelationConnector));
					if (r.Bidirectional)
					{
						relationStrings.Add(string.Format("\"{0}{4}{1}{5}{2}{4}{3}\"",
							r.Destination.Mode.ToString(),
							collection.GetIndex(r.Destination),
							r.Origin.Mode.ToString(),
							collection.GetIndex(r.Origin),
							ConceptIdSep,
							JsonRelationConnector));
					}
				}
			}
			var jsonString = string.Format("\"{0}\":[{1}], \"{0}{3}\": [{2}]",
				collection.Type(),
				string.Join(", ", conceptStrings.ToArray()),
				string.Join(", ", relationStrings.ToArray()),
				JsonRelationSuffix
			);
			if (collection.Type() == ConceptType.PROPERTY)
			{
				var subgroupStrings = new List<string>();
				subgroupStrings.AddRange(collection.Subgroups.Select(g =>
					string.Format("{{\"name\":\"{0}\", \"type\": \"{1}\"}}",
						g.Name,
						g.Type.ToString())));
				jsonString += ", " + string.Format("\"{0}{1}\":[{2}]",
								  collection.Type(),
								  JsonSubgroupSuffix,
								  string.Join(", ", subgroupStrings.ToArray()));
			}
			return jsonString;
		}

		public static string JsonifyEpistemicStateInitiation(EpistemicState collections)
		{
			return string.Format("{{{0}}}", 
				string.Join(", ", collections.GetAllConcepts().Select(
					JsonifyConceptDefinitions).ToArray()));
		}

		public static string JsonifyUpdatedConcepts(EpistemicState state, params Concept[] concepts)
		{
			if (concepts.Length <= 0) return "[]";
			var updatedConceptIndices = new string[concepts.Length];
			for (int i = 0; i < concepts.Length; i++)
			{
				var concept = concepts[i];
				var collection = state.GetConcepts(concept.Type);
				updatedConceptIndices[i] = 
					string.Format("\"{0}-{1}-{2}{4}{3:0.00}\"",
						(int)concept.Type,
						(int)concept.Mode,
						collection.GetIndex(concept),
						concept.Certainty,
						CertaintySep);
			}
			return string.Format("[{0}]", string.Join(", ", updatedConceptIndices));
		}
		
		public static string JsonifyUpdatedRelations(EpistemicState state, params Relation[] relations)
		{
			if (relations.Length <= 0) return "[]";
			var collection = state.GetConcepts(relations[0].Origin.Type);
			return string.Format("[{0}]", string.Join(", ",
				relations.Select(relation =>
					string.Format("\"{0}-{1}-{2}-{3}-{4}{6}{5:0.00}\"",
						(int) collection.Type(),
						(int) relation.Origin.Mode,
						collection.GetIndex(relation.Origin),
						(int)  relation.Destination.Mode,
						collection.GetIndex(relation.Destination),
						relation.Certainty,
						CertaintySep
					)).ToArray()));
		}

		public static string JsonifyUpdates(EpistemicState state, Concept[] concepts, Relation[] relations)
		{
			return string.Format("{{\"c\": {0}, \"r\": {1}}}",
				JsonifyUpdatedConcepts(state, concepts),
				JsonifyUpdatedRelations(state, relations));
		}

	}
}