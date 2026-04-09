using UnityEngine;
using System.Collections.Generic;

// Static utility class for geometric placement operations used by DungeonGenerator.
// All methods are pure: they only read or write the transforms/state passed to them,
// and have no dependency on MonoBehaviour lifecycle.
public static class DungeonPlacer
{
    // Rotates piece so that newDoor faces the opposite direction of targetDoor,
    // making the two doors snap together face-to-face.
    // SignedAngle returns the signed Y rotation (degrees) needed on the horizontal plane.
    public static void RotatePieceToMatchDoors(DungeonPiece piece, DoorSocket newDoor, DoorSocket targetDoor)
    {
        Vector3 targetForward = targetDoor.transform.forward;
        Vector3 newForward = newDoor.transform.forward;

        targetForward.y = 0f;
        newForward.y = 0f;
        targetForward.Normalize();
        newForward.Normalize();

        float angle = Vector3.SignedAngle(newForward, -targetForward, Vector3.up);
        piece.transform.Rotate(0f, angle, 0f);
    }

    // Translates piece so that newDoor's world position coincides with targetDoor's.
    // The offset between the door and the piece root is preserved after rotation,
    // so this must be called after RotatePieceToMatchDoors.
    public static void AlignPiecePosition(DungeonPiece piece, DoorSocket newDoor, DoorSocket targetDoor)
    {
        Vector3 offset = newDoor.transform.position - piece.transform.position;
        piece.transform.position = targetDoor.transform.position - offset;
    }

    // Returns true if the candidate piece's bounds (shrunk by overlapTolerance)
    // intersect any already-placed piece.
    public static bool OverlapsAnyPiece(DungeonPiece candidate, IReadOnlyList<DungeonPiece> placedPieces, float overlapTolerance)
    {
        Bounds candidateBounds = candidate.GetBounds();
        candidateBounds.Expand(-overlapTolerance);

        foreach (DungeonPiece placed in placedPieces)
            if (candidateBounds.Intersects(placed.GetBounds())) return true;

        return false;
    }

    // Returns the unconnected door of a piece that best continues the hallway direction.
    // "Best" means the door whose forward vector is most opposite to the entry door's forward,
    // found via the dot product: dot = -1 means perfectly opposite (straight through),
    // dot = 1 means same direction (dead end / U-turn). We pick the smallest dot value.
    public static DoorSocket FindContinuationDoor(DungeonPiece piece, DoorSocket entryDoor)
    {
        DoorSocket best = null;
        float bestDot = 1f;

        foreach (DoorSocket door in piece.Doors)
        {
            if (door.IsConnected) continue;
            float dot = Vector3.Dot(door.transform.forward, entryDoor.transform.forward);
            if (dot < bestDot) { bestDot = dot; best = door; }
        }

        return best;
    }

    // Returns an array of indices [0..count-1] in a random order (Fisher-Yates shuffle).
    // Used to iterate prefab and door lists without bias.
    public static int[] RandomOrder(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices;
    }
}
