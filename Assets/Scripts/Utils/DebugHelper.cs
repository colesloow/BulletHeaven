using UnityEngine;
using UnityEngine.InputSystem;

// Debug-only helper. Remove before shipping.
public class DebugHelper : MonoBehaviour
{
    [SerializeField] private float xpPerPress = 50f;
    [SerializeField] private WeaponManager weaponManager;

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            GameManager.Instance.PlayerXP += xpPerPress;

        if (weaponManager == null) return;
        var satelliteWeapon = weaponManager.SatelliteWeapon;
        if (satelliteWeapon == null) return;

        var kb = Keyboard.current;

        if (kb.digit1Key.wasPressedThisFrame) satelliteWeapon.DebugSetSatelliteCount(1);
        if (kb.digit2Key.wasPressedThisFrame) satelliteWeapon.DebugSetSatelliteCount(2);
        if (kb.digit3Key.wasPressedThisFrame) satelliteWeapon.DebugSetSatelliteCount(3);
        if (kb.digit4Key.wasPressedThisFrame) satelliteWeapon.DebugSetSatelliteCount(4);
        if (kb.digit5Key.wasPressedThisFrame) satelliteWeapon.DebugSetSatelliteCount(5);

        if (kb.lKey.wasPressedThisFrame) satelliteWeapon.DebugUnlockLaser();

        if (kb.numpadPlusKey.wasPressedThisFrame || kb.equalsKey.wasPressedThisFrame)
            satelliteWeapon.DebugAddLaser(1);
        if (kb.minusKey.wasPressedThisFrame)
            satelliteWeapon.DebugAddLaser(-1);
    }
}
