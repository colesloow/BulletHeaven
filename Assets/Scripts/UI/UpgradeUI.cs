using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private Transform _buttonContainer;

    private readonly List<Button> _spawnedButtons = new();
    private Action<WeaponUpgrade> _onPicked;

    public void Show(List<WeaponUpgrade> choices, Action<WeaponUpgrade> onPicked)
    {
        _onPicked = onPicked;

        foreach (var btn in _spawnedButtons)
            Destroy(btn.gameObject);
        _spawnedButtons.Clear();

        foreach (WeaponUpgrade upgrade in choices)
        {
            Button btn = Instantiate(_buttonPrefab, _buttonContainer);
            _spawnedButtons.Add(btn);

            btn.GetComponentInChildren<TextMeshProUGUI>().text =
                $"[{upgrade.Rarity}]  {upgrade.UpgradeName}";

            WeaponUpgrade captured = upgrade;
            btn.onClick.AddListener(() => Pick(captured));
        }

        _panel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Pick(WeaponUpgrade upgrade)
    {
        _panel.SetActive(false);
        Time.timeScale = 1f;
        _onPicked?.Invoke(upgrade);
    }
}
