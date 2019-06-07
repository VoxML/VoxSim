using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;

public class KSIMIOClient : MonoBehaviour {
    KSIMSocket _ksimSocket;
    public KSIMSocket KSIMSocket {
        get { return _ksimSocket; }
        set { _ksimSocket = value; }
    }

    CommunicationsBridge commBridge;

    // Use this for initialization
    void Start() {
        commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
        _ksimSocket = (KSIMSocket)commBridge.FindSocketConnectionByLabel("KSIM");
    }

    // Update is called once per frame
    void Update() {
        if (_ksimSocket != null) {
            string ksimUrl = string.Format("{0}:{1}", _ksimSocket.Address, _ksimSocket.Port);
            if (_ksimSocket.IsConnected()) {
                if (commBridge.tryAgainSockets.ContainsKey(ksimUrl)) {
                    if (commBridge.tryAgainSockets[ksimUrl] == typeof(FusionSocket)) {
                        _ksimSocket = (KSIMSocket)commBridge.FindSocketConnectionByLabel("KSIM");
                        //Debug.Log(_fusionSocket.IsConnected());
                    }
                }
            }
            else {
                //SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
                //TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
                //_fusionSocket.OnConnectionLost(this, null);
                if (!commBridge.tryAgainSockets.ContainsKey(ksimUrl)) {
                    commBridge.tryAgainSockets.Add(ksimUrl, _ksimSocket.GetType());
                }
            }
        }
    }
}
