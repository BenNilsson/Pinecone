using UnityEngine;
using TMPro;

public class PlayerWonUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI winnerText;

    private void OnEnable()
    {
        PongNetworkManager.OnPlayerWon += PlayerWon;
    }

    private void OnDisable()
    {
        PongNetworkManager.OnPlayerWon -= PlayerWon;
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