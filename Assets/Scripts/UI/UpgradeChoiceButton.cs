using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeChoiceButton : MonoBehaviour
{
    [Header("Themes")]
    [SerializeField] private ButtonTheme commonTheme;
    [SerializeField] private ButtonTheme uncommonTheme;
    [SerializeField] private ButtonTheme rareTheme;
    [SerializeField] private ButtonTheme disabledTheme;

    [Header("References")]
    [SerializeField] private Image border;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;

    public void Apply(WeaponUpgrade upgrade, bool canAfford)
    {
        rarityText.text = $"[{upgrade.Rarity}]";
        nameText.text = upgrade.UpgradeName;
        costText.text = upgrade.ScrapCost > 0 ? $"{upgrade.ScrapCost} screws" : "Free";

        ButtonTheme theme = canAfford ? GetRarityTheme(upgrade.Rarity) : disabledTheme;
        Color bodyColor = canAfford ? Color.white : theme.accentColor;

        border.color = theme.accentColor;
        rarityText.color = theme.accentColor;
        nameText.color = bodyColor;
        costText.color = bodyColor;
    }

    private ButtonTheme GetRarityTheme(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => commonTheme,
        UpgradeRarity.Uncommon => uncommonTheme,
        UpgradeRarity.Rare => rareTheme,
        _ => commonTheme,
    };
}
