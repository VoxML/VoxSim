using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using VoxSimPlatform.Interaction;

namespace VoxSimPlatform {
    namespace Network {
        public class NLURestClient : RestClient {
            String route = ""; // to be set on the first contact to the server
            String payload = ""; //Json payload
            public String last_read = "";

            /// <summary>
            /// Name to match, that's about it.
            /// </summary>
            public NLURestClient() {
                clientType = typeof(NLUIOClient);
            }


            /// <summary>
            /// In this method, we actually invoke a request to the outside server
            /// </summary>
            public override IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error) {
                if (!url.StartsWith("http")) {
                    url = "http://" + url;
                }
                UnityWebRequest webRequest;

                Debug.LogWarning("Payload is: " + jsonPayload);
                //Debug.LogWarning(jsonPayload);
                if (jsonPayload.StartsWith("web")) {
                    // Hacky way to circumvent normal command structure.
                    // Also, love how 'hack' gets syntax highlighted lol
                    Debug.LogWarning("WEB tag called");
                    GameObject browser = GameObject.Find("Browser (GUI)");
                    BrowserInterface bi = browser.GetComponent<BrowserInterface>();
                    bi.grab_selected(jsonPayload);
                    jsonPayload = "";
                }

                if (jsonPayload != "0") {
                    var form = new WWWForm();
                    form.AddField("sentence", jsonPayload); // IMPORTANT: Assumes there is a form with THIS PARICULAR NAME OF FIELD
                    webRequest = UnityWebRequest.Post(url, form);
                }
                else {
                    // Only really handles the initialization step, to see if the server is, in fact, real
                    webRequest = new UnityWebRequest(url + route, method); // route is specific page as directed by server
                    var payloadBytes = string.IsNullOrEmpty(jsonPayload)
                        ? Encoding.UTF8.GetBytes("{}")
                        : Encoding.UTF8.GetBytes(jsonPayload);

                    UploadHandler upload = new UploadHandlerRaw(payloadBytes);
                    webRequest.uploadHandler = upload;
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                }

                webRequest.SendWebRequest();
                int count = 0; // Try several times before failing
                while (count < 20) { // 2 seconds max is good? Probably.
                    yield return new WaitForSeconds((float)0.1); // Totally sufficient
                    if (webRequest.isNetworkError || webRequest.isHttpError) {
                        Debug.LogWarning("Some sort of network error: " + webRequest.error + " from " + url);
                    }
                    else {
                        // Show results as text            
                        if (webRequest.downloadHandler.text != "") {
                            last_read = webRequest.downloadHandler.text;
                            //BroadcastMessage("LookForNewParse"); // Tell something, in JointGestureDemo for instance, to grab the result
                            if(webRequest.downloadHandler.text != "connected") {
                                SingleAgentInteraction sai = GameObject.FindObjectOfType<SingleAgentInteraction>();
                                sai.SendMessage("LookForNewParse");
                            }
                            else {
                                // Blatantly janky
                                NLUIOClient parent = GameObject.FindObjectOfType<NLUIOClient>();
                                parent.nlurestclient = this; // Ew, disgusting
                            }

                            Debug.Log("Server took " + count * 0.1 + " seconds");
                            POST_okay(count * 0.1); // Parameter literally means nothing here.
                            break;
                        }
                    }
                    count++;
                }
                if (count >= 20) {
                    Debug.LogWarning("Server took 2+ seconds ");
                }
            }
        }
    }
}