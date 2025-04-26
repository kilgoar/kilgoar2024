using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;
using UnityEngine.EventSystems;
using UIRecycleTreeNamespace;
using RTG;
using System;
using System.Linq;
using EasyRoads3Dv3;
using static WorldSerialization;

public class CameraManager : MonoBehaviour
{
	public GameObject transformTool;
	public UIRecycleTree itemTree;
	public Camera cam;
	public Camera camTransform;
	public GameObject xPos, yPos, zPos;
	public LayerMask worldToolLayer;
	public Terrain landTerrain;
	public List<InputField> snapFields;
	public Vector3 position;
	public bool lockCam;
	

    public Vector3 movement = new Vector3(0, 0, 0);
    public float movementSpeed = 100f;
    public float rotationSpeed = .25f;
    public InputControl<Vector2> mouseMovement;
    public Vector3 globalMove = new Vector3(0, 0, 0);
    public float pitch = 90f;
    public float yaw = 0f;
    public float sprint = 1f;
    public bool dragXarrow, dragYarrow, dragZarrow, sync;
    Quaternion dutchlessTilt;
	public Keyboard key;
	public Mouse mouse;

	public float currentTime;
	public float lastUpdateTime = 0f;
	public float updateFrequency = .3f;
	
	private ObjectTransformGizmo _objectMoveGizmo;
    private ObjectTransformGizmo _objectRotationGizmo;
    private ObjectTransformGizmo _objectScaleGizmo;
    private ObjectTransformGizmo _objectUniversalGizmo;
	
	public List<GameObject> _selectedObjects = new List<GameObject>();
	public PathData _selectedRoad = new PathData();
	
	private int layerMask = 1 << 10; // "Land" layer	
	
	public GizmoId _workGizmoId;
	public ObjectTransformGizmo _workGizmo;

	private List<RaycastHit> previousHits = new List<RaycastHit>();
	private int currentSelectionIndex = 0;
	
	public delegate void SelectionChangedHandler();
    public event SelectionChangedHandler OnSelectionChanged;

    FilePreset settings;
	
	public static CameraManager Instance { get; private set; }

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
        Configure();
		MapManager.Callbacks.MapLoaded += OnMapLoaded;
		InitializeGizmos();
		SetupSnapListeners();
    }
	
