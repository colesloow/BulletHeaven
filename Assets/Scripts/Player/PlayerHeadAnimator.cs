using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerHeadAnimator : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Vector3 restOffset = new Vector3(0f, 1.3f, 0f);

    [Header("Lag")]
    [SerializeField] private float lagStrength = 0.06f;
    [SerializeField] private float maxLagDistance = 0.35f;
    [SerializeField] private float springStiffness = 22f;
    [SerializeField] private float springDamping = 7f;

    [Header("Tilt")]
    [SerializeField] private float maxTiltAngle = 20f;
    [SerializeField] private float tiltSpeedReference = 5f;
    [SerializeField] private float tiltSmoothing = 8f;

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.04f;
    [SerializeField] private float hoverFrequency = 1.3f;

    private Rigidbody rb;
    private Vector3 springVelocity;
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

        // Spring lag: pull head opposite to movement direction
        Vector3 targetOffset = speed > 0.01f
            ? -flatVel.normalized * Mathf.Min(speed * lagStrength, maxLagDistance)
            : Vector3.zero;

        Vector3 springForce = (targetOffset - springOffset) * springStiffness - springVelocity * springDamping;
        springVelocity += springForce * Time.deltaTime;
        springOffset += springVelocity * Time.deltaTime;

        // Hover bob
        float hover = Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;

        head.position = transform.position + restOffset + springOffset + Vector3.up * hover;

        // Tilt: decompose into fixed world-axis rotations to avoid axis-flip spinning
        float pitch = Mathf.Clamp(-flatVel.z / tiltSpeedReference * maxTiltAngle, -maxTiltAngle, maxTiltAngle);
        float bank = Mathf.Clamp(flatVel.x / tiltSpeedReference * maxTiltAngle, -maxTiltAngle, maxTiltAngle);
        Quaternion targetRot = Quaternion.Euler(pitch, 0f, -bank);
        head.rotation = Quaternion.Slerp(head.rotation, targetRot, Time.deltaTime * tiltSmoothing);
    }
}
