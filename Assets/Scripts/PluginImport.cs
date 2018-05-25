using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using Network;
using NLU;

public class PluginImport : MonoBehaviour {

	private INLParser _parser;
	private CmdServer _cmdServer;
	private CSUClient _csuClient;
	private EventLearningClient _eventLearningClient;
	private CommanderClient _commanderClient;

	public CSUClient CSUClient {
		get { return _csuClient; }
	}

	public EventLearningClient EventLearningClient {
		get { return _eventLearningClient; }
	}

	public CommanderClient CommanderClient {
		get { return _commanderClient; }
	}

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

	void Start()
	{
		string port = PlayerPrefs.GetString("Listener Port");
		if (port != "")
		{
			OpenPortInternal(port);
		}
		else
		{
			Debug.Log ("No listener port specified. Skipping interface startup.");
		}

		if (PlayerPrefs.HasKey ("URLs")) {
			string csuUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "CSU URL") {
					csuUrlString = url.Split ('=') [1];
					break;
				}
			}

			string eventLearnerUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Event Learner URL") {
					eventLearnerUrlString = url.Split ('=') [1];
					break;
				}
			}

			string commanderUrlString = string.Empty;
			foreach (string url in PlayerPrefs.GetString("URLs").Split(';')) {
				if (url.Split ('=') [0] == "Commander URL") {
					commanderUrlString = url.Split ('=') [1];
					break;
				}
			}

			string[] csuUrl = csuUrlString.Split(':');
			string csuAddress = csuUrl [0];
			if (csuAddress != "") {
				int csuPort = Convert.ToInt32 (csuUrl [1]);
				try {
					ConnectCSU (csuAddress, csuPort);
				}
				catch (Exception e) {
					Debug.Log (e.Message);
				}
			}
			else {
				Debug.Log ("CSU gesture input is not specified.");
			}

			string[] eventLearnerUrl = eventLearnerUrlString.Split(':');
			string eventLearnerAddress = eventLearnerUrl [0];
			if (eventLearnerAddress != "") {
				int eventLearnerPort = Convert.ToInt32 (eventLearnerUrl [1]);
				try {
					//_eventLearningClient = (EventLearningClient)ConnectSocket (eventLearnerAddress, eventLearnerPort, typeof(EventLearningClient));
				}
				catch (Exception e) {
					Debug.Log (e.Message);
				}
			}
			else {
				Debug.Log ("Event learner input is not specified.");
			}

			string[] commanderUrl = commanderUrlString.Split(':');
			string commanderAddress = commanderUrl [0];
			if (commanderAddress != "") {
				int commanderPort = Convert.ToInt32 (commanderUrl [1]);
				try {
					_commanderClient = (CommanderClient)ConnectSocket (commanderAddress, commanderPort, typeof(CommanderClient));
				}
				catch (Exception e) {
					Debug.Log (e.Message);
				}
			}
			else {
				Debug.Log ("Commander input is not specified.");
			}
		}
		else {
			Debug.Log ("No input URLs specified.");
		}

		InitParser();

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
		if (_csuClient != null)
		{
			if (_csuClient.IsConnected())
			{
				string inputFromCSU = _csuClient.GetMessage();
				if (inputFromCSU != "")
				{
					Debug.Log(inputFromCSU);
					Debug.Log(_csuClient.HowManyLeft() + " messages left.");
					_csuClient.OnGestureReceived(this, new GestureEventArgs(inputFromCSU));
				}
			}
			else
			{
				Debug.LogError("Connection to CSU server is lost!");
				_csuClient.OnConnectionLost(this, null);
				_csuClient = null;
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

		if (_commanderClient != null) {
//			Debug.Log (_commanderClient.IsConnected ());
			string inputFromCommander = _commanderClient.GetMessage();
			if (inputFromCommander != "") {
				Debug.Log (inputFromCommander);
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).inputString = inputFromCommander.Trim();
				((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).MessageReceived(inputFromCommander.Trim());
			}
		}
	}

	public void ConnectCSU(string address, int port)
	{
		Debug.Log(string.Format("Trying connection to {0}:{1}",address,port)); 
		_csuClient = new CSUClient();
		_csuClient.Connect(address, port);
		Debug.Log(string.Format("{2} :: Connected to CSU recognizer @ {0}:{1}", address, port, _csuClient.IsConnected()));
	}

	public SocketClient ConnectSocket(string address, int port, Type clientType)
	{ // TODO: Abstract EventLearningClient and CSUClient to generic type inheritance
		Debug.Log(string.Format("Trying connection to {0}:{1}",address,port)); 

		SocketClient client = null;

		if (clientType == typeof(CommanderClient)) {
			client = new CommanderClient ();
		}
		else if (clientType == typeof(EventLearningClient)) {
			client = new EventLearningClient ();
		}

		if (client != null) {
			client.Connect (address, port);
			Debug.Log (string.Format ("{2} :: Connected to client @ {0}:{1} as {3}", address, port, client.IsConnected (), clientType.ToString()));
		}
		else {
			Debug.Log ("Failed to create client");
		}
			
		return client;
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

		if (_csuClient != null && _csuClient.IsConnected())
		{
			_csuClient.Close();
			_csuClient = null;
		}

		if (_commanderClient != null && _commanderClient.IsConnected())
		{
			_commanderClient.Close();
			_commanderClient = null;
		}
	}

	void OnApplicationQuit () {
		OnDestroy();
	}
}
