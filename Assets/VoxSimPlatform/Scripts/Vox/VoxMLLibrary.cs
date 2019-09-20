using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        /// <summary>
        /// This class creates a VoxMLEntityTypeDict with key value pairs of xml filename and entity type (folder name), 
        /// and a VoxMLObjectDict with key value pairs of xml filename and VoxML object. 
        /// </summary>
        public class VoxMLLibrary : MonoBehaviour {
            public Dictionary<string, string> VoxMLEntityTypeDict;
            public Dictionary<string, VoxML> VoxMLObjectDict;

#if UNITY_EDITOR
            [CustomEditor(typeof(VoxMLLibrary))]
            public class DebugPreview : Editor {
                public override void OnInspectorGUI() {
                    var bold = new GUIStyle();
                    bold.fontStyle = FontStyle.Bold;
                    // some styling for the header, this is optional
                    GUILayout.Label("VoxML Entity Types", bold);

                    // add a label for each item, you can add more properties
                    // you can even access components inside each item and display them
                    // for example if every item had a sprite we could easily show it 
                    if (((VoxMLLibrary) target).VoxMLEntityTypeDict != null) {
                        foreach (string item in ((VoxMLLibrary) target).VoxMLEntityTypeDict.Keys) {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item,GUILayout.Width(150));
                            GUILayout.Label(((VoxMLLibrary) target).VoxMLEntityTypeDict[item]);
                            GUILayout.EndHorizontal();
                        }
                    }

                    // some styling for the header, this is optional
                    GUILayout.Label("VoxML Objects", bold);

                    // add a label for each item, you can add more properties
                    // you can even access components inside each item and display them
                    // for example if every item had a sprite we could easily show it 
                    if (((VoxMLLibrary) target).VoxMLObjectDict != null) {
                        foreach (string item in ((VoxMLLibrary) target).VoxMLEntityTypeDict.Keys) {
                            GUILayout.Label(item);
                        }
                    }
                }
            }
#endif

            void Start() {
                VoxMLEntityTypeDict = new Dictionary<string, string>();
                VoxMLObjectDict = new Dictionary<string, VoxML>();
                VoxML.LoadedFromText += OnLoadedFromText;

                WalkDir(Data.voxmlDataPath);
            }

            private void WalkDir(string sDir) {
                Debug.Log(string.Format("Walking directory: {0}", sDir));
                try {
                    foreach (string d in Directory.GetDirectories(sDir)) {
                        foreach (string f in Directory.GetFiles(d, "*.xml")) {
                            Debug.Log(string.Format("Adding VoxML Entity: {0}", Path.GetFileNameWithoutExtension(f)));
                            VoxMLEntityTypeDict.Add(Path.GetFileNameWithoutExtension(f), Path.GetFileName(d));
                            using (StreamReader sr = new StreamReader(f)) {
                                VoxML.LoadFromText(sr.ReadToEnd(), Path.GetFileNameWithoutExtension(f));
                            }
                        }
                        WalkDir(d);
                    }
                }
                catch (Exception excpt) {
                    Debug.LogError(excpt.Message);
                }
            }

            public void OnLoadedFromText(object sender, VoxMLObjectEventArgs e) {
                CreateVoxmlObjectDict(e.Filename, e.VoxML);
            }

            public void CreateVoxmlObjectDict(string filename, VoxML voxml) {
                if (!VoxMLObjectDict.ContainsKey(filename)) {
                    VoxMLObjectDict.Add(Path.GetFileName(filename), voxml);
                }

                string s = "";
                foreach (KeyValuePair<string, VoxSimPlatform.Vox.VoxML> kvp in VoxMLObjectDict) {
                    s += string.Format("Key = {0}, Value = {1}\n", kvp.Key, kvp.Value);
                }
                Debug.Log("VoxML dictionary content:" + s);
            }
        }
    }
}