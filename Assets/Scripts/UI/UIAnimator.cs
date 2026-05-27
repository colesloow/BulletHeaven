using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public abstract class UIAnimator : MonoBehaviour
{
    private Coroutine currentAnim;

    private void OnEnable() => Play();
    private void OnDisable() => StopCurrent();

    public void Play()
    {
        StopCurrent();
        currentAnim = StartCoroutine(PlayRoutine());
    }

    public void PlayReverse()
    {
        StopCurrent();
        currentAnim = StartCoroutine(ReverseRoutine());
    }

    protected abstract IEnumerator PlayRoutine();
    protected abstract IEnumerator ReverseRoutine();

    protected void StopCurrent()
    {
        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            currentAnim = null;
        }
    }
}
