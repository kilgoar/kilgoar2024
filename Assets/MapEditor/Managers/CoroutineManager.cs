using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using RTG;

public class CoroutineManager : MonoBehaviour
{
    private int currentStyle;
    private static CoroutineManager _instance;
    private bool _isInitialized = false;
    private float nextCrumpetTime = 0f;
    private List<GameObject> _selectedObjects = new List<GameObject>();
    private Camera cam;
    private Texture2D pointerTexture;
    private Texture2D paintBrushTexture;
    private InputAction mouseLeftClick;
    private int heightTool;
	private int layerMask = 1 << 10; // "Land" layer
	private RaycastHit hit;
	
    public static CoroutineManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CoroutineManager");
                _instance = go.AddComponent<CoroutineManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError("Destroying duplicate CoroutineManager");
            Destroy(gameObject);
            return;
        }
        _instance = this;
        _isInitialized = true;
        cam = CameraManager.Instance.cam;
    }

    private void Start()
    {
        mouseLeftClick = new InputAction("MouseLeftClick", InputActionType.Button, "<Mouse>/leftButton");
        mouseLeftClick.Enable();
        mouseLeftClick.AddBinding("<Mouse>/leftButton");
        ChangeStylus(1);
    }

    public bool isInitialized => _isInitialized;

    public void ResetStylus()
    {
        ChangeStylus(currentStyle);
    }

    public void SetHeightTool(int tool)
    {
        heightTool = tool;
    }

    public void ChangeStylus(int style)
    {
        currentStyle = style;
		//Debug.LogError(style + " " + currentStyle);
    }

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
		CleanUp();
	}

	public static void CleanUp()
	{
		if (_instance != null)
		{
			_instance.StopAllCoroutines();
			GameObject.Destroy(_instance.gameObject);
			_instance = null;
		}
	}

    private bool OverUI()
    {
		if(EventSystem.current.IsPointerOverGameObject())
		{
			return true;
		}
		
		return false;
    }

    private void Update()
    {
        if (!OverUI()){
				
				switch (currentStyle)
				{
					case 0: // disabled
						break;
					case 1:
						ItemStylusMode();
						break;
					case 2:
						if (Time.time >= nextCrumpetTime)	{
							PaintBrushMode();
							nextCrumpetTime = Time.time + 0.05f;
						}
						break;
				}

			}

    }

    private void ItemStylusMode()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && RTGizmosEngine.Get.HoveredGizmo == null)
        {
			if (Keyboard.current.altKey.isPressed) {
				if (HierarchyWindow.Instance!=null){
					if (Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out hit, Mathf.Infinity, layerMask)){
						HierarchyWindow.Instance.PlacePrefab(hit.point); 
					}

				}
			
			}
            CameraManager.Instance.SelectPrefab();
        }
    }

    private void PaintBrushMode()
    {
				
		
        
		if (Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out hit, Mathf.Infinity, layerMask)){


					TerrainManager.GetTerrainCoordinates(hit, MainScript.Instance.brushSize, out int numX, out int numY);
					
					
					if (mouseLeftClick.ReadValue<float>() > 0.5f)
					{
						if(MainScript.Instance.paintMode == -1){
							MainScript.Instance.ModifySplat(numX, numY);
						}
						else if (MainScript.Instance.paintMode == -3){
							MainScript.Instance.ModifyAlpha(numX, numY);
						}
						else if (MainScript.Instance.paintMode == -2){
							MainScript.Instance.ModifyTopology(numX, numY);
						}
						else {	MainScript.Instance.ModifyTerrain(numX, numY); }
						
						MainScript.Instance.PreviewTerrain(numX, numY);
					}
					else
					{
						MainScript.Instance.PreviewTerrain(numX, numY);
					}
				
				return;

		}
    }
	
	public void StopRuntimeCoroutine(Coroutine coroutine)
	{
		if (coroutine != null)
		{
			StopCoroutine(coroutine);
		}
	}
		
	public Coroutine StartRuntimeCoroutine(IEnumerator coroutine)
    {
        if (coroutine == null)
        {
            Debug.LogError("Coroutine is null!");
            return null;
        }
        return StartCoroutine(coroutine);
    }
}
	
