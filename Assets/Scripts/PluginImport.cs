using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Network;
using NLU;

public class PluginImport : MonoBehaviour {
	// port definitions
	public string port = "";

	private INLParser _parser;
	private CmdServer _cmdServer;

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
		port = PlayerPrefs.GetString("Listener Port");
		OpenPortInternal(port);
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
		if (port == "" || _cmdServer == null) {
			return;
		}

		// ask Nihkil
		string input = _cmdServer.GetMessage();
		if (input != "") {
			Debug.Log (input);
			((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).inputString = input.Trim();
			((InputController)(GameObject.Find ("IOController").GetComponent ("InputController"))).MessageReceived(input.Trim());
		}
	}

	public void OpenPortInternal(string port) {
		if (port != "") {
			try
			{
				// pass true as first param to make the server visible only to 'localhost'
				// (for testing, for exmaple)
                _cmdServer = new CmdServer(true, int.Parse(port), 1);
                OnPortOpened (this, null);
			}
			catch (Exception e) {
				Debug.Log ("Failed to open port " + port);
				Debug.Log(e.StackTrace);
			}
		}
		else {
			Debug.Log ("No listener port specified. Skipping interface startup.");
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
		if (port == "") {
			return;
		}

		Debug.Log ("Closing port " + port);
		_cmdServer.Close();
	}

	void OnApplicationQuit () {
		if (port == "") {
			return;
		}

		Debug.Log ("Closing port " + port);

		_cmdServer.Close();
	}
}
