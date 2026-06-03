using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [SerializeField] private GameState[] visibleInStates;
    // If unassigned, falls back to the first Selectable child.
    [SerializeField] private Selectable defaultSelected;

    private CanvasGroup canvasGroup;

    private void Awake() => canvasGroup = GetComponent<CanvasGroup>();

    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += OnStateChanged;
        OnStateChanged(GameManager.Instance.State);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        bool visible = System.Array.IndexOf(visibleInStates, state) >= 0;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        if (visible)
            StartCoroutine(SelectDefault());
    }

    private IEnumerator SelectDefault()
    {
        yield return null;
        Selectable target = defaultSelected != null ? defaultSelected : GetComponentInChildren<Selectable>();
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}