public void InitializeGizmos()
{
    // Create gizmos
    _objectMoveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
    _objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
    _objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
    _objectUniversalGizmo = RTGizmosEngine.Get.CreateObjectUniversalGizmo();

    // Disable gizmos by default (visual enablement), but enable snapping
    _objectMoveGizmo.Gizmo.SetEnabled(false);
    MoveGizmo moveGizmo = _objectMoveGizmo.Gizmo.GetFirstBehaviourOfType<MoveGizmo>();
    if (moveGizmo != null)
    {
        moveGizmo.SetSnapEnabled(true); // Enable snap by default
    }

    _objectRotationGizmo.Gizmo.SetEnabled(false);
    RotationGizmo rotationGizmo = _objectRotationGizmo.Gizmo.GetFirstBehaviourOfType<RotationGizmo>();
    if (rotationGizmo != null)
    {
        rotationGizmo.SetSnapEnabled(true); // Enable snap by default
    }

    _objectScaleGizmo.Gizmo.SetEnabled(false);
    ScaleGizmo scaleGizmo = _objectScaleGizmo.Gizmo.GetFirstBehaviourOfType<ScaleGizmo>();
    if (scaleGizmo != null)
    {
        scaleGizmo.SetSnapEnabled(true); // Enable snap by default
    }

    _objectUniversalGizmo.Gizmo.SetEnabled(false);
    UniversalGizmo universalGizmo = _objectUniversalGizmo.Gizmo.GetFirstBehaviourOfType<UniversalGizmo>();
    if (universalGizmo != null)
    {
        universalGizmo.SetSnapEnabled(true); // Enable snap by default
    }

    // Set target objects for gizmos
    _objectMoveGizmo.SetTargetObjects(_selectedObjects);
    _objectRotationGizmo.SetTargetObjects(_selectedObjects);
    _objectScaleGizmo.SetTargetObjects(_selectedObjects);
    _objectUniversalGizmo.SetTargetObjects(_selectedObjects);

    // Default to Move gizmo for selection
    _workGizmo = _objectMoveGizmo;
    _workGizmoId = GizmoId.Move;
}

	public PrefabDataHolder[] SelectedDataHolders()
	{
		if (_selectedObjects == null || _selectedObjects.Count == 0)
		{
			return new PrefabDataHolder[0]; // Return empty array if no objects are selected
		}

		// Use LINQ to filter and collect all PrefabDataHolder components
		var prefabDataHolders = _selectedObjects
			.Where(obj => obj != null) // Ensure the GameObject exists
			.SelectMany(obj => obj.GetComponents<PrefabDataHolder>()) // Get all PrefabDataHolder components from each GameObject
			.ToArray();

		return prefabDataHolders;
	}

    public enum GizmoId    {
            Move = 1,
            Rotate,
            Scale,
            Universal
    }

	public void SetCameraPosition(Vector3 position)
	{

		cam.transform.position = position;
		this.position = position; 

		currentTime = Time.time;
		if (currentTime - lastUpdateTime > updateFrequency)
		{
			AreaManager.UpdateSectors(position, settings.prefabRenderDistance);
			lastUpdateTime = currentTime;
		}

	}

	public void SetCamera(Vector3 targetPosition)
	{
		float distance = 25.0f;
		
		Vector3 initialPosition = cam.transform.position;
		initialPosition.y = targetPosition.y;
		cam.transform.position = initialPosition;
		
		Vector3 directionToTarget = targetPosition - cam.transform.position;
		Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);

		cam.transform.rotation = lookRotation;

		Vector3 offset = directionToTarget.normalized * distance;
		cam.transform.position = targetPosition - offset;

		Vector3 forward = cam.transform.forward;

		yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

		pitch = Mathf.Asin(forward.y) * Mathf.Rad2Deg;

		Quaternion finalRotation = Quaternion.Euler(pitch, yaw, 0f);
		cam.transform.rotation = finalRotation;
		position = cam.transform.position;
	}

	public void SetRenderLimit(){
		settings = SettingsManager.application;   
		cam.farClipPlane = settings.prefabRenderDistance;
		camTransform.farClipPlane = settings.prefabRenderDistance;
	}
	
	public void Configure()
	{
		
		
		lockCam = false;
		dragXarrow = false;
		dragYarrow = false;
		dragZarrow = false;
		sync=false;
		
		if (cam == null) {
			Debug.LogError("No camera found with tag 'MainCamera'. Please assign a camera to the scene.");
			return; 
		}

		settings = SettingsManager.application;        
		if (object.ReferenceEquals(settings, null)) {
			Debug.LogError("SettingsManager.application is null. Ensure it is properly initialized.");
			return; 
		}
		
		//cam.depthTextureMode = DepthTextureMode.Depth;
		cam.farClipPlane = settings.prefabRenderDistance;	
		key = Keyboard.current;
		mouse = Mouse.current;
		
		SetRenderLimit();
	}
	
	public void SetGizmoEnabled(bool enabled)
    {
        if (_workGizmo != null)
        {
            _workGizmo.Gizmo.SetEnabled(enabled);
        }
    }

    void Update()
    {
        if (cam == null) return;
		
		if (lockCam == true) return;
		
		if (LoadScreen.Instance.isEnabled){
			return;
		}
		
		if (Keyboard.current.altKey.isPressed)
			{
				SetGizmoEnabled(false);
			}
		else
			{
				UpdateGizmoState(); // Re-enable gizmo based on selection when Alt is not pressed
			}
		

		//right click down (rotate cam)
		if (mouse.rightButton.isPressed) {
				mouseMovement = mouse.delta;

				pitch -= mouseMovement.ReadValue().y * rotationSpeed;
				yaw += mouseMovement.ReadValue().x * rotationSpeed;
				if (pitch > 89f || pitch < -89f) {
					cam.transform.rotation *= Quaternion.Euler(pitch, yaw, 0f);
				}

				Quaternion dutchlessTilt = Quaternion.Euler(pitch, yaw, 0f);
				cam.transform.rotation = dutchlessTilt;
			}
		
		if(!AppManager.Instance.IsAnyInputFieldActive()){

			float sprint = .25f; // Default speed

			if (Keyboard.current.yKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Move);
			else if (Keyboard.current.eKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Rotate);
			else if (Keyboard.current.rKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Scale);
			else if (Keyboard.current.tKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Universal);
			else if (Keyboard.current.xKey.wasPressedThisFrame) ToggleGizmoSpace();

			if (Keyboard.current.ctrlKey.isPressed)
			{
				if (Keyboard.current.dKey.wasPressedThisFrame)
				{
					DuplicateSelection();
					return;
				}
				if (Keyboard.current.aKey.wasPressedThisFrame)
				{
					CreateParent();
					return;
				}
				return;
			}

			if (Keyboard.current.shiftKey.isPressed && Keyboard.current.altKey.isPressed)
			{
				sprint = 0.0375f; // Alt and Shift pressed
			}
			else if (Keyboard.current.shiftKey.isPressed)
			{
				sprint = 3f; // Only Shift pressed
			}
			else if (Keyboard.current.altKey.isPressed)
			{
				sprint = 0.075f; // Only Alt pressed
			}

			float currentSpeed = movementSpeed * sprint * Time.deltaTime;

			globalMove = Vector3.zero;
			
			if (key.wKey.isPressed) {
				globalMove += cam.transform.forward * currentSpeed;
			}

			if (key.sKey.isPressed) {
				globalMove -= cam.transform.forward * currentSpeed;
			}

			if (key.aKey.isPressed) {
				globalMove -= cam.transform.right * currentSpeed;
			}

			if (key.dKey.isPressed) {
				globalMove += cam.transform.right * currentSpeed;
			}

			if (key.zKey.isPressed) {
				globalMove -= cam.transform.up * currentSpeed;
			}

			if (key.spaceKey.isPressed) {
				globalMove += cam.transform.up * currentSpeed;
			}
			
			if (key.deleteKey.wasPressedThisFrame) {
				DeleteSelection();
				
			}
			
			if (globalMove!=Vector3.zero){
			
				cam.transform.position += globalMove;
				position = cam.transform.position;			
				currentTime = Time.time;
				
				if (currentTime - lastUpdateTime > updateFrequency)
				{
					AreaManager.UpdateSectors(position, settings.prefabRenderDistance);
				}
				lastUpdateTime = currentTime;
			}
			
			
			if(ItemsWindow.Instance!=null){
				if(ItemsWindow.Instance.gameObject.activeInHierarchy){
						ItemsWindow.Instance.UpdateData();
					}
				}
		}
	}
	
	
	public void UpdateItemsWindow(){
		if(ItemsWindow.Instance != null){
			ItemsWindow.Instance.PopulateList();
			ItemsWindow.Instance.CheckSelection();
			itemTree.Rebuild();
			ItemsWindow.Instance.FocusItem(_selectedObjects);
		}
	}
	
	public void CreateParent()
	{
		// Create a new parent
		GameObject newParent = new GameObject("Collection" + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10) + UnityEngine.Random.Range(0,10));
		newParent.tag = "Collection";
		newParent.transform.SetParent(PrefabManager.PrefabParent);
		
		// Position the parent at the average position of all selected objects
		Vector3 averagePosition = Vector3.zero;
		int validObjectCount = 0;
		
		foreach (GameObject go in _selectedObjects)
		{
			if (go != null)
			{
				averagePosition += go.transform.position;
				validObjectCount++;
			}
		}
		
		if (validObjectCount > 0)
		{
			newParent.transform.position = averagePosition / validObjectCount;
			
			// Set all selected objects as children of the new parent
			foreach (GameObject go in _selectedObjects)
			{
				if (go != null)
				{
					// GameObject sets to new parent
					go.transform.SetParent(newParent.transform, true); // true keeps world position
				}
			}
		}
		UpdateItemsWindow();
	}
		
	public void DuplicateSelection()
	{
		
		foreach (GameObject go in _selectedObjects)
		{
			if (go != null)
			{
				// Create new object copying the original
				GameObject newObject = Instantiate(go, go.transform.position, go.transform.rotation);
				// Maintain the same parent as original
				newObject.transform.parent = go.transform.parent;
				Unselect (newObject); //unselect new objects
			}
		}
		
		
	}
	
	public void DeleteSelection()
	{
		if(_selectedObjects.Count < 1){ return; }
		
		_workGizmo.Gizmo.SetEnabled(false);
		// For each object in _selectedObjects, destroy the object
		foreach (GameObject go in _selectedObjects) // Use ToList() to avoid modifying the collection while iterating
		{
			if (go != null)
			{
				//first find matching node by data in items window and delete it
				Node toDestroy = itemTree.FindFirstNodeByDataRecursive(go);
				if (toDestroy!=null){
				toDestroy.parentNode.nodes.RemoveWithoutNotify(toDestroy);
				}
				UnityEngine.Object.Destroy(go); // Use UnityEngine.Object.Destroy for game objects
				
			}
		}
		_selectedObjects.Clear();
		
		itemTree.Rebuild();
		UpdateGizmoState();
		PrefabManager.NotifyItemsChanged(false);
	}
	
	public GameObject FindParentWithTag(GameObject hitObject, string tag){
		while (hitObject != null && !hitObject.CompareTag(tag))
		{
			hitObject = hitObject.transform.parent?.gameObject;
		}
		return hitObject;
	}
	
	
