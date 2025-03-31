using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManifest : ScriptableObject
{
    [Serializable] public struct PooledString { [SerializeField] public string str; [SerializeField] public uint hash; }
    [Serializable] public class PrefabProperties { [SerializeField] public string name; [SerializeField] public string guid; [SerializeField] public uint hash; [SerializeField] public bool pool; }
    [Serializable] public class EffectCategory { [SerializeField] public string folder; [SerializeField] public List<string> prefabs; }
    [Serializable] public class GuidPath { [SerializeField] public string name; [SerializeField] public string guid; }

    [SerializeField] public PooledString[] pooledStrings;
    [SerializeField] public PrefabProperties[] prefabProperties;
    [SerializeField] public EffectCategory[] effectCategories;
    [SerializeField] public GuidPath[] guidPaths;
    [SerializeField] public string[] entities;


}