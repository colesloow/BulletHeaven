using UnityEngine;

[CreateAssetMenu(fileName = "UITheme", menuName = "BulletHeaven/UI Theme")]
public class UITheme : ScriptableObject
{
    [Header("Rarity Colors")]
    public Color commonColor = new Color(0.35f, 0.85f, 0.35f);
    public Color uncommonColor = new Color(0.25f, 0.55f, 1f);
    public Color rareColor = new Color(0.65f, 0.25f, 1f);

    [Header("States")]
    public Color activeTextColor = Color.white;
    public Color disabledColor = new Color(0.4f, 0.4f, 0.4f);

    public Color GetRarityColor(UpgradeRarity rarity) => rarity switch
    {
        UpgradeRarity.Common => commonColor,
        UpgradeRarity.Uncommon => uncommonColor,
        UpgradeRarity.Rare => rareColor,
        _ => activeTextColor,
    };
}