public void SelectPath()
{
    if (Keyboard.current.altKey.isPressed) { return; }

    int pathLayerMask = 1 << 9; // Paths are on layer 9
    Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
    RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, pathLayerMask);

    if (hits.Length == 0)
    {
		ClearAndDepopulateSelection();
        return;
    }

    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
    GameObject hitPathObject = hits[0].transform.gameObject;

    // Resolve the road and node objects
    GameObject roadObject = null;
    GameObject nodeObject = null;
    if (hitPathObject.CompareTag("Node"))
    {
        nodeObject = hitPathObject;
        roadObject = hitPathObject.transform.parent?.GetComponentInParent<PathDataHolder>()?.gameObject;
    }
    else if (hitPathObject.CompareTag("Path"))
    {
        roadObject = hitPathObject;
    }    
	else
    {
        ClearAndDepopulateSelection(); // unlikely this could ever happen
        return;
    }
   

   

    if (roadObject == null)
    {
		ClearAndDepopulateSelection();
        Debug.LogWarning("Could not resolve road object from hit.");
        return;
    }

    // Get the PathDataHolder to check against _selectedRoad
    PathDataHolder pathDataHolder = roadObject.GetComponent<PathDataHolder>();
    if (pathDataHolder == null || pathDataHolder.pathData == null)
    {
        Debug.LogWarning($"No valid PathDataHolder found on '{roadObject.name}'.");
        return;
    }

    // Handle node selection
    if (nodeObject != null)
    {
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            // Multi-selection: toggle node inclusion
            if (_selectedObjects.Contains(nodeObject))
            {
                _selectedObjects.Remove(nodeObject);
                EmissionHighlight(GetRenderers(nodeObject), false);
            }
            else
            {
                _selectedObjects.Add(nodeObject);
                EmissionHighlight(GetRenderers(nodeObject), true);
            }
        }
        else
        {
            // Single selection: clear all and select this node
            Unselect();
            _selectedObjects.Add(nodeObject);
            EmissionHighlight(GetRenderers(nodeObject), true);
        }

        // Sync with ItemsWindow if active
        if (ItemsWindow.Instance != null)
        {
            Node nodeInTree = ItemsWindow.Instance.tree?.FindFirstNodeByDataRecursive(nodeObject);
            if (nodeInTree != null)
            {
                nodeInTree.SetCheckedWithoutNotify(_selectedObjects.Contains(nodeObject));
                ItemsWindow.Instance.FocusList(nodeInTree);
            }
            else
            {
                // Populate the road in the tree if not already present
                ItemsWindow.Instance.PopulateList();
                nodeInTree = ItemsWindow.Instance.tree?.FindFirstNodeByDataRecursive(nodeObject);
                if (nodeInTree != null)
                {
                    nodeInTree.SetCheckedWithoutNotify(true);
                    ItemsWindow.Instance.FocusList(nodeInTree);
                }
            }
        }
    }
    // Handle road selection
    else if (_selectedRoad != null && pathDataHolder.pathData == _selectedRoad)
    {
        // Deselect the road if clicked again
		ClearAndDepopulateSelection();
    }
    else
    {
        // Clear previous selection and select the road
        Unselect();
        _selectedRoad = pathDataHolder.pathData;

        // Populate nodes and select the first one
        GameObject firstNode = PopulateNodesForRoad(roadObject);
        if (firstNode != null)
        {
            _selectedObjects.Add(firstNode);
            EmissionHighlight(GetRenderers(firstNode), true);
        }

        // Sync with ItemsWindow if active
        if (ItemsWindow.Instance != null)
        {
            ItemsWindow.Instance.PopulateList(); // Ensure the tree reflects the new road and nodes
            Node pathNode = ItemsWindow.Instance.tree?.FindFirstNodeByDataRecursive(roadObject);
            if (pathNode != null)
            {
                pathNode.isChecked = false; // Road itself isn’t "checked," nodes are
                Node firstNodeInTree = ItemsWindow.Instance.tree.FindFirstNodeByDataRecursive(firstNode);
                if (firstNodeInTree != null)
                {
                    firstNodeInTree.SetCheckedWithoutNotify(true);
                    ItemsWindow.Instance.FocusList(firstNodeInTree);
                }
            }
        }
    }

    UpdateGizmoState();
    NotifySelectionChanged();

    // Update PathWindow regardless of ItemsWindow state
    if (PathWindow.Instance != null)
    {
        Debug.Log("SelectPath: Updating PathWindow");
        PathWindow.Instance.UpdateData();
    }
}

	public void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke();
    }

	// Helper method to handle repeated clearing and depopulation logic
	private void ClearAndDepopulateSelection()
	{
		foreach (GameObject selected in _selectedObjects.ToList())
		{
			GameObject depopObject = selected.transform.parent?.GetComponentInParent<PathDataHolder>()?.gameObject;
			if (depopObject != null)
			{
				DepopulateNodesForRoad(depopObject);
			}
		}
		Unselect();
		UpdateItemsWindow();
		UpdateGizmoState();
	}

	public GameObject PopulateNodesForRoad(GameObject roadObject)
    {
        GameObject result = PopulateNodesForRoadInternal(roadObject); // Refactor to avoid duplication
        if (result != null)
        {
            OnSelectionChanged?.Invoke(); // Notify after populating road
        }
        return result;
    }

	public GameObject PopulateNodesForRoadInternal(GameObject roadObject)
	{
		if (roadObject == null) return null;

		// Ensure only one road is ever selected
		GameObject[] allPaths = GameObject.FindGameObjectsWithTag("Path");
		foreach (GameObject path in allPaths)
		{
			DepopulateNodesForRoad(path);
		}

		Transform existingNodes = roadObject.transform.Find("Nodes");
		if (existingNodes != null) return existingNodes.gameObject; // Return existing NodeCollection if already populated

		// Get the ERRoad from the road network using PathDataHolder
		PathDataHolder pathDataHolder = roadObject.GetComponent<PathDataHolder>();
		if (pathDataHolder == null || pathDataHolder.pathData == null)
		{
			Debug.LogError($"No PathDataHolder or PathData found on {roadObject.name}. Cannot populate nodes.");
			return null;
		}

		ERRoadNetwork roadNetwork = new ERRoadNetwork();
		ERRoad road = roadNetwork.GetRoadByName(pathDataHolder.pathData.name);
		if (road == null)
		{
			Debug.LogError($"Could not find ERRoad for {pathDataHolder.pathData.name} in the road network.");
			return null;
		}

		AppManager.Instance.ActivateWindow(9);

		GameObject nodeContainer = new GameObject("Nodes");
		nodeContainer.tag = "NodeParent";
		nodeContainer.transform.SetParent(roadObject.transform, false);
		NodeCollection nodeCollection = nodeContainer.AddComponent<NodeCollection>();
		nodeCollection.Initialize(road);
		nodeCollection.pathData = pathDataHolder.pathData; // Set pathData directly
		nodeCollection.PopulateNodes();

		PathManager.SetCurrentNodeCollection(nodeCollection); // Static reference

		_selectedRoad = pathDataHolder.pathData;

		// Update the ItemsWindow tree
		if (ItemsWindow.Instance != null)
		{
			// Find the tree node corresponding to the roadObject
			Node pathNode = ItemsWindow.Instance.tree.FindFirstNodeByDataRecursive(roadObject);
			if (pathNode != null)
			{
				// Update the node's data to reference the NodeCollection
				pathNode.data = nodeContainer;

				// Clear existing child nodes (if any) to avoid duplicates
				pathNode.nodes.Clear();

				// Add each node from the NodeCollection as a child node in the tree
				foreach (Transform nodeTransform in nodeCollection.GetNodes())
				{
					if (nodeTransform != null)
					{
						Node childNode = new Node(nodeTransform.name) { data = nodeTransform.gameObject };
						pathNode.nodes.AddWithoutNotify(childNode);
					}
				}

				// Rebuild the tree to reflect changes
				ItemsWindow.Instance.tree.Rebuild();
				Debug.Log($"Updated tree for path '{roadObject.name}' with {nodeCollection.GetNodes().Count} nodes.");
			}
			else
			{
				Debug.LogWarning($"Could not find tree node for path '{roadObject.name}' to update with NodeCollection.");
			}
		}

		// Optionally log if no nodes were created (for debugging)
		if (nodeContainer.transform.childCount == 0)
		{
			Debug.LogWarning($"No nodes were populated for road {roadObject.name}. Selection set to road object anyway.");
		}

		Transform firstNode = nodeCollection.GetFirstNode();
		return firstNode != null ? firstNode.gameObject : null;
	}

