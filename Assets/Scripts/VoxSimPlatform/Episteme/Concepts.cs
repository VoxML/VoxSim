using System.Collections.Generic;
using System.Linq;

namespace VoxSimPlatform {
    namespace Episteme {
    	public class Concepts {
    		private ConceptType _type;
    		private Dictionary<ConceptMode, List<Concept>> _concepts;
    		private List<Relation> _relations;
    		private List<PropertyGroup> _subgroups;

    		public Concepts(ConceptType type) {
    			_type = type;
    			_concepts = new Dictionary<ConceptMode, List<Concept>>();
    			_relations = new List<Relation>();
    			_subgroups = new List<PropertyGroup>();
    		}

    		public List<PropertyGroup> Subgroups {
    			get { return _subgroups; }
    		}

    		public ConceptType Type() {
    			return _type;
    		}

    		public int Add(Concept concept) {
    			if (!_concepts.ContainsKey(concept.Mode)) {
    				_concepts.Add(concept.Mode, new List<Concept>());
    			}

    			if (!_concepts[concept.Mode].Contains(concept)) {
    				_concepts[concept.Mode].Add(concept);
    			}

    			return _concepts[concept.Mode].Count;
    		}

    		public void AddSubgroup(PropertyGroup group) {
    			_subgroups.Add(group);
    		}

    		public void AddSubgroup(string name, PropertyType type) {
    			AddSubgroup(new PropertyGroup(name, type));
    		}

    		public Concept GetConcept(string name, ConceptMode mode) {
    			var found = _concepts[mode].First(c => c.Name == name);
    			return found;
    		}

    		public Concept GetConcept(int conceptIdx, ConceptMode mode) {
    			return _concepts[mode][conceptIdx];
    		}

    		public int GetIndex(Concept concept) {
    			return _concepts[concept.Mode].IndexOf(concept);
    		}

    		public void Link(Concept concept1, Concept concept2) {
    			concept1.Relate(concept2);
    			_relations.Add(new Relation(concept1, concept2));
    		}

    		public void MutualLink(Concept c1, Concept c2) {
    			c1.Relate(c2);
    			c2.Relate(c1);
    			var relation = new Relation(c1, c2) {Bidirectional = true};
    			_relations.Add(relation);
    		}

    		public List<Concept> GetRelated(Concept origin) {
    			List<Concept> related = new List<Concept>();
    			foreach (var relation in _relations) {
    				if (Equals(relation.Origin, origin)) {
    					related.Add(relation.Destination);
    				}
    				else if (Equals(relation.Destination, origin) && relation.Bidirectional) {
    					related.Add(relation.Origin);
    				}
    			}

    			return related;
    		}

    		public Relation GetRelation(Concept ori, Concept dest) {
    			foreach (var relation in _relations) {
    				if (
    					(Equals(relation.Origin, ori) && Equals(relation.Destination, dest)) ||
    					(Equals(relation.Origin, dest) && Equals(relation.Destination, ori) && relation.Bidirectional)) {
    					return relation;
    				}
    			}

    			return null;
    		}

    		public List<Relation> GetRelations() {
    			return _relations;
    		}

    		public Dictionary<ConceptMode, List<Concept>> GetConcepts() {
    			return _concepts;
    		}
    	}
    }
}