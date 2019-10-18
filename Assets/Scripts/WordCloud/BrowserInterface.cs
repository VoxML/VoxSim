using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ZenFulcrum.EmbeddedBrowser;


namespace VoxSimPlatform {
    namespace Network {
        public class BrowserInterface : MonoBehaviour {
            float prior0;
            float prior1;
            string saved_nodes = "jyf";
            Browser b;
            ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]

            // Start is called before the first frame update
            void Start() {
                // immediately remember the browser forever.
                b = Selection.activeGameObject.GetComponent<Browser>();
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetMouseButtonDown(0)) {
                    // Send a note to the browser to crop around the current mouse position.
                    b.EvalJS("").Then(ret => Debug.LogWarning("" + ret));
                }
            }

            public string Arbitrary_Func(string payload = "") {
                

                //b.EvalJS("").Then(ret => Debug.LogWarning(ret)).Done();
                //b.EvalJS("document.title").Then(ret => Debug.LogWarning("Document title: " + ret)).Done();

                b.EvalJS("this.saved_selected_nodes").Then(ret => Debug.LogWarning("Returned Cells: " + ret)).Done();
                b.EvalJS("document.title").Then(ret => Debug.LogWarning("Document title: " + ret)).Done();
                b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                Debug.LogWarning("WE GOT THEM: " + saved_nodes);
                //b.CallFunction("gibberishjask").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();

                //b.CallFunction("get_selected_nodes").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("Clustergrammer.get_selected_nodes").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("Clustergrammer").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("export_matrix_string").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("Clustergrammer.export_matrix_string").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("Clustergrammer.export_matrix").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("export_matrix").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();
                //b.CallFunction("close").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done(); // should kill it

                //b.CallFunction("d3.selection").Then(ret => Debug.LogWarning("Saved Nodes: " + ret)).Done();


                //Debug.LogWarning(saved_nodes);

                //Debug.LogWarning(ip + "\n" + ip.GetType().ToString());
                // ZenFulcrum.EmbeddedBrowser.Promise`1[ZenFulcrum.EmbeddedBrowser.JSONNode]


                //Webpack is a module bundler.It takes disparate dependencies, creates modules for them and bundles the entire network up into manageable output files.This is especially useful for Single Page Applications(SPAs), which is the defacto standard for Web Applications today.

                // TODO tomorrow: Figure out what parameters belong passed into Clustergrammer.
                // Right now breaks on config.networkdata because something needs initialized.
                // Rejection: TypeError: Cannot read property 'network_data' of undefined
                //at make_config(https://game.local/clustergrammer-master/clustergrammer.js:203:31)
                //at Clustergrammer(https://game.local/clustergrammer-master/clustergrammer.js:91:17)
                //at eval(eval at < anonymous > (: 1:35), < anonymous >:1:1)
                //at<anonymous>:1:35
                // They used a tool (webpack) to squish all functions into one file here: https://medium.com/ag-grid/webpack-tutorial-understanding-how-it-works-f73dfa164f01

                return "test";
            }

            [MenuItem("Jarvis/Arbitrary &#A")]
            static void Arb() {
                BrowserInterface bi = Selection.activeGameObject.GetComponent<BrowserInterface>();
                Debug.LogWarning("Result: " + bi.Arbitrary_Func());
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