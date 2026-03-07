using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField]
    private float _value = 10f;
    [SerializeField]
    private bool _isXp = true;
    [SerializeField]
    private bool _isHealth = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_isXp) GameManager.Instance.PlayerXP += _value;
            if (_isHealth) GameManager.Instance.PlayerHealth += _value;

            // Play collect sound
            SoundManager.PlaySound(SoundType.COLLECT);

            Destroy(gameObject); // destroy collectable once collected
        }
    }
}
