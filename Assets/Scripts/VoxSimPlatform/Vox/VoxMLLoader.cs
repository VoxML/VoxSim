using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Vox {
        /// <summary>
        /// This class creates a VoxMLEntityTypeDict with key value pairs of xml filename and entity type(foldername), 
        /// and a VoxMLObjectDict with key value pairs of xml filename and VoxML object. 
        /// </summary>
        public class VoxMLLoader : MonoBehaviour {
            public Dictionary<string, string> VoxMLEntityTypeDict;
            public Dictionary<string, VoxML> VoxMLObjectDict;

            void Start() {
                VoxMLEntityTypeDict = new Dictionary<string, string>();
                WalkDir(Data.voxmlDataPath);

                VoxMLObjectDict = new Dictionary<string, VoxML>();
                VoxML.LoadedFromText += OnLoadedFromText;

            }

            private void WalkDir(string sDir) {
                try {
                    foreach (string d in Directory.GetDirectories(sDir)) {
                        foreach (string f in Directory.GetFiles(d)) {
                            VoxMLEntityTypeDict.Add(Path.GetFileNameWithoutExtension(f), Path.GetFileName(d));
                        }
                        WalkDir(d);
                    }
                }
                catch (Exception excpt) {
                    Debug.Log(excpt.Message);
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