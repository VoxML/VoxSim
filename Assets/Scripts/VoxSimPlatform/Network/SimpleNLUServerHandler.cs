using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

// This class connects to a server in the outside world
// Then it passes strings (presumably utterances from the user)
// And returns a JSON of the parse.
// Needs to be a Monobehavior in order to be an object in the scene
// KNOWN BUG: Cannot properly wait for the reply. This is because Unity disapproves
// of pausing the main thread, which would cause stuttering in a normal game
// This is pretty serious, but looks hard to fix without rehashing whole program structure into coroutines

public class NLUServerHandler : MonoBehaviour {
    // Use this for initialization
    UnityWebRequest www = null;
    string last_read = ""; //
    string input_string = ""; //eg "put the plate on the knife";
    string url = "";//https://voxsim-practice.herokuapp.com/nltk as specified in preferences
    
    void Update() {
        if (input_string != ""){
            StartCoroutine(PostText());
        }
    }

    /// <summary>
    /// Takes the input string (if it exists) and packs it up to send to the server
    /// </summary>
    IEnumerator PostText() {
        if (input_string != "") {
            var form = new WWWForm();
            form.AddField("sentence", input_string);
            input_string = "";
            www = UnityWebRequest.Post(url, form);
            www.timeout = 3; // Seconds to wait for a response. 3 seems reasonable.
            //last_read = www.downloadHandler.text; // Saved to return later.
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
                Debug.Log(www.error);
            }
            else {
                // Show results as text            
                if (www.downloadHandler.text != "") {
                    last_read = www.downloadHandler.text;
                    //Debug.LogWarning("RESULTS AS TEXT: " + last_read);
                }
            }
        }
    }


    public void set_post(string input) {
        input_string = input;
    }

    public UnityWebRequest get_www() {
        return www;
    }

    public void set_url(string in_url) {
        url = in_url;
    }

    public string get_last_read() {
        if(www == null) {
            return "{}"; // Can't just loop because coroutines != threads. Unity would never halt.
        }
        //while (!www.isDone && !www.isNetworkError) {
        //    //Just wait here. Doesn't seem to quite do it.
        //}
        if (!www.isNetworkError) {
            www = null;
            return last_read;
        }
        else {
            Debug.LogWarning("Server did not respond fast enough");
            www = null;
            return ("{}");
        }
    }
}