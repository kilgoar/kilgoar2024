using System.Collections;
using System.Collections.Generic;
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