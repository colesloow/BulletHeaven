using UnityEngine;

public class CollectableAnimator : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    private float originY;
    private float timeOffset;
    private bool initialized;

    private void OnEnable()
    {
        initialized = false;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (!initialized)
        {
            originY = transform.position.y;
            initialized = true;
        }

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);

        Vector3 pos = transform.position;
        pos.y = originY + Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobHeight;
        transform.position = pos;
    }
}
