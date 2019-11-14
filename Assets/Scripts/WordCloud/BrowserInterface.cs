using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZenFulcrum.EmbeddedBrowser;


using VoxSimPlatform;

using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace VoxSimPlatform {
    namespace Network {
        public class GetSocket {
            private static Socket ConnectSocket(string server, int port) {
                Socket s = null;
                IPHostEntry hostEntry = null;

                // Get host related information.
                hostEntry = Dns.GetHostEntry(server);

                // Loop through the AddressList to obtain the supported AddressFamily. This is to avoid
                // an exception that occurs when the host IP Address is not compatible with the address family
                // (typical in the IPv6 case).
                foreach (IPAddress address in hostEntry.AddressList) {
                    IPEndPoint ipe = new IPEndPoint(address, port);
                    Socket tempSocket =
                        new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);

                    if (tempSocket.Connected) {
                        s = tempSocket;
                        break;
                    }
                    else {
                        continue;
                    }
                }
                return s;
            }

            // This method requests the home page content for the specified server.
            private static Socket SocketSendReceive(string server, int port, Socket s = null, string request = null, bool receive = false) {
                if (s == null) {
                    s = ConnectSocket(server, port);
                }

                if(request != null) {
                    byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                    // Create a socket connection with the specified server and port
                    // Send request to the server.
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    Debug.LogWarning(request + "   " + s.Handle + "    " + s.IsBound + "  result: " + result + "  connected: " + s.RemoteEndPoint);

                }
                else {
                    // Initialize the connection
                    string id = Random.Range(0, 999999).ToString(); // This allows us to re-register on a new port without, like, doing it manually.
                    string[] requests = { "(register :name DIANA" + id +")", // Particular number needs to change while facilitator is open
                    "(subscribe :content (request &key :content (record-gene-data . * )))",
                    "(subscribe :content (request &key :content (cluster-analysis . * )))",
                    "(tell :content (module-status ready))",
                    "(subscribe :content (tell &key :content (utterance . *)))"};

                    foreach (string requestLoop in requests) {
                        request = requestLoop;
                        byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                        byte[] bytesReceived = new byte[256];

                        // Create a socket connection with the specified server and port.

                        // Send request to the server.
                        s.Send(bytesSent, bytesSent.Length, 0);
                    }
                }
                return s;
            }

            public static Socket Main(Socket s = null, string request = null, bool receive = false) {
                string host;
                int port;

                //if (args.Length == 0)
                //    // If no server name is passed as argument to this program, 
                //    // use the current host name as the default.
                //    host = Dns.GetHostName();
                //else
                //    host = args[0];
                //Debug.LogWarning(host + "    " + port);
                host = "localhost";
                port = 6200;
                return SocketSendReceive(host, port, s, request, receive); // where s gets passed
                //Debug.LogWarning(result);
            }
        }

        public class BrowserInterface : MonoBehaviour {
            float prior0;
            float prior1;
            //Vector2 topleft; // of the cgm window
            //Vector2 bottomright; // of the cgm window
            Vector3 to_enter = new Vector3(0,0,0); // Only a Vector3 because that's how mouse position is recorded. Only first 2 used
            string saved_nodes = "";
            Browser b;
            Socket s;
            VoxSimPlatform.Agent.AgentTextController atc;
            ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]



            //GetSocket gs = new GetSocket();

            // Start is called before the first frame update
            void Start() {
                // immediately remember the browser forever.
                b = FindObjectOfType<Browser>(); // Only one browser allowed right now. Just a couple lines to specify further
                GameObject diana = GameObject.Find("Diana");
                atc = diana.GetComponent<Agent.AgentTextController>();
            }

            // Set the location you are otherwise clicking on. IDK how you want to do it
            void setEntry(Vector3 to_set) {
                to_enter = to_set;
            }

            string GetSavedNodes() {
                return saved_nodes;
            }

            public void ZoomIn(Vector3 x, Vector2 size) {
                // Send a note to the browser to crop around the current mouse position.
                //Debug.LogWarning(b);
                Debug.LogWarning("SIZE IS: " + size);
                if (x[0] > 0 && x[1] > 0) {
                    string point = x.ToString().Replace("(", "[").Replace(")", "]");
                    //Debug.LogWarning("POINT" + point);

                    //b.EvalJS("console.log(this)");
                    // We just need a nice centerpoint. How to get?
                    //b.EvalJS("this.cgm.brush_crop_matrix([[0,0],[10,10]])").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                    b.EvalJS("this.cgm.brush_crop_matrix(" + point + "," + size[0] + "," + size[1] + ")").Then(ret => {
                        Debug.LogWarning("Brush cropper turned on" + ret);
                        b.EvalJS("this.saved_selected_nodes").Then(ret1 => {
                            saved_nodes = ret1;
                            // Problem here: the call to brush_crop_matrix only changes the state of the
                            // Javascript to begin listening for an input. It does not wait for the
                            // input to be received or responded to. So the Then statement does not
                            // prevent reading saved_nodes before it has been changed.
                            Debug.LogWarning("saved" + saved_nodes);
                            }).Done();
                    }).Done(); //
                    
                }
                else {
                    Debug.LogWarning("You probably clicked outside the box.");
                }
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetMouseButton(1)) { // Right click to avoid double input//Input.GetMouseButtonDown(0). Feel free to change to keypresses or whatever.
                    Vector3 x;
                    if (to_enter != new Vector3(0, 0, 0)) {
                        x = to_enter; // Set to_enter elsewhere to make t
                    }
                    else {
                        x = Input.mousePosition; // if point isn't set, steal it from the mouse
                    }
                    x = RemapToWindow(x);
                    ZoomIn(x, new Vector2(50,50));
                }
                else if (Input.GetKeyDown("s")) { // s for server, initializes the socket connection
                    s = GetSocket.Main(s);
                    ExternalCall("s");
                }
                else if (Input.GetKeyDown("d")) { // Downloads whatever, prints to debug.
                    ExternalCall("d");

                }
                else if (Input.GetKeyDown("p")) { // p for path
                    ExternalCall("p");

                }
                else if (Input.GetKeyDown("g")) { // g for genes
                    ExternalCall("g");
                }
                else if (Input.GetKeyDown("2")) {
                    ExternalCall("2");
                }
                else if (Input.GetKeyDown("3")) {
                    ExternalCall("3");
                }
                else if (Input.GetKeyDown("n")) {
                    GrabSelected("");
                }else if (Input.GetKeyDown("i")) {
                    b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                }
            }


            void ExternalCall(string j) {
                // Turn the update things into a function that takes a string for choices.
                if (j == "1") { // Right click to avoid double input//Input.GetMouseButtonDown(0). Feel free to change to keypresses or whatever.
                    // Send a note to the browser to crop around the current mouse position.
                    Debug.LogWarning(b);
                    Vector3 x;
                    if (to_enter != new Vector3(0, 0, 0)) {
                        x = to_enter; // Set to_enter elsewhere to make t
                    }
                    else {
                        x = Input.mousePosition; // if point isn't set, steal it from the mouse
                    }
                    Debug.LogWarning("ORIGINAL: " + x.ToString());
                    x = RemapToWindow(x);
                    if (x[0] > 0 && x[1] > 0) {
                        string point = x.ToString().Replace("(", "[").Replace(")", "]");
                        Debug.LogWarning("POINT" + point);
                        // We just need a nice centerpoint. How to get?
                        b.EvalJS("this.cgm.brush_crop_matrix(" + point + ")").Then(ret => {
                            Debug.LogWarning("Brush cropper turned on" + ret);
                            b.EvalJS("this.saved_selected_nodes").Then(ret1 => {
                                saved_nodes = ret1;
                                Debug.LogWarning("Saved Nodes:" + saved_nodes);
                                }).Done();
                        }).Done();

                    }
                    else {
                        Debug.LogWarning("You probably clicked outside the box.");
                    }
                    return;
                }
                if (s == null) { // s for server, initializes the socket connection
                    // Calls when you press s too, right now.
                    s = GetSocket.Main(s);
                }
                if (j == "d") { // Downloads whatever, prints to debug.
                    //string[] the_list = { };
                    //s = GetSocket.Main(s, "", true);
                    byte[] bytesReceived = new byte[256];
                    int i = s.Receive(bytesReceived);
                    string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(from_trips);
                }
                else if (j == "p") { // p for path
                                                  // Request a path to a cluster from the server. (or rather, just grab whatever it hands you)
                                                  // Then cluster, then give the server a success notification.
                                                  //s = GetSocket.Main(s, "", true);
                    byte[] bytesReceived = new byte[256];
                    int i = s.Receive(bytesReceived);
                    string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(from_trips);
                    // Create a socket connection with the specified server and port.

                    // Parse the string here.
                    // (request :content (record-gene-data :filename "path on localhost"  :data " content of csv file")
                    // string to_eval = Regex.Match(from_trips, "test.*test").Value;
                    string to_eval = Regex.Match(from_trips, @""".*json").Value;
                    to_eval = to_eval.Substring(1, to_eval.Length - 1); //UGH

                    Debug.LogWarning("eval: " + to_eval);
                    b.EvalJS("make_clust('" + to_eval + "');").Then(ret => saved_nodes = ret).Done();
                    // Send request to the server.
                    byte[] bytesSent = Encoding.ASCII.GetBytes("(reply :content (success :status done)");
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                }
                else if (j == "g") { // g for genes
                    //s = GetSocket.Main(s, "", true);
                    string to_send = "(tell: content (selected-genes :gene-list (" + saved_nodes + ")))";
                    Debug.LogWarning(to_send);
                    byte[] bytesSent = Encoding.ASCII.GetBytes(to_send);
                    // Create a socket connection with the specified server and port.

                    // Send request to the server.
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    byte[] bytesReceived = new byte[256];
                    Debug.LogWarning(result);
                }
                else if (j == "2") { // pull a list of genes FROM FACILITATOR
                    byte[] bytesReceived = new byte[2048];
                    int i = s.Receive(bytesReceived);
                    string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(from_trips);
                    // Parse the string here.
                    // (request :content (record-gene-data :filename "path on localhost"  :data " content of csv file")
                    string to_eval = Regex.Match(from_trips, @""".*""").Value;
                    to_eval = to_eval.Substring(1, to_eval.Length - 2); //UGH


                    Debug.LogWarning("GENES FROM BOB: " + to_eval);
                    //b.EvalJS("make_clust('" + to_eval + "');").Then(ret => saved_nodes = ret).Done();
                    // Send request to the server
                    atc.outputString = to_eval;
                    atc.textField = true;
                    byte[] bytesSent = Encoding.ASCII.GetBytes("(reply :content (success :status done)");
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    

                }
            }


            ///
            public Vector3 RemapToWindow(Vector3 x) {
                /// remap input pixel to its relative position in the actual matrix.
                /// Hard coded for me rn
                /// (0,0) is the top left
                // 271, 333 are the top left in Unity
                // 673, 56 are bottom right.
                // ^ Grab by right clicking on each corner and reading unity console.

                // Width is 408
                // Height is 278
                // ^ Grab these by going to javascript console (on Browser (GUI))
                //      and typing in > cgm.params.viz.clust.dim.width
                //> cgm.params.viz.clust.dim.height
                // Switcheroo
                x[0] = ((x[0] - 271) / (675 - 271)) * 408f; // 700 since was empirically too far right.
                x[1] = ((333 - x[1]) / (333 - 56)) * 278f;
                return x;
                
            }

            // Acquire the list of selected genes
            public void GrabSelected(string payload = "") {
                b.EvalJS("this.saved_selected_nodes").Then(ret => {
                    saved_nodes = ret;
                    Debug.LogWarning("WE GOT THEM2: " + saved_nodes);

                }).Done();
            }

            [MenuItem("Jarvis/Arbitrary &#A")]
            static void Arb() {
                BrowserInterface bi = Selection.activeGameObject.GetComponent<BrowserInterface>();
                bi.GrabSelected("");
                var promise = new Promise<string>();
            }

            // Makes sure that we have this object selected yo
            [MenuItem("Jarvis/Arbitrary &#A", true)]
            static bool ValidateArb() {
                return (Selection.activeGameObject != null) &&
               (Selection.activeGameObject.GetComponent<BrowserInterface>() != null) &&
               (Selection.activeGameObject.GetComponent<Browser>() != null);
            }
        }
    }
}