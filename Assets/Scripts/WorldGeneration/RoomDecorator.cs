using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Static utility class that scatters decoration objects on the navigable floor of a room.
// Mirrors the pattern of DungeonPlacer: pure logic, no MonoBehaviour dependency.
public static class RoomDecorator
{
    // Cache so each unique prefab is measured only once per play session.
    private static readonly Dictionary<GameObject, float> radiusCache = new();

    public static void DecorateRoom(Room room, RoomDecorationRules rules, Transform parent)
    {
        if (rules == null) return;

        ScatterFloor(room, rules, parent);
    }

    // ── Floor scatter ─────────────────────────────────────────────────────────

    private static void ScatterFloor(Room room, RoomDecorationRules rules, Transform parent)
    {
        if (rules.FloorEntries == null) return;

        Bounds floorBounds = room.GetFloorBounds();
        float area = floorBounds.size.x * floorBounds.size.z;

        var doorPositions = new List<Vector3>();
        foreach (DoorSocket socket in room.Doors)
            doorPositions.Add(socket.transform.position);

        // Shared across all entries: (position, radius) for cross-entry overlap checks.
        var allPlaced = new List<(Vector3 position, float radius)>();

        foreach (DecorationEntry entry in rules.FloorEntries)
        {
            if (entry.Variants == null || entry.Variants.Length == 0) continue;
            if (entry.MinRoomArea > 0f && area < entry.MinRoomArea) continue;

            // Radius is the largest footprint across all variants (conservative overlap check).
            float objectRadius = GetVariantsRadius(entry.Variants);

            float density = Random.Range(entry.MinDensity, entry.MaxDensity);
            int count = Mathf.RoundToInt(density * area);

            if (entry.MaxCount > 0)
                count = Mathf.Min(count, entry.MaxCount);

            // Per-entry list for same-type spacing checks.
            var entryPlaced = new List<Vector3>();

            for (int i = 0; i < count; i++)
            {
                if (rules.MaxTotalObjects > 0 && allPlaced.Count >= rules.MaxTotalObjects)
                    return;

                TryPlaceOnFloor(entry, objectRadius, floorBounds, doorPositions, allPlaced, entryPlaced, parent, rules.MaxAttemptsPerEntry);
            }
        }
    }

    private static void TryPlaceOnFloor(
        DecorationEntry entry,
        float objectRadius,
        Bounds floorBounds,
        List<Vector3> doorPositions,
        List<(Vector3 position, float radius)> allPlaced,
        List<Vector3> entryPlaced,
        Transform parent,
        int maxAttempts)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(floorBounds.min.x, floorBounds.max.x);
            float z = Random.Range(floorBounds.min.z, floorBounds.max.z);
            var candidate = new Vector3(x, 0f, z);

            // Reject points outside the actual floor shape (handles non-convex rooms).
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 0.3f, NavMesh.AllAreas))
                continue;

            candidate = new Vector3(navHit.position.x, 0f, navHit.position.z);

            // Reject if NavMesh snapping pushed the candidate outside the room floor bounds.
            // This catches cases where SamplePosition snaps to a nearby corridor NavMesh.
            if (candidate.x < floorBounds.min.x || candidate.x > floorBounds.max.x ||
                candidate.z < floorBounds.min.z || candidate.z > floorBounds.max.z)
                continue;

            if (!IsFarEnoughFromDoors(candidate, doorPositions, entry.DoorExclusionRadius))
                continue;

            if (!IsFarEnoughFromAllObjects(candidate, objectRadius, allPlaced))
                continue;

            if (!IsFarEnoughFromSameEntry(candidate, entryPlaced, entry.SpacingRadius))
                continue;

            bool needsEdge = entry.MinWallDistance > 0f || entry.MaxWallDistance > 0f;
            if (needsEdge)
            {
                if (!NavMesh.FindClosestEdge(candidate, out NavMeshHit edgeHit, NavMesh.AllAreas))
                    continue;
                if (entry.MinWallDistance > 0f && edgeHit.distance < entry.MinWallDistance)
                    continue;
                if (entry.MaxWallDistance > 0f && edgeHit.distance > entry.MaxWallDistance)
                    continue;
            }

            Quaternion rotation = entry.RandomYRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;

            GameObject prefab = entry.Variants[Random.Range(0, entry.Variants.Length)];
            UnityEngine.Object.Instantiate(prefab, candidate, rotation, parent);
            allPlaced.Add((candidate, objectRadius));
            entryPlaced.Add(candidate);
            return;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsFarEnoughFromDoors(Vector3 candidate, List<Vector3> doorPositions, float radius)
    {
        float radiusSq = radius * radius;
        foreach (Vector3 door in doorPositions)
        {
            float dx = candidate.x - door.x;
            float dz = candidate.z - door.z;
            if (dx * dx + dz * dz < radiusSq) return false;
        }
        return true;
    }

    // Checks against all placed objects using the sum of their radii.
    private static bool IsFarEnoughFromAllObjects(Vector3 candidate, float candidateRadius, List<(Vector3 position, float radius)> allPlaced)
    {
        foreach (var (pos, radius) in allPlaced)
        {
            float minDist = candidateRadius + radius;
            float dx = candidate.x - pos.x;
            float dz = candidate.z - pos.z;
            if (dx * dx + dz * dz < minDist * minDist) return false;
        }
        return true;
    }

    private static bool IsFarEnoughFromSameEntry(Vector3 candidate, List<Vector3> entryPlaced, float spacingRadius)
    {
        float radiusSq = spacingRadius * spacingRadius;
        foreach (Vector3 placed in entryPlaced)
        {
            float dx = candidate.x - placed.x;
            float dz = candidate.z - placed.z;
            if (dx * dx + dz * dz < radiusSq) return false;
        }
        return true;
    }

    // Returns the largest XZ footprint radius across all variants.
    // Used as a conservative overlap radius so no variant can clip into another object.
    private static float GetVariantsRadius(GameObject[] variants)
    {
        float max = 0f;
        foreach (GameObject variant in variants)
        {
            if (variant != null)
                max = Mathf.Max(max, GetPrefabRadius(variant));
        }
        return max;
    }

    // Instantiates the prefab once to measure its renderer bounds, then destroys it.
    // Result is cached so each prefab is measured only once per play session.
    private static float GetPrefabRadius(GameObject prefab)
    {
        if (radiusCache.TryGetValue(prefab, out float cached))
            return cached;

        // Instantiate far below the scene so the temp object is never visible.
        GameObject temp = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, -9999f, 0f), Quaternion.identity);
        Renderer[] renderers = temp.GetComponentsInChildren<Renderer>();

        float radius = 0f;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // XZ footprint only — height does not affect placement overlap.
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        UnityEngine.Object.Destroy(temp);
        radiusCache[prefab] = radius;
        return radius;
    }
}
