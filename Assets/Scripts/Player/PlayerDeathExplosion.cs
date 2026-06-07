using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathExplosion : MonoBehaviour
{
    [SerializeField] private Transform robotRoot;
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private float explosionForce = 5f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private float upwardModifier = 0.4f;

    private struct PartState
    {
        public GameObject obj;
        public Transform originalParent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private readonly List<PartState> parts = new();
    private GameObject tempFloor;

    public void Explode()
    {
        Cleanup();
        StartCoroutine(DoExplosion());
    }

    // Removes physics components and restores all parts to their original position in the hierarchy.
    // Must be called before a new game starts so the robot looks intact again.
    public void Cleanup()
    {
        StopAllCoroutines();

        foreach (PartState ps in parts)
        {
            if (ps.obj == null) continue;

            Rigidbody rb = ps.obj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            MeshCollider col = ps.obj.GetComponent<MeshCollider>();
            if (col != null) Destroy(col);

            ps.obj.transform.SetParent(ps.originalParent, false);
            ps.obj.transform.localPosition = ps.localPosition;
            ps.obj.transform.localRotation = ps.localRotation;
            ps.obj.transform.localScale = ps.localScale;
        }
        parts.Clear();

        if (tempFloor != null)
        {
            Destroy(tempFloor);
            tempFloor = null;
        }
    }

    private IEnumerator DoExplosion()
    {
        MeshFilter[] meshFilters = robotRoot.GetComponentsInChildren<MeshFilter>();
        Vector3 center = robotRoot.position;

        // Floor at y=0 matches the dungeon floor level; prevents parts from falling through.
        tempFloor = CreateTempFloor();

        var rigidbodies = new Rigidbody[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            Transform t = meshFilters[i].transform;

            parts.Add(new PartState
            {
                obj = t.gameObject,
                originalParent = t.parent,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            });

            // Detach so physics is independent of the Player Rigidbody.
            t.SetParent(null, true);

            MeshCollider col = t.gameObject.AddComponent<MeshCollider>();
            col.convex = true;

            Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbodies[i] = rb;
        }

        // One physics step so all colliders are registered before the impulse.
        yield return new WaitForFixedUpdate();

        if (explosionVFX != null)
            Instantiate(explosionVFX, center, Quaternion.identity);

        foreach (Rigidbody rb in rigidbodies)
            rb.AddExplosionForce(explosionForce, center, explosionRadius, upwardModifier, ForceMode.Impulse);
    }

    private GameObject CreateTempFloor()
    {
        var floor = new GameObject("TempDeathFloor");
        floor.transform.position = Vector3.zero;
        BoxCollider col = floor.AddComponent<BoxCollider>();
        col.size = new Vector3(200f, 0.1f, 200f);
        return floor;
    }
}
