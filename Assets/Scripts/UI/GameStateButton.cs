using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GameStateButton : MonoBehaviour
{
    public enum Action { StartGame, RestartGame, ReturnToMainMenu }

    [SerializeField] private Action action;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Execute);
    }

    private void Execute()
    {
        switch (action)
        {
            case Action.StartGame: GameManager.Instance.StartGame(); break;
            case Action.RestartGame: GameManager.Instance.RestartGame(); break;
            case Action.ReturnToMainMenu: GameManager.Instance.ReturnToMainMenu(); break;
        }
    }
}
