using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using Network;
using NLU;

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
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }
            }
            else {
                Debug.Log("Fusion socket is not specified.");
            }

            /******************/
            /* EVENT LEARNING */
            /******************/

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
            // Oz studies

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
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }

                if (_ksimSocket != null) {
                    byte[] bytes = BitConverter.GetBytes(1).Concat(new byte[] { 0x02 }).ToArray<byte>();
                    _ksimSocket.Write(bytes);
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
                }
                catch (Exception e) {
                    Debug.Log(e.Message);
                }

                if (_adeSocket != null) {
                    byte[] bytes = BitConverter.GetBytes(1).Concat(new byte[] { 0x02 }).ToArray<byte>();
                    _adeSocket.Write(bytes);
                }
            }
            else {
                Debug.Log("ADE socket is not specified.");
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
		if (_fusionSocket != null)
		{
			if (_fusionSocket.IsConnected())
			{
                string inputFromFusion = _fusionSocket.GetMessage();
				if (inputFromFusion != "")
				{
					Debug.Log(inputFromFusion);
					Debug.Log(_fusionSocket.HowManyLeft() + " messages left.");
					_fusionSocket.OnGestureReceived(this, new FusionEventArgs(inputFromFusion));
				}
			}
			else
			{
				Debug.LogError("Connection to Fusion server is lost!");
				_fusionSocket.OnConnectionLost(this, null);
				_fusionSocket = null;
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
	}

	public SocketConnection ConnectSocket(string address, int port, Type clientType)
	{
		Debug.Log(string.Format("Trying connection to {0}:{1}",address,port)); 

		SocketConnection client = null;

        if (clientType == typeof(FusionSocket)) {
            client = new FusionSocket();
        }
        if (clientType == typeof(CommanderSocket)) {
			client = new CommanderSocket ();
		}
		else if (clientType == typeof(EventLearningSocket)) {
			client = new EventLearningSocket ();
		}
        else if (clientType == typeof(KSIMSocket)) {
            client = new KSIMSocket();
        }
        else if (clientType == typeof(ADESocket)) {
            client = new ADESocket();
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
	}

	void OnApplicationQuit () {
		OnDestroy();
	}
}
