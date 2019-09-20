namespace VoxSimPlatform {
    namespace Episteme {
    	public class Relation {
    		private Concept _origin;
    		private Concept _destination;
    		private bool _bidirectional;
    		private double _certainty;

    		public Relation(Concept origin, Concept destination) {
    			_origin = origin;
    			_destination = destination;
    			_bidirectional = false;
    			_certainty = 0;
    		}

    		public Concept Origin {
    			get { return _origin; }
    			set { _origin = value; }
    		}

    		public Concept Destination {
    			get { return _destination; }
    			set { _destination = value; }
    		}

    		public bool Bidirectional {
    			get { return _bidirectional; }
    			set { _bidirectional = value; }
    		}

    		public double Certainty {
    			get { return _certainty; }
    			set { _certainty = value; }
    		}
    	}
    }
}