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


// This file is a bit too big and unwieldy. See about refactoring.
namespace VoxSimPlatform {
    namespace Network {
        
        public class BrowserInterface : CommunicationsBridge {
            float prior0;
            float prior1;
            //Vector2 topleft; // of the cgm window
            //Vector2 bottomright; // of the cgm window
            Vector3 to_enter = new Vector3(0,0,0); // Only a Vector3 because that's how mouse position is recorded. Only first 2 used
            public string saved_nodes = "";
            string to_display = "";
            ClusterGram b;
            //Browser b;
            Socket s;
            VoxSimPlatform.Agent.AgentTextController atc;
            IPromise<JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]
            ClicIOClient io;


            //GetSocket gs = new GetSocket();
            public Socket GetSock() {
                return s;
            }

            // Start is called before the first frame update
            void Start() {
                // immediately remember the browser forever.
                //b = FindObjectOfType<Browser>(); // Only one browser allowed right now. Just a couple lines to specify further
                b = FindObjectOfType<ClusterGram>();
                GameObject diana = GameObject.Find("Diana");
                atc = diana.GetComponent<Agent.AgentTextController>();
            }

            public string GetDisplay() {
                Debug.LogWarning(to_display);
                return to_display;
            }

            // Set the location you are otherwise clicking on. IDK how you want to do it
            void SetEntry(Vector3 to_set) {
                to_enter = to_set;
            }

            string GetSavedNodes() {
                return saved_nodes;
            }

            public void ZoomIn(Vector3 x, Vector2 size) {
                // Send a note to the browser to crop around the current mouse position.
                //Debug.LogWarning(b);
                Debug.LogWarning("SIZE IS: " + size);

                // New, internal CG solution:
                b.Crop(x[0], x[1], size[0], size[1]);

                // Old solution with Browser running clustergram
                //if (x[0] > 0 && x[1] > 0) {
                //    string point = x.ToString().Replace("(", "[").Replace(")", "]");
                //    //Debug.LogWarning("POINT" + point);


                //    b.EvalJS("this.cgm.brush_crop_matrix(" + point + "," + size[0] + "," + size[1] + ")").Then(ret => {
                //        Debug.LogWarning("Brush cropper turned on" + ret);
                //        b.EvalJS("this.saved_selected_nodes").Then(ret1 => {
                //            saved_nodes = ret1;
                //            // Problem here: the call to brush_crop_matrix only changes the state of the
                //            // Javascript to begin listening for an input. It does not wait for the
                //            // input to be received or responded to. So the Then statement does not
                //            // prevent reading saved_nodes before it has been changed.
                //            Debug.LogWarning("saved" + saved_nodes);
                //        }).Done();
                //    }).Done(); //
                //}
                //else {
                //    Debug.LogWarning("You probably clicked outside the box.");
                //}
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetMouseButton(1)) { // Right click to avoid double input//Input.GetMouseButtonDown(0). Feel free to change to keypresses or whatever.
                    //Vector3 x;
                    //if (to_enter != new Vector3(0, 0, 0)) {
                    //    x = to_enter; // Set to_enter elsewhere to make t
                    //}
                    //else {
                    //    x = Input.mousePosition; // if point isn't set, steal it from the mouse
                    //    Debug.LogWarning("Mouse pos is: " + x);
                    //}
                    //x = RemapToWindow(x);
                    //ZoomIn(x, new Vector2(50, 50));
                }
                else if (Input.GetKeyDown("s")) { // s for server, initializes the socket connection
                    s = GetSocket.Main(s);
                    ExternalCall("s");
                }
                else if (Input.GetKeyDown("d")) { // Downloads whatever, prints to debug.
                    ExternalCall("d");
                }
                //else if (Input.GetKeyDown("p")) { // p for path
                //    ExternalCall("p");

