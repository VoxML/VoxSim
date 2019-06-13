using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;

public class FusionIOClient : MonoBehaviour {
    FusionSocket _fusionSocket;
    public FusionSocket FusionSocket {
        get { return _fusionSocket; }
        set { _fusionSocket = value; }
    }

    CommunicationsBridge commBridge;

    // Use this for initialization
    void Start() {
        commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        _fusionSocket = (FusionSocket)commBridge.FindSocketConnectionByLabel("Fusion");
    }

    // Update is called once per frame
    void Update() {
        if (_fusionSocket != null) {
            string fusionUrl = string.Format("{0}:{1}", _fusionSocket.Address, _fusionSocket.Port);
            if (_fusionSocket.IsConnected()) {
                if (commBridge.tryAgainSockets.ContainsKey(fusionUrl)) {
                    if (commBridge.tryAgainSockets[fusionUrl] == typeof(FusionSocket)) {
                        _fusionSocket = (FusionSocket)commBridge.FindSocketConnectionByLabel("Fusion");
                        //Debug.Log(_fusionSocket.IsConnected());
                    }
                }

                string inputFromFusion = _fusionSocket.GetMessage();
                if (inputFromFusion != "") {
                    Debug.Log(inputFromFusion);
                    Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
                    _fusionSocket.OnFusionReceived(this, new FusionEventArgs(inputFromFusion));
                }
            }
            else {
                //SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
                //TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
                //_fusionSocket.OnConnectionLost(this, null);
                if (!commBridge.tryAgainSockets.ContainsKey(fusionUrl)) {
                    commBridge.tryAgainSockets.Add(fusionUrl, _fusionSocket.GetType());
                }
            }
        }
    }
}
