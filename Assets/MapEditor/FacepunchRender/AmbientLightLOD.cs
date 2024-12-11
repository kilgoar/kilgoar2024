using UnityEngine;
using UnityEngine.Rendering;

public class AmbientLightLOD : LODComponent
{
    public bool isDynamic;

    public float enabledRadius = 100.0f;

    public bool toggleFade = true;

    public float toggleFadeDuration = 1.0f;

    private Light ambientLight;
    private LightEx lightEx; // Assuming LightEx is some extension of Light
    private VLB.VolumetricLightBeam volBeam; // For volumetric light effects

    private float targetIntensity = 0.0f;
    private float currentIntensity = 0.0f;
    private float lastLODCalculationTime = 0.0f;
    private bool isCurrentlyActive = false;

    private void Awake()
    {
        ambientLight = GetComponent<Light>();
        if (ambientLight == null)
        {
            Debug.LogError($"{gameObject.name} does not have a Light component!");
            return;
        }
        lightEx = GetComponent<LightEx>();
        volBeam = GetComponent<VLB.VolumetricLightBeam>();

        lastLODCalculationTime = Time.time;
        currentIntensity = ambientLight.intensity;
    }

    private void Start()
    {
        //SetLightActive(isDynamic || Vector3.Distance(Camera.main.transform.position, transform.position) < enabledRadius);
    }

    protected override int CalculateLODLevel(float distance)
    {
        // This is a very basic LOD calculation. Adjust as needed for your game's needs.
        if (distance < enabledRadius) return 0; // Full intensity
        else if (distance < enabledRadius * 2) return 1; // Half intensity
        else return 2; // No light
    }
	
	protected override void CheckLOD(float distance)
		{
			int newLevel = CalculateLODLevel(distance);
			
			if (newLevel != currentLODLevel){
				//UpdateLOD(newLevel);
			}
		}
		
	public void SetLightActive(bool active)
    {
        if (active != isCurrentlyActive)
        {
            isCurrentlyActive = active;
            if (toggleFade)
            {
                targetIntensity = active ? 1.0f : 0.0f;
            }
            else
            {
                currentIntensity = targetIntensity = active ? 1.0f : 0.0f;
                ApplyIntensity();
            }
        }
    }

    private void UpdateIntensityFade()
    {
        float timeSinceLastFade = Time.time - lastLODCalculationTime;
        if (timeSinceLastFade < toggleFadeDuration)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, timeSinceLastFade / toggleFadeDuration);
            ApplyIntensity();
        }
        lastLODCalculationTime = Time.time;
    }

    private void ApplyIntensity()
    {
        if (ambientLight != null) ambientLight.intensity = currentIntensity;
        if (lightEx != null) lightEx.SetIntensity(currentIntensity);
        if (volBeam != null) volBeam.SetIntensity(currentIntensity);
    }

    // Implement other methods as needed, like RefreshLOD or ChangeLOD, 
    // depending on how you want to handle LOD transitions for ambient light
}