using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private CanvasGroup hudPanel;
    [SerializeField] private CanvasGroup gameOverPanel;
    [SerializeField] private UpgradeUI upgradeUI;

    [Header("HUD Elements")]
    [SerializeField] private Image healthSlider;
    [SerializeField] private Image xpSlider;
    [SerializeField] private TextMeshProUGUI level;
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI screws;

    [Header("References")]
    [SerializeField] private SceneLoader sceneLoader;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetPanel(hudPanel, true);
        SetPanel(gameOverPanel, false);
    }

    private void Start()
    {
        InitializeUI();

        // subscribe to GameManager events
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnHealthChanged += UpdateHealth;
        GameManager.Instance.OnXPChanged += UpdateXP;
        GameManager.Instance.OnLevelUp += UpdateLevel;
        GameManager.Instance.OnTimerChanged += UpdateTimer;
        GameManager.Instance.OnScrewsChanged += UpdateScrews;
    }

    private void OnDestroy()
    {
        // unsubscribe from GameManager events
        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnHealthChanged -= UpdateHealth;
        GameManager.Instance.OnXPChanged -= UpdateXP;
        GameManager.Instance.OnLevelUp -= UpdateLevel;
        GameManager.Instance.OnTimerChanged -= UpdateTimer;
        GameManager.Instance.OnScrewsChanged -= UpdateScrews;
    }

    private void InitializeUI()
    {
        UpdateScore(GameManager.Instance.TotalScore);
        UpdateHealth(GameManager.Instance.PlayerHealth);
        UpdateXP(GameManager.Instance.PlayerXP);
        UpdateTimer(GameManager.Instance.SecondsRemaining);
        UpdateScrews(GameManager.Instance.PlayerScrews);
    }

    private void UpdateScore(int score)
    {
        this.score.text = score + " pts";
    }

    private void UpdateHealth(float health)
    {
        healthSlider.fillAmount = health / 100f; // fill between 0 & 1
    }

    private void UpdateXP(float xp)
    {
        xpSlider.fillAmount = xp / 100f;
    }

    private void UpdateLevel(int level)
    {
        this.level.text = "Level " + level;
    }

    private void UpdateTimer(int seconds)
    {
        timer.text = $"{seconds / 60:D2}:{seconds % 60:D2}";
    }

    private void UpdateScrews(int count)
    {
        if (screws != null) screws.text = count.ToString();
    }

    public void ShowUpgradePanel(System.Collections.Generic.List<WeaponUpgrade> choices, System.Action<WeaponUpgrade> onPicked)
    {
        upgradeUI.Show(choices, onPicked);
    }

    public void ShowGameOver()
    {
        SetPanel(gameOverPanel, true);
    }

    public void ResetUI()
    {
        UpdateScore(0);
        UpdateHealth(100f);
        UpdateXP(0f);
        UpdateScrews(0);
        UpdateLevel(1);
        UpdateTimer(GameManager.Instance.SecondsRemaining);
        SetPanel(gameOverPanel, false);
    }

    private void SetPanel(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
