// Handles the hologram spawn effect: visual overlay + enemy freeze/invincibility during spawn.
// If other spawn effects are added, consider splitting the visual logic from the spawn state management
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HologramEffect : MonoBehaviour
{
    [SerializeField] private Material hologramMaterial;
    [SerializeField] private float phase1Duration = 0.8f;
    [SerializeField] private float holdDuration = 0.2f;
    [SerializeField] private float phase2Duration = 0.8f;

    private static readonly int Wave1ID = Shader.PropertyToID("_Wave1");
    private static readonly int Wave2ID = Shader.PropertyToID("_Wave2");
    private static readonly int WorldYMinID = Shader.PropertyToID("_WorldYMin");
    private static readonly int WorldYMaxID = Shader.PropertyToID("_WorldYMax");

    private Renderer[] originalRenderers;
    private ShadowCastingMode[] originalShadowModes;
    private Material holoInstance;
    private readonly List<GameObject> overlayCopies = new();
    private EnemyController enemyController;
    private UnityEngine.AI.NavMeshAgent navAgent;
    private Health health;

    private void BeginSpawning()
    {
        enemyController = GetComponent<EnemyController>();
        navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        health = GetComponent<Health>();

        if (enemyController != null) enemyController.enabled = false;
        // Disabling EnemyController stops new SetDestination calls, but the NavMeshAgent
        // keeps following its current path — stop it explicitly.
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.isStopped = true;
        if (health != null) health.IsInvincible = true;
    }

    private void EndSpawning()
    {
        if (navAgent != null) { if (navAgent.isOnNavMesh) navAgent.isStopped = false; navAgent = null; }
        if (enemyController != null) { enemyController.enabled = true; enemyController = null; }
        if (health != null) { health.IsInvincible = false; health = null; }
    }

    private void OnEnable() => StartEffect();

    public void StartEffect()
    {
        StopAllCoroutines();
        Cleanup();
        // Freeze immediately, before any Update runs on other components.
        BeginSpawning();
        StartCoroutine(BeginNextFrame());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        Cleanup();
    }

    private IEnumerator BeginNextFrame()
    {
        // One-frame delay: pool positions the object after OnEnable.
        yield return null;
        yield return Play();
    }

    private IEnumerator Play()
    {
        if (hologramMaterial == null) yield break;

        originalRenderers = GetComponentsInChildren<Renderer>();
        if (originalRenderers.Length == 0) yield break;

        holoInstance = new Material(hologramMaterial);

        (float yMin, float yMax) = ComputeWorldYBounds();
        holoInstance.SetFloat(WorldYMinID, yMin);
        holoInstance.SetFloat(WorldYMaxID, yMax);
        holoInstance.SetFloat(Wave1ID, 0f);
        holoInstance.SetFloat(Wave2ID, 0f);

        BuildOverlay();

        // Suppress original renderer shadows: the overlay ShadowCaster pass handles them
        // progressively (grows bottom-to-top with wave1, full during phase 2).
        originalShadowModes = new ShadowCastingMode[originalRenderers.Length];
        for (int i = 0; i < originalRenderers.Length; i++)
        {
            originalShadowModes[i] = originalRenderers[i].shadowCastingMode;
            originalRenderers[i].shadowCastingMode = ShadowCastingMode.Off;
        }

        SetOriginalRenderersEnabled(false);

        // Phase 1: hologram sweeps from bottom to top.
        float elapsed = 0f;
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            holoInstance.SetFloat(Wave1ID, Mathf.Clamp01(elapsed / phase1Duration));
            yield return null;
        }
        holoInstance.SetFloat(Wave1ID, 1f);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        // Phase 2: real materials reveal from bottom to top.
        // Overlay stays on top (Queue+1, Offset -1) but discards pixels below _Wave2,
        // so original renderers become visible as the hologram band sweeps upward.
        // Shadows remain suppressed on originals — handled by the overlay ShadowCaster.
        SetOriginalRenderersEnabled(true);
        elapsed = 0f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            holoInstance.SetFloat(Wave2ID, Mathf.Clamp01(elapsed / phase2Duration));
            yield return null;
        }

        Cleanup();
    }

    private void BuildOverlay()
    {
        foreach (Renderer r in originalRenderers)
        {
            if (!r.TryGetComponent(out MeshFilter mf)) continue;

            // Parent each copy directly to the renderer's own transform so it follows
            // any child animations (hover, bob, etc.) without needing per-frame updates.
            GameObject copy = new("HoloMesh");
            copy.transform.SetParent(r.transform, false);

            copy.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var mr = copy.AddComponent<MeshRenderer>();
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = holoInstance;
            mr.sharedMaterials = mats;
            overlayCopies.Add(copy);
        }
    }

    private void SetOriginalRenderersEnabled(bool enabled)
    {
        if (originalRenderers == null) return;
        foreach (Renderer r in originalRenderers)
            if (r != null) r.enabled = enabled;
    }

    private void Cleanup()
    {
        EndSpawning();
        RestoreOriginalShadows();
        SetOriginalRenderersEnabled(true);

        foreach (GameObject copy in overlayCopies)
            if (copy != null) Destroy(copy);
        overlayCopies.Clear();

        if (holoInstance != null)
        {
            Destroy(holoInstance);
            holoInstance = null;
        }

        originalRenderers = null;
    }

    private void RestoreOriginalShadows()
    {
        if (originalRenderers == null || originalShadowModes == null) return;
        for (int i = 0; i < originalRenderers.Length; i++)
            if (originalRenderers[i] != null)
                originalRenderers[i].shadowCastingMode = originalShadowModes[i];
        originalShadowModes = null;
    }

    private (float yMin, float yMax) ComputeWorldYBounds()
    {
        float yMin = float.MaxValue;
        float yMax = float.MinValue;
        foreach (Renderer r in originalRenderers)
        {
            yMin = Mathf.Min(yMin, r.bounds.min.y);
            yMax = Mathf.Max(yMax, r.bounds.max.y);
        }
        if (yMin == float.MaxValue) { yMin = -0.5f; yMax = 0.5f; }
        return (yMin, yMax);
    }
}
