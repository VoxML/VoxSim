using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZenFulcrum.EmbeddedBrowser;

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
                //string request = "GET / HTTP/1.1\r\nHost: " + server +
                //    "\r\nConnection: Close\r\n\r\n";
                if (s == null) {
                    s = ConnectSocket(server, port);
                    //s.ReceiveTimeout = 1;

                }

                if(request != null) {
                    byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                    // Create a socket connection with the specified server and port.

                    // Send request to the server.
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    Debug.LogWarning(request + "   " + s.Handle + "    " + s.IsBound + "  result: " + result + "  connected: " + s.RemoteEndPoint);


                }
                else {
                    // Initialize the connection
                    string[] requests = { "(register :name DIANA" + "67"+")", // Particular number needs to change while facilitator is open
                    "(subscribe :content (request &key :content (record-gene-data . * )))",
                    "(subscribe :content (request &key :content (cluster-analysis . * )))",
                    "(tell :content (module-status ready))",
                    "(subscribe :content (tell &key :content (utterance . *)))"};

                    foreach (string requestLoop in requests) {
                        request = requestLoop;
                        byte[] bytesSent = Encoding.ASCII.GetBytes(request);
                        byte[] bytesReceived = new byte[256];
                        //string page = "";

                        // Create a socket connection with the specified server and port.

                        // Send request to the server.
                        s.Send(bytesSent, bytesSent.Length, 0);
                        //int i = s.Receive(bytesReceived);
                        
                        //Debug.LogWarning(bytesReceived);
                    }
                }


                //return page;
                return s;
                //string request = "(register :name DIANA)";

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
            ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]

            //GetSocket gs = new GetSocket();

            // Start is called before the first frame update
            void Start() {
                // immediately remember the browser forever.
                b = FindObjectOfType<Browser>(); // Only one browser allowed right now. Just a couple lines to specify further
            }

            // Set the location you are otherwise clicking on. IDK how you want to do it
            void setEntry(Vector3 to_set) {
                to_enter = to_set;
            }

            string get_saved_nodes() {
                return saved_nodes;
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetMouseButton(1)) { // Right click to avoid double input//Input.GetMouseButtonDown(0). Feel free to change to keypresses or whatever.
                    // Send a note to the browser to crop around the current mouse position.
                    Debug.LogWarning(b);
                    Vector3 x;
                    if (to_enter != new Vector3(0,0,0)){
                        x = to_enter; // Set to_enter elsewhere to make t
                    }
                    else {
                        x = Input.mousePosition; // if point isn't set, steal it from the mouse
                    }
                    Debug.LogWarning("ORIGINAL: " + x.ToString());
                    //x[0] = 30;
                    //x[1] = 30;
                    x = remap_to_window(x);
                    if (x[0] > 0 && x[1] > 0){
                        string point = x.ToString().Replace("(", "[").Replace(")", "]");
                        Debug.LogWarning("POINT" + point);

                        //b.EvalJS("console.log(this)");
                        // We just need a nice centerpoint. How to get?
                        //b.EvalJS("this.cgm.brush_crop_matrix([[0,0],[10,10]])").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                        b.EvalJS("this.cgm.brush_crop_matrix(" + point + ")").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                        b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                    }
                    else {
                        Debug.LogWarning("You probably clicked outside the box.");
                    }
                }
                else if (Input.GetKeyDown("s")) { // s for server, initializes the socket connection
                    //string[] the_list = { };
                    s = GetSocket.Main(s);//, "(register :name DIANA14)");
                    external_call("s");
                }
                else if (Input.GetKeyDown("d")) { // Downloads whatever, prints to debug.
                    external_call("d");

                }
                else if (Input.GetKeyDown("p")) { // p for path
                    external_call("p");

                }
                else if (Input.GetKeyDown("g")) { // g for genes
                    external_call("g");
                }
                else if (Input.GetKeyDown("n")) { // g for genes
                    grab_selected("");
                }else if (Input.GetKeyDown("i")) {
                    b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                }
            }


            void external_call(string j) {
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
                    //x[0] = 30;
                    //x[1] = 30;
                    x = remap_to_window(x);
                    if (x[0] > 0 && x[1] > 0) {
                        string point = x.ToString().Replace("(", "[").Replace(")", "]");
                        Debug.LogWarning("POINT" + point);

                        //b.EvalJS("console.log(this)");
                        // We just need a nice centerpoint. How to get?
                        //b.EvalJS("this.cgm.brush_crop_matrix([[0,0],[10,10]])").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                        b.EvalJS("this.cgm.brush_crop_matrix(" + point + ")").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                        b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();

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
                    string to_send = "(tell: content(selected - genes :gene - list(" + saved_nodes + ")))";
                    Debug.LogWarning(to_send);
                    byte[] bytesSent = Encoding.ASCII.GetBytes(to_send);
                    // Create a socket connection with the specified server and port.

                    // Send request to the server.
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                    byte[] bytesReceived = new byte[256];
                    //int i = s.Receive(bytesReceived);
                    //string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(result);
                }
            }


            ///
            private Vector3 remap_to_window(Vector3 x) {
                /// remap input pixel to its relative position in the actual matrix.
                /// Hard coded for me rn
                /// (0,0) is the top left
                // 263, 276 are the top left in Unity
                // 689, 55 are bottom right.
                // ^ Grab by right clicking on each corner and reading unity console.

                // Width is 425.5
                // Height is 215
                // ^ Grab these by going to javascript console (on Browser (GUI))
                //      and typing in > cgm.params.viz.clust.dim.width
                //> cgm.params.viz.clust.dim.height
                // Switcheroo
                x[0] = ((x[0] - 263) / (700 - 263)) * 425.5f; // 700 since was empirically too far right.
                x[1] = ((276 - x[1]) / (276 - 55)) * 215f;
                return x;
                
            }

            public string grab_selected(string payload = "") {
                
                //b.EvalJS("this.saved_selected_nodes").Then(ret => Debug.LogWarning("Returned Cells: " + ret)).Done();
                //b.EvalJS("document.title").Then(ret => Debug.LogWarning("Document title: " + ret)).Done();
                b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                Debug.LogWarning("WE GOT THEM: " + saved_nodes); // race condition. 
                return "test";
            }

            [MenuItem("Jarvis/Arbitrary &#A")]
            static void Arb() {
                BrowserInterface bi = Selection.activeGameObject.GetComponent<BrowserInterface>();
                Debug.LogWarning("Result: " + bi.grab_selected());
            }

            // Makes sure that we have this object selected yo
            //[MenuItem("VoxSim/New WordCloud &#w", true)]
            //static bool ValidateNewWordCloud() {

            //}

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