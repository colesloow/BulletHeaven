using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public static readonly List<Health> ActiveEnemies = new();

    [SerializeField]
    private float _maxHealth = 100f;
    [SerializeField]
    private float _currentHealth;
    [SerializeField]
    private int _level;

    private Animator _animator;
    private WeaponManager _weaponManager;
    private PooledObject _pooledObject;

    public bool IsDead = false;

    private void OnEnable()
    {
        if (CompareTag("Enemy")) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _weaponManager = GetComponent<WeaponManager>();
        _pooledObject = GetComponent<PooledObject>();
        _currentHealth = _maxHealth;

        // if game object is the player, synchronize health
        if (gameObject.CompareTag("Player") && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHealth = _currentHealth;
        }
        else if (gameObject.CompareTag("Enemy") && WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemiesLevelUp += ScaleHealth;
        }
    }

    private void ScaleHealth(float healthMultiplier, float damageMultiplier)
    {
        if (gameObject.CompareTag("Enemy"))
        {
            _maxHealth *= healthMultiplier;
            _currentHealth = _maxHealth; // reset life to max
        }
    }

    public void LoseHealth(float amount)
    {
        // Debug.Log($"{gameObject.name} took {amount} damage. Current Health: {_currentHealth}");

        _currentHealth -= amount;
        if (_currentHealth < 0)
        {
            _currentHealth = 0;
        }

        if (gameObject.CompareTag("Player") && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHealth = _currentHealth;
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void GainHealth(float amount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0, _maxHealth); // clamp to max health
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }

        if (gameObject.CompareTag("Player") && GameManager.Instance != null)
        {
            GameManager.Instance.PlayerHealth = _currentHealth;
        }
    }

    private void Die()
    {
        if (gameObject.CompareTag("Enemy") && WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemiesLevelUp -= ScaleHealth;
        }

        if (gameObject.CompareTag("Player"))
        {
            // if player is dead
            StartCoroutine(PlayerDeathSequence());
        }
        else
        {
            // if ememy is dead,
            StartCoroutine(EnemyDeathSequence());
        }
    }

    private IEnumerator PlayerDeathSequence()
    {
        IsDead = true;
        _animator.SetBool("IsDying", IsDead);
        if (_weaponManager != null) _weaponManager.OnPlayerDeath();
        SoundManager.PlaySound(SoundType.DEATH); // play death sound

        // CharacterController playerController = GetComponent<CharacterController>();
        // if (playerController != null)
        // {
        //     playerController.DisableMovement();
        // }

        yield return new WaitForSeconds(1.5f);

        // show game over
        UIManager.Instance.ShowGameOver();
        GameManager.Instance.TriggerGameOver();
    }

    private static readonly WaitForSeconds _deathDelay = new(1f);

    private IEnumerator EnemyDeathSequence()
    {
        IsDead = true;

        var meshes = GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshes)
            mesh.enabled = false;

        yield return _deathDelay;

        IsDead = false;
        _currentHealth = _maxHealth;
        foreach (var mesh in meshes)
            mesh.enabled = true;

        if (_pooledObject != null) _pooledObject.Release();
    }
}
