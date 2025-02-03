using UnityEngine;
using System.Collections.Generic;

public class SpawnGroup : MonoBehaviour
{
    // Determines the tier or category of this monument or spawn group.
    //[InspectorFlags]
    
	//public MonumentTier Tier;

    // List of spawnable prefabs with their respective spawn configurations.
    public List<SpawnGroup.SpawnEntry> prefabs;

    // Maximum number of objects that can be spawned from this group at any one time.
    public int maxPopulation;

    // The minimum and maximum number of objects that should be spawned each tick or cycle.
    public int numToSpawnPerTickMin;
    public int numToSpawnPerTickMax;

    // Minimum and maximum time delay between respawns of objects in this group.
    public float respawnDelayMin;
    public float respawnDelayMax;

    // Flags for controlling spawn behavior:
    public bool wantsInitialSpawn;    // If true, the group will attempt to spawn items when first activated.
    public bool temporary;            // If true, this group might be for temporary or event-based spawning.
    public bool forceInitialSpawn;    // Forces an initial spawn regardless of other conditions.
    public bool preventDuplicates;    // Ensures the same prefab isn't spawned multiple times if active.
    public bool isSpawnerActive;      // Toggle to control if this spawn group is currently spawning objects.


    // A collider defining an area where if objects move beyond, they might be considered 'free' or despawned.
    public BoxCollider setFreeIfMovedBeyond;

    // Category for this spawn group, possibly used for grouping or filtering in the editor or game logic.
    public string category;

    // Inner class to define what can be spawned and how.
    //[Serializable]
    public class SpawnEntry
    {
        // Reference to the prefab that this entry will spawn.
        public GameObjectRef prefab;

        // Weight determines the likelihood of this prefab being chosen when spawning.
        public int weight;

        // Indicates if the spawned object can move or if it's static.
        public bool mobile;

        public SpawnEntry()
        {
        }
    }

    // Constructor for SpawnGroup, typically used by Unity for initialization.
    public SpawnGroup()
    {
    }
}