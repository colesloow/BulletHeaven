using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public WeaponData Data { get; private set; }

    public void Initialize(WeaponData data)
    {
        Data = data;
        OnInitialize();
    }

    protected abstract void OnInitialize();
    public abstract void ApplyUpgrade(WeaponUpgrade upgrade);
    public virtual bool IsUpgradeAvailable(WeaponUpgrade upgrade) => true;
    public virtual void OnPlayerDeath() { }
}
