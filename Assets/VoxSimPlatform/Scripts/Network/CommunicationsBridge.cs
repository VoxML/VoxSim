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
using VoxSimPlatform.Network;
using VoxSimPlatform.Agent;

namespace VoxSimPlatform {
    namespace Network {
        /// <summary>
        /// Class into which the contents of a socket config file is deserialized.
        /// The socket config file is in the local_config to the Assets folder (socket_config.xml).
        /// </summary>
        public class VoxSimSocketConfig {
            [XmlArray("SocketsList")]
            [XmlArrayItem("Socket")]
            public List<VoxSimSocket> Sockets = new List<VoxSimSocket>();
        }

        /// <summary>
        /// Socket node in socket config represents an endpoint.  It contains a Name (label), 
        ///  URL (including port), Type (name of user-defined class that must inherit from either
        ///  SocketConnection or RestClient), and Enabled value.
        /// </summary>
        public class VoxSimSocket {
            public string Name = "";
            public string Type = "";
            public string URL = "";
            public bool Enabled = false;
        }

        public class SocketEventArgs : EventArgs {
            public Type SocketType { get; set; }

            public SocketEventArgs(Type type) {
                this.SocketType = type;
            }
        }

        public class CommunicationsBridge : MonoBehaviour {
            List<string> socketLabels = new List<string>();
            List<string> socketTypes = new List<string>();
            List<string> socketUrls = new List<string>();
            List<bool> socketActiveStatuses = new List<bool>();

            private INLParser _parser;
            public INLParser parser {
                get { return _parser; }
                set { _parser = value; }
            }

            //private CmdServer _cmdServer;
            //private FusionSocket _fusionSocket;
            //private EventLearningSocket _eventLearningSocket;
            //private StructureLearningSocket _structureLearningSocket;
            //private CommanderSocket _commanderSocket;
            //private KSIMSocket _ksimSocket;
            //private ADESocket _adeSocket;

            //public FusionSocket FusionSocket {
            //    get { return _fusionSocket; }
            //}

            //public EventLearningSocket EventLearningSocket {
            //    get { return _eventLearningSocket; }
            //}

            //public StructureLearningSocket StructureLearningSocket {
            //    get { return _structureLearningSocket; }
            //}

            //public CommanderSocket CommanderSocket {
            //    get { return _commanderSocket; }
            //}

            //public KSIMSocket KSIMSocket {
            //    get { return _ksimSocket; }
            //}

            //public ADESocket ADESocket {
            //    get { return _adeSocket; }
            //}

            List<SocketConnection> _socketConnections;
            public List<SocketConnection> SocketConnections {
                get { return _socketConnections; }
            }
            public Dictionary<string, Type> tryAgainSockets = new Dictionary<string, Type>();

            List<RestClient> _restClients;
            public List<RestClient> RestClients {
                get { return _restClients; }
            }

            public Dictionary<string, Type> tryAgainRest = new Dictionary<string, Type>();

            public List<string> connected = new List<string>();

            // Make our calls from the Plugin
            [DllImport("CommunicationsBridge")]
            public static extern IntPtr PythonCall(string scriptsPath, string module, string function, string[] args,
                int numArgs);

            public event EventHandler PortOpened;

            public void OnPortOpened(object sender, EventArgs e) {
                if (PortOpened != null) {
                    PortOpened(this, e);
                }
            }

            public int connectionRetryTimerTime;
            Timer connectionRetryTimer;
            bool retryConnections = false;

