using UnityEngine;
using System;

    [System.Serializable]
    public class SplatType
    {
        public string Name;
        public Color AridColor;
        public SplatOverlay AridOverlay;
        public Color TemperateColor;
        public SplatOverlay TemperateOverlay;
        public Color TundraColor;
        public SplatOverlay TundraOverlay;
        public Color ArcticColor;
		public Color JungleColor;
        public SplatOverlay ArcticOverlay;
		public SplatOverlay JungleOverlay;
        public PhysicMaterial Material;
        public float SplatTiling;
        public float UVMixMult;  
        public float UVMixStart;
        public float UVMixDist;
    }
