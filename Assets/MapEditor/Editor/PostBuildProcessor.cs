using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Diagnostics;
public class PostBuildProcessor
{
    [MenuItem("Rust Map Editor/Build")]
    public static void BuildGame()
    {
        string buildPath = "Builds/RustMapper.exe";
		
		BuildPipeline.BuildPlayer(
			EditorBuildSettings.scenes, 
			buildPath, 
			BuildTarget.StandaloneWindows64, 
			BuildOptions.Development | BuildOptions.ConnectWithProfiler
		);

        string appDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RustMapper");
		
        SettingsManager.CopyDirectory("Presets", Path.Combine(appDataPath, "Presets"));
        SettingsManager.CopyDirectory("Custom", Path.Combine(appDataPath, "Custom"));
        SettingsManager.CopyEditorSettings(Path.Combine(appDataPath, "EditorSettings.json"));
    }
	
	[MenuItem("Rust Map Editor/Build Release")]
	public static void BuildRelease()
    {
		RemoveDirectory("E:/RustMapper");
        string buildPath = "E:/RustMapper/RustMapper.exe";
		string releasePath = "E:/RustMapper";
        string presetsPath = Path.Combine(releasePath, "Presets");
        string customPath = Path.Combine(releasePath, "Custom");
        string editorSettingsPath = Path.Combine(releasePath, "EditorSettings.json");
		
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
		
        SettingsManager.CopyDirectory("Presets", "E:/RustMapper/Presets");
        SettingsManager.CopyDirectory("Custom", "E:/RustMapper/Custom");
		SettingsManager.CopyDirectory("HarmonyMods", "E:/RustMapper/HarmonyMods");
        SettingsManager.CopyEditorSettings("E:/RustMapper/EditorSettings.json");
		
		// Hide default settings and so on
        try
        {
            if (Directory.Exists(presetsPath))
            {
                DirectoryInfo di = new DirectoryInfo(presetsPath);
                di.Attributes |= FileAttributes.Hidden; // Set Hidden attribute
                UnityEngine.Debug.Log($"Set 'Presets' folder to hidden at {presetsPath}");
            }
            if (Directory.Exists(customPath))
            {
                DirectoryInfo di = new DirectoryInfo(customPath);
                di.Attributes |= FileAttributes.Hidden; // Set Hidden attribute
                UnityEngine.Debug.Log($"Set 'Custom' folder to hidden at {customPath}");
            }
            if (File.Exists(editorSettingsPath))
            {
                FileInfo fi = new FileInfo(editorSettingsPath);
                fi.Attributes |= FileAttributes.Hidden; // Set Hidden attribute
                UnityEngine.Debug.Log($"Set 'EditorSettings.json' to hidden at {editorSettingsPath}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to hide files/folders: {ex.Message}");
        }
		
		try
        {
            if (File.Exists("unity default resources"))
            {
                File.Copy("unity default resources", "E:/RustMapper/RustMapper_Data/Resources/unity default resources", true);
                UnityEngine.Debug.Log("Successfully copied splash screen");
            }
            else
            {
                UnityEngine.Debug.LogError("Source unity default resources not found");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Failed to copy unity default resources: {ex.Message}");
        }
		
		//we need to copy a file, full path "unity default resources"  to "E:/RustMapper/RustMapper_Data/Resources/" and overwrite the unity default resources there already
		
		RemoveDirectory("E:/RustMapper/RustMapper_BurstDebugInformation_DoNotShip");
    }
	
	// yeah this doesn't work
	/*
	private static void SetRunAsAdministrator(string exePath)
    {
        try
        {
            string script = $@"
            $bytes = [System.IO.File]::ReadAllBytes('{exePath}')
            $peOffset = [System.BitConverter]::ToInt32($bytes[0x3C..0x3F], 0) + 4
            $bytes[$peOffset] = [byte]0x20
            [System.IO.File]::WriteAllBytes('{exePath}', $bytes)
            ";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    UnityEngine.Debug.LogError($"Failed to set run as admin for {exePath}. Error: {error}");
                }
                else
                {
                    UnityEngine.Debug.Log($"Successfully set {exePath} to run as administrator.");
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error setting run as admin: {ex.Message}");
        }
    }
	*/
	public static void RemoveDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            UnityEngine.Debug.LogError("Directory path is null or empty.");
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
                UnityEngine.Debug.Log($"Directory '{directoryPath}' and its contents have been deleted.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to delete directory '{directoryPath}'. Error: {ex.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Directory '{directoryPath}' does not exist.");
        }
    }
	

}