using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private float _speed;
    
    private void Start()
    {
        _player = FindFirstObjectByType<CharacterController>().transform;
    }

    void Update()
    {
        // Move our position a step closer to the target.
        var step = _speed * Time.deltaTime; // calculate distance to move
        transform.position = Vector3.MoveTowards(transform.position, _player.position, step);

        Vector3 relativePos = _player.position - transform.position;

        // the second argument, upwards, defaults to Vector3.up
        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        transform.rotation = rotation;
    }

}
