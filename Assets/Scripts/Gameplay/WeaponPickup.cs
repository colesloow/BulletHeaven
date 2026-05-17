using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData _weaponData;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance?.WeaponManager == null) return;

        GameManager.Instance.WeaponManager.AddWeapon(_weaponData);
        gameObject.SetActive(false);
    }
}
