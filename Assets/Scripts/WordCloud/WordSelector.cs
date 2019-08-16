using UnityEngine;
using TMPro;


// Based on code from Andrew Sage: https://medium.com/@SymboticaAndrew/a-vr-word-cloud-in-unity-f7cb8cf17b6b

    /// <summary>
    /// Uses line-of-sight by default, since originally meant for Oculus Rift.
    /// Probably want to change that.
    /// </summary>
public class WordSelector : MonoBehaviour {
    public Color highlighted = Color.yellow;
    public Color unhighlighted = Color.white;

    private GameObject currentWord;

    Camera main_camera;

    private void Start() {
        main_camera = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update() {
        //transform.position = transform.parent.position;
        // Hmmm, min camera may be wrong to use.
        Ray ray;
        if (main_camera != null) {
            //Debug.LogWarning("Found main camera");
            ray = main_camera.ScreenPointToRay(Input.mousePosition);
        }
        else {
            //Debug.LogWarning("Did not find main camera");
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }
        //Ray ray = new Ray(transform.position, transform.rotation * Vector3.forward);
        RaycastHit hit;
        GameObject hitWord = null;

        // 'Word' prefab needs to have a tag of "Word" as well.
        if (Physics.Raycast(ray, out hit) && (hit.transform.gameObject.tag == "Word")) {
            //Debug.Log("Hit something");
            hitWord = hit.transform.gameObject;
            if (currentWord != hitWord) {
                if (currentWord != null) {
                    // Unhighlight
                    TextMeshPro phraseText = currentWord.transform.GetComponent<TextMeshPro>();
                    phraseText.color = unhighlighted;
                }
                currentWord = hitWord;
                if (currentWord != null) {
                    // Highlight
                    TextMeshPro phraseText = hit.transform.GetComponent<TextMeshPro>();
                    unhighlighted = phraseText.color;
                    phraseText.color = highlighted;
                    
                }
            }
            // An okay place to set the active word. Reorganize words based on it.
            if (Input.GetMouseButtonDown(0))
                Debug.LogWarning("Pressed primary button: " + hit.transform.parent.parent.gameObject.name); // Double parent to get to *actual name*
        }

        else {
            //Debug.Log("No hits");
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 200, Color.yellow);
            if (currentWord != null) {
                // Unhighlight
                TextMeshPro phraseText = currentWord.transform.GetComponent<TextMeshPro>();
                phraseText.color = unhighlighted;
                currentWord = null;
            }
        }
    }
}