            void Start() {
                _socketConnections = new List<SocketConnection>();
                _restClients = new List<RestClient>();
                connectionRetryTimer = new Timer(connectionRetryTimerTime);
                connectionRetryTimer.Enabled = true;
                connectionRetryTimer.Elapsed += RetryConnections;

                //string port = PlayerPrefs.GetString("Listener Port");
                //if (port != "") {
                //    OpenPortInternal(port);
                //}
                //else {
                //    Debug.Log("No listener port specified. Skipping interface startup.");
                //}

                if (PlayerPrefs.HasKey("URLs")) {
                    // TODO: Refactor generically
                    //// Sets up sockets for external connections?
                    //// Not actually entirely clear what refers to socketLabels et al

                    List<string> socketStrings = PlayerPrefs.GetString("URLs").Split(';').ToList();
                    int numSockets = socketStrings.Count;
                    for (int i = 0; i < numSockets; i++) {
                        string[] segments = socketStrings[i].Split(new char[] { '|', '=', ',' });
                        try {
                            socketLabels.Add(segments[0]);  // the current socket label
                        }
                        catch (Exception ex) {
                            if (ex is ArgumentOutOfRangeException)
                            {
                                Debug.LogError(string.Format("Argument 0 (label) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
                            }
                        }

                        try {
                            socketTypes.Add(segments[1]);   // the current socket specified type (as string)
                        }
                        catch (Exception ex) {
                            if (ex is ArgumentOutOfRangeException) {
                                Debug.LogError(string.Format("Argument 1 (type) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
                            }
                        }
                        try {
                            socketUrls.Add(segments[2]);    // the current socket URL
                        }
                        catch (Exception ex) {
                            if (ex is ArgumentOutOfRangeException) {
                                Debug.LogError(string.Format("Argument 2 (URL) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
                            }
                        }

                        try {
                            socketActiveStatuses.Add(bool.Parse(segments[3]));  // is this socket to be active?
                        }
                        catch (Exception ex) {
                            if (ex is ArgumentOutOfRangeException) {
                                Debug.LogError(string.Format("Argument 3 (active status) for socket #{0} is not specified!  Try resaving socket connections from the main menu!", i));
                            }
                        }
                        // split the URL into IP and port
                        Debug.Log(segments[0] + " " + segments[1] + " " + segments[2]);
                        if (socketActiveStatuses[i] == true) {
                            string[] socketAddress = segments[2].Split(':'); //// Assumes in form of IP address, not url
                            if (!string.IsNullOrEmpty(socketAddress[0])) {
                                Type socketType = null;
                                socketType = Type.GetType(segments[1]);
                                //if(socketType == null) {
                                    //socketType = typeof(NLURestClient);
                                //    socketType = Type.GetType("VoxSimPlatform.Network." + segments[1]); //ugh, GetType doesn't find it by default
                                //}
                                //socketType = Type.GetType("String");
                                if (socketType != null) {
                                    if (socketType.IsSubclassOf(typeof(SocketConnection))) {
                                        SocketConnection newSocket = null;
                                        try {
                                            Debug.Log(string.Format("Creating new socket {0} of type {1}", segments[0], socketType));
                                            newSocket = ConnectSocket(socketAddress[0], Convert.ToInt32(socketAddress[1]),
                                                socketType);
                                            newSocket.Label = segments[0];
                                            _socketConnections.Add(newSocket);

                                            // add socket's IOClientType component to CommunicationsBridge
                                            gameObject.AddComponent(newSocket.IOClientType);
                                        }
                                        catch (Exception e) {
                                            Debug.Log(e.Message);
                                        }

                                        if (newSocket != null) {
                                            if (newSocket.IsConnected()) {
                                                connected.Add(segments[2]);
                                            }
                                            else {
                                                if (!tryAgainSockets.ContainsKey(newSocket.Label)) {
                                                    Debug.Log(string.Format("Adding socket {0}@{1} to tryAgainSockets", newSocket.Label, segments[2]));
                                                    tryAgainSockets.Add(segments[2], socketType);
                                                }
                                            }
                                        }
                                    }
                                    else if (socketType.IsSubclassOf(typeof(RestClient))) {
                                        RestClient newSocket = null;
                                        try {
                                            Debug.Log(string.Format("Creating new REST interface {0} of type {1}", segments[0], socketType));
                                            newSocket = CreateRestClient(socketAddress[0], Convert.ToInt32(socketAddress[1]), socketType);
                                            newSocket.name = segments[0];
                                            _restClients.Add(newSocket);

                                            // add socket's IOClientType component to CommunicationsBridge
                                            gameObject.AddComponent(newSocket.clientType);
                                        }
                                        catch (Exception e) {
                                            Debug.Log(e.Message);
                                        }

                                        //if (newSocket != null) {
                                        //    if (newSocket.isConnected) {
                                        //        connected.Add(segments[2]);
                                        //    }
                                        //    else {
                                        //        if (!tryAgainSockets.ContainsKey(newSocket.name)) {
                                        //            Debug.Log(string.Format("Adding socket {0}@{1} to tryAgainRest", newSocket.name, segments[2]));
                                        //            tryAgainSockets.Add(segments[2], socketType);
                                        //        }
                                        //    }
                                        //}
                                    }
                                    else {
                                        Debug.LogWarning(string.Format("CommunicationsBridge.Start: Specified type {0} is not subclass of SocketConnection or RestClient.",
                                            socketType));
                                    }
                                }
                                else {
                                        Debug.Log(string.Format("CommunicationsBridge.Start: No type {0} found for socket", segments[1]));
                                }
                            }
                        }
                    }
                }
                else {
                    Debug.Log("No input URLs specified.");
                }
                InitDefaultParser(); // Init the default (simple) parser
            }

            public void InitDefaultParser() {
                _parser = new SimpleParser();
            }

            //public void InitParser() {
            //    NLUIOClient nluIO = gameObject.GetComponent<NLUIOClient>();
            //    if (nluIO == null) {
            //        _parser = new SimpleParser();
                    //
            //    }
            //    else { // I think that using the IO handlers is what's expected here
            //        _parser = new PythonJSONParser();
            //        _parser.InitParserService(nluIO);
            //    }
            //}

            void Update() {
                //if (_fusionSocket != null) {
                //    if (_fusionSocket.IsConnected()) {
                //        string inputFromFusion = _fusionSocket.GetMessage();
                //        if (inputFromFusion != "") {
                //            Debug.Log(inputFromFusion);
                //            Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
                //            _fusionSocket.OnFusionReceived(this, new FusionEventArgs(inputFromFusion));
                //        }
                //    }
                //    else {
                //        //SocketConnection _retry = socketConnections.FirstOrDefault(s => s.GetType() == typeof(FusionSocket));
                //        //TryReconnectSocket(_fusionSocket.Address, _fusionSocket.Port, typeof(FusionSocket), ref _retry);
                //        //_fusionSocket.OnConnectionLost(this, null);
                //        string fusionAddress = string.Format("{0}:{1}", _fusionSocket.Address, _fusionSocket.Port);
                //        if (!tryAgain.ContainsKey(fusionAddress)) {
                //            tryAgain.Add(fusionAddress, _fusionSocket.GetType());
                //        }
                //    }
                //}

                //if (_cmdServer != null) {
                //    string inputFromCommander = _cmdServer.GetMessage();
                //    if (inputFromCommander != "") {
                //        Debug.Log(inputFromCommander);
                //        ((InputController) (GameObject.Find("IOController").GetComponent("InputController"))).inputString =
                //            inputFromCommander.Trim();
                //        ((InputController) (GameObject.Find("IOController").GetComponent("InputController"))).MessageReceived(
                //            inputFromCommander.Trim());
                //    }
                //}

                //if (_commanderSocket != null) {
        //      //      Debug.Log (_commanderClient.IsConnected ());
                //    string inputFromCommander = _commanderSocket.GetMessage();
                //    if (inputFromCommander != "") {
                //        Debug.Log(inputFromCommander);
                //        ((InputController) (GameObject.Find("IOController").GetComponent("InputController"))).inputString =
                //            inputFromCommander.Trim();
                //        ((InputController) (GameObject.Find("IOController").GetComponent("InputController"))).MessageReceived(
                //            inputFromCommander.Trim());
                //    }
                //}

                //if (_ksimSocket != null) {
                //    if (_ksimSocket.IsConnected()) {
                //    }
                //    else {
                //        _ksimSocket.OnConnectionLost(this, null);
                //        string ksimAddress = string.Format("{0}:{1}", _ksimSocket.Address, _ksimSocket.Port);
                //        if (!tryAgainSockets.ContainsKey(ksimAddress)) {
                //            tryAgainSockets.Add(ksimAddress, _ksimSocket.GetType());
                //        }
                //    }
                //}

                //if (_adeSocket != null) {
                //    if (_adeSocket.IsConnected()) {
                //    }
                //    else {
                //        _adeSocket.OnConnectionLost(this, null);
                //        string adeAddress = string.Format("{0}:{1}", _adeSocket.Address, _adeSocket.Port);
                //        if (!tryAgainSockets.ContainsKey(adeAddress)) {
                //            tryAgainSockets.Add(adeAddress, _adeSocket.GetType());
                //        }
                //    }
                //}

                if ((retryConnections) && (tryAgainSockets.Keys.Count > 0)) {
                    foreach (string connectionUrl in tryAgainSockets.Keys) {
                        if (tryAgainSockets[connectionUrl] != null) {
                            SocketConnection socket =
                                _socketConnections.FirstOrDefault(s => s.GetType() == tryAgainSockets[connectionUrl]);
                            if (socket != null) {
                                if (!socket.IsConnected()) {
                                    Debug.Log(string.Format("Retrying connection {0}@{1}", tryAgainSockets[connectionUrl],
                                        connectionUrl));
                                    // try again
                                    try {
                                        string[] url = connectionUrl.Split(':');
                                        string address = url[0];
                                        if (address != "") {
                                            int port = Convert.ToInt32(url[1]);
                                            try {
                                                Type socketType = tryAgainSockets[connectionUrl];
                                                TryReconnectSocket(address, port, socketType, ref socket);
                                            }
                                            catch (Exception e) {
                                                Debug.Log(e.Message);
                                            }
                                        }
                                    }
                                    catch (Exception e) {
                                        Debug.Log(e.Message);
                                    }

                                    if (socket.IsConnected()) {
                                        connected.Add(connectionUrl);
                                    }
                                    else {
                                        //Debug.Log(string.Format("Connection to {0} is lost!", socket.GetType()));
                                    }

                                    //if (tryAgainSockets[connectionUrl] == typeof(FusionSocket)) {
                                        //_fusionSocket = (FusionSocket) socket;
                                        //Debug.Log(_fusionSocket.IsConnected());
                                    //}
                                    //else if (tryAgainSockets[connectionUrl] == typeof(KSIMSocket)) {
                                    //    _ksimSocket = (KSIMSocket) socket;

                                    //    if (_ksimSocket.IsConnected()) {
                                            // register VoxSim
                                    //        byte[] bytes = BitConverter.GetBytes(1).Concat(new byte[] {0x02}).ToArray<byte>();
                                    //        _ksimSocket.Write(bytes);
                                    //    }
                                    //}
                                    //else if (tryAgainSockets[connectionUrl] == typeof(ADESocket)) {
                                    //    _adeSocket = (ADESocket) socket;
                                        //Debug.Log(_fusionSocket.IsConnected());
                                    //}
                                }
                            }
                        }
                    }

                    foreach (string label in connected) {
                        if (tryAgainSockets.ContainsKey(label)) {
                            tryAgainSockets.Remove(label);
                        }
                    }

                    connected.Clear();

                    retryConnections = false;
                }
            }

            void RetryConnections(object sender, ElapsedEventArgs e) {
                connectionRetryTimer.Interval = connectionRetryTimerTime;
                retryConnections = true;
            }

            public SocketConnection ConnectSocket(string address, int port, Type socketType) {
                Debug.Log(string.Format("Trying connection to {0}:{1} as type {2}", address, port, socketType));

                SocketConnection socket = (SocketConnection)Activator.CreateInstance(socketType);

                if (socket != null) {
                    socket.owner = this;
                    try {
                        socket.Connect(address, port);
                        Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
                            socket.IsConnected(), socketType));
                        socket.OnConnectionMade(this, new SocketEventArgs(socketType));
                    }
                    catch (Exception e) {
                        Debug.Log(e.Message);
                    }
                }
                else {
                    Debug.Log("Failed to create client");
                    //socket = null;
                }

                return socket;
            }

            public void TryReconnectSocket(string address, int port, Type socketType, ref SocketConnection socket) {
                if (socket != null) {
                    try {
                        socket.Connect(address, port);
                        Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
                            socket.IsConnected(), socketType));
                        socket.OnConnectionMade(this, new SocketEventArgs(socketType));
                    }
                    catch (Exception e) {
                        socket.OnConnectionLost(this, null);
                        //Debug.Log(e.Message);
                    }
                }
                else {
                    Debug.Log("Failed to create client");
                    //socket = null;
                }
            }

            public RestClient CreateRestClient(string address, int port, Type socketType) {
                Debug.Log(string.Format("Trying connection to {0}:{1} as type {2}", address, port, socketType));

                RestClient client = (RestClient)Activator.CreateInstance(socketType);
                if (client != null) {
                    client.owner = this;
                    try {
                        client.PostError += client.ConnectionLost;
                        StartCoroutine(TryConnectRestClient(client, address, port));
                        //Debug.Log(result.GetType());
                        //Debug.Log(result.coroutine.GetType());
                        //Debug.Log(result.result.GetType());
                    }
                    catch (Exception e) {
                        Debug.Log(e.Message);
                    }
                }
                else {
                    Debug.Log("Failed to create client");
                    //socket = null;
                }

                return client;
            }

            private IEnumerator TryConnectRestClient(RestClient client, string address, int port) {
                // Tries a connection, stores result in a RestDataContainer for future reference.
                RestDataContainer result = new RestDataContainer(this, client.TryConnect(address, port));
                //Debug.Log(string.Format("Result: {0}",((UnityWebRequestAsyncOperation)result.result).webRequest.responseCode));
                yield return result.result;
                // here make the check whether it is connected or not
                // unlike SocketConnection, RestClient needs to let the server yield
                // connection to commBridge comes later
                Debug.Log(result.result);
                Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port,
                            client.isConnected, client.GetType()));

                if (client.isConnected) {
                    connected.Add(client.address);
                }
                else {
                    if (!tryAgainSockets.ContainsKey(client.name)) {
                        Debug.Log(string.Format("Adding socket {0}@{1} to tryAgainRest", client.name, client.address));
                        tryAgainSockets.Add(client.address, client.clientType);
                    }
                }
            }

