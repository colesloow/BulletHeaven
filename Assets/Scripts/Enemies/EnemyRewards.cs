using UnityEngine;

public class Scor : MonoBehaviour
{
    [SerializeField]
    private int _objectScoring;
    [SerializeField]
    private float _objectXP;

    public void GrantRewards()
    {
        // add score & xp to game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TotalScore += _objectScoring;
            GameManager.Instance.PlayerXP += _objectXP;
        }
    }

}
