using UnityEngine;
using System.Collections;
using WordCloud;


public class CloudRotator : MonoBehaviour {
    GameObject wordcloud;

    // Use this for initialization
    void Start() {
        wordcloud = GameObject.Find("WordCloud");
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown("w")) {
            //Debug.LogWarning("Pressed primary button: " + hit.transform.parent.parent.gameObject.name); // Double parent to get to *actual name*
                                                                                                        // Kinda wordy way to get to the word itself, there's probably a faster way.
            //wordcloud.GetComponent<FormWordCloud>().HighlightWord(hit.transform.parent.parent.gameObject.name.ToLower());
            //transform.Rotate(Vector3.up * speed * Time.deltaTime);

            wordcloud.transform.Rotate(Vector3.up * 1 * Time.deltaTime);

        }
    }
}
