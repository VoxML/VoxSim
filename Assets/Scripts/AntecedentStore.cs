using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AntecedentStore : MonoBehaviour
{
    public enum AntecedentType
    {
        Object,
        Location
    };

    public Stack<object> stack;

#if UNITY_EDITOR
    [CustomEditor(typeof(AntecedentStore))]
    public class DebugPreview : Editor
    {
        public override void OnInspectorGUI()
        {
            var bold = new GUIStyle();
            bold.fontStyle = FontStyle.Bold;

            GUILayout.Label("Stack", bold);

            // add a label for each item, you can add more properties
            // you can even access components inside each item and display them
            // for example if every item had a sprite we could easily show it 
            if (((AntecedentStore)target).stack != null)
            {
                foreach (object item in ((AntecedentStore)target).stack)
                {
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
    void Start()
	{
        stack = new Stack<object>();
	}

	// Update is called once per frame
	void Update()
	{
			
	}

    List<object> MatchBy(AntecedentType glType)
    {
        List<object> matches = new List<object>();

        return matches;
    }
}
