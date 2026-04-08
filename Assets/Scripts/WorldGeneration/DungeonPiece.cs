using UnityEngine;

// Base class for all connectable dungeon pieces (rooms and corridors).
public abstract class DungeonPiece : MonoBehaviour
{
    public DoorSocket[] Doors;

    protected virtual void Awake()
    {
        Doors = GetComponentsInChildren<DoorSocket>();
    }

    // Returns the combined world-space bounds of all renderers in this piece.
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
