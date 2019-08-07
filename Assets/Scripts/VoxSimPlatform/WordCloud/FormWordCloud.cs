using UnityEngine;
using System.Collections.Generic;
//using LitJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using VoxSimPlatform.Vox;


// Based on code from Andrew Sage: https://medium.com/@SymboticaAndrew/a-vr-word-cloud-in-unity-f7cb8cf17b6b

public class Phrase {
    // From tutorial. But the 'occurrences' should be remappable to whatever importance metric we wind up using.
    public string term;
    public float occurrences;
}

public class FormWordCloud : MonoBehaviour {
    public GameObject childObject;
    public float size = 10.0f;
    private List<Phrase> phrases = new List<Phrase>();
    private List<Transform> precious_children = new List<Transform>();
    //private List<Phrase> randomisedPhrases = new List<Phrase>();
    Transform camera1;
    private int bug = 0;

    private float totalOccurances = 0.0f;


    //private string jsonString = "[{\"term\":\"the\", \"occurrences\":504},{\"term\":\"to\",\"occurrences\":447},{\"term\":\"rt\",\"occurrences\":433},{\"term\":\"a\",\"occurrences\":382},{\"term\":\"in\",\"occurrences\":299},{\"term\":\"of\",\"occurrences\":274},{\"term\":\"adventure\",\"occurrences\":236},{\"term\":\"and\",\"occurrences\":216},{\"term\":\"for\",\"occurrences\":166},{\"term\":\"is\",\"occurrences\":157},{\"term\":\"on\",\"occurrences\":154},{\"term\":\"cars\",\"occurrences\":136},{\"term\":\"it\",\"occurrences\":122},{\"term\":\"you\",\"occurrences\":116},{\"term\":\"with\",\"occurrences\":100},{\"term\":\"from\",\"occurrences\":87},{\"term\":\"at\",\"occurrences\":85},{\"term\":\"i\",\"occurrences\":85},{\"term\":\"this\",\"occurrences\":85},{\"term\":\"that\",\"occurrences\":83}]";

    private string jsonString = "{ \"took\": 52, \"timed_out\": false, \"_shards\": { \"total\": 5, \"successful\": 5, \"skipped\": 0, \"failed\": 0 }, \"hits\": { \"total\": 389, \"max_score\": 0, \"hits\": [] }, \"aggregations\": { \"2\": { \"doc_count_error_upper_bound\": 271, \"sum_other_doc_count\": 20928, \"buckets\": [ { \"key\": \"blocky\", \"doc_count\": 389 }, { \"key\": \"spoon\", \"doc_count\": 700 }, { \"key\": \"fork\", \"doc_count\": 600 }, { \"key\": \"base\", \"doc_count\": 388 }, { \"key\": \"given\", \"doc_count\": 387 }, { \"key\": \"graph\", \"doc_count\": 387 }, { \"key\": \"number\", \"doc_count\": 387 }, { \"key\": \"power\", \"doc_count\": 387 }, { \"key\": \"social\", \"doc_count\": 387 }, { \"key\": \"system\", \"doc_count\": 387 }, { \"key\": \"consider\", \"doc_count\": 386 }, { \"key\": \"control\", \"doc_count\": 386 }, { \"key\": \"failure\", \"doc_count\": 500 }, { \"key\": \"figure\", \"doc_count\": 386 }, { \"key\": \"hybrid\", \"doc_count\": 100 }, { \"key\": \"scenario\", \"doc_count\": 386 }, { \"key\": \"smart\", \"doc_count\": 386 }, { \"key\": \"spread\", \"doc_count\": 386 }, { \"key\": \"using\", \"doc_count\": 386 }, { \"key\": \"values\", \"doc_count\": 400 }, { \"key\": \"propose\", \"doc_count\": 385 }, { \"key\": \"degree\", \"doc_count\": 384 }, { \"key\": \"forecast\", \"doc_count\": 200 }, { \"key\": \"algorithm\", \"doc_count\": 382 }, { \"key\": \"generation\", \"doc_count\": 382 }, { \"key\": \"high-order\", \"doc_count\": 382 }, { \"key\": \"hosploc\", \"doc_count\": 382 }, { \"key\": \"markov\", \"doc_count\": 382 }, { \"key\": \"structure\", \"doc_count\": 382 }, { \"key\": \"function\", \"doc_count\": 379 }, { \"key\": \"fraction\", \"doc_count\": 378 }, { \"key\": \"random\", \"doc_count\": 375 }, { \"key\": \"component\", \"doc_count\": 374 }, { \"key\": \"distribution\", \"doc_count\": 374 }, { \"key\": \"provide\", \"doc_count\": 371 }, { \"key\": \"problem\", \"doc_count\": 367 }, { \"key\": \"optimal\", \"doc_count\": 363 }, { \"key\": \"attack\", \"doc_count\": 362 }, { \"key\": \"percolation\", \"doc_count\": 362 }, { \"key\": \"communication\", \"doc_count\": 355 }, { \"key\": \"domain\", \"doc_count\": 355 }, { \"key\": \"represent\", \"doc_count\": 355 }, { \"key\": \"service\", \"doc_count\": 355 }, { \"key\": \"services\", \"doc_count\": 355 }, { \"key\": \"vertex\", \"doc_count\": 355 }, { \"key\": \"result\", \"doc_count\": 354 }, { \"key\": \"probability\", \"doc_count\": 353 }, { \"key\": \"autophagy\", \"doc_count\": 346 }, { \"key\": \"combination\", \"doc_count\": 346 }, { \"key\": \"drug\", \"doc_count\": 346 } ] } }, \"status\": 200 }";

