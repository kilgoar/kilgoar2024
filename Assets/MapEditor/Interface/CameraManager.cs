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
	

    private Vector3 movement = new Vector3(0, 0, 0);
    private float movementSpeed = 100f;
    private float rotationSpeed = .25f;
    private InputControl<Vector2> mouseMovement;
    private Vector3 globalMove = new Vector3(0, 0, 0);
    private float pitch = 90f;
    private float yaw = 0f;
    private float sprint = 1f;
    private bool dragXarrow, dragYarrow, dragZarrow, sync;
    Quaternion dutchlessTilt;
	private Keyboard key;
	private Mouse mouse;

	private float currentTime;
	private float lastUpdateTime = 0f;
	private float updateFrequency = .3f;
	
	private ObjectTransformGizmo _objectMoveGizmo;
    private ObjectTransformGizmo _objectRotationGizmo;
    private ObjectTransformGizmo _objectScaleGizmo;
    private ObjectTransformGizmo _objectUniversalGizmo;
	
	public List<GameObject> _selectedObjects = new List<GameObject>();
	
	private int layerMask = 1 << 10; // "Land" layer	
	
	private GizmoId _workGizmoId;
	private ObjectTransformGizmo _workGizmo;

	private List<RaycastHit> previousHits = new List<RaycastHit>();
	private int currentSelectionIndex = 0;

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

    void Update()
    {
        if (cam == null) return;
		
		if (LoadScreen.Instance.isEnabled){
			return;
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
			
			if (key.deleteKey.isPressed) {
				DeleteSelection();
				
				if(ItemsWindow.Instance!=null){
					ItemsWindow.Instance.PopulateList();
				}
				
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
	
	public void SelectPrefabWithoutNotify(GameObject go){
		_selectedObjects.Add(go);
		ItemsWindow.Instance.SetSelection(go);
		EmissionHighlight(GetRenderers(go));
		UpdateGizmoState();
	}
	
	public void SelectPrefab(GameObject go){
		_selectedObjects.Add(go);
		ItemsWindow.Instance.SetSelection(go);
		EmissionHighlight(GetRenderers(go));
		UpdateItemsWindow();
		UpdateGizmoState();

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
	}
	
	public GameObject FindParentWithTag(GameObject hitObject, string tag){
		while (hitObject != null && !hitObject.CompareTag(tag))
		{
			hitObject = hitObject.transform.parent?.gameObject;
		}
		return hitObject;
	}
	
	public void SelectPrefab()
	{
		int meshCount=0;
		int volumesCount=0;
		
		int prefabLayerMask = 1 << 3; // prefab layer
		int landLayerMask = 1 << 10; // land layer
		int allLayersMask = ~0; // all layers

		Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
		RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, allLayersMask);

		Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

		// Determine the closest land distance
		float landDistance = float.MaxValue;
		foreach (var hit in hits)
		{
			if ((1 << hit.transform.gameObject.layer & landLayerMask) != 0)
			{
				landDistance = hit.distance;
				break;
			}
		}

		// Rebuild the hits array with priority baked into the sorting
		List<RaycastHit> prioritizedHits = new List<RaycastHit>();

		// Add prefabs first, only those closer than the land
		foreach (var hit in hits)
		{
			if ((1 << hit.transform.gameObject.layer & prefabLayerMask) != 0 && hit.distance < landDistance)
			{
				prioritizedHits.Add(hit);
				meshCount++;
			}
		}

		// Add volumes next (layer 2), also limited by the land distance
		foreach (var hit in hits)
		{
			if ((1 << hit.transform.gameObject.layer & (1 << 2)) != 0 && hit.distance < landDistance)
			{
				prioritizedHits.Add(hit);
				volumesCount++;
			}
		}
	
		foreach (var hit in prioritizedHits)
		{
			Debug.LogError(hit.transform.name);
		}
	
		Debug.LogError(meshCount + " meshes and " + volumesCount + " volumes");

		// Check if the prioritized hits match the previous set
		bool hitsUnchanged = prioritizedHits.SequenceEqual(previousHits);
		

		// If unchanged, cycle the selection
		if (hitsUnchanged && prioritizedHits.Count > 1)
		{
			currentSelectionIndex = (currentSelectionIndex + 1) % prioritizedHits.Count;
		}
		else
		{
			currentSelectionIndex = 0;
		}
		
		previousHits = new List<RaycastHit>(prioritizedHits);

		// Select the current hit based on the index
		GameObject hitPrefab = null;
		if (prioritizedHits.Count > 0)
		{
			hitPrefab = prioritizedHits[currentSelectionIndex].transform.gameObject;
		}



		if (hitPrefab != null)
		{
			GameObject hitObject;
			
			if(Keyboard.current.ctrlKey.isPressed){
				hitObject = FindParentWithTag(hitPrefab, "Prefab");
			}
			else{
				hitObject = FindParentWithTag(hitPrefab, "Collection");
				if(hitObject==null){
					hitObject = FindParentWithTag(hitPrefab, "Prefab");
				}
			}
			
			if (hitObject != null)
			{
					List<LODMasterMesh> lodMasterMeshes = new List<LODMasterMesh>(hitObject.GetComponentsInChildren<LODMasterMesh>());

					if (_selectedObjects.Contains(hitObject)){
							Unselect(hitObject);
							return;
					}

					// Multi-select with shift key
					if (Keyboard.current.leftShiftKey.isPressed)
					{
							_selectedObjects.Add(hitObject);
					}
					else
					{
						Unselect(); // Clear previous selection
						_selectedObjects.Add(hitObject);
					}
					
					//find corresponding node for item
					Node node = null;
					if (itemTree != null)	{
						node = itemTree.FindFirstNodeByDataRecursive(hitObject);
					}

					//focus item
					if (node != null)	{
						if (ItemsWindow.Instance != null)	{
							ItemsWindow.Instance.FocusList(node);
						}
					}

					// Use the lodMasterMesh to fetch renderers and highlight them
					if (lodMasterMeshes.Count > 0)
					{
						List<Renderer> renderers = new List<Renderer>();
						foreach (var lodMasterMesh in lodMasterMeshes)
						{
							renderers.AddRange(lodMasterMesh.FetchRenderers());
						}
						EmissionHighlight(renderers);
					}
					else
					{
						EmissionHighlight(GetRenderers(hitObject));
					}
					UpdateItemsWindow();
					UpdateGizmoState();
					return;
				
			}
		}

		Unselect();
	}
	
	public void Unselect(GameObject obj)
	{
		_selectedObjects.Remove(obj);
		EmissionUnhighlight(GetRenderers(obj)); // Get the renderers from the object using helper method
		UpdateItemsWindow();
		UpdateGizmoState();
	}
	
	public void Unselect()
	{
		// Unhighlight all previously selected items
		foreach (GameObject obj in _selectedObjects)
		{
			if (obj != null) {
				EmissionUnhighlight(GetRenderers(obj)); // Get the renderers from selected objects using helper method
			}
		}
		_selectedObjects.Clear();
		// Disable all gizmos
		_workGizmo.Gizmo.SetEnabled(false);
		_objectMoveGizmo.Gizmo.SetEnabled(false);
		_objectRotationGizmo.Gizmo.SetEnabled(false);
		_objectScaleGizmo.Gizmo.SetEnabled(false);
		_objectUniversalGizmo.Gizmo.SetEnabled(false);
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

	// Helper method to modify material settings for emission highlight
	private void EmissionHighlight(List<Renderer> selection)
	{
		foreach (Renderer renderer in selection)
		{
			Material[] materials = renderer.materials;
			foreach (Material material in materials)
			{
				// Enable emission for the material
				material.EnableKeyword("_EMISSION");
				
				// Set emission color (you can adjust the color as needed)
				Color emissionColor = Color.yellow; // Example: yellow highlight
				material.SetColor("_EmissionColor", emissionColor);
			}
			renderer.materials = materials;
		}
	}

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
        if (_selectedObjects.Count > 0)
        {
            _workGizmo.Gizmo.SetEnabled(true);
            _workGizmo.SetTargetPivotObject(_selectedObjects[_selectedObjects.Count - 1]);
            _workGizmo.RefreshPositionAndRotation();
        }
        else
        {
            _workGizmo.Gizmo.SetEnabled(false);
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