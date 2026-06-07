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

    [SerializeField] private ParticleSystem tipVFX;

    [Header("Damage")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float tickInterval = 0.2f;
    [SerializeField] private float contactRadius = 0.5f;

    private readonly Dictionary<Health, float> nextHitTime = new();
    private bool unlocked = false;
    private bool firing = false;
    private float currentBeamStart = 0f;
    private float currentBeamEnd = 0f;
    private Coroutine autoFireCoroutine;

    // Time until the next fire, set by Unlock() to sync with other lasers.
    private float nextFireDelay = -1f;

    private void OnEnable() => Health.OnEnemyDisabled += OnEnemyRemoved;
    private void OnDisable() => Health.OnEnemyDisabled -= OnEnemyRemoved;
    private void OnEnemyRemoved(Health enemy) => nextHitTime.Remove(enemy);

    private void Start()
    {
        laserLine.useWorldSpace = true;
        laserLine.enabled = false;
        // Unlock() may have already started the coroutine before Start() ran.
        if (autoFireCoroutine == null)
            autoFireCoroutine = StartCoroutine(AutoFireLoop());
    }

    // initialDelay: seconds before the first shot, used by SatelliteWeapon to sync multiple lasers.
    // -1 means "use laserInterval" (default behaviour for the first laser).
    public void Unlock(float initialDelay = -1f)
    {
        // Already unlocked with no sync needed — avoid restarting a running loop.
        if (unlocked && initialDelay < 0f) return;

        unlocked = true;
        nextFireDelay = initialDelay;

        // Restart the loop so it picks up nextFireDelay even if Start() already ran.
        if (autoFireCoroutine != null) StopCoroutine(autoFireCoroutine);
        laserLine.enabled = false;
        if (tipVFX != null) tipVFX.Stop();
        firing = false;
        currentBeamStart = 0f;
        currentBeamEnd = 0f;

        // isActiveAndEnabled guard: if called before Start(), autoFireCoroutine stays null
        // and Start() will launch the loop itself once the GO is active.
        if (isActiveAndEnabled)
            autoFireCoroutine = StartCoroutine(AutoFireLoop());
    }

    public void ModifyInterval(float delta) => laserInterval = Mathf.Max(0.5f, laserInterval + delta);
    public void ModifyDuration(float delta) => laserDuration = Mathf.Max(0.5f, laserDuration + delta);
    public void ModifyLength(float delta) => laserMaxLength = Mathf.Max(0.01f, laserMaxLength + delta);

    public void StopLaser()
    {
        if (autoFireCoroutine != null) StopCoroutine(autoFireCoroutine);
        laserLine.enabled = false;
        firing = false;
    }

    private void Update()
    {
        if (!firing) return;
        CheckHits();
    }

    // Counts down through both the idle wait and the active FireCycle, so SatelliteWeapon
    // can pass it directly as initialDelay to a newly added laser for perfect sync.
    public float TimeUntilNextFire { get; private set; } = 0f;
    public float LaserInterval => laserInterval;

    private IEnumerator AutoFireLoop()
    {
        float delay = nextFireDelay >= 0f ? nextFireDelay : laserInterval;
        nextFireDelay = -1f;

        while (true)
        {
            float remaining = delay;
            while (remaining > 0f)
            {
                TimeUntilNextFire = remaining;
                remaining -= Time.deltaTime;
                yield return null;
            }
            TimeUntilNextFire = 0f;

            if (unlocked)
                yield return FireCycle();

            delay = laserInterval;
        }
    }

    private IEnumerator FireCycle()
    {
        firing = true;
        float expandTime = laserMaxLength / laserExpandSpeed;
        // Used to keep TimeUntilNextFire accurate while the beam is active.
        float remainingCycle = expandTime + laserDuration + expandTime;

        laserLine.SetPosition(0, laserLine.transform.position);
        laserLine.SetPosition(1, laserLine.transform.position);
        laserLine.enabled = true;
        SoundManager.PlaySound(SoundType.LASER);

        if (tipVFX != null)
        {
            tipVFX.transform.position = laserLine.transform.position;
            tipVFX.Play();
        }

        float length = 0f;
        while (length < laserMaxLength)
        {
            remainingCycle -= Time.deltaTime;
            TimeUntilNextFire = laserInterval + Mathf.Max(0f, remainingCycle);
            length = Mathf.Min(length + laserExpandSpeed * Time.deltaTime, laserMaxLength);
            UpdateBeamPositions(0f, length);
            yield return null;
        }

        float elapsed = 0f;
        while (elapsed < laserDuration)
        {
            remainingCycle -= Time.deltaTime;
            TimeUntilNextFire = laserInterval + Mathf.Max(0f, remainingCycle);
            elapsed += Time.deltaTime;
            UpdateBeamPositions(0f, laserMaxLength);
            yield return null;
        }

        // Retract by advancing the base toward the tip along the current forward direction.
        float retractProgress = 0f;
        while (retractProgress < laserMaxLength)
        {
            remainingCycle -= Time.deltaTime;
            TimeUntilNextFire = laserInterval + Mathf.Max(0f, remainingCycle);
            retractProgress = Mathf.Min(retractProgress + laserExpandSpeed * Time.deltaTime, laserMaxLength);
            UpdateBeamPositions(retractProgress, laserMaxLength);
            yield return null;
        }

        if (tipVFX != null) tipVFX.Stop();
        laserLine.enabled = false;
        currentBeamStart = 0f;
        currentBeamEnd = 0f;
        firing = false;
    }

    private void UpdateBeamPositions(float startDist, float endDist)
    {
        Vector3 origin = laserLine.transform.position;
        Vector3 dir = laserLine.transform.forward;
        Vector3 basePos = origin + dir * startDist;
        Vector3 tip = origin + dir * endDist;
        // position 0 = tip (index 0 = start of gradient = pink/source color in material)
        // position 1 = base (index 1 = end of gradient = blue/impact color)
        laserLine.SetPosition(0, tip);
        laserLine.SetPosition(1, basePos);
        currentBeamStart = startDist;
        currentBeamEnd = endDist;
        if (tipVFX != null) tipVFX.transform.position = tip;
    }

    private void CheckHits()
    {
        if (currentBeamEnd <= currentBeamStart) return;

        Vector3 worldStart = laserLine.GetPosition(0);
        Vector3 worldEnd = laserLine.GetPosition(1);

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
        Vector3 worldStart = laserLine.GetPosition(0);
        Vector3 worldEnd = laserLine.GetPosition(1);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        for (int i = 0; i <= 12; i++)
        {
            Vector3 p = Vector3.Lerp(worldStart, worldEnd, (float)i / 12);
            p.y = 0f;
            Gizmos.DrawWireSphere(p, contactRadius);
        }
    }
}
