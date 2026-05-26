using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private UpgradeUI upgradeUI;

    [Header("HUD Elements")]
    [SerializeField] private Image healthSlider;
    [SerializeField] private Image xpSlider;
    [SerializeField] private TextMeshProUGUI level;
    [SerializeField] private TextMeshProUGUI score;
    [SerializeField] private TextMeshProUGUI timer;
    [SerializeField] private TextMeshProUGUI screws;

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
        GameManager.Instance.OnGameStateChanged += OnStateChanged;
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnHealthChanged += UpdateHealth;
        GameManager.Instance.OnXPChanged += UpdateXP;
        GameManager.Instance.OnLevelUp += UpdateLevel;
        GameManager.Instance.OnTimerChanged += UpdateTimer;
        GameManager.Instance.OnScrewsChanged += UpdateScrews;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnHealthChanged -= UpdateHealth;
        GameManager.Instance.OnXPChanged -= UpdateXP;
        GameManager.Instance.OnLevelUp -= UpdateLevel;
        GameManager.Instance.OnTimerChanged -= UpdateTimer;
        GameManager.Instance.OnScrewsChanged -= UpdateScrews;
    }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.Playing)
            RefreshHUD();
    }

    private void RefreshHUD()
    {
        upgradeUI.Hide();
        UpdateScore(GameManager.Instance.TotalScore);
        UpdateHealth(GameManager.Instance.PlayerHealth);
        UpdateXP(GameManager.Instance.PlayerXP);
        UpdateLevel(GameManager.Instance.Level);
        UpdateTimer(GameManager.Instance.SecondsRemaining);
        UpdateScrews(GameManager.Instance.PlayerScrews);
    }

    public void ShowUpgradePanel(System.Collections.Generic.List<WeaponUpgrade> choices, System.Action<WeaponUpgrade> onPicked)
    {
        upgradeUI.Show(choices, onPicked);
    }

    private void UpdateScore(int value) => score.text = value + " pts";
    private void UpdateHealth(float value) => healthSlider.fillAmount = value / 100f;
    private void UpdateXP(float value) => xpSlider.fillAmount = value / 100f;
    private void UpdateLevel(int value) => level.text = "Level " + value;
    private void UpdateTimer(int seconds) => timer.text = $"{seconds / 60:D2}:{seconds % 60:D2}";
    private void UpdateScrews(int value) { if (screws != null) screws.text = value.ToString(); }
}
