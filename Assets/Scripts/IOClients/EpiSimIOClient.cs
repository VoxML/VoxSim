using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;

public class EpiSimIOClient : MonoBehaviour {
    EpistemicState _epiSimSocket;
    public EpistemicState EpiSimSocket {
        get { return _epiSimSocket; }
        set { _epiSimSocket = value; }
    }

    CommunicationsBridge commBridge;

    // Use this for initialization
    void Start() {
        commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        _epiSimSocket = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
    }

    // Update is called once per frame
    void Update() {
        if (_epiSimSocket != null) {
            string epiSimUrl = string.Format("{0}:{1}", _epiSimSocket.address, _epiSimSocket.port);
            if (_epiSimSocket.isConnected) {
                if (commBridge.tryAgainSockets.ContainsKey(epiSimUrl)) {
                    if (commBridge.tryAgainSockets[epiSimUrl] == typeof(FusionSocket)) {
                        _epiSimSocket = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
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
                    commBridge.tryAgainRest.Add(epiSimUrl, _epiSimSocket.GetType());
                }
            }
        }
    }

    public void Get(string route) {
        EpiSimSocket.Get(route);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_epiSimSocket.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Post(string route, string content) {
        EpiSimSocket.Post(route,content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_epiSimSocket.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Put(string route, string content) {
       EpiSimSocket.Put(route, content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_epiSimSocket.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }

    public void Delete(string route, string content) {
        EpiSimSocket.Delete(route, content);

        //if (result.result.webRequest.isNetworkError) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
        //}
        //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
        //    gameObject.BroadcastMessage(_epiSimSocket.ErrorStr, result.result.webRequest.downloadHandler.text,
        //        SendMessageOptions.DontRequireReceiver);
        //}
        //else {
        //    //Debug.Log (webRequest.downloadHandler.text);
        //    gameObject.BroadcastMessage(_epiSimSocket.SuccessStr, result.result.webRequest.downloadHandler.text);
        //}
    }
}