public void DepopulateNodesForRoad(GameObject roadObject)
{
    if (roadObject == null) return;

    // Clear static references
    if (_selectedRoad != null && _selectedRoad == roadObject.GetComponent<PathDataHolder>()?.pathData)
    {
        _selectedRoad = null;
    }
    if (PathManager.CurrentNodeCollection != null && PathManager.CurrentNodeCollection.gameObject.transform.parent == roadObject.transform)
    {
        PathManager.ClearCurrentNodeCollection();
    }

    // Find and destroy the Nodes GameObject
    Transform nodeParent = roadObject.transform.Find("Nodes");
    if (nodeParent != null)
    {
        GameObject.DestroyImmediate(nodeParent.gameObject);
        Debug.Log($"Destroyed Nodes GameObject for '{roadObject.name}'.");
    }

    // Restore the ItemsWindow tree
    if (ItemsWindow.Instance != null && ItemsWindow.Instance.tree != null)
    {
        Node pathDataNode = ItemsWindow.Instance.tree.FindFirstNodeByDataRecursive(roadObject);
        if (pathDataNode == null)
        {
            pathDataNode = ItemsWindow.Instance.tree.FindFirstNodeByDataRecursive(nodeParent?.gameObject);
            if (pathDataNode != null)
            {
                // If the node was reassigned to nodeContainer, fix it
                pathDataNode.data = roadObject;
                pathDataNode.nodes.Clear();
                pathDataNode.SetCheckedWithoutNotify(false);
                Debug.Log($"Restored PathData node for '{roadObject.name}' in tree.");
            }
            else
            {
                Debug.LogWarning($"No PathData node found for '{roadObject.name}' to restore.");
            }
        }
        else
        {
            // Reset the node’s data and clear children
            pathDataNode.data = roadObject;
            pathDataNode.nodes.Clear();
            pathDataNode.SetCheckedWithoutNotify(false);
            Debug.Log($"Reset PathData node for '{roadObject.name}' in tree.");
        }

        ItemsWindow.Instance.tree.Rebuild();
        Debug.Log($"Tree rebuilt after depopulating nodes for '{roadObject.name}'.");
    }

}
	
    public void SelectPrefabWithoutNotify(GameObject go)
    {
        _selectedObjects.Add(go);
        ItemsWindow.Instance.SetSelection(go);
        EmissionHighlight(GetRenderers(go), true);
        UpdateGizmoState();
        OnSelectionChanged?.Invoke(); // Notify listeners
    }

    public void SelectPrefab(GameObject go)
    {
        _selectedObjects.Add(go);
        ItemsWindow.Instance.SetSelection(go);
        EmissionHighlight(GetRenderers(go), true);
        UpdateItemsWindow();
        UpdateGizmoState();
        OnSelectionChanged?.Invoke(); // Notify listeners
    }
	
