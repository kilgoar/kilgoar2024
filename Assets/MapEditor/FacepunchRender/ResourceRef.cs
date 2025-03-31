using UnityEngine;

public class ResourceRef<T> where T : UnityEngine.Object
{
    public string guid;
    public T cachedInstance;

    public ResourceRef()
    {
        guid = string.Empty;
        cachedInstance = null;
    }

    public string Guid => guid;
    public uint ResourceId => string.IsNullOrEmpty(guid) ? 0U : AssetManager.ToID(AssetManager.GuidToPath.TryGetValue(guid, out string path) ? path : guid);
    public bool IsValid => !string.IsNullOrEmpty(guid) && GetResource() != null;

    public T resource => GetResource();

    public virtual T GetResource()
    {
        if (cachedInstance != null) return cachedInstance;
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
        cachedInstance = AssetManager.GetAssetByGuid<T>(guid);
        if (cachedInstance == null)
        {
            Debug.LogError($"Failed to load resource from AssetManager for GUID: {guid}");
        }
        return cachedInstance;
    }
}