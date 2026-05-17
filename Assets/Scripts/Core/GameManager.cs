using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public WeaponManager WeaponManager { get; set; }

    [Header("Health & Score")]
    [SerializeField] private int totalScore;
    [SerializeField] private float playerHealth;

    [Header("Levels & XP")]
    [SerializeField] private float playerXP;
    [SerializeField] private int level = 1;
    [SerializeField] private float xpThreshold = 100f;

    [Header("Currency")]
    [SerializeField] private int playerScrews;

    [Header("Timer")]
    [SerializeField] private float gameDuration = 600f;
    private float timeRemaining;
    private bool timerRunning;
    private int secondsRemaining;

    public int SecondsRemaining
    {
        get => secondsRemaining;
        private set
        {
            if (value == secondsRemaining) return;
            secondsRemaining = value;
            OnTimerChanged?.Invoke(secondsRemaining);
        }
    }

    public event Action<int> OnScoreChanged;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnXPChanged;
    public event Action<int> OnLevelUp;
    public event Action<int> OnTimerChanged;
    public event Action<int> OnScrewsChanged;

    public int TotalScore
    {
        get => totalScore;
        set
        {
            totalScore = value;
            OnScoreChanged?.Invoke(totalScore);
        }
    }

    public float PlayerHealth
    {
        get => playerHealth;
        set
        {
            playerHealth = Mathf.Clamp(value, 0, 100);
            OnHealthChanged?.Invoke(playerHealth);
        }
    }

    public float PlayerXP
    {
        get => playerXP;
        set
        {
            playerXP = Mathf.Clamp(value, 0, xpThreshold);

            if (playerXP >= xpThreshold)
            {
                playerXP = 0;
                LevelUp();
            }

            OnXPChanged?.Invoke(playerXP);
        }
    }

    public int PlayerScrews
    {
        get => playerScrews;
        set
        {
            playerScrews = Mathf.Max(0, value);
            OnScrewsChanged?.Invoke(playerScrews);
        }
    }

    public float TimeRemaining => timeRemaining;

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

    private void Start()
    {
        StartTimer();
    }

    private void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        SecondsRemaining = Mathf.CeilToInt(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            timerRunning = false;
            TriggerGameOver();
        }
    }

    private void StartTimer()
    {
        timeRemaining = gameDuration;
        timerRunning = true;
        secondsRemaining = -1;
    }

    public void TriggerGameOver()
    {
        timerRunning = false;
        Time.timeScale = 0;
        UIManager.Instance.ShowGameOver();
    }

    public void ResetGame()
    {
        TotalScore = 0;
        PlayerHealth = 100f;
        PlayerXP = 0f;
        PlayerScrews = 0;
        level = 1;
        StartTimer();

        UIManager.Instance.ResetUI();
    }

    private void LevelUp()
    {
        level++;
        OnLevelUp?.Invoke(level);
        SoundManager.PlaySound(SoundType.LEVELUP);
    }
}
