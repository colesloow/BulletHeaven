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

[CreateAssetMenu(fileName = "DungeonRules", menuName = "BulletHeaven/Dungeon Rules")]
public class DungeonRules : ScriptableObject
{
    [Header("Room type rules")]
    public RoomRule[] RoomRules;

    [Header("Topology")]
    [Range(0f, 1f)] public float BifurcationProbability;
    public int MaxBranchDepth;
}
