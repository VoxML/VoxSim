using System.Collections.Generic;
using System.Globalization;
using RootMotion.FinalIK;
using UnityEngine;
using UnityEngine.UI;

public class AvatarGestureAutomatedTest : MonoBehaviour {
	public AvatarGestureController gestureController;
	public LookAtIK lookAtIk;
	public Text textDisplay;

	private Queue<AvatarGesture> allGestures;

	// Use this for initialization
	void Start() {
		allGestures = new Queue<AvatarGesture>();

		foreach (AvatarGesture gesture in AvatarGesture.AllGestures.Values) {
			if (gesture.Name.ToLower().Contains("idle")) continue;
			//if (!gesture.Name.ToLower().Contains("head")) continue;
			allGestures.Enqueue(gesture);
		}

		gestureController.GestureStart += GestureController_GestureStart;
		gestureController.GestureEnd += GestureController_GestureEnd;

		Debug.Log("Starting sequential playback of all gestures...");
		Invoke("PlayNextGesture", 2);
	}

	private void GestureController_GestureStart(object sender, AvatarGesture gesture) {
	}

	private void GestureController_GestureEnd(object sender, AvatarGesture gesture) {
		Invoke("PlayNextGesture", 1);
	}

	private void PlayNextGesture() {
		if (allGestures.Count > 0) {
			AvatarGesture gesture = allGestures.Dequeue();
			Debug.Log("Playing gesture: " + gesture.Name);

			// Make pretty display name
			string gestureName = gesture.Name.ToLower();
			gestureName = gestureName.Substring(gestureName.IndexOf('_') + 1);
			gestureName = gestureName.Replace('_', ' ');
			gestureName = new CultureInfo("en-US", false).TextInfo.ToTitleCase(gestureName);
			textDisplay.text = gestureName;

			// Check if Head IK needs to be disabled
			if (gesture.Name.ToLower().Contains("head")) {
				lookAtIk.solver.headWeight = 0.7f;
			}
			else {
				lookAtIk.solver.headWeight = 1;
			}

			// Perform the gesture
			gestureController.PerformGesture(gesture);
		}
		else {
			Debug.Log("Finished playing all gestures!");
		}
	}

	// Update is called once per frame
	void Update() {
	}
}