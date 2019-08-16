using UnityEngine;
using System.Collections.Generic;
//using LitJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using VoxSimPlatform.Vox;
using System.Linq;
using UnityEditor;
using System.IO;


// Based on code from Andrew Sage: https://medium.com/@SymboticaAndrew/a-vr-word-cloud-in-unity-f7cb8cf17b6b

namespace VoxSimPlatform {

    public class Phrase {
        // From tutorial. But the 'occurrences' should be remappable to whatever importance metric we wind up using.
        public string term;
        public float occurrences;

        //Items added to allow smooth transitions to new places/sizes
        public Vector3 size; // Font size
        public Vector3 ideal_position; // Where the phrase wants to be (will move there)
        public bool is_happy = false; // Whether is complacent, or will try to move to ideal location
        public GameObject obj; // Probably want an actual pointer to the object lol
    }

    public class FormWordCloud : MonoBehaviour {
        public GameObject childObject;
        public float size = 10.0f;
        private List<Phrase> phrases = new List<Phrase>(); // ordered from biggest to smallest
        private Dictionary<string, Phrase> precious_children = new Dictionary<string, Phrase>(); // For fast lookup
                                                                                                 //private List<Phrase> randomisedPhrases = new List<Phrase>();
        Transform camera1;
        private int bug = 0;

        private float totalOccurrences = 0.0f;
        private float maxOccurrences = 0;


        //private string jsonString = "[{\"term\":\"the\", \"occurrences\":504},{\"term\":\"to\",\"occurrences\":447},{\"term\":\"rt\",\"occurrences\":433},{\"term\":\"a\",\"occurrences\":382},{\"term\":\"in\",\"occurrences\":299},{\"term\":\"of\",\"occurrences\":274},{\"term\":\"adventure\",\"occurrences\":236},{\"term\":\"and\",\"occurrences\":216},{\"term\":\"for\",\"occurrences\":166},{\"term\":\"is\",\"occurrences\":157},{\"term\":\"on\",\"occurrences\":154},{\"term\":\"cars\",\"occurrences\":136},{\"term\":\"it\",\"occurrences\":122},{\"term\":\"you\",\"occurrences\":116},{\"term\":\"with\",\"occurrences\":100},{\"term\":\"from\",\"occurrences\":87},{\"term\":\"at\",\"occurrences\":85},{\"term\":\"i\",\"occurrences\":85},{\"term\":\"this\",\"occurrences\":85},{\"term\":\"that\",\"occurrences\":83}]";

