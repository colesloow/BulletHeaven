using UnityEngine;
using System.Collections.Generic;

// Runs before all other scripts so the dungeon exists before anything tries to use it.
// Requires DungeonNavMeshBuilder on the same GameObject to build the runtime NavMesh
// once generation is complete.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(DungeonNavMeshBuilder))]
public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Room startRoom;
    [SerializeField] private DungeonRules rules;

    // All pieces (rooms + corridors) placed so far.
    private readonly List<DungeonPiece> placedPieces = new();
    public IReadOnlyList<DungeonPiece> PlacedPieces => placedPieces;

    // Open doors waiting to receive a room or corridor, paired with their branch depth.
    // Depth is tracked to enforce MaxBranchDepth and limit infinite branching.
    private readonly List<(DoorSocket door, int depth)> openDoors = new();

    // How many rooms of each type have been placed (used to enforce MaxCount per rule).
    private readonly Dictionary<RoomType, int> roomCounts = new();

    // Wall instances spawned to block unused door openings.
    private readonly List<GameObject> sealingWalls = new();

    // Root transform that groups all generated pieces in the hierarchy.
    private Transform dungeonRoot;

    // -------------------------------------------------------------------------
    // Generation entry point
    // -------------------------------------------------------------------------

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
        for (int i = 0; i < rules.RoomCount; i++)
        {
            if (openDoors.Count == 0)
                break;

            int randomIndex = Random.Range(0, openDoors.Count);
            (DoorSocket targetDoor, int depth) = openDoors[randomIndex];
            openDoors.RemoveAt(randomIndex);

            DoorSocket roomDoor = targetDoor;
            bool corridorPlaced = false;

            if (rules.CorridorProbability > 0f && Random.value < rules.CorridorProbability)
            {
                // Try to place a corridor sequence first. If successful, the room
                // attaches to the sequence's final door instead of the original one.
                DoorSocket continuation = AttachCorridorSequence(targetDoor, depth);
                if (continuation != null)
                {
                    roomDoor = continuation;
                    corridorPlaced = true;
                }
            }

            bool roomPlaced = AttachRoom(roomDoor, depth);

            // If no room fits at the end of a corridor sequence, cap it with an End piece
            // so the hallway terminates cleanly instead of stopping mid-air.
            if (!roomPlaced && corridorPlaced)
                TryPlaceOnePiece(CorridorType.End, roomDoor);
        }

        TryCloseLoops();
        SealOpenDoors();
        GetComponent<DungeonNavMeshBuilder>().Build(placedPieces, sealingWalls);
        DecorateRooms();
    }

    // -------------------------------------------------------------------------
    // Room attachment
    // -------------------------------------------------------------------------

    // Builds a weighted candidate list from all active RoomRules.
    // Each rule contributes (weight * 10) copies of each of its prefabs,
    // making weighted random selection equivalent to a random array pick.
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

    // Tries to attach a room to targetDoor.
    // Iterates prefabs in random order and accepts the first placement that fits.
    // Returns true if a room was successfully placed, false otherwise.
    private bool AttachRoom(DoorSocket targetDoor, int depth)
    {
        var candidates = GetRoomCandidates();
        if (candidates.Count == 0) return false;

        foreach (int i in DungeonPlacer.RandomOrder(candidates.Count))
        {
            (Room prefab, RoomType assignedType) = candidates[i];
            Room newRoom = Instantiate(prefab, dungeonRoot);

            DoorSocket fittingDoor = FindFittingDoor(newRoom, targetDoor);
            if (fittingDoor != null)
            {
                targetDoor.IsConnected = true;
                fittingDoor.IsConnected = true;
                RegisterPlacedRoom(newRoom, assignedType, depth);
                return true;
            }

            Destroy(newRoom.gameObject);
        }

        return false;
    }

    // Records a successfully placed room: updates type, piece list, room counts,
    // and adds its free doors to openDoors for future iterations.
    private void RegisterPlacedRoom(Room room, RoomType type, int depth)
    {
        room.Type = type;
        placedPieces.Add(room);
        roomCounts.TryGetValue(type, out int current);
        roomCounts[type] = current + 1;

        // Always add at least one free door so the tree keeps growing.
        // Additional doors are added probabilistically (BifurcationProbability).
        bool firstFreeDoor = true;
        foreach (DoorSocket door in room.Doors)
        {
            if (door.IsConnected) continue;
            if (depth >= rules.MaxBranchDepth) continue;
            if (firstFreeDoor || Random.value < rules.BifurcationProbability)
                openDoors.Add((door, depth + 1));
            firstFreeDoor = false;
        }
    }

    // -------------------------------------------------------------------------
    // Corridor sequence attachment
    // -------------------------------------------------------------------------

    // Places a sequence of corridor pieces starting from targetDoor.
    // Returns the final continuation door (where the room will attach),
    // or null if no corridor could be placed (room attaches to targetDoor directly).
    // Partial placement is fine: if a step fails, the room attaches to the
    // last successfully placed corridor's continuation door.
    // Branch doors (extra exits on junctions) are added to openDoors for future rooms.
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

            // Deconstruct the nullable tuple: piece = placed corridor, entryDoor = door used to connect.
            (Corridor placed, DoorSocket entryDoor) = result.Value;
            DoorSocket continuation = DungeonPlacer.FindContinuationDoor(placed, entryDoor);

            // Non-continuation free doors (e.g. a junction's side exits) become
            // open attachment points for future rooms.
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

        // If currentDoor is unchanged, no corridor was placed.
        return currentDoor != targetDoor ? currentDoor : null;
    }

    // Picks a weighted random sequence from DungeonRules.CorridorSequences.
    // Returns null if no valid sequences are defined.
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
    // The return type is a nullable value tuple: (Corridor, DoorSocket)? allows
    // returning null to signal "nothing was placed" without allocating a class.
    private (Corridor piece, DoorSocket entryDoor)? TryPlaceOnePiece(CorridorType type, DoorSocket targetDoor)
    {
        var candidates = new List<Corridor>();
        foreach (CorridorPrefabSet set in rules.CorridorPrefabs)
        {
            if (set.Type != type || set.Prefabs == null) continue;
            foreach (Corridor prefab in set.Prefabs) candidates.Add(prefab);
        }

        if (candidates.Count == 0) return null;

        foreach (int i in DungeonPlacer.RandomOrder(candidates.Count))
        {
            Corridor piece = Instantiate(candidates[i], dungeonRoot);

            DoorSocket entry = FindFittingDoor(piece, targetDoor);
            if (entry != null)
            {
                targetDoor.IsConnected = true;
                entry.IsConnected = true;
                piece.Type = type;
                placedPieces.Add(piece);
                return (piece, entry);
            }

            Destroy(piece.gameObject);
        }

        return null;
    }

    // Tries each door on piece (in random order) to find one that matches targetDoor's width
    // and allows placement without overlap. If found, piece is rotated and positioned in place
    // and the matched door is returned. Returns null if no door fits.
    // Only connect doors of matching width (Normal <-> Normal, Wide <-> Wide).
    private DoorSocket FindFittingDoor(DungeonPiece piece, DoorSocket targetDoor)
    {
        foreach (int j in DungeonPlacer.RandomOrder(piece.Doors.Length))
        {
            DoorSocket door = piece.Doors[j];
            if (door.Width != targetDoor.Width) continue;

            DungeonPlacer.RotatePieceToMatchDoors(piece, door, targetDoor);
            DungeonPlacer.AlignPiecePosition(piece, door, targetDoor);

            if (!DungeonPlacer.OverlapsAnyPiece(piece, placedPieces, rules.OverlapTolerance))
                return door;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Loop closing (atomic)
    // -------------------------------------------------------------------------

    // After the main generation tree is complete, tries to connect pairs of facing
    // open doors with a straight corridor, creating cycles in the dungeon graph.
    // "Atomic" means either the connection fully succeeds or nothing is placed.
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

    // Tries to connect doorA and doorB with a single straight corridor.
    // Places the corridor at doorA, then checks if the exit aligns precisely with doorB
    // (distance < 0.1 and dot product > 0.99 = nearly perfectly facing).
    // If not, the corridor is destroyed and we try the next candidate.
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

                DungeonPlacer.RotatePieceToMatchDoors(piece, entry, doorA);
                DungeonPlacer.AlignPiecePosition(piece, entry, doorA);

                if (DungeonPlacer.OverlapsAnyPiece(piece, placedPieces, rules.OverlapTolerance)) continue;

                DoorSocket exit = DungeonPlacer.FindContinuationDoor(piece, entry);
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
    // Room decoration
    // -------------------------------------------------------------------------

    private void DecorateRooms()
    {
        foreach (DungeonPiece piece in placedPieces)
        {
            if (piece is not Room room) continue;

            RoomDecorationRules decorRules = GetDecorationRulesFor(room.Type);
            if (decorRules == null) continue;

            Transform decorParent = new GameObject("Decorations").transform;
            decorParent.SetParent(room.transform);
            RoomDecorator.DecorateRoom(room, decorRules, decorParent);
        }
    }

    private RoomDecorationRules GetDecorationRulesFor(RoomType type)
    {
        foreach (RoomRule rule in rules.RoomRules)
            if (rule.Type == type) return rule.DecorationRules;
        return null;
    }

    // -------------------------------------------------------------------------
    // Door sealing
    // -------------------------------------------------------------------------

    // Spawns a wall prefab in front of every door that was never connected.
    private void SealOpenDoors()
    {
        if (rules.WallPrefab == null) return;

        foreach (DungeonPiece piece in placedPieces)
        {
            foreach (DoorSocket socket in piece.Doors)
            {
                if (socket.IsConnected) continue;
                GameObject wall = Instantiate(rules.WallPrefab, socket.transform.position, socket.transform.rotation, piece.transform);
                sealingWalls.Add(wall);
            }
        }
    }
}
