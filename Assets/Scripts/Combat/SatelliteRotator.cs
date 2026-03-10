using UnityEngine;

public class SatelliteRotator : MonoBehaviour
{
    [SerializeField]
    private float _rotationSpeed = 100f;
    [SerializeField]
    private Transform _player;

    private void Update()
    {
        transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
        transform.position = _player.position;
    }

    public void StopRotation()
    {
        _rotationSpeed = 0;
    }
}
