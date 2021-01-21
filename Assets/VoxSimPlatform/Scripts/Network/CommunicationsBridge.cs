#if !UNITY_WEBGL
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using System.Xml.Serialization;

using VoxSimPlatform.NLU;

namespace VoxSimPlatform
{
	namespace Network
	{
		/// <summary>
		/// Class into which the contents of a socket config file is deserialized.
		/// The socket config file is in the local_config to the Assets folder (socket_config.xml).
		/// </summary>
		public class VoxSimSocketConfig
		{
			[XmlArray("SocketsList")]
			[XmlArrayItem("Socket")]
			public List<VoxSimSocket> Sockets = new List<VoxSimSocket>();
		}

		/// <summary>
		/// Socket node in socket config represents an endpoint.  It contains a Name (label), 
		///  URL (including port), Type (name of user-defined class that must inherit from either
		///  SocketConnection or RestClient), and Enabled value.
		/// </summary>
		public class VoxSimSocket
		{
			public string Name = "";
			public string Type = "";
			public string URL = "";
			public bool Enabled = false;
		}

		public class SocketEventArgs : EventArgs
		{
			public Type SocketType { get; set; }

			public SocketEventArgs(Type type)
			{
				this.SocketType = type;
			}
		}

		public class CommunicationsBridge : MonoBehaviour
		{
			List<string> socketLabels = new List<string>();
			List<string> socketTypes = new List<string>();
			List<string> socketUrls = new List<string>();
			List<bool> socketActiveStatuses = new List<bool>();

			private INLParser _parser;
			public INLParser parser
			{
				get { return _parser; }
				set { _parser = value; }
			}

			List<SocketConnection> _socketConnections;
			public List<SocketConnection> SocketConnections
			{
				get { return _socketConnections; }
			}
			public Dictionary<string, Type> tryAgainSockets = new Dictionary<string, Type>();

			List<RESTClient> _restClients;
			public List<RESTClient> RestClients
			{
				get { return _restClients; }
			}

			public Dictionary<string, Type> tryAgainRest = new Dictionary<string, Type>();

			public List<string> connected = new List<string>();

			// Make our calls from the Plugin
			[DllImport("CommunicationsBridge")]
			public static extern IntPtr PythonCall(string scriptsPath, string module, string function, string[] args,
				int numArgs);

			public event EventHandler PortOpened;

			public void OnPortOpened(object sender, EventArgs e)
			{
				if (PortOpened != null)
				{
					PortOpened(this, e);
				}
			}

			public int connectionRetryTimerTime;
			Timer connectionRetryTimer;
			bool retryConnections = false;

