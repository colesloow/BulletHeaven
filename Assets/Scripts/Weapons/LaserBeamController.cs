using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeamController : MonoBehaviour
{
    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private float laserMaxLength = 5f;
    [SerializeField] private float laserExpandSpeed = 5f;
    [SerializeField] private float laserDuration = 5f;
    [SerializeField] private float laserInterval = 5f;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float tickInterval = 0.2f;
    [SerializeField] private float contactRadius = 0.5f;

    private readonly Dictionary<Health, float> nextHitTime = new();
    private bool unlocked = false;
    private bool firing = false;
    private float currentBeamStart = 0f;
    private float currentBeamEnd = 0f;

    private void Start()
    {
        laserLine.useWorldSpace = false;
        laserLine.enabled = false;
        StartCoroutine(AutoFireLoop());
    }

    public void Unlock() => unlocked = true;
    public void ModifyInterval(float delta) => laserInterval = Mathf.Max(0.5f, laserInterval + delta);
    public void ModifyDuration(float delta) => laserDuration = Mathf.Max(0.5f, laserDuration + delta);
    public void ModifyLength(float delta) => laserMaxLength = Mathf.Max(0.01f, laserMaxLength + delta);

    public void StopLaser()
    {
        StopAllCoroutines();
        laserLine.enabled = false;
        firing = false;
    }

    private void Update()
    {
        if (!firing) return;
        CheckHits();
    }

    private IEnumerator AutoFireLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(laserInterval);

            if (unlocked && !firing)
                yield return FireCycle();
        }
    }

    private IEnumerator FireCycle()
    {
        firing = true;
        laserLine.SetPosition(0, Vector3.zero);
        laserLine.SetPosition(1, Vector3.zero);
        laserLine.enabled = true;
        SoundManager.PlaySound(SoundType.LASER);

        float length = 0f;
        while (length < laserMaxLength)
        {
            length = Mathf.Min(length + laserExpandSpeed * Time.deltaTime, laserMaxLength);
            laserLine.SetPosition(0, new Vector3(0f, 0f, length));
            currentBeamStart = 0f;
            currentBeamEnd = length;
            yield return null;
        }

        yield return new WaitForSeconds(laserDuration);

        float retractStart = 0f;
        while (retractStart < laserMaxLength)
        {
            retractStart = Mathf.Min(retractStart + laserExpandSpeed * Time.deltaTime, laserMaxLength);
            laserLine.SetPosition(1, new Vector3(0f, 0f, retractStart));
            currentBeamStart = retractStart;
            currentBeamEnd = laserMaxLength;
            yield return null;
        }

        laserLine.enabled = false;
        currentBeamStart = 0f;
        currentBeamEnd = 0f;
        firing = false;
    }

    private void CheckHits()
    {
        if (currentBeamEnd <= currentBeamStart) return;

        Vector3 worldStart = laserLine.transform.TransformPoint(laserLine.GetPosition(0));
        Vector3 worldEnd = laserLine.transform.TransformPoint(laserLine.GetPosition(1));

        Vector2 start2D = new(worldStart.x, worldStart.z);
        Vector2 end2D = new(worldEnd.x, worldEnd.z);

        foreach (Health enemy in Health.ActiveEnemies)
        {
            if (nextHitTime.TryGetValue(enemy, out float next) && Time.time < next) continue;

            Vector2 enemy2D = new(enemy.transform.position.x, enemy.transform.position.z);
            Vector2 closest = ClosestPointOnSegment(start2D, end2D, enemy2D);
            if (Vector2.Distance(closest, enemy2D) > contactRadius) continue;

            enemy.LoseHealth(damage);
            nextHitTime[enemy] = Time.time + tickInterval;
        }
    }

    private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return a + t * ab;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !firing) return;
        Vector3 worldStart = laserLine.transform.TransformPoint(laserLine.GetPosition(0));
        Vector3 worldEnd = laserLine.transform.TransformPoint(laserLine.GetPosition(1));
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        for (int i = 0; i <= 12; i++)
        {
            Vector3 p = Vector3.Lerp(worldStart, worldEnd, (float)i / 12);
            p.y = 0f;
            Gizmos.DrawWireSphere(p, contactRadius);
        }
    }
}
