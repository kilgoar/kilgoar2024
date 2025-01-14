using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;
using UnityEngine.EventSystems;
using UIRecycleTreeNamespace;
using RTG;

public class CameraManager : MonoBehaviour
{
	public GameObject transformTool;
	public UIRecycleTree itemTree;
	public Camera cam;
	public Camera camTransform;
	public GameObject xPos, yPos, zPos;
	public LayerMask worldToolLayer;
	public Terrain landTerrain;
	
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
	private float updateFrequency = .1f;
	
	private ObjectTransformGizmo _objectMoveGizmo;
    private ObjectTransformGizmo _objectRotationGizmo;
    private ObjectTransformGizmo _objectScaleGizmo;
    private ObjectTransformGizmo _objectUniversalGizmo;
	
	private List<GameObject> _selectedObjects = new List<GameObject>();
	
	private GizmoId _workGizmoId;
	private ObjectTransformGizmo _workGizmo;

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
    }
	
	private void InitializeGizmos()
    {
        // Create gizmos
        _objectMoveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
        _objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
        _objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        _objectUniversalGizmo = RTGizmosEngine.Get.CreateObjectUniversalGizmo();

        // Disable gizmos by default
        _objectMoveGizmo.Gizmo.SetEnabled(false);
        _objectRotationGizmo.Gizmo.SetEnabled(false);
        _objectScaleGizmo.Gizmo.SetEnabled(false);
        _objectUniversalGizmo.Gizmo.SetEnabled(false);

        // Set target objects for gizmos
        _objectMoveGizmo.SetTargetObjects(_selectedObjects);
        _objectRotationGizmo.SetTargetObjects(_selectedObjects);
        _objectScaleGizmo.SetTargetObjects(_selectedObjects);
        _objectUniversalGizmo.SetTargetObjects(_selectedObjects);

        // Default to Move gizmo for selection
        _workGizmo = _objectMoveGizmo;
        _workGizmoId = GizmoId.Move;
    }

    private enum GizmoId    {
            Move = 1,
            Rotate,
            Scale,
            Universal
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
		
        if (Keyboard.current.yKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Move);
        else if (Keyboard.current.eKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Rotate);
        else if (Keyboard.current.rKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Scale);
        else if (Keyboard.current.tKey.wasPressedThisFrame) SetWorkGizmoId(GizmoId.Universal);

		/*
        // Handle selection
        if (Mouse.current.leftButton.wasPressedThisFrame && RTGizmosEngine.Get.HoveredGizmo == null)
        {
            SelectPrefab();
        }
		*/
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
		

        sprint = key.shiftKey.isPressed ? 3f : 1f;

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

        if (key.ctrlKey.isPressed) {
            globalMove -= cam.transform.up * currentSpeed;
        }

        if (key.spaceKey.isPressed) {
            globalMove += cam.transform.up * currentSpeed;
        }
		if (globalMove!=Vector3.zero){			
			cam.transform.position += globalMove;
			position = cam.transform.position;
			
			if (LoadScreen.Instance.isEnabled){
				return;
			}
			currentTime = Time.time;
			if (currentTime - lastUpdateTime < updateFrequency)
			{
				return;
			}
			lastUpdateTime = currentTime;
			AreaManager.UpdateSectors(position, settings.prefabRenderDistance);	//send changed position to area manager for LODs
		}
    }
	
	public void SelectPrefab(PrefabDataHolder holder){
		GameObject go = holder.gameObject;
		_selectedObjects.Add(go);
		UpdateGizmoState();
	}
	
	public void SelectPrefab()
	{
		Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			GameObject hitObject = hit.transform.gameObject;
			while (hitObject != null && !hitObject.CompareTag("Prefab"))
			{
				hitObject = hitObject.transform.parent?.gameObject;
			}

			if (hitObject != null && hitObject.CompareTag("Prefab"))
			{
				PrefabDataHolder dataHolder = hitObject.GetComponent<PrefabDataHolder>();
				Node node = itemTree.FindFirstNodeByDataRecursive(dataHolder);
				
				if (Keyboard.current.leftShiftKey.isPressed)
				{
					if (_selectedObjects.Contains(hitObject)) _selectedObjects.Remove(hitObject);
					else _selectedObjects.Add(hitObject);
					node.isChecked = true;					
					
					ItemsWindow.Instance.enabled = true;
					ItemsWindow.Instance.FocusList(node);
				}
				else
				{
					_selectedObjects.Clear();
					_selectedObjects.Add(hitObject);
					node.isChecked = true; 
					
					
					ItemsWindow.Instance.enabled = true;
					ItemsWindow.Instance.FocusList(node);
				}
				
				UpdateGizmoState();
				return;
			}
		}
		ItemsWindow.Instance.UncheckAll();
		_selectedObjects.Clear();
		UpdateGizmoState();
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

    public void UpdateGizmoState(List<GameObject> selection)
    {
		_selectedObjects = selection;
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
	
	void OnMapLoaded(string mapName=""){
		//CenterCamera();
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
	
}