			void Start()
			{
				_socketConnections = new List<SocketConnection>();
				_restClients = new List<RESTClient>();
				connectionRetryTimer = new Timer(connectionRetryTimerTime);
				connectionRetryTimer.Enabled = true;
				connectionRetryTimer.Elapsed += RetryConnections;

				if (PlayerPrefs.HasKey("URLs"))
				{
					// TODO: Refactor generically
					//// Sets up sockets for external connections?
					//// Not actually entirely clear what refers to socketLabels et al

					List<string> socketStrings = PlayerPrefs.GetString("URLs").Split(';').ToList();
					int numSockets = socketStrings.Count;
					for (int i = 0; i < numSockets; i++)
					{
						string[] segments = socketStrings[i].Split(new char[] { '|', '=', ',' });
						try
						{
							socketLabels.Add(segments[0]);  // the current socket label
						}
						catch (Exception ex)
						{
							if (ex is ArgumentOutOfRangeException)
							{
								Debug.LogError(string.Format("Argument 0 (label) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
							}
						}

						try
						{
							socketTypes.Add(segments[1]);   // the current socket specified type (as string)
						}
						catch (Exception ex)
						{
							if (ex is ArgumentOutOfRangeException)
							{
								Debug.LogError(string.Format("Argument 1 (type) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
							}
						}
						try
						{
							socketUrls.Add(segments[2]);    // the current socket URL
						}
						catch (Exception ex)
						{
							if (ex is ArgumentOutOfRangeException)
							{
								Debug.LogError(string.Format("Argument 2 (URL) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
							}
						}

						try
						{
							socketActiveStatuses.Add(bool.Parse(segments[3]));  // is this socket to be active?
						}
						catch (Exception ex)
						{
							if (ex is ArgumentOutOfRangeException)
							{
								Debug.LogError(string.Format("Argument 3 (active status) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
							}
						}
						// split the URL into IP and port
						Debug.Log(segments[0] + " " + segments[1] + " " + segments[2]);
						if (socketActiveStatuses[i] == true)
						{
							string[] socketAddress = segments[2].Split(':'); // Assumes in form of IP address, not url
							if (!string.IsNullOrEmpty(socketAddress[0]))
							{
								Type socketType = null;
								// append Assembly-CSharp.dll assembly to search in
								socketType = Type.GetType(segments[1]) != null ? Type.GetType(segments[1]) : Type.GetType(segments[1] + ",Assembly-CSharp.dll");
								if (socketType != null)
								{
									if (socketType.IsSubclassOf(typeof(SocketConnection)))
									{
										SocketConnection newSocket = null;
										try
										{
											Debug.Log(string.Format("Creating new socket {0} of type {1}", segments[0], socketType));
											newSocket = ConnectSocket(socketAddress[0], Convert.ToInt32(socketAddress[1]),
												socketType);
											newSocket.Label = segments[0];
											_socketConnections.Add(newSocket);

											// add socket's IOClientType component to CommunicationsBridge
											gameObject.AddComponent(newSocket.IOClientType);
										}
										catch (Exception e)
										{
											Debug.Log(e.Message);
										}

										if (newSocket != null)
										{
											if (newSocket.IsConnected())
											{
												connected.Add(segments[2]);
											}
											else
											{
												if (!tryAgainSockets.ContainsKey(newSocket.Label))
												{
													Debug.Log(string.Format("Adding socket {0}@{1} to tryAgainSockets", newSocket.Label, segments[2]));
													tryAgainSockets.Add(segments[2], socketType);
												}
											}
										}
									}
									else if (socketType.IsSubclassOf(typeof(RESTClient)))
									{
										RESTClient newSocket = null;
										try
										{
											Debug.Log(string.Format("Creating new REST interface {0} of type {1}", segments[0], socketType));
											newSocket = CreateRESTClient(socketAddress[0], Convert.ToInt32(socketAddress[1]), socketType);
											newSocket.name = segments[0];
											_restClients.Add(newSocket);

											// add socket's IOClientType component to CommunicationsBridge
											gameObject.AddComponent(newSocket.clientType);
										}
										catch (Exception e)
										{
											Debug.Log(e.Message);
										}
									}
									else
									{
										Debug.LogWarning(string.Format("CommunicationsBridge.Start: Specified type {0} is not subclass of SocketConnection or RestClient.",
											socketType));
									}
								}
								else
								{
									Debug.Log(string.Format("CommunicationsBridge.Start: No type {0} found for socket", segments[1]));
								}
							}
						}
					}
				}
				else
				{
					Debug.Log("No input URLs specified.");
				}
				InitDefaultParser(); // Init the default (simple) parser
			}

			public void InitDefaultParser()
			{
				_parser = new SimpleParser();
			}

			void Update()
			{
				if ((retryConnections) && (tryAgainSockets.Keys.Count > 0))
				{
					foreach (string connectionUrl in tryAgainSockets.Keys)
					{
						if (tryAgainSockets[connectionUrl] != null)
						{
							//Debug.Log(connectionUrl);
							SocketConnection socket =
								_socketConnections.FirstOrDefault(s => s.GetType() == tryAgainSockets[connectionUrl]);
							if (socket != null)
							{
								if (!socket.IsConnected())
								{
									Debug.Log(string.Format("Retrying connection {0}@{1}", tryAgainSockets[connectionUrl],
										connectionUrl));
									// try again
									try
									{
										string[] url = connectionUrl.Split(':');
										string address = url[0];
										if (address != "")
										{
											int port = Convert.ToInt32(url[1]);
											try
											{
												Type socketType = tryAgainSockets[connectionUrl];
												TryReconnectSocket(address, port, socketType, ref socket);
											}
											catch (Exception e)
											{
												Debug.Log(e.Message);
											}
										}
									}
									catch (Exception e)
									{
										Debug.Log(e.Message);
									}

									if (socket.IsConnected())
									{
										connected.Add(connectionUrl);
									}
									else
									{
										//Debug.Log(string.Format("Connection to {0} is lost!", socket.GetType()));
									}
								}
							}
						}
					}

					foreach (string label in connected)
					{
						if (tryAgainSockets.ContainsKey(label))
						{
							tryAgainSockets.Remove(label);
						}
					}

					connected.Clear();

					retryConnections = false;
				}
			}

			void RetryConnections(object sender, ElapsedEventArgs e)
			{
				connectionRetryTimer.Interval = connectionRetryTimerTime;
				retryConnections = true;
			}
			public SocketConnection ConnectSocket(string address, int port, Type socketType)
			{
				Debug.Log(string.Format("Trying connection to {0}:{1} as type {2}", address, port, socketType));

				SocketConnection socket = (SocketConnection)Activator.CreateInstance(socketType);

				if (socket != null)
				{
					socket.owner = this;
					try
					{
						socket.Connect(address, port);
						Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
							socket.IsConnected(), socketType));
						socket.OnConnectionMade(this, new SocketEventArgs(socketType));
					}
					catch (Exception e)
					{
						Debug.Log(e.Message);
					}
				}
				else
				{
					Debug.Log("Failed to create client");
					//socket = null;
				}

				return socket;
			}

			public void TryReconnectSocket(string address, int port, Type socketType, ref SocketConnection socket)
			{
				if (socket != null)
				{
					try
					{
						socket.Connect(address, port);
						Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
							socket.IsConnected(), socketType));
						socket.OnConnectionMade(this, new SocketEventArgs(socketType));
					}
					catch (Exception e)
					{
						socket.OnConnectionLost(this, null);
						//Debug.Log(e.Message);
					}
				}
				else
				{
					Debug.Log("Failed to create client");
					//socket = null;
				}
			}

			public RESTClient CreateRESTClient(string address, int port, Type socketType)
			{
				Debug.Log(string.Format("Trying connection to {0}:{1} as type {2}", address, port, socketType));

				RESTClient client = (RESTClient)Activator.CreateInstance(socketType);
				if (client != null)
				{
					client.owner = this;
					try
					{
						client.GetError += client.ConnectionLost;
						StartCoroutine(TryConnectRESTClient(client, address, port));
						//Debug.Log(result.GetType());
						//Debug.Log(result.coroutine.GetType());
						//Debug.Log(result.result.GetType());
					}
					catch (Exception e)
					{
						Debug.Log(e.Message);
					}
				}
				else
				{
					Debug.Log("Failed to create client");
					//socket = null;
				}

				return client;
			}

			private IEnumerator TryConnectRESTClient(RESTClient client, string address, int port)
			{
				// Tries a connection, stores result in a RestDataContainer for future reference.
				RESTDataContainer result = new RESTDataContainer(this, client.TryConnect(address, port));
				//Debug.Log(string.Format("Result: {0}",((UnityWebRequestAsyncOperation)result.result).webRequest.responseCode));
				yield return result.result;
				// here make the check whether it is connected or not
				// unlike SocketConnection, RestClient needs to let the server yield
				// connection to commBridge comes later
				Debug.Log(result.result);
				Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
							client.isConnected, client.GetType()));

				if (client.isConnected)
				{
					connected.Add(client.address);
				}
				else
				{
					if (!tryAgainSockets.ContainsKey(client.name))
					{
						Debug.Log(string.Format("Adding socket {0}@{1} to tryAgainRest", client.name, client.address));
						tryAgainSockets.Add(client.address, client.clientType);
					}
				}
			}

			public string NLParse(string input)
			{
				var result = _parser.NLParse(input);
				if (result == "WAIT")
				{
					return "";
				}
				return result;
			}

			/// <summary>
			/// After waiting around for a new parse, collect it and send it on upward.
			/// </summary>
			/// <returns></returns>
			public string GrabParse()
			{
				return _parser.ConcludeNLParse();
			}

			public SocketConnection FindSocketConnectionByLabel(string label)
			{
				SocketConnection socket = null;

				socket = _socketConnections.FirstOrDefault(s => s.Label == label);

				return socket;
			}

			public SocketConnection FindSocketConnectionByLabel(string label, List<string> exclude)
			{
				SocketConnection socket = null;

				socket = _socketConnections.Except(_socketConnections.Where(s => exclude.Contains(s.Label))).
					FirstOrDefault(s => s.Label == label);

				return socket;
			}

			public SocketConnection FindSocketConnectionByType(Type type)
			{
				SocketConnection socket = null;

				socket = _socketConnections.FirstOrDefault(s => s.IOClientType == type);

				return socket;
			}

			public SocketConnection FindSocketConnectionByType(Type type, List<string> exclude)
			{
				SocketConnection socket = null;

				socket = _socketConnections.Except(_socketConnections.Where(s => exclude.Contains(s.Label))).
					FirstOrDefault(s => s.IOClientType == type);

				return socket;
			}

			public RESTClient FindRESTClientByLabel(string label)
			{
				RESTClient socket = null;

				socket = _restClients.FirstOrDefault(s => s.name == label);

				return socket;
			}

			public RESTClient FindRESTClientByLabel(string label, List<string> exclude)
			{
				RESTClient socket = null;

				socket = _restClients.Except(_restClients.Where(s => exclude.Contains(s.name))).
					FirstOrDefault(s => s.name == label);

				return socket;
			}

			public RESTClient FindRESTClientByType(Type type)
			{
				RESTClient socket = null;

				socket = _restClients.FirstOrDefault(s => s.clientType == type);

				return socket;
			}

			public RESTClient FindRESTClientByType(Type type, List<string> exclude)
			{
				RESTClient socket = null;

				socket = _restClients.Except(_restClients.Where(s => exclude.Contains(s.name))).
					FirstOrDefault(s => s.clientType == type);

				return socket;
			}

			void OnDestroy()
			{
				for (int i = 0; i < _socketConnections.Count; i++)
				{
					if (_socketConnections[i] != null && _socketConnections[i].IsConnected())
					{
						_socketConnections[i].Close();
						_socketConnections[i] = null;
					}
				}

				for (int i = 0; i < _restClients.Count; i++)
				{
					if (_restClients[i] != null && _restClients[i].isConnected)
					{
						_restClients[i] = null;
					}
				}
			}

			void OnApplicationQuit()
			{
				OnDestroy();
			}
		}
	}
}  
#endif