using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateWithMe : MonoBehaviour {
	public enum Axis
	{
		X=0, Y, Z
	}

	public GameObject source;
	public Axis rotateAround = Axis.Y;

	private Vector3 sourcePosition;
	private Vector3 startPosition;

	private Vector3 startEulerRotation;
	private Vector3 startOrientation;
	private Vector2 flatStart;

	// Use this for initialization
	void Start () {
		startPosition = transform.position;
		sourcePosition = source.transform.position;

		startEulerRotation = transform.rotation.eulerAngles;
		startOrientation = (startPosition - sourcePosition).normalized;

		flatStart = ReduceDimensions (startOrientation);
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 currentOrientation = (transform.position - sourcePosition).normalized;
		Vector2 flatCurrent = ReduceDimensions (currentOrientation);

		// Calculate arc from start to current
		float angle = Vector2.Angle(flatStart, flatCurrent);

		// Calculate desired angle
		Vector3 currentRotation = transform.rotation.eulerAngles;

		switch (rotateAround) {
		case Axis.X:
			currentRotation.x = startEulerRotation.x - angle;
			break;
		case Axis.Y:
		default:
			currentRotation.y = startEulerRotation.y - angle;
			break;
		case Axis.Z:
			currentRotation.z = startEulerRotation.z - angle;
			break;
		}

		// Apply to transform
		transform.rotation = Quaternion.Euler(currentRotation);
	}

	Vector2 ReduceDimensions(Vector3 input) {
		Vector2 output = new Vector2 ();

		switch (rotateAround) {
		case Axis.X:
			output.x = input.y;
			output.y = input.z;
			break;
		case Axis.Y:
		default:
			output.x = input.x;
			output.y = input.z;
			break;
		case Axis.Z:
			output.x = input.x;
			output.y = input.z;
			break;
		}

		return output;
	}
}
