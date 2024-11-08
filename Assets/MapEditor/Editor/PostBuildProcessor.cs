using UnityEditor;
using UnityEngine;
using System.IO;
using System;
public class PostBuildProcessor
{
    [MenuItem("Rust Map Editor/Build")]
    public static void BuildGame()
    {
        string buildPath = "Builds/RustMapper.exe";
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.Development);

        string appDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RustMapper");
		
        SettingsManager.CopyDirectory("Presets", Path.Combine(appDataPath, "Presets"));
        SettingsManager.CopyDirectory("Custom", Path.Combine(appDataPath, "Custom"));
        SettingsManager.CopyEditorSettings(Path.Combine(appDataPath, "EditorSettings.json"));
    }
	
	[MenuItem("Rust Map Editor/Build Release")]
	public static void BuildRelease()
    {
        string buildPath = "E:/RustMapper/RustMapper.exe";
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
		
        SettingsManager.CopyDirectory("Presets", "E:/RustMapper/Presets");
        SettingsManager.CopyDirectory("Custom", "E:/RustMapper/Custom");
        SettingsManager.CopyEditorSettings("E:/RustMapper/EditorSettings.json");
		RemoveDirectory("E:/RustMapper/RustMapper_BurstDebugInformation_DoNotShip");
    }
	
	public static void RemoveDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            Debug.LogError("Directory path is null or empty.");
            return;
        }

        if (Directory.Exists(directoryPath))
        {
            try
            {
                foreach (var file in Directory.GetFiles(directoryPath))                {
                    File.Delete(file);
                }
				
                foreach (var subDir in Directory.GetDirectories(directoryPath))                {
                    RemoveDirectory(subDir); 
                }

                // Finally, delete the directory itself
                Directory.Delete(directoryPath, true);
                Debug.Log($"Directory '{directoryPath}' and its contents have been deleted.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete directory '{directoryPath}'. Error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Directory '{directoryPath}' does not exist.");
        }
    }
	

}