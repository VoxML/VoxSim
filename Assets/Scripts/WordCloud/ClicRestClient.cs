using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using VoxSimPlatform.Interaction;
using System.Collections.Generic;

namespace VoxSimPlatform {
    namespace Network {
        public class ClicRestClient : RestClient {
            String route = ""; // to be set on the first contact to the server
            String payload = ""; //Json payload
            public String last_read = "";
            BrowserInterface bi;

            /// <summary>
            /// Name to match, that's about it.
            /// </summary>
            public ClicRestClient() {
                clientType = typeof(ClicIOClient);
            }


            /// <summary>
            /// In this method, we actually invoke a request to the outside server
            /// </summary>
            public override IEnumerator AsyncRequest(string to_say_and_payload, string method, string url, string success, string error) {
                if (!url.StartsWith("http")) {
                    url = "http://" + url;
                }
                UnityWebRequest webRequest;


                string jsonPayload;
                string to_say;
                // Split the sentence from the payload

                if(!to_say_and_payload.Contains("~")){
                    Debug.LogWarning("No '~' character in to_say_and_payload");
                    to_say = to_say_and_payload;
                    jsonPayload = "";
                }
                else {
                    var x = to_say_and_payload.Split('~'); // Tilde ~ seems like an okay split character
                    to_say = x[0];
                    jsonPayload = x[1];
                }


                Debug.LogWarning("To_say is: " + to_say);
                Debug.LogWarning("Payload is: " + jsonPayload);


                var form = new WWWForm();

                // Look at ADDBINARYDATA later

                foreach(var s in form.headers) {
                    Debug.LogWarning("header " + s);

                }
                var data = "{\"geneSetMembers\":[\"UST\"],\"geneSetName\":\"selection0\"}";


                // Convert our data string to a bunch of bytes. 
                {
                    // Only really handles the initialization step, to see if the server is, in fact, real
                    webRequest = new UnityWebRequest(url + route + to_say, method); // route is specific page as directed by server
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
                            if (webRequest.downloadHandler.text != "connected") {
                                //SingleAgentInteraction sai = GameObject.FindObjectOfType<SingleAgentInteraction>();
                                //sai.SendMessage("LookForNewParse");
                                bi.SendMessage("BobSaidSomething");
                            }
                            else {
                                // Blatantly janky
                                ClicIOClient parent = GameObject.FindObjectOfType<ClicIOClient>();
                                parent.clicrestclient = this; // Ew, disgusting
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