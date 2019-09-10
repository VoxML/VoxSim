using UnityEngine;
using System.Collections;
using TMPro;


namespace WordCloud {


    // To be attached to points of interest. Such as the concept of "forward"
    // which is a literal physical place so long as I'm writing things without touching the actual language model
    public class HighlightPoint : MonoBehaviour {
        private GameObject currentWord;


        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

            Ray ray;
            RaycastHit hit;
            GameObject hitWord = null;
            ray = new Ray(transform.position, transform.up);


            // 'Word' prefab needs to have a tag of "Word" as well.
            if (Physics.Raycast(ray, out hit) && (hit.transform.gameObject.tag == "Word")) {
                //Debug.Log("Hit something");
                hitWord = hit.transform.gameObject;
                GameObject wordcloud = hitWord;
                while (wordcloud != null && wordcloud.GetComponent<FormWordCloud>() == null) {
                    wordcloud = wordcloud.transform.parent.gameObject;
                }
                if (currentWord != hitWord) {
                    currentWord = hitWord;
                    TextMeshPro phraseText = currentWord.transform.GetComponentInChildren<TextMeshPro>();
                    string term = phraseText.text.ToLower();
                    Debug.LogWarning(term + " is above " + gameObject.name);
                    if (wordcloud != null) {
                        wordcloud.GetComponent<FormWordCloud>().HighlightWord(term);
                    }

                    else {
                        Debug.LogWarning("Phrase with no wordcloud parent (synchronization issue?)");
                    }
                }
            }
            else {
                currentWord = null;
            }


        }
    }
}
