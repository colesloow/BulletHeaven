using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    [SerializeField]
    private float delay = 5f;

    void Start()
    {
        Destroy(gameObject, delay);
    }
}
