using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LightEx extends the functionality of Unity's Light component with additional features like color and intensity variations over time.
/// </summary>
public class LightEx : MonoBehaviour
{
    public bool alterColor = false;

    public float colorTimeScale = 1.0f;

    public Color colorA = Color.white;

    public Color colorB = Color.white;

    public AnimationCurve blendCurve;

    public bool loopColor = true;

    public bool alterIntensity = false;
	
    public float intensityTimeScale = 1.0f;

    public AnimationCurve intenseCurve;

    public float intensityCurveScale = 1.0f;

    public bool loopIntensity = true;

    public bool randomOffset = false;

    public float randomIntensityStartScale = 1.0f;

    public List<Light> syncLights;

    // Private fields for internal use
    private float colorChangeProgress;
    private Color currentColor;
    private float intensityChangeProgress;
    private bool isInitialized;
    private float initialIntensity;
    private Light lightComponent;
    private LightLOD lightLOD; // Assuming LightLOD is a related class for LOD management of lights

    protected void OnEnable()
    {
        // Initialization logic when the component is enabled
        if (!isInitialized)
        {
            Initialize();
        }
    }

    protected void Awake()
    {
        // Initialize when the object is created
        Initialize();
    }

    private void Initialize()
    {
        lightComponent = GetComponent<Light>();
        lightLOD = GetComponent<LightLOD>();

        if (randomOffset)
        {
            colorChangeProgress = UnityEngine.Random.Range(0f, 1f);
            if (alterIntensity)
            {
                intensityChangeProgress = UnityEngine.Random.Range(0f, randomIntensityStartScale);
            }
        }

        currentColor = colorA;
        initialIntensity = lightComponent.intensity;
        isInitialized = true;
    }

    public void DeltaUpdate(float deltaTime)
    {
        if (alterColor)
        {
            UpdateLightColor(deltaTime);
        }
        if (alterIntensity)
        {
            UpdateLightIntensity(deltaTime);
        }
    }

    private void UpdateLightColor(float deltaTime)
    {
        if (colorTimeScale > 0)
        {
            colorChangeProgress += deltaTime / colorTimeScale;
            if (loopColor)
            {
                colorChangeProgress %= 1.0f; // Loop the progress
            }
            
            float blend = blendCurve.Evaluate(colorChangeProgress);
            currentColor = Color.Lerp(colorA, colorB, blend);
            lightComponent.color = currentColor;
            // Sync with other lights if necessary
        }
    }

    private void UpdateLightIntensity(float deltaTime)
    {
        if (intensityTimeScale > 0)
        {
            intensityChangeProgress += deltaTime / intensityTimeScale;
            if (loopIntensity)
            {
                intensityChangeProgress %= 1.0f;
            }

            float intensityFactor = intenseCurve.Evaluate(intensityChangeProgress) * intensityCurveScale;
            lightComponent.intensity = initialIntensity * intensityFactor;
            // Sync with other lights if necessary
        }
    }
	
	
	public void SetIntensity(float newIntensity)
    {
        if (lightComponent != null)
        {
            lightComponent.intensity = newIntensity;
            // Update the initial intensity for relative intensity changes
            initialIntensity = newIntensity;
        }
        else
        {
        }
    }
	

    protected void OnDisable()
    {
        // Clean up or save state when the component is disabled
    }

    // Static methods to check for the presence of LightEx on GameObjects
    public static bool HasLightEx(GameObject go) => go != null && go.GetComponent<LightEx>() != null;

    // Constructor
    public LightEx() : base()
    {
    }
}