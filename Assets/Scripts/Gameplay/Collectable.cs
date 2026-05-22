using UnityEngine;

public enum CollectableType { XP, Health, Screw }

public class Collectable : MonoBehaviour
{
    [SerializeField] private CollectableType type;
    [SerializeField] private float value = 10f;
    [SerializeField] private float attractRadius = 2.5f;
    [SerializeField] private float collectRadius = 0.5f;
    [SerializeField] private float attractSpeed = 8f;

    private PooledObject pooledObject;
    private Transform playerTransform;
    private Health playerHealth;

    private void Start()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;
        playerTransform = player.transform;
        playerHealth = player.GetComponent<Health>();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance > attractRadius) return;

        if (distance < collectRadius)
        {
            Collect();
            return;
        }

        float speed = Mathf.Min(attractSpeed * (attractRadius / distance), attractSpeed * 3f);
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);
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
