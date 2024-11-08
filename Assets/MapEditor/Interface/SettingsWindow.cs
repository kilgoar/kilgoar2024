using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;

public class SettingsWindow : MonoBehaviour
{
    FilePreset settings;
	
	public Slider pathsRender;
	public InputField directoryField;
	public Toggle styleToggle, assetLoadToggle;
	
	void Start(){
		settings = SettingsManager.application;
		
		pathsRender.value = settings.pathRenderDistance;
		
		directoryField.text = settings.rustDirectory;
		
		assetLoadToggle.isOn = settings.loadbundleonlaunch;
		styleToggle.isOn = settings.terrainTextureSet;
		
        pathsRender.onValueChanged.AddListener(delegate { CameraChange(); });
        directoryField.onValueChanged.AddListener(delegate { DirectoryChange(); });
        assetLoadToggle.onValueChanged.AddListener(delegate { AssetLoader(); });
        
		styleToggle.onValueChanged.AddListener(delegate { StyleChange(); });
		styleToggle.onValueChanged.AddListener(delegate { ToggleStyle(); });
	}
	
	void AssetLoader(){
		settings.loadbundleonlaunch = assetLoadToggle.isOn;
	}
	
	void Browse(){
		Debug.LogError("browse");
	}
	
	
	void StyleChange(){
			settings.terrainTextureSet = styleToggle.isOn;
			TerrainManager.SetTerrainReferences();
			TerrainManager.SetTerrainLayers();
	}
	
	void CameraChange(){		
		CameraManager cameraManager = FindObjectOfType<CameraManager>();
		settings.pathRenderDistance = pathsRender.value;
		
		if (cameraManager != null)		{
			cameraManager.Configure();
		}
	}
	
	
	void DirectoryChange(){
        settings.rustDirectory = directoryField.text;              
		SettingsManager.application = settings;		
		
	}
	
	void ToggleStyle(){
		TerrainManager.SetTerrainLayers();
	}
}