        // To get grabbed from somewhere else in future.
        // jsonString2 is identical, except values for spoon and fork are swapped.
        private string jsonString = "{ \"took\": 52, \"timed_out\": false, \"_shards\": { \"total\": 5, \"successful\": 5, \"skipped\": 0, \"failed\": 0 }, \"hits\": { \"total\": 389, \"max_score\": 0, \"hits\": [] }, \"aggregations\": { \"2\": { \"doc_count_error_upper_bound\": 271, \"sum_other_doc_count\": 20928, \"buckets\": [ { \"key\": \"garbage\", \"doc_count\": 590 }, { \"key\": \"spoon\", \"doc_count\": 700 }, { \"key\": \"fork\", \"doc_count\": 600 }, { \"key\": \"base\", \"doc_count\": 388 }, { \"key\": \"given\", \"doc_count\": 387 }, { \"key\": \"graph\", \"doc_count\": 387 }, { \"key\": \"number\", \"doc_count\": 387 }, { \"key\": \"power\", \"doc_count\": 387 }, { \"key\": \"social\", \"doc_count\": 387 }, { \"key\": \"system\", \"doc_count\": 387 }, { \"key\": \"consider\", \"doc_count\": 386 }, { \"key\": \"control\", \"doc_count\": 386 }, { \"key\": \"failure\", \"doc_count\": 500 }, { \"key\": \"figure\", \"doc_count\": 386 }, { \"key\": \"hybrid\", \"doc_count\": 100 }, { \"key\": \"scenario\", \"doc_count\": 386 }, { \"key\": \"smart\", \"doc_count\": 386 }, { \"key\": \"spread\", \"doc_count\": 386 }, { \"key\": \"using\", \"doc_count\": 386 }, { \"key\": \"values\", \"doc_count\": 400 }, { \"key\": \"propose\", \"doc_count\": 385 }, { \"key\": \"degree\", \"doc_count\": 384 }, { \"key\": \"forecast\", \"doc_count\": 200 }, { \"key\": \"algorithm\", \"doc_count\": 382 }, { \"key\": \"generation\", \"doc_count\": 382 }, { \"key\": \"high-order\", \"doc_count\": 382 }, { \"key\": \"hosploc\", \"doc_count\": 382 }, { \"key\": \"markov\", \"doc_count\": 382 }, { \"key\": \"structure\", \"doc_count\": 382 }, { \"key\": \"function\", \"doc_count\": 379 }, { \"key\": \"fraction\", \"doc_count\": 378 }, { \"key\": \"random\", \"doc_count\": 375 }, { \"key\": \"component\", \"doc_count\": 374 }, { \"key\": \"distribution\", \"doc_count\": 374 }, { \"key\": \"provide\", \"doc_count\": 371 }, { \"key\": \"problem\", \"doc_count\": 367 }, { \"key\": \"optimal\", \"doc_count\": 363 }, { \"key\": \"attack\", \"doc_count\": 362 }, { \"key\": \"percolation\", \"doc_count\": 362 }, { \"key\": \"communication\", \"doc_count\": 355 }, { \"key\": \"domain\", \"doc_count\": 355 }, { \"key\": \"represent\", \"doc_count\": 355 }, { \"key\": \"service\", \"doc_count\": 355 }, { \"key\": \"services\", \"doc_count\": 355 }, { \"key\": \"vertex\", \"doc_count\": 355 }, { \"key\": \"result\", \"doc_count\": 354 }, { \"key\": \"probability\", \"doc_count\": 353 }, { \"key\": \"autophagy\", \"doc_count\": 346 }, { \"key\": \"combination\", \"doc_count\": 346 }, { \"key\": \"drug\", \"doc_count\": 346 } ] } }, \"status\": 200 }";
        private string jsonString2 = "{ \"took\": 52, \"timed_out\": false, \"_shards\": { \"total\": 5, \"successful\": 5, \"skipped\": 0, \"failed\": 0 }, \"hits\": { \"total\": 389, \"max_score\": 0, \"hits\": [] }, \"aggregations\": { \"2\": { \"doc_count_error_upper_bound\": 271, \"sum_other_doc_count\": 20928, \"buckets\": [ { \"key\": \"garbage\", \"doc_count\": 590 }, { \"key\": \"spoon\", \"doc_count\": 600 }, { \"key\": \"fork\", \"doc_count\": 700 }, { \"key\": \"base\", \"doc_count\": 388 }, { \"key\": \"given\", \"doc_count\": 387 }, { \"key\": \"graph\", \"doc_count\": 387 }, { \"key\": \"number\", \"doc_count\": 387 }, { \"key\": \"power\", \"doc_count\": 387 }, { \"key\": \"social\", \"doc_count\": 387 }, { \"key\": \"system\", \"doc_count\": 387 }, { \"key\": \"consider\", \"doc_count\": 386 }, { \"key\": \"control\", \"doc_count\": 386 }, { \"key\": \"failure\", \"doc_count\": 500 }, { \"key\": \"figure\", \"doc_count\": 386 }, { \"key\": \"hybrid\", \"doc_count\": 100 }, { \"key\": \"scenario\", \"doc_count\": 386 }, { \"key\": \"smart\", \"doc_count\": 386 }, { \"key\": \"spread\", \"doc_count\": 386 }, { \"key\": \"using\", \"doc_count\": 386 }, { \"key\": \"values\", \"doc_count\": 400 }, { \"key\": \"propose\", \"doc_count\": 385 }, { \"key\": \"degree\", \"doc_count\": 384 }, { \"key\": \"forecast\", \"doc_count\": 200 }, { \"key\": \"algorithm\", \"doc_count\": 382 }, { \"key\": \"generation\", \"doc_count\": 382 }, { \"key\": \"high-order\", \"doc_count\": 382 }, { \"key\": \"hosploc\", \"doc_count\": 382 }, { \"key\": \"markov\", \"doc_count\": 382 }, { \"key\": \"structure\", \"doc_count\": 382 }, { \"key\": \"function\", \"doc_count\": 379 }, { \"key\": \"fraction\", \"doc_count\": 378 }, { \"key\": \"random\", \"doc_count\": 375 }, { \"key\": \"component\", \"doc_count\": 374 }, { \"key\": \"distribution\", \"doc_count\": 374 }, { \"key\": \"provide\", \"doc_count\": 371 }, { \"key\": \"problem\", \"doc_count\": 367 }, { \"key\": \"optimal\", \"doc_count\": 363 }, { \"key\": \"attack\", \"doc_count\": 362 }, { \"key\": \"percolation\", \"doc_count\": 362 }, { \"key\": \"communication\", \"doc_count\": 355 }, { \"key\": \"domain\", \"doc_count\": 355 }, { \"key\": \"represent\", \"doc_count\": 355 }, { \"key\": \"service\", \"doc_count\": 355 }, { \"key\": \"services\", \"doc_count\": 355 }, { \"key\": \"vertex\", \"doc_count\": 355 }, { \"key\": \"result\", \"doc_count\": 354 }, { \"key\": \"probability\", \"doc_count\": 353 }, { \"key\": \"autophagy\", \"doc_count\": 346 }, { \"key\": \"combination\", \"doc_count\": 346 }, { \"key\": \"drug\", \"doc_count\": 346 } ] } }, \"status\": 200 }";


