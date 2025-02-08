using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;
using System.IO;
using RustMapEditor.Variables;
using System.Linq;

public class GeologyWindow : MonoBehaviour
{
	public TogglePanels tabs;
	public UIRecycleTree tree;
	public Toggle placement;
	public Text footer, item;
	public Slider xRot,yRot,zRot, xRotH, yRotH, zRotH, xScale, yScale, zScale, xScaleH, yScaleH, zScaleH, xJitter, yJitter, zJitter, xJitterH, yJitterH, zJitterH, height, heightH, slope, slopeH,curve, curveH, frequency;
	public Toggle flip, geofield, snapX, snapY, snapZ, preview, curveRange, slopeRange, heightRange, rayTest;
	
	public Dropdown collisionLayer;
	private ColliderLayer colliderLayer;
	
	public HierarchyWindow hierarchyWindow;

	public Button addGeologyItem;
	public Toggle minMax;
	public InputField distance;
		
	public Button removeColliderTemplate, addCollisionItem;
	public Text ColliderLayerTemplate;
	public Toggle minMaxTemplate;
	public InputField distanceTemplate;
	
	public Toggle slopeToggle, heightToggle, curveToggle, arid, temperate, arctic, tundra;
	
	public Button removePresetTemplate;
	public Text presetLabelTemplate;
	
	public Button AddPreset, ApplyPresetList, ApplyGeology, SaveList, SavePreset, LoadPreset, DeletePreset;
	public InputField macroField, PresetField;
	public Text PresetLabel;
	public Dropdown MacroDropDown;	
	
	public GameObject itemCollisionTemplate, presetListTemplate;
	public GameObject content, presetContent;
	
	public List<Toggle> TopologyToggles;
	public Toggle everything;
	
	public Button StopGeneration, include, exclude;
	
	public Image progress, frame;
	
