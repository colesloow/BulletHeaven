using UnityEngine;
using System;

public enum GameState { MainMenu, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public WeaponManager WeaponManager { get; set; }
    public GameState State { get; private set; } = GameState.MainMenu;

    [Header("References")]
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private GameObject player;

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
    private float playerSpawnY;
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

    public event Action<GameState> OnGameStateChanged;
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

    public int Level => level;
    public float TimeRemaining => timeRemaining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        playerSpawnY = player.transform.position.y;
        player.SetActive(false);
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

    public void StartGame()
    {
        WaveManager.Instance?.CleanupEnemies();

        for (int i = Collectable.Active.Count - 1; i >= 0; i--)
        {
            Collectable c = Collectable.Active[i];
            if (c.TryGetComponent(out PooledObject pooled)) pooled.Release();
            else Destroy(c.gameObject);
        }

        dungeonGenerator.Cleanup();
        dungeonGenerator.Generate();

        player.SetActive(true);
        player.transform.position = new Vector3(0f, playerSpawnY, 0f);
        if (player.TryGetComponent(out Rigidbody rb)) rb.linearVelocity = Vector3.zero;
        if (player.TryGetComponent(out Health health)) health.Revive();
        if (player.TryGetComponent(out WeaponManager wm)) wm.ResetWeapons();

        TotalScore = 0;
        PlayerHealth = 100f;
        PlayerXP = 0f;
        PlayerScrews = 0;
        level = 1;
        timeRemaining = gameDuration;
        secondsRemaining = -1;
        timerRunning = true;
        Resume();
        SetState(GameState.Playing);
    }

    public void RestartGame() => StartGame();

    public void ReturnToMainMenu()
    {
        WaveManager.Instance?.CleanupEnemies();
        dungeonGenerator.Cleanup();
        timerRunning = false;
        player.SetActive(false);
        Resume();
        SetState(GameState.MainMenu);
    }

    public void TriggerGameOver()
    {
        timerRunning = false;
        Pause();
        SetState(GameState.GameOver);
    }

    public void Pause() => Time.timeScale = 0f;
    public void Resume() => Time.timeScale = 1f;

    private void SetState(GameState newState)
    {
        State = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    private void LevelUp()
    {
        level++;
        OnLevelUp?.Invoke(level);
        SoundManager.PlaySound(SoundType.LEVELUP);
    }
}
