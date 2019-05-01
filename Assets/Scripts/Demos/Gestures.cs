using UnityEngine;

public class Gestures : MonoBehaviour {
	public enum HandPose {
		Neutral,
		LeftClaw,
		LeftPoint,
		LeftThumbsUp,
		RightClaw,
		RightPoint,
		RightThumbsUp
	}

	GameObject leftReachObj, rightReachObj, lookObj;

	// Use this for initialization
	void Start() {
		leftReachObj = GameObject.Find("LeftReachObject");
		rightReachObj = GameObject.Find("RightReachObject");
		lookObj = GameObject.Find("LookObject");
	}

	// Update is called once per frame
	void Update() {
		if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) &&
		    (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
			// shift-click, move LookObject
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			Physics.Raycast(ray, out hitInfo);
			if (hitInfo.collider.gameObject.name == "CollisionPane") {
				lookObj.transform.position = hitInfo.point;
			}
		}
		else if (Input.GetMouseButton(0)) {
			// left-click, move LeftReachObject
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			Physics.Raycast(ray, out hitInfo);
			if (hitInfo.collider.gameObject.name == "CollisionPane") {
				leftReachObj.transform.position = hitInfo.point;
			}
		}
		else if (Input.GetMouseButton(1)) {
			// right-click, move RightReachObject
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			Physics.Raycast(ray, out hitInfo);
			if (hitInfo.collider.gameObject.name == "CollisionPane") {
				rightReachObj.transform.position = hitInfo.point;
			}
		}
	}
}