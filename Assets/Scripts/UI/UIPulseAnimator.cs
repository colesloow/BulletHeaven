using System.Collections;
using UnityEngine;

public class UIPulseAnimator : UIAnimator
{
    [SerializeField] private float scaleFactor = 1.1f;
    [SerializeField] private float halfPeriod = 0.4f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    protected override IEnumerator PlayRoutine()
    {
        Vector3 big = originalScale * scaleFactor;
        while (true)
        {
            yield return ScaleTo(big);
            yield return ScaleTo(originalScale);
        }
    }

    protected override IEnumerator ReverseRoutine() => ScaleTo(originalScale);

    private IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 from = transform.localScale;
        float elapsed = 0f;

        while (elapsed < halfPeriod)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / halfPeriod));
            transform.localScale = Vector3.LerpUnclamped(from, target, t);
            yield return null;
        }

        transform.localScale = target;
    }
}
