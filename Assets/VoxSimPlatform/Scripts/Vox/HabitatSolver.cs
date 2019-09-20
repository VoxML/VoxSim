using UnityEngine;
using System.Text.RegularExpressions;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        public class HabitatSolver : MonoBehaviour {
        	// Use this for initialization
        	void Start() {
        	}

        	// IN: GameObject args[0]: the object in question
        	//	string args[1]: the object's axis
        	//	string args[2]: the world axis
        	public bool align(object[] args) {
        		GameObject obj = null;
        		string axis1str = string.Empty, axis2str = string.Empty;
        		Vector3 axis1 = Vector3.zero, axis2 = Vector3.zero;

        		if (args[0] is GameObject) {
        			obj = (args[0] as GameObject);
        		}

        		if (args[1] is string) {
        			axis1str = (args[1] as string);
        			if (Constants.Axes.ContainsKey(axis1str)) {
        				axis1 = obj.transform.rotation * Constants.Axes[axis1str];
        			}
        		}

        		if (args[2] is string) {
        			axis2str = (args[2] as string);
        			if (Constants.Axes.ContainsKey(axis2str.Replace("E_", string.Empty).ToUpper())) {
        				axis2 = Constants.Axes[axis2str.Replace("E_", string.Empty).ToUpper()];
        			}
        		}

        		Debug.Log(string.Format("{0}.align({1},{2})", obj.name, axis1, axis2));
        		//Debug.Log (Vector3.Dot(axis1,axis2));

        		bool r = Mathf.Abs(Mathf.Abs(Vector3.Dot(axis1, axis2)) - 1) < Constants.EPSILON;
        		Debug.Log(r);
        		return r;
        	}

        	// IN: GameObject args[0]: the object in question
        	//	string args[1]: the world axis vector to test against
        	public bool top(object[] args) {
        		GameObject obj = null;
        		string axisStr = string.Empty;
        		Vector3 axis = Vector3.zero;
        		Regex signs = new Regex(@"[+-]");

        		if (args[0] is GameObject) {
        			obj = (args[0] as GameObject);
        		}

        		if (args[1] is string) {
        			axisStr = (args[1] as string);
        			if (Constants.Axes.ContainsKey(signs.Replace(axisStr, string.Empty))) {
        				axis = Constants.Axes[signs.Replace(axisStr, string.Empty)];
        				if (axisStr[0] == '-') {
        					axis = -axis;
        				}
        			}
        		}

        		Debug.Log(obj.transform.up);

        		Debug.Log(string.Format("{0}.top({1})", obj.name, axis));
        		bool r = Mathf.Abs(Vector3.Dot(obj.transform.up, axis) - 1) < Constants.EPSILON;
        		Debug.Log(r);
        		return r;
        	}
        }
    } 
}