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
            Vector2 topleft; // of the cgm window
            Vector2 bottomright; // of the cgm window
            string saved_nodes = "jyf";
            Browser b;
            ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]

            // Start is called before the first frame update
            void Start() {
                // immediately remember the browser forever.
                b = FindObjectOfType<Browser>(); // Only one browser allowed right now. Just a couple lines to specify further
            }

            // Update is called once per frame
            void Update() {
                if (Input.GetMouseButton(1)) { // Right click to avoid double input//Input.GetMouseButtonDown(0)
                    // Send a note to the browser to crop around the current mouse position.
                    Debug.LogWarning(b);
                    var x = Input.mousePosition;
                    Debug.LogWarning(x.ToString());
                    //x[0] = 30;
                    //x[1] = 30;
                    remap_to_window(x);
                    string point = x.ToString().Replace("(", "[").Replace(")","]");
                    Debug.LogWarning(point);

                    //b.EvalJS("console.log(this)");
                    // We just need a nice centerpoint. How to get?
                    //b.EvalJS("this.cgm.brush_crop_matrix([[0,0],[10,10]])").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();
                    b.EvalJS("this.cgm.brush_crop_matrix("+ point + ")").Then(ret => Debug.LogWarning("Brush cropper turned on" + ret)).Done();

                    //b.EvalJS("this.cgm").Then(ret => Debug.LogWarning("CG: " + ret)).Done();

                    //Debug.LogWarning(saved_nodes);
                }
                if (Input.GetKeyDown("p")) {
                    // Set parameters. Like window height/width within
                    // Get pixel of the top left
                    // Don't even need bottom right I wager.
                }
            }

            ///
            private void remap_to_window(Vector3 x) {
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
                x[0] = ((x[0] - 263) / (689  -263)) * 425.5f;
                x[1] = ((276 - x[1]) / (276 - 55)) * 215f;
                
            }

            public string Arbitrary_Func(string payload = "") {
                
                //b.EvalJS("this.saved_selected_nodes").Then(ret => Debug.LogWarning("Returned Cells: " + ret)).Done();
                //b.EvalJS("document.title").Then(ret => Debug.LogWarning("Document title: " + ret)).Done();
                b.EvalJS("this.saved_selected_nodes").Then(ret => saved_nodes = ret).Done();
                Debug.LogWarning("WE GOT THEM: " + saved_nodes); // race condition. 
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