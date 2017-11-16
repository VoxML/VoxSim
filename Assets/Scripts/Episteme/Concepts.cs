using System.Collections.Generic;
using System.Linq;

namespace Episteme
{
	public class Concepts
	{
		private ConceptType _type;
		private Dictionary<ConceptMode, List<Concept>> _concepts;
		private List<Relation> _relations;

		public Concepts(ConceptType type)
		{
			_type = type;
			_concepts = new Dictionary<ConceptMode, List<Concept>>();
			_relations = new List<Relation>();
		}

		public ConceptType Type()
		{
			return _type;
		}

		public int Add(Concept concept)
		{
			if (!_concepts.ContainsKey(concept.Mode))
			{
				_concepts.Add(concept.Mode, new List<Concept>());
			}
			_concepts[concept.Mode].Add(concept);
			return _concepts[concept.Mode].Count;
		}

		public int GetIndex(Concept concept)
		{
			return _concepts[concept.Mode].IndexOf(concept);
		}

		public void Link(Concept concept1, Concept concept2)
		{
			concept1.Relate(concept2);
			_relations.Add(new Relation(concept1, concept2));
		}

		public void MutualLink(Concept c1, Concept c2)
		{
			c1.Relate(c2);
			c2.Relate(c1);
			var relation = new Relation(c1, c2) {Bidirectional = true};
			_relations.Add(relation);
		}
		
		public List<Relation> GetRelations()
		{
			return _relations;
		}
		
		public Dictionary<ConceptMode, List<Concept>> GetConcepts()
		{
			return _concepts;
		}
		
	}
}