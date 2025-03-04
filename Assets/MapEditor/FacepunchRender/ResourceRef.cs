using System;
using UnityEngine;

[Serializable]
public class ResourceRef<T> where T : UnityEngine.Object
{
    [SerializeField]
    private string guid;

    [NonSerialized]
    protected T cachedInstance;

    public ResourceRef()
    {
        guid = string.Empty;
        cachedInstance = null;
    }

    public string Guid => guid;

    public uint ResourceId => string.IsNullOrEmpty(guid) ? 0U : AssetManager.ToID(guid); // Use AssetManager’s ID mapping

    public bool IsValid => !string.IsNullOrEmpty(guid) && GetResource() != null;

    public virtual T GetResource()
    {
        if (cachedInstance != null)
        {
            return cachedInstance;
        }

        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning("GUID is empty, cannot load resource.");
            return null;
        }

        if (!AssetManager.IsInitialised)
        {
            Debug.LogWarning($"AssetManager is not initialized. Cannot load resource for GUID: {guid}");
            return null;
        }

        //cachedInstance = AssetManager.LoadAsset<T>(ResourceId);
        if (cachedInstance == null)
        {
            Debug.LogError($"Failed to load resource from AssetManager for GUID: {guid}");
        }
        return cachedInstance;
    }
}