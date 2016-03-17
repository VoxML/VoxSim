﻿using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GhostFreeRoamCamera : MonoBehaviour
{
	public float initialSpeed = 10f;
	public float increaseSpeed = 1.25f;
	
	public bool allowMovement = true;
	public bool allowRotation = true;
	
	public KeyCode forwardButton = KeyCode.W;
	public KeyCode backwardButton = KeyCode.S;
	public KeyCode rightButton = KeyCode.D;
	public KeyCode leftButton = KeyCode.A;
	
	public float cursorSensitivity = 0.025f;
	public bool cursorToggleAllowed = true;
	public KeyCode cursorToggleButton = KeyCode.Escape;
	
	private float currentSpeed = 0f;
	private bool moving = false;
	private bool togglePressed = false;
	
	private Rigidbody rb;
	private Vector3 deltaPosition;

	private float angle = 0;

	private void OnEnable()
	{
		if (cursorToggleAllowed)
		{
			Screen.lockCursor = true;
			Cursor.visible = false;
		}
	}
	
	private void FixedUpdate()
	{
		if (allowMovement)
		{
			bool lastMoving = moving;
			deltaPosition = Vector3.zero;
			
			if (moving)
				currentSpeed += increaseSpeed * Time.deltaTime;
			
			moving = false;
			
			CheckMove(forwardButton, transform.forward);
			CheckMove(backwardButton, -transform.forward);
			CheckMove(rightButton, transform.right);
			CheckMove(leftButton, -transform.right);
			
			if (moving)
			{
				if (moving != lastMoving)
					currentSpeed = initialSpeed;
				
				transform.position += deltaPosition * currentSpeed * Time.deltaTime;
			}
			else currentSpeed = 0f;            
		}
		
		if (allowRotation)
		{
			Vector3 eulerAngles = transform.eulerAngles;
			eulerAngles.x += -Input.GetAxis("Mouse Y") * 359f * cursorSensitivity;
			eulerAngles.y += Input.GetAxis("Mouse X") * 359f * cursorSensitivity;
			transform.eulerAngles = eulerAngles;
		}
		
		if (cursorToggleAllowed)
		{
			if (Input.GetKey(cursorToggleButton))
			{
				if (!togglePressed)
				{
					togglePressed = true;
					Screen.lockCursor = !Screen.lockCursor;
					Cursor.visible = !Cursor.visible;
				}
			}
			else togglePressed = false;
		}
		else
		{
			togglePressed = false;
			Cursor.visible = false;
		}
		
	}

	private void Start() {	
		rb = GetComponent<Rigidbody> ();
	}

	private void CheckMove(KeyCode keyCode, Vector3 directionVector)
	{
		if (Input.GetKey(keyCode))
		{
			moving = true;
			deltaPosition += directionVector;
		}
	}
	void OnCollisionEnter(Collision other) {
		if (other.gameObject.tag != "Boundary") {
////			currentSpeed = 0;
//			Debug.Log(other.transform.name);
//			Vector3 dir = other.transform.position - transform.position;
//			dir = other.transform.InverseTransformDirection(dir);
////			float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//			Debug.Log(dir);
//			Debug.Log (deltaPosition);
//			deltaPosition -= dir;
//			transform.position += deltaPosition * currentSpeed * Time.deltaTime;
			Physics.IgnoreCollision (GetComponent<Collider> (), other.gameObject.GetComponent<Collider> ());
//			transform.position -= dir;
//			transform.position = deltaPosition * currentSpeed * Mathf.Sin ((Vector3.Angle(deltaPosition, other.transform.local)));
//			if (transform.position.x == other.transform.position.x) {
//				Debug.Log("1");
//			}
//			if (transform.position.y == other.transform.position.y) {
//				Debug.Log ("2");
//			}
//			if (transform.position.z == other.transform.position.z) {
//				Debug.Log("3");
//			}
//			transform.position -= deltaPosition * currentSpeed * Time.deltaTime;
		}
	}
	
	void OnCollisionStay(Collision other) {
		if (other.gameObject.tag != "Boundary") {
//			currentSpeed = 0;
			Physics.IgnoreCollision(GetComponent<Collider>(), other.gameObject.GetComponent<Collider>());
//			if (transform.position.x == other.transform.position.x) {
//				Debug.Log("x " + other.name);
//			}
//			if (transform.position.y == other.transform.position.y) {
//				Debug.Log ("y " + other.name);
//			}
//			if (transform.position.z == other.transform.position.z) {
//				Debug.Log("z " + other.name);
//			}
//			transform.position -= deltaPosition * currentSpeed * Time.deltaTime;
//		}
//		rb.AddForce(other.transform.up);
//		Debug.Log(other.gameObject.name);
//			Vector3 dir = other.transform.position - transform.position;
//			dir = other.transform.InverseTransformDirection(dir);
//			float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
//			Debug.Log(angle);
		}

	}


// NOTE:* Set the Euler angles above to define the relative orientation of the capsule (it depends on the cube axes).
}
