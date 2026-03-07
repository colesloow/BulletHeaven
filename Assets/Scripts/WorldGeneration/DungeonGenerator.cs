using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private Room startRoom;
    [SerializeField] private Room[] roomPrefabs;

    private List<DoorSocket> openDoors = new List<DoorSocket>(); // available door sockets

    private void Start()
    {
        // Instantiate the starting room at world origin
        Room firstRoom = Instantiate(startRoom, Vector3.zero, Quaternion.identity);

        // Register all door sockets of the starting room as available connection points
        foreach (DoorSocket door in firstRoom.Doors)
        {
            openDoors.Add(door);
        }

        // Test: attach a new room to the first available door
        AttachRoom(openDoors[0]);
    }

    private void AttachRoom(DoorSocket targetDoor)
    {
        // Pick a random room prefab from the available set
        Room prefab = roomPrefabs[Random.Range(0, roomPrefabs.Length)];

        Room newRoom = Instantiate(prefab);

        // Pick a random door from the newly created room
        DoorSocket newDoor = newRoom.Doors[Random.Range(0, newRoom.Doors.Length)];

        // Calculate rotation: align forward direction of the new door with the opposite direction of the target door
        // (ensures both doors face each other)
        Quaternion rotation = Quaternion.FromToRotation(newDoor.transform.forward, -targetDoor.transform.forward);

        newRoom.transform.rotation = rotation * newRoom.transform.rotation;

        // After rotating the room, compute the positional offset
        // between the new door and the room's root transform
        Vector3 offset = newDoor.transform.position - newRoom.transform.position;

        // Move the room so that the new door perfectly overlaps the target door
        newRoom.transform.position = targetDoor.transform.position - offset;

        // Mark both doors as connected so they are not reused
        targetDoor.IsConnected = true;
        newDoor.IsConnected = true;

        // Register all remaining unconnected doors of the new room
        // as potential attachment points for future rooms
        foreach (DoorSocket door in newRoom.Doors)
        {
            if (!door.IsConnected)
                openDoors.Add(door);
        }
    }
}