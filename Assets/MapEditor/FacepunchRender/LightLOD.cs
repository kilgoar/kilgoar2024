using System;
using System.Collections.Generic;
using UnityEngine;
using VLB;

public class LightLOD : MonoBehaviour
{
    public float DistanceBias = 1.0f;

    public bool ToggleLight = true;

    public bool ToggleShadows = true;

    private LightOccludee lightOccludee;
    private VolumetricLightBeam volumetricLightBeam;
    private Light lightComponent;
    private LightEx lightEx;
    private float currentDistance; // Distance to the camera or player
    private float lastDistanceChange; // Timestamp of last distance change for smoothing
    private float smoothingFactor = 0.1f; // Smoothing factor for LOD transitions
    private float lodThreshold; // Threshold for changing LOD
    private LightShadows originalShadowSettings;

    private void Awake()
    {
        // Initialize components and properties
        InitializeComponents();
    }

    private void OnEnable()
    {
        // Register for global LOD updates if needed
        LightLODEnvironment.RegisterLightLOD(this);
    }

    private void OnDisable()
    {
        // Unregister from global LOD updates
        LightLODEnvironment.UnregisterLightLOD(this);
    }

    private void InitializeComponents()
    {
        lightComponent = GetComponent<Light>();
        lightEx = GetComponent<LightEx>();
        volumetricLightBeam = GetComponent<VolumetricLightBeam>();
        lightOccludee = GetComponent<LightOccludee>();
        if (lightComponent != null) originalShadowSettings = lightComponent.shadows;
    }

    public void RefreshLOD()
    {
        // Update LOD based on current distance from camera or player
        UpdateDistance();
        ChangeLOD();
    }

    public void ChangeLOD()
    {
        // Simplified LOD logic:
        // If the distance exceeds the threshold, change to lower LOD or disable

        if (currentDistance > lodThreshold)
        {
            if (ToggleLight) lightComponent.enabled = false;
            if (ToggleShadows) lightComponent.shadows = LightShadows.None;
        }
        else
        {
            if (ToggleLight) lightComponent.enabled = true;
            if (ToggleShadows) lightComponent.shadows = originalShadowSettings;
        }

        // Here you might adjust intensity, range, or other properties based on LOD
    }

    private void UpdateDistance()
    {
        // Calculate distance from light to camera/player
        Vector3 position = transform.position;
        Vector3 cameraPosition = Camera.main.transform.position;
        currentDistance = Vector3.Distance(position, cameraPosition);
        
        // Smooth the distance change over time
        currentDistance = Mathf.Lerp(currentDistance, lastDistanceChange, smoothingFactor);
        lastDistanceChange = Time.time;
    }

    // Static class to manage global LOD settings or lists of LightLOD instances
    private static class LightLODEnvironment
    {
        public static List<LightLOD> allLights = new List<LightLOD>();

        public static void RegisterLightLOD(LightLOD lod)
        {
            if (!allLights.Contains(lod))
            {
                allLights.Add(lod);
            }
        }

        public static void UnregisterLightLOD(LightLOD lod)
        {
            allLights.Remove(lod);
        }

        // Placeholder for methods that might update all LODs globally
        public static void UpdateAllLODs() { /* Implement global LOD update logic */ }
    }
}