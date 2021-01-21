#if !UNITY_WEBGL
using UnityEngine;

using VoxSimPlatform.Network;

namespace VoxSimPlatform
{
	namespace Examples
	{
		namespace IOClients
		{
			public class RelationExtractorIOClient : MonoBehaviour
			{
				SocketConnections.RelationExtractorSocket _relationExtractorSocket;
				public SocketConnections.RelationExtractorSocket RelationExtractorSocket
				{
					get { return _relationExtractorSocket; }
					set { _relationExtractorSocket = value; }
				}

				CommunicationsBridge commBridge;

				// Use this for initialization
				void Start()
				{
					commBridge = GameObject.Find("CommunicationsBridge").GetComponent<CommunicationsBridge>();
					_relationExtractorSocket = (SocketConnections.RelationExtractorSocket)commBridge.FindSocketConnectionByType(typeof(RelationExtractorIOClient));
				}

				// Update is called once per frame
				void Update()
				{
					if (_relationExtractorSocket != null)
					{
						string fusionUrl = string.Format("{0}:{1}", _relationExtractorSocket.Address, _relationExtractorSocket.Port);
						if (_relationExtractorSocket.IsConnected())
						{
							if (commBridge.tryAgainSockets.ContainsKey(fusionUrl))
							{
								if (commBridge.tryAgainSockets[fusionUrl] == typeof(SocketConnections.RelationExtractorSocket))
								{
									_relationExtractorSocket = (SocketConnections.RelationExtractorSocket)commBridge.FindSocketConnectionByType(typeof(RelationExtractorIOClient));
								}
							}
						}
						else
						{
							if (!commBridge.tryAgainSockets.ContainsKey(fusionUrl))
							{
								commBridge.tryAgainSockets.Add(fusionUrl, _relationExtractorSocket.GetType());
							}
						}
					}
				}
			}
		}
	}
}

#endif