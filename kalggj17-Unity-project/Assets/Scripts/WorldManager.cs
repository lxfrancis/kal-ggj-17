using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour {


	public WorldConfig[] worldConfigs;
	public int currentConfig = 0;

	public MeshRenderer terrainMR;
	public MeshRenderer skyMR;
	public MeshRenderer waterMR;
	Material terrainMat;
	Material skyMat;
	Material waterMat;
	public Light dirLight;
	public AudioSource musicAudioSource;


	void Start()
	{
		terrainMat = terrainMR.material;
		skyMat = skyMR.material;
		waterMat = waterMR.material;
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

		if(Input.GetKeyUp(KeyCode.M))
		{
			musicAudioSource.enabled = !musicAudioSource.enabled;
		}
		
	}

	void UpdateConfig()
	{
		WorldConfig conf = worldConfigs[currentConfig];
		terrainMat.SetTexture("_MainTex", conf.tex);
		terrainMat.SetColor("_FogColor", conf.fogColor);
		terrainMat.SetColor("_AmbientColor", conf.ambientColor);
		dirLight.color = conf.dirLightColor;
		skyMat.SetColor("_TopColor", conf.skyColor);
		skyMat.SetColor("_BottomColor", conf.skyColor2);
		waterMR.enabled = conf.useWater;
		waterMat.SetColor("_TintColor", conf.waterColor);
		//waterMat.SetColor("_FogColor", conf.fogColor);

		EntityController entityController = GameObject.FindObjectOfType<EntityController>();
		entityController.spawnTrees = conf.useTrees;

	}
}
