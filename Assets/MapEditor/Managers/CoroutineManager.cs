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
    }

    public static void CleanUp()
    {
        if (_instance != null)
        {
            _instance.StopAllCoroutines();
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
		
			if (Time.time >= nextCrumpetTime)
			{
				switch (currentStyle)
				{
					case 0: // disabled
						break;
					case 1:
						//ItemStylusMode();
						break;
					case 2:
						PaintBrushMode();
						break;
				}
				nextCrumpetTime = Time.time + 0.1f;
			}
			
		}

    }

    private void ItemStylusMode()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && RTGizmosEngine.Get.HoveredGizmo == null)
        {
            CameraManager.Instance.SelectPrefab();
        }
    }

    private void PaintBrushMode()
    {
		int layerMask = 1 << 10; // "Land" layer
		//layerMask = ~layerMask & ~(1 << 5); // Exclude UI layer 5
		//Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out hit, Mathf.Infinity, layerMask);
				
		RaycastHit hit;
        
		if (Physics.Raycast(cam.ScreenPointToRay(Mouse.current.position.ReadValue()), out hit, Mathf.Infinity, layerMask)){
			
			//if(MainScript.Instance.brushType == 4){

				
					TerrainManager.GetTerrainCoordinates(hit, MainScript.Instance.brushSize, out int numX, out int numY);
					
					if (mouseLeftClick.ReadValue<float>() > 0.5f)
					{
						MainScript.Instance.ModifyTerrain(numX, numY);
						MainScript.Instance.PreviewTerrain(numX, numY);
					}
					else
					{
						MainScript.Instance.PreviewTerrain(numX, numY);
					}
				
				return;
			//}
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
	
