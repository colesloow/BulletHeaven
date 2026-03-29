using System.Collections;
using UnityEngine;

public class LaserBeamController : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private LineRenderer _laserLine;
    [SerializeField] private BoxCollider _laserCollider;
    [SerializeField] private float _laserMaxLength = 5f;
    [SerializeField] private float _laserExpandSpeed = 5f;
    [SerializeField] private float _laserDuration = 5f;
    [SerializeField] private float _laserInterval = 5f;

    private Coroutine _laserCoroutine;
    private bool _unlocked = false;

    private void Start()
    {
        _laserCollider.enabled = false;
        StartCoroutine(AutoFireLoop());
    }

    // Called by SatelliteWeapon when the laser upgrade is applied
    public void Unlock()
    {
        _unlocked = true;
    }

    public void ModifyInterval(float delta)
    {
        _laserInterval = Mathf.Max(0.5f, _laserInterval + delta);
    }

    public void ModifyDuration(float delta)
    {
        _laserDuration = Mathf.Max(0.5f, _laserDuration + delta);
    }

    public void ModifyLength(float delta)
    {
        _laserMaxLength = Mathf.Max(1f, _laserMaxLength + delta);
    }

    public void StopLaser()
    {
        if (_laserCoroutine != null)
            StopCoroutine(_laserCoroutine);

        _laserCoroutine = StartCoroutine(RetractLaser());
    }

    private IEnumerator AutoFireLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_laserInterval);

            if (_unlocked)
                StartLaser();
        }
    }

    private void StartLaser()
    {
        if (_laserCoroutine != null)
            StopCoroutine(_laserCoroutine);

        _laserCoroutine = StartCoroutine(ShootLaser());
    }

    private IEnumerator ShootLaser()
    {
        _laserLine.enabled = true;
        _laserCollider.enabled = true;

        Vector3[] positions = new Vector3[2];
        positions[0] = Vector3.zero;
        positions[1] = Vector3.zero;

        SoundManager.PlaySound(SoundType.LASER);
        _laserLine.SetPositions(positions);

        float currentLength = 0f;

        while (currentLength < _laserMaxLength)
        {
            currentLength += _laserExpandSpeed * Time.deltaTime;
            positions[1].z = Mathf.Min(currentLength, _laserMaxLength);
            _laserLine.SetPositions(positions);
            UpdateCollider(currentLength);
            yield return null;
        }

        yield return new WaitForSeconds(_laserDuration);

        StopLaser();
    }

    private IEnumerator RetractLaser()
    {
        Vector3[] positions = new Vector3[2];
        _laserLine.GetPositions(positions);

        float currentStart = positions[0].z;
        float currentEnd = positions[1].z;

        while (currentStart < currentEnd)
        {
            currentStart += _laserExpandSpeed * Time.deltaTime;
            positions[0].z = Mathf.Min(currentStart, currentEnd);
            _laserLine.SetPositions(positions);
            UpdateCollider(currentEnd - currentStart);
            yield return null;
        }

        _laserLine.enabled = false;
        _laserCollider.enabled = false;
    }

    private void UpdateCollider(float length)
    {
        _laserCollider.center = new Vector3(0, 0, length / 2);
        _laserCollider.size = new Vector3(0.001f, 0.001f, length);
    }
}
