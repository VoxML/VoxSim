using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;
using VoxSimPlatform.NLU;
using System.Net.Sockets;


// Making the probably-a-mistake of having some rest client and some socket connection interface to clic
namespace VoxSimPlatform {
    namespace Network {

        public class ClicIOClient : MonoBehaviour {
            ClicRestClient _clicSocket;

            /// <summary>
            /// Pretty much the same as the EpiSimIOClient, but with name changes
            /// </summary>
            public ClicRestClient clicrestclient {
                get { return _clicSocket; }
                set { _clicSocket = value; }
            }

            CommunicationsBridge commBridge;
            BrowserInterface browser_interface;
            Socket s;


            // Use this for initialization
            void Start() {
                browser_interface = gameObject.GetComponent<BrowserInterface>();
                commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
                //_clicrestclient = (EpistemicState)commBridge.FindRestClientByLabel("EpiSim");
                //_clicSocket = (ClicRestClient)commBridge.FindRestClientByLabel("EpiSim");
                _clicSocket = new ClicRestClient();
                _clicSocket.port = 8000;
                _clicSocket.address = "localhost";
                _clicSocket.owner = browser_interface;
          
            }

            // Update is called once per frame
            void Update() {
                if (_clicSocket != null) {
                    string epiSimUrl = string.Format("{0}:{1}", _clicSocket.address, _clicSocket.port);
                    if (_clicSocket.isConnected) {
                        if (commBridge.tryAgainSockets.ContainsKey(epiSimUrl)) {
                            if (commBridge.tryAgainSockets[epiSimUrl] == typeof(FusionSocket)) {
                                _clicSocket = (ClicRestClient)commBridge.FindRestClientByLabel("Parser URL"); // Maybe wrong
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
                            commBridge.tryAgainRest.Add(epiSimUrl, _clicSocket.GetType());
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.B)) {
                    Post("/clic/say?text=", "Make+the+gene+set.");
                }
            }

            //Get... may need to happen directly through the ports
            public void Get(string route) {
                RestDataContainer result = new RestDataContainer(this, clicrestclient.Get(route));

                //if (result.result.webRequest.isNetworkError) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
                //}
                //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
                //        SendMessageOptions.DontRequireReceiver);
                //}
                //else {
                //    //Debug.Log (webRequest.downloadHandler.text);
                //    gameObject.BroadcastMessage(_clicrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
                //}
            }


            // Posting can be done to the web interface
            public void Post(string route, string content) {
                Debug.LogWarning("ROUTE/CONTENT:" + route + content);
                RestDataContainer result = new RestDataContainer(this, clicrestclient.Post(route, content));
                Debug.LogWarning(result);
                //if (result.result.webRequest.isNetworkError) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
                //}
                //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
                //        SendMessageOptions.DontRequireReceiver);
                //}
                //else {
                //    //Debug.Log (webRequest.downloadHandler.text);
                //    gameObject.BroadcastMessage(_clicrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
                //}
            }

            public void Put(string route, string content) {
                RestDataContainer result = new RestDataContainer(this, clicrestclient.Put(route, content));

                //if (result.result.webRequest.isNetworkError) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
                //}
                //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
                //        SendMessageOptions.DontRequireReceiver);
                //}
                //else {
                //    //Debug.Log (webRequest.downloadHandler.text);
                //    gameObject.BroadcastMessage(_clicrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
                //}
            }

            public void Delete(string route, string content) {
                RestDataContainer result = new RestDataContainer(this, clicrestclient.Delete(route, content));

                //if (result.result.webRequest.isNetworkError) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.error, SendMessageOptions.DontRequireReceiver);
                //}
                //else if (result.result.webRequest.responseCode < 200 || result.result.webRequest.responseCode >= 400) {
                //    gameObject.BroadcastMessage(_clicrestclient.ErrorStr, result.result.webRequest.downloadHandler.text,
                //        SendMessageOptions.DontRequireReceiver);
                //}
                //else {
                //    //Debug.Log (webRequest.downloadHandler.text);
                //    gameObject.BroadcastMessage(_clicrestclient.SuccessStr, result.result.webRequest.downloadHandler.text);
                //}
            }
        }
    }
}