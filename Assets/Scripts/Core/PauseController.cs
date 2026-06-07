using UnityEngine;

public class PauseController : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameState.Playing)
            GameManager.Instance.PauseGame();
        else if (GameManager.Instance.State == GameState.Paused)
            GameManager.Instance.ResumeGame();
    }
}
