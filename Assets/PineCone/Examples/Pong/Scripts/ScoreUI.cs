using UnityEngine;
using TMPro;
using System;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI goalText;
    [SerializeField] private int playerIndex;

    private int score;

    private void OnEnable()
    {
        GameManager.OnGoal += GoalScored;
        GameManager.OnPlayerWon += PlayerWon;
    }

    private void OnDisable()
    {
        GameManager.OnGoal -= GoalScored;
        GameManager.OnPlayerWon -= PlayerWon;
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
