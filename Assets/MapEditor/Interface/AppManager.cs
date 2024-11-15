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
    public Toggle lockToggle;

    private Dictionary<Toggle, GameObject> windowDictionary = new Dictionary<Toggle, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInit()
    {
		GameObject loadingObject = GameObject.FindGameObjectWithTag("loading");
        loadingObject.transform.SetAsLastSibling();
        loadingObject.SetActive(true);
		
        SceneController.InitializeScene();
        SettingsManager.RuntimeInit();
        TerrainManager.RuntimeInit();
        PrefabManager.RuntimeInit();
        PathManager.RuntimeInit();

        if (SettingsManager.application.loadbundleonlaunch)
            AssetManager.RuntimeInit();

        FilePreset application = SettingsManager.application;
        MapManager.CreateMap(application.newSize, application.newSplat, application.newBiome, application.newHeight * 1000f);
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

		if (windowToggles.Count > 0)
		{
			windowToggles[0].isOn = true;
			windowPanels[0].SetActive(true);

			if (RecycleTrees.Count > 0 && RecycleTrees[0] != null)
			{
				RecycleTrees[0].gameObject.SetActive(true);
			}
		}
	}
	
	private void CloseWindow(int index)
    {
        if (index >= 0 && index < windowPanels.Count)        {
            windowPanels[index].SetActive(false);
            windowToggles[index].SetIsOnWithoutNotify(false);

            if (index < RecycleTrees.Count && RecycleTrees[index] != null)            {
                RecycleTrees[index].gameObject.SetActive(false);
            }
        }
    }
	

	private void OnWindowToggle(Toggle windowToggle, GameObject windowPanel)
	{
		bool windowState = windowToggle.isOn;
		windowPanel.SetActive(windowState);
		windowPanel.transform.SetAsLastSibling();
		
		int index = windowPanels.IndexOf(windowPanel);
		if (index >= 0 && index < RecycleTrees.Count && RecycleTrees[index] != null)
		{
			RecycleTrees[index].gameObject.SetActive(windowState);

			if (windowState)		{				
				RecycleTrees[index].transform.SetAsLastSibling();
			}
			
		}

		SettingsManager.SaveSettings();
	}

		public void LockWindows()    {
			lockToggle.targetGraphic.enabled = !lockToggle.isOn;
			SettingsManager.SaveSettings();
		}
	}