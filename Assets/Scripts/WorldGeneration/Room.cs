using UnityEngine;

public class Room : MonoBehaviour
{
    public DoorSocket[] Doors;

    void Awake()
    {
        Doors = GetComponentsInChildren<DoorSocket>();
    }
}