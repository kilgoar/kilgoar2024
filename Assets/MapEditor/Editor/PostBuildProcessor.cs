using UnityEditor;
using UnityEngine;
using System.IO;
//needs streamingAssets copies to set defaults for shipment
public class PostBuildProcessor
{
    [MenuItem("Rust Map Editor/Build")]
    public static void BuildGame()
    {
        string buildPath = "Builds/RustMapper.exe";
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.Development);

        string appDataPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "RustMapper");
		
        CopyDirectory("Presets", Path.Combine(appDataPath, "Presets"));
        CopyDirectory("Custom", Path.Combine(appDataPath, "Custom"));
        CopyEditorSettings(Path.Combine(appDataPath, "EditorSettings.json"));
    }

    public static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var directory in Directory.GetDirectories(sourceDir))
        {
            string directoryName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destinationDir, directoryName));
        }
    }

    public static void CopyEditorSettings(string destinationFile)
    {
        string sourceFile = "EditorSettings.json"; 

        if (File.Exists(sourceFile))
        {
            File.Copy(sourceFile, destinationFile, true);
            Debug.Log($"Copied EditorSettings.json to: {destinationFile}");
        }
        else
        {
            Debug.LogWarning("EditorSettings.json not found at: " + sourceFile);
        }
    }
}