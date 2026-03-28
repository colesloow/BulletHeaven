using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Room startRoom;
    [SerializeField] private DungeonRules rules;
    [SerializeField] private int roomCount = 10;

    // Amount by which room bounds are shrunk before overlap testing,
    // so that rooms sharing a wall do not incorrectly trigger a collision.
    [SerializeField] private float overlapTolerance = 0.2f;

    // Prefab used to seal door openings that were not connected to any room
    [SerializeField] private GameObject wallPrefab;

    private readonly List<(DoorSocket door, int depth)> openDoors = new();
    private readonly List<Room> placedRooms = new();
    public IReadOnlyList<Room> PlacedRooms => placedRooms;
    private readonly Dictionary<RoomType, int> roomCounts = new();

    // Parent transform that groups all generated rooms in the hierarchy
    private Transform dungeonRoot;

    private NavMeshDataInstance _navMeshInstance;

    private void Awake()
    {
        dungeonRoot = new GameObject("Dungeon").transform;
        dungeonRoot.SetParent(transform);

        // Instantiate the starting room at the world origin
        Room firstRoom = Instantiate(startRoom, Vector3.zero, startRoom.transform.rotation, dungeonRoot);
        placedRooms.Add(firstRoom);

        foreach (DoorSocket door in firstRoom.Doors)
            openDoors.Add((door, 1));

        // Generate additional rooms one by one
        for (int i = 0; i < roomCount; i++)
        {
            if (openDoors.Count == 0)
                break;

            int randomIndex = Random.Range(0, openDoors.Count);
            (DoorSocket targetDoor, int depth) = openDoors[randomIndex];
            openDoors.RemoveAt(randomIndex);

            AttachRoom(targetDoor, depth);
        }

        // Close all door openings that remain unconnected after generation
        SealOpenDoors();

        BuildNavMesh();
    }

    private void OnDestroy()
    {
        NavMesh.RemoveNavMeshData(_navMeshInstance);
    }

    // Returns a flat list of (prefab, type) pairs weighted by rule weights and filtered by max counts.
    private List<(Room prefab, RoomType type)> GetCandidates()
    {
        var candidates = new List<(Room, RoomType)>();

        foreach (RoomRule rule in rules.RoomRules)
        {
            if (rule.MaxCount > 0 && roomCounts.TryGetValue(rule.Type, out int count) && count >= rule.MaxCount)
                continue;

            if (rule.Weight <= 0f || rule.Prefabs == null || rule.Prefabs.Length == 0)
                continue;

            int slots = Mathf.Max(1, Mathf.RoundToInt(rule.Weight * 10f));
            for (int i = 0; i < slots; i++)
                foreach (Room prefab in rule.Prefabs)
                    candidates.Add((prefab, rule.Type));
        }

        return candidates;
    }

    private void AttachRoom(DoorSocket targetDoor, int depth)
    {
        var candidates = GetCandidates();
        if (candidates.Count == 0) return;

        // Try each candidate in a random order
        int[] order = RandomOrder(candidates.Count);

        foreach (int i in order)
        {
            (Room prefab, RoomType assignedType) = candidates[i];
            Room newRoom = Instantiate(prefab, dungeonRoot);

            // Try each door of the new room as the connection point
            int[] doorOrder = RandomOrder(newRoom.Doors.Length);
            bool placed = false;

            foreach (int j in doorOrder)
            {
                DoorSocket newDoor = newRoom.Doors[j];

                RotateRoomToMatchDoors(newRoom, newDoor, targetDoor);
                AlignRoomPosition(newRoom, newDoor, targetDoor);

                // Only validate placement if the room does not overlap any existing room
                if (!OverlapsAnyRoom(newRoom))
                {
                    targetDoor.IsConnected = true;
                    newDoor.IsConnected = true;

                    // Register free doors as available connection points, respecting depth and bifurcation rules
                    bool firstFreeDoor = true;
                    foreach (DoorSocket door in newRoom.Doors)
                    {
                        if (door.IsConnected) continue;
                        if (depth >= rules.MaxBranchDepth) continue;

                        if (firstFreeDoor || Random.value < rules.BifurcationProbability)
                            openDoors.Add((door, depth + 1));

                        firstFreeDoor = false;
                    }

                    newRoom.Type = assignedType;
                    placedRooms.Add(newRoom);
                    roomCounts.TryGetValue(assignedType, out int current);
                    roomCounts[assignedType] = current + 1;
                    placed = true;
                    break;
                }
            }

            if (placed) return;

            // This prefab could not be placed without overlapping ; discard it and try the next
            Destroy(newRoom.gameObject);
        }

        // No prefab could be placed at this door: the door remains permanently closed
    }

    // Instantiates a wall prefab at every door socket that was not connected to another room.
    // Each wall is parented to its room to keep the hierarchy clean.
    private void SealOpenDoors()
    {
        if (wallPrefab == null) return;

        foreach (Room room in placedRooms)
        {
            foreach (DoorSocket socket in room.Doors)
            {
                if (socket.IsConnected) continue;

                Instantiate(wallPrefab, socket.transform.position, socket.transform.rotation, room.transform);
            }
        }
    }

    // Returns true if the candidate room overlaps any already placed room.
    // Bounds are slightly shrunk by overlapTolerance so that touching walls are allowed.
    private bool OverlapsAnyRoom(Room candidate)
    {
        Bounds candidateBounds = candidate.GetBounds();
        candidateBounds.Expand(-overlapTolerance);

        foreach (Room placed in placedRooms)
        {
            if (candidateBounds.Intersects(placed.GetBounds()))
                return true;
        }

        return false;
    }

    // Returns an array of indices from 0 to count-1 in a random order (Fisher-Yates shuffle).
    private int[] RandomOrder(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices;
    }

    // Builds a NavMesh at runtime from flat box sources derived from each room's floor footprint.
    // This avoids relying on mesh collider quality or FBX normals.
    private void BuildNavMesh()
    {
        var sources = new List<NavMeshBuildSource>();
        bool first = true;
        Bounds totalBounds = default;

        foreach (Room room in placedRooms)
        {
            Mesh floorMesh = room.NavFloorMesh;

            // Fall back to bounding box if no floor mesh is assigned
            if (floorMesh == null)
            {
                Bounds floor = room.GetFloorBounds();
                sources.Add(new NavMeshBuildSource
                {
                    shape     = NavMeshBuildSourceShape.Box,
                    size      = floor.size,
                    transform = Matrix4x4.TRS(floor.center, Quaternion.identity, Vector3.one),
                    area      = 0
                });

                if (first) { totalBounds = floor; first = false; }
                else totalBounds.Encapsulate(floor);
                continue;
            }

            sources.Add(new NavMeshBuildSource
            {
                shape        = NavMeshBuildSourceShape.Mesh,
                sourceObject = floorMesh,
                transform    = Matrix4x4.TRS(room.transform.position, room.transform.rotation, Vector3.one),
                area         = 0 // Walkable
            });

            Bounds meshBounds = new(
                room.transform.TransformPoint(floorMesh.bounds.center),
                floorMesh.bounds.size
            );

            if (first) { totalBounds = meshBounds; first = false; }
            else totalBounds.Encapsulate(meshBounds);
        }

        if (sources.Count == 0) return;

        NavMeshData data = NavMeshBuilder.BuildNavMeshData(
            NavMesh.GetSettingsByID(0),
            sources,
            totalBounds,
            Vector3.zero,
            Quaternion.identity
        );

        if (data != null)
            _navMeshInstance = NavMesh.AddNavMeshData(data);
    }

    private void RotateRoomToMatchDoors(Room room, DoorSocket newDoor, DoorSocket targetDoor)
    {
        Vector3 targetForward = targetDoor.transform.forward;
        Vector3 newForward = newDoor.transform.forward;

        targetForward.y = 0f;
        newForward.y = 0f;

        targetForward.Normalize();
        newForward.Normalize();

        // Compute the signed angle around Y so the new door faces the target door
        float angle = Vector3.SignedAngle(newForward, -targetForward, Vector3.up);
        room.transform.Rotate(0f, angle, 0f);
    }

    private void AlignRoomPosition(Room room, DoorSocket newDoor, DoorSocket targetDoor)
    {
        // Move the room so the two door sockets perfectly overlap
        Vector3 offset = newDoor.transform.position - room.transform.position;
        room.transform.position = targetDoor.transform.position - offset;
    }
}
