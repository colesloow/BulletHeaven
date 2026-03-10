using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Room startRoom;
    [SerializeField] private Room[] roomPrefabs;
    [SerializeField] private int roomCount = 10;

    // Amount by which room bounds are shrunk before overlap testing,
    // so that rooms sharing a wall do not incorrectly trigger a collision.
    [SerializeField] private float overlapTolerance = 0.2f;

    private readonly List<DoorSocket> openDoors = new(); // available door sockets
    private readonly List<Room> placedRooms = new();

    private void Start()
    {
        // Instantiate the starting room at the world origin
        Room firstRoom = Instantiate(startRoom, Vector3.zero, Quaternion.identity);
        placedRooms.Add(firstRoom);

        foreach (DoorSocket door in firstRoom.Doors)
            openDoors.Add(door);

        // Generate additional rooms one by one
        for (int i = 0; i < roomCount; i++)
        {
            if (openDoors.Count == 0)
                break;

            int randomIndex = Random.Range(0, openDoors.Count);
            DoorSocket targetDoor = openDoors[randomIndex];
            openDoors.RemoveAt(randomIndex);

            AttachRoom(targetDoor);
        }
    }

    private void AttachRoom(DoorSocket targetDoor)
    {
        // Try each room prefab in a random order
        int[] prefabOrder = RandomOrder(roomPrefabs.Length);

        foreach (int i in prefabOrder)
        {
            Room newRoom = Instantiate(roomPrefabs[i]);

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

                    // Register the remaining doors of the new room as available connection points
                    foreach (DoorSocket door in newRoom.Doors)
                    {
                        if (!door.IsConnected)
                            openDoors.Add(door);
                    }

                    placedRooms.Add(newRoom);
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
