using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pinecone;

public partial class GameLogic : NetworkBehaviour
{
    public GameManager gameManager;

    public void CallGeneratedGoalRPC(int scoredIndex)
    {
        Generated.RPCGoalScored(this, scoredIndex);
    }

    public void CallGeneratedPlayerWonRPC(int playerIndex)
    {
        Generated.RPCPlayerWon(this, playerIndex);
    }

    public void CallGeneratedGameResetRPC()
    {
        Generated.RPCGameReset(this);
    }

    [NetworkRPC]
    public void RPCGoalScored(int scoredIndex)
    {
        if (scoredIndex == 0)
            gameManager.ScorePlayer1++;
        else if (scoredIndex == 1)
            gameManager.ScorePlayer2++;

        GameManager.TriggerOnGoal(scoredIndex);
    }

    [NetworkRPC]
    public void RPCPlayerWon(int playerIndex)
    {
        gameManager.ScorePlayer1 = 0;
        gameManager.ScorePlayer2 = 0;

        GameManager.TriggerPlayerWon(playerIndex);
    }

    [NetworkRPC]
    public void RPCGameReset()
    {
        gameManager.ScorePlayer1 = 0;
        gameManager.ScorePlayer2 = 0;

        GameManager.TriggerGameReset();
    }
}
