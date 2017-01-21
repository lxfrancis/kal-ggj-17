using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panner : MonoBehaviour {

	public PitchTracker pitchTracker;
	public Transform panner;
	public float panSpeed = 1;
	public Transform pitchMover;
	public float heightScale = 3;

	public float bottomLimit = 500;
	public float topLimit = 1000;

	
	// Update is called once per frame
	void Update () {
		
		panner.position += new Vector3(panSpeed * Time.deltaTime, 0, 0);
		float height = (pitchTracker.pitchValue - bottomLimit) / (topLimit - bottomLimit);
		pitchMover.localPosition = new Vector3(0, pitchTracker.singValue * heightScale, 0);
	}
}
