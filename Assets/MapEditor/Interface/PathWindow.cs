using UnityEngine;
using UnityEngine.UI;
using EasyRoads3Dv3;
using System.Collections.Generic;
using System;
using RustMapEditor.Variables;
using static WorldSerialization;

public class PathWindow : MonoBehaviour
{
    // UI Elements for PathData properties
    public Text nameField;
    public InputField widthField;
    public InputField innerPaddingField;
    public InputField outerPaddingField;
    public InputField innerFadeField;
    public InputField outerFadeField;
    public Dropdown splatDropdown;
    public Dropdown topologyDropdown;

    private PathDataHolder currentPathHolder; // Reference to the selected path's data holder
    private ERRoad currentRoad; // Reference to the associated ERRoad
    private List<TerrainSplat.Enum> splatEnums = new List<TerrainSplat.Enum>();
    private List<TerrainTopology.Enum> topologyEnums = new List<TerrainTopology.Enum>();

    public static PathWindow Instance { get; private set; }

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

    void Start()
    {
        // Populate dropdowns with enum values
        PopulateDropdowns();
    }

    void OnEnable()
    {
        // Switch to stylus 3 for path editing
        CoroutineManager.Instance.ChangeStylus(3);
        Debug.Log("Path window enabled");

        // Populate UI if a path is already selected
        UpdateData();
    }

    void OnDisable()
    {
        // Revert stylus to default (1)
        CoroutineManager.Instance.ChangeStylus(1);
        Debug.Log("Path window disabled");
        DestroyListeners();
    }

    // Initialize UI with PathData from a selected GameObject
    public void SetSelection(GameObject go)
    {
        if (go == null)
        {
            ClearUI();
            return;
        }

        currentPathHolder = go.GetComponent<PathDataHolder>();
        currentRoad = go.GetComponent<ERModularRoad>()?.road;

        if (currentPathHolder == null || currentRoad == null)
        {
            Debug.LogWarning($"Selected object {go.name} is not a valid path with PathDataHolder or ERRoad.");
            ClearUI();
            return;
        }

        RetrievePathData(currentPathHolder.pathData);
    }

    // Update UI based on current selection in CameraManager
    public void UpdateData()
    {
        if (CameraManager.Instance._selectedRoad !=null)
        {
			RetrievePathData(CameraManager.Instance._selectedRoad);
        }
        else
        {
            ClearUI();
        }
    }

    // Populate UI fields with PathData values
    private void RetrievePathData(PathData pathData)
    {
        DestroyListeners();

        nameField.text = pathData.name;
        widthField.text = pathData.width.ToString("F2");
        innerPaddingField.text = pathData.innerPadding.ToString("F2");
        outerPaddingField.text = pathData.outerPadding.ToString("F2");
        innerFadeField.text = pathData.innerFade.ToString("F2");
        outerFadeField.text = pathData.outerFade.ToString("F2");
        splatDropdown.value = splatEnums.IndexOf((TerrainSplat.Enum)pathData.splat);
        topologyDropdown.value = topologyEnums.IndexOf((TerrainTopology.Enum)pathData.topology);

        CreateListeners();
    }

    // Clear UI fields when no path is selected
    private void ClearUI()
    {
        DestroyListeners();
        nameField.text = string.Empty;
        widthField.text = string.Empty;
        innerPaddingField.text = string.Empty;
        outerPaddingField.text = string.Empty;
        innerFadeField.text = string.Empty;
        outerFadeField.text = string.Empty;
        splatDropdown.value = 0;
        topologyDropdown.value = 0;

        currentPathHolder = null;
        currentRoad = null;
    }

    // Update PathData and apply to the road
    public void SendPathData()
    {
        if (currentPathHolder == null || currentRoad == null)
        {
            Debug.LogWarning("No valid path selected to update.");
            return;
        }

        PathData data = currentPathHolder.pathData;
        data.name = nameField.text;
        data.width = float.TryParse(widthField.text, out float width) ? width : data.width;
        data.innerPadding = float.TryParse(innerPaddingField.text, out float innerPadding) ? innerPadding : data.innerPadding;
        data.outerPadding = float.TryParse(outerPaddingField.text, out float outerPadding) ? outerPadding : data.outerPadding;
        data.innerFade = float.TryParse(innerFadeField.text, out float innerFade) ? innerFade : data.innerFade;
        data.outerFade = float.TryParse(outerFadeField.text, out float outerFade) ? outerFade : data.outerFade;
        data.splat = (int)splatEnums[splatDropdown.value];
        data.topology = (int)topologyEnums[topologyDropdown.value];

        // Update the road configuration
        PathManager.ConfigureRoad(currentRoad, data);
        currentRoad.Refresh();

        // Update NodeCollection if present
        NodeCollection nodeCollection = currentPathHolder.GetComponent<NodeCollection>();
        if (nodeCollection != null)
        {
            nodeCollection.UpdatePathData();
            nodeCollection.UpdateRoadMarkers();
        }

        Debug.Log($"Updated path '{data.name}' with new data.");
    }

    // Populate dropdowns with TerrainSplat and TerrainTopology enums
    private void PopulateDropdowns()
    {
        splatDropdown.options.Clear();
        foreach (TerrainSplat.Enum splat in Enum.GetValues(typeof(TerrainSplat.Enum)))
        {
            splatEnums.Add(splat);
            splatDropdown.options.Add(new Dropdown.OptionData(splat.ToString()));
        }
        splatDropdown.RefreshShownValue();

        topologyDropdown.options.Clear();
        foreach (TerrainTopology.Enum topology in Enum.GetValues(typeof(TerrainTopology.Enum)))
        {
            topologyEnums.Add(topology);
            topologyDropdown.options.Add(new Dropdown.OptionData(topology.ToString()));
        }
        topologyDropdown.RefreshShownValue();
    }

    // Add listeners to UI elements
    private void CreateListeners()
    {
        widthField.onEndEdit.AddListener(text => SendPathData());
        innerPaddingField.onEndEdit.AddListener(text => SendPathData());
        outerPaddingField.onEndEdit.AddListener(text => SendPathData());
        innerFadeField.onEndEdit.AddListener(text => SendPathData());
        outerFadeField.onEndEdit.AddListener(text => SendPathData());
        splatDropdown.onValueChanged.AddListener(value => SendPathData());
        topologyDropdown.onValueChanged.AddListener(value => SendPathData());
    }

    // Remove all listeners from UI elements
    private void DestroyListeners()
    {
        widthField.onEndEdit.RemoveAllListeners();
        innerPaddingField.onEndEdit.RemoveAllListeners();
        outerPaddingField.onEndEdit.RemoveAllListeners();
        innerFadeField.onEndEdit.RemoveAllListeners();
        outerFadeField.onEndEdit.RemoveAllListeners();
        splatDropdown.onValueChanged.RemoveAllListeners();
        topologyDropdown.onValueChanged.RemoveAllListeners();
    }
}