                //}
                //else if (Input.GetKeyDown("g")) { // g for genes
                //    ExternalCall("g");
                //}
                else if (Input.GetKeyDown("2")) {
                    ExternalCall("2");
                }
                else if (Input.GetKeyDown("3")) {
                    ExternalCall("3");
                }
                else if (Input.GetKeyDown("n")) {
                    GrabSelected("");
                }
                else if (Input.GetKeyDown("i")) {
                    saved_nodes = b.get_selected_names();
                    // Old way
                    //b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                }
                else if (Input.GetKeyDown("5")) {
                    PostRequest("Hello");
                }
                else if (Input.GetKeyDown("6")) {
                    //geneSetMembers: (41) ["UST", "SAMD5", "PLAGL1", "SLC35D3", "SAMD3", "TMEM200A", "TRDN"]
                    //geneSetName: "selection0"
                    var to_say = "Create+the+gene+set.";
                    var payload = "{\"geneSetMembers\":[" + saved_nodes + "], \"geneSetName\": \"selection0\"}";
                    //var payload = "{\"geneSetMembers\":[\"UST\", \"SAMD5\", \"PLAGL1\", \"SLC35D3\", \"SAMD3\", \"TMEM200A\", \"TRDN\"], \"geneSetName\": \"selection0\"}";
                    PostRequest(to_say + "~" + payload);
                }
                else if (Input.GetKeyDown("7")) {
                    //geneSetMembers: (41) ["UST", "SAMD5", "PLAGL1", "SLC35D3", "SAMD3", "TMEM200A", "TRDN"]
                    //geneSetName: "selection0"
                    var to_say = "Which of these are transcription factors:QM";
                    var payload = "";
                    //var payload = "{\"geneSetMembers\":[\"UST\", \"SAMD5\", \"PLAGL1\", \"SLC35D3\", \"SAMD3\", \"TMEM200A\", \"TRDN\"], \"geneSetName\": \"selection0\"}";
                    PostRequest(to_say + "~" + payload);
                }
            }

            new public string NLParse(string input) {
                Debug.LogWarning("NLParse called.");
                return "";
            }


            void ExternalCall(string j) {
                // Turn the update things into a function that takes a string for choices.
                //if (j == "1") { // Right click to avoid double input//Input.GetMouseButtonDown(0). Feel free to change to keypresses or whatever.
                //    // Send a note to the browser to crop around the current mouse position.
                //    Debug.LogWarning(b);
                //    Vector3 x;
                //    if (to_enter != new Vector3(0, 0, 0)) {
                //        x = to_enter; // Set to_enter elsewhere to make t
                //    }
                //    else {
                //        x = Input.mousePosition; // if point isn't set, steal it from the mouse
                //    }
                //    Debug.LogWarning("ORIGINAL: " + x.ToString());
                //    x = RemapToWindow(x);
                //    if (x[0] > 0 && x[1] > 0) {
                //        string point = x.ToString().Replace("(", "[").Replace(")", "]");
                //        Debug.LogWarning("POINT" + point);
                //        // We just need a nice centerpoint. How to get?
                //        b.EvalJS("this.cgm.brush_crop_matrix(" + point + ")").Then(ret => {
                //            Debug.LogWarning("Brush cropper turned on" + ret);
                //            b.EvalJS("this.saved_selected_nodes").Then(ret1 => {
                //                saved_nodes = ret1;
                //                Debug.LogWarning("Saved Nodes:" + saved_nodes);
                //                }).Done();
                //        }).Done();

                //    }
                //    else {
                //        Debug.LogWarning("You probably clicked outside the box.");
                //    }
                //    return;
                //}
                if (s == null) { // s for server, initializes the socket connection
                    // Calls when you press s too, right now.
                    s = GetSocket.Main(s);
                }
                if (j == "d") { // Downloads whatever, prints to debug.
                    //string[] the_list = { };
                    //s = GetSocket.Main(s, "", true);
                    byte[] bytesReceived = new byte[1024];
                    int i = s.Receive(bytesReceived);
                    string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(from_trips);
                }
                //else if (j == "g") { // g for genes
                //                     //s = GetSocket.Main(s, "", true);
                //                     // (REQUEST :CONTENT (TAG :TEXT "Create the gene set." :IMITATE-KEYBOARD-MANAGER T))

                //    // Dec 5 2019:
                //    //(tell :receiver BA :content (create-gene-set :request-body ((:gene-set-members $list) (:gene-set-name $name))))

                //    // Expanded below. Name should maybe be some kind of incrementing counter.
                //    //(tell: receiver BA: content(create - gene - set :request - body((: GENE - SET - MEMBERS "APEX2"
                //    //                                "APOD" "APP" "ASPA"
                //    //                                "MYDGF" "C1D" "CBR3"
                //    //                                "CDH19" "CLEC2B" "COL5A2"
                //    //                                "COL9A3" "CUBN" "DHRS3"
                //    //                                "ECHDC1" "ENPP2" "FAM3C"
                //    //                                "TENT5A" "FN1" "FXYD3"
                //    //                                "GALNT5" "IFNGR2" "ITGB5"
                //    //                                "ITM2B" "KHDRBS3" "LAMA4"
                //    //                                "LAMB1" "LAMC1" "LOXL3"
                //    //                                "LSAMP" "MAGED1" "MAGED2"
                //    //                                "MAGEH1" "MANSC1" "MAS1"
                //    //                                "MIA" "MRPS6" "PDLIM4"
                //    //                                "PDZRN3" "PLAT" "PLEKHA4"
                //    //                                "PON2" "PRKAR1A" "HTRA1"
                //    //                                "S100A13" "S100B" "SAT1"
                //    //                                "SCRG1" "SERPINA3"
                //    //                                "SERPINE2" "SLC26A2"
                //    //                                "SMPDL3A" "SNRPB2"
                //    //                                "SPAG16" "SPARC" "STXBP6"
                //    //                                "DYNLT3" "TIAM2" "TIMP1"
                //    //                                "ANO1" "TUSC3" "ZNF521")
                //    //                               (:GENE - SET - NAME
                //    //                                . "selection0"))))


                //    //(tell :receiver BA :content (create-gene-set :request-body ((:gene-set-members $list) (:gene-set-name $name))))
                //    string to_send = "(tell :receiver BA :content (create-gene-set :request-body ((:gene-set-members " + saved_nodes + ") (:gene-set-name . \"selection0\"))))";

                //    //string to_send = "(tell :content (selected-genes :gene-list (" + saved_nodes + ")))";
                //    Debug.LogWarning(to_send);
                //    byte[] bytesSent = Encoding.ASCII.GetBytes(to_send);
                //    // Create a socket connection with the specified server and port.s

                //    // Send request to the server.
                //    Debug.LogWarning(bytesSent + "  " + bytesSent.Length);

                //    int result = s.Send(bytesSent, bytesSent.Length, 0);
                //    byte[] bytesReceived = new byte[1024];
                //    Debug.LogWarning(result);
                //    Debug.LogWarning(" " + s.IsBound);
                //}
                else if (j == "2") { // pull a list of genes FROM FACILITATOR
                    byte[] bytesReceived = new byte[2048];
                    int i = s.Receive(bytesReceived);
                    string from_trips = Encoding.UTF8.GetString(bytesReceived);
                    Debug.LogWarning(from_trips);
                    // Parse the string here.
                    // (request :content (record-gene-data :filename "path on localhost"  :data " content of csv file")
                    // Literally just finds the substring with a pair of quotes around it.
                    string to_eval = Regex.Match(from_trips, @""".*""").Value;
                    to_eval = to_eval.Substring(1, to_eval.Length - 2); //UGH


                    Debug.LogWarning("GENES FROM BOB: " + to_eval);
                    //b.EvalJS("make_clust('" + to_eval + "');").Then(ret => saved_nodes = ret).Done();
                    // Send request to the server

                    to_display = to_eval;

                    atc.outputString = to_display;
                    //atc.textField = true; // Not displaying under current specs
                    byte[] bytesSent = Encoding.ASCII.GetBytes("(reply :content (success :status done)");
                    int result = s.Send(bytesSent, bytesSent.Length, 0);
                }
            }


