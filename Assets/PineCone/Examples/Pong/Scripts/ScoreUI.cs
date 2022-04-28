using UnityEngine;
using TMPro;
using System.ComponentModel;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private GameLogic gameLogic;
    [SerializeField] TextMeshProUGUI goalText;
    [SerializeField] private int playerIndex;

    private int score;

    private void OnEnable()
    {
        gameLogic.OnSyncVarValueChanged += ScoreValueChanged;
        PongNetworkManager.OnPlayerWon += PlayerWon;
    }

    private void OnDisable()
    {
        gameLogic.OnSyncVarValueChanged -= ScoreValueChanged;
        PongNetworkManager.OnPlayerWon -= PlayerWon;
    }

    private void ScoreValueChanged(object sender, PropertyChangedEventArgs e)
    {
        GoalScored(e.PropertyName == "Player1Score" ? 0 : 1);
    }

    private void PlayerWon(int player)
    {
        goalText.text = "0";
        score = 0;
    }

    private void GoalScored(int goal)
    {
        if (goal != playerIndex)
            return;

        score++;
        goalText.text = score.ToString();
    }
}
