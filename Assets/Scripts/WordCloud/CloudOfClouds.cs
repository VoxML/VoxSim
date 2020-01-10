using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VoxSimPlatform.Core;
using VoxSimPlatform.Vox;



//NOTE: Not currently used. Placeholder in case of handling multiple clouds at once

// 11/21/19: Gonna start trying to have multiple clouds to allow selection of subsets

namespace WordCloud {
    public class CloudOfClouds : MonoBehaviour {
        // CLOUD OF CLOUDS
        // FOR EV ER
        // AND EV ER
        // HALLELUJAH HALLELUJAH
        // Use this for initialization

        private List<FormWordCloud> clouds = new List<FormWordCloud>();
        FormWordCloud prime;
        public List<string> all_words = new List<string>(); // List of all the words to display
        public List<string[]> word_lists = new List<string[]>(); // one string for each cloud, internally space delineated.

        private Dictionary<string, Phrase> unloved_orphans = new Dictionary<string, Phrase>(); // For fast lookup

        List<Phrase> to_destroy = new List<Phrase>(); // To avoid errors deleting in the for loop

        public float size = 2.0f;
        Transform camera1;



        void Start() {
            // Probably opens the CSV file
            // Distribute words among the clouds
            // Like, negative words to one cloud, positive to the other.
            // And/or divvied up by the genes.

            Camera main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();

            if (main_camera != null) {
                camera1 = main_camera.transform;
            }
            else {
                camera1 = Camera.main.transform;
            }

            foreach (FormWordCloud child in GetComponentsInChildren<FormWordCloud>()) {
                if (prime == null) prime = child; // One of the clouds becomes prime
                else clouds.Add(child);
            }
            prime.name = "CloudPrime";
            //if (clouds.Count == 0) {
            //    GameObject new_child = Instantiate(prime.gameObject, new Vector3(0, 0, 0), Quaternion.identity);
            //    //GameObject asterisk = new_child.transform.Find("phrase*").gameObject;
            //    new_child.transform.SetParent(transform);
            //    new_child.transform.position = transform.position;
            //    new_child.name = "0";
            //}
        }

        // Update is called once per frame
        void Update() {
            //if (clouds.Count == 0) {
            //    GameObject new_child = Instantiate(prime.gameObject, new Vector3(0, 0, 0), Quaternion.identity);
            //    //GameObject asterisk = new_child.transform.Find("phrase*").gameObject;
            //    new_child.transform.SetParent(transform);
            //    new_child.transform.position = transform.position;
            //    new_child.name = "0";
            //    clouds.Add(new_child.GetComponent<FormWordCloud>());
            //}



            // Probably not much in here. Maybe prevent clouds from overlapping each other? That'd be about it.
            if (Input.GetKeyDown(KeyCode.M)) {

                // Should divvy up the orphans that are wanted, leaving only unwanted ones in its wake.
                MakeNewClouds();

            }

            foreach (Phrase child in unloved_orphans.Values) {
                //if (child.obj.name.EndsWith("*")) {
                //    //child.obj.transform.parent.SetParent(transform);
                //    child.asterisk = child.obj;
                //    child.obj = child.obj.transform.parent.gameObject; // point exclusively to the top-level.
                //}
                //if (child.asterisk == null) {
                //    // Set that yo
                //    child.asterisk = child.obj.transform.Find(child.term + "*").gameObject;
                //}
                //if (child.obj.transform.parent != transform) {
                //    // Someone has stolen my child :o

                //    // Quick check if its parent is in our list of highlight spots
                //    if (child.obj.transform.parent != null && highlight_points.Contains(child.obj.transform.parent.gameObject)) {
                //        child.is_highlighted = true;
                //        Debug.LogWarning("Got " + child.term + " highlighted.");
                //    }
                //    child.obj.transform.SetParent(transform);
                //}
                //Voxeme vx = child.obj.transform.GetComponent<Voxeme>(); // Kinda awkward to do this here.
                ////vx.is_phrase = true; //Every frame, like taking a sledghammer to a banana
                //vx.moveSpeed = 0.5f;//
                //// Look at the camera
                //Quaternion toRotation1;
                //toRotation1 = Quaternion.LookRotation(child.asterisk.transform.position - camera1.position);
                //float speed = 0.7f;
                //if (Quaternion.Angle(toRotation1, child.asterisk.transform.rotation) > 5) {
                //    child.asterisk.transform.rotation = Quaternion.Lerp(child.asterisk.transform.rotation, toRotation1, speed * Time.deltaTime);
                //}
                //if ((child.asterisk.transform.localScale - child.size).magnitude > 0.001) {
                //    child.asterisk.transform.localScale = Vector3.Lerp(child.asterisk.transform.localScale, child.size, speed * Time.deltaTime);
                //}
                //// If you're not where you're supposed to be, let the voxphrase physics know that.
                //// Make it so it does not override movement.
                //if (!child.is_happy && vx.targetPosition != child.ideal_position) {
                //    // Increment to new location. Only do if position is not set manually by user.
                //    vx.targetPosition = child.ideal_position;
                //    child.is_happy = true; // Kinda papering over the problem right now. Should probably specify when things should override preexisting locations.
                //}

                //if (child.asterisk.transform.localScale.magnitude < 0.005) { // Explode the heckin tiny ones that shrunk the heck away
                //    to_destroy.Add(child);
                //}

            }

            ObjectSelector objSelector = GameObject.Find("VoxWorld").GetComponent<ObjectSelector>();
            //foreach (Phrase child in to_destroy) {
            //    // DELETE DELETE DELETE
            //    // Add steps to this until no errors remain
            //    unloved_orphans.Remove(child.term);
            //    // A list of 'all voxemes' needs to know if one goes bye bye I guess
            //    objSelector.allVoxemes.Remove(child.obj.transform.GetComponent<Voxeme>());
            //    DestroyImmediate(transform.Find(child.term).gameObject);
            //}


        }

