using UnityEngine;

public enum PlacementType { Floor, Wall }

[System.Serializable]
public struct DecorationEntry
{
    public GameObject Prefab;
    public PlacementType Placement;

    // Objects per square metre. Count = Random(Min, Max) * room floor area.
    [Min(0f)] public float MinDensity;
    [Min(0f)] public float MaxDensity;

    // Physical footprint radius used to prevent overlaps with all other placed objects.
    // Leave at 0 to auto-compute from the prefab's renderer bounds.
    [Min(0f)] public float ObjectRadius;

    // Extra minimum distance between two instances of this same entry.
    // Useful to spread out objects of the same type (e.g. keep columns far apart).
    [Min(0f)] public float SpacingRadius;

    // Objects will not be placed within this distance from any door socket.
    [Min(0f)] public float DoorExclusionRadius;

    // Minimum distance from any wall surface, measured via NavMesh.FindClosestEdge.
    // Works correctly on any room shape; not affected by MeshCollider normal direction.
    [Min(0f)] public float MinWallDistance;

    // If false, the object keeps its prefab rotation (useful for aligned furniture, crates, etc.).
    public bool RandomYRotation;
}

[CreateAssetMenu(fileName = "RoomDecorationRules", menuName = "BulletHeaven/Room Decoration Rules")]
public class RoomDecorationRules : ScriptableObject
{
    public DecorationEntry[] Entries;

    // How many random positions to attempt before giving up on one instance.
    [Min(1)] public int MaxAttemptsPerEntry = 30;
}
