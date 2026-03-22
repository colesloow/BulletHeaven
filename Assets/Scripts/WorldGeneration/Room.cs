using UnityEngine;

public enum RoomType
{
    Normal,
    Treasure,
    Arena,   // doors lock on entry, must clear enemies to proceed
    Trap,
    Event    // for special events?
}

public class Room : MonoBehaviour
{
    public RoomType Type { get; set; }
    public DoorSocket[] Doors;

    void Awake()
    {
        Doors = GetComponentsInChildren<DoorSocket>();
    }

    // Returns the combined world-space bounds of all renderers in this room.
    // Used by the dungeon generator to detect overlaps before validating placement.
    public Bounds GetBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
