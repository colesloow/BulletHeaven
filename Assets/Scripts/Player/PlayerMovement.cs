using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private Transform bodyMesh;

    private Rigidbody rb;
    private Vector3 _moveDirection;
    private Camera mainCamera;

    public void AddSpeed(float amount) => speed += amount;

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        Vector3 camForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
        _moveDirection = (camForward * input.y + camRight * input.x).normalized;

        // Drive rolling from actual Rigidbody velocity so it reflects real movement
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude > 0.01f)
        {
            float distance = velocity.magnitude * Time.deltaTime;
            float angle = distance / radius * Mathf.Rad2Deg;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            bodyMesh.Rotate(rollAxis, angle, Space.World);
        }
    }

    private void FixedUpdate()
    {
        Vector3 targetVelocity = _moveDirection * speed;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }
}
