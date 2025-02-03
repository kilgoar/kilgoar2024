using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Instancing
{
    [Serializable]
    public class InstancedLODState
    {
        public Mesh Mesh;
        public Material[] Materials;
        public Matrix4x4 LocalToWorld;
        public ShadowCastingMode CastShadows;
        public bool RecieveShadows;
        public LightProbeUsage LightProbes;
        public int LodLevel;
        public int TotalLodLevels;
        public InstancedMeshCategory MeshCategory;
        public float MinimumDistance;
        public float MaximumDistance;

        public InstancedLODState(Matrix4x4 localToWorld, MeshRenderer meshRenderer, float minimumDistance, float maximumDistance, int lodLevel, int totalLodLevels, InstancedMeshCategory meshCategory)
        {
            // Constructor implementation would go here
        }
    }
}