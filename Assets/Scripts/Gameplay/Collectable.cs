using System.Collections.Generic;
using UnityEngine;

public enum CollectableType { XP, Health, Screw }

public class Collectable : MonoBehaviour
{
    public static readonly List<Collectable> Active = new();

    [SerializeField] private CollectableType type;
    [SerializeField] private float value = 10f;

    private PickupAttractor attractor;
    private PooledObject pooledObject;
    private bool collected;

    private void Start()
    {
        pooledObject = GetComponent<PooledObject>();
        attractor = GetComponent<PickupAttractor>();
        if (attractor != null)
            attractor.OnCollect += CollectByProximity;
    }

    private void OnEnable()
    {
        Active.Add(this);
        collected = false;
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Tags.Player)) return;
        Collect(other.GetComponent<Health>());
    }

    private void CollectByProximity()
    {
        Health playerHealth = GameObject.FindWithTag(Tags.Player)?.GetComponent<Health>();
        Collect(playerHealth);
    }

    private void Collect(Health playerHealth)
    {
        if (collected) return;
        collected = true;

        switch (type)
        {
            case CollectableType.XP:
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerXP += value;
                break;

            case CollectableType.Health:
                playerHealth?.GainHealth(value);
                break;

            case CollectableType.Screw:
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerScrews += Mathf.RoundToInt(value);
                break;
        }

        SoundManager.PlaySound(SoundType.COLLECT);

        if (pooledObject != null) pooledObject.Release();
        else Destroy(gameObject);
    }
}
