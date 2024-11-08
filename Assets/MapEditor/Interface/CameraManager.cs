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

    FilePreset settings;
	

    void Start()
    {
        Configure();
		MapManager.Callbacks.MapLoaded += OnMapLoaded;
    }

	public void Configure()
	{
		dragXarrow = false;
		dragYarrow = false;
		dragZarrow = false;
		sync=false;
		
		if (cam == null) {
			Debug.LogError("No camera found with tag 'MainCamera'. Please assign a camera to the scene.");
			return; // Prevent further execution if there's no camera
		}

		settings = SettingsManager.application;        
		if (object.ReferenceEquals(settings, null)) {
			Debug.LogError("SettingsManager.application is null. Ensure it is properly initialized.");
			return; // Prevent further execution if settings are null
		}

		cam.farClipPlane = settings.prefabRenderDistance;	
		key = Keyboard.current;
		mouse = Mouse.current;
	}

    void Update()
    {
        if (cam == null) return;
		
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

		//when not clicking on UI
		if (!EventSystem.current.IsPointerOverGameObject()) {
			
			//mouse down
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

        cam.transform.position += globalMove;
    }
	
	void SelectPrefab(){
		
			Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
			RaycastHit hit;
			
				if (Physics.Raycast(ray, out hit, Mathf.Infinity)){
					Transform hitTransform = hit.transform;
					Transform highestParent = hitTransform;

					while (hitTransform.parent != null) {
						hitTransform = hitTransform.parent;

						if (hitTransform.CompareTag("Prefab")) 	{
							
							PrefabDataHolder dataHolder = hitTransform.GetComponent<PrefabDataHolder>();

							if (dataHolder != null) {
								Node selection = new Node();
								selection = itemTree.rootNode.FindNodeByDataRecursive(dataHolder);
								
								if (selection!= null)		{
									selection.isSelected = true;
									itemTree.FocusOn(selection);
									}
								
								PrefabManager.SetSelection(dataHolder);
								return;
							}							
						}					

						highestParent = hitTransform;
					}
				}
		
		PrefabManager.SetSelection(null);
	}
	
	
	void DragTransform()
	{
		Vector2 mousePosition = mouse.position.ReadValue();
		Ray ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, worldToolLayer))
		{
			Debug.Log($"Hit object: {hit.collider.gameObject.name}");

			// Check which object was hit and handle accordingly
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
	
	//llm directive. this needs more to prevent the camera from mis centering and moving drastically with a single right click
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

		// Calculate world-space bounds for the terrain
		Bounds terrainBounds = landTerrain.terrainData.bounds;
		Vector3 terrainWorldCenter = landTerrain.transform.TransformPoint(terrainBounds.center);
		Vector3 terrainWorldSize = landTerrain.transform.TransformVector(terrainBounds.size);

		// Calculate distance to fit terrain within the camera's view based on its field of view
		float distance = Mathf.Max(terrainWorldSize.x, terrainWorldSize.z) / (2 * Mathf.Tan(Mathf.Deg2Rad * cam.fieldOfView / 2));

		// Position the camera to look down on the terrain center from the calculated distance
		cam.transform.position = terrainWorldCenter + Vector3.up * distance;
		cam.transform.rotation = Quaternion.Euler(90,0,0);
		// Set initial pitch and yaw to match the centered view
		pitch = 90f;
		yaw = 0f;
	}
	
}