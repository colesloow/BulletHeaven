using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField]
    private string _gameSceneName;
    [SerializeField]
    private GameObject _mainMenuPanel;

    public void StartGame()
    {
        // load scene asynchronously 
        StartCoroutine(LoadGameScene());
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameScene());
    }

    private IEnumerator LoadGameScene()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Additive);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // disable ui
        if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);

        // set new scene as active once loaded
        Scene newScene = SceneManager.GetSceneByName(_gameSceneName);
        SceneManager.SetActiveScene(newScene);
    }

    private IEnumerator RestartGameScene()
    {
        // unload game scene
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(_gameSceneName);

        while (!unloadOperation.isDone)
        {
            yield return null;
        }

        // reload game scene
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Additive);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        // reset time scale after restart
        Time.timeScale = 1;

        // set the new scene as active once loaded
        Scene newScene = SceneManager.GetSceneByName(_gameSceneName);
        SceneManager.SetActiveScene(newScene);

        // call GameManager to reset game state
        GameManager.Instance.ResetGame();
    }
}

