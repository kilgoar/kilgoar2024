using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DebugManager : MonoBehaviour
{
	public Text fpsReadout;
	private int frames;
	private float time;
	private float framerate;
	public long usedMemory;
	public float usedMemoryf;
	
	    private StreamWriter logWriter;
    private string logFilePath;

    private void Awake()
    {
        // Initialize log file
        string logDir = Path.Combine(SettingsManager.AppDataPath(), "Logs");
        try
        {
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            logFilePath = Path.Combine(logDir, "DebugLog.txt");
            logWriter = new StreamWriter(logFilePath, false); // Overwrite file
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize log file at {logFilePath}: {ex.Message}");
        }

        // Register log handler
        Application.logMessageReceived += HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logWriter == null) return;

        try
        {
            // Write: [LogType] Message
            logWriter.WriteLine($"[{type}] {logString}");
			
			if(ConsoleWindow.Instance!=null){
				ConsoleWindow.Instance.Post($"[{type}] {logString}");
			}

            // Include stack trace for errors and exceptions
            if (type == LogType.Error || type == LogType.Exception)
            {
                logWriter.WriteLine(stackTrace);
                logWriter.WriteLine();
            }
        }
        catch (Exception ex)
        {
			//you're fucked
        }
    }

    private void OnDestroy()
    {
        // Clean up
        Application.logMessageReceived -= HandleLog;
        if (logWriter != null)
        {
            try
            {
                logWriter.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to close log file: {ex.Message}");
            }
            logWriter = null;
        }
    }

	
	
    public void Update(){
		time += Time.deltaTime;
		frames++;
		if(time > 1f)
		{	
		framerate = (float)frames / time; 
		
		
		usedMemory = System.GC.GetTotalMemory(false);
        usedMemoryf = usedMemory / (1024f * 1024f);
		


            // Terrain data from TerrainManager
            Vector3 landSize = TerrainManager.Land.terrainData.size;
            int landHeightRes = TerrainManager.Land.terrainData.heightmapResolution;
            Vector3 landMaskSize = TerrainManager.LandMask.terrainData.size;
            int landMaskHeightRes = TerrainManager.LandMask.terrainData.heightmapResolution;
            Vector3 oceanSize = TerrainManager.Water.terrainData.size;
            int oceanHeightRes = TerrainManager.Water.terrainData.heightmapResolution;
            int splatRes = TerrainManager.SplatMapRes; // Shared splatmap resolution

            // readability
            fpsReadout.text = 
                              $"Land    : {landSize.x}x{landSize.z}m, H:{landHeightRes}x{landHeightRes}, S:{splatRes}x{splatRes}\n" +
							  $"{framerate:F1} fps | {usedMemoryf:F1} MB";
							  
		time -=1f;
		frames = 0;
			
		}
	}
	
}