using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// This class create a VoxmlDatadictionary with key value pairs of filename and foldername.
/// </summary>
public class CreateVoxmlDataDict : MonoBehaviour
{
    public Dictionary<string, string> VoxmlDataDict; 

    void Start()
    {
        VoxmlDataDict = new Dictionary<string, string>();
        WalkDir("Data/voxml"); 
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
        catch (System.Exception excpt)
        {
            Debug.Log(excpt.Message);
        }
    }
}