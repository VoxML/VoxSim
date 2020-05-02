using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using ZenFulcrum.EmbeddedBrowser;
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
            Vector3 goal_loc;
            Vector3 tiny_loc;
            Vector3 big_loc;
            CloudOfClouds cc;
            //FormWordCloud wc;
            string wordstring = ""; // List of all words across clouds
            public List<string[]> word_lists = new List<string[]>(); // one string for each cloud.



            void Start() {
                speed = 1;
                tiny = new Vector3(0.1f, 0.1f, 0.1f);
                big = new Vector3(1f, 1f, 1f);
                Vector3[] corners = new Vector3[4];
                GetComponent<RectTransform>().GetWorldCorners(corners);
                tiny_loc = new Vector3(50, 50, 0);
                //foreach (Vector3 corner in corners) {
                //    Debug.LogWarning(corner);
                //    tiny_loc = corner;
                //}


                ClusterGram cg = GetComponentInChildren<ClusterGram>();
                RectTransform rt = cg.gameObject.GetComponent<RectTransform>();
                big_loc = rt.position; // What a nice way to avoid measuring it.

                goal = big;
                goal_loc = big_loc;

                states = new List<string> { "Clustering", "WordClouding" };
                //wc = FindObjectOfType<FormWordCloud>();
                cc = FindObjectOfType<CloudOfClouds>();

                canvas = GetComponent<Canvas>();
                //Debug.LogWarning(canvas);
                if (canvas == null) {
                    Debug.LogError("Canvas not found..", gameObject);
                    this.enabled = false;
                }
            }

            void Update() {
                // switch modes on left mouse click
                ClusterGram cg = GetComponentInChildren<ClusterGram>();
                RectTransform rt = cg.gameObject.GetComponent<RectTransform>();

                GameObject cursor = transform.Find("UI Holder").Find("CursorBox").gameObject;
                //if (Input.GetKeyDown("w")) {
                //    // Don't see a reason not to have it here.
                //    //canvas.renderMode = RenderMode.ScreenSpaceCamera;
                //    BrowserInterface bi = FindObjectOfType<BrowserInterface>();
                //    var previous = cc.word_lists;
                //    wordstring = bi.GetDisplay();
                    
                //    goal = tiny;
                //    goal_loc = tiny_loc;
                //    cursor.SetActive(false);
                //    //string jsonString = "{ \"took\": 52, \"timed_out\": false, \"_shards\": { \"total\": 5, \"successful\": 5, \"skipped\": 0, \"failed\": 0 }, \"hits\": { \"total\": 389, \"max_score\": 0, \"hits\": [] }, \"aggregations\": { \"2\": { \"doc_count_error_upper_bound\": 271, \"sum_other_doc_count\": 20928, \"buckets\": [ { \"key\": \"garbage\", \"doc_count\": 590 }, { \"key\": \"spoon1\", \"doc_count\": 600 }, { \"key\": \"fork\", \"doc_count\": 700 }, { \"key\": \"base\", \"doc_count\": 1000 }, { \"key\": \"given\", \"doc_count\": 387 }, { \"key\": \"graph\", \"doc_count\": 387 }, { \"key\": \"number\", \"doc_count\": 387 }, { \"key\": \"power\", \"doc_count\": 387 }, { \"key\": \"social\", \"doc_count\": 387 }, { \"key\": \"system\", \"doc_count\": 387 }, { \"key\": \"consider\", \"doc_count\": 386 }, { \"key\": \"control\", \"doc_count\": 386 }, { \"key\": \"failure\", \"doc_count\": 500 }, { \"key\": \"figure\", \"doc_count\": 386 }, { \"key\": \"hybrid\", \"doc_count\": 100 }, { \"key\": \"scenario\", \"doc_count\": 386 }, { \"key\": \"smart\", \"doc_count\": 386 }, { \"key\": \"spread\", \"doc_count\": 386 }, { \"key\": \"using\", \"doc_count\": 386 }, { \"key\": \"values\", \"doc_count\": 400 }, { \"key\": \"propose\", \"doc_count\": 385 }, { \"key\": \"degree\", \"doc_count\": 384 }, { \"key\": \"forecast\", \"doc_count\": 200 }, { \"key\": \"algorithm\", \"doc_count\": 382 }, { \"key\": \"generation\", \"doc_count\": 382 }, { \"key\": \"high-order\", \"doc_count\": 382 }, { \"key\": \"hosploc\", \"doc_count\": 382 }, { \"key\": \"markov\", \"doc_count\": 382 }, { \"key\": \"structure\", \"doc_count\": 382 }, { \"key\": \"function\", \"doc_count\": 379 }, { \"key\": \"fraction\", \"doc_count\": 378 }, { \"key\": \"random\", \"doc_count\": 375 }, { \"key\": \"component\", \"doc_count\": 374 }, { \"key\": \"distribution\", \"doc_count\": 374 }, { \"key\": \"provide\", \"doc_count\": 371 }, { \"key\": \"problem\", \"doc_count\": 367 }, { \"key\": \"optimal\", \"doc_count\": 363 }, { \"key\": \"attack\", \"doc_count\": 362 }, { \"key\": \"percolation\", \"doc_count\": 362 }, { \"key\": \"communication\", \"doc_count\": 355 }, { \"key\": \"domain\", \"doc_count\": 355 }, { \"key\": \"represent\", \"doc_count\": 355 }, { \"key\": \"service\", \"doc_count\": 355 }, { \"key\": \"services\", \"doc_count\": 355 }, { \"key\": \"vertex\", \"doc_count\": 355 }, { \"key\": \"result\", \"doc_count\": 354 }, { \"key\": \"probability\", \"doc_count\": 353 }, { \"key\": \"autophagy\", \"doc_count\": 346 }, { \"key\": \"combination\", \"doc_count\": 346 }, { \"key\": \"drug\", \"doc_count\": 346 } ] } }, \"status\": 200 }";
                //    cc.word_lists = new List<string>();
                //    cc.word_lists.Add(wordstring);
                //    //Debug.LogWarning("ccwordlists" + cc.word_lists[0]);
                //    cc.MakeNewClouds();
                //    //wc.NewSphere(just_words: wordstring); // Will need 'real' new json someday


                //}
                if (Input.GetKeyDown("q")) {
                    // Create a subset to be rendered as a second (or third, etc) cloud.
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    BrowserInterface bi = FindObjectOfType<BrowserInterface>();
                    cc.gameObject.SetActive(true);
                    if(wordstring == "") {
                        wordstring = bi.GetDisplay();
                        var all_words = wordstring.Split(' ');
                        word_lists = new List<string[]>();
                        //word_lists.Add(all_words);
                        cc.all_words = new List<string>(all_words);
                    }
                    else {
                        
                        if (bi.GetDisplay().Length == wordstring.Length) {
                            return; // This is not a subset of the full list, it just is the list.
                        }
                        var to_add = bi.GetDisplay().Split(' ');
                        if (word_lists.Count > 0) {

                            if (word_lists[word_lists.Count - 1] == to_add) {
                                return;
                            }
                        }
                        word_lists.Add(to_add);
                    }

                    cc.word_lists = word_lists;

                    goal = tiny;
                    goal_loc = tiny_loc;
                    cursor.transform.parent.gameObject.SetActive(false);
                    cc.MakeNewClouds();

                }


                if (Input.GetKeyDown("c")) {
                    //canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    goal = big;
                    goal_loc = big_loc;
                    //cc.word_lists.Add(wordstring);
                    cc.MakeNewClouds();
                    //wc.NewSphere(just_words: "");
                }
                if ((rt.localScale - goal).magnitude > 0.01) {
                    if (goal == big) {
                        cursor.transform.parent.gameObject.SetActive(true);
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    }
                    rt.localScale = Vector3.Lerp(rt.localScale, goal, speed * Time.deltaTime);
                    rt.position = Vector3.Lerp(rt.position, goal_loc, speed * Time.deltaTime);

                }
                else { // snap in place.
                    rt.localScale = goal;
                    if(goal == big) {
                        cc.gameObject.SetActive(false); // No clouds while the clustergram is filling the screen.
                    }
                }
            }
        }
    }
}