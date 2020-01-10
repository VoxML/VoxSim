using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using VoxSimPlatform.Interaction;

// NOTE: no successful connection to outside has been established yet.
// But that's mostly a matter of putting together the right call to outside in the rest client (that's this one)
// May be awkward timing things to pick up from server. Solution is *probably* to send some kind of message, that's what made NLURestClient work (see Rest Clients folder)

namespace VoxSimPlatform {
    namespace Network {
        public class WordCloudRestClient : RestClient {
            String route = ""; // to be set on the first contact to the server
            String payload = ""; //Json payload
            public String last_read = "";

            /// <summary>
            /// Name to match, that's about it.
            /// </summary>
            public WordCloudRestClient() {
                clientType = typeof(WordCloudIOClient);
            }

            //public void TestWordCloudRestClient() {

            //}


            /// <summary>
            /// In this method, we actually invoke a request to the outside server
            /// </summary>
            public override IEnumerator AsyncRequest(string jsonPayload, string method, string url, string success, string error) {
                

                if (!url.StartsWith("http")) {
                    url = "http://" + url;
                }
                UnityWebRequest webRequest;

                Debug.Log("Payload is: " + jsonPayload);

                if (jsonPayload != "0") {
                    var form = new WWWForm();
                    form.AddField("message", jsonPayload); // IMPORTANT: Assumes there is a form with THIS PARICULAR NAME OF FIELD
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
//                        Debug.LogWarning("Some sort of network error: " + webRequest.error + " from " + url);
                    }
                    else {
                        // Show results as text            
                        if (webRequest.downloadHandler.text != "") {
                            last_read = webRequest.downloadHandler.text;
                            //BroadcastMessage("LookForNewParse"); // Tell something, in JointGestureDemo for instance, to grab the result
                            if (webRequest.downloadHandler.text != "connected") {
                                // Really needs to change. SingleAgentInteraction isn't gonna be extensible in our package-based future
                                // And I didn't ever put together that LookForNewParse function either, this section is just a ghost :P
                                SingleAgentInteraction sai = GameObject.FindObjectOfType<SingleAgentInteraction>();
                                sai.SendMessage("LookForNewParse");
                            }
                            else {
                                // Blatantly janky
                                WordCloudIOClient parent = GameObject.FindObjectOfType<WordCloudIOClient>();
                                parent.wordcloudrestclient = this; // Ew, disgusting
                            }

                            Debug.Log("Server took " + count * 0.1 + " seconds");
                            POST_okay(count * 0.1); // Parameter literally means nothing here.
                            break;
                        }
                    }
                    count++;
                }
                if (count >= 20) {
//                    Debug.LogWarning("WordCloud Server took 2+ seconds ");
//                    Debug.LogWarning(webRequest.uploadHandler.data);
                }
            }
        }
    }
}