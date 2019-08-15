using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;

public class NLUIOClient : MonoBehaviour {
    NLURestClient _nluSocket;

    /// <summary>
    /// Pretty much the same as the EpiSimIOClient, but with name changes
    /// </summary>
    public NLURestClient nlurestclient {
        get { return _nluSocket; }
        set { _nluSocket = value; }
    }

    CommunicationsBridge commBridge;

    // Use this for initialization
    void Start() {
        commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        //_nlurestclient = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
        _nluSocket = (NLURestClient)commBridge.FindRestClientByLabel("NLTK");
    }

    // Update is called once per frame
    void Update() {
        if (_nluSocket != null) {
            string epiSimUrl = string.Format("{0}:{1}", _nluSocket.address, _nluSocket.port);
            if (_nluSocket.isConnected) {
                if (commBridge.tryAgainSockets.ContainsKey(epiSimUrl)) {
                    if (commBridge.tryAgainSockets[epiSimUrl] == typeof(FusionSocket)) {
                        _nluSocket = (NLURestClient)commBridge.FindRestClientByLabel("NLTK"); // Maybe wrong
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
                    commBridge.tryAgainRest.Add(epiSimUrl, _nluSocket.GetType());
                }
            }
        }
    }

    public void Get(string route) {
        nlurestclient.Get(route);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Post(string route, string content) {
        nlurestclient.Post(route,content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Put(string route, string content) {
        nlurestclient.Put(route, content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Delete(string route, string content) {
        nlurestclient.Delete(route, content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_nlurestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_nlurestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }
}