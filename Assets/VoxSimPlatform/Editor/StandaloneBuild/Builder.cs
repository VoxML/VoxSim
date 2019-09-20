using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using VoxSimPlatform.Global;
namespace StandaloneBuild {
	/// <summary>
	/// Class into which the contents of a build config file is deserialized.
	/// An example build config file is provided parallel to the Assets folder (sample_build_config.xml).
	/// </summary>
	public class VoxSimBuildConfig {
		[XmlArray("ScenesList")]
        [XmlArrayItem("SceneFile")]
		public List<SceneFile> Scenes = new List<SceneFile>();
	}

	/// <summary>
	/// Scene file node in build config contains a Path parameter, which is the path to a scene to include in the build.
	/// All included scenes must be in the Scenes folder, though subfolders within it are allowed.
	/// </summary>
	public class SceneFile {
		[XmlAttribute]
        public string Path { get; set; }
	}

	/// <summary>
	/// This class handles the build pipelines for various platforms that can be initiated through a build script (e.g.,
	///  build_[mac,win,ios].sh.  These scripts can be run natively on Unix systems or through MinGW/Gitbash on Windows.
	///  As of April 2019, these have been tested on Windows and OSX.  See "Build Scripts" in the documentation for more.
	/// </summary>
	public static class AutoBuilder {
		/// <summary>
		/// Processes the build config.  Produces ScenesList.txt in the process and stores this in Assets/Resources.
		/// This file is bundled into the build to populate the menu in the launcher, if VoxSimMenu is included in the build.
		/// VoxSimMenu does not have to be included but if it is, it provides the default level of customizability and a tidy
		///  way to switch between scenes.
		/// </summary>
		// IN: string: path to the build config file
		// OUT: none
		public static void ProcessBuildConfig(string path) {
			// read in the build config file and deserialize it to an instance of VoxSimBuildConfig
			XmlSerializer serializer = new XmlSerializer(typeof(VoxSimBuildConfig));
			using (var stream = new FileStream(path, FileMode.Open)) {
				VoxSimBuildConfig config = serializer.Deserialize(stream) as VoxSimBuildConfig;

				// create a new SceneList.txt file in Resources (overwrite if it already exists)
				using (StreamWriter file = new StreamWriter(@"Assets/Resources/ScenesList.txt")) {
					foreach (SceneFile s in config.Scenes) {
						// for each scene extracted from build config
						//  see if a scene by that name exists in Assets/Scenes
						// all scenes specified in build config must be in the Scenes folder
						string scenePath = Application.dataPath + "/" + s.Path;
						if (File.Exists(scenePath)) {
							// found a file
							// write the name of the scene to ScenesList
							// no other path information, no file extension
							file.WriteLine(s.Path.Replace(".unity", ""));
						}
						else {
							Debug.Log(string.Format("ProcessBuildConfig: Scene file {0} does not exist!", scenePath));
						}
					}
				}
			}
		}

