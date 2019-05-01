using UnityEngine;
using System;
using System.Collections;
using System.Timers;

using Object = UnityEngine.Object;
using SimpleJSON;
using VoxSimPlatform.Core;
using VoxSimPlatform.Global;

public class SRIInterpreter : MonoBehaviour {
	string url;
	//http://www.voxicon.net/cwc/block-state-log.json
	//http://192.168.1.2:1236/world-api/block-state.json

	Vector2 voxTableSize = new Vector2(2.916323f, 2.916323f);
	Vector2 sriTableSize = new Vector2(1.7f, 1.7f);
	float yOffset = 0;

	Timer pollTimer;
	float pollInterval = 1000;
	bool poll = false;

	void Start() {
		url = PlayerPrefs.GetString("SRI URL");

		GameObject bc = GameObject.Find("BehaviorController");
		GameObject table = GameObject.Find("square_table");
		Predicates preds = bc.GetComponent<Predicates>();

		yOffset = preds.TOP(new object[] {table}).y;

		// Create poll timer
		// Create a timer
		pollTimer = new Timer();
		// Tell the timer what to do when it elapses
		pollTimer.Elapsed += new ElapsedEventHandler(PollApparatus);
		// Set it to go off every second
		pollTimer.Interval = pollInterval;
		// And start it        
		pollTimer.Enabled = true;
	}

	void ClearBlocks() {
		GameObject[] blocks = GameObject.FindGameObjectsWithTag("Block");

		for (int i = 0; i < blocks.Length; i++) {
			Destroy(blocks[i]);
		}
	}

	IEnumerator GetApparatusData() {
		using (WWW www = new WWW(url)) {
			yield return www;

			string content = www.text;

			JSONNode blockStates = JSONNode.Parse(content);

			if (blockStates != null) {
				for (int i = 0; i < blockStates["BlockStates"].Count; i++) {
					float temp;

					GameObject block = InstantiateObject("block");
					block.tag = "Block";
					//GameObject block = GameObject.Find("block"+blockStates["BlockStates"][i]["ID"]);

					Vector3 targetPosition =
						Helper.ParsableToVector(
							((String) blockStates["BlockStates"][i]["Position"]).Replace(",", ";"));
					temp = targetPosition.y;
					targetPosition.y = targetPosition.z;
					targetPosition.z = temp;

					targetPosition.x *= voxTableSize.x / sriTableSize.x;
					targetPosition.y += yOffset;
					targetPosition.z *= voxTableSize.y / sriTableSize.y;

					Quaternion targetRotation =
						Helper.ParsableToQuaternion(
							((String) blockStates["BlockStates"][i]["Rotation"]).Replace(",", ";"));
					//temp = targetRotation.y;
					//targetRotation.y = targetRotation.z;
					//targetRotation.z = temp;

					block.transform.position = targetPosition;
					block.transform.rotation = targetRotation;
					//block.transform.rotation = targetRotation;
					block.transform.Rotate(Vector3.right, 90, Space.World);
					block.transform.localScale = new Vector3(0.152439f, 0.152439f, 0.152439f);
					//block.GetComponent<Entity>().targetPosition = targetPosition;
					//block.GetComponent<Entity>().targetRotation = targetRotation;
				}
			}
		}
	}

	public GameObject InstantiateObject(string objName) {
		Debug.Log("Instantiate " + objName);

		Object prefab = Resources.Load(objName);
		GameObject go = (GameObject) Instantiate(prefab);
		go.transform.position = Vector3.zero;
		go.SetActive(true);
		go.name = go.name.Replace("(Clone)", "");

		return go;
	}

	// Update is called once per frame
	void Update() {
		if (poll) {
			ClearBlocks();
			StartCoroutine("GetApparatusData");
			poll = false;
		}
	}

	// Implement a call with the right signature for events going off
	private void PollApparatus(object source, ElapsedEventArgs e) {
		poll = true;

		// Reset timer
		pollTimer.Interval = pollInterval;
		pollTimer.Enabled = true;
	}

	void OnGUI() {
		if (GUI.Button(new Rect(10, Screen.height - 55, 100, 20), "Refresh")) {
			ClearBlocks();
			StartCoroutine("GetApparatusData");
		}
	}
}