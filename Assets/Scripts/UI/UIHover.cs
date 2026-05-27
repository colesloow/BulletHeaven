using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject[] targets;

    public void OnPointerEnter(PointerEventData _)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
        foreach (var t in targets) t.SetActive(true);
    }

    public void OnPointerExit(PointerEventData _)
    {
        // If this object is still selected (mouse moved to empty area), leave it selected
        // and visible — UISelect will hide it when keyboard navigates away.
        if (EventSystem.current.currentSelectedGameObject == gameObject)
            return;

        foreach (var t in targets) t.SetActive(false);
    }
}
