using UnityEngine;
using Pinecone;

public partial class GameLogic : NetworkBehaviour
{
    public PongNetworkManager gameManager;
    public Ball spawnedBall;

    [NetworkSync]
    public int Player1Score;
    [NetworkSync]
    public int Player2Score;

    public void ServerIncrementScore(int playerIndex)
    {
        spawnedBall.transform.position = new Vector3(0, 100);

        if (playerIndex == 0)
            Player1ScoreGenerated++;
        else if (playerIndex == 1)
            Player2ScoreGenerated++;

        if (Player1Score >= 5 || Player2Score >= 5)
        {
            CallGeneratedPlayerWonRPC(Player1Score == 5 ? 0 : 1);
        }

        Generated.RPCGoalScored(this);
        Invoke(nameof(ServerResetGame), 2f);
    }

    public void ServerResetGame()
    {
        spawnedBall.transform.position = Vector3.zero;
        spawnedBall.StartMovingBall();
    }

    [NetworkRPC]
    public void RPCGoalScored()
    {
        PongNetworkManager.TriggerGoalScored();
    }

    public void CallGeneratedPlayerWonRPC(int playerIndex)
    {
        // Reset Scores
        Player1ScoreGenerated = 0;
        Player2ScoreGenerated = 0;

        Generated.RPCPlayerWon(this, playerIndex);
    }

    [NetworkRPC]
    public void RPCPlayerWon(int playerIndex)
    {
        spawnedBall.transform.position = new Vector3(0, 100);

        PongNetworkManager.TriggerPlayerWon(playerIndex);
    }
}
