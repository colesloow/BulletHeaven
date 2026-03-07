using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Room startRoom;
    [SerializeField] private Room[] roomPrefabs;
    [SerializeField] private int roomCount = 10;

    // List of door sockets that are currently available for attaching new rooms
    private List<DoorSocket> openDoors = new List<DoorSocket>();

    private void Start()
    {
        // Instantiate the starting room at world origin
        Room firstRoom = Instantiate(startRoom, Vector3.zero, Quaternion.identity);

        // Register all door sockets of the starting room
        foreach (DoorSocket door in firstRoom.Doors)
        {
            openDoors.Add(door);
        }

        // Generate additional rooms
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
        // Select a random room prefab
        Room prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

        // Instantiate the room
        Room newRoom = Instantiate(prefab);

        // Select a random door from the new room
        DoorSocket newDoor = newRoom.Doors[Random.Range(0, newRoom.Doors.Length)];

        // Rotate the room so both doors face each other
        RotateRoomToMatchDoors(newRoom, newDoor, targetDoor);

        // Move the room so both door sockets overlap
        AlignRoomPosition(newRoom, newDoor, targetDoor);

        // Mark doors as connected
        targetDoor.IsConnected = true;
        newDoor.IsConnected = true;

        // Register remaining doors as available connection points
        foreach (DoorSocket door in newRoom.Doors)
        {
            if (!door.IsConnected)
            {
                openDoors.Add(door);
            }
        }
    }

    private void RotateRoomToMatchDoors(Room room, DoorSocket newDoor, DoorSocket targetDoor)
    {
        // Get forward directions and flatten them on the horizontal plane
        Vector3 targetForward = targetDoor.transform.forward;
        Vector3 newForward = newDoor.transform.forward;

        targetForward.y = 0f;
        newForward.y = 0f;

        targetForward.Normalize();
        newForward.Normalize();

        // Compute signed angle around the Y axis
        float angle = Vector3.SignedAngle(newForward, -targetForward, Vector3.up);

        // Apply rotation only around Y axis
        room.transform.Rotate(0f, angle, 0f);
    }

    private void AlignRoomPosition(Room room, DoorSocket newDoor, DoorSocket targetDoor)
    {
        // Compute the positional offset between the new door and the room root
        Vector3 offset = newDoor.transform.position - room.transform.position;

        // Move the room so both doors perfectly overlap
        room.transform.position = targetDoor.transform.position - offset;
    }
}