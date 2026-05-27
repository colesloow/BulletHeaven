using System.Collections;
using UnityEngine;

public class UIScaleAnimator : UIAnimator
{
    [SerializeField] private float scaleFactor = 1.08f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    protected override IEnumerator PlayRoutine() => ScaleTo(originalScale * scaleFactor);
    protected override IEnumerator ReverseRoutine() => ScaleTo(originalScale);

    private IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 from = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            transform.localScale = Vector3.LerpUnclamped(from, target, t);
            yield return null;
        }

        transform.localScale = target;
    }
}
