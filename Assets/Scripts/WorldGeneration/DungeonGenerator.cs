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
    private readonly List<GameObject> _sealingWalls = new();

    private Transform dungeonRoot;
    private NavMeshDataInstance _navMeshInstance;

    private void Awake()
    {
        dungeonRoot = new GameObject("Dungeon").transform;
        dungeonRoot.SetParent(transform);

        Room firstRoom = Instantiate(startRoom, Vector3.zero, startRoom.transform.rotation, dungeonRoot);
        placedPieces.Add(firstRoom);

        foreach (DoorSocket door in firstRoom.Doors)
            openDoors.Add((door, 1));

        // Each iteration places exactly one room. Corridors are transitions
        // inserted before the room and do not consume a room slot.
        for (int i = 0; i < roomCount; i++)
        {
            if (openDoors.Count == 0)
                break;

            int randomIndex = Random.Range(0, openDoors.Count);
            (DoorSocket targetDoor, int depth) = openDoors[randomIndex];
            openDoors.RemoveAt(randomIndex);

            DoorSocket roomDoor = targetDoor;

            if (rules.CorridorProbability > 0f && Random.value < rules.CorridorProbability)
            {
                DoorSocket continuation = AttachCorridorSequence(targetDoor, depth);
                if (continuation != null)
                    roomDoor = continuation;
            }

            AttachRoom(roomDoor, depth);
        }

        TryCloseLoops();
        SealOpenDoors();
        BuildNavMesh();
    }

    private void OnDestroy()
    {
        NavMesh.RemoveNavMeshData(_navMeshInstance);
    }

    // -------------------------------------------------------------------------
    // Room attachment
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Corridor sequence attachment
    // -------------------------------------------------------------------------

    // Places a sequence of corridor pieces starting from targetDoor.
    // Returns the final continuation door (where the room should attach),
    // or null if no corridor could be placed (room attaches to targetDoor directly).
    // Partial placement is acceptable: if a step fails, the last successful
    // continuation door is returned and the room attaches there.
    // Branch doors (e.g. extra exits on a junction) are added to openDoors.
    private DoorSocket AttachCorridorSequence(DoorSocket targetDoor, int depth)
    {
        CorridorSequence? seq = PickSequence();
        if (seq == null) return null;

        CorridorType[] pattern = seq.Value.Pattern;
        if (pattern == null || pattern.Length == 0) return null;

        DoorSocket currentDoor = targetDoor;

        foreach (CorridorType type in pattern)
        {
            var result = TryPlaceOnePiece(type, currentDoor);
            if (result == null) break;

            (Corridor placed, DoorSocket entryDoor) = result.Value;
            DoorSocket continuation = FindContinuationDoor(placed, entryDoor);

            // Non-continuation free doors become branch attachment points for future rooms
            foreach (DoorSocket door in placed.Doors)
            {
                if (door.IsConnected) continue;
                if (door == continuation) continue;
                if (depth >= rules.MaxBranchDepth) continue;
                openDoors.Add((door, depth + 1));
            }

            if (continuation == null) break;
            currentDoor = continuation;
        }

        // Return null if no corridor was placed (currentDoor unchanged)
        return currentDoor != targetDoor ? currentDoor : null;
    }

    // Picks a weighted random sequence from DungeonRules.CorridorSequences.
    private CorridorSequence? PickSequence()
    {
        if (rules.CorridorSequences == null || rules.CorridorSequences.Length == 0)
            return null;

        var weighted = new List<CorridorSequence>();
        foreach (CorridorSequence seq in rules.CorridorSequences)
        {
            if (seq.Weight <= 0f || seq.Pattern == null || seq.Pattern.Length == 0) continue;
            int slots = Mathf.Max(1, Mathf.RoundToInt(seq.Weight * 10f));
            for (int i = 0; i < slots; i++) weighted.Add(seq);
        }

        return weighted.Count > 0 ? weighted[Random.Range(0, weighted.Count)] : null;
    }

    // Tries to place one corridor of the given type at targetDoor.
    // Returns the placed piece and the entry door used, or null on failure.
    private (Corridor piece, DoorSocket entryDoor)? TryPlaceOnePiece(CorridorType type, DoorSocket targetDoor)
    {
        var candidates = new List<Corridor>();
        foreach (CorridorPrefabSet set in rules.CorridorPrefabs)
        {
            if (set.Type != type || set.Prefabs == null) continue;
            foreach (Corridor prefab in set.Prefabs) candidates.Add(prefab);
        }

        if (candidates.Count == 0) return null;

        int[] order = RandomOrder(candidates.Count);
        foreach (int i in order)
        {
            Corridor piece = Instantiate(candidates[i], dungeonRoot);
            int[] doorOrder = RandomOrder(piece.Doors.Length);

            foreach (int j in doorOrder)
            {
                DoorSocket entry = piece.Doors[j];
                if (entry.Width != targetDoor.Width) continue;

                RotatePieceToMatchDoors(piece, entry, targetDoor);
                AlignPiecePosition(piece, entry, targetDoor);

                if (!OverlapsAnyPiece(piece))
                {
                    targetDoor.IsConnected = true;
                    entry.IsConnected = true;
                    piece.Type = type;
                    placedPieces.Add(piece);
                    return (piece, entry);
                }
            }

            Destroy(piece.gameObject);
        }

        return null;
    }

    // Returns the door of a placed piece that best continues the hallway direction,
    // i.e. the unconnected door whose forward is most opposite to the entry door's forward.
    private DoorSocket FindContinuationDoor(DungeonPiece piece, DoorSocket entryDoor)
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

    // -------------------------------------------------------------------------
    // Loop closing (atomic)
    // -------------------------------------------------------------------------

    // After the main generation tree is complete, tries to connect pairs of facing
    // open doors with a straight corridor, creating cycles in the dungeon graph.
    private void TryCloseLoops()
    {
        if (rules.LoopProbability <= 0f || rules.MaxLoops <= 0) return;

        var openList = new List<DoorSocket>();
        foreach (DungeonPiece piece in placedPieces)
            foreach (DoorSocket door in piece.Doors)
                if (!door.IsConnected) openList.Add(door);

        int loopsClosed = 0;

        for (int a = 0; a < openList.Count && loopsClosed < rules.MaxLoops; a++)
        {
            DoorSocket doorA = openList[a];
            if (doorA.IsConnected) continue;
            if (Random.value > rules.LoopProbability) continue;

            for (int b = a + 1; b < openList.Count; b++)
            {
                DoorSocket doorB = openList[b];
                if (doorB.IsConnected) continue;
                if (doorB.Width != doorA.Width) continue;

                if (TryConnectWithStraight(doorA, doorB))
                {
                    loopsClosed++;
                    break;
                }
            }
        }
    }

    // Tries to connect doorA and doorB with a single straight corridor piece.
    // Atomic: either it fully succeeds or nothing is placed.
    private bool TryConnectWithStraight(DoorSocket doorA, DoorSocket doorB)
    {
        var candidates = new List<Corridor>();
        foreach (CorridorPrefabSet set in rules.CorridorPrefabs)
        {
            if (set.Type != CorridorType.Straight || set.Prefabs == null) continue;
            foreach (Corridor prefab in set.Prefabs) candidates.Add(prefab);
        }

        foreach (Corridor prefab in candidates)
        {
            Corridor piece = Instantiate(prefab, dungeonRoot);

            foreach (DoorSocket entry in piece.Doors)
            {
                if (entry.Width != doorA.Width) continue;

                RotatePieceToMatchDoors(piece, entry, doorA);
                AlignPiecePosition(piece, entry, doorA);

                if (OverlapsAnyPiece(piece)) continue;

                DoorSocket exit = FindContinuationDoor(piece, entry);
                if (exit == null) continue;

                float dist = Vector3.Distance(exit.transform.position, doorB.transform.position);
                float dot  = Vector3.Dot(exit.transform.forward, -doorB.transform.forward);

                if (dist < 0.1f && dot > 0.99f)
                {
                    doorA.IsConnected = true;
                    entry.IsConnected = true;
                    doorB.IsConnected = true;
                    exit.IsConnected  = true;
                    piece.Type = CorridorType.Straight;
                    placedPieces.Add(piece);
                    return true;
                }
            }

            Destroy(piece.gameObject);
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Door sealing and NavMesh
    // -------------------------------------------------------------------------

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
            if (piece is not Room room) continue;

            Mesh floorMesh = room.NavFloorMesh;

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
                continue;
            }

            sources.Add(new NavMeshBuildSource
            {
                shape        = NavMeshBuildSourceShape.Mesh,
                sourceObject = floorMesh,
                transform    = Matrix4x4.TRS(room.transform.position, room.transform.rotation, Vector3.one),
                area         = 0
            });

            Bounds meshBounds = new(
                room.transform.TransformPoint(floorMesh.bounds.center),
                floorMesh.bounds.size
            );
            if (first) { totalBounds = meshBounds; first = false; }
            else totalBounds.Encapsulate(meshBounds);
        }

        foreach (GameObject wall in _sealingWalls)
        {
            foreach (Collider col in wall.GetComponentsInChildren<Collider>())
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape     = NavMeshBuildSourceShape.Box,
                    size      = col.bounds.size,
                    transform = Matrix4x4.TRS(col.bounds.center, Quaternion.identity, Vector3.one),
                    area      = 1
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

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private bool OverlapsAnyPiece(DungeonPiece candidate)
    {
        Bounds candidateBounds = candidate.GetBounds();
        candidateBounds.Expand(-overlapTolerance);

        foreach (DungeonPiece placed in placedPieces)
            if (candidateBounds.Intersects(placed.GetBounds())) return true;

        return false;
    }

    private int[] RandomOrder(int count)
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

    private void RotatePieceToMatchDoors(DungeonPiece piece, DoorSocket newDoor, DoorSocket targetDoor)
    {
        Vector3 targetForward = targetDoor.transform.forward;
        Vector3 newForward    = newDoor.transform.forward;

        targetForward.y = 0f;
        newForward.y    = 0f;
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
