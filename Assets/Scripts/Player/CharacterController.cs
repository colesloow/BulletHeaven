using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float _movementSpeed;
    [SerializeField]
    private InputActionReference _moveActionReference;

    [Header("Boost Settings")]
    [SerializeField]
    private float _boostSpeed;
    [SerializeField]
    private InputActionReference _boostActionReference;

    private Animator animator;
    private bool _boostUnlocked = false;
    private const string ANIMATOR_BOOL_IS_MOVING = "IsMoving";
    private const string ANIMATOR_BOOL_IS_RUNNING = "IsRunning";

    private void Start()
    {
        GameManager.Instance.OnBoostUnlocked += UnlockBoost; // subscribe to boost unlock event
        animator = GetComponent<Animator>();

        _moveActionReference.action.Enable();
        _boostActionReference.action.Enable();
    }

    private void Update()
    {
        float boost = 1;

        // check if boost is unlocked and activated
        if (_boostUnlocked && _boostActionReference.action.phase == InputActionPhase.Performed)
        {
            boost = _boostSpeed;
        }

        // read movement inputs
        Vector2 frameMovement = _moveActionReference.action.ReadValue<Vector2>();
        Vector3 frameMovement3D = new Vector3(frameMovement.x, 0, frameMovement.y);

        // character movement
        Vector3 newPos = transform.position + frameMovement3D * _movementSpeed * boost * Time.deltaTime;
        transform.position = newPos;

        // set is moving bool
        bool isMoving = frameMovement3D.sqrMagnitude > 0.01f;
        animator.SetBool(ANIMATOR_BOOL_IS_MOVING, isMoving);

        // set is running bool
        bool isRunning = boost > 1;
        animator.SetBool(ANIMATOR_BOOL_IS_RUNNING, isRunning);

        // calculate direction and rotation
        if (isMoving) // check for a significant movement
        {
            Quaternion targetRotation = Quaternion.LookRotation(frameMovement3D.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    private void UnlockBoost(bool unlocked)
    {
        _boostUnlocked = unlocked;
    }

    public void DisableMovement()
    {
        _moveActionReference.action.Disable();
        _boostActionReference.action.Disable();
        animator.SetBool(ANIMATOR_BOOL_IS_MOVING, false);
        animator.SetBool(ANIMATOR_BOOL_IS_RUNNING, false);
    }


}
