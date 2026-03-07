using UnityEngine;

public class Scoring : MonoBehaviour
{
    [SerializeField]
    private int _objectScoring;
    [SerializeField]
    private GameObject _xpCollectablePrefab;
    [SerializeField]
    private GameObject _healthCollectablePrefab;
    [SerializeField]
    [Range(0f, 100f)]
    private float _healthDropChance = 20f; 

    public void GrantRewards()
    {
        GameManager.Instance.TotalScore += _objectScoring;

        if (Random.value * 100f <= _healthDropChance)
        {
            Instantiate(_healthCollectablePrefab, transform.position, Quaternion.identity);
        }
        else 
        {
            Instantiate(_xpCollectablePrefab, transform.position, Quaternion.identity);
        }
    }
}
