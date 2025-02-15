using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using static TerrainManager;

public static class AreaManager
{
    public static Area ActiveArea = new Area(0, 512, 0, 512); // This is used by other methods to track the splat map resolution

    private static List<Area> sectors = new List<Area>();
    private static Dictionary<Area, List<PrefabDataHolder>> prefabDataBySector = new Dictionary<Area, List<PrefabDataHolder>>();

    public static void Reset()
    {
        ActiveArea = new Area(0, TerrainManager.SplatMapRes, 0, TerrainManager.SplatMapRes);
    }

    public static void Initialize()
    {
        Reset();
        CacheSectors();
		//CullEmpties();
    }

    public static void CacheSectors()
    {
        sectors.Clear();
        prefabDataBySector.Clear();
        int sectorSize = 200;
        for (int x = 0; x < TerrainManager.TerrainSize.x; x += sectorSize)
        {
            for (int z = 0; z < TerrainManager.TerrainSize.z; z += sectorSize)
            {
                Area sector = CreateSector(x, z);
                sectors.Add(sector);
                prefabDataBySector[sector] = new List<PrefabDataHolder>(); // Initialize an empty list for each sector
            }
        }
    }

    public static Area CreateSector(int x, int z)
    {
        // Add center point 
        int sectorSize = 200;
        int x1 = (int)Mathf.Min(x + sectorSize, TerrainManager.TerrainSize.x);
        int z1 = (int)Mathf.Min(z + sectorSize, TerrainManager.TerrainSize.z);
        Area newSector = new Area(x, x1, z, z1);
        newSector.centerpoint = new Vector3((x + x1) / 2f, 0, (z + z1) / 2f);
        return newSector;
    }

    public static Area FindSector(Vector3 position)
    {
        // Check if the position is within any existing sector
        foreach (Area sector in sectors)
        {
            if (position.x >= sector.x0 && position.x < sector.x1 && position.z >= sector.z0 && position.z < sector.z1)
            {
                return sector;
            }
        }

        // If no sector found, create a new one for the position
        int sectorSize = 200;
        int xSector = (int)Mathf.Floor(position.x / sectorSize) * sectorSize;
        int zSector = (int)Mathf.Floor(position.z / sectorSize) * sectorSize;

        // Create a new sector even if outside of terrain bounds
        Area newSector = new Area(xSector, xSector + sectorSize, zSector, zSector + sectorSize);
        newSector.centerpoint = new Vector3(xSector + sectorSize / 2f, 0, zSector + sectorSize / 2f);
        
        sectors.Add(newSector); // Add the new sector to our list
        prefabDataBySector[newSector] = new List<PrefabDataHolder>(); // Initialize a new list for this sector
        return newSector;
    }
    
public static IEnumerator UpdateSectorsCoroutine(Vector3 position, float distance)
{
    // Find sectors within the specified distance
    List<Area> activeSectors = FindSectors(position, distance);

    int sectorCount = activeSectors.Count;
    int batchSize = 2; // Divide work into manageable batches
    List<PrefabDataHolder> toRemove = new List<PrefabDataHolder>();

    // Throttle settings
    float checksPerSecond = 10f; // Target 10 checks per second
    float delayBetweenBatches = .25f / checksPerSecond; // Time per check (0.1 seconds for 10 checks/second)

    // Process sectors in parallel using batches
    for (int i = 0; i < sectorCount; i += batchSize)
    {
        int start = i;
        int end = Mathf.Min(i + batchSize, sectorCount); // Calculate batch range

        for (int j = start; j < end; j++)
        {
            Area sector = activeSectors[j];

            toRemove.Clear();

            if (prefabDataBySector.TryGetValue(sector, out List<PrefabDataHolder> prefabHolders))
            {
                // Use a list to collect items to remove
                foreach (var holder in prefabHolders)
                {
                    if (holder != null)
                    {
                        holder.Refresh();
                    }
                    else
                    {
                        toRemove.Add(holder); // Mark for removal
                    }
                }

                // Remove null entries from the list after processing
                foreach (var holder in toRemove)
                {
                    prefabHolders.Remove(holder);
                }

                // If the list becomes empty after removing nulls, consider removing the sector from the dictionary
                if (prefabHolders.Count == 0)
                {
                    prefabDataBySector.Remove(sector);
                }
            }
        }

        // Add a delay to throttle the coroutine to ~10 checks per second
        yield return new WaitForSeconds(delayBetweenBatches);
    }
}

    public static void UpdateSectors(Vector3 position, float distance)
    {
        CoroutineManager.Instance.StartRuntimeCoroutine(UpdateSectorsCoroutine(position, distance));
    }
	/*
	public static void UpdateSectors(Vector3 position, float distance)
	{
		// Find sectors within the specified distance
		List<Area> activeSectors = FindSectors(position, distance);
		foreach (Area sector in activeSectors)
		{
			if (prefabDataBySector.TryGetValue(sector, out List<PrefabDataHolder> prefabHolders))
			{
				foreach (var holder in prefabHolders)
				{
					if (holder != null)
					{
						holder.Refresh();
					}
				}
			}
		}
	}
	*/

	public static List<Area> FindSectors(Vector3 position, float distance)
	{
		List<Area> result = new List<Area>();



		foreach (Area sector in sectors)
		{
			// Skip sectors beyond the distance threshold
			float dist = Vector2.Distance(new Vector2(position.x, position.z),
										   new Vector2(sector.centerpoint.x, sector.centerpoint.z));
			if (dist > distance) continue;



				result.Add(sector);

		}
		return result;
	}


    public static void AddPrefabToSector(Area sector, PrefabDataHolder prefabDataHolder)
    {
        if (!prefabDataBySector.ContainsKey(sector))
        {
            prefabDataBySector[sector] = new List<PrefabDataHolder>();
        }
        prefabDataBySector[sector].Add(prefabDataHolder);
    }

	public static void CullEmpties()
	{
		List<Area> sectorsToRemove = new List<Area>();

		foreach (var sector in sectors)
		{
			if (!prefabDataBySector.ContainsKey(sector) || prefabDataBySector[sector].Count == 0)
			{
				sectorsToRemove.Add(sector);
			}
		}

		foreach (var sector in sectorsToRemove)
		{
			sectors.Remove(sector);
			prefabDataBySector.Remove(sector);
		}
	}

    public static List<PrefabDataHolder> GetPrefabsInSector(Area sector)
    {
        if (prefabDataBySector.TryGetValue(sector, out List<PrefabDataHolder> prefabs))
        {
            return prefabs;
        }
        return new List<PrefabDataHolder>(); // Return an empty list if sector not found
    }


    public class Area
    {
		public bool enabled;
        public Vector3 centerpoint;
        public int x0;
        public int x1;
        public int z0;
        public int z1;

        public Area(int x0, int x1, int z0, int z1)
        {
            this.x0 = x0;
            this.x1 = x1;
            this.z0 = z0;
            this.z1 = z1;
        }

        public static Area HeightMapDimensions()
        {
            return new Area(0, TerrainManager.HeightMapRes, 0, TerrainManager.HeightMapRes);
        }
    }
}