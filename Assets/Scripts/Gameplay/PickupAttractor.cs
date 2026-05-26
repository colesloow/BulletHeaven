using System;
using UnityEngine;

public class PickupAttractor : MonoBehaviour
{
    [SerializeField] private float attractRadius = 2f;
    [SerializeField] private float collectRadius = 0.4f;
    [SerializeField] private float attractSpeed = 5f;

    public event Action OnCollect;

    private Transform playerTransform;

    private void OnEnable()
    {
        GameObject player = GameObject.FindWithTag(Tags.Player);
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag(Tags.Player);
            if (player == null) return;
            playerTransform = player.transform;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > attractRadius) return;

        if (distance < collectRadius)
        {
            OnCollect?.Invoke();
            gameObject.SetActive(false);
            return;
        }

        float speed = Mathf.Min(attractSpeed * (attractRadius / distance), attractSpeed * 3f);
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);
    }
}
