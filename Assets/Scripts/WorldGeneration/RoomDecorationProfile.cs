using UnityEngine;

[System.Serializable]
public struct DecorationProfileEntry
{
    public RoomType Type;
    public GameObject[] Presets;
}

// One asset per room shape. Lists layout presets per RoomType so the generator
// can pick a random layout that is both shape-compatible and type-appropriate.
[CreateAssetMenu(fileName = "RoomDecorationProfile", menuName = "BulletHeaven/Room Decoration Profile")]
public class RoomDecorationProfile : ScriptableObject
{
    public DecorationProfileEntry[] Entries;

    public GameObject GetRandomPreset(RoomType type)
    {
        if (Entries == null) return null;

        foreach (DecorationProfileEntry entry in Entries)
        {
            if (entry.Type != type) continue;
            if (entry.Presets == null || entry.Presets.Length == 0) continue;
            return entry.Presets[Random.Range(0, entry.Presets.Length)];
        }

        return null;
    }
}
