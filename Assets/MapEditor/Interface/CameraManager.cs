using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RustMapEditor.Variables;
using UnityEngine.EventSystems;
using UIRecycleTreeNamespace;

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

			if (mouse.leftButton.isPressed)			{
				Vector2 mouseDelta = mouse.delta.ReadValue();
				Vector3 moveDirection = Vector3.zero;

				if (dragZarrow)				{
					Vector3 camViewOnX = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
					float moveAmountX = mouseDelta.x * 0.4f;

					if (!(Vector3.Dot(cam.transform.forward, Vector3.Cross(Vector3.up, Vector3.right)) < 0))
						moveAmountX = -moveAmountX;

					moveDirection = Vector3.right * moveAmountX;
					sync = true;
				}

				if (dragYarrow)				{
					float moveAmountY = mouseDelta.y * 0.4f;

					if (!(Vector3.Dot(cam.transform.forward, Vector3.up) < 0))
						moveAmountY = -moveAmountY;

					moveDirection = Vector3.up * moveAmountY;
					sync = true;
				}

				if (dragXarrow)				{
					Vector3 camViewOnZ = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
					float moveAmountZ = mouseDelta.x * 0.4f;

					if (!(Vector3.Dot(cam.transform.forward, Vector3.Cross(Vector3.up, Vector3.forward)) < 0))
						moveAmountZ = -moveAmountZ;

					moveDirection = Vector3.forward * moveAmountZ;
					sync = true;
				}

				transformTool.transform.position += moveDirection;

				if (sync)
				{
					PrefabManager.SyncSelection(transformTool.transform);
					sync = false;
				}
			}

		if (!EventSystem.current.IsPointerOverGameObject()) {
			

			if (mouse.leftButton.wasPressedThisFrame) {
				DragTransform();
			}
			
			if (mouse.leftButton.wasReleasedThisFrame) {
				if (dragXarrow || dragYarrow || dragZarrow){
					dragXarrow = false;
					dragYarrow = false;
					dragZarrow = false;
				}
				else
				{
					SelectPrefab();
				}
			}

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
	
	void SelectPrefab()
	{
		Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			Transform hitTransform = hit.transform;
			Debug.LogError(hitTransform.tag);
			
			while (hitTransform.CompareTag("Untagged")){
				hitTransform = hitTransform.parent;
			}
			
			if (hitTransform.CompareTag("Prefab"))
			{
				PrefabDataHolder dataHolder = hitTransform.GetComponent<PrefabDataHolder>();
				Transform collectionParent = null;
				Transform current = hitTransform;

				while (current.parent != null)
				{
					current = current.parent;
					
					if (current.CompareTag("Collection"))
					{
						collectionParent = current;
						break;
					}
				}

				if (collectionParent != null)
				{
					Node selection = itemTree.rootNode.FindNodeByDataRecursive(collectionParent);
					if (selection != null)
					{
						selection.isSelected = true;
						itemTree.FocusOn(selection);
					}
					PrefabManager.SetSelection(collectionParent);
				}
				else
				{
					Node selection = itemTree.rootNode.FindNodeByDataRecursive(dataHolder);
					if (selection != null)
					{
						selection.isSelected = true;
						itemTree.FocusOn(selection);
					}
					PrefabManager.SetSelection(dataHolder);
				}
				return;
			}
		}

		PrefabManager.SetSelection();
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
		CenterCamera();
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