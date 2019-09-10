using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;


public class WordCloudIOClient : MonoBehaviour {
    WordCloudRestClient _cloudSocket;

    /// <summary>
    /// Pretty much the same as the EpiSimIOClient, but with name changes
    /// </summary>
    public WordCloudRestClient wordcloudrestclient {
        get { return _cloudSocket; }
        set { _cloudSocket = value; }
    }

    CommunicationsBridge commBridge;

    // Use this for initialization
    void Start() {
        commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        //_wordcloudrestclient = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
        _cloudSocket = (WordCloudRestClient)commBridge.FindRestClientByLabel("EpiSim");
    }

    // Update is called once per frame
    void Update() {
        if (_cloudSocket != null) {
            string epiSimUrl = string.Format("{0}:{1}", _cloudSocket.address, _cloudSocket.port);
            if (_cloudSocket.isConnected) {
                if (commBridge.tryAgainSockets.ContainsKey(epiSimUrl)) {
                    if (commBridge.tryAgainSockets[epiSimUrl] == typeof(FusionSocket)) {
                        _cloudSocket = (WordCloudRestClient)commBridge.FindRestClientByLabel("Parser URL"); // Maybe wrong
                        //Debug.Log(_fusionSocket.IsConnected());
                    }
                }

                //string inputFromFusion = _fusionSocket.GetMessage();
                //if (inputFromFusion != "") {
                //    Debug.Log(inputFromFusion);
                //    Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
                //    _fusionSocket.OnFusionReceived(this, new FusionEventArgs(inputFromFusion));
                //}
            }
            else {
                //SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
                //TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
                //_fusionSocket.OnConnectionLost(this, null);
                if (!commBridge.tryAgainRest.ContainsKey(epiSimUrl)) {
                    commBridge.tryAgainRest.Add(epiSimUrl, _cloudSocket.GetType());
                }
            }
        }
    }

    public void Get(string route) {
        RestDataContainer result = new RestDataContainer(this, wordcloudrestclient.Get(route));

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_wordcloudrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Post(string route, string content) {
        RestDataContainer result = new RestDataContainer(this, wordcloudrestclient.Post(route, content));

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_wordcloudrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Put(string route, string content) {
        RestDataContainer result = new RestDataContainer(this, wordcloudrestclient.Put(route, content));

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_wordcloudrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Delete(string route, string content) {
        RestDataContainer result = new RestDataContainer(this, wordcloudrestclient.Delete(route, content));

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_wordcloudrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_wordcloudrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }
}