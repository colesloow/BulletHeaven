using UnityEngine;
using UnityEngine.InputSystem;

// Debug-only helper. Remove before shipping.
public class DebugHelper : MonoBehaviour
{
    [SerializeField] private float _xpPerPress = 50f;

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
            GameManager.Instance.PlayerXP += _xpPerPress;
    }
}
