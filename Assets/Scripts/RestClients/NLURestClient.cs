using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

using VoxSimPlatform.Interaction;
using VoxSimPlatform.Network;

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

    public override IEnumerator Get(string route) {
        Debug.Log(string.Format("RestClient GET from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
        RestDataContainer result = new RestDataContainer(owner,
            Request(string.Format("{0}:{1}/{2}", address, port, route), "GET", null, "GET_" + SuccessStr, "GET_" + ErrorStr));
        yield return result.coroutine;
    }

    public override IEnumerator Post(string route, string jsonPayload) {
        Debug.Log(string.Format("RestClient POST to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
        RestDataContainer result = new RestDataContainer(owner,
            Request(string.Format("{0}:{1}/{2}", address, port, route), "POST", jsonPayload, "POST_" + SuccessStr, "POST_" + ErrorStr));
        //Debug.Log(string.Format("RestClient.Post: {0}", result));
        yield return result.result;
    }

    public override IEnumerator Put(string route, string jsonPayload) {
        Debug.Log(string.Format("RestClient PUT to {0}", string.Format("{0}:{1}/{2}", address, port, route)));
        RestDataContainer result = new RestDataContainer(owner,
            Request(string.Format("{0}:{1}/{2}", address, port, route), "PUT", jsonPayload, "PUT_" + SuccessStr, "PUT_" + ErrorStr));
        yield return result.coroutine;
    }

    public override IEnumerator Delete(string route, string jsonPayload) {
        Debug.Log(string.Format("RestClient DELETE from {0}", string.Format("{0}:{1}/{2}", address, port, route)));
        RestDataContainer result = new RestDataContainer(owner,
            Request(string.Format("{0}:{1}/{2}", address, port, route), "DELETE", jsonPayload, "DELETE_" + SuccessStr, "DELETE_" + ErrorStr));
        yield return result.coroutine;
    }

    public override IEnumerator Request(string url, string method, string jsonPayload, string success, string error) {
        //StartCoroutine(AsyncRequest(jsonPayload, method, url, success, error));
        //IEnumerator r = AsyncRequest(jsonPayload, method, url, success, error);
        //yield return r;
        //Debug.Log((UnityWebRequest)r);
        //Debug.Log(r.GetType());
        //Debug.Log(r.Current);
        //Debug.Log(r.Current.GetType());
        //url = url.Replace(":0/", ""); // filler port from before. Might as well handle it here
        //Debug.LogWarning("URL for request: " + url);
        Debug.Log(string.Format("RestClient Request {1} to {0}", url, method));
        RestDataContainer result = new RestDataContainer(owner, AsyncRequest(jsonPayload, method, url, success, error));
        //Debug.Log(string.Format("RestClient.Request: {0}", result));
        yield return result.result;
    }

    /// <summary>
    /// In this method, we actually invoke a request to the outside server
    /// </summary>
    public override IEnumerator AsyncRequest(string payload, string method, string url, string success, string error) {
        if (!url.StartsWith("http")) {
            url = "http://" + url;
        }

        Debug.Log("Payload is: " + payload);

        if (payload != "0") {
            var form = new WWWForm();
            form.AddField("sentence", payload); // IMPORTANT: Assumes there is a form with THIS PARICULAR NAME OF FIELD
            webRequest = UnityWebRequest.Post(url, form);
        }
        else {
            // Only really handles the initialization step, to see if the server is, in fact, real
            webRequest = new UnityWebRequest(url + route, method); // route is specific page as directed by server
            var payloadBytes = string.IsNullOrEmpty(payload)
                ? Encoding.UTF8.GetBytes("{}")
                : Encoding.UTF8.GetBytes(payload);

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