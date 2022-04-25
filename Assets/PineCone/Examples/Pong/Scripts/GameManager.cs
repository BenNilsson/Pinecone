using UnityEngine;
using Pinecone;
using System;

public partial class GameManager : NetworkManager
{
    public static event Action<int> OnGoal;
    public static event Action<int> OnPlayerWon;
    public static event Action OnGameReset;

    public static void TriggerOnGoal(int player) { OnGoal?.Invoke(player); }
    public static void TriggerPlayerWon(int player) { OnPlayerWon?.Invoke(player); }
    public static void TriggerGameReset() { OnGameReset?.Invoke(); }


    [SerializeField] private GameObject ball;
    private GameObject spawnedBall;

    [SerializeField] private Vector3[] spawnPositions;

    // Required to send RPC and spawn objects on the network.
    [SerializeField] public GameLogic gameLogicNetworkBehaviour;

    public int ScorePlayer1;
    public int ScorePlayer2;

    private GameObject player1;
    private GameObject player2;

    public override void OnServerStart()
    {
        OnGoal += GoalScored;
    }

    public override void OnServerStopped()
    {
        OnGoal -= GoalScored;
    }

    public override GameObject SpawnPlayer()
    {
        GameObject player = null;
        if (player1 == null)
        {
            player = Instantiate(spawnableObjects[playerGameObject], spawnPositions[0], Quaternion.identity);
            player1 = player;
        }
        else if (player2 == null)
        {
            player = Instantiate(spawnableObjects[playerGameObject], spawnPositions[1], Quaternion.identity);
            player2 = player;
        }
        
        return player;
    }

    private void GoalScored(int playerIndex)
    {
        NetworkServer.Destroy(spawnedBall, gameLogicNetworkBehaviour, true);
        gameLogicNetworkBehaviour.CallGeneratedGoalRPC(playerIndex);

        if (playerIndex == 0)
            ScorePlayer1++;
        else if (playerIndex == 1)
            ScorePlayer2++;

        if (ScorePlayer1 >= 5 || ScorePlayer2 >= 5)
        {
            gameLogicNetworkBehaviour.CallGeneratedPlayerWonRPC(ScorePlayer1 == 5 ? 0 : 1);
            OnPlayerWon?.Invoke(ScorePlayer1 == 5 ? 0 : 1);
            Invoke(nameof(ServerResetGame), 3);

            ScorePlayer1 = 0;
            ScorePlayer2 = 0;
        }
        else
        {
            Invoke(nameof(SpawnBall), 2);
        }
    }

    private void SpawnBall()
    {
        spawnedBall = Instantiate(ball.gameObject, Vector3.zero, Quaternion.identity);
        spawnedBall.GetComponent<Ball>().StartMovingBall();
        NetworkServer.Spawn(spawnedBall, gameLogicNetworkBehaviour, true);
    }
    
    public void ServerResetGame()
    {
        gameLogicNetworkBehaviour.CallGeneratedGameResetRPC();
        OnGameReset?.Invoke();
        if (NetworkManager.NumberOfPlayers == 2)
        {
            SpawnBall();
        }
    }

    public override void ClientConnectedServer(int connectionId)
    {
        if (NetworkManager.NumberOfPlayers == 2)
            SpawnBall();
    }

    public override void ClientDisconnectedServer(int connectionId)
    {
        if (spawnedBall != null)
        {
            NetworkServer.Destroy(spawnedBall, gameLogicNetworkBehaviour, true);
        }
    }
}
