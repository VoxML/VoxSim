using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//NOTE: Not used. There was a tag in some of the core code that allows camera movement when unchecked.
// That's in Ghost Free Roam.

public class Movement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            Vector3 position = this.transform.position;
            position.x++;
            this.transform.position = position;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            Vector3 position = this.transform.position;
            position.x--;
            this.transform.position = position;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            Vector3 position = this.transform.position;
            position.z--;
            this.transform.position = position;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            Vector3 position = this.transform.position;
            position.z++;
            this.transform.position = position;
        }
        this.transform.LookAt(GameObject.Find("WordCloud").transform);//.position - this.transform.position);
    }
}
