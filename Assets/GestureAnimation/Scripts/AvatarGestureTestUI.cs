using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarGestureTestUI : MonoBehaviour {
    public AvatarGestureController gestureController;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void TriggerGestureAnimation(InputField textInput)
    {
        gestureController.PerformGesture(textInput.text);
    }
}
