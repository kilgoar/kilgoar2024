using UnityEngine;
using UnityEngine.Events;


//disabling IO components

public class EntityFlag_Toggle : MonoBehaviour
{
    public bool disableAllRenderers = true;

    void Awake()
    {
        if (disableAllRenderers)
        {
            DisableRenderers(transform);
        }
    }

    void DisableRenderers(Transform parent)
    {
        // Get all Renderer components on the current GameObject
        Renderer[] renderers = parent.GetComponents<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Recursively disable renderers on child GameObjects
        for (int i = 0; i < parent.childCount; i++)
        {
            DisableRenderers(parent.GetChild(i));
        }
    }

    // Additional methods can be added here for enabling renderers, toggling based on flags, etc.
}