using UnityEngine;

public enum RoomType
{
    Normal,
    Treasure,
    Arena,   // doors lock on entry, must clear enemies to proceed
    Trap,
    Event    // for special events?
}

public class Room : DungeonPiece
{
    public RoomType Type { get; set; }

    // Navmesh mesh asset for this room. Assign the navmesh submesh from the room FBX in each prefab.
    public Mesh NavFloorMesh;

    // Decoration profile for this room shape. Contains layout presets per RoomType.
    public RoomDecorationProfile DecorationProfile;

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
