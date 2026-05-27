using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UISelect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private GameObject[] targets;

    public void OnSelect(BaseEventData _)
    {
        foreach (var t in targets) t.SetActive(true);
    }

    public void OnDeselect(BaseEventData _)
    {
        foreach (var t in targets) t.SetActive(false);
    }
}
