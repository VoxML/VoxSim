#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(AssetBundleBuilder))]
public class AssetBundleBuilder : Editor {
	
	public override void OnInspectorGUI () 
	{
		base.OnInspectorGUI();
		
		if (GUILayout.Button ("Build Asset Bundles", GUILayout.Height (30))) {
			BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
		}
	}
}
#endif