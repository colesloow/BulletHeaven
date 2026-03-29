using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _radius = 0.5f;
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private Transform _bodyMesh;

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;

    public void AddSpeed(float amount) => _speed += amount;

    private void OnEnable()
    {
        _moveAction.action.Enable();
    }

    private void OnDisable()
    {
        _moveAction.action.Disable();
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector2 input = _moveAction.action.ReadValue<Vector2>();

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up).normalized;
        _moveDirection = (camForward * input.y + camRight * input.x).normalized;

        // Drive rolling from actual Rigidbody velocity so it reflects real movement
        Vector3 velocity = _rigidbody.linearVelocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude > 0.01f)
        {
            float distance = velocity.magnitude * Time.deltaTime;
            float angle = distance / _radius * Mathf.Rad2Deg;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            _bodyMesh.Rotate(rollAxis, angle, Space.World);
        }
    }

    private void FixedUpdate()
    {
        Vector3 targetVelocity = _moveDirection * _speed;
        _rigidbody.linearVelocity = Vector3.MoveTowards(_rigidbody.linearVelocity, targetVelocity, _acceleration * Time.fixedDeltaTime);
    }
}
