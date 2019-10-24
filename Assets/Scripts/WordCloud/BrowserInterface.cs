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
            //Vector2 topleft; // of the cgm window
            //Vector2 bottomright; // of the cgm window
            Vector3 to_enter = new Vector3(0,0,0); // Only a Vector3 because that's how mouse position is recorded. Only first 2 used
            string saved_nodes = "";
            Browser b;
            ZenFulcrum.EmbeddedBrowser.IPromise<ZenFulcrum.EmbeddedBrowser.JSONNode> ip = null; //`1[ZenFulcrum.EmbeddedBrowser.JSONNode]

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

                    }
                    else {
                        Debug.LogWarning("You probably clicked outside the box.");
                    }

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
                x[0] = ((x[0] - 263) / (700  -263)) * 425.5f; // 700 since was empirically too far right.
                x[1] = ((276 - x[1]) / (276 - 55)) * 215f;
                return x;
                
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