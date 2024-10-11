using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;

public class FileWindow : MonoBehaviour
{
	FilePreset settings;

	public Slider newSizeSlider, newHeightSlider;
	public Dropdown splatDrop, biomeDrop;

	private List<TerrainSplat.Enum> splatEnums = new List<TerrainSplat.Enum>();
	private List<TerrainBiome.Enum> biomeEnums = new List<TerrainBiome.Enum>();

	public void Start()
	{
		settings = SettingsManager.application;

		PopulateLists();
		splatDrop.value = splatEnums.IndexOf(settings.newSplat);
		biomeDrop.value = biomeEnums.IndexOf(settings.newBiome);
		newSizeSlider.value = settings.newSize;
		newHeightSlider.value = settings.newHeight;

		splatDrop.onValueChanged.AddListener(delegate { StateChange(); });
		biomeDrop.onValueChanged.AddListener(delegate { StateChange(); });
		newSizeSlider.onValueChanged.AddListener(delegate { StateChange(); });
		newHeightSlider.onValueChanged.AddListener(delegate { StateChange(); });
	}

	public void PopulateLists()
	{
		foreach (TerrainSplat.Enum splat in Enum.GetValues(typeof(TerrainSplat.Enum)))
		{
			splatEnums.Add(splat);
			splatDrop.options.Add(new Dropdown.OptionData(splat.ToString()));
		}

		foreach (TerrainBiome.Enum biome in Enum.GetValues(typeof(TerrainBiome.Enum)))
		{
			biomeEnums.Add(biome);
			biomeDrop.options.Add(new Dropdown.OptionData(biome.ToString()));
		}
	}

	public void StateChange()
	{
		FilePreset application = SettingsManager.application;
		application.newSplat = splatEnums[splatDrop.value]; 
		application.newBiome = biomeEnums[biomeDrop.value];
		application.newSize = (int)newSizeSlider.value;    
		application.newHeight = newHeightSlider.value;
		SettingsManager.application = application;
	}

	public void OpenFile()
	{
		var world = new WorldSerialization();
		world.Load("f:/newfolder3/Mars3250v2v1.1.1.map");
		MapManager.Load(WorldConverter.WorldToTerrain(world), "f:/newfolder3/Mars3250v2v1.1.1.map");
	}

	public void SaveFile()
	{
		Debug.LogError("browse file");
	}

	public void NewFile()
	{
		MapManager.CreateMap(settings.newSize, settings.newSplat, settings.newBiome, settings.newHeight);
	}
}