using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using Global;

namespace StandaloneBuild {

    public class VoxSimBuildConfig
    {
        [XmlArray("ScenesList")]
        [XmlArrayItem("SceneFile")]
        public List<SceneFile> Scenes = new List<SceneFile>();
    }

    public class SceneFile
    {
        [XmlAttribute]
        public string Path { get; set; }
    }

    public static class AutoBuilder {

        public static void ProcessBuildConfig(string path) {
            XmlSerializer serializer = new XmlSerializer(typeof(VoxSimBuildConfig));
            using (var stream = new FileStream(path, FileMode.Open)) {
                VoxSimBuildConfig config = serializer.Deserialize(stream) as VoxSimBuildConfig;

                using (StreamWriter file = new StreamWriter(@"Assets/Resources/ScenesList.txt")) {
                    foreach (SceneFile s in config.Scenes) {
                        string scenePath = Application.dataPath + "/Scenes/" + s.Path;
                        if (File.Exists(scenePath)) {
                            string sceneName = scenePath.Remove(0, Application.dataPath.Length - "Assets".Length);
                            file.WriteLine(s.Path.Replace(".unity", ""));
                        }
                        else {
                            Debug.Log(string.Format("ProcessBuildConfig: Scene file {0} does not exist!", scenePath));
                        }
                    }
                }
            }
        }

    	public static void BuildMac() {
    		string buildName = System.Environment.GetCommandLineArgs()[5];
            string buildConfig = System.Environment.GetCommandLineArgs()[6];
            Debug.Log (string.Format("Building target {0} for OSX with configuration {1}", buildName, buildConfig));

            ProcessBuildConfig(buildConfig);

    		List<string> scenes = new List<string>();

            try {
        		using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\n').ToList();
                    string scenesDirPath = Application.dataPath + "/Scenes/";
                    List<string> fileEntries = Directory.GetFiles (scenesDirPath, "*.unity").ToList();
                    foreach (string s in scenesList) {
                        if (s != string.Empty) {
                            string scenePath = scenesDirPath + s + ".unity";
                            if (fileEntries.Contains(scenePath)) {
                                Debug.Log(string.Format("Adding scene {0} at relative path {1}", s, scenePath));
                                if (!scenes.Contains (scenePath)) {
                					scenes.Add (scenePath);
                				}
                            }
                            else {
                                Debug.Log(string.Format("BuildMac: No file {0} found!  Skipping.", scenePath));
                            }
                        }
                    }
        		}
    			
        		Data.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/mac/Data", true);
        		BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/mac/" + buildName, BuildTarget.StandaloneOSX, BuildOptions.None);
            }
            catch (FileNotFoundException e) {
                Debug.Log(string.Format("BuildMac: File {0} not found!", e.FileName));
            }
    	}

        public static void BuildWindows() {
            string buildName = System.Environment.GetCommandLineArgs()[5];
            string buildConfig = System.Environment.GetCommandLineArgs()[6];
            Debug.Log (string.Format("Building target {0} for Windows with configuration {1}", buildName, buildConfig));

            ProcessBuildConfig(buildConfig);

            List<string> scenes = new List<string>();

            try {
                using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\n').ToList();
                    string scenesDirPath = Application.dataPath + "/Scenes/";
                    List<string> fileEntries = Directory.GetFiles (scenesDirPath, "*.unity").ToList();
                    foreach (string s in scenesList) {
                        if (s != string.Empty) {
                            string scenePath = scenesDirPath + s + ".unity";
                            if (fileEntries.Contains(scenePath)) {
                                Debug.Log(string.Format("Adding scene {0} at relative path {1}", s, scenePath));
                                if (!scenes.Contains (scenePath)) {
                                    scenes.Add (scenePath);
                                }
                            }
                            else {
                                Debug.Log(string.Format("BuildMac: No file {0} found!  Skipping.", scenePath));
                            }
                        }
                    }
                }
                
                Data.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/win/Data", true);
                BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/win/" + buildName, BuildTarget.StandaloneWindows, BuildOptions.None);
            }
            catch (FileNotFoundException e) {
                Debug.Log(string.Format("BuildMac: File {0} not found!", e.FileName));
            }
        }

    	//public static void BuildWindows() {
    	//	string buildName = System.Environment.GetCommandLineArgs()[5];
     //       Debug.Log(string.Format("Building target {0} for Windows", buildName));

     //       List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

    	//	using (System.IO.StreamWriter file =
    	//		new System.IO.StreamWriter (@"Assets/Resources/ScenesList.txt")) {
    	//		string scenesDirPath = Application.dataPath + "/Scenes/";
    	//		string[] fileEntries = Directory.GetFiles (Application.dataPath + "/Scenes/", "*.unity");
    	//		foreach (string s in fileEntries) {
    	//			string sceneName = s.Remove (0, Application.dataPath.Length - "Assets".Length);
    	//			if (!scenes.Contains (sceneName)) {
    	//				scenes.Add (sceneName);
    	//				file.WriteLine (sceneName.Split ('/') [2].Replace (".unity", ""));
    	//			}
    	//		}
    	//	}

    	//	DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/win/Data", true);
    	//	BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/win/" + buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);
    	//}

        public static void BuildIOS() {
            string buildName = System.Environment.GetCommandLineArgs()[5];
            string buildConfig = System.Environment.GetCommandLineArgs()[6];
            Debug.Log (string.Format("Building target {0} for iOS with configuration {1}", buildName, buildConfig));

            ProcessBuildConfig(buildConfig);

            List<string> scenes = new List<string>();

            try {
                using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\n').ToList();
                    string scenesDirPath = Application.dataPath + "/Scenes/";
                    List<string> fileEntries = Directory.GetFiles (scenesDirPath, "*.unity").ToList();
                    foreach (string s in scenesList) {
                        if (s != string.Empty) {
                            string scenePath = scenesDirPath + s + ".unity";
                            if (fileEntries.Contains(scenePath)) {
                                Debug.Log(string.Format("Adding scene {0} at relative path {1}", s, scenePath));
                                if (!scenes.Contains (scenePath)) {
                                    scenes.Add (scenePath);
                                }
                            }
                            else {
                                Debug.Log(string.Format("BuildMac: No file {0} found!  Skipping.", scenePath));
                            }
                        }
                    }
                }
                
                BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
                    BuildOptions.AcceptExternalModificationsToPlayer));
            }
            catch (FileNotFoundException e) {
                Debug.Log(string.Format("BuildMac: File {0} not found!", e.FileName));
            }
        }

    	//public static void BuildIOS() {
    	//	string buildName = System.Environment.GetCommandLineArgs()[5];
     //       Debug.Log(string.Format("Building target {0} for iOS", buildName));

     //       List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

    	//	using (System.IO.StreamWriter file =
    	//		new System.IO.StreamWriter (@"Assets/Resources/ScenesList.txt")) {
    	//		string scenesDirPath = Application.dataPath + "/Scenes/";
    	//		string[] fileEntries = Directory.GetFiles (Application.dataPath + "/Scenes/", "*.unity");
    	//		foreach (string s in fileEntries) {
    	//			string sceneName = s.Remove (0, Application.dataPath.Length - "Assets".Length);
    	//			if (!scenes.Contains (sceneName)) {
    	//				scenes.Add (sceneName);
    	//				file.WriteLine (sceneName.Split ('/') [2].Replace (".unity", ""));
    	//			}
    	//		}
    	//	}

    	//	BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
    	//		BuildOptions.AcceptExternalModificationsToPlayer));
    	//	//DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/ios/" + buildName + "/VoxML", true);
    	//}
    }
}
