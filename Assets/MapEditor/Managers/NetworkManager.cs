using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public static class NetworkManager
{
    public static Dictionary<int, GameObject> AddressLookup = new Dictionary<int, GameObject>();
    public static int currentAddress = 0;

    static NetworkManager()
    {
        AddressLookup = new Dictionary<int, GameObject>();
        currentAddress = 0;
    }

    public static int Register(GameObject objectToRegister)
    {
        if (objectToRegister == null)
        {
            Debug.LogError("Cannot register null object");
            return -1;
        }

        AddressLookup.Add(currentAddress, objectToRegister);
        int assignedAddress = currentAddress;
        currentAddress++;
        Debug.Log(currentAddress);
        return assignedAddress;
    }

    public static bool UnregisterObject(int address)
    {
        return AddressLookup.Remove(address);
    }

    public static GameObject GetObject(int address)
    {
        return AddressLookup.TryGetValue(address, out GameObject obj) ? obj : null;
    }

    public static bool IsObjectRegistered(GameObject obj)
    {
        return AddressLookup.ContainsValue(obj);
    }

    public static void Clear()
    {
        AddressLookup.Clear();
        currentAddress = 0;
    }
}