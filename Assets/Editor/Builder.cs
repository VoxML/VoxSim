using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

using Global;

[CustomEditor(typeof(Builder))]
public class Builder : Editor {

	public string buildName = "VoxSim";
	List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

	bool buildMac,buildWindows,buildIOS,buildAll;
	bool build;

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI();
		buildMac = GUILayout.Button ("Build Mac", GUILayout.Height (30));
		buildWindows = GUILayout.Button ("Build Windows", GUILayout.Height (30));
		buildIOS = GUILayout.Button ("Build iOS", GUILayout.Height (30));
		buildAll = GUILayout.Button ("Build All", GUILayout.Height (30));
		build = (buildMac || buildWindows || buildIOS || buildAll);

		if (build) {
			using (System.IO.StreamWriter file =
				new System.IO.StreamWriter(@"Assets/Resources/ScenesList.txt"))
			{
				string scenesDirPath = Application.dataPath + "/Scenes/";
				string [] fileEntries = Directory.GetFiles(Application.dataPath+"/Scenes/","*.unity");
				foreach (string s in fileEntries) {
					string sceneName = s.Remove(0,Application.dataPath.Length-"Assets".Length);
					if (!scenes.Contains(sceneName)) {
						scenes.Add(sceneName);
						file.WriteLine(sceneName.Split ('/')[2].Replace (".unity",""));
					}
				}
			}

			//Debug.Log(@"Build/mac/VoxSim/Contents".Remove (@"Build/mac/VoxSim/Contents".LastIndexOf('/', @"Build/mac/VoxSim/Contents".LastIndexOf('/') - 1)) + string.Format ("/Data/voxml"));
			//Debug.Log(@"Build/win/VoxSim_Data".Remove (@"Build/win/VoxSim_Data".LastIndexOf('/') + 1) + string.Format ("Data/voxml"));

			Debug.Log (Data.voxmlDataPath);

			// TODO: Migrate away from this in favor of build scripts
			if (buildMac || buildAll) {
				AutoBuilder.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/mac/Data", true);
				BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/mac/" + buildName, BuildTarget.StandaloneOSXUniversal, BuildOptions.None);
			}

			if (buildWindows || buildAll) {
				AutoBuilder.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/win/Data", true);
				BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/win/" + buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);
			}

			if (buildIOS || buildAll) {
				BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
					BuildOptions.AcceptExternalModificationsToPlayer));
				AutoBuilder.DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/ios/" + buildName + "/VoxML", true);
			}

			//if (buildWeb) {
			//	BuildPipeline.BuildPlayer(scenes.ToArray(),"Build/web/"+buildName,BuildTarget.WebPlayer,BuildOptions.None);
			//}
		}
	}
}

public static class AutoBuilder {

	public static void BuildMac() {
		string buildName = System.Environment.GetCommandLineArgs()[5];
		Debug.Log (buildName);

		List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

		using (System.IO.StreamWriter file =
		    new System.IO.StreamWriter (@"Assets/Resources/ScenesList.txt")) {
			string scenesDirPath = Application.dataPath + "/Scenes/";
			string[] fileEntries = Directory.GetFiles (Application.dataPath + "/Scenes/", "*.unity");
			foreach (string s in fileEntries) {
				string sceneName = s.Remove (0, Application.dataPath.Length - "Assets".Length);
				if (!scenes.Contains (sceneName)) {
					scenes.Add (sceneName);
					file.WriteLine (sceneName.Split ('/') [2].Replace (".unity", ""));
				}
			}
		}
			
		DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/mac/Data", true);
		BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/mac/" + buildName, BuildTarget.StandaloneOSXUniversal, BuildOptions.None);
	}

	public static void BuildWindows() {
		string buildName = System.Environment.GetCommandLineArgs()[5];
		Debug.Log (buildName);

		List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

		using (System.IO.StreamWriter file =
			new System.IO.StreamWriter (@"Assets/Resources/ScenesList.txt")) {
			string scenesDirPath = Application.dataPath + "/Scenes/";
			string[] fileEntries = Directory.GetFiles (Application.dataPath + "/Scenes/", "*.unity");
			foreach (string s in fileEntries) {
				string sceneName = s.Remove (0, Application.dataPath.Length - "Assets".Length);
				if (!scenes.Contains (sceneName)) {
					scenes.Add (sceneName);
					file.WriteLine (sceneName.Split ('/') [2].Replace (".unity", ""));
				}
			}
		}

		DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/win/Data", true);
		BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/win/" + buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);
	}

	public static void BuildIOS() {
		string buildName = System.Environment.GetCommandLineArgs()[5];
		Debug.Log (buildName);

		List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

		using (System.IO.StreamWriter file =
			new System.IO.StreamWriter (@"Assets/Resources/ScenesList.txt")) {
			string scenesDirPath = Application.dataPath + "/Scenes/";
			string[] fileEntries = Directory.GetFiles (Application.dataPath + "/Scenes/", "*.unity");
			foreach (string s in fileEntries) {
				string sceneName = s.Remove (0, Application.dataPath.Length - "Assets".Length);
				if (!scenes.Contains (sceneName)) {
					scenes.Add (sceneName);
					file.WriteLine (sceneName.Split ('/') [2].Replace (".unity", ""));
				}
			}
		}

		BuildPipeline.BuildPlayer (scenes.ToArray (), "Build/ios/" + buildName, BuildTarget.iOS, (BuildOptions.BuildScriptsOnly |
			BuildOptions.AcceptExternalModificationsToPlayer));
		//DirectoryCopy (Path.GetFullPath (Data.voxmlDataPath + "/../"), @"Build/ios/" + buildName + "/VoxML", true);
	}

	public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
	{
		// Get the subdirectories for the specified directory.
		DirectoryInfo dir = new DirectoryInfo(sourceDirName);

		if (!dir.Exists)
		{
			throw new DirectoryNotFoundException(
				"Source directory does not exist or could not be found: "
				+ sourceDirName);
		}

		DirectoryInfo[] dirs = dir.GetDirectories();
		// If the destination directory doesn't exist, create it.
		if (!Directory.Exists(destDirName))
		{
			Directory.CreateDirectory(destDirName);
		}

		// Get the files in the directory and copy them to the new location.
		FileInfo[] files = dir.GetFiles();
		foreach (FileInfo file in files)
		{
			string temppath = Path.Combine(destDirName, file.Name);
			file.CopyTo(temppath, true);
		}

		// If copying subdirectories, copy them and their contents to new location.
		if (copySubDirs)
		{
			foreach (DirectoryInfo subdir in dirs)
			{
				string temppath = Path.Combine(destDirName, subdir.Name);
				DirectoryCopy(subdir.FullName, temppath, copySubDirs);
			}
		}
	}
}