public void SelectPrefab()
{
    int prefabLayerMask = 1 << 3; // prefab layer
    int landLayerMask = 1 << 10; // land layer
    int allLayersMask = ~0; // all layers

    Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
    RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, allLayersMask);

    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    float landDistance = float.MaxValue;
    foreach (var hit in hits)
    {
        if ((1 << hit.transform.gameObject.layer & landLayerMask) != 0)
        {
            landDistance = hit.distance;
            break;
        }
    }

    List<RaycastHit> prioritizedHits = new List<RaycastHit>();
    foreach (var hit in hits)
    {
        if ((1 << hit.transform.gameObject.layer & prefabLayerMask) != 0 && hit.distance < landDistance)
        {
            prioritizedHits.Add(hit);
        }
    }
    foreach (var hit in hits)
    {
        if ((1 << hit.transform.gameObject.layer & (1 << 2)) != 0 && hit.distance < landDistance)
        {
            prioritizedHits.Add(hit);
        }
    }

    bool hitsUnchanged = prioritizedHits.SequenceEqual(previousHits);
    if (hitsUnchanged && prioritizedHits.Count > 1)
    {
        currentSelectionIndex = (currentSelectionIndex + 1) % prioritizedHits.Count;
    }
    else
    {
        currentSelectionIndex = 0;
    }
    previousHits = new List<RaycastHit>(prioritizedHits);

    GameObject hitPrefab = null;
    if (prioritizedHits.Count > 0)
    {
        hitPrefab = prioritizedHits[currentSelectionIndex].transform.gameObject;
    }

    if (hitPrefab != null)
    {
        GameObject hitObject = Keyboard.current.ctrlKey.isPressed
            ? FindParentWithTag(hitPrefab, "Prefab")
            : FindParentWithTag(hitPrefab, "Collection") ?? FindParentWithTag(hitPrefab, "Prefab");

        if (hitObject != null)
        {
            if (_selectedObjects.Contains(hitObject))
            {
                Unselect(hitObject);
                return;
            }

            // Clear previous checkmarks unless shift is pressed
            if (!Keyboard.current.leftShiftKey.isPressed)
            {
                ItemsWindow.Instance?.UnselectAllInTree();
            }

            if (Keyboard.current.leftShiftKey.isPressed)
            {
                _selectedObjects.Add(hitObject);
            }
            else
            {
                Unselect(); // Clear previous selection
                _selectedObjects.Add(hitObject);
            }

            Node node = itemTree?.FindFirstNodeByDataRecursive(hitObject);
            if (node != null && ItemsWindow.Instance != null)
            {
                ItemsWindow.Instance.FocusList(node);
            }

            List<LODMasterMesh> lodMasterMeshes = new List<LODMasterMesh>(hitObject.GetComponentsInChildren<LODMasterMesh>());
            if (lodMasterMeshes.Count > 0)
            {
                List<Renderer> renderers = new List<Renderer>();
                foreach (var lodMasterMesh in lodMasterMeshes)
                {
                    renderers.AddRange(lodMasterMesh.FetchRenderers());
                }
                EmissionHighlight(renderers, true);
            }
            else
            {
                EmissionHighlight(GetRenderers(hitObject), true);
            }
            UpdateItemsWindow();
            UpdateGizmoState();
            return;
        }
    }

    if (!Keyboard.current.leftShiftKey.isPressed)
    {
        ItemsWindow.Instance?.UnselectAllInTree();
        Unselect();
    }
}
		
