using System.Collections.Generic;

namespace VoxSimPlatform {
    namespace Episteme {
    	public enum ConceptMode {
    		L,
    		G
    	};

    	public enum ConceptType {
    		ACTION,
    		PROPERTY,
    		OBJECT
    	};

    	public class Concept {
    		private string _name;
    		private ConceptType _type;
    		private ConceptMode _mode;
    		private List<Concept> _related;
    		private double _certainty;
    		private string _subgroupNameName;

    		public Concept(string name, ConceptType type, ConceptMode mode) {
    			_name = name;
    			_type = type;
    			_mode = mode;
    			_related = new List<Concept>();
    			_certainty = 0;
    			_subgroupNameName = null;
    		}

    		public string SubgroupName {
    			get { return _subgroupNameName; }
    			set { _subgroupNameName = value; }
    		}

    		public double Certainty {
    			get { return _certainty; }
    			set { _certainty = value; }
    		}

    		public List<Concept> Related {
    			get { return _related; }
    			set { _related = value; }
    		}

    		public string Name {
    			get { return _name; }
    			set { _name = value; }
    		}

    		public ConceptType Type {
    			get { return _type; }
    			set { _type = value; }
    		}

    		public ConceptMode Mode {
    			get { return _mode; }
    			set { _mode = value; }
    		}

    		public void Relate(Concept other) {
    			_related.Add(other);
    		}


    		protected bool Equals(Concept other) {
    			return string.Equals(_name, other._name) && _type == other._type && _mode == other._mode;
    		}

    		public override bool Equals(object obj) {
    			if (ReferenceEquals(null, obj)) return false;
    			if (ReferenceEquals(this, obj)) return true;
    			if (obj.GetType() != this.GetType()) return false;
    			return Equals((Concept) obj);
    		}

    		public override int GetHashCode() {
    			unchecked {
    				var hashCode = (_name != null ? _name.GetHashCode() : 0);
    				hashCode = (hashCode * 397) ^ (int) _type;
    				hashCode = (hashCode * 397) ^ (int) _mode;
    				return hashCode;
    			}
    		}

    		public override string ToString() {
    			return _type + "::" + _name + "::" + _mode + "::" + _certainty;
    		}
    	}
    }
}