            //public void OpenPortInternal(string port) {
            //    try {
                    // pass true as first param to make the server visible only to 'localhost'
                    // (for testing, for exmaple)
                    //_cmdServer = new CmdServer(false, int.Parse(port), 1);
            //        OnPortOpened(this, null);
            //    }
            //    catch (Exception e) {
            //        Debug.Log("Failed to open port " + port);
            //        Debug.Log(e.Message);
            //        Debug.Log(e.InnerException);
            //        Debug.Log(e.StackTrace);
            //        Debug.Log(e.Data);
            //    }
            //}

            public string NLParse(string input) {
        //        string[] args = new string[]{input};
        //        string result = Marshal.PtrToStringAuto(PythonCall (Application.dataPath + "/Externals/python/", "change_to_forms", "parse_sent", args, args.Length));
                var result = _parser.NLParse(input);
                //Debug.LogWarning("Parsed as: " + result);
                if(result == "WAIT"){
                    return "";
                }
                return result;
            }

            /// <summary>
            /// After waiting around for a new parse, collect it and send it on upward.
            /// </summary>
            /// <returns></returns>
            public string GrabParse() {
                return _parser.ConcludeNLParse();
            }

            public SocketConnection FindSocketConnectionByLabel(string label) {
                SocketConnection socket = null;

                socket = _socketConnections.FirstOrDefault(s => s.Label == label);

                return socket;
            }

