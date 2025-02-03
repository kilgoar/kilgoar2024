using UnityEngine;

public abstract class BaseSpawnPoint : MonoBehaviour
{
    // Enum defining different types of spawn points, which might dictate different spawning behaviors or characteristics.
    public enum SpawnPointType
    {
        Normal,
        Tugboat,
        Motorbike,
        Bicycle
    }

    // Public field to set or get the type of this spawn point.
    public SpawnPointType spawnPointType;

    // Protected field used for player positioning or spawning logic, determining a margin around the spawn point.
    [SerializeField]
    [Range(1f, 25f)]
    protected float playerCheckMargin;

    // Constructor for BaseSpawnPoint, typically used by Unity for initialization.
    protected BaseSpawnPoint()
    {
    }
}