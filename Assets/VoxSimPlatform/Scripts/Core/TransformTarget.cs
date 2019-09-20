using UnityEngine;

using VoxSimPlatform.Global;

namespace VoxSimPlatform {
    namespace Core {  
        public class TransformTarget : MonoBehaviour {
        	public Vector3 targetPosition, targetRotation, targetScale;
        	public float moveSpeed = 1.0f;

        	void Start() {
        	}

        	// Update is called once per frame
        	void Update() {
        		if (transform.position != targetPosition) {
        			Vector3 offset = MoveToward(targetPosition);

        			if (offset.sqrMagnitude <= Constants.EPSILON) {
        				transform.position = targetPosition;
        			}
        		}
        	}

        	Vector3 MoveToward(Vector3 target) {
        		Vector3 offset = transform.position - target;
        		Vector3 normalizedOffset = Vector3.Normalize(offset);

        		transform.position = new Vector3(transform.position.x - normalizedOffset.x * Time.deltaTime * moveSpeed,
        			transform.position.y - normalizedOffset.y * Time.deltaTime * moveSpeed,
        			transform.position.z - normalizedOffset.z * Time.deltaTime * moveSpeed);

        		return offset;
        	}
        }
    }
}