        void Start() {
            Camera main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            if (main_camera != null) {
                camera1 = main_camera.transform;
            }
            else {
                camera1 = Camera.main.transform;
            }

            //Pull from somewhere more real in future.
            //phrases = ProcessCSV("Assets/Resources/combined1699OvlapedGenes.csv");
            phrases = ProcessWords(jsonString);
            float start_time;
            float end_time;
            start_time = Time.realtimeSinceStartup;
            Sphere();
            end_time = Time.realtimeSinceStartup;
            Debug.LogWarning("Sphere took " + (end_time - start_time) + " ... seconds?");
        }

        // Update is called once per frame. FixedUpdate is 50 times a second regardless of framerate
        // If anything seems slow, it's probably because of dead weight somewhere in here :)
        void FixedUpdate() {
            Vector3 Point;
            float zDistance;

            // Tell each of the objects to look at the camera
            foreach (Phrase child in precious_children.Values) {
                if (child.obj.transform.parent != transform && child.obj.transform.parent.parent != transform) {
                    // Someone has stolen my child :o
                    if (child.obj.name.EndsWith("*")) {
                        child.obj.transform.parent.SetParent(transform);
                    }
                    else {
                        child.obj.transform.SetParent(transform);
                    }
                }
                Voxeme vx = child.obj.transform.parent.GetComponent<Voxeme>(); // Kinda awkward to do this here.
                vx.is_phrase = true; //Every frame, like taking a sledghammer to a banana
                                     //Debug.Log("IS PHRASE: " + vx.is_phrase);
                vx.moveSpeed = 0.5f;
                // Look at the camera
                Quaternion toRotation1 = Quaternion.LookRotation(child.obj.transform.position - camera1.position);
                float speed = 0.7f;
                if (Quaternion.Angle(toRotation1, child.obj.transform.rotation) > 5) {
                    child.obj.transform.rotation = Quaternion.Lerp(child.obj.transform.rotation, toRotation1, speed * Time.deltaTime);
                }
                if (child.obj.transform.localScale != child.size) {
                    //child.obj.GetComponent<TextMeshPro>().fontSize = Quaternion.Lerp(child.obj.GetComponent<TextMeshPro>().fontSize, child.size, speed * Time.deltaTime);
                    child.obj.transform.localScale = Vector3.Lerp(child.obj.transform.localScale, child.size, speed * Time.deltaTime);
                }
                // If you're not where you're supposed to be, let the voxphrase physics know that.
                // Make it so it does not override movement.
                if (!child.is_happy && vx.targetPosition != child.ideal_position) {
                    // Increment to new location. Only do if position is not set manually by user.
                    vx.targetPosition = child.ideal_position;
                    child.is_happy = true; // Kinda papering over the problem right now. Should probably specify when things should override preexisting locations.
                }
            }
        }

