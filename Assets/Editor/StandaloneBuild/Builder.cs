using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using Global;

namespace StandaloneBuild {

    /// <summary>
    /// Class into which the contents of a build config file is deserialized.
    /// An example build config file is provided parallel to the Assets folder (sample_build_config.xml).
    /// </summary>
    public class VoxSimBuildConfig
    {
        [XmlArray("ScenesList")]
        [XmlArrayItem("SceneFile")]
        public List<SceneFile> Scenes = new List<SceneFile>();
    }

    /// <summary>
    /// Scene file node in build config contains a Path parameter, which is the path to a scene to include in the build.
    /// All included scenes must be in the Scenes folder, though subfolders within it are allowed.
    /// </summary>
    public class SceneFile
    {
        [XmlAttribute]
        public string Path { get; set; }
    }

    /// <summary>
    /// This class handles the build pipelines for various platforms that can be initiated through a build script (e.g.,
    ///  build_[mac,win,ios].sh.
    /// </summary>
    public static class AutoBuilder {

        /// <summary>
        /// Processes the build config.
        /// 
        /// Produces ScenesList.txt in the process and stores this in Assets/Resources.  This file is bundled into the 
        ///  build to populate the menu in the launcher, if VoxSimMenu is included in the build.
        /// </summary>
        // IN: string: path to the build config file
        // OUT: none
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
            AssetDatabase.Refresh();

    		List<string> scenes = new List<string>();

            try {
        		using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    // Read each scene name from the ScenesList file constructed by ProcessBuildConfig
                    // Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
                    // On Unix, lines only end in \n (\r\n on Windows)
                    // If somehow a Unix system ends up with a ScenesList file created on a Windows system
                    //  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\r', '\n').ToList();

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
            AssetDatabase.Refresh();

            List<string> scenes = new List<string>();

            try {
                using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    // Read each scene name from the ScenesList file constructed by ProcessBuildConfig
                    // Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
                    // On Unix, lines only end in \n (\r\n on Windows)
                    // If somehow a Unix system ends up with a ScenesList file created on a Windows system
                    //  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\r', '\n').ToList();

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
                                Debug.Log(string.Format("BuildWindows: No file {0} found!  Skipping.", scenePath));
                            }
                        }
                    }
                }
                
                Data.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/win/Data", true);
                BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/win/" + buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);
            }
            catch (FileNotFoundException e) {
                Debug.Log(string.Format("BuildWindows: File {0} not found!", e.FileName));
            }
        }
           
        public static void BuildIOS() {
            string buildName = System.Environment.GetCommandLineArgs()[5];
            string buildConfig = System.Environment.GetCommandLineArgs()[6];
            Debug.Log (string.Format("Building target {0} for iOS with configuration {1}", buildName, buildConfig));

            ProcessBuildConfig(buildConfig);
            AssetDatabase.Refresh();

            List<string> scenes = new List<string>();

            try {
                using (StreamReader scenesListfile = new StreamReader (@"Assets/Resources/ScenesList.txt")) {
                    // Read each scene name from the ScenesList file constructed by ProcessBuildConfig
                    // Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
                    // On Unix, lines only end in \n (\r\n on Windows)
                    // If somehow a Unix system ends up with a ScenesList file created on a Windows system
                    //  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
                    List<string> scenesList = scenesListfile.ReadToEnd().Split('\r','\n').ToList();

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
                                Debug.Log(string.Format("BuildIOS: No file {0} found!  Skipping.", scenePath));
                            }
                        }
                    }
                }
                
                BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
                    BuildOptions.AcceptExternalModificationsToPlayer));
            }
            catch (FileNotFoundException e) {
                Debug.Log(string.Format("BuildIOS: File {0} not found!", e.FileName));
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
