using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Static utility class that scatters decoration objects inside a room.
// Mirrors the pattern of DungeonPlacer: pure logic, no MonoBehaviour dependency.
public static class RoomDecorator
{
    // Cache so each unique prefab is measured only once per play session.
    private static readonly Dictionary<GameObject, float> radiusCache = new();

    public static void DecorateRoom(Room room, RoomDecorationRules rules, Transform parent)
    {
        if (rules == null || rules.Entries == null) return;

        Bounds floorBounds = room.GetFloorBounds();
        float area = floorBounds.size.x * floorBounds.size.z;

        var doorPositions = new List<Vector3>();
        foreach (DoorSocket socket in room.Doors)
            doorPositions.Add(socket.transform.position);

        // Shared across all entries: (position, radius) for cross-entry overlap checks.
        var allPlaced = new List<(Vector3 position, float radius)>();

        foreach (DecorationEntry entry in rules.Entries)
        {
            if (entry.Prefab == null) continue;

            // Use inspector value if set, otherwise measure the prefab automatically.
            float objectRadius = entry.ObjectRadius > 0f
                ? entry.ObjectRadius
                : GetPrefabRadius(entry.Prefab);

            float density = Random.Range(entry.MinDensity, entry.MaxDensity);
            int count = Mathf.RoundToInt(density * area);

            // Per-entry list for same-type spacing checks.
            var entryPlaced = new List<Vector3>();

            for (int i = 0; i < count; i++)
            {
                if (entry.Placement == PlacementType.Floor)
                    TryPlaceOnFloor(entry, objectRadius, floorBounds, doorPositions, allPlaced, entryPlaced, parent, rules.MaxAttemptsPerEntry);
                else
                    TryPlaceOnWall(entry, objectRadius, floorBounds, doorPositions, allPlaced, entryPlaced, parent, rules.MaxAttemptsPerEntry);
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
        int rejectNavMesh = 0, rejectDoor = 0, rejectOverlap = 0, rejectSpacing = 0, rejectWall = 0;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(floorBounds.min.x, floorBounds.max.x);
            float z = Random.Range(floorBounds.min.z, floorBounds.max.z);
            var candidate = new Vector3(x, 0f, z);

            // Reject points outside the actual floor shape (handles non-convex rooms).
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 0.3f, NavMesh.AllAreas))
            { rejectNavMesh++; continue; }

            candidate = new Vector3(navHit.position.x, 0f, navHit.position.z);

            if (!IsFarEnoughFromDoors(candidate, doorPositions, entry.DoorExclusionRadius))
            { rejectDoor++; continue; }

            if (!IsFarEnoughFromAllObjects(candidate, objectRadius, allPlaced))
            { rejectOverlap++; continue; }

            if (!IsFarEnoughFromSameEntry(candidate, entryPlaced, entry.SpacingRadius))
            { rejectSpacing++; continue; }

            // FindClosestEdge returns the distance to the nearest NavMesh boundary (= wall surface).
            // This works correctly regardless of room shape or MeshCollider normal direction,
            // because it queries the NavMesh geometry rather than physics colliders.
            if (entry.MinWallDistance > 0f &&
                (!NavMesh.FindClosestEdge(candidate, out NavMeshHit edgeHit, NavMesh.AllAreas) || edgeHit.distance < entry.MinWallDistance))
            { rejectWall++; continue; }

            float yRot = entry.RandomYRotation ? Random.Range(0f, 360f) : 0f;
            UnityEngine.Object.Instantiate(entry.Prefab, candidate, Quaternion.Euler(0f, yRot, 0f), parent);
            allPlaced.Add((candidate, objectRadius));
            entryPlaced.Add(candidate);
            Debug.Log($"[RoomDecorator] Placed {entry.Prefab.name} at {candidate} after {attempt + 1} attempt(s). Rejects: navmesh={rejectNavMesh} door={rejectDoor} overlap={rejectOverlap} spacing={rejectSpacing} wall={rejectWall}");
            return;
        }

        Debug.LogWarning($"[RoomDecorator] Failed to place {entry.Prefab.name} after {maxAttempts} attempts. Rejects: navmesh={rejectNavMesh} door={rejectDoor} overlap={rejectOverlap} spacing={rejectSpacing} wall={rejectWall}");
    }

    private static void TryPlaceOnWall(
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
            // Cast from a random interior point at half-height outward in a random horizontal direction.
            float x = Random.Range(floorBounds.min.x, floorBounds.max.x);
            float z = Random.Range(floorBounds.min.z, floorBounds.max.z);
            var origin = new Vector3(x, 0.5f, z);

            // Only cast from points that are actually inside the room's NavMesh.
            if (!NavMesh.SamplePosition(origin, out _, 0.3f, NavMesh.AllAreas))
                continue;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, 20f))
                continue;

            var candidate = new Vector3(hit.point.x, 0f, hit.point.z);

            if (!IsFarEnoughFromDoors(candidate, doorPositions, entry.DoorExclusionRadius))
                continue;

            if (!IsFarEnoughFromAllObjects(candidate, objectRadius, allPlaced))
                continue;

            if (!IsFarEnoughFromSameEntry(candidate, entryPlaced, entry.SpacingRadius))
                continue;

            // Face away from the wall (toward room interior).
            Quaternion rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            UnityEngine.Object.Instantiate(entry.Prefab, candidate, rotation, parent);
            allPlaced.Add((candidate, objectRadius));
            entryPlaced.Add(candidate);
            return;
        }
    }

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
        foreach ((Vector3 pos, float radius) in allPlaced)
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

    // Instantiates the prefab once to measure its renderer bounds, then destroys it.
    // Result is cached so each prefab is measured only once per play session.
    private static float GetPrefabRadius(GameObject prefab)
    {
        if (radiusCache.TryGetValue(prefab, out float cached))
            return cached;

        GameObject temp = UnityEngine.Object.Instantiate(prefab);
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
