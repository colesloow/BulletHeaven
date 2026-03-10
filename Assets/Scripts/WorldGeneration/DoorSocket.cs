using UnityEngine;

public class DoorSocket : MonoBehaviour
{
    public bool IsConnected = false;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2);
    }
}