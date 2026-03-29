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

    // Navmesh mesh asset for this room. Assign the navmesh submesh from the room FBX in each prefab.
    public Mesh NavFloorMesh;

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

    // Returns a flat box at floor level (Y=0) covering the XZ footprint of this room.
    // Used to build NavMesh sources without relying on mesh colliders.
    public Bounds GetFloorBounds()
    {
        Bounds b = GetBounds();
        return new Bounds(
            new Vector3(b.center.x, 0f, b.center.z),
            new Vector3(b.size.x, 0.2f, b.size.z)
        );
    }
}
