using UnityEngine;
using System.Collections;
using System.Linq;

using VoxSimPlatform.Network;

namespace VoxSimPlatform {
    namespace SpatialReasoning {
        namespace QSR {
            public class QSRLibIOClient : MonoBehaviour {
                QSRLibSocket _qsrLibSocket;
                public QSRLibSocket QSRLibSocket {
                    get { return _qsrLibSocket; }
                    set { _qsrLibSocket = value; }
                }

                CommunicationsBridge commBridge;

                // Use this for initialization
                void Start() {
                    commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
                    _qsrLibSocket = (QSRLibSocket)commBridge.FindSocketConnectionByType(typeof(QSRLibIOClient));
                }

                // Update is called once per frame
                void Update() {
                    if (_qsrLibSocket != null) {
                        string qsrUrl = string.Format("{0}:{1}", _qsrLibSocket.Address, _qsrLibSocket.Port);
                        if (_qsrLibSocket.IsConnected()) {
                            if (commBridge.tryAgainSockets.ContainsKey(qsrUrl)) {
                                if (commBridge.tryAgainSockets[qsrUrl] == typeof(QSRLibSocket)) {
                                    _qsrLibSocket = (QSRLibSocket)commBridge.FindSocketConnectionByType(typeof(QSRLibIOClient));
                                }
                            }

                            string inputFromQsrLib = _qsrLibSocket.GetMessage();
                            if (inputFromQsrLib != "") {
                                Debug.Log(inputFromQsrLib);
                                Debug.Log(_qsrLibSocket.HowManyLeft() + " messages left.");
                                _qsrLibSocket.OnQSRReceived(this, new QSRLibEventArgs(inputFromQsrLib));
                            }
                        }
                        else {
                            if (!commBridge.tryAgainSockets.ContainsKey(qsrUrl)) {
                                commBridge.tryAgainSockets.Add(qsrUrl, _qsrLibSocket.GetType());
                            }
                        }
                    }
                }
            }
        }
    }
}
