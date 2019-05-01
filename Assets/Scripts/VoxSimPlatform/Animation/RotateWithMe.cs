using UnityEngine;

namespace VoxSimPlatform {
    namespace Animation {
        public class RotateWithMe : MonoBehaviour {
        	public enum Axis {
        		X = 0,
        		Y,
        		Z
        	}

        	public GameObject source;
        	public Axis rotateAround = Axis.Y;

        	private Vector3 sourcePosition;
        	private Vector3 startPosition;

        	private Vector3 startEulerRotation;
        	private Vector3 startOrientation;
        	private Vector2 flatStart;

        	public bool debug;

        	// Use this for initialization
        	void Start() {
        		startPosition = transform.position;
        		sourcePosition = source.transform.position;

        		startEulerRotation = transform.rotation.eulerAngles;
        		startOrientation = (startPosition - sourcePosition).normalized;

        		flatStart = ReduceDimensions(startOrientation);
        	}

        	// Update is called once per frame
        	void Update() {
        		Vector3 currentOrientation = (transform.position - sourcePosition).normalized;
        		Vector2 flatCurrent = ReduceDimensions(currentOrientation);

        		// Calculate angle from start to current
        		float angle = GetAngleBetween(flatStart, flatCurrent);

        		// Calculate desired rotation
        		Vector3 currentRotation = transform.rotation.eulerAngles;

        		switch (rotateAround) {
        			case Axis.X:
        				currentRotation.x = startEulerRotation.x + angle;
        				break;
        			case Axis.Y:
        			default:
        				currentRotation.y = startEulerRotation.y + angle;
        				break;
        			case Axis.Z:
        				currentRotation.z = startEulerRotation.z + angle;
        				break;
        		}

        		// Apply to transform
        		transform.rotation = Quaternion.Euler(currentRotation);
        	}

        	public static float GetAngleBetween(Vector2 from, Vector2 to) {
        		float angle = (Mathf.Atan2(from.y, from.x) - Mathf.Atan2(to.y, to.x)) * Mathf.Rad2Deg;

        		// Constrain within range -180 to 180
        		if (angle > 180) {
        			angle -= 360;
        		}
        		else if (angle < -180) {
        			angle += 360;
        		}

        		return angle;
        	}

        	Vector2 ReduceDimensions(Vector3 input) {
        		Vector2 output = new Vector2();

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
    }
}