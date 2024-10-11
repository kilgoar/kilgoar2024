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
		
		fpsReadout.text = framerate.ToString("F1") + "fps" + " | " + usedMemoryf.ToString("F1") + " MB";
		time -=1f;
		frames = 0;
		
		
		
		}
	}
	
}
