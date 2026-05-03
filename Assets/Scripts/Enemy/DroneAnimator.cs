using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DroneAnimator : MonoBehaviour
{
    [SerializeField] private Transform body;

    [Header("Tilt")]
    [SerializeField] private float maxPitchAngle = 18f;
    [SerializeField] private float maxBankAngle = 12f;
    [SerializeField] private float tiltSmoothing = 6f;

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.08f;
    [SerializeField] private float hoverFrequency = 1.2f;

    private NavMeshAgent agent;
    private Vector3 bodyBaseLocalPos;
    private float hoverPhase;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (body != null)
            bodyBaseLocalPos = body.localPosition;

        hoverPhase = Random.value;
    }

    private void Update()
    {
        if (body == null) return;

        Vector3 velocity = agent.velocity;
        float normalizedSpeed = agent.speed > 0f ? agent.speed : 1f;

        // Pitch forward proportional to forward speed (inertia)
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float targetPitch = -(forwardSpeed / normalizedSpeed) * maxPitchAngle;

        // Bank into turns: cross product gives +1 when turning right, -1 when turning left
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float bankSign = flatVelocity.magnitude > 0.1f
            ? Vector3.Cross(transform.forward, flatVelocity.normalized).y
            : 0f;
        float targetBank = bankSign * maxBankAngle;

        Quaternion targetRot = Quaternion.Euler(targetPitch, 0f, targetBank);
        body.localRotation = Quaternion.Slerp(body.localRotation, targetRot, Time.deltaTime * tiltSmoothing);

        // Hover bob on body child to avoid fighting NavMeshAgent Y control on root
        float hover = Mathf.Sin((Time.time * hoverFrequency + hoverPhase) * Mathf.PI * 2f) * hoverAmplitude;
        body.localPosition = bodyBaseLocalPos + new Vector3(0f, hover, 0f);
    }
}
