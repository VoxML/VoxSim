using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// This class create a VoxmlDatadictionary with key value pairs of xml filename and entity type(foldername), 
/// and a VoxmlObectDict with key value pairs of xml filename and VoxML object. 
/// </summary>
public class CreateVoxmlDataDict : MonoBehaviour
{
    public Dictionary<string, string> VoxmlDataDict;
    public Dictionary<string, VoxSimPlatform.Vox.VoxML> VoxmlObjectDict; 

    void Start()
    {
        VoxmlDataDict = new Dictionary<string, string>();
        WalkDir("Data/voxml");

        VoxmlObjectDict = new Dictionary<string, VoxSimPlatform.Vox.VoxML>();
        VoxSimPlatform.Vox.VoxML.LoadedFromText += OnLoadedFromText; 

    }

    private void WalkDir(string sDir)
    {
        try
        {
            foreach (string d in Directory.GetDirectories(sDir))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    VoxmlDataDict.Add(Path.GetFileNameWithoutExtension(f), Path.GetFileName(d));
                }
                WalkDir(d);
            }
        }
        catch (Exception excpt)
        {
            Debug.Log(excpt.Message);
        }
    }

    public void OnLoadedFromText(object sender, VoxSimPlatform.Vox.VoxMLObjectEventArgs e)
    {
        CreateVoxmlObjectDict(e.Filename, e.VoxML); 
    }

    public void CreateVoxmlObjectDict(string filename, VoxSimPlatform.Vox.VoxML voxML)
    {
        if (!VoxmlObjectDict.ContainsKey(filename)) {
            VoxmlObjectDict.Add(Path.GetFileName(filename), voxML);
        }

        string s = ""; 
        foreach (KeyValuePair<string, VoxSimPlatform.Vox.VoxML> kvp in VoxmlObjectDict)
        {
            s += string.Format("Key = {0}, Value = {1}\n", kvp.Key, kvp.Value);
        }
        Debug.Log("Now printing dictionary**********:" + s); 
    }
}