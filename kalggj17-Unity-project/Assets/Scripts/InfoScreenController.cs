using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoScreenController : MonoBehaviour {

	[SerializeField]
	float opacity;
	public MeshRenderer mr;


	// Update is called once per frame
	void Update () {
		
		if(RippleController.instance.GetRippleCount() < 1)
		{
			opacity = Mathf.MoveTowards(opacity, 1, Time.deltaTime * 1f);
		}
		else
		{
			opacity = Mathf.MoveTowards(opacity, 0, Time.deltaTime * 1f);
		}

		if(opacity == 0)
		{
			mr.enabled = false;
		}
		else
		{
			mr.enabled = true;
			mr.material.SetColor("_TintColor", new Color(1,1,1, Mathf.Clamp(opacity,0f,1))/2);

		}

	}
}
