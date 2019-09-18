using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

using VoxSimPlatform.Network;

namespace VoxSimPlatform {
    namespace Global {
        /// <summary>
        /// SceneHelper class
        /// </summary>
        public static class SceneHelper {
            public static IEnumerator LoadScene(string sceneName) {
                yield return null;

                AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
                ao.allowSceneActivation = true;

                while (!ao.isDone) {
                    yield return null;
                }

                //CommunicationsBridge commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
                //commBridge.OpenPortInternal(PlayerPrefs.GetString("Listener Port"));

#if UNITY_IOS
                Screen.SetResolution(1280,960,true);
                Debug.Log(Screen.currentResolution);
#endif
            }
        }
    }
}