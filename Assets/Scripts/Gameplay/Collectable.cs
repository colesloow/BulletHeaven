using UnityEngine;

public enum CollectableType { XP, Health, Screw }

public class Collectable : MonoBehaviour
{
    [SerializeField] private CollectableType type;
    [SerializeField] private float value = 10f;

    private PooledObject pooledObject;
    private Health playerHealth;

    private void Start()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        GameObject player = GameObject.FindWithTag(Tags.Player);
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(Tags.Player)) return;
        Collect();
    }

    private void Collect()
    {
        switch (type)
        {
            case CollectableType.XP:
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerXP += value;
                break;

            case CollectableType.Health:
                if (playerHealth != null) playerHealth.GainHealth(value);
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