            ///
            public Vector3 RemapToWindow(Vector3 x) {
                /// remap input pixel to its relative position in the actual matrix.
                /// Hard coded for me rn
                /// (0,0) is the top left
                // 270, 428 are the top left in Unity (270, 367)
                // 725, 56 are bottom right. (725, 56)
                // ^ Grab by right clicking on each corner and reading unity console.

                // Width is 459 (455)
                // Height is 372 (312)
                // ^ Grab these by going to javascript console (on Browser (GUI))
                //      and typing in > cgm.params.viz.clust.dim.width
                //> cgm.params.viz.clust.dim.height
                // Switcheroo
                var old = (x[0], x[1]);
                x[0] = ((x[0] - 270) / (725 - 270)) * 455f; // 700 since was empirically too far right.
                x[1] = ((367 - x[1]) / (367 - 56)) * 312f;
                Debug.LogWarning("Remapped " + old + "to " + x);
                return x;
                
            }

            public string PostRequest(string rawSent) {
                string route = "clic/say?text="; // idk rn
                //to_return = ExecuteCommand(rawSent);
                if (io == null) {
                    io = GameObject.Find("interface").GetComponent<ClicIOClient>();
                }
                io.Post(route, rawSent);
                return "WAIT";
            }

            public void BobSaidSomething() {
                Debug.LogWarning("Bob said something back to the REST client.");
            }

            // Acquire the list of selected genes
            public void GrabSelected(string payload = "") {


                // Old way
                //b.EvalJS("this.saved_selected_nodes").Then(ret => {
                //    saved_nodes = ret;
                //    Debug.LogWarning("WE GOT THEM2: " + saved_nodes);

                //}).Done();
            }
#if UNITY_EDITOR

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
#endif
        }
    }
}