        // Create new locations for sphere based on new numbers.
        void NewSphere(string new_json) {
            Dictionary<string, Phrase> old_children = precious_children;
            precious_children = new Dictionary<string, Phrase>();


            List<Phrase> new_phrases = ProcessWords(new_json); // fills in precious children
            Dictionary<string, Phrase> new_precious_children = new Dictionary<string, Phrase>();
            List<Vector3>.Enumerator point_locations = MakePointList(new_phrases.Count);

            point_locations.MoveNext();

            foreach (Phrase child in new_phrases) {
                new_precious_children.Add(child.term, child);
            }

            // Loop through precious children, union of old AND new
            List<string> old_and_new_children = Enumerable.Union(new_precious_children.Keys, precious_children.Keys).ToList();

            foreach (string child in old_and_new_children) {
                // see if they are in new sphere
                if (new_precious_children.ContainsKey(child)) {
                    // if they are, give them instructions to slowly move to new location
                    if (old_children.ContainsKey(child)) {
                        // In both, just need to set new location and size as ideals
                        new_precious_children[child].ideal_position = point_locations.Current + transform.position;
                        new_precious_children[child].obj = old_children[child].obj;
                        new_precious_children[child].size = old_children[child].size;

                        point_locations.MoveNext();
                    }
                    else {
                        // New child, grow out of nothingness.
                        // TODO: that^
                    }
                }
                else {
                    // if not, disown them
                    // Talk down to them until they shrink into nothingness
                    // Then DestroyImmediate
                    // Get out of precious children list
                }
            }
            // TODO: Make all the new phrases that otherwise don't exist

            precious_children = new_precious_children;

        }

        //// Diana keeps taking away my children >:(
        //// I love my children, and they should always love me
        //// Actually, might be easier attached to the phrase lol
        //void HelicopterParent() {

        //}


        //// Keep everything nice and visible. Not implemented.
        //private void Jiggle() {

        //}

        // Generate the locations for words around the sphere
        // Return an enumerable in case we make an arbitrary-size method in future.
        // (That may allow us to do some sort of spiral-backward thing)
        private List<Vector3>.Enumerator MakePointList(float points) {
            Debug.LogWarning(phrases.Count);
            // points is the number of phrases
            float increment = Mathf.PI * (3 - Mathf.Sqrt(5));
            float offset = 2 / points;

            List<Vector3> point_locations = new List<Vector3>();
            for (float i = 0; i < points; i++) {
                float y = i * offset - 1 + (offset / 2);
                float radius = Mathf.Sqrt(1 - y * y);
                //float radius_scaled = radius * (totalOccurances / phrases[(int)i].occurrences);
                float angle = i * increment;
                Vector3 pos = new Vector3((Mathf.Cos(angle) * radius * size), y * size, Mathf.Sin(angle) * radius * size);
                point_locations.Add(pos);
            }

            // Gotta add OUR position lol
            point_locations.Sort((x, y) => Vector3.Distance(x + transform.position, camera1.position).CompareTo(Vector3.Distance(y + transform.position, camera1.position)));
            return point_locations.GetEnumerator();

        }



