using UnityEngine;
using System;

namespace Pinecone.Examples.Pong
{
    public partial class PongNetworkManager : NetworkManager
    {
        public static event Action<int> OnPlayerWon;
        public static event Action OnGoalScored;

        public static void TriggerPlayerWon(int player) { OnPlayerWon?.Invoke(player); }
        public static void TriggerGoalScored() { OnGoalScored?.Invoke(); }


        [SerializeField] private GameObject ball;
        private Ball spawnedBall;

        [SerializeField] private Vector3[] spawnPositions;

        // Required to send RPC and spawn objects on the network.
        [SerializeField] public GameLogic gameLogicNetworkBehaviour;

        public int ScorePlayer1;
        public int ScorePlayer2;

        private GameObject player1;
        private GameObject player2;

        public override void OnServerStart()
        {
            base.OnServerStart();

            // Set up here since the object is not spawned in. OnStart does not get called otherwise.
            gameLogicNetworkBehaviour.gameObject.GetComponent<NetworkObject>().OnStart();
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

        private void ServerSpawnBall()
        {
            spawnedBall = Instantiate(ball, Vector3.zero, Quaternion.identity).GetComponent<Ball>();
            gameLogicNetworkBehaviour.spawnedBall = spawnedBall;
            gameLogicNetworkBehaviour.ServerResetGame();

            NetworkServer.Spawn(spawnedBall.gameObject, gameLogicNetworkBehaviour, true);
        }

        public override void ClientConnectedServer(int connectionId)
        {
            if (NetworkManager.NumberOfPlayers == 2)
                ServerSpawnBall();
        }

        public override void ClientDisconnectedServer(int connectionId)
        {
            if (spawnedBall != null)
            {
                NetworkServer.Destroy(spawnedBall.gameObject, gameLogicNetworkBehaviour, true);
            }
        }
    }
}
