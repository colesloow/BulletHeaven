using UnityEngine;

public enum CollectableType { XP, Health, Scrap }

public class Collectable : MonoBehaviour
{
    [SerializeField] private CollectableType _type;
    [SerializeField] private float _value = 10f;

    private PooledObject _pooledObject;

    private void Start()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (_type)
        {
            case CollectableType.XP:
                if (GameManager.Instance != null)
                    GameManager.Instance.PlayerXP += _value;
                break;

            case CollectableType.Health:
                other.GetComponent<Health>()?.GainHealth(_value);
                break;

            case CollectableType.Scrap:
                // TODO: add scrap currency to GameManager
                break;
        }

        SoundManager.PlaySound(SoundType.COLLECT);

        if (_pooledObject != null) _pooledObject.Release();
        else Destroy(gameObject);
    }
}
