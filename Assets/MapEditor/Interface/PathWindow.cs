using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using EasyRoads3Dv3;
using System.Collections.Generic;
using System;
using RustMapEditor.Variables;
using static WorldSerialization;

public class PathWindow : MonoBehaviour
{
    public Text nameField;
    public InputField widthField;
    public InputField innerPaddingField;
    public InputField outerPaddingField;
    public InputField innerFadeField;
    public InputField outerFadeField;
    public Dropdown splatDropdown;
    public Dropdown topologyDropdown;

    private PathDataHolder currentPathHolder;
    private ERRoad currentRoad;
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
        PopulateDropdowns();
        // Subscribe to selection changes
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnSelectionChanged += UpdateData;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnSelectionChanged -= UpdateData;
        }
    }

    public void UpdateData()
    {

        if (CameraManager.Instance._selectedRoad != null)
        {
            PathData pathData = CameraManager.Instance._selectedRoad;
            GameObject roadObject = null;

            PathDataHolder[] pathHolders = PathManager.CurrentMapPaths;
            foreach (var holder in pathHolders)
            {
                if (holder != null && holder.pathData == pathData)
                {
                    roadObject = holder.gameObject;
                    break;
                }
            }

            if (roadObject != null)
            {
                if (!gameObject.activeSelf && AppManager.Instance != null)
                {
                    Debug.Log($"Activating PathWindow for road: {roadObject.name}");
                    AppManager.Instance.ActivateWindow(9);
                }
                SetSelection(roadObject);
            }
            else
            {
                Debug.LogWarning($"Could not find road object for PathData: {pathData.name}");
                ClearUI();
            }
        }
        else if (CameraManager.Instance._selectedObjects.Count > 0)
        {
            // Check if a node or NodeCollection is selected
            GameObject selectedObject = CameraManager.Instance._selectedObjects[CameraManager.Instance._selectedObjects.Count - 1];
            if (selectedObject.CompareTag("Node") || selectedObject.CompareTag("NodeParent"))
            {
                PathDataHolder pathHolder = selectedObject.GetComponentInParent<PathDataHolder>();
                if (pathHolder != null)
                {
                    CameraManager.Instance._selectedRoad = pathHolder.pathData;
                    SetSelection(pathHolder.gameObject);
                    return;
                }
            }
            ClearUI();
        }
        else
        {
            ClearUI();
        }
    }


    void OnEnable()
    {
        CoroutineManager.Instance.ChangeStylus(3);
        Debug.Log("Path window enabled");
        UpdateData(); // Reflect current selection when activated
    }

void OnDisable()
{
    CoroutineManager.Instance.ChangeStylus(1);
    Debug.Log("Path window disabled");

    if (CameraManager.Instance != null && CameraManager.Instance._selectedRoad != null)
    {
        GameObject currentRoad = PathManager.CurrentMapPaths.FirstOrDefault(h => h?.pathData == CameraManager.Instance._selectedRoad)?.gameObject;
        if (currentRoad != null)
        {
            CameraManager.Instance.DepopulateNodesForRoad(currentRoad);
        }
        CameraManager.Instance._selectedRoad = null;
        CameraManager.Instance._selectedObjects.Clear();
        CameraManager.Instance.NotifySelectionChanged();
        CameraManager.Instance.UpdateGizmoState();
        Debug.Log("Selected road unselected, nodes depopulated, and gizmos updated.");
    }

    DestroyListeners();
}


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

    private void RetrievePathData(PathData pathData)
    {
        DestroyListeners();

        nameField.text = pathData.name;
        widthField.text = pathData.width.ToString();
        innerPaddingField.text = pathData.innerPadding.ToString();
        outerPaddingField.text = pathData.outerPadding.ToString();
        innerFadeField.text = pathData.innerFade.ToString();
        outerFadeField.text = pathData.outerFade.ToString();
        splatDropdown.value = splatEnums.IndexOf((TerrainSplat.Enum)pathData.splat);
        topologyDropdown.value = topologyEnums.IndexOf((TerrainTopology.Enum)pathData.topology);

        CreateListeners();
    }

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

        PathManager.ConfigureRoad(currentRoad, data);
        currentRoad.Refresh();

        NodeCollection nodeCollection = currentPathHolder.GetComponent<NodeCollection>();
        if (nodeCollection != null)
        {
            nodeCollection.UpdatePathData();
            nodeCollection.UpdateRoadMarkers();
        }

        CameraManager.Instance._selectedRoad = data;
        Debug.Log($"Updated path '{data.name}' with new data.");
    }

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