using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponData _startingWeapon;
    [SerializeField] private int _upgradeChoices = 2;

    public void AddUpgradeChoice() => _upgradeChoices++;

    private readonly List<Weapon> _activeWeapons = new();

    private void Start()
    {
        GameManager.Instance.OnLevelUp += OnLevelUp;

        if (_startingWeapon != null)
            AddWeapon(_startingWeapon);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= OnLevelUp;
    }

    public void AddWeapon(WeaponData data)
    {
        if (data.Prerequisite != null && !HasWeapon(data.Prerequisite))
        {
            Debug.LogWarning($"Cannot add {data.WeaponName}: missing prerequisite {data.Prerequisite.WeaponName}.");
            return;
        }

        if (data.Prefab == null) return;

        GameObject go = Instantiate(data.Prefab, transform);
        Weapon weapon = go.GetComponent<Weapon>();
        if (weapon == null) return;

        weapon.Initialize(data);
        _activeWeapons.Add(weapon);
    }

    public void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        Weapon target = _activeWeapons.FirstOrDefault(w => w.Data == upgrade.TargetWeapon);
        target?.ApplyUpgrade(upgrade);
    }

    public bool HasWeapon(WeaponData data) => _activeWeapons.Any(w => w.Data == data);

    public void OnPlayerDeath()
    {
        foreach (var weapon in _activeWeapons)
            weapon.OnPlayerDeath();
    }

    // Returns a weighted random selection of available upgrades across all active weapons.
    // Common = weight 10, Uncommon = weight 4, Rare = weight 1.
    public List<WeaponUpgrade> GetUpgradeChoices()
    {
        var pool = _activeWeapons
            .Where(w => w.Data.AvailableUpgrades != null)
            .SelectMany(w => w.Data.AvailableUpgrades)
            .ToList();

        // Build weighted list: each entry appears N times proportional to its weight
        var weighted = new List<WeaponUpgrade>();
        foreach (var upgrade in pool)
        {
            int weight = upgrade.Rarity switch
            {
                UpgradeRarity.Common   => 10,
                UpgradeRarity.Uncommon => 4,
                UpgradeRarity.Rare     => 1,
                _                      => 1,
            };
            for (int i = 0; i < weight; i++)
                weighted.Add(upgrade);
        }

        var choices = new List<WeaponUpgrade>();
        var seen = new System.Collections.Generic.HashSet<WeaponUpgrade>();

        var shuffled = weighted.OrderBy(_ => Random.value).ToList();
        foreach (var upgrade in shuffled)
        {
            if (seen.Add(upgrade))
                choices.Add(upgrade);
            if (choices.Count >= _upgradeChoices)
                break;
        }

        return choices;
    }

    private void OnLevelUp(int level)
    {
        var choices = GetUpgradeChoices();
        if (choices.Count == 0) return;

        UIManager.Instance.ShowUpgradePanel(choices, ApplyUpgrade);
    }
}
