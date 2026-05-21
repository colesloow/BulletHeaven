using UnityEngine;

// Drives the floating head mesh independently of the body.
// No rigidbody or collider on the head ; position and rotation are set directly each frame.
[RequireComponent(typeof(Rigidbody))]
public class PlayerHeadAnimator : MonoBehaviour
{
    [SerializeField] private Transform head;
    // World offset from the player root to the head's resting position.
    // Leave at (0,0,0) if the head pivot is already placed correctly in the scene.
    [SerializeField] private Vector3 restOffset = new Vector3(0f, 1.3f, 0f);

    [Header("Lag")]
    // How far the head drifts per unit of speed. Controls how much inertia is visible.
    [SerializeField] private float lagStrength = 0.06f;
    // Hard cap on drift distance regardless of speed.
    [SerializeField] private float maxLagDistance = 0.35f;
    // Lerp speed while the head is trailing behind the body during normal movement.
    [SerializeField] private float followSpeed = 4f;
    // Lerp speed used when snapping back to rest (on stop or direction reversal).
    // Higher = more reactive, lower = more floaty.
    [SerializeField] private float returnSpeed = 20f;

    [Header("Tilt")]
    // Maximum tilt angle reached when the head is at full lag distance.
    [SerializeField] private float maxTiltAngle = 12f;
    // Smoothing applied to the rotation to avoid snapping. Lower = snappier.
    [SerializeField] private float tiltSmoothing = 8f;

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.04f;
    [SerializeField] private float hoverFrequency = 1.3f;

    private Rigidbody rb;
    // Current lag displacement of the head relative to its rest position.
    private Vector3 springOffset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (head == null) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
        float speed = flatVel.magnitude;

        // Head trails opposite to movement direction, capped at maxLagDistance.
        Vector3 targetOffset = speed > 0.01f
            ? -flatVel.normalized * Mathf.Min(speed * lagStrength, maxLagDistance)
            : Vector3.zero;

        // Use a fast lerp when the player stops or sharply reverses direction so the
        // head snaps back quickly rather than slowly crossing through the center.
        bool reversing = targetOffset.sqrMagnitude > 0.001f
            && springOffset.sqrMagnitude > 0.001f
            && Vector3.Dot(springOffset.normalized, targetOffset.normalized) < 0f;
        float lerpSpeed = (speed <= 0.01f || reversing) ? returnSpeed : followSpeed;
        springOffset = Vector3.Lerp(springOffset, targetOffset, Time.deltaTime * lerpSpeed);

        float hover = Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        head.position = transform.position + restOffset + springOffset + Vector3.up * hover;

        // Tilt the head so the flat face angles toward the body as the lag increases.
        // Angle is proportional to lag: 0 at rest, maxTiltAngle at full drift.
        Vector3 springXZ = new(springOffset.x, 0f, springOffset.z);
        Quaternion targetRot = Quaternion.identity;
        if (springXZ.sqrMagnitude > 0.001f)
        {
            Quaternion yawRot = Quaternion.LookRotation(-springXZ.normalized, Vector3.up);
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, springXZ.normalized);
            float tiltAngle = springXZ.magnitude / maxLagDistance * maxTiltAngle;
            targetRot = Quaternion.AngleAxis(tiltAngle, tiltAxis) * yawRot;
        }
        head.rotation = Quaternion.Slerp(head.rotation, targetRot, Time.deltaTime * tiltSmoothing);
    }

    private void OnDrawGizmosSelected()
    {
        if (head == null) return;

        Vector3 restPos = transform.position + restOffset;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(restPos, 0.05f);                  // rest position

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(restPos + springOffset, 0.08f);   // lag target
        Gizmos.DrawLine(restPos, restPos + springOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(head.position, 0.1f);             // actual head position
    }
}
