using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PrefabAttribute : MonoBehaviour
{
    // Non-serialized fields for transformation data
    [NonSerialized] public Vector3 position;
    [NonSerialized] public Quaternion rotation;
    [NonSerialized] public Vector3 scale;

    // Additional non-serialized fields
    [NonSerialized] public string someStringData;
    [NonSerialized] public uint someUintData;
    [NonSerialized] public int someIntData;
    [NonSerialized] public bool someBoolData;

    // Protected virtual method for initialization
    protected virtual void Initialize(GameObject go, string identifier, bool someFlag1, bool someFlag2, bool someFlag3)
    {
        // Default implementation (can be overridden)
    }

    // Abstract method to determine the type of this attribute
    protected abstract Type GetPrefabAttributeType();

    // Override Equals for comparison
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType()) return false;
        return base.Equals(obj); // Add specific comparison logic if needed
    }

    // Override GetHashCode for collections
    public override int GetHashCode()
    {
        return base.GetHashCode(); // Add specific hash logic if needed
    }

    // Override ToString for debugging
    public override string ToString()
    {
        return $"{GetType().Name} ({name})";
    }

    // Virtual methods for attribute behavior
    public virtual string GetName() => "PrefabAttribute";
    public virtual bool IsActive() => true;

    // Nested class for managing collections of attributes
    public class AttributeCollection
    {
        private List<PrefabAttribute> attributes = new List<PrefabAttribute>();

        public void AddAttribute(PrefabAttribute attribute)
        {
            if (attribute != null) attributes.Add(attribute);
        }

        public List<PrefabAttribute> GetAttributesByType(Type type)
        {
            return attributes.FindAll(attr => attr.GetType() == type);
        }
    }

    // Nested class for managing attributes by ID
    public class AttributeManager
    {
        private Dictionary<uint, List<PrefabAttribute>> attributeMap = new Dictionary<uint, List<PrefabAttribute>>();

        public void AddAttribute(uint id, PrefabAttribute attribute)
        {
            if (!attributeMap.ContainsKey(id)) attributeMap[id] = new List<PrefabAttribute>();
            if (attribute != null) attributeMap[id].Add(attribute);
        }

        public T GetAttribute<T>(uint id) where T : PrefabAttribute
        {
            if (attributeMap.TryGetValue(id, out var list))
                return list.Find(attr => attr is T) as T;
            return null;
        }

        public T[] GetAllAttributes<T>(uint id) where T : PrefabAttribute
        {
            if (attributeMap.TryGetValue(id, out var list))
                return list.FindAll(attr => attr is T).ToArray() as T[];
            return new T[0];
        }
    }

    // Static class for global attribute management
    public static class AttributeRegistry
    {
        public static AttributeManager SomeManager1 = new AttributeManager();
        public static AttributeManager SomeManager2 = new AttributeManager();
        // Add more managers as needed
    }
}