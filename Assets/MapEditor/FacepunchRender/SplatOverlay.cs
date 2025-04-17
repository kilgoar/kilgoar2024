using UnityEngine;
using System;    
	
	[System.Serializable]
    public class SplatOverlay
    {
        public Color Color = Color.black; // OverlayColor from shortened version
        [Range(0f, 1f)] public float Smoothness = 0.5f;
        [Range(0f, 1f)] public float NormalIntensity;
        [Range(0f, 8f)] public float BlendFactor;
        [Range(0.01f, 32f)] public float BlendFalloff;
    }