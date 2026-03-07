using UnityEngine;
using UnityEngine.SceneManagement;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField]
    private Transform _player;

    private void Start()
    {
        SceneManager.activeSceneChanged += PopulateFields;
    }

    private void Update()
    {
        if (_player == null) return;
        transform.position = _player.transform.position + new Vector3(0, 6, -16);
    }

    private void PopulateFields(Scene current, Scene next)
    {
        // find references in scene when active scene changed
        _player = FindFirstObjectByType<CharacterController>().transform;
    }
}