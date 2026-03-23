using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Health & Score")]
    [SerializeField]
    private int _totalScore;
    [SerializeField]
    private float _playerHealth;

    [Header("Levels & XP")]
    [SerializeField]
    private float _playerXP;
    [SerializeField]
    private int _level = 1;
    [SerializeField]
    private float _xpThreshold = 100f;

    [Header("Enemies")]
    [SerializeField]
    private float _enemyHealthMultiplier = 1.5f;
    [SerializeField]
    private float _enemyDamageMultiplier = 1.2f;
    [SerializeField]
    private int _maxEnemies;
    public int MaxEnemies { get => _maxEnemies; }

    public event Action<int> OnScoreChanged;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnXPChanged;
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnEnemiesLevelUp;

    public int TotalScore
    {
        get => _totalScore;
        set
        {
            _totalScore = value;
            OnScoreChanged?.Invoke(_totalScore);
        }
    }

    public float PlayerHealth
    {
        get => _playerHealth;
        set
        {
            _playerHealth = Mathf.Clamp(value, 0, 100);
            OnHealthChanged?.Invoke(_playerHealth);
        }
    }

    public float PlayerXP
    {
        get => _playerXP;
        set
        {
            _playerXP = Mathf.Clamp(value, 0, _xpThreshold);

            if (_playerXP >= _xpThreshold)
            {
                _playerXP = 0;
                LevelUp();
            }

            OnXPChanged?.Invoke(_playerXP);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerGameOver()
    {
        Time.timeScale = 0;
        UIManager.Instance.ShowGameOver();
    }

    public void ResetGame()
    {
        TotalScore = 0;
        PlayerHealth = 100f;
        PlayerXP = 0f;
        _level = 1;

        UIManager.Instance.ResetUI();
    }

    private void ScaleEnemiesWithLevel()
    {
        OnEnemiesLevelUp?.Invoke(_enemyHealthMultiplier, _enemyDamageMultiplier);
    }

    private void LevelUp()
    {
        _level++;
        OnLevelUp?.Invoke(_level);
        SoundManager.PlaySound(SoundType.LEVELUP);
        ScaleEnemiesWithLevel();
    }
}
