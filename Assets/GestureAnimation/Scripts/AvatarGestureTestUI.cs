using UnityEngine;
using UnityEngine.UI;

public class AvatarGestureTestUI : MonoBehaviour {
	public AvatarGestureController gestureController;

	// Use this for initialization
	void Start() {
		// Example of subscribing to gesture event
		gestureController.GestureStart += delegate(object sender, AvatarGesture ag) {
			Debug.Log("Gesture Start: " + ag.Name + " (IsGesturing=" + gestureController.IsGesturing + ")");
		};
	}

	// Update is called once per frame
	void Update() {
	}

	public void TriggerGestureAnimation(InputField textInput) {
		// Example of using callback along with PerformGesture
		gestureController.PerformGesture(textInput.text,
			delegate(AvatarGesture ag) { Debug.Log("Gesture End: " + ag.Name + " (IsGesturing=" + gestureController.IsGesturing + ")"); });
	}
}