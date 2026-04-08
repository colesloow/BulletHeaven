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
    private readonly List<DungeonPiece> placedPieces = new();
    public IReadOnlyList<DungeonPiece> PlacedPieces => placedPieces;
    private readonly Dictionary<RoomType, int> roomCounts = new();
    private readonly Dictionary<CorridorType, int> corridorCounts = new();
    private readonly List<GameObject> _sealingWalls = new();

    // Parent transform that groups all generated pieces in the hierarchy
    private Transform dungeonRoot;

    private NavMeshDataInstance _navMeshInstance;

    private void Awake()
    {
        dungeonRoot = new GameObject("Dungeon").transform;
        dungeonRoot.SetParent(transform);

        // Instantiate the starting room at the world origin
        Room firstRoom = Instantiate(startRoom, Vector3.zero, startRoom.transform.rotation, dungeonRoot);
        placedPieces.Add(firstRoom);

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

            // Optionally insert a corridor before attaching the next room
            if (rules.CorridorProbability > 0f && Random.value < rules.CorridorProbability)
                AttachCorridor(targetDoor, depth);
            else
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
    private List<(Room prefab, RoomType type)> GetRoomCandidates()
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

    // Returns a flat list of (prefab, type) pairs for corridors, weighted by rule weights.
    private List<(Corridor prefab, CorridorType type)> GetCorridorCandidates()
    {
        var candidates = new List<(Corridor, CorridorType)>();

        foreach (CorridorRule rule in rules.CorridorRules)
        {
            if (rule.MaxCount > 0 && corridorCounts.TryGetValue(rule.Type, out int count) && count >= rule.MaxCount)
                continue;

            if (rule.Weight <= 0f || rule.Prefabs == null || rule.Prefabs.Length == 0)
                continue;

            int slots = Mathf.Max(1, Mathf.RoundToInt(rule.Weight * 10f));
            for (int i = 0; i < slots; i++)
                foreach (Corridor prefab in rule.Prefabs)
                    candidates.Add((prefab, rule.Type));
        }

        return candidates;
    }

    private void AttachRoom(DoorSocket targetDoor, int depth)
    {
        var candidates = GetRoomCandidates();
        if (candidates.Count == 0) return;

        int[] order = RandomOrder(candidates.Count);

        foreach (int i in order)
        {
            (Room prefab, RoomType assignedType) = candidates[i];
            Room newRoom = Instantiate(prefab, dungeonRoot);

            int[] doorOrder = RandomOrder(newRoom.Doors.Length);
            bool placed = false;

            foreach (int j in doorOrder)
            {
                DoorSocket newDoor = newRoom.Doors[j];

                // Only connect doors with matching widths
                if (newDoor.Width != targetDoor.Width) continue;

                RotatePieceToMatchDoors(newRoom, newDoor, targetDoor);
                AlignPiecePosition(newRoom, newDoor, targetDoor);

                if (!OverlapsAnyPiece(newRoom))
                {
                    targetDoor.IsConnected = true;
                    newDoor.IsConnected = true;

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
                    placedPieces.Add(newRoom);
                    roomCounts.TryGetValue(assignedType, out int current);
                    roomCounts[assignedType] = current + 1;
                    placed = true;
                    break;
                }
            }

            if (placed) return;

            Destroy(newRoom.gameObject);
        }
    }

    private void AttachCorridor(DoorSocket targetDoor, int depth)
    {
        var candidates = GetCorridorCandidates();
        if (candidates.Count == 0)
        {
            // No corridor candidates available: fall back to direct room attachment
            AttachRoom(targetDoor, depth);
            return;
        }

        int[] order = RandomOrder(candidates.Count);

        foreach (int i in order)
        {
            (Corridor prefab, CorridorType assignedType) = candidates[i];
            Corridor newCorridor = Instantiate(prefab, dungeonRoot);

            int[] doorOrder = RandomOrder(newCorridor.Doors.Length);
            bool placed = false;

            foreach (int j in doorOrder)
            {
                DoorSocket newDoor = newCorridor.Doors[j];

                if (newDoor.Width != targetDoor.Width) continue;

                RotatePieceToMatchDoors(newCorridor, newDoor, targetDoor);
                AlignPiecePosition(newCorridor, newDoor, targetDoor);

                if (!OverlapsAnyPiece(newCorridor))
                {
                    targetDoor.IsConnected = true;
                    newDoor.IsConnected = true;

                    // Expose free corridor doors as room attachment points
                    foreach (DoorSocket door in newCorridor.Doors)
                    {
                        if (door.IsConnected) continue;
                        if (depth >= rules.MaxBranchDepth) continue;
                        openDoors.Add((door, depth + 1));
                    }

                    newCorridor.Type = assignedType;
                    placedPieces.Add(newCorridor);
                    corridorCounts.TryGetValue(assignedType, out int current);
                    corridorCounts[assignedType] = current + 1;
                    placed = true;
                    break;
                }
            }

            if (placed) return;

            Destroy(newCorridor.gameObject);
        }

        // No corridor could be placed: fall back to direct room attachment
        AttachRoom(targetDoor, depth);
    }

    // Instantiates a wall prefab at every door socket that was not connected to another piece.
    private void SealOpenDoors()
    {
        if (wallPrefab == null) return;

        foreach (DungeonPiece piece in placedPieces)
        {
            foreach (DoorSocket socket in piece.Doors)
            {
                if (socket.IsConnected) continue;

                GameObject wall = Instantiate(wallPrefab, socket.transform.position, socket.transform.rotation, piece.transform);
                _sealingWalls.Add(wall);
            }
        }
    }

    // Returns true if the candidate piece overlaps any already placed piece.
    private bool OverlapsAnyPiece(DungeonPiece candidate)
    {
        Bounds candidateBounds = candidate.GetBounds();
        candidateBounds.Expand(-overlapTolerance);

        foreach (DungeonPiece placed in placedPieces)
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

    // Builds a NavMesh at runtime from the navmesh mesh source on each room.
    // Corridors are skipped (no navmesh source assigned yet).
    private void BuildNavMesh()
    {
        var sources = new List<NavMeshBuildSource>();

        bool first = true;
        Bounds totalBounds = default;

        foreach (DungeonPiece piece in placedPieces)
        {
            Bounds b = piece.GetBounds();
            if (first) { totalBounds = b; first = false; }
            else totalBounds.Encapsulate(b);
        }

        foreach (DungeonPiece piece in placedPieces)
        {
            // Only rooms contribute to the NavMesh
            if (piece is not Room room) continue;

            Mesh floorMesh = room.NavFloorMesh;

            // Fall back to bounding box if no navmesh source is assigned
            if (floorMesh == null)
            {
                Bounds floor = room.GetFloorBounds();
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = floor.size,
                    transform = Matrix4x4.TRS(floor.center, Quaternion.identity, Vector3.one),
                    area = 0
                });
                continue;
            }

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = floorMesh,
                transform = Matrix4x4.TRS(room.transform.position, room.transform.rotation, Vector3.one),
                area = 0 // Walkable
            });

            Bounds meshBounds = new(
                room.transform.TransformPoint(floorMesh.bounds.center),
                floorMesh.bounds.size
            );
            if (first) { totalBounds = meshBounds; first = false; }
            else totalBounds.Encapsulate(meshBounds);
        }

        // Add sealing walls as Not Walkable Box sources (Collider bounds).
        foreach (GameObject wall in _sealingWalls)
        {
            foreach (Collider col in wall.GetComponentsInChildren<Collider>())
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = col.bounds.size,
                    transform = Matrix4x4.TRS(col.bounds.center, Quaternion.identity, Vector3.one),
                    area = 1 // Not Walkable
                });
            }
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

    private void RotatePieceToMatchDoors(DungeonPiece piece, DoorSocket newDoor, DoorSocket targetDoor)
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

    private void AlignPiecePosition(DungeonPiece piece, DoorSocket newDoor, DoorSocket targetDoor)
    {
        Vector3 offset = newDoor.transform.position - piece.transform.position;
        piece.transform.position = targetDoor.transform.position - offset;
    }
}
