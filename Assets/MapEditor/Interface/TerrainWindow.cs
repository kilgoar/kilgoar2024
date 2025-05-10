using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UIRecycleTreeNamespace;
using RustMapEditor;
using RustMapEditor.Variables;
using static WorldSerialization;
using static TerrainManager;


public class TerrainWindow : MonoBehaviour
{
	public List<Toggle> layerToggles;
	public List<GameObject> layerPanels;	
	public List<Toggle> carveToggles, waterToggles;
	public List<Toggle> TopologyToggles;	
	public Slider strength, size, height, waterHeight;
	public Text footer;
	
	public Transform brushRowParent;
    public List<GameObject> brushRows = new List<GameObject>();
    public const int BRUSHES_PER_ROW = 8;
	
	public Button TemplateButton; //this has a raw image child object RawImage which displays the paint brush. clone this when constructing rows
	
	public int topo, lastIndex;	
	public List<Texture2D> loadedBrushTextures = new List<Texture2D>(); 
    public string[] brushFiles;	
	public Toggle randomRotations;	
	Layers layers = new Layers() { Ground = TerrainSplat.Enum.Grass, Biome = TerrainBiome.Enum.Temperate, Topologies = TerrainTopology.Enum.Field};
	
	/*
	public bool rotations;
	public int targetTopo, paintMode, selectedSplatPaint;	
	public float brushStrength;
    public int brushSize, brushType;
    public float myBrushSize;
    public float terrainHeight;
    public float flattenHeight;
	*/
	public static TerrainWindow Instance { get; private set; }
	

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
	
	public void Setup()
	{
		Debug.Log("Setting up terrain window");

		// Ensure MainScript.Instance is available
		if (MainScript.Instance == null)
		{
			Debug.LogError("MainScript.Instance is null during TerrainWindow setup!");
			return;
		}

		// Populate brush buttons first
		PopulateBrushButtons();

		// Validate toggle and panel lists
		if (layerToggles == null || layerPanels == null || layerToggles.Count != layerPanels.Count)
		{
			Debug.LogError("Invalid terrain window config: layerToggles or layerPanels issue");
			return;
		}

		// Add slider listeners (values are already set in Inspector)
		strength.onValueChanged.AddListener(_ => SendSettings());
		size.onValueChanged.AddListener(_ => SendSettings());
		height.onValueChanged.AddListener(_ => SendSettings());
		waterHeight.onValueChanged.AddListener(_ => SendSettings());
		randomRotations.onValueChanged.AddListener(OnRandomRotationsChanged);
		
        MainScript.Instance.rotations = randomRotations.isOn;

		// Find and sync the initial layer toggle (based on Inspector state)
		int initialLayerIndex = -1;
		for (int i = 0; i < layerToggles.Count; i++)
		{
			if (layerToggles[i] != null && layerToggles[i].isOn)
			{
				initialLayerIndex = i;
				break;
			}
		}
		if (initialLayerIndex == -1) // Fallback if no toggle is on
		{
			initialLayerIndex = 0;
			layerToggles[0].isOn = true;
		}
		for (int i = 0; i < layerToggles.Count; i++)
		{
			if (i < layerPanels.Count && layerPanels[i] != null)
			{
				layerPanels[i].SetActive(i == initialLayerIndex);
			}
			if (i < layerToggles.Count && layerToggles[i] != null)
			{
				layerToggles[i].SetIsOnWithoutNotify(i == initialLayerIndex);
				layerToggles[i].interactable = i != initialLayerIndex;
				int index = i;
				layerToggles[i].onValueChanged.AddListener((isOn) => OnToggleChanged(index));
			}
		}
		OnToggleChanged(initialLayerIndex); // Push Inspector state to MainScript


		int initialCarveIndex = 0;
		carveToggles[0].isOn = true;

		for (int i = 0; i < carveToggles.Count; i++)
		{
			if (i < carveToggles.Count && carveToggles[i] != null)
			{
				carveToggles[i].SetIsOnWithoutNotify(i == 0);
				carveToggles[i].interactable = i != 0;
				int index = i;
				carveToggles[i].onValueChanged.AddListener((isOn) => OnCarveChanged(index));
			}
		}
		OnCarveChanged(initialCarveIndex); // Push Inspector state to MainScript
		
		int initialWaterIndex = 0;
		waterToggles[0].isOn = true;

		for (int i = 0; i < waterToggles.Count; i++)
		{
			if (i < waterToggles.Count && waterToggles[i] != null)
			{
				waterToggles[i].SetIsOnWithoutNotify(i == 0);
				waterToggles[i].interactable = i != 0;
				int index = i;
				waterToggles[i].onValueChanged.AddListener((isOn) => OnWaterChanged(index));
			}
		}
		OnWaterChanged(initialWaterIndex); // Push Inspector state to MainScript

		// Find and sync the initial topology toggle (based on Inspector state)
		int initialTopoIndex = -1;
		for (int i = 0; i < TopologyToggles.Count; i++)
		{
			if (TopologyToggles[i] != null && TopologyToggles[i].isOn)
			{
				initialTopoIndex = i;
				break;
			}
		}
		if (initialTopoIndex == -1) // Fallback if no toggle is on
		{
			initialTopoIndex = 0;
			TopologyToggles[0].isOn = true;
		}
		topo = initialTopoIndex;
		for (int i = 0; i < TopologyToggles.Count; i++)
		{
			if (i < TopologyToggles.Count && TopologyToggles[i] != null)
			{
				TopologyToggles[i].SetIsOnWithoutNotify(i == initialTopoIndex);
				TopologyToggles[i].interactable = i != initialTopoIndex;
				int index = i;
				TopologyToggles[i].onValueChanged.AddListener((isOn) => OnTopologyChanged(index));
			}
		}
		OnTopologyChanged(initialTopoIndex); // Push Inspector state to MainScript

		// Set initial brush based on Inspector or first available
		if (loadedBrushTextures.Count > 0)
		{
			OnBrushSelected(0); // Default to first brush unless specified otherwise
		}

		// Send initial slider settings to MainScript (values from Inspector)
		SendSettings();
	}
	
