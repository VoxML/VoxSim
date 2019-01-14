using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PreloaderScript : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        SceneManager.LoadScene(1);
        Application.targetFrameRate = 30;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Framerate is set to" + Application.targetFrameRate);
    }
}
