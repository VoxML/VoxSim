using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxSimPlatform {
    namespace Agent {
        public class ReferentStore : MonoBehaviour {
        	public enum ReferentType {
        		Object,
        		Location
        	};

        	public Stack<object> stack;

        #if UNITY_EDITOR
        	[CustomEditor(typeof(ReferentStore))]
        	public class DebugPreview : Editor {
        		public override void OnInspectorGUI() {
        			var bold = new GUIStyle();
        			bold.fontStyle = FontStyle.Bold;

        			GUILayout.Label("Stack", bold);

        			// add a label for each item, you can add more properties
        			// you can even access components inside each item and display them
        			// for example if every item had a sprite we could easily show it 
        			if (((ReferentStore) target).stack != null) {
        				foreach (object item in ((ReferentStore) target).stack) {
        					GUILayout.BeginHorizontal();
        					GUILayout.Label(item.ToString());
        					GUILayout.Label(item.GetType().ToString());
        					GUILayout.EndHorizontal();
        				}
        			}
        		}
        	}
        #endif

        	// Use this for initialization
        	void Start() {
        		stack = new Stack<object>();
        	}

        	// Update is called once per frame
        	void Update() {
        	}

        	List<object> MatchBy(ReferentType glType) {
        		List<object> matches = new List<object>();

        		return matches;
        	}
        }
    }
}