        /// <summary>
        /// Makes build for Mac OSX platform
        /// </summary>
        // IN: none
        // OUT: none
		public static void BuildMac() {
			// buildName is element 5 in the build script build command, the build config path is element 6
			string buildName = Environment.GetCommandLineArgs()[5];
			string buildConfig = Environment.GetCommandLineArgs()[6];
			Debug.Log(string.Format("Building target {0} for OSX with configuration {1}", buildName, buildConfig));

			// process the build config and refresh the assets database afterwards to get ScenesList into Resources
			ProcessBuildConfig(buildConfig);
			AssetDatabase.Refresh();

			// the list of scenes to populate
			List<string> scenes = new List<string>();

			try {
				using (StreamReader scenesListfile = new StreamReader(@"Assets/Resources/ScenesList.txt")) {
					// Read each scene name from the ScenesList file constructed by ProcessBuildConfig
					// Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
					// On Unix, lines only end in \n (\r\n on Windows)
					// If somehow a Unix system ends up with a ScenesList file created on a Windows system
					//  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
					List<string> scenesList = scenesListfile.ReadToEnd().Split('\r', '\n').ToList();

					// get the editor build setting so we can add any config-specified scenes missing from it
					List<EditorBuildSettingsScene> editorBuildSettingsScenes = EditorBuildSettings.scenes.ToList();

					// all scenes included in the build must be in Scenes folder, but subdirectiories are OK as long as the paths
					//  match what is in the build config
					//  (e.g., you could have a scene at path "/Assets/Scenes/Agents/Diana.unity", and you would specify 
					//  "Agents/Diana.unity" in the build config to include it
					string assetsPath = Application.dataPath + "/";
					// Windows will extract any subdirectories from the build config with a backslash instead of a forward slash
					//  but Unity/any Unix system needs forward slashes, so replace them
					// therefore, make sure none of your actual filenames somehow has a backslash in it
					// but that shouldn't be happening anyway!
					List<string> fileEntries = Directory.GetFiles(assetsPath, "*.unity", SearchOption.AllDirectories).ToList()
						.Select(f => f.Replace('\\', '/')).ToList();
					foreach (string s in scenesList) {
						if (s != string.Empty) {
							// scene name must not be empty (skips empty lines created by cross-platform line ending confusion)
							string scenePath = assetsPath + s + ".unity";
							if (fileEntries.Contains(scenePath)) {
								// if the list of files in Scenes contains a config-specified scene
								if (!scenes.Contains(scenePath)) {
									// if that scene hasn't already been added to the list of scenes to build
									Debug.Log(string.Format("Adding scene {0} at path {1}", s, scenePath));
									// don't double-add scenes to editor build settings
									if (!editorBuildSettingsScenes.Any(f => f.enabled && f.path == "Assets/" + s + ".unity")) {
										Debug.Log(string.Format("Adding scene {0} to Editor Build Settings", "Assets/" + s + ".unity"));
										editorBuildSettingsScenes.Add(new EditorBuildSettingsScene("Assets/" + s + ".unity", true));
									}

									scenes.Add(scenePath);
								}
							}
							else {
								Debug.Log(string.Format("BuildMac: No file {0} found!  Skipping.", scenePath));
							}
						}
					}

					// save the editor build settings
					EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
				}

				// copy external VoxML folder to target location
				Data.DirectoryCopy(Path.GetFullPath(Data.voxmlDataPath + "/../"), @"Build/mac/VoxML", true);
				// build with the specified scenes
				BuildPipeline.BuildPlayer(scenes.ToArray(), "Build/mac/" + buildName + ".app", BuildTarget.StandaloneOSX, BuildOptions.None);
			}
			catch (FileNotFoundException e) {
				Debug.Log(string.Format("BuildMac: File {0} not found!", e.FileName));
			}
		}

        /// <summary>
        /// Makes build for Windows platform
        /// </summary>
        // IN: none
        // OUT: none
		public static void BuildWindows() {
			// buildName is element 5 in the build script build command, the build config path is element 6
			string buildName = Environment.GetCommandLineArgs()[5];
			string buildConfig = Environment.GetCommandLineArgs()[6];
			Debug.Log(string.Format("Building target {0} for Windows with configuration {1}", buildName, buildConfig));

			// process the build config and refresh the assets database afterwards to get ScenesList into Resources
			ProcessBuildConfig(buildConfig);
			AssetDatabase.Refresh();

			// the list of scenes to populate
			List<string> scenes = new List<string>();

			try {
				using (StreamReader scenesListfile = new StreamReader(@"Assets/Resources/ScenesList.txt")) {
					// Read each scene name from the ScenesList file constructed by ProcessBuildConfig
					// Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
					// On Unix, lines only end in \n (\r\n on Windows)
					// If somehow a Unix system ends up with a ScenesList file created on a Windows system
					//  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
					List<string> scenesList = scenesListfile.ReadToEnd().Split('\r', '\n').ToList();

					// get the editor build setting so we can add any config-specified scenes missing from it
					List<EditorBuildSettingsScene> editorBuildSettingsScenes = EditorBuildSettings.scenes.ToList();

					// all scenes included in the build must be in Scenes folder, but subdirectiories are OK as long as the paths
					//  match what is in the build config
					//  (e.g., you could have a scene at path "/Assets/Scenes/Agents/Diana.unity", and you would specify 
					//  "Agents/Diana.unity" in the build config to include it
					string assetsPath = Application.dataPath + "/";
					// Windows will extract any subdirectories from the build config with a backslash instead of a forward slash
					//  but Unity/any Unix system needs forward slashes, so replace them
					// therefore, make sure none of your actual filenames somehow has a backslash in it
					// but that shouldn't be happening anyway!
					List<string> fileEntries = Directory.GetFiles(assetsPath, "*.unity", SearchOption.AllDirectories).ToList()
						.Select(f => f.Replace('\\', '/')).ToList();
					foreach (string s in scenesList) {
						if (s != string.Empty) {
							// scene name must not be empty (skips empty lines created by cross-platform line ending confusion)
							string scenePath = assetsPath + s + ".unity";
							if (fileEntries.Contains(scenePath)) {
								// if the list of files in Scenes contains a config-specified scene
								if (!scenes.Contains(scenePath)) {
									// if that scene hasn't already been added to the list of scenes to build
									Debug.Log(string.Format("Adding scene {0} at path {1}", s, scenePath));
									// don't double-add scenes to editor build settings
									if (!editorBuildSettingsScenes.Any(f => f.enabled && f.path == "Assets/" + s + ".unity")) {
										Debug.Log(string.Format("Adding scene {0} to Editor Build Settings", "Assets/" + s + ".unity"));
										editorBuildSettingsScenes.Add(new EditorBuildSettingsScene("Assets/" + s + ".unity", true));
									}

									scenes.Add(scenePath);
								}
							}
							else {
								Debug.Log(string.Format("BuildWindows: No file {0} found!  Skipping.", scenePath));
							}
						}
					}

					// save the editor build settings
					EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
				}

				// copy external VoxML folder to target location
				Data.DirectoryCopy(Path.GetFullPath(Data.voxmlDataPath + "/../"), @"Build/win/VoxML", true);
				// build with the specified scenes
				BuildPipeline.BuildPlayer(scenes.ToArray(), "Build/win/" + buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);
			}
			catch (FileNotFoundException e) {
				Debug.Log(string.Format("BuildWindows: File {0} not found!", e.FileName));
			}
		}

