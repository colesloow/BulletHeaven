using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;

    private void Start()
    {
        var attractor = GetComponent<PickupAttractor>();
        if (attractor != null)
            attractor.OnCollect += Collect;
    }

    private void Collect()
    {
        if (GameManager.Instance?.WeaponManager == null) return;
        GameManager.Instance.WeaponManager.AddWeapon(weaponData);
    }
}
