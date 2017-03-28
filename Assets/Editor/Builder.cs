using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

using Global;

[CustomEditor(typeof(Builder))]
public class Builder : Editor {

	public string buildName = "VoxSim";
	List<string> scenes = new List<string>(){"Assets/Scenes/VoxSimMenu.unity"};

	public override void OnInspectorGUI ()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button ("Build", GUILayout.Height (30))) {
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

			DirectoryCopy(Path.GetFullPath(Data.voxmlDataPath + "/../"), @"Build/mac/Data", true);
			DirectoryCopy(Path.GetFullPath(Data.voxmlDataPath + "/../"), @"Build/win/Data", true);

			BuildPipeline.BuildPlayer(scenes.ToArray(),"Build/mac/"+buildName,BuildTarget.StandaloneOSXUniversal,BuildOptions.None);
            BuildPipeline.BuildPlayer(scenes.ToArray(),"Build/win/"+buildName,BuildTarget.StandaloneWindows,BuildOptions.None);
			//BuildPipeline.BuildPlayer(scenes.ToArray(),"Build/web/"+buildName,BuildTarget.WebPlayer,BuildOptions.None);
		}
	}

	private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