	public static GeologyWindow Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)        
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else        
        {
            Destroy(gameObject);
        }
    }
    
	
	
	private void PopulatePresetTree(){
        string path = SettingsManager.AppDataPath() + "Presets/Geology";
		List<string> pathsList = SettingsManager.GetDataPaths(path, "Geology");	
		SettingsManager.ConvertPathsToNodes(tree, pathsList, ".json");
		tree.Rebuild();
	}
	
    void Start()    {
		
		PopulatePresetTree();
		PopulateSettings();			
		OnPresetListChanged(MacroDropDown.value);
		colliderLayer = ColliderLayer.Prefabs;
		
		PresetField.onValueChanged.AddListener(_ => SendSettings());
		macroField.onValueChanged.AddListener(_ => OnMacroFieldChanged());
		
		tree.onNodeSelected.AddListener(OnSelect);
		addCollisionItem.onClick.AddListener(AddCollisionItem); 
		
		xRot.onValueChanged.AddListener(_ => SendSettings());
		yRot.onValueChanged.AddListener(_ => SendSettings());
		zRot.onValueChanged.AddListener(_ => SendSettings());
		xRotH.onValueChanged.AddListener(_ => SendSettings());
		yRotH.onValueChanged.AddListener(_ => SendSettings());
		zRotH.onValueChanged.AddListener(_ => SendSettings());

		xScale.onValueChanged.AddListener(_ => SendSettings());
		yScale.onValueChanged.AddListener(_ => SendSettings());
		zScale.onValueChanged.AddListener(_ => SendSettings());
		xScaleH.onValueChanged.AddListener(_ => SendSettings());
		yScaleH.onValueChanged.AddListener(_ => SendSettings());
		zScaleH.onValueChanged.AddListener(_ => SendSettings());

		xJitter.onValueChanged.AddListener(_ => SendSettings());
		yJitter.onValueChanged.AddListener(_ => SendSettings());
		zJitter.onValueChanged.AddListener(_ => SendSettings());
		xJitterH.onValueChanged.AddListener(_ => SendSettings());
		yJitterH.onValueChanged.AddListener(_ => SendSettings());
		zJitterH.onValueChanged.AddListener(_ => SendSettings());
		
		frequency.onValueChanged.AddListener(_ => SendSettingsPreview());

		height.onValueChanged.AddListener(_ => SendSettingsPreview());
		heightH.onValueChanged.AddListener(_ => SendSettingsPreview());
		slope.onValueChanged.AddListener(_ => SendSettingsPreview());
		slopeH.onValueChanged.AddListener(_ => SendSettingsPreview());
		curve.onValueChanged.AddListener(_ => SendSettingsPreview());
		curveH.onValueChanged.AddListener(_ => SendSettingsPreview());

		flip.onValueChanged.AddListener(_ => SendSettings());
		geofield.onValueChanged.AddListener(_ => SendSettings());
		snapX.onValueChanged.AddListener(_ => SendSettings());
		snapY.onValueChanged.AddListener(_ => SendSettings());
		snapZ.onValueChanged.AddListener(_ => SendSettings());
		
		curveToggle.onValueChanged.AddListener(_ => SendSettingsPreview());
		heightToggle.onValueChanged.AddListener(_ => SendSettingsPreview());
		slopeToggle.onValueChanged.AddListener(_ => SendSettingsPreview());
		rayTest.onValueChanged.AddListener(_ => SendSettings());

		arid.onValueChanged.AddListener(_ => SendSettingsPreview());
		temperate.onValueChanged.AddListener(_ => SendSettingsPreview());
		arctic.onValueChanged.AddListener(_ => SendSettingsPreview());
		tundra.onValueChanged.AddListener(_ => SendSettingsPreview());
		
		exclude.onClick.AddListener(() => OnExcludeClicked());
        include.onClick.AddListener(() => OnIncludeClicked());
		
		everything.onValueChanged.AddListener(_ => FlipEverything());
		
		for (int i = 0; i < TopologyToggles.Count; i++){
			TopologyToggles[i].onValueChanged.AddListener(_ => SendSettingsPreview());
		}
		
		preview.onValueChanged.AddListener(_ => SendSettingsPreview());
		
		AddPreset.onClick.AddListener(OnAddToPresetList);
        ApplyPresetList.onClick.AddListener(OnApplyPresetList);
        ApplyGeology.onClick.AddListener(OnApplyGeologyPreset);
        SaveList.onClick.AddListener(OnSavePresetList);
        SavePreset.onClick.AddListener(OnSavePreset);
        LoadPreset.onClick.AddListener(OnLoadPreset);
		MacroDropDown.onValueChanged.AddListener(OnPresetListChanged);
		DeletePreset.onClick.AddListener(OnDeletePreset);
		StopGeneration.onClick.AddListener(OnStopGeneration);

		collisionLayer.onValueChanged.AddListener(OnCollisionLayerChanged);
		//tabs.OnToggleChanged(1);
		//tabs.OnToggleChanged(5);
		SetPreview();
		
    }
	
	//clicking each one should make them become  non interactable and the other interactable
	//
    private void OnIncludeClicked()
    {
        var settings = SettingsManager.geology;
        settings.avoidTopo = false;

        // Update button states
        include.interactable = false; // Disable include button
        exclude.interactable = true;  // Enable exclude button
		
		SettingsManager.geology = settings;
		SetPreview();
    }

    private void OnExcludeClicked()
    {
        var settings = SettingsManager.geology;
        settings.avoidTopo = true;

        // Update button states
        include.interactable = true;  // Enable include button
        exclude.interactable = false; // Disable exclude button
		
		SettingsManager.geology = settings;
		SetPreview();
    }
	
	private void OnStopGeneration(){
		GenerativeManager.StopCliffs();
	}
	
	private void OnMacroFieldChanged()
	{
		// Check if the macroField's text corresponds to an existing macro path
		bool isExistingMacroPath = SettingsManager.MacroExists(macroField.text);
		
		if (ApplyPresetList != null)
		{
			ApplyPresetList.interactable = isExistingMacroPath;
		}
		
		if (SaveList != null)
		{
			SaveList.interactable = !string.IsNullOrEmpty(macroField.text);
		}
	}
	
	private void SetPreview()
	{
		var geologySettings = SettingsManager.geology;
		geologySettings.preview = preview.isOn;

		if (preview.isOn && gameObject.activeInHierarchy)
		{
			TerrainManager.ShowLandMask();

			GenerativeManager.MakeCliffMap(SettingsManager.geology, OnCliffMapComplete);
		}
		else
		{
			TerrainManager.HideLandMask();
			footer.text = "";
		}
	}

	public void Progress(float percent)
    {
        if (progress == null || frame == null) return; // Safety check

        RectTransform frameRect = frame.rectTransform;
        RectTransform progressRect = progress.rectTransform;

        float frameWidth = frameRect.rect.width;
        float newWidth = frameWidth * .98f * percent;

        // This method respects anchors
        progressRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }

	private void OnCliffMapComplete()
	{
		footer.text = GenerativeManager.GeologySpawns.ToString() + " spawns";
	}

	private void OnEnable(){
		TerrainManager.FillLandMask();
		PopulatePresetLists();
		SetPreview();
		Progress(0f);
	}
	
	private void OnDisable(){
		SetPreview();
	}
	
	private void SendSettingsPreview() {
		var geologySettings = SettingsManager.geology;
		
		geologySettings.preview = preview.isOn;
		
		geologySettings.curveRange = curveRange.isOn;
		geologySettings.heightRange = heightRange.isOn;
		geologySettings.slopeRange = slopeRange.isOn;

		// Frequency and height settings
		geologySettings.frequency = (int)frequency.value;
		geologySettings.heights = new HeightSelector
		{
			heightMin = height.value,
			heightMax = heightH.value,
			slopeLow = slope.value,
			slopeHigh = slopeH.value,
			curveMin = curve.value,
			curveMax = curveH.value,
			slopeWeight = 1f,
			curveWeight = 1f
		};

		// Set biome toggles
		geologySettings.arid = arid.isOn;
		geologySettings.temperate = temperate.isOn;
		geologySettings.arctic = arctic.isOn;
		geologySettings.tundra = tundra.isOn;

		// Set topology flags based on toggles
		geologySettings.topologies = 0;  // Reset flags
		for (int i = 0; i < TopologyToggles.Count; i++)
		{
			if (TopologyToggles[i].isOn)
			{
				geologySettings.topologies |= (Topologies)(1 << i);  // Set the bit corresponding to the toggle
			}
		}

		// Update SettingsManager
		SettingsManager.geology = geologySettings;
		SetPreview();
	}
	
	private void SendSettings()	{
		var geologySettings = SettingsManager.geology;

		// Set rotation, scale, and jitter settings
		geologySettings.rotationsLow = new Vector3(xRot.value, yRot.value, zRot.value);
		geologySettings.rotationsHigh = new Vector3(xRotH.value, yRotH.value, zRotH.value);

		geologySettings.scalesLow = new Vector3(xScale.value, yScale.value, zScale.value);
		geologySettings.scalesHigh = new Vector3(xScaleH.value, yScaleH.value, zScaleH.value);

		geologySettings.jitterLow = new Vector3(xJitter.value, yJitter.value, zJitter.value);
		geologySettings.jitterHigh = new Vector3(xJitterH.value, yJitterH.value, zJitterH.value);

		// Range and cliff test toggles

		geologySettings.cliffTest = rayTest.isOn;

		// Other settings (flip, geofield, snap options)
		geologySettings.flipping = flip.isOn;
		geologySettings.tilting = geofield.isOn;
		geologySettings.normalizeX = snapX.isOn;
		geologySettings.normalizeY = snapY.isOn;
		geologySettings.normalizeZ = snapZ.isOn;

		geologySettings.title = PresetField.text;
		
		SettingsManager.geology = geologySettings;

	}

	private void PopulateSettings()
	{
		var geologySettings = SettingsManager.geology;

		// Set values for rotations
		xRot.SetValueWithoutNotify(geologySettings.rotationsLow.x);
		yRot.SetValueWithoutNotify(geologySettings.rotationsLow.y);
		zRot.SetValueWithoutNotify(geologySettings.rotationsLow.z);
		xRotH.SetValueWithoutNotify(geologySettings.rotationsHigh.x);
		yRotH.SetValueWithoutNotify(geologySettings.rotationsHigh.y);
		zRotH.SetValueWithoutNotify(geologySettings.rotationsHigh.z);

		// Set values for scales
		xScale.SetValueWithoutNotify(geologySettings.scalesLow.x);
		yScale.SetValueWithoutNotify(geologySettings.scalesLow.y);
		zScale.SetValueWithoutNotify(geologySettings.scalesLow.z);
		xScaleH.SetValueWithoutNotify(geologySettings.scalesHigh.x);
		yScaleH.SetValueWithoutNotify(geologySettings.scalesHigh.y);
		zScaleH.SetValueWithoutNotify(geologySettings.scalesHigh.z);

		// Set values for jitter
		xJitter.SetValueWithoutNotify(geologySettings.jitterLow.x);
		yJitter.SetValueWithoutNotify(geologySettings.jitterLow.y);
		zJitter.SetValueWithoutNotify(geologySettings.jitterLow.z);
		xJitterH.SetValueWithoutNotify(geologySettings.jitterHigh.x);
		yJitterH.SetValueWithoutNotify(geologySettings.jitterHigh.y);
		zJitterH.SetValueWithoutNotify(geologySettings.jitterHigh.z);

		// Other settings
		frequency.SetValueWithoutNotify(geologySettings.frequency);
		height.SetValueWithoutNotify(geologySettings.heights.heightMin);
		heightH.SetValueWithoutNotify(geologySettings.heights.heightMax);
		slope.SetValueWithoutNotify(geologySettings.heights.slopeLow);
		slopeH.SetValueWithoutNotify(geologySettings.heights.slopeHigh);
		curve.SetValueWithoutNotify(geologySettings.heights.curveMin);
		curveH.SetValueWithoutNotify(geologySettings.heights.curveMax);

		// Toggle settings
		flip.SetIsOnWithoutNotify(geologySettings.flipping);
		geofield.SetIsOnWithoutNotify(geologySettings.tilting);
		snapX.SetIsOnWithoutNotify(geologySettings.normalizeX);
		snapY.SetIsOnWithoutNotify(geologySettings.normalizeY);
		snapZ.SetIsOnWithoutNotify(geologySettings.normalizeZ);
		preview.SetIsOnWithoutNotify(geologySettings.preview);
		curveToggle.SetIsOnWithoutNotify(geologySettings.curveRange);
		heightToggle.SetIsOnWithoutNotify(geologySettings.heightRange);
		slopeToggle.SetIsOnWithoutNotify(geologySettings.slopeRange);
		rayTest.SetIsOnWithoutNotify(geologySettings.cliffTest);

		// Biome toggles
		arid.SetIsOnWithoutNotify(geologySettings.arid);
		temperate.SetIsOnWithoutNotify(geologySettings.temperate);
		arctic.SetIsOnWithoutNotify(geologySettings.arctic);
		tundra.SetIsOnWithoutNotify(geologySettings.tundra);

		// Topology toggles
		for (int i = 0; i < TopologyToggles.Count; i++)
		{
			Topologies currentTopology = (Topologies)(1 << i);
			TopologyToggles[i].SetIsOnWithoutNotify(geologySettings.topologies.HasFlag(currentTopology));
		}

		// Populate additional lists
		PopulateToggleList();
		PopulateCollisionList();
		PopulateCollisionDropdown();
		hierarchyWindow.PopulateItemList();

	}

	private void AddCollisionItem()
	{
		GeologyCollisions item = new GeologyCollisions();
		
		if (float.TryParse(distance.text, out float parsedRadius))		{
			item.radius = parsedRadius;
		}
		item.minMax = minMax.isOn;
		item.layer = colliderLayer;
		SettingsManager.geology.geologyCollisions.Add(item);
		
		PopulateCollisionList(); 
	}

	private void PopulatePresetList()
	{
		foreach (Transform child in presetContent.transform)
		{
			Destroy(child.gameObject);
		}

		List<string> macroList = SettingsManager.macro;

		for (int i = 0; i < macroList.Count; i++)
		{        
			var itemCopy = Instantiate(presetListTemplate);
			var button = itemCopy.transform.Find("RemoveItem").GetComponent<Button>();
			var presetText = itemCopy.transform.Find("PresetLabel").GetComponent<Text>();

			presetText.text = macroList[i];

			int currentIndex = i;

			button.onClick.AddListener(() =>
			{
				SettingsManager.RemovePreset(currentIndex);                
				PopulatePresetList();
			});

			itemCopy.transform.SetParent(presetContent.transform, false);
			itemCopy.gameObject.SetActive(true);
		}
	}

	public void OnPresetListChanged(int selectedIndex)
	{
		// Check if MacroDropDown has any options
		if (MacroDropDown == null || MacroDropDown.options.Count == 0)
		{
			Debug.LogWarning("MacroDropDown has no options available.");
			return;
		}

		// Check if selectedIndex is within the range of options
		if (selectedIndex < 0 || selectedIndex >= MacroDropDown.options.Count)
		{
			Debug.LogWarning($"Invalid selectedIndex: {selectedIndex}. It should be between 0 and {MacroDropDown.options.Count - 1}.");
			return;
		}

		// Get the selected option text and update macroField
		string selectedOption = MacroDropDown.options[selectedIndex].text;
		macroField.text = selectedOption;

		SettingsManager.LoadGeologyMacro(selectedOption);		
		PopulatePresetList();
	}
	
	private void FlipEverything()	{
		bool thing = everything.isOn;
		
		var geo = SettingsManager.geology;		
		geo.avoidTopo=thing;
		SettingsManager.geology =geo;
		
		SetPreview();
		/*
		// Use SetIsOnWithoutNotify to avoid triggering events
		for (int i = 0; i < TopologyToggles.Count; i++)
		{     
			TopologyToggles[i].SetIsOnWithoutNotify(thing);
		}

		arctic.SetIsOnWithoutNotify(thing);
		temperate.SetIsOnWithoutNotify(thing);
		arid.SetIsOnWithoutNotify(thing);
		tundra.SetIsOnWithoutNotify(thing);
		SendSettings();
		SetPreview();
		*/
	}
	
	private void PopulateToggleList()
	{
		var topoEnum = SettingsManager.geology.topologies;

		for (int i = 0; i < TopologyToggles.Count; i++)
		{
			// Use SetIsOnWithoutNotify to set the toggle state without triggering events
			Topologies currentTopology = (Topologies)(1 << i);
			TopologyToggles[i].SetIsOnWithoutNotify(topoEnum.HasFlag(currentTopology));
		}
	}

	private void PopulateCollisionList()
	{
		ClearCollisionList();

		foreach (GeologyCollisions item in SettingsManager.geology.geologyCollisions)
		{
			var itemCopy = Instantiate(itemCollisionTemplate);

			var layerText = itemCopy.transform.Find("LayerLabel").GetComponent<Text>();
			var distanceField = itemCopy.transform.Find("DistanceField").GetComponent<InputField>();
			var button = itemCopy.transform.Find("RemoveItem").GetComponent<Button>();
			var minMaxToggle = itemCopy.transform.Find("DistanceToggle").GetComponent<Toggle>();

			layerText.text = item.layer.ToString();
			distanceField.text = item.radius.ToString(); 
			minMaxToggle.isOn = item.minMax;

			var currentItem = item;

			button.onClick.AddListener(() =>
			{
				SettingsManager.geology.geologyCollisions.Remove(currentItem);
				PopulateCollisionList(); 
			});

			distanceField.onEndEdit.AddListener(value =>
			{
				if (float.TryParse(value, out float newDistance))
				{
					currentItem.radius = newDistance;
				}
			});
			
			minMaxToggle.onValueChanged.AddListener(isOn =>
			{
				currentItem.minMax = isOn; 
			});

			itemCopy.transform.SetParent(content.transform, false);
			itemCopy.gameObject.SetActive(true);
		}
	}

	private void ClearCollisionList()
	{
		foreach (Transform child in content.transform)
		{
			Destroy(child.gameObject);
		}
	}
	
	private void PopulateCollisionDropdown()
	{
		collisionLayer.ClearOptions();

		// Only add the specific layers you want to display
		var options = new List<string>
		{
			nameof(ColliderLayer.Prefabs),
			nameof(ColliderLayer.Volumes),
			nameof(ColliderLayer.Paths),
			nameof(ColliderLayer.Land),
			nameof(ColliderLayer.Water)
		};

		collisionLayer.AddOptions(options);

		int defaultIndex = options.IndexOf(nameof(ColliderLayer.Prefabs)); // default
		collisionLayer.value = defaultIndex >= 0 ? defaultIndex : 0; // Safe default to 0 if not found
		collisionLayer.RefreshShownValue();
		

	}
	
	private void OnCollisionLayerChanged(int index)
	{
		var selectedLayerName = collisionLayer.options[index].text;

		if (Enum.TryParse(selectedLayerName, out ColliderLayer selectedLayer))		{
			Debug.LogError((int)selectedLayer);
			colliderLayer = selectedLayer;
		}
	}
	
	public void PopulatePresetLists(){

		if (SettingsManager.geologyPresets != null)		{
			MacroDropDown.ClearOptions();
			SettingsManager.LoadMacros();
			MacroDropDown.AddOptions(SettingsManager.geologyPresetLists.ToList());
		}
		else
		{
			MacroDropDown.ClearOptions();
			Debug.LogError("No Preset Lists found");
		}
		
	}
	
	public void OnSelect(Node selected)	{
		//footer.text = SettingsManager.AppDataPath() + selected.name;
		PresetField.text = selected.name;
		PresetLabel.text = PresetField.text;
		SettingsManager.LoadGeologyPreset(PresetField.text);
		PopulateSettings();
		SetPreview();
	}
		
	public void OnAddToPresetList(){

		SettingsManager.AddToMacro(PresetField.text);		
		SettingsManager.SaveGeologyMacro(macroField.text);
		SettingsManager.LoadGeologyMacro(macroField.text);
		PopulatePresetList();
	}
	
	public void OnApplyGeologyPreset(){
		GenerativeManager.ApplyGeologyPreset(SettingsManager.geology);
	}
	
	public void OnApplyPresetList(){
		StopGeneration.interactable = false;
		SettingsManager.LoadGeologyMacro(macroField.text);
		GenerativeManager.ApplyGeologyTemplate();
	}
	
	public void OnSavePresetList(){
		SettingsManager.SaveGeologyMacro(macroField.text);
		PopulatePresetLists();
	}
	
	public void OnSavePreset(){		
		SettingsManager.SaveGeologyPreset();
		PopulatePresetTree();
	}
	
	public void OnLoadPreset(){
		PresetLabel.text = PresetField.text;
		SettingsManager.LoadGeologyPreset(PresetField.text);
		PopulateSettings();
		SetPreview();
	}
	
	public void OnDeletePreset(){
		SettingsManager.DeleteGeologyPreset();
		PopulatePresetTree();
	}


}
