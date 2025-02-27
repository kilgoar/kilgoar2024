using System.Collections.Generic;
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
	private List<InputField> allInputFields = new List<InputField>();
	public GameObject menuPanel;
    public Toggle lockToggle;

    private Dictionary<Toggle, GameObject> windowDictionary = new Dictionary<Toggle, GameObject>();

	public static AppManager Instance { get; private set; }
	
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
	
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void RuntimeInit()
    {
		//GameObject loadingObject = GameObject.FindGameObjectWithTag("loading");
        //loadingObject.transform.SetAsLastSibling();
        //loadingObject.SetActive(true);
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

        for (int i = 0; i < CloseButtons.Count; i++)
        {
            int index = i;
            CloseButtons[i].onClick.AddListener(() => CloseWindow(index));
        }

		CollectInputFields();
		
		
	}
	
	public void ActivateWindow(int index){
		windowToggles[index].isOn = true;
		windowPanels[index].SetActive(true);
		RecycleTrees[index].gameObject.SetActive(true);
				
                RectTransform windowRect = windowPanels[index].GetComponent<RectTransform>();
                Vector3 menuScale = menuPanel.GetComponent<RectTransform>().localScale;
                Vector3 adjustedScale = menuScale - Vector3.one;
				
				adjustedScale.x = Mathf.Clamp(adjustedScale.x, .6f, 3f);
				adjustedScale.y = Mathf.Clamp(adjustedScale.y, .6f, 3f);

                if (windowRect != null)
                {
                    windowRect.localScale = adjustedScale;
                }

                if (index < RecycleTrees.Count && RecycleTrees[index] != null)
                {
                    RectTransform treeRect = RecycleTrees[index].GetComponent<RectTransform>();
                    if (treeRect != null)
                    {
                        treeRect.localScale = adjustedScale;
                    }
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
				Debug.Log("okay"); // Adjusted from Debug.LogError to Debug.Log since it’s not an error
				RecycleTrees[index].transform.SetAsLastSibling();

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

		SettingsManager.SaveSettings();
	}
	
	public void LockWindows()    {
			lockToggle.targetGraphic.enabled = !lockToggle.isOn;
			SettingsManager.SaveSettings();
	}


	}