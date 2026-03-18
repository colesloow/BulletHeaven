using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _radius = 0.5f;
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private Transform _bodyMesh;

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;

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
        _moveDirection = new Vector3(input.x, 0f, input.y).normalized;

        if (_moveDirection.sqrMagnitude > 0.01f)
        {
            // Rolling: angle = distance / radius, axis perpendicular to movement direction
            float distance = _speed * Time.deltaTime;
            float angle = distance / _radius * Mathf.Rad2Deg;
            Vector3 rollAxis = Vector3.Cross(Vector3.up, _moveDirection);
            _bodyMesh.Rotate(rollAxis, angle, Space.World);
        }
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = _moveDirection * _speed;
    }
}
