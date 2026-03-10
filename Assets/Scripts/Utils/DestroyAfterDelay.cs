using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    [SerializeField]
    private float delay = 5f;

    void Update()
    {
        Destroy(gameObject, delay);
    }
}
