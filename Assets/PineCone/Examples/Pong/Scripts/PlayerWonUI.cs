using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerWonUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI winnerText;

    private void OnEnable()
    {
        GameManager.OnPlayerWon += PlayerWon;
        GameManager.OnGameReset += ResetText;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerWon -= PlayerWon;
        GameManager.OnGameReset -= ResetText;
    }

    private void PlayerWon(int player)
    {
        winnerText.text = $"Player {player + 1} Won!";
        Invoke(nameof(ResetText), 1.5f);
    }

    private void ResetText()
    {
        winnerText.text = "";
    }
}