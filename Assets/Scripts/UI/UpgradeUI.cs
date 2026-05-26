using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup _panel;
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private Transform _buttonContainer;
    [SerializeField] private TextMeshProUGUI _emptyLabel;

    private readonly List<Button> _spawnedButtons = new();
    private readonly WaitForSeconds _autoDismissDelay = new(2f);

    private void Awake()
    {
        HidePanel();
        _emptyLabel.gameObject.SetActive(false);
    }

    private void ShowPanel()
    {
        _panel.alpha = 1f;
        _panel.interactable = true;
        _panel.blocksRaycasts = true;
    }

    public void Hide() => HidePanel();

    private void HidePanel()
    {
        _panel.alpha = 0f;
        _panel.interactable = false;
        _panel.blocksRaycasts = false;
    }
    private Action<WeaponUpgrade> _onPicked;

    public void Show(List<WeaponUpgrade> choices, Action<WeaponUpgrade> onPicked)
    {
        _onPicked = onPicked;

        foreach (var btn in _spawnedButtons)
            Destroy(btn.gameObject);
        _spawnedButtons.Clear();

        bool hasChoices = choices.Count > 0;
        _emptyLabel.gameObject.SetActive(!hasChoices);

        foreach (WeaponUpgrade upgrade in choices)
        {
            Button btn = Instantiate(_buttonPrefab, _buttonContainer);
            _spawnedButtons.Add(btn);

            string costLabel = upgrade.ScrapCost > 0 ? $"{upgrade.ScrapCost} screws" : "Free";
            btn.GetComponentInChildren<TextMeshProUGUI>().text =
                $"[{upgrade.Rarity}]  {upgrade.UpgradeName}\n{costLabel}";

            bool canAfford = upgrade.ScrapCost == 0 ||
                             (GameManager.Instance != null && GameManager.Instance.PlayerScrews >= upgrade.ScrapCost);
            btn.interactable = canAfford;

            WeaponUpgrade captured = upgrade;
            btn.onClick.AddListener(() => Pick(captured));
        }

        if (!hasChoices)
        {
            _emptyLabel.text = "No upgrade available";
            // Auto-dismiss after a short delay since there is nothing to pick
            StartCoroutine(AutoDismiss());
            return;
        }

        ShowPanel();
        GameManager.Instance?.Pause();
    }

    private System.Collections.IEnumerator AutoDismiss()
    {
        ShowPanel();
        yield return _autoDismissDelay;
        HidePanel();
    }

    private void Pick(WeaponUpgrade upgrade)
    {
        HidePanel();
        GameManager.Instance?.Resume();
        if (upgrade.ScrapCost > 0 && GameManager.Instance != null)
            GameManager.Instance.PlayerScrews -= upgrade.ScrapCost;
        _onPicked?.Invoke(upgrade);
    }
}
