using UnityEngine;

public enum DoorWidth { Normal, Wide }

public class DoorSocket : MonoBehaviour
{
    public bool IsConnected = false;
    public DoorWidth Width = DoorWidth.Normal;

    void OnDrawGizmos()
    {
        Gizmos.color = Width == DoorWidth.Wide ? Color.cyan : Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2);
    }
}