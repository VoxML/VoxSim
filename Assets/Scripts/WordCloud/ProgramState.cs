using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;
using WordCloud;

using VoxSimPlatform.Network;

// usage: Attach this script to gameobject with Canvas component
// click mouse button to switch modes *note: worldspace will not be visible without scaling it
// https://docs.unity3d.com/ScriptReference/Canvas-renderMode.html

namespace WordCloud {
    namespace VoxSimPlatform {

        public class ProgramState : MonoBehaviour {
            Canvas canvas;
            List<string> states;
            int cur_state = 0;
            float speed;
            Vector3 tiny;
            Vector3 big;
            Vector3 goal;
            FormWordCloud wc;
            string wordstring = "test width booyah";


            void Start() {
                speed = 1;
                tiny = new Vector3(0.1f, 0.1f, 0.1f);
                big = new Vector3(1f, 1f, 1f);
                goal = big;
                states = new List<string> { "Clustering", "WordClouding" };
                wc = FindObjectOfType<FormWordCloud>();


                canvas = GetComponent<Canvas>();
                //Debug.LogWarning(canvas);
                if (canvas == null) {
                    Debug.LogError("Canvas not found..", gameObject);
                    this.enabled = false;
                }
            }

            void Update() {
                // switch modes on left mouse click
                Browser b = GetComponentInChildren<Browser>();
                RectTransform rt = b.gameObject.GetComponent<RectTransform>();

                GameObject cursor = transform.Find("CursorBox").gameObject;
                if (Input.GetKeyDown("w")) {
                    // Don't see a reason not to have it here.
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    BrowserInterface bi = FindObjectOfType<BrowserInterface>();
                    wordstring = bi.GetDisplay();
                    
                    goal = tiny;
                    cursor.SetActive(false);
                    //string jsonString = "{ \"took\": 52, \"timed_out\": false, \"_shards\": { \"total\": 5, \"successful\": 5, \"skipped\": 0, \"failed\": 0 }, \"hits\": { \"total\": 389, \"max_score\": 0, \"hits\": [] }, \"aggregations\": { \"2\": { \"doc_count_error_upper_bound\": 271, \"sum_other_doc_count\": 20928, \"buckets\": [ { \"key\": \"garbage\", \"doc_count\": 590 }, { \"key\": \"spoon1\", \"doc_count\": 600 }, { \"key\": \"fork\", \"doc_count\": 700 }, { \"key\": \"base\", \"doc_count\": 1000 }, { \"key\": \"given\", \"doc_count\": 387 }, { \"key\": \"graph\", \"doc_count\": 387 }, { \"key\": \"number\", \"doc_count\": 387 }, { \"key\": \"power\", \"doc_count\": 387 }, { \"key\": \"social\", \"doc_count\": 387 }, { \"key\": \"system\", \"doc_count\": 387 }, { \"key\": \"consider\", \"doc_count\": 386 }, { \"key\": \"control\", \"doc_count\": 386 }, { \"key\": \"failure\", \"doc_count\": 500 }, { \"key\": \"figure\", \"doc_count\": 386 }, { \"key\": \"hybrid\", \"doc_count\": 100 }, { \"key\": \"scenario\", \"doc_count\": 386 }, { \"key\": \"smart\", \"doc_count\": 386 }, { \"key\": \"spread\", \"doc_count\": 386 }, { \"key\": \"using\", \"doc_count\": 386 }, { \"key\": \"values\", \"doc_count\": 400 }, { \"key\": \"propose\", \"doc_count\": 385 }, { \"key\": \"degree\", \"doc_count\": 384 }, { \"key\": \"forecast\", \"doc_count\": 200 }, { \"key\": \"algorithm\", \"doc_count\": 382 }, { \"key\": \"generation\", \"doc_count\": 382 }, { \"key\": \"high-order\", \"doc_count\": 382 }, { \"key\": \"hosploc\", \"doc_count\": 382 }, { \"key\": \"markov\", \"doc_count\": 382 }, { \"key\": \"structure\", \"doc_count\": 382 }, { \"key\": \"function\", \"doc_count\": 379 }, { \"key\": \"fraction\", \"doc_count\": 378 }, { \"key\": \"random\", \"doc_count\": 375 }, { \"key\": \"component\", \"doc_count\": 374 }, { \"key\": \"distribution\", \"doc_count\": 374 }, { \"key\": \"provide\", \"doc_count\": 371 }, { \"key\": \"problem\", \"doc_count\": 367 }, { \"key\": \"optimal\", \"doc_count\": 363 }, { \"key\": \"attack\", \"doc_count\": 362 }, { \"key\": \"percolation\", \"doc_count\": 362 }, { \"key\": \"communication\", \"doc_count\": 355 }, { \"key\": \"domain\", \"doc_count\": 355 }, { \"key\": \"represent\", \"doc_count\": 355 }, { \"key\": \"service\", \"doc_count\": 355 }, { \"key\": \"services\", \"doc_count\": 355 }, { \"key\": \"vertex\", \"doc_count\": 355 }, { \"key\": \"result\", \"doc_count\": 354 }, { \"key\": \"probability\", \"doc_count\": 353 }, { \"key\": \"autophagy\", \"doc_count\": 346 }, { \"key\": \"combination\", \"doc_count\": 346 }, { \"key\": \"drug\", \"doc_count\": 346 } ] } }, \"status\": 200 }";
                    wc.NewSphere(just_words: wordstring); // Will need 'real' new json someday


                }
                if (Input.GetKeyDown("c")) {
                    //canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    goal = big;
                    wc.NewSphere(just_words: "");
                }
                if ((rt.localScale - goal).magnitude > 0.01) {
                    if (goal == big) {
                        cursor.SetActive(true);
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    }
                    rt.localScale = Vector3.Lerp(rt.localScale, goal, speed * Time.deltaTime);
                }
                else { // snap in place.
                    rt.localScale = goal;
                }
            }
        }
    }
}