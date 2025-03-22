using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RustMapEditor.Variables;

public class MenuManager : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public Toggle lockToggle;
    public GameObject MenuPanel;
    public Button scaleButton;
    public Button closeButton;
    public GameObject confirmationPanel;
    public Button quitButton;
    public Button cancelButton;
	public Toggle templateTogglePrefab;
    private bool isScaling = false;
    private RectTransform menuRectTransform;
    private RectTransform confirmationRectTransform;
    private Canvas parentCanvas;

    public static MenuManager Instance { get; private set; }

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
	
	public void Hide()
	{
		if (MenuPanel != null)		{
			MenuPanel.SetActive(false);
		}
	}

	public void Show()
	{
		if (MenuPanel != null)		{
			MenuPanel.SetActive(true);
		}
	}

    private void Start()
    {
        GameObject canvasObj = GameObject.FindWithTag("AppCanvas");
        parentCanvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;
        if (parentCanvas == null) Debug.LogWarning("No Canvas with tag 'AppCanvas' found.");

        menuRectTransform = GetComponent<RectTransform>();
        confirmationRectTransform = confirmationPanel.GetComponent<RectTransform>();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ShowConfirmationPanel);
        }
        else
        {
            Debug.LogWarning("Close button is not assigned in the Inspector.");
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(CloseApplication);
        }
        else
        {
            Debug.LogWarning("Quit button is not assigned in the Inspector.");
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(HideConfirmationPanel);
        }
        else
        {
            Debug.LogWarning("Cancel button is not assigned in the Inspector.");
        }

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        LoadMenuState();
    }
	
	public void LoadTemplateTogglePrefab()
    {
        if (templateTogglePrefab == null)
        {
            templateTogglePrefab = Resources.Load<Toggle>("TemplateToggle");
            if (templateTogglePrefab == null)
            {
                Debug.LogError("");
            }
        }
    }

    public Vector3 GetMenuScale()
    {
        return menuRectTransform.localScale;
    }

    public void SaveMenuState()
    {
        SettingsManager.menuState = new MenuState(menuRectTransform.localScale, menuRectTransform.position);
        SettingsManager.SaveSettings();
    }

    public void LoadMenuState()
    {
        Vector3 loadedScale = SettingsManager.menuState.scale;
        Vector3 loadedPosition = SettingsManager.menuState.position;

        if (loadedPosition == Vector3.zero)
        {
            Vector3 parentOff = menuRectTransform.parent != null ? menuRectTransform.parent.position : Vector3.zero;
            loadedPosition = new Vector3(parentOff.x * 2, parentOff.y * 2, 0);
        }

        if (loadedScale == Vector3.zero || loadedScale.x <= 0 || loadedScale.y <= 0)
        {
            loadedScale = Vector3.one * 1.5f;
        }

        loadedScale.x = Mathf.Clamp(loadedScale.x, 1f, 3f);
        loadedScale.y = Mathf.Clamp(loadedScale.y, 1f, 3f);
        loadedScale.z = 1f;

        Vector3 parentOffset = menuRectTransform.parent != null ? menuRectTransform.parent.position : Vector3.zero;
        menuRectTransform.localScale = loadedScale;
        menuRectTransform.position = ClampToCanvas(loadedPosition - parentOffset) + parentOffset;
        if (Compass.Instance != null)
        {
            Compass.Instance.SyncScaleWithMenu();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!lockToggle.isOn)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(scaleButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
            {
                isScaling = true;
                ScaleMenu(eventData);
            }
            else
            {
                isScaling = false;
                Vector3 newPos = transform.localPosition + new Vector3(eventData.delta.x, eventData.delta.y, 0f);
                transform.localPosition = ClampToCanvas(newPos);
            }

            transform.SetAsLastSibling();
            MenuPanel.transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (lockToggle.isOn || eventData.button != PointerEventData.InputButton.Left)
            return;

        if (isScaling)
        {
            ScaleMenu(eventData);
            return;
        }

        Vector3 newPos = transform.localPosition + new Vector3(eventData.delta.x, eventData.delta.y, 0f);
        transform.localPosition = ClampToCanvas(newPos);
        SaveMenuState();
    }

    public void ScaleMenu(PointerEventData eventData)
    {
        float scaleChange = (eventData.delta.x * -0.004f) + (eventData.delta.y * -0.004f);
        Vector3 newScale = menuRectTransform.localScale + new Vector3(scaleChange, scaleChange, 0f);

        newScale.x = Mathf.Clamp(newScale.x, 1f, 3f);
        newScale.y = Mathf.Clamp(newScale.y, 1f, 3f);

        menuRectTransform.localScale = newScale;

        Vector3 adjustedScale = newScale - Vector3.one;
        if (AppManager.Instance != null)
        {
            AppManager.Instance.ScaleAllWindows(adjustedScale);
        }

        if (Compass.Instance != null)
        {
            Compass.Instance.SyncScaleWithMenu();
        }

        SaveMenuState();
    }

    public void ShowConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            confirmationRectTransform.localScale = menuRectTransform.localScale - Vector3.one;
            confirmationPanel.transform.SetAsLastSibling();
        }
    }

    public void HideConfirmationPanel()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }



public Toggle CreateCustomToggle(string iconPath = null)
{
    if (templateTogglePrefab == null)
    {
        LoadTemplateTogglePrefab();
        if (templateTogglePrefab == null)
        {
            Debug.LogError("Failed to load TemplateToggle prefab.");
            return null;
        }
    }

    // Instantiate the toggle under MenuPanel
    Toggle newToggle = Instantiate(templateTogglePrefab, MenuPanel.transform);

    // Position before menucontrols
    Transform menuControls = MenuPanel.transform.Find("menucontrols");
    if (menuControls != null)
    {
        newToggle.transform.SetSiblingIndex(menuControls.GetSiblingIndex());
    }
    else
    {
        Debug.LogWarning("No 'menucontrols' child found in MenuPanel; toggle added as last sibling.");
        newToggle.transform.SetAsLastSibling();
    }

    // Load and apply custom icon if iconPath is provided
    if (!string.IsNullOrEmpty(iconPath))
    {
        Texture2D texture = ModManager.LoadTexture(iconPath);
        if (texture != null)
        {
            Sprite iconSprite = ModManager.CreateSprite(texture);

            // Find Background and Checkmark images
            Image backgroundImage = newToggle.transform.Find("Background")?.GetComponent<Image>();
            Image checkmarkImage = newToggle.transform.Find("Background/Checkmark")?.GetComponent<Image>();

            if (backgroundImage == null || checkmarkImage == null)
            {
                Debug.LogError("Toggle prefab is missing Background or Checkmark Image components.");
                Destroy(newToggle.gameObject);
                return null;
            }

            // Assign the sprite
            backgroundImage.sprite = iconSprite;
            checkmarkImage.sprite = iconSprite;

            // Define and apply colors
            Color activeColor = new Color32(0x72, 0x8D, 0x44, 0xFF); // #728d44
            Color inactiveColor = new Color32(0x8F, 0x8F, 0x8F, 0xFF); // #d9d9d9

            backgroundImage.color = inactiveColor; // Inactive state
            checkmarkImage.color = activeColor;    // Active state

            // Sync visibility with toggle state
            newToggle.onValueChanged.AddListener((isOn) =>
            {
                checkmarkImage.enabled = isOn;
                backgroundImage.enabled = !isOn;
            });

            // Set initial state
            checkmarkImage.enabled = newToggle.isOn;
            backgroundImage.enabled = !newToggle.isOn;
        }
        else
        {
            Debug.LogError($"Failed to load texture from path: {iconPath}");
            // Still return the toggle with default prefab settings
        }
    }

    return newToggle;
}

// In MenuManager.cs
public Toggle CreateWindowToggle(string iconPath)
{
    
    // Use CreateCustomToggle with the icon path
    Toggle newToggle = CreateCustomToggle(iconPath);

    if (newToggle != null)
    {
        // Ensure the toggle is placed correctly in the MenuPanel hierarchy
        Transform menuControls = MenuPanel.transform.Find("menucontrols");
        if (menuControls != null)
        {
            newToggle.transform.SetSiblingIndex(menuControls.GetSiblingIndex()-1);
        }
        else
        {
            Debug.LogWarning("No 'menucontrols' child found in MenuPanel; toggle added as last sibling.");
            newToggle.transform.SetAsLastSibling();
        }
    }

    return newToggle;
}

    public void CloseApplication()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public Vector3 ClampToCanvas(Vector3 newPos)
    {
        if (parentCanvas == null) return newPos;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 size = menuRectTransform.sizeDelta * menuRectTransform.localScale;
        Vector2 canvasSize = canvasRect.sizeDelta;
		Vector2 halfCanvas = canvasSize * 0.5f;

        Vector2 halfSize = size * 0.5f; 
        Vector2 minPos = Vector2.zero; 
        Vector2 maxPos = canvasSize; 
		
        newPos.x = Mathf.Clamp(newPos.x, -minPos.x-halfCanvas.x+size.x, maxPos.x-halfCanvas.x);
        newPos.y = Mathf.Clamp(newPos.y, -minPos.y-halfCanvas.y+size.y, maxPos.y-halfCanvas.y);
        newPos.z = 0f;

        return newPos;
    }
}