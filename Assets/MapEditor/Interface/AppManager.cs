using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using RustMapEditor.Variables;
using UIRecycleTreeNamespace;
using static TerrainManager;

public class AppManager : MonoBehaviour
{
    public List<Toggle> windowToggles = new List<Toggle>();
    public List<GameObject> windowPanels = new List<GameObject>();
	public List<UIRecycleTree> RecycleTrees = new List<UIRecycleTree>();
	public List<Button> CloseButtons = new List<Button>();
	public List<InputField> allInputFields = new List<InputField>();
	public GameObject menuPanel;
    public Toggle lockToggle;
	public string harmonyMessage;

    public Dictionary<Toggle, GameObject> windowDictionary = new Dictionary<Toggle, GameObject>();

    public TemplateWindow templateWindowPrefab;
	public Canvas uiCanvas;
	public static AppManager Instance { get; private set; }
	
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;            
            DontDestroyOnLoad(gameObject); 
			LoadTemplatePrefab();
        }
        else
        {
            Destroy(gameObject);
        }
    }
	
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void RuntimeInit()
    {
		HarmonyLoader.DeleteLog();
		Instance.harmonyMessage = HarmonyLoader.LoadHarmonyMods(Path.Combine(SettingsManager.AppDataPath(), "HarmonyMods"));
		Instance.harmonyMessage += "\n" + HarmonyLoader.LoadHarmonyMods(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "HarmonyMods"));
		
		
		LoadScreen.Instance.Show();
		LoadScreen.Instance.transform.SetAsLastSibling();

		
        SceneController.InitializeScene();
        SettingsManager.RuntimeInit();
        TerrainManager.RuntimeInit();
        PrefabManager.RuntimeInit();
        PathManager.RuntimeInit();
		AreaManager.Initialize();

        if (SettingsManager.application.loadbundleonlaunch)
            AssetManager.RuntimeInit();

        FilePreset application = SettingsManager.application;

    }

	private void Start()
	{
		if (windowToggles.Count != windowPanels.Count)
		{
			Debug.LogError("The number of window toggles and window panels must match.");
			return;
		}

		for (int i = 0; i < windowToggles.Count; i++)
		{
			windowDictionary.Add(windowToggles[i], windowPanels[i]);
		}

		foreach (var entry in windowDictionary)
		{
			entry.Key.onValueChanged.AddListener(delegate { OnWindowToggle(entry.Key, entry.Value); });
		}

		lockToggle.onValueChanged.AddListener(delegate { LockWindows(); });

		/*
		foreach (var entry in windowDictionary)
		{
			entry.Key.isOn = false;
			entry.Value.SetActive(false);

			int index = windowPanels.IndexOf(entry.Value);
			if (index >= 0 && index < RecycleTrees.Count && RecycleTrees[index] != null)
			{
				RecycleTrees[index].gameObject.SetActive(false);
			}
		}
		*/
		
        for (int i = 0; i < CloseButtons.Count; i++)
        {
            int index = i;
            CloseButtons[i].onClick.AddListener(() => CloseWindow(index));
        }

		CollectInputFields();
		
		
	}
	
	public void SaveWindowStates()
    {
        WindowState[] states = new WindowState[windowPanels.Count];
        for (int i = 0; i < windowPanels.Count; i++)
        {
            RectTransform rect = windowPanels[i].GetComponent<RectTransform>();
            states[i] = new WindowState(
                windowPanels[i].activeSelf,
                rect.localPosition,
                rect.localScale
            );
        }
        SettingsManager.windowStates = states;
        SettingsManager.SaveSettings();
    }

	public void LoadWindowStates()
	{
		if (SettingsManager.windowStates == null || SettingsManager.windowStates.Length != windowPanels.Count)
		{
			Debug.LogWarning("Window states not initialized or mismatch in count. Using defaults.");
			return;
		}

		for (int i = 0; i < windowPanels.Count; i++)
		{
			// Skip activation for these windows
			bool isRestrictedWindow = (i == 6 || i == 9);

			RectTransform rect = windowPanels[i].GetComponent<RectTransform>();
			if (rect == null)
			{
				Debug.LogWarning($"Window panel at index {i} has no RectTransform. Skipping.");
				continue;
			}

			WindowState state = SettingsManager.windowStates[i];

			// Only set active state if not a restricted window
			if (!isRestrictedWindow)
			{
				windowPanels[i].SetActive(state.isActive);
			}
			else
			{
				windowPanels[i].SetActive(false); // Ensure restricted windows stay inactive
			}

			// Apply loaded scale and position
			rect.localScale = state.scale;
			rect.localPosition = state.position; // THIS WAS MISSING!

			// Reflect toggle state without triggering callbacks
			windowToggles[i].SetIsOnWithoutNotify(state.isActive && !isRestrictedWindow);

			// Sync and load the associated RecycleTree
			if (i < RecycleTrees.Count && RecycleTrees[i] != null)
			{
				RecycleTrees[i].gameObject.SetActive(state.isActive && !isRestrictedWindow);
				RectTransform treeRect = RecycleTrees[i].GetComponent<RectTransform>();
				if (treeRect != null)
				{
					treeRect.localScale = state.scale;
					treeRect.localPosition = rect.localPosition; // Sync position locally
				}
			}

			// Set the window as the last sibling after loading its state
			windowPanels[i].transform.SetAsLastSibling();
		}

		// Ensure menu stays on top
		if (menuPanel != null)
		{
			menuPanel.transform.SetAsLastSibling();
		}

		Debug.Log("Window states loaded and applied.");
	}

	public void DeactivateWindow(int index)
    {
        if (index < 0 || index >= windowToggles.Count || index >= windowPanels.Count)
        {
            Debug.LogWarning($"Invalid window index: {index}. Must be within bounds of windowToggles and windowPanels arrays.");
            return;
        }

        if (windowToggles[index] == null || windowPanels[index] == null)
        {
            Debug.LogWarning($"Null reference found at index {index}: windowToggles or windowPanels is null.");
            return;
        }

        windowToggles[index].SetIsOnWithoutNotify(false); // Update toggle without triggering callback
        windowPanels[index].SetActive(false);

        // Deactivate associated RecycleTree if it exists
        if (index < RecycleTrees.Count && RecycleTrees[index] != null)
        {
            RecycleTrees[index].gameObject.SetActive(false);
        }

        SaveWindowStates(); // Persist the state change
    }
		
    public void ActivateWindow(int index)
    {
        if (index < 0 || index >= windowToggles.Count || index >= windowPanels.Count)
        {
            Debug.LogWarning($"Invalid window index: {index}. Must be within bounds of windowToggles and windowPanels arrays.");
            return;
        }

        if (windowToggles[index] == null || windowPanels[index] == null)
        {
            Debug.LogWarning($"Null reference found at index {index}: windowToggles or windowPanels is null.");
            return;
        }

        windowToggles[index].isOn = true;
        windowPanels[index].SetActive(true);

        RectTransform windowRect = windowPanels[index].GetComponent<RectTransform>();
        if (windowRect == null)
        {
            Debug.LogWarning($"Window panel at index {index} has no RectTransform component.");
            return;
        }

        if (menuPanel == null)
        {
            Debug.LogWarning("menuPanel is null. Cannot adjust scale.");
            return;
        }

        RectTransform menuRect = menuPanel.GetComponent<RectTransform>();
        if (menuRect == null)
        {
            Debug.LogWarning("menuPanel has no RectTransform component. Cannot adjust scale.");
            return;
        }

        Vector3 menuScale = menuRect.localScale;
        Vector3 adjustedScale = menuScale - Vector3.one;

        adjustedScale.x = Mathf.Clamp(adjustedScale.x, 0.6f, 3f);
        adjustedScale.y = Mathf.Clamp(adjustedScale.y, 0.6f, 3f);
        adjustedScale.z = Mathf.Clamp(adjustedScale.z, 0.6f, 3f);

        windowRect.localScale = adjustedScale;

        ActivateRecycleTree(index, adjustedScale);
    }

	private void ActivateRecycleTree(int index, Vector3 adjustedScale)
	{
		// Validate RecycleTrees list and index
		if (RecycleTrees == null || index < 0 || index >= RecycleTrees.Count)
		{
			// Silently return since RecycleTrees are optional
			return;
		}

		// Check if the RecycleTree at index exists
		if (RecycleTrees[index] == null)
		{
			// Silently return since it's fine for RecycleTrees to not exist
			return;
		}

		// Activate the RecycleTree GameObject
		RecycleTrees[index].gameObject.SetActive(true);

		// Safely handle RecycleTree scaling
		RectTransform treeRect = RecycleTrees[index].GetComponent<RectTransform>();
		if (treeRect != null)
		{
			treeRect.localScale = adjustedScale;
		}
		else
		{
			Debug.LogWarning($"RecycleTree at index {index} has no RectTransform component.");
		}
	}
		
	public void CloseWindow(int index)
    {
        if (index >= 0 && index < windowPanels.Count)        {
            windowPanels[index].SetActive(false);
            windowToggles[index].SetIsOnWithoutNotify(false);

            if (index < RecycleTrees.Count && RecycleTrees[index] != null)            {
                RecycleTrees[index].gameObject.SetActive(false);
            }
        }
    }
	
	public void CollectInputFields()
    {
        foreach (var panel in windowPanels)
        {
            if (panel != null)
            {
                InputField[] inputFields = panel.GetComponentsInChildren<InputField>(true);
                allInputFields.AddRange(inputFields);
            }
        }
    }

    // Method to check if any input field is active
    public bool IsAnyInputFieldActive()
    {
        foreach (var field in allInputFields)
        {
            if (field != null && field.isFocused)
            {
                return true;
            }
        }
        return false;
    }
	
	public void ScaleAllWindows(Vector3 scale)
	{
		// Clamp the scale values to the range 0.6f to 3f
		Vector3 clampedScale = scale;
		clampedScale.x = Mathf.Clamp(clampedScale.x, 0.6f, 3f);
		clampedScale.y = Mathf.Clamp(clampedScale.y, 0.6f, 3f);

		foreach (GameObject window in windowPanels)
		{
			if (window != null && window.activeInHierarchy) // Only scale active windows
			{
				RectTransform rect = window.GetComponent<RectTransform>();
				if (rect != null)
				{
					rect.localScale = clampedScale;
				}
			}
		}

		// Optionally scale associated RecycleTrees if they're active
		for (int i = 0; i < RecycleTrees.Count; i++)
		{
			if (i < windowPanels.Count && windowPanels[i].activeInHierarchy && RecycleTrees[i] != null)
			{
				RectTransform treeRect = RecycleTrees[i].GetComponent<RectTransform>();
				if (treeRect != null)
				{
					treeRect.localScale = clampedScale;
				}
			}
		}
		SaveWindowStates();
	}

	public void OnWindowToggle(Toggle windowToggle, GameObject windowPanel)
	{
		bool windowState = windowToggle.isOn;
		windowPanel.SetActive(windowState);
		windowPanel.transform.SetAsLastSibling();

		int index = windowPanels.IndexOf(windowPanel);

		// Apply menu's adjusted scale (menu scale - 1) to the window if activated
		if (windowState && menuPanel != null)
		{
			RectTransform windowRect = windowPanel.GetComponent<RectTransform>();
			Vector3 menuScale = menuPanel.GetComponent<RectTransform>().localScale;
			Vector3 adjustedScale = menuScale - Vector3.one;

			adjustedScale.x = Mathf.Clamp(adjustedScale.x, 0.6f, 3f);
			adjustedScale.y = Mathf.Clamp(adjustedScale.y, 0.6f, 3f);

			if (windowRect != null)
			{
				windowRect.localScale = adjustedScale;
			}
		}

		// Handle RecycleTree if it exists
		if (index >= 0 && index < RecycleTrees.Count && RecycleTrees[index] != null)
		{
			RecycleTrees[index].gameObject.SetActive(windowState);

			if (windowState)
			{
				int windowSiblingIndex = windowPanel.transform.GetSiblingIndex();
				RecycleTrees[index].transform.SetSiblingIndex(windowSiblingIndex + 1);

				// Apply the adjusted scale to the tree
				if (menuPanel != null)
				{
					RectTransform treeRect = RecycleTrees[index].GetComponent<RectTransform>();
					Vector3 menuScale = menuPanel.GetComponent<RectTransform>().localScale;
					Vector3 adjustedScale = menuScale - Vector3.one;

					adjustedScale.x = Mathf.Clamp(adjustedScale.x, 0.6f, 3f);
					adjustedScale.y = Mathf.Clamp(adjustedScale.y, 0.6f, 3f);

					if (treeRect != null)
					{
						treeRect.localScale = adjustedScale;
					}
				}
			}
		}
		if (menuPanel != null)
		{
			menuPanel.transform.SetAsLastSibling();
		}
		SaveWindowStates();
		SettingsManager.SaveSettings();
	}
	
	public void LockWindows()    {
			lockToggle.targetGraphic.enabled = !lockToggle.isOn;
			SettingsManager.SaveSettings();
	}

	public void LoadTemplatePrefab()
    {
        if (templateWindowPrefab == null)
        {
            templateWindowPrefab = Resources.Load<TemplateWindow>("TemplateWindow");
            if (templateWindowPrefab == null)
            {
                Debug.LogError("Failed to load TemplateWindow prefab");
            }
        }
    }

     public TemplateWindow CreateWindow(string titleText, Rect rect)
    {
        if (Instance == null)
        {
            Debug.LogError("AppManager Instance is not initialized.");
            return null;
        }

        if (templateWindowPrefab == null)
        {
            LoadTemplatePrefab();
            if (templateWindowPrefab == null)
            {
                return null;
            }
        }

        if (uiCanvas == null)
        {
            Debug.LogError("uiCanvas is not assigned in AppManager. Cannot create window.");
            return null;
        }

        // Instantiate the window under the child Canvas
        TemplateWindow newWindow = Instantiate(templateWindowPrefab, uiCanvas.transform);
        RectTransform windowRect = newWindow.GetComponent<RectTransform>();

        // Clean up non-essential children
        CleanUpNonEssentialChildren(newWindow);

        // Configure the remaining essential components
        newWindow.title.text = titleText;
        windowRect.anchoredPosition = new Vector2(rect.x, rect.y);
        windowRect.sizeDelta = new Vector2(rect.width, rect.height);

        if (menuPanel != null)
        {
            Vector3 menuScale = menuPanel.GetComponent<RectTransform>().localScale;
            Vector3 adjustedScale = menuScale - Vector3.one;
            adjustedScale.x = Mathf.Clamp(adjustedScale.x, 0.6f, 3f);
            adjustedScale.y = Mathf.Clamp(adjustedScale.y, 0.6f, 3f);
            windowRect.localScale = adjustedScale;
        }
        else
        {
            windowRect.localScale = Vector3.zero;
        }

        newWindow.gameObject.SetActive(false);

        // Add WindowManager component and configure it
        WindowManager windowManager = newWindow.gameObject.AddComponent<WindowManager>();
        ConfigureWindowManager(windowManager, newWindow);

        // Create a toggle in MenuManager and replace the window's toggle
        if (MenuManager.Instance != null)
        {
            Toggle newToggle = MenuManager.Instance.CreateWindowToggle();
            if (newToggle != null)
            {
                newWindow.toggle = newToggle;
            }
            else
            {
                Debug.LogWarning("Failed to create toggle for window in MenuManager.");
            }
        }
        else
        {
            Debug.LogWarning("MenuManager Instance is not available to create window toggle.");
        }

        RegisterWindow(newWindow);

        return newWindow;
    }

    // New method to remove non-essential children
    private void CleanUpNonEssentialChildren(TemplateWindow newWindow)
    {
        // Define essential GameObjects to keep
        List<GameObject> essentialObjects = new List<GameObject>
        {
            newWindow.gameObject,           // The panel (root)
            newWindow.title.gameObject,     // The title Text
            newWindow.close.gameObject,      // The close Button
			newWindow.footer.gameObject,
			newWindow.rescale.gameObject
        };

        // Get all children and destroy non-essential ones
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in newWindow.transform)
        {
            if (!essentialObjects.Contains(child.gameObject))
            {
                childrenToDestroy.Add(child.gameObject);
            }
        }

        foreach (GameObject child in childrenToDestroy)
        {
            Destroy(child);
        }
    }

    // Updated method to configure the WindowManager
    private void ConfigureWindowManager(WindowManager windowManager, TemplateWindow newWindow)
    {
        windowManager.WindowsPanel = newWindow.gameObject; // The window itself is the panel
        windowManager.Window = newWindow.gameObject;       // Same GameObject for simplicity
        windowManager.lockToggle = lockToggle;             // Use AppManager's lockToggle

        // Get the RectTransform for positioning
        RectTransform windowRect = newWindow.GetComponent<RectTransform>();

        // Use the close button as rescaleButton, or create a new one if needed
        windowManager.rescaleButton = newWindow.rescale;

        // Tree remains null unless specified
        windowManager.Tree = null;
    }

    public void RegisterWindow(TemplateWindow window)
    {
        if (window.toggle == null || window.close == null)
        {
            Debug.LogError("TemplateWindow prefab is missing required components (Toggle or Close Button).");
            return;
        }

        windowToggles.Add(window.toggle);
        windowPanels.Add(window.gameObject);
        CloseButtons.Add(window.close);

        windowDictionary.Add(window.toggle, window.gameObject);
        window.toggle.onValueChanged.AddListener(delegate { OnWindowToggle(window.toggle, window.gameObject); });

        int index = CloseButtons.Count - 1;
        CloseButtons[index].onClick.AddListener(() => CloseWindow(index));

        InputField[] inputFields = window.gameObject.GetComponentsInChildren<InputField>(true);
        allInputFields.AddRange(inputFields);

        SaveWindowStates();

        if (menuPanel != null)
        {
            menuPanel.transform.SetAsLastSibling();
        }
    }

    // UI Element Creation Methods
    public Toggle CreateToggle(Transform parent, Rect rect, string text = "")
    {
        if (templateWindowPrefab == null || templateWindowPrefab.toggle == null)
        {
            Debug.LogError("TemplateWindow prefab or its Toggle is not available.");
            return null;
        }
        Toggle newToggle = Instantiate(templateWindowPrefab.toggle, parent);
        RectTransform toggleRect = newToggle.GetComponent<RectTransform>();
        toggleRect.anchoredPosition = new Vector2(rect.x, rect.y);
        toggleRect.sizeDelta = new Vector2(rect.width, rect.height);

        // Set the label if it exists as a child
        Text label = newToggle.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = text;
        }
        else if (!string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Toggle prefab has no child Text component to set label.");
        }

        return newToggle;
    }

    public Button CreateButton(Transform parent, Rect rect, string text = "")
    {
        if (templateWindowPrefab == null || templateWindowPrefab.button == null)
        {
            Debug.LogError("TemplateWindow prefab or its default Button is not available.");
            return null;
        }
        Button newButton = Instantiate(templateWindowPrefab.button, parent);
        RectTransform buttonRect = newButton.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(rect.x, rect.y);
        buttonRect.sizeDelta = new Vector2(rect.width, rect.height);

        // Set the label if it exists as a child
        Text label = newButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = text;
        }
        else if (!string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Button prefab has no child Text component to set label.");
        }

        return newButton;
    }

    public Button CreateBrightButton(Transform parent, Rect rect, string text = "")
    {
        if (templateWindowPrefab == null || templateWindowPrefab.buttonbright == null)
        {
            Debug.LogError("TemplateWindow prefab or its Bright Button is not available.");
            return null;
        }
        Button newButton = Instantiate(templateWindowPrefab.buttonbright, parent);
        RectTransform buttonRect = newButton.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(rect.x, rect.y);
        buttonRect.sizeDelta = new Vector2(rect.width, rect.height);

        // Set the label if it exists as a child
        Text label = newButton.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = text;
        }
        else if (!string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Bright Button prefab has no child Text component to set label.");
        }

        return newButton;
    }

    public Text CreateLabelText(Transform parent, Rect rect, string text = "")
    {
        if (templateWindowPrefab == null || templateWindowPrefab.label == null)
        {
            Debug.LogError("TemplateWindow prefab or its Label Text is not available.");
            return null;
        }
        Text newText = Instantiate(templateWindowPrefab.label, parent);
        RectTransform textRect = newText.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(rect.x, rect.y);
        textRect.sizeDelta = new Vector2(rect.width, rect.height);
        newText.text = text;
        return newText;
    }

    public Slider CreateSlider(Transform parent, Rect rect)
    {
        if (templateWindowPrefab == null || templateWindowPrefab.slider == null)
        {
            Debug.LogError("TemplateWindow prefab or its Slider is not available.");
            return null;
        }
        Slider newSlider = Instantiate(templateWindowPrefab.slider, parent);
        RectTransform sliderRect = newSlider.GetComponent<RectTransform>();
        sliderRect.anchoredPosition = new Vector2(rect.x, rect.y);
        sliderRect.sizeDelta = new Vector2(rect.width, rect.height);
        return newSlider;
    }

    public Dropdown CreateDropdown(Transform parent, Rect rect)
    {
        if (templateWindowPrefab == null || templateWindowPrefab.dropdown == null)
        {
            Debug.LogError("TemplateWindow prefab or its Dropdown is not available.");
            return null;
        }
        Dropdown newDropdown = Instantiate(templateWindowPrefab.dropdown, parent);
        RectTransform dropdownRect = newDropdown.GetComponent<RectTransform>();
        dropdownRect.anchoredPosition = new Vector2(rect.x, rect.y);
        dropdownRect.sizeDelta = new Vector2(rect.width, rect.height);
        return newDropdown;
    }

    public InputField CreateInputField(Transform parent, Rect rect, string text = "")
    {
        if (templateWindowPrefab == null || templateWindowPrefab.inputField == null)
        {
            Debug.LogError("TemplateWindow prefab or its InputField is not available.");
            return null;
        }
        InputField newInputField = Instantiate(templateWindowPrefab.inputField, parent);
        RectTransform inputRect = newInputField.GetComponent<RectTransform>();
        inputRect.anchoredPosition = new Vector2(rect.x, rect.y);
        inputRect.sizeDelta = new Vector2(rect.width, rect.height);
        newInputField.text = text;
        allInputFields.Add(newInputField);
        return newInputField;
    }



}