public void PaintNodes()
{
    if (Keyboard.current.altKey.isPressed)
    {
        RaycastHit localHit;
        if (Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out localHit, Mathf.Infinity, layerMask))
        {
            // Check if we have an active road selection via CurrentNodeCollection
            if (PathManager.CurrentNodeCollection == null)
            {

                GameObject newRoadObject = PathManager.CreatePathAtPosition(localHit.point);
                if (newRoadObject != null)
                {
                    // Clear previous selection and set up the new road
                    Unselect();
                    GameObject firstNode = PopulateNodesForRoad(newRoadObject);
                    _selectedObjects.Add(firstNode);
                    _selectedRoad = newRoadObject.GetComponent<PathDataHolder>().pathData;

					NodeCollection nodeCollection = PathManager.CurrentNodeCollection;
                        if (nodeCollection != null && nodeCollection.GetNodes().Count >= 2)
                        {
                            Transform secondNode = nodeCollection.GetNodes()[1]; // Second node
                            if (secondNode != null)
                            {
                                _selectedObjects.Add(secondNode.gameObject);
                                EmissionHighlight(GetRenderers(secondNode.gameObject), true);
                            }
                        }

                    // Sync UI and gizmos
                    UpdateItemsWindow();
                    UpdateGizmoState();
                    PathWindow.Instance.SetSelection(newRoadObject);

                    Debug.Log($"Created new road '{_selectedRoad.name}' at {localHit.point} with properties from potentialPathData.");
                }
            }
            else
            {
                // Active road selection exists, add node to it
                NodeCollection nodeCollection = PathManager.CurrentNodeCollection;
                if (nodeCollection != null)
                {
                    Vector3 hitPoint = localHit.point;
                    nodeCollection.AddNodeAtPosition(hitPoint, _selectedObjects);
                    UpdateItemsWindow();
                    UpdateGizmoState();
                }
                else
                {
                    Debug.LogWarning("NodeCollection is null despite being checked earlier. This should not happen.");
                }
            }
        }
    }
}
	
    public void DragNodes()
    {
        RaycastHit localHit;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out localHit, Mathf.Infinity, layerMask))
        {
            Vector3 hitPoint = localHit.point;

            if (PathManager.CurrentNodeCollection == null)
            {
                // Create a new road if no collection exists (only once)
                GameObject newRoadObject = PathManager.CreatePathAtPosition(hitPoint);
                if (newRoadObject != null)
                {
                    Unselect();
                    GameObject firstNode = PopulateNodesForRoad(newRoadObject);
                    _selectedObjects.Add(firstNode);
                    _selectedRoad = newRoadObject.GetComponent<PathDataHolder>().pathData;

                    UpdateItemsWindow();
                    UpdateGizmoState();
                    PathWindow.Instance.SetSelection(newRoadObject);
                }
            }
            else
            {
                NodeCollection nodeCollection = PathManager.CurrentNodeCollection;
                nodeCollection.AddNodeAtPosition(hitPoint, _selectedObjects, 25f);
                    UpdateItemsWindow();
                    UpdateGizmoState();
                
            }
        }
	}
	
	public void Unselect(GameObject obj)
	{
		_selectedObjects.Remove(obj);
		EmissionHighlight(GetRenderers(obj),false); // Get the renderers from the object using helper method
		UpdateItemsWindow();
		UpdateGizmoState();
	}
	
	public void Unselect()
	{
		foreach (GameObject obj in _selectedObjects)
		{
			if (obj != null)
			{
				EmissionHighlight(GetRenderers(obj), false);
			}
		}
		_selectedObjects.Clear();
		_selectedRoad = null;

		if (_objectMoveGizmo != null) _objectMoveGizmo.Gizmo.SetEnabled(false);
		if (_objectRotationGizmo != null) _objectRotationGizmo.Gizmo.SetEnabled(false);
		if (_objectScaleGizmo != null) _objectScaleGizmo.Gizmo.SetEnabled(false);
		if (_objectUniversalGizmo != null) _objectUniversalGizmo.Gizmo.SetEnabled(false);
		if (_workGizmo != null)
		{
			_workGizmo.Gizmo.SetEnabled(false);
			_workGizmo.SetTargetObjects(new List<GameObject>());
		}

		if (PathWindow.Instance != null)
		{
			Debug.Log("Unselect: Road was selected, resetting PathWindow");
			PathWindow.Instance.ClearUI(); // Reset potentialPathData and sync UI
			PathWindow.Instance.UpdateData(); // Ensure UI reflects the no-selection state
		}
		
		OnSelectionChanged?.Invoke();
	}

	public List<Renderer> GetRenderers(GameObject gameObject)
	{
		List<Renderer> renderers = new List<Renderer>();
		List<LODMasterMesh> lodMasterMeshes = new List<LODMasterMesh>(gameObject.GetComponentsInChildren<LODMasterMesh>());
				
		//add renderers from hlod
		if (lodMasterMeshes.Count > 0)
			{
				foreach (var lodMasterMesh in lodMasterMeshes)
					{
						renderers.AddRange(lodMasterMesh.FetchRenderers());
						return renderers;
					}
			}
		
		
		//add renderers by recursive traversal (too expensive for large objects)
		AddRenderersFromChildren(ref renderers, gameObject);

		return renderers;
	}
	
	public void AddRenderersFromChildren(ref List<Renderer> renderers, GameObject obj)
	{
		// Check if the GameObject itself has a Renderer component
		Renderer renderer = obj.GetComponent<Renderer>();
		if (renderer != null)
		{
			renderers.Add(renderer);
		}

		// Recursively check all children
		foreach (Transform child in obj.transform)
		{
			AddRenderersFromChildren(ref renderers, child.gameObject);
		}
	}

