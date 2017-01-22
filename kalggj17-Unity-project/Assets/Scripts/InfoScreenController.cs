using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoScreenController : MonoBehaviour {

	[SerializeField]
	float opacity;
	public MeshRenderer mr;


	// Update is called once per frame
	void Update () {

		if(Time.time > RippleController.instance.lastInputTime + 1)
		{
			opacity = Mathf.MoveTowards(opacity, 1, Time.deltaTime * 0.1f);
		}
		else
		{
			opacity = Mathf.MoveTowards(opacity, -0.5f, Time.deltaTime * 0.25f);
		}

		mr.material.SetColor("_TintColor", new Color(1,1,1, Mathf.Clamp(opacity,0f,1f)));
	}
}