            public SocketConnection FindSocketConnectionByType(Type type) {
                SocketConnection socket = null;

                socket = _socketConnections.FirstOrDefault(s => s.IOClientType == type);

                return socket;
            }

            public RestClient FindRestClientByLabel(string label) {
                RestClient socket = null;

                socket = _restClients.FirstOrDefault(s => s.name == label);

                return socket;
            }
                
            public RestClient FindRestClientByType(Type type) {
                RestClient socket = null;

                socket = _restClients.FirstOrDefault(s => s.clientType == type);

                return socket;
            }

            void OnDestroy() {
                //if (_cmdServer != null) {
                //    _cmdServer.Close();
                //    _cmdServer = null;
                //}

                for (int i = 0; i < _socketConnections.Count; i++) {
                    if (_socketConnections[i] != null && _socketConnections[i].IsConnected()) {
                        _socketConnections[i].Close();
                        _socketConnections[i] = null;
                    }
                }

                for (int i = 0; i < _restClients.Count; i++) {
                    if (_restClients[i] != null && _restClients[i].isConnected) {
                        _restClients[i] = null;
                    }
                }

                //if (_fusionSocket != null && _fusionSocket.IsConnected()) {
                //    _fusionSocket.Close();
                //    _fusionSocket = null;
                //}

                //if (_commanderSocket != null && _commanderSocket.IsConnected()) {
                //    _commanderSocket.Close();
                //    _commanderSocket = null;
                //}

                //if (_ksimSocket != null && _ksimSocket.IsConnected()) {
                //    _ksimSocket.Close();
                //    _ksimSocket = null;
                //}

                //if (_adeSocket != null && _adeSocket.IsConnected()) {
                //    _adeSocket.Close();
                //    _adeSocket = null;
                //}
            }

            void OnApplicationQuit() {
                OnDestroy();
            }
        }
    }
}