private void EmissionHighlight(List<Renderer> selection, bool enable)
{
    foreach (Renderer renderer in selection)
    {
        Material[] materials = renderer.materials;
        foreach (Material material in materials)
        {
            if (enable)
            {
                // Subtle yellow for emission
                Color subtleYellow = new Color(.8f, .7f, 0f, 1f);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", subtleYellow);
                }
            }
            else
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", Color.black);
                    material.DisableKeyword("_EMISSION");
                }
            }
        }
        renderer.materials = materials; // Apply changes
    }
}

	/*
	// Helper method to undo emission highlight
	private void EmissionUnhighlight(List<Renderer> selection)
	{
		if (selection == null) return;

		foreach (Renderer renderer in selection)
		{
			if (renderer == null) continue;
			
			Material[] materials = renderer.materials;
			
			for (int i = 0; i < materials.Length; i++)
			{
				Material material = materials[i];
				if (material == null) continue;

				// Disable emission for the material
				material.DisableKeyword("_EMISSION");
				
				// Reset emission color to black
				material.SetColor("_EmissionColor", Color.black);

			}

				renderer.materials = materials;

		}
	}
*/
	
	private void SetWorkGizmoId(GizmoId gizmoId)
    {
        if (gizmoId == _workGizmoId) return;

        // Disable all gizmos first
        _objectMoveGizmo.Gizmo.SetEnabled(false);
        _objectRotationGizmo.Gizmo.SetEnabled(false);
        _objectScaleGizmo.Gizmo.SetEnabled(false);
        _objectUniversalGizmo.Gizmo.SetEnabled(false);


        // Set the new work gizmo
        _workGizmoId = gizmoId;
        switch(gizmoId)
        {
            case GizmoId.Move:
                _workGizmo = _objectMoveGizmo;
                break;
            case GizmoId.Rotate:
                _workGizmo = _objectRotationGizmo;
                break;
            case GizmoId.Scale:
                _workGizmo = _objectScaleGizmo;
                break;
            case GizmoId.Universal:
                _workGizmo = _objectUniversalGizmo;
                break;
        }

        // Enable the work gizmo if there are selected objects
        if (_selectedObjects.Count > 0)
        {
            _workGizmo.Gizmo.SetEnabled(true);
            _workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
            _workGizmo.RefreshPositionAndRotation();
        }
    }
	
	private void ToggleGizmoSpace()
	{
		// Check if any gizmo is null to avoid errors
		if (_objectMoveGizmo == null || _objectRotationGizmo == null || 
			_objectScaleGizmo == null || _objectUniversalGizmo == null) return;

		// Get the current space from the move gizmo as a reference (assume they’re all synced)
		bool isLocal = _objectMoveGizmo.TransformSpace == GizmoSpace.Local;
		
		// Toggle to the opposite space
		GizmoSpace newSpace = isLocal ? GizmoSpace.Global : GizmoSpace.Local;

		// Apply the new space to all gizmos
		_objectMoveGizmo.SetTransformSpace(newSpace);
		_objectRotationGizmo.SetTransformSpace(newSpace);
		_objectScaleGizmo.SetTransformSpace(newSpace);
		_objectUniversalGizmo.SetTransformSpace(newSpace);

		// Refresh the active work gizmo’s position and rotation
		if (_workGizmo != null && _workGizmo.Gizmo.IsEnabled)
		{
			_workGizmo.RefreshPositionAndRotation();
		}

		Debug.Log($"Gizmo space switched to {newSpace}");
	}
	
	
	public void UpdateGizmoState()
	{
		if (_workGizmo == null)
		{
			Debug.LogWarning("Work gizmo is null. Cannot update gizmo state.");
			return;
		}

		// Filter out null or destroyed objects from _selectedObjects
		_selectedObjects.RemoveAll(obj => obj == null);
		if (_selectedObjects.Count > 0)
		{
			// Ensure all objects are still valid
			bool hasValidObjects = false;
			GameObject lastValidObject = null;

			for (int i = _selectedObjects.Count - 1; i >= 0; i--)
			{
				if (_selectedObjects[i] != null)
				{
					hasValidObjects = true;
					lastValidObject = _selectedObjects[i]; // Last valid object for pivot
					break; // We only need the last valid one for the pivot
				}
			}

			if (hasValidObjects && lastValidObject != null)
			{
				_workGizmo.Gizmo.SetEnabled(true);
				_workGizmo.SetTargetObjects(_selectedObjects); // Set all objects as targets
				try
				{
					_workGizmo.SetTargetPivotObject(lastValidObject); // Set pivot to last valid object
					_workGizmo.RefreshPositionAndRotation();
				}
				catch (MissingReferenceException ex)
				{
					Debug.LogWarning($"Missing reference encountered while updating gizmo state: {ex.Message}. Disabling gizmo.");
					_workGizmo.Gizmo.SetEnabled(false);
					_workGizmo.SetTargetObjects(new List<GameObject>()); // Clear targets
				}
			}
			else
			{
				// No valid objects left after filtering
				_workGizmo.Gizmo.SetEnabled(false);
				_workGizmo.SetTargetObjects(new List<GameObject>()); // Clear targets
			}
		}
		else
		{
			// No selected objects
			_workGizmo.Gizmo.SetEnabled(false);
			_workGizmo.SetTargetObjects(new List<GameObject>()); // Clear targets to avoid stale references
		}
	}
	
	void DragTransform()
	{
		Vector2 mousePosition = mouse.position.ReadValue();
		Ray ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, worldToolLayer))
		{
			Debug.Log($"Hit object: {hit.collider.gameObject.name}");

			if (hit.collider.gameObject == xPos)
			{
				dragXarrow = true;
			}
			else if (hit.collider.gameObject == yPos)
			{
				dragYarrow = true;
			}
			else if (hit.collider.gameObject == zPos)
			{
				dragZarrow = true;
			}
		}
		else
		{
			Debug.Log("Raycast did not hit any objects.");
		}
	}
	
	public void OnMapLoaded(string mapName=""){
		
		
		if (ItemsWindow.Instance!=null){
			ItemsWindow.Instance.PopulateList();
		}

	}
	
	public void CenterCamera()
	{
		if (cam == null || landTerrain == null)
		{
			Debug.LogWarning("Camera or Land terrain reference is missing.");
			return;
		}


		Bounds terrainBounds = landTerrain.terrainData.bounds;
		Vector3 terrainWorldCenter = landTerrain.transform.TransformPoint(terrainBounds.center);
		Vector3 terrainWorldSize = landTerrain.transform.TransformVector(terrainBounds.size);

		float distance = Mathf.Max(terrainWorldSize.x, terrainWorldSize.z) / (2 * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2));

		cam.transform.position = terrainWorldCenter + Vector3.up * distance;
		position = cam.transform.position;
		
		cam.transform.rotation = Quaternion.Euler(90,0,0);
		
		pitch = 90f;
		yaw = 0f;
	}
	
	private void SetupSnapListeners()
    {

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                snapFields[i].onEndEdit.AddListener((text) => UpdateSnapSettings());
            }

    }

