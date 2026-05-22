using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public static readonly List<Health> ActiveEnemies = new();
    public static event Action<Health> OnEnemyDisabled;

    [SerializeField]
    private float maxHealth = 100f;
    [SerializeField]
    private float currentHealth;

    private float baseMaxHealth;
    private Animator animator;
    private WeaponManager weaponManager;
    private PooledObject pooledObject;

    public event Action<float> OnDamaged;

    public bool IsDead { get; private set; }

    private void OnEnable()
    {
        if (CompareTag(Tags.Enemy)) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        if (CompareTag(Tags.Enemy))
        {
            ActiveEnemies.Remove(this);
            OnEnemyDisabled?.Invoke(this);
        }
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        weaponManager = GetComponent<WeaponManager>();
        pooledObject = GetComponent<PooledObject>();
        baseMaxHealth = maxHealth;
        currentHealth = maxHealth;

        if (gameObject.CompareTag(Tags.Player) && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHealth = currentHealth;
        }
        else if (gameObject.CompareTag(Tags.Enemy) && WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemiesLevelUp += ScaleHealth;
        }
    }

    private void ScaleHealth(float healthScalingPerLevel, float damageScalingPerLevel, int level)
    {
        maxHealth = baseMaxHealth * (1f + (level - 1) * healthScalingPerLevel);
        currentHealth = maxHealth;
    }

    public void LoseHealth(float amount)
    {
        if (IsDead) return;

        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;

        OnDamaged?.Invoke(amount);

        if (gameObject.CompareTag(Tags.Player) && GameManager.Instance != null)
            GameManager.Instance.PlayerHealth = currentHealth;

        if (currentHealth <= 0)
            Die();
    }

    public void GainHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        if (gameObject.CompareTag(Tags.Player) && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHealth = currentHealth;
        }
    }

    private void Die()
    {
        if (gameObject.CompareTag(Tags.Enemy) && WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemiesLevelUp -= ScaleHealth;
        }

        if (gameObject.CompareTag(Tags.Player))
            StartCoroutine(PlayerDeathSequence());
        else
            StartCoroutine(EnemyDeathSequence());
    }

    private IEnumerator PlayerDeathSequence()
    {
        IsDead = true;
        animator.SetBool("IsDying", IsDead);
        if (weaponManager != null) weaponManager.OnPlayerDeath();
        SoundManager.PlaySound(SoundType.DEATH);

        yield return new WaitForSeconds(1.5f);

        UIManager.Instance.ShowGameOver();
        GameManager.Instance.TriggerGameOver();
    }

    private static readonly WaitForSeconds deathDelay = new(1f);

    private IEnumerator EnemyDeathSequence()
    {
        IsDead = true;

        GetComponent<EnemyRewards>()?.GrantRewards(transform.position);

        var meshes = GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshes)
            mesh.enabled = false;

        yield return deathDelay;

        IsDead = false;
        currentHealth = maxHealth;
        foreach (var mesh in meshes)
            mesh.enabled = true;

        pooledObject ??= GetComponent<PooledObject>();
        if (pooledObject != null)
            pooledObject.Release();
        else
            gameObject.SetActive(false);
    }
}
