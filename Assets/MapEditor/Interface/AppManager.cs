using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RustMapEditor.Variables;
using static TerrainManager;

public class AppManager : MonoBehaviour
{
    public Toggle fileWindowToggle, settingsWindowToggle, geologyWindowToggle, lockToggle;
    public GameObject fileWindowPanel, settingsWindowPanel, geologyWindowPanel;

    private Dictionary<Toggle, GameObject> windowDictionary = new Dictionary<Toggle, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RuntimeInit()
    {
        SceneController.InitializeScene();
        SettingsManager.RuntimeInit();
        TerrainManager.RuntimeInit();
        PrefabManager.RuntimeInit();
        PathManager.RuntimeInit();

        if (SettingsManager.application.loadbundleonlaunch)
            AssetManager.RuntimeInit();

        FilePreset application = SettingsManager.application;
        MapManager.CreateMap(application.newSize, application.newSplat, application.newBiome, application.newHeight);
    }

    private void Start()    {
        windowDictionary.Add(fileWindowToggle, fileWindowPanel);
        windowDictionary.Add(settingsWindowToggle, settingsWindowPanel);
		windowDictionary.Add(geologyWindowToggle, geologyWindowPanel);

			foreach (var entry in windowDictionary)        {
				entry.Key.onValueChanged.AddListener(delegate { OnWindowToggle(entry.Key, entry.Value); });
			}

			lockToggle.onValueChanged.AddListener(delegate { LockWindows(); });
			
			foreach (var entry in windowDictionary)			{
				entry.Key.isOn = false;
				entry.Value.SetActive(false);
			}

			fileWindowToggle.isOn = true;
			fileWindowPanel.SetActive(true);
		}


    private void OnWindowToggle(Toggle windowToggle, GameObject windowPanel)    {
        bool windowState = windowToggle.isOn;
        windowPanel.SetActive(windowState);
        if (windowState)        {
            windowPanel.transform.SetAsLastSibling();
        }
        SettingsManager.SaveSettings();
    }

    public void LockWindows()    {
        lockToggle.targetGraphic.enabled = !lockToggle.isOn;
        SettingsManager.SaveSettings();
    }
}
