using System.Collections;
using System.Collections.Generic;
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

    [Header("Damage")]
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _tickInterval = 0.2f;

    private readonly Dictionary<Health, float> _nextHitTime = new();
    private bool _unlocked = false;
    private bool _firing = false;

    private void Start()
    {
        _laserLine.useWorldSpace = false;
        _laserLine.enabled = false;
        _laserCollider.enabled = false;
        StartCoroutine(AutoFireLoop());
    }

    public void Unlock() => _unlocked = true;
    public void ModifyInterval(float delta) => _laserInterval = Mathf.Max(0.5f, _laserInterval + delta);
    public void ModifyDuration(float delta) => _laserDuration = Mathf.Max(0.5f, _laserDuration + delta);
    public void ModifyLength(float delta) => _laserMaxLength = Mathf.Max(0.01f, _laserMaxLength + delta);

    public void StopLaser()
    {
        StopAllCoroutines();
        _laserLine.enabled = false;
        _laserCollider.enabled = false;
        _firing = false;
    }

    private IEnumerator AutoFireLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_laserInterval);

            if (_unlocked && !_firing)
                yield return FireCycle();
        }
    }

    private IEnumerator FireCycle()
    {
        _firing = true;

        _laserLine.SetPosition(0, Vector3.zero);
        _laserLine.SetPosition(1, Vector3.zero);
        _laserLine.enabled = true;
        _laserCollider.enabled = true;
        SoundManager.PlaySound(SoundType.LASER);

        float length = 0f;
        while (length < _laserMaxLength)
        {
            length = Mathf.Min(length + _laserExpandSpeed * Time.deltaTime, _laserMaxLength);
            _laserLine.SetPosition(1, new Vector3(0f, 0f, length));
            UpdateCollider(length);
            yield return null;
        }

        yield return new WaitForSeconds(_laserDuration);

        float retractStart = 0f;
        while (retractStart < _laserMaxLength)
        {
            retractStart = Mathf.Min(retractStart + _laserExpandSpeed * Time.deltaTime, _laserMaxLength);
            _laserLine.SetPosition(0, new Vector3(0f, 0f, retractStart));
            UpdateCollider(_laserMaxLength - retractStart);
            yield return null;
        }

        _laserLine.enabled = false;
        _laserCollider.enabled = false;
        _firing = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_laserCollider.enabled) return;
        if (!other.TryGetComponent<Health>(out var health)) return;
        if (_nextHitTime.TryGetValue(health, out float next) && Time.time < next) return;

        health.LoseHealth(_damage);
        _nextHitTime[health] = Time.time + _tickInterval;
    }

    private void UpdateCollider(float length)
    {
        _laserCollider.center = new Vector3(0f, 0f, length / 2f);
        _laserCollider.size = new Vector3(0.3f, 0.3f, length);
    }
}