    void Start() {
        Camera main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (main_camera != null) {
            //Debug.LogWarning("Found main camera: " + main_camera.transform.position);

            camera1 = main_camera.transform;
        }
        else {
            //Debug.LogWarning("Missed main camera");

            camera1 = Camera.main.transform;
        }

        ProcessWords(jsonString);
        //Sphere();
        Sphere2();
    }

    // Update is called once per frame. FixedUpdate is 50 times a second regardless of framerate
    void FixedUpdate() {
        Vector3 Point;
        float zDistance;

        // Tell each of the objects to look at the camera
        foreach (Transform child in precious_children) {
            if(child.parent != transform && child.parent.parent != transform) {
                // Someone has stolen my child :o
                if (child.name.EndsWith("*")) {
                    child.parent.SetParent(transform);
                }
                else {
                    child.SetParent(transform);
                }
            }
            Voxeme vx = child.GetComponent<Voxeme>(); // Kinda awkward to do this here.
            vx.is_phrase = true; //Every frame, like taking a sledghammer to a banana
            Quaternion toRotation1  = Quaternion.LookRotation(child.transform.position - camera1.position);
            if (Quaternion.Angle(toRotation1, child.transform.rotation) > 5) {
                float speed = 0.7f;
                child.transform.rotation = Quaternion.Lerp(child.transform.rotation, toRotation1, speed * Time.deltaTime);
            }
        }
    }

    // Diana keeps taking away my children >:(
    // I love my children, and they should always love me
    // Actually, might be easier attached to the phrase lol
    void HelicopterParent() {

    }


    // Keep everything nice and visible. Not implemented.
    private void Jiggle() {

    }


    // More important words to the front now.
    private void Sphere2() {
        float points = phrases.Count;
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

        // Points in order of distance
        point_locations.Sort((x, y) => Vector3.Distance(x, camera1.position).CompareTo(Vector3.Distance(y, camera1.position)));

        // Start in center, spiral out?
        for (float i = 0; i < points; i++) {
            float y = i * offset - 1 + (offset / 2);
            float radius = Mathf.Sqrt(1 - y * y);
            //float radius_scaled = radius * (totalOccurances / phrases[(int)i].occurrences);
            float angle = i * increment;
            Vector3 pos = point_locations[(int)i];
            //Vector3 pos = new Vector3((Mathf.Cos(angle) * radius * size), y * size, Mathf.Sin(angle) * radius * size);

            // Create the object as a child of the sphere
            GameObject child = Instantiate(childObject, pos + transform.position, Quaternion.identity) as GameObject;

            child.transform.SetParent(transform);
            // Child object actually has the text now lol
            //foreach (Component comp in child.Chil()) {
            //    Debug.LogWarning(comp + " " + comp.name);
            //}
            TextMeshPro phraseText = child.transform.Find("phrase_text").GetComponent<TextMeshPro>();

            phraseText.text = phrases[(int)i].term.ToUpper();
            float scale = (phrases[(int)i].occurrences / totalOccurances) * 30;
            child.transform.localScale = new Vector3(scale, scale, scale);
            float scalar = (phrases[(int)i].occurrences / totalOccurances);
            phraseText.fontSize = phraseText.fontSize * (scalar) * 60;

            Vector3 dimensions = phraseText.GetPreferredValues(phraseText.text, 800, Mathf.Infinity);
            BoxCollider bc = child.GetComponent<BoxCollider>(); // To resize based on size of string
            bc.size = dimensions; //Make the hit box about the right size
            precious_children.Add(child.transform);
            child.name = phraseText.text.ToLower();
            if (child.name == "vertex") {
//                Debug.LogWarning(child.transform.position);
//                Debug.LogWarning(pos);
            }
        }
    }


    private void ProcessWords(string jsonString) {
        JObject jsonvale = JObject.Parse(jsonString);
        //Not exactly future-proof here. This is the structure of the current Json returned
        Debug.Log(jsonvale["aggregations"]["2"]["buckets"]);
        JArray second_layer = (JArray)jsonvale["aggregations"]["2"]["buckets"];

        for (int i = 0; i < second_layer.Count; i++) {
            Phrase phrase = new Phrase();
            phrase.term = second_layer[i]["key"].ToString();
            phrase.occurrences = float.Parse(second_layer[i]["doc_count"].ToString());
            phrases.Add(phrase);
            totalOccurances += phrase.occurrences;
        }


        // Sort the list by number of occurrences
        phrases.Sort((x, y) => x.occurrences.CompareTo(y.occurrences));
        phrases.Reverse();

    }
}