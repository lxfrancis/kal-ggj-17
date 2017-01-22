using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour {

	public WorldConfig[] worldConfigs;
	public int currentConfig = 0;

	public MeshRenderer terrainMR;
	Material terrainMat;
	public Light dirLight;

	void Start()
	{
		terrainMat = terrainMR.material;
		UpdateConfig();
	}

	
	// Update is called once per frame
	void Update () {

		int prevConfig = currentConfig;

		if(Input.GetKeyDown(KeyCode.Space))
		{
			currentConfig ++;
			if(currentConfig >= worldConfigs.Length)
			{
				currentConfig = 0;
			}
		}

		if(currentConfig != prevConfig)
		{
			UpdateConfig();
		}
		
	}

	void UpdateConfig()
	{
		WorldConfig conf = worldConfigs[currentConfig];
		terrainMat.SetTexture("_MainTex", conf.tex);
		terrainMat.SetColor("_FogColor", conf.fogColor);
		terrainMat.SetColor("_AmbientColor", conf.ambientColor);
		dirLight.color = conf.dirLightColor;
		Camera.main.backgroundColor = conf.skyColor;
	}
}