	private void OnRandomRotationsChanged(bool isOn)
	{
		MainScript.Instance.rotations = isOn;
		MainScript.Instance.RegenerateBrushWithRotation();
	}

	private void Start()
	{
		Setup();
	}
	
	void OnEnable()	{

		CoroutineManager.Instance.ChangeStylus(2);
		SetLayer(MainScript.Instance.brushType);		
		TopologyData.InitializeTexture();
		TopologyData.UpdateTexture();
		OnTopologyChanged(-1);
	}
	
	void SendSettings(){
		MainScript.Instance.brushStrength = strength.value;
		Land.materialTemplate.SetFloat("_BrushStrength", (float)strength.value);
		MainScript.Instance.TerrainTarget(height.value);
		MainScript.Instance.WaterTarget(waterHeight.value);
		Land.materialTemplate.SetFloat("_TerrainTarget", (float)height.value);
		MainScript.Instance.ChangeBrushSize((int)size.value*2);
	}
	
	void OnDisable(){
		//TerrainManager.UpdateHeightCache();
		CoroutineManager.Instance.ChangeStylus(1);
		Land.materialTemplate.SetFloat("_PreviewMode", 0f);

	}
	
	public void OnTopologyChanged(int index)
	{
		topo=index;
		float t = (float)index;
		MainScript.Instance.targetTopo = index;
		
		for (int i = 0; i < TopologyToggles.Count; i++)        {	
			bool isActive = i == index;
            TopologyToggles[i].SetIsOnWithoutNotify(isActive);
            TopologyToggles[i].interactable = !isActive;
        }
		Land.materialTemplate.SetFloat("_TopologyMode", t);
	}
	
	public void OnCarveChanged(int index)
	{
		MainScript.Instance.paintMode = index;
		
		for (int i = 0; i < carveToggles.Count; i++)
        {
            bool isActive = i == index;
            carveToggles[i].SetIsOnWithoutNotify(isActive);
            carveToggles[i].interactable = !isActive;
        }
	}
	
	public void OnWaterChanged(int index)
	{
		Debug.Log("changin water mode" + index);
		MainScript.Instance.waterPaintMode = index;
		
		for (int i = 0; i < waterToggles.Count; i++)
        {
            bool isActive = i == index;
            waterToggles[i].SetIsOnWithoutNotify(isActive);
            waterToggles[i].interactable = !isActive;
        }
	}
	
