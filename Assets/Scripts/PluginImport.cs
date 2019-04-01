using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Network;
using NLU;

public class SocketEventArgs : EventArgs
{
    public Type SocketType { get; set; }

    public SocketEventArgs(Type type)
    {
        this.SocketType = type;
    }
}

public class PluginImport : MonoBehaviour {
	private INLParser _parser;
	private CmdServer _cmdServer;
    private FusionSocket _fusionSocket;
    private EventLearningSocket _eventLearningSocket;
    private StructureLearningSocket _structureLearningSocket;
    private CommanderSocket _commanderSocket;
    private KSIMSocket _ksimSocket;
    private ADESocket _adeSocket;

	public FusionSocket FusionSocket {
        get { return _fusionSocket; }
	}

    public EventLearningSocket EventLearningSocket {
		get { return _eventLearningSocket; }
	}

    public StructureLearningSocket StructureLearningSocket {
        get { return _structureLearningSocket; }
    }

	public CommanderSocket CommanderSocket {
		get { return _commanderSocket; }
	}

    public KSIMSocket KSIMSocket {
        get { return _ksimSocket; }
    }

    public ADESocket ADESocket {
        get { return _adeSocket; }
    }

    List<SocketConnection> socketConnections = new List<SocketConnection>();
    Dictionary<string, Type> tryAgain = new Dictionary<string, Type>();
    List<string> connected = new List<string>();

	// Make our calls from the Plugin
	[DllImport ("CommunicationsBridge")]
	public static extern IntPtr PythonCall(string scriptsPath, string module, string function, string[] args, int numArgs);

	public event EventHandler PortOpened;

	public void OnPortOpened(object sender, EventArgs e)
	{
		if (PortOpened != null)
		{
			PortOpened(this, e);
		}
	}

    public int connectionRetryTimerTime = 3000;
    Timer connectionRetryTimer;
    bool retryConnections = false;

	void Start()
	{
        connectionRetryTimer = new Timer(connectionRetryTimerTime);
        connectionRetryTimer.Enabled = true;
        connectionRetryTimer.Elapsed += RetryConnections;
        //BackgroundWorker worker = new BackgroundWorker();
        //worker.WorkerSupportsCancellation = true;
        //worker.DoWork += new DoWorkEventHandler(RetryConnections);

		string port = PlayerPrefs.GetString("Listener Port");
		if (port != "")
		{
			OpenPortInternal(port);
		}
		else
		{
			Debug.Log ("No listener port specified. Skipping interface startup.");
		}

        InitParser();

		if (PlayerPrefs.HasKey ("URLs")) {

            // TODO: Refactor generically

            /**********/
            /* FUSION */
            /**********/
            // CSU

			string fusionUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Fusion URL") {
                    fusionUrlString = url.Split ('=') [1];
					break;
				}
			}

            string[] fusionUrl = fusionUrlString.Split(':');
            string fusionAddress = fusionUrl[0];
            if (fusionAddress != "") {
                int fusionPort = Convert.ToInt32(fusionUrl[1]);
                try {
                    _fusionSocket = (FusionSocket)ConnectSocket(fusionAddress, fusionPort, typeof(FusionSocket));
                    socketConnections.Add(_fusionSocket);
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }

                if (!_fusionSocket.IsConnected())
                {
                    if (!tryAgain.ContainsKey(fusionUrlString))
                    {
                        //tryAgain.Add(fusionUrlString, typeof(FusionSocket));
                    }
                }
            }
            else {
                Debug.Log("Fusion socket is not specified.");
            }

            /******************/
            /* EVENT LEARNING */
            /******************/
            // Brandeis

