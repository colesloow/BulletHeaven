using UnityEngine;

[System.Serializable]
public struct RoomRule
{
    public RoomType Type;
    public Room[] Prefabs;

    [Min(0f)] public float Weight;

    // Maximum number of rooms of this type in the dungeon. 0 means unlimited.
    [Min(0)] public int MaxCount;
}

// Maps a CorridorType to its set of prefabs. Used by the sequence system to
// instantiate the right piece for each step in a CorridorSequence pattern.
[System.Serializable]
public struct CorridorPrefabSet
{
    public CorridorType Type;
    public Corridor[] Prefabs;
}

// Defines an ordered sequence of corridor types to place between two rooms.
// e.g. [Straight, Corner, Straight] produces an L-shaped hallway.
// If a step cannot be placed (overlap), the sequence stops there and a room
// will attach to the last successful continuation door.
[System.Serializable]
public struct CorridorSequence
{
    public CorridorType[] Pattern;
    [Min(0f)] public float Weight;
}

[CreateAssetMenu(fileName = "DungeonRules", menuName = "BulletHeaven/Dungeon Rules")]
public class DungeonRules : ScriptableObject
{
    [Header("Room type rules")]
    public RoomRule[] RoomRules;

    [Header("Corridor prefabs")]
    // Maps each CorridorType to its prefabs. Used by the sequence system.
    public CorridorPrefabSet[] CorridorPrefabs;

    [Header("Corridor sequences")]
    // Defines the hallway patterns placed before each room.
    // If empty, rooms connect directly (or via a single random corridor if CorridorProbability > 0).
    public CorridorSequence[] CorridorSequences;

    [Header("Topology")]
    // Probability that a corridor sequence is inserted before a room. 0 = rooms connect directly.
    [Range(0f, 1f)] public float CorridorProbability;

    // Probability of creating a loop by connecting two facing open doors with a straight corridor.
    [Range(0f, 1f)] public float LoopProbability;

    // Maximum number of loops the generator will attempt to close.
    public int MaxLoops;

    [Range(0f, 1f)] public float BifurcationProbability;
    public int MaxBranchDepth;
}