	public void PopulateBrushButtons()
	{
		// Clear existing rows except the first one (template row)
		for (int i = 1; i < brushRows.Count; i++)
		{
			Destroy(brushRows[i]);
		}
		brushRows.Clear();
		brushRows.Add(brushRowParent.gameObject); // Keep the original Brush Row

		Debug.LogError("made it this far 0 ");

		// Get brush paths from SettingsManager
		string brushPath = Path.Combine(SettingsManager.AppDataPath(), "Custom/Brushes");
		brushFiles = Directory.GetFiles(brushPath, "*.png");

		if (brushFiles.Length == 0)
		{
			Debug.Log("No brush images found in Custom/Brushes directory");
			footer.text = "No brushes found"; // Update footer even if no brushes are loaded
			return;
		}

		// Load all textures and store them
		loadedBrushTextures.Clear();
		
		int count= 0;
		foreach (string file in brushFiles)
		{
			Texture2D texture = LoadTextureFromFile(file);
			if (texture != null)
			{
				loadedBrushTextures.Add(texture);
				count++;
			}
		}
		
		

		// Pass the loaded textures to MainScript
		MainScript.Instance.SetBrushTextures(loadedBrushTextures.ToArray());


		// Calculate number of rows needed
		int rowCount = Mathf.CeilToInt((float)loadedBrushTextures.Count / BRUSHES_PER_ROW);

		// Create additional rows if needed
		for (int i = 1; i < rowCount; i++)
		{
			GameObject newRow = Instantiate(brushRowParent.gameObject, brushRowParent.parent);
			newRow.name = $"Brush Row {i + 1}";
			
			int targetSiblingIndex = brushRows[i - 1].transform.GetSiblingIndex() + 1;
			newRow.transform.SetSiblingIndex(targetSiblingIndex);
			
			brushRows.Add(newRow);
			
			foreach (Transform child in newRow.transform)
			{
				Destroy(child.gameObject);
			}
		}


		// Create buttons for each brush
		for (int i = 0; i < loadedBrushTextures.Count; i++)
		{
			int rowIndex = i / BRUSHES_PER_ROW;
			int buttonIndex = i % BRUSHES_PER_ROW;

			GameObject buttonObj = Instantiate(TemplateButton.gameObject, brushRows[rowIndex].transform);
			Button button = buttonObj.GetComponent<Button>();
			buttonObj.SetActive(true); 
			RawImage brushImage = buttonObj.GetComponentInChildren<RawImage>();

			// Set the texture for display
			brushImage.texture = loadedBrushTextures[i];

			// Set button properties
			buttonObj.name = $"Brush_{Path.GetFileNameWithoutExtension(brushFiles[i])}";
			
			// Add click listener with brush ID
			int brushId = i; // Capture the index as the brush ID
			button.onClick.AddListener(() => OnBrushSelected(brushId));
			
	        if (i == 0) // Use first brush as default
				{
					button.Select(); // Visually highlight
					OnBrushSelected(brushId); // Push to MainScript
				}
		}

		// Update footer text with the number of brushes loaded
		footer.text = $"{brushPath}";

		// Force layout rebuild
		LayoutRebuilder.ForceRebuildLayoutImmediate(brushRowParent.GetComponent<RectTransform>());
	}
	
    public Texture2D LoadTextureFromFile(string filePath)
    {
        try        {
            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                return texture;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load brush texture from {filePath}: {e.Message}");
        }
        return null;
    }

    public void OnBrushSelected(int brushId)
    {
        if (brushId >= 0 && brushId < loadedBrushTextures.Count)        {
            MainScript.Instance.SetBrush(brushId);
            Debug.Log($"Selected brush ID: {brushId} - {Path.GetFileName(brushFiles[brushId])}");
        }
        else
        {
            Debug.LogError($"Brush ID {brushId} is out of range for loaded textures.");
        }
    }


	
	public void SetLayer(int index){
		
		
		if (index == 1){   //show biomes
			TerrainManager.ChangeLayer(LayerType.Biome, TerrainTopology.TypeToIndex((int)layers.Topologies));
		}
		else 
		{
			TerrainManager.ChangeLayer(LayerType.Ground, TerrainTopology.TypeToIndex((int)layers.Topologies));
		}
		
		if(index == 0){    //height map editing
			OnTopologyChanged(-1); //hide topos
			return;
		}
		
		if (index == 2){                    //holes
			OnTopologyChanged(-1); //hide topos
		return;
		}
		
		if (index == 3){                   //topos
			OnTopologyChanged(MainScript.Instance.targetTopo); //hide topos			
		return;
		
		if (index == 6){
			Land.materialTemplate.SetFloat("_PreviewMode", -1f);
			return;
		}
		}
		
		
	}
	
	public void SampleHeightAtClick(RaycastHit hit)    {            
            // Get the height at the clicked position
            height.value = .001f*hit.point.y;         
    }
	
public void OnToggleChanged(int index){
	
	Debug.Log(index + " paint mode");
	
    if (index == 0 || index == 1) { MainScript.Instance.paintMode = -1; } // splat and biome
    else if (index == 2) { MainScript.Instance.paintMode = -3; } // alpha
    else if (index == 3) { MainScript.Instance.paintMode = -2; } // topology
    else if (index == 4) { MainScript.Instance.paintMode = index; } // heights
	else if (index == 5) { MainScript.Instance.paintMode = -5;  } // water
	else if (index == 6) { MainScript.Instance.paintMode = -4; } //monument blend map

    MainScript.Instance.brushType = index;
    MainScript.Instance.selectedSplatPaint = 0; // Reset to paint mode for topology
    SetLayer(index);

    // Regenerate brush to ensure correct data for the new mode
    MainScript.Instance.GenerateBrush();

    for (int i = 0; i < layerPanels.Count; i++)    {
        bool isActive = i == index;
        layerPanels[i].SetActive(isActive);
        layerToggles[i].SetIsOnWithoutNotify(isActive);
        layerToggles[i].interactable = !isActive;
    }

    Debug.Log($"Switched to layer {index}, paintMode={MainScript.Instance.paintMode}, brushSize={MainScript.Instance.brushSize}");
    LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
}
	
	

	
}