        public void MakeNewClouds() {
            // Reorganize clouds into something that is aesthetic?
            // Account for currently highlighted words, distribute other words by their proximity to those words?
            // Probably need subfunctions to compute ordering at least.
            // This is why this class is getting written later, if ever.

            // Get a list of... strings I guess.

            // +1 for default cloud, for everything that is not selected. Gets last point from locations
            List<Vector3>.Enumerator point_locations = MakePointList(word_lists.Count + 1);
            point_locations.MoveNext();

            Dictionary<string, int> all_words_locations = new Dictionary<string, int>();
            foreach (string word in all_words) {
                all_words_locations[word.ToLower()] = -2;
            }

            for (int i = 0; i < word_lists.Count + 1 && i >= 0; i++) {
                FormWordCloud c;
                Vector3 pos = point_locations.Current + transform.position;
                point_locations.MoveNext();
                if (i == word_lists.Count) { // last one is catchall
                    i = -2;
                }

                try {
                    c = transform.Find(i.ToString()).gameObject.GetComponent<FormWordCloud>();
                }
                catch { // if we don't have that many clouds yet, make a new one.
                    GameObject new_child = Instantiate(prime.gameObject, pos, Quaternion.identity);
                    //GameObject asterisk = new_child.transform.Find("phrase*").gameObject;
                    new_child.transform.SetParent(transform);
                    //new_child.transform.position = transform.position;
                    new_child.name = i.ToString();
                    c = new_child.GetComponent<FormWordCloud>();
                    clouds.Add(c);
                }

                string[] words;

                // Pass current list of orphans to wordcloud so it doesn't remake them, and instead zooms them around
                if ( i != -2) {
                    Debug.LogWarning("number contained in word_lists: " + word_lists.Count + " and i is: " + i);
                    words = word_lists[i];
                    Debug.LogWarning("Genes to display: " + words[0]);
                    foreach(var orphan in unloved_orphans) {
                        Debug.LogWarning(orphan.Key + orphan.Value.obj.name);
                    }
                    foreach (string word in words) {
                        var lowered = word.ToLower();
                        if (unloved_orphans.ContainsKey(lowered)) { // Not the right check???
                            Debug.LogWarning(lowered + " contained");
                            // pass this lucky orphan to be adopted by a loving family!
                            unloved_orphans[lowered].obj.transform.parent.parent.SetParent(c.transform);
                            c.GetPreciousChildren()[lowered] = unloved_orphans[lowered];
                            unloved_orphans.Remove(lowered);
                        }
                        if (all_words_locations.ContainsKey(lowered)) {
                            all_words_locations[lowered] = i;
                        }
                        else {
                            Debug.LogWarning("Word not included in all_words: " + word);
                        }
                    }
                    c.NewSphere(just_words: words);

                }
                else { // catchall for words not slected by a subset. Goes to c of -2.
                    //words = new string[unloved_orphans.Keys.Count];
                    //unloved_orphans = new Dictionary<string, Phrase>();
                    var word_list = new List<string>();
                    //unloved_orphans.Keys.CopyTo(words, 0); // Will be in leftovers.

                    foreach (string word in all_words) {
                        var leftover = word.ToLower();
                        //if (unloved_orphans.ContainsKey(leftover)) {
                        //    // pass this lucky orphan to be adopted by a loving family!
                        //    unloved_orphans[leftover].obj.transform.parent.parent.SetParent(c.transform);
                        //    c.GetPreciousChildren()[leftover] = unloved_orphans[leftover];
                        //    unloved_orphans.Remove(leftover);
                        //}
                        if (all_words_locations[leftover] == -2) {
                            word_list.Add(leftover);
                        }
                    }
                    words = word_list.ToArray();
                    c.NewSphere(just_words: words);
                    unloved_orphans = c.GetPreciousChildren();
                }

                //Debug.LogWarning("Words in " + i + ":" + words.Length);
                // Grab list of new orphans
            }

            // Euthanize all orphans without parents. It's the humane thing to do.
            //foreach (Phrase child in unloved_orphans.Values) {
            //    child.size = new Vector3(0, 0, 0); // Will shrink pretty quick.
            //}
        }

        // Generate the locations for words around the sphere
        // Return an enumerable in case we make an arbitrary-size method in future.
        // (That may allow us to do some sort of spiral-backward thing)
        private List<Vector3>.Enumerator MakePointList(float points) {
            //Debug.LogWarning(phrases.Count);
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
    }
}
