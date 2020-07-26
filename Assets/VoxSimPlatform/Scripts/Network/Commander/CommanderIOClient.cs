using UnityEngine;
using System.Collections;
using System.Linq;

namespace VoxSimPlatform {
    namespace Network {
        namespace Commander {
            public class CommanderIOClient : MonoBehaviour {
                CommanderSocket _commanderSocket;
                public CommanderSocket CommanderSocket {
                    get { return _commanderSocket; }
                    set { _commanderSocket = value; }
                }

                CommunicationsBridge commBridge;

                // Use this for initialization
                void Start() {
                    commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
                    _commanderSocket = (CommanderSocket)commBridge.FindSocketConnectionByType(typeof(CommanderIOClient));
                }

                // Update is called once per frame
                void Update() {
                    if (_commanderSocket != null) {
                        string fusionUrl = string.Format("{0}:{1}", _commanderSocket.Address, _commanderSocket.Port);
                        if (_commanderSocket.IsConnected()) {
                            if (commBridge.tryAgainSockets.ContainsKey(fusionUrl)) {
                                if (commBridge.tryAgainSockets[fusionUrl] == typeof(CommanderSocket)) {
                                    _commanderSocket = (CommanderSocket)commBridge.FindSocketConnectionByType(typeof(CommanderIOClient));
                                    //Debug.Log(_fusionSocket.IsConnected());
                                }
                            }
                        }
                        else {
                            //SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
                            //TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
                            //_fusionSocket.OnConnectionLost(this, null);
                            if (!commBridge.tryAgainSockets.ContainsKey(fusionUrl)) {
                                commBridge.tryAgainSockets.Add(fusionUrl, _commanderSocket.GetType());
                            }
                        }
                    }
                }
            }
        }
    }
}
