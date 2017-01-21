using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

	public Transform cameraTarget;
	public float rotateRate;

	Vector3 prevMousePosition;


	// Update is called once per frame
	void Update () {

		// drag sideways to rotate camera around the vertical axis


		if(Input.GetMouseButton(0))
		{
			Vector3 mouseDelta = (Input.mousePosition - prevMousePosition) * rotateRate * Time.deltaTime;
			cameraTarget.RotateAround(cameraTarget.position, Vector3.up, mouseDelta.x);
			cameraTarget.RotateAround(cameraTarget.position, Camera.main.transform.right, -mouseDelta.y);
		}

		prevMousePosition = Input.mousePosition;


		// frag up/down to tilt camera up/down

		
	}
}
