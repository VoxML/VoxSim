namespace VoxSimPlatform {
    namespace Episteme {
    	public enum PropertyType {
    		Nominal,
    		Ordinal
    	};

    	public class PropertyGroup {
    		private string _name;
    		private PropertyType _type;

    		public PropertyGroup(string name, PropertyType type) {
    			_name = name;
    			_type = type;
    		}

    		public string Name {
    			get { return _name; }
    			set { _name = value; }
    		}

    		public PropertyType Type {
    			get { return _type; }
    			set { _type = value; }
    		}
    	}
    }
}