using UnityEngine;
using UnityEngine.Serialization;

// ── Floor scatter entry ───────────────────────────────────────────────────────
// Placed at random positions on the navigable floor (cables, plates, debris…).
[System.Serializable]
public struct DecorationEntry
{
    // All variants of this entry. One is chosen at random on each placement.
    public GameObject[] Variants;

    // Objects per square metre. Count = Random(Min, Max) * room floor area.
    [Min(0f)] public float MinDensity;
    [Min(0f)] public float MaxDensity;

    // Hard cap on how many instances of this entry can appear in one room.
    // 0 = no cap (density alone drives the count).
    [Min(0)] public int MaxCount;

    // Skip this entry entirely if the room floor area (m²) is below this threshold.
    [Min(0f)] public float MinRoomArea;

    // Extra minimum distance between two instances of this same entry.
    [Min(0f)] public float SpacingRadius;

    // Objects will not be placed within this distance from any door socket.
    [Min(0f)] public float DoorExclusionRadius;

    // Distance from any wall surface, measured via NavMesh.FindClosestEdge.
    [Min(0f)] public float MinWallDistance;
    [Min(0f)] public float MaxWallDistance;

    // If false, the object keeps its prefab rotation.
    public bool RandomYRotation;
}

// ── ScriptableObject ──────────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "RoomDecorationRules", menuName = "BulletHeaven/Room Decoration Rules")]
public class RoomDecorationRules : ScriptableObject
{
    // Random floor scatter (cables, plates, debris…).
    // FormerlySerializedAs preserves data from assets that used the old "Entries" field name.
    [FormerlySerializedAs("Entries")]
    public DecorationEntry[] FloorEntries;

    // How many random positions to attempt before giving up on one floor instance.
    [Min(1)] public int MaxAttemptsPerEntry = 30;

    // Hard cap on total floor objects placed in a single room across all FloorEntries.
    // 0 = no cap.
    [Min(0)] public int MaxTotalObjects;
}
