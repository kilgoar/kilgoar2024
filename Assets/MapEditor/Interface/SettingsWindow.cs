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
	
	public Slider prefabRender, pathsRender, waterTransparency;
	public InputField directoryField;
	public Toggle styleToggle, assetLoadToggle;
	
	void Start(){
		settings = SettingsManager.application;
		
		prefabRender.value = settings.prefabRenderDistance;
		pathsRender.value = settings.pathRenderDistance;
		waterTransparency.value = settings.waterTransparency;
		
		directoryField.text = settings.rustDirectory;
		
		assetLoadToggle.isOn = settings.loadbundleonlaunch;
		styleToggle.isOn = settings.terrainTextureSet;
		
		prefabRender.onValueChanged.AddListener(delegate { StateChange(); });
        pathsRender.onValueChanged.AddListener(delegate { StateChange(); });
        waterTransparency.onValueChanged.AddListener(delegate { StateChange(); });
        directoryField.onValueChanged.AddListener(delegate { StateChange(); });
        assetLoadToggle.onValueChanged.AddListener(delegate { StateChange(); });
        
		styleToggle.onValueChanged.AddListener(delegate { StateChange(); });
			styleToggle.onValueChanged.AddListener(delegate { ToggleStyle(); });
	}
	
	void Browse(){
		Debug.LogError("browse");
	}
	
	void StateChange(){
		settings.prefabRenderDistance = prefabRender.value;
        settings.pathRenderDistance = pathsRender.value;
        settings.waterTransparency = waterTransparency.value;
        settings.rustDirectory = directoryField.text;
        settings.loadbundleonlaunch = assetLoadToggle.isOn;
        settings.terrainTextureSet = styleToggle.isOn;
		SettingsManager.application = settings;
	}
	
	void ToggleStyle(){
		TerrainManager.SetTerrainLayers();
	}
}
