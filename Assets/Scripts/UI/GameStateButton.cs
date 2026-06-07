using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GameStateButton : MonoBehaviour
{
    public enum Action { StartGame, RestartGame, ReturnToMainMenu, ResumeGame }

    [SerializeField] private Action action;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private Image loadingBar;
    // Fake loading stages: purely cosmetic, not tied to actual generation progress.
    // Generation is already done before this animation starts; the bar just adds perceived feedback.
    // (target fill, duration in seconds)
    private static readonly (float target, float duration)[] barStages =
    {
        (0.25f, 0.35f),
        (0.55f, 0.70f),
        (0.72f, 0.90f),
        (0.90f, 0.50f),
        (1.00f, 0.45f),
    };

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Execute);
        if (loadingOverlay != null) loadingOverlay.SetActive(false);
        if (loadingBar != null) loadingBar.fillAmount = 0f;
    }

    private void Execute()
    {
        if (loadingOverlay != null && NeedsLoading())
            StartCoroutine(ExecuteWithLoading());
        else
            Run();
    }

    private IEnumerator ExecuteWithLoading()
    {
        GetComponent<Button>().interactable = false;
        loadingBar.fillAmount = 0f;
        loadingOverlay.SetActive(true);

        // One frame so the overlay renders before the generation freeze.
        yield return null;

        GameManager.Instance.PrepareGame();

        // Animate bar through stages after generation to simulate loading steps.
        // SmoothStep gives each stage an ease-in/out feel.
        // realtimeSinceStartup avoids the large unscaledDeltaTime spike right after the freeze.
        float from = 0f;
        foreach ((float target, float duration) in barStages)
        {
            float stageStart = Time.realtimeSinceStartup;
            while (true)
            {
                float t = (Time.realtimeSinceStartup - stageStart) / duration;
                if (t >= 1f) break;
                loadingBar.fillAmount = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            loadingBar.fillAmount = target;
            from = target;
        }

        GameManager.Instance.FinalizeStartGame();
        // One frame for Cinemachine to process the repositioned player before revealing the scene.
        yield return null;
        loadingOverlay.SetActive(false);
        GetComponent<Button>().interactable = true;
    }

    private bool NeedsLoading() =>
        action == Action.StartGame || action == Action.RestartGame;

    private void Run()
    {
        switch (action)
        {
            case Action.StartGame: GameManager.Instance.StartGame(); break;
            case Action.RestartGame: GameManager.Instance.RestartGame(); break;
            case Action.ReturnToMainMenu: GameManager.Instance.ReturnToMainMenu(); break;
            case Action.ResumeGame: GameManager.Instance.ResumeGame(); break;
        }
    }
}