			string eventLearnerUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Event Learner URL") {
					eventLearnerUrlString = url.Split ('=') [1];
					break;
				}
			}

            string[] eventLearnerUrl = eventLearnerUrlString.Split(':');
            string eventLearnerAddress = eventLearnerUrl[0];
            if (eventLearnerAddress != "") {
                int eventLearnerPort = Convert.ToInt32(eventLearnerUrl[1]);
                try {
                    //_eventLearningClient = (EventLearningClient)ConnectSocket (eventLearnerAddress, eventLearnerPort, typeof(EventLearningClient));
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }
            }
            else {
                Debug.Log("Event learner socket is not specified.");
            }

            /**********************/
            /* STRUCTURE LEARNING */
            /**********************/
            // Brandeis

            string structureLearnerUrlString = string.Empty;
            foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
                if (url.Split('=')[0] == "Structure Learner URL") {
                    structureLearnerUrlString = url.Split('=')[1];
                    break;
                }
            }

            string[] structureLearnerUrl = structureLearnerUrlString.Split(':');
            string structureLearnerAddress = structureLearnerUrl[0];
            if (structureLearnerAddress != "") {
                int structureLearnerPort = Convert.ToInt32(structureLearnerUrl[1]);
                try {
                    _structureLearningSocket = (StructureLearningSocket)ConnectSocket (structureLearnerAddress, structureLearnerPort, typeof(StructureLearningSocket));
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }
            }
            else {
                Debug.Log("Structure learner socket is not specified.");
            }

            /*************/
            /* COMMANDER */
            /*************/
            // Oz studies (UF)

			string commanderUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Commander URL") {
					commanderUrlString = url.Split ('=') [1];
					break;
				}
			}

            string[] commanderUrl = commanderUrlString.Split(':');
            string commanderAddress = commanderUrl[0];
            if (commanderAddress != "") {
                int commanderPort = Convert.ToInt32(commanderUrl[1]);
                try {
                    _commanderSocket = (CommanderSocket)ConnectSocket(commanderAddress, commanderPort, typeof(CommanderSocket));
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }
            }
            else {
                Debug.Log("Commander socket is not specified.");
            }

            /********/
            /* KSIM */
            /********/
            // CSU

            string ksimUrlString = string.Empty;
            foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
                if (url.Split('=')[0] == "KSIM URL")
                {
                    ksimUrlString = url.Split('=')[1];
                    break;
                }
            }

            string[] ksimUrl = ksimUrlString.Split(':');
            string ksimAddress = ksimUrl[0];
            if (ksimAddress != "") {
                int ksimPort = Convert.ToInt32(ksimUrl[1]);
                try {
                    _ksimSocket = (KSIMSocket)ConnectSocket(ksimAddress, ksimPort, typeof(KSIMSocket));
                    socketConnections.Add(_ksimSocket);
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }

                if (_ksimSocket != null) {
                    if (!_ksimSocket.IsConnected())
                    {
                        Debug.Log("KSIM socket failed to connect.");

                        if (!tryAgain.ContainsKey(ksimUrlString))
                        {
                            tryAgain.Add(ksimUrlString, typeof(KSIMSocket));
                        }
                    }
                    else
                    {
                        // register VoxSim
                        byte[] bytes = BitConverter.GetBytes(1).Concat(new byte[] { 0x02 }).ToArray<byte>();
                        _ksimSocket.Write(bytes);
                    }
                }
            }
            else {
                Debug.Log("KSIM socket is not specified.");
            }

            /*******/
            /* ADE */
            /*******/
            // Tufts

            string adeUrlString = string.Empty;
            foreach (string url in PlayerPrefs.GetString("URLs").Split(';'))
            {
                if (url.Split('=')[0] == "ADE URL")
                {
                    adeUrlString = url.Split('=')[1];
                    break;
                }
            }

            string[] adeUrl = adeUrlString.Split(':');
            string adeAddress = adeUrl[0];
            if (adeAddress != "") {
                int adePort = Convert.ToInt32(adeUrl[1]);
                try {
                    _adeSocket = (ADESocket)ConnectSocket(adeAddress, adePort, typeof(ADESocket));
                    socketConnections.Add(_adeSocket);
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }

                if (_adeSocket != null) {
                    if (!_adeSocket.IsConnected())
                    {
                        Debug.Log("ADE socket failed to connect.");

                        if (!tryAgain.ContainsKey(adeUrlString))
                        {
                            tryAgain.Add(adeUrlString, typeof(ADESocket));
                        }
                    }
                    else {
                    }
                }
            }
            else {
                Debug.Log("ADE socket is not specified.");
            }
		}
		else {
			Debug.Log ("No input URLs specified.");
		}
	}

	public void InitParser() {
		var parserUrl = PlayerPrefs.GetString ("Parser URL");
		if (parserUrl.Length == 0)
		{
			Debug.Log("Initializing Simple Parser");
			_parser = new SimpleParser();
		}
		else
		{
			Debug.Log("Initializing Stanford Dependency Parser");
			//parser = new StanfordWrapper();
			Debug.Log("Finding Stanford service at " + parserUrl);
			_parser.InitParserService(parserUrl);
		}
	}

	void Update () {
		if (_fusionSocket != null)
		{
            if (_fusionSocket.IsConnected())
			{
                string inputFromFusion = _fusionSocket.GetMessage();
				if (inputFromFusion != "")
				{
					//Debug.Log(inputFromFusion);
					//Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
					_fusionSocket.OnFusionReceived(this, new FusionEventArgs(inputFromFusion));
				}
			}
			else
			{
				_fusionSocket.OnConnectionLost(this, null);
                string fusionAddress = string.Format("{0}:{1}", _fusionSocket.Address, _fusionSocket.Port);
                if (!tryAgain.ContainsKey(fusionAddress))
                {
                    tryAgain.Add(fusionAddress, _fusionSocket.GetType());
                }
			}
		}

		if (_cmdServer != null)
		{
			string inputFromCommander = _cmdServer.GetMessage();
			if (inputFromCommander != "") {
				Debug.Log (inputFromCommander);
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).inputString = inputFromCommander.Trim();
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).MessageReceived(inputFromCommander.Trim());
			}
		}

		if (_commanderSocket != null) {
//			Debug.Log (_commanderClient.IsConnected ());
			string inputFromCommander = _commanderSocket.GetMessage();
			if (inputFromCommander != "") {
				Debug.Log (inputFromCommander);
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).inputString = inputFromCommander.Trim();
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).MessageReceived(inputFromCommander.Trim());
			}
		}

        if (_ksimSocket != null)
        {
            if (_ksimSocket.IsConnected())
            {
            }
            else
            {
                _ksimSocket.OnConnectionLost(this, null);
                string ksimAddress = string.Format("{0}:{1}", _ksimSocket.Address, _ksimSocket.Port);
                if (!tryAgain.ContainsKey(ksimAddress))
                {
                    tryAgain.Add(ksimAddress, _ksimSocket.GetType());
                }
            }
        }

        if (_adeSocket != null)
        {
            if (_adeSocket.IsConnected())
            {
            }
            else
            {
                _adeSocket.OnConnectionLost(this, null);
                string adeAddress = string.Format("{0}:{1}", _adeSocket.Address, _adeSocket.Port);
                if (!tryAgain.ContainsKey(adeAddress))
                {
                    tryAgain.Add(adeAddress, _adeSocket.GetType());
                }
            }
        }

        if ((retryConnections) && (tryAgain.Keys.Count > 0))
        {
            foreach (string connectionLabel in tryAgain.Keys)
            {
                if (tryAgain[connectionLabel] != null)
                {
                    SocketConnection socket = socketConnections.FirstOrDefault(s => s.GetType() == tryAgain[connectionLabel]);
                    if (socket != null)
                    {
                        if (!socket.IsConnected())
                        {
                            Debug.Log(string.Format("Retrying connection {0}@{1}",tryAgain[connectionLabel],connectionLabel));
                            // try again
                            try
                            {
                                string[] url = connectionLabel.Split(':');
                                string address = url[0];
                                if (address != "")
                                {
                                    int port = Convert.ToInt32(url[1]);
                                    try
                                    {
                                        Type socketType = tryAgain[connectionLabel];
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
                                connected.Add(connectionLabel);
                            }
                            else
                            {
                                Debug.Log(string.Format("Connection to {0} is lost!",socket.GetType()));
                            }

                            if (tryAgain[connectionLabel] == typeof(FusionSocket))
                            {
                                _fusionSocket = (FusionSocket)socket;
                                //Debug.Log(_fusionSocket.IsConnected());
                            }
                            else if (tryAgain[connectionLabel] == typeof(KSIMSocket))
                            {
                                _ksimSocket = (KSIMSocket)socket;

                                if (_ksimSocket.IsConnected())
                                {
                                    // register VoxSim
                                    byte[] bytes = BitConverter.GetBytes(1).Concat(new byte[] { 0x02 }).ToArray<byte>();
                                    _ksimSocket.Write(bytes);
                                }
                            }
                            else if (tryAgain[connectionLabel] == typeof(ADESocket))
                            {
                                _adeSocket = (ADESocket)socket;
                                //Debug.Log(_fusionSocket.IsConnected());
                            }
                        }
                    }
                }
            }

            foreach (string label in connected)
            {
                if (tryAgain.ContainsKey(label))
                {
                    tryAgain.Remove(label);
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
		Debug.Log(string.Format("Trying connection to {0}:{1}",address,port)); 

        SocketConnection socket = null;

        if (socketType == typeof(FusionSocket)) {
            socket = new FusionSocket();
        }
        if (socketType == typeof(CommanderSocket)) {
			socket = new CommanderSocket ();
		}
		else if (socketType == typeof(EventLearningSocket)) {
			socket = new EventLearningSocket ();
		}
        else if (socketType == typeof(KSIMSocket)) {
            socket = new KSIMSocket();
        }
        else if (socketType == typeof(ADESocket)) {
            socket = new ADESocket();
        }

        if (socket != null)
        {
            try
            {
                socket.Connect(address, port);
                Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port, socket.IsConnected(), socketType.ToString()));
                socket.OnConnectionMade(this, new SocketEventArgs(socketType));
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
		else {
			Debug.Log ("Failed to create client");
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
                Debug.Log(string.Format("{2} :: Connected to client @ {0}:{1} as {3}", address, port, socket.IsConnected(), socketType.ToString()));
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
    }

	public void OpenPortInternal(string port) {
		try
		{
			// pass true as first param to make the server visible only to 'localhost'
			// (for testing, for exmaple)
			_cmdServer = new CmdServer(false, int.Parse(port), 1);
			OnPortOpened (this, null);
		}
		catch (Exception e) {
			Debug.Log ("Failed to open port " + port);
			Debug.Log(e.Message);
			Debug.Log(e.InnerException);
			Debug.Log(e.StackTrace);
			Debug.Log(e.Data);
		}
	}

	public string NLParse(string input) {
//		string[] args = new string[]{input};
//		string result = Marshal.PtrToStringAuto(PythonCall (Application.dataPath + "/Externals/python/", "change_to_forms", "parse_sent", args, args.Length));
		var result = _parser.NLParse(input);
		Debug.Log ("Parsed as: " + result);

		return result;
	}

	void OnDestroy () {
		if (_cmdServer != null)
		{
			_cmdServer.Close();
			_cmdServer = null;
		}

		if (_fusionSocket != null && _fusionSocket.IsConnected())
		{
			_fusionSocket.Close();
            _fusionSocket = null;
		}

		if (_commanderSocket != null && _commanderSocket.IsConnected())
		{
			_commanderSocket.Close();
			_commanderSocket = null;
		}

        if (_ksimSocket != null && _ksimSocket.IsConnected())
        {
            _ksimSocket.Close();
            _ksimSocket = null;
        }

        if (_adeSocket != null && _adeSocket.IsConnected())
        {
            _adeSocket.Close();
            _adeSocket = null;
        }
	}

	void OnApplicationQuit () {
		OnDestroy();
	}
}