        // More important words to the front now.
        private void Sphere() {
            float points = phrases.Count;
            float increment = Mathf.PI * (3 - Mathf.Sqrt(5));
            float offset = 2 / points;

            List<Vector3>.Enumerator point_locations = MakePointList(points);

            // Populate the points with text.
            // Could probably use a cleanup.
            for (float i = 0; i < points; i++) {
                Phrase phrase = phrases[(int)i];
                point_locations.MoveNext();
                Vector3 pos = point_locations.Current;

                // Create the object as a child of the sphere
                GameObject child = Instantiate(childObject, pos + transform.position, Quaternion.identity) as GameObject;

                child.transform.SetParent(transform);

                Voxphrase vp = child.GetComponent<Voxphrase>();
                float steepness = 0.7f;

                float scale = 1 / (1 + Mathf.Exp(-steepness * (phrase.occurrences - (maxOccurrences * 0.75f)))) + 0.25f;

                //float scale = Mathf.Log(phrase.occurrences) / Mathf.Log(maxOccurrences);
                child.transform.localScale = new Vector3(scale, scale, scale);
                vp.setupVoxPhrase(phrase);
                precious_children.Add(child.name, phrase);
            }
        }

        private List<Phrase> ProcessWords(string jsonString) {
            JObject jsonvale = JObject.Parse(jsonString);
            //Not exactly future-proof here. This is the structure of the current Json returned
            Debug.Log(jsonvale["aggregations"]["2"]["buckets"]);
            JArray second_layer = (JArray)jsonvale["aggregations"]["2"]["buckets"];

            List<Phrase> to_return = new List<Phrase>();

            for (int i = 0; i < second_layer.Count; i++) {
                Phrase phrase = new Phrase();
                phrase.term = second_layer[i]["key"].ToString().ToLower();
                phrase.occurrences = float.Parse(second_layer[i]["doc_count"].ToString());
                if (phrase.occurrences > maxOccurrences) {
                    maxOccurrences = phrase.occurrences;
                }
                to_return.Add(phrase);
                totalOccurrences += phrase.occurrences;
            }


            // Sort the list by number of occurrences
            to_return.Sort((x, y) => x.occurrences.CompareTo(y.occurrences));
            to_return.Reverse();
            return to_return;
        }

        private List<Phrase> ProcessCSV(string path) {
            // Open up a CSV file, make the structure we want.
            // But right this moment, it just returns a list of proteins connected to one gene.

            List<Phrase> to_return = new List<Phrase>();
            totalOccurrences = 0;

            StreamReader reader = new StreamReader(path);
            string first_line = reader.ReadLine();
            string second_line = reader.ReadLine();
            reader.Close();


            string[] first_line_list = first_line.Split(',');
            string[] second_line_list = second_line.Split(',');
            for (int i = 2; i < first_line_list.Length - 1; i++) {
                Phrase phrase = new Phrase();
                phrase.term = first_line_list[i].Replace('.','_').ToLower();
                float correlation = float.Parse(second_line_list[i]);
                if(correlation > 0) {
                    phrase.occurrences = correlation;
                    to_return.Add(phrase);
                    totalOccurrences += phrase.occurrences;
                    if(correlation > maxOccurrences) {
                        maxOccurrences = correlation;
                    }
                }
                
                
            }

            return to_return;
        }



        [MenuItem("VoxSim/New WordCloud &#w")]
        static void NewWordCloud() {
            FormWordCloud wc = Selection.activeGameObject.GetComponent<FormWordCloud>();
            wc.NewSphere(wc.jsonString2); // Will need 'real' new json someday
        }

        // Makes sure that we have this object selected yo
        [MenuItem("VoxSim/New WordCloud &#w", true)]
        static bool ValidateNewWordCloud() {
            return (Selection.activeGameObject != null) &&
                   (Selection.activeGameObject.GetComponent<FormWordCloud>() != null);
        }
    }
}