        /// <summary>
        /// Makes Xcode project for iOS platform
        /// The Xcode project then has to be built and deployed to the device.
        ///  The build_ios build script handles the entire process automatically.
        /// </summary>
        // IN: none
        // OUT: none
		public static void BuildIOS() {
			// buildName is element 5 in the build script build command, the build config path is element 6
			string buildName = Environment.GetCommandLineArgs()[5];
			string buildConfig = Environment.GetCommandLineArgs()[6];
			Debug.Log(string.Format("Building target {0} for iOS with configuration {1}", buildName, buildConfig));

			// process the build config and refresh the assets database afterwards to get ScenesList into Resources
			ProcessBuildConfig(buildConfig);
			AssetDatabase.Refresh();

			// the list of scenes to populate
			List<string> scenes = new List<string>();

			try {
				using (StreamReader scenesListfile = new StreamReader(@"Assets/Resources/ScenesList.txt")) {
					// Read each scene name from the ScenesList file constructed by ProcessBuildConfig
					// Use both Win32 and Unix line endings so we can run this on both Win and Unix systems
					// On Unix, lines only end in \n (\r\n on Windows)
					// If somehow a Unix system ends up with a ScenesList file created on a Windows system
					//  it should split on \r and \n and end up with some lines of 0 length, which are then skipped below
					List<string> scenesList = scenesListfile.ReadToEnd().Split('\r', '\n').ToList();

					// get the editor build setting so we can add any config-specified scenes missing from it
					List<EditorBuildSettingsScene> editorBuildSettingsScenes = EditorBuildSettings.scenes.ToList();

					// all scenes included in the build must be in Scenes folder, but subdirectiories are OK as long as the paths
					//  match what is in the build config
					//  (e.g., you could have a scene at path "/Assets/Scenes/Agents/Diana.unity", and you would specify 
					//  "Agents/Diana.unity" in the build config to include it
					string scenesDirPath = Application.dataPath + "/Scenes/";
					// Windows will extract any subdirectories from the build config with a backslash instead of a forward slash
					//  but Unity/any Unix system needs forward slashes, so replace them
					// therefore, make sure none of your actual filenames somehow has a backslash in it
					// but that shouldn't be happening anyway!
					// Since iOS building is only really supported on Max OS, this shouldn't be an issue here,
					//  but the check is included anyway for consistency
					List<string> fileEntries = Directory.GetFiles(scenesDirPath, "*.unity", SearchOption.AllDirectories).ToList()
						.Select(f => f.Replace('\\', '/')).ToList();
					foreach (string s in scenesList) {
						if (s != string.Empty) {
							// scene name must not be empty (skips empty lines created by cross-platform line ending confusion)
							string scenePath = scenesDirPath + s + ".unity";
							if (fileEntries.Contains(scenePath)) {
								// if the list of files in Scenes contains a config-specified scene
								if (!scenes.Contains(scenePath)) {
									// if that scene hasn't already been added to the list of scenes to build
									Debug.Log(string.Format("Adding scene {0} at path {1}", s, scenePath));
									// don't double-add scenes to editor build settings
									if (!editorBuildSettingsScenes.Any(f => f.enabled && f.path == "Assets/Scenes/" + s + ".unity")) {
										Debug.Log(string.Format("Adding scene {0} to Editor Build Settings", "Assets/Scenes/" + s + ".unity"));
										editorBuildSettingsScenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + s + ".unity", true));
									}

									scenes.Add(scenePath);
								}
							}
							else {
								Debug.Log(string.Format("BuildIOS: No file {0} found!  Skipping.", scenePath));
							}
						}
					}

					// save the editor build settings
					EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
				}

				// build with the specified scenes
				BuildPipeline.BuildPlayer(scenes.ToArray(), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
				                                                                                        BuildOptions.AcceptExternalModificationsToPlayer));
			}
			catch (FileNotFoundException e) {
				Debug.Log(string.Format("BuildIOS: File {0} not found!", e.FileName));
			}
		}
	}
}