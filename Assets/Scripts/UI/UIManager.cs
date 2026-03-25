using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private CanvasGroup _hudPanel;
    [SerializeField] private CanvasGroup _gameOverPanel;
    [SerializeField] private UpgradeUI _upgradeUI;

    [Header("HUD Elements")]
    [SerializeField] private Image _healthSlider;
    [SerializeField] private Image _xpSlider;
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private TextMeshProUGUI _score;

    [Header("References")]
    [SerializeField] private SceneLoader _sceneLoader;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetPanel(_hudPanel, true);
        SetPanel(_gameOverPanel, false);
    }

    private void Start()
    {
        InitializeUI();

        // subscribe to GameManager events
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnHealthChanged += UpdateHealth;
        GameManager.Instance.OnXPChanged += UpdateXP;
        GameManager.Instance.OnLevelUp += UpdateLevel;
    }

    private void OnDestroy()
    {
        // unsubscribe from GameManager events
        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnHealthChanged -= UpdateHealth;
        GameManager.Instance.OnXPChanged -= UpdateXP;
        GameManager.Instance.OnLevelUp -= UpdateLevel;
    }

    private void InitializeUI()
    {
        UpdateScore(GameManager.Instance.TotalScore);
        UpdateHealth(GameManager.Instance.PlayerHealth);
        UpdateXP(GameManager.Instance.PlayerXP);
    }

    private void UpdateScore(int score)
    {
        _score.text = score + " pts";
    }

    private void UpdateHealth(float health)
    {
        _healthSlider.fillAmount = health / 100f; // fill between 0 & 1
    }

    private void UpdateXP(float xp)
    {
        _xpSlider.fillAmount = xp / 100f;
    }

    private void UpdateLevel(int level)
    {
        _level.text = "Level " + level;
    }

    public void ShowUpgradePanel(System.Collections.Generic.List<WeaponUpgrade> choices, System.Action<WeaponUpgrade> onPicked)
    {
        _upgradeUI.Show(choices, onPicked);
    }

    public void ShowGameOver()
    {
        SetPanel(_gameOverPanel, true);
    }

    public void ResetUI()
    {
        UpdateScore(0);
        UpdateHealth(100f);
        UpdateXP(0f);
        UpdateLevel(1);
        SetPanel(_gameOverPanel, false);
    }

    private void SetPanel(CanvasGroup panel, bool visible)
    {
        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
