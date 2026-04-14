using UnityEngine;

// Placed as child GameObjects inside room prefabs to mark valid wall positions for furniture sets.
// Position the socket against the wall; set forward (+Z) pointing into the room interior.
// The decorator shuffles all sockets and distributes WallEntries variants across them.
public class WallSocket : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.85f);
        Gizmos.matrix = transform.localToWorldMatrix;
        // Flat rectangle representing the wall slot footprint.
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.2f, 1.8f, 0.05f));
        // Arrow showing the room-facing direction (forward = into the room).
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * 0.6f);
    }
#endif
}