private void UpdateSnapSettings()
{
	
    if (_workGizmo == null) return;


    float moveSnap = ParseSnapValue(snapFields[0].text);
    float rotateSnap = ParseSnapValue(snapFields[1].text);
    float scaleSnap = ParseSnapValue(snapFields[2].text);

    // Apply settings directly to the current work gizmo based on its type
    switch (_workGizmoId)
    {
        case GizmoId.Move:
            MoveGizmo moveGizmo = _workGizmo.Gizmo.GetFirstBehaviourOfType<MoveGizmo>();
            if (moveGizmo != null)
            {
                moveGizmo.SetSnapEnabled(true);

                    moveGizmo.Settings3D.SetXSnapStep(moveSnap);
                    moveGizmo.Settings3D.SetYSnapStep(moveSnap);
                    moveGizmo.Settings3D.SetZSnapStep(moveSnap);


            }
            break;

        case GizmoId.Rotate:
            RotationGizmo rotationGizmo = _workGizmo.Gizmo.GetFirstBehaviourOfType<RotationGizmo>();
            if (rotationGizmo != null)
            {
                rotationGizmo.SetSnapEnabled(true);

                    rotationGizmo.Settings3D.SetAxisSnapStep(0, rotateSnap);
                    rotationGizmo.Settings3D.SetAxisSnapStep(1, rotateSnap);
                    rotationGizmo.Settings3D.SetAxisSnapStep(2, rotateSnap);


            }
            break;

        case GizmoId.Scale:
            ScaleGizmo scaleGizmo = _workGizmo.Gizmo.GetFirstBehaviourOfType<ScaleGizmo>();
            if (scaleGizmo != null)
            {
				scaleGizmo.SetSnapEnabled(true);


                    scaleGizmo.Settings3D.SetXSnapStep(scaleSnap);
                    scaleGizmo.Settings3D.SetYSnapStep(scaleSnap);
                    scaleGizmo.Settings3D.SetZSnapStep(scaleSnap);


            }
            break;

        case GizmoId.Universal:
            UniversalGizmo universalGizmo = _workGizmo.Gizmo.GetFirstBehaviourOfType<UniversalGizmo>();
            if (universalGizmo != null)
            {

                universalGizmo.SetSnapEnabled(true);
				
                    universalGizmo.Settings3D.SetMvXSnapStep(moveSnap);
                    universalGizmo.Settings3D.SetMvYSnapStep(moveSnap);
                    universalGizmo.Settings3D.SetMvZSnapStep(moveSnap);

                    universalGizmo.Settings3D.SetRtAxisSnapStep(0, rotateSnap);
                    universalGizmo.Settings3D.SetRtAxisSnapStep(1, rotateSnap);
                    universalGizmo.Settings3D.SetRtAxisSnapStep(2, rotateSnap);

                    universalGizmo.Settings3D.SetScXSnapStep(scaleSnap);
                    universalGizmo.Settings3D.SetScYSnapStep(scaleSnap);
                    universalGizmo.Settings3D.SetScZSnapStep(scaleSnap);

            }
            break;
    }
	
	UpdateGizmoState();

}
    private float ParseSnapValue(string text)
    {
        if (string.IsNullOrEmpty(text) || !float.TryParse(text, out float value) || value <= 0f)
        {
            return 0f; // Return 0 to disable snapping
        }
        return value;
    }

	
	
}