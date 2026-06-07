using TMPro;
using UnityEngine;

public class GameOverStats : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI levelText;

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (state != GameState.GameOver) return;

        if (scoreText != null) scoreText.text = "Score: " + GameManager.Instance.TotalScore + " pts";
        if (killsText != null) killsText.text = "Kills: " + GameManager.Instance.EnemiesKilled;
        if (damageText != null) damageText.text = "Damage: " + GameManager.Instance.DamageTaken;
        if (levelText != null) levelText.text = "Level: " + GameManager.Instance.Level;
    }
}
