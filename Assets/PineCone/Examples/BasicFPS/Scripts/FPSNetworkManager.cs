using UnityEngine;
using System.Collections.Generic;

// BasicFPS uses a lot of inefficient gameplay code (such as GetComponent everywhere).
// The example is meant to show the useability of the networking library, not how to make an FPS.

namespace Pinecone.Examples.BasicFPS
{
    [System.Serializable]
    public struct PlayerColor
    {
        public Color32 color;
        public string colorName;

        public PlayerColor(Color32 color, string colorName)
        {
            this.color = color;
            this.colorName = colorName;
        }
    }

    public class FPSNetworkManager : NetworkManager
    {
        [SerializeField] private Transform[] spawnPositions;

        [SerializeField] private List<PlayerColor> playerColors = new List<PlayerColor>();
        private Dictionary<int, PlayerColor> usedColors = new Dictionary<int, PlayerColor>();

        private void OnEnable()
        {
            NetworkServer.OnPlayerSpawned += PlayerSpawned;
            NetworkServer.OnOtherPlayersSpawned += OtherPlayersSpawned;
        }

        private void OnDisable()
        {
            NetworkServer.OnPlayerSpawned -= PlayerSpawned;
            NetworkServer.OnOtherPlayersSpawned -= OtherPlayersSpawned;
        }

        private void PlayerSpawned(int connectionId)
        {
            PlayerColor color = new PlayerColor();
            if (playerColors.Count > 0)
            {
                color = playerColors[Random.Range(0, playerColors.Count)];
                playerColors.Remove(color);
                usedColors.Add(connectionId, color);
            }
            PlayerConnection playerConnection = NetworkServer.Connections.Find(x => x.ConnectionId == connectionId);
            Player player = playerConnection.PlayerObject.GetComponent<Player>();
            player.ServerSetPlayerColor(color.color, color.colorName);
            player.ServerAddScoreboardElement(player);
        }

        private void OtherPlayersSpawned(int connectionId)
        {
            PlayerConnection playerConnection = NetworkServer.Connections.Find(x => x.ConnectionId == connectionId);
            NetworkBehaviour newPlayer = playerConnection.PlayerObject.GetComponent<NetworkBehaviour>();
            foreach (var connection in NetworkServer.Connections)
            {
                if (connection.ConnectionId == connectionId)
                    continue;

                Player p = connection.PlayerObject.GetComponent<Player>();
                p.ServerCallSetPlayerColors(newPlayer, p.playerColor.color, p.playerColor.colorName);
                p.ServerAddPlayerToScoreboard(newPlayer, p);
            }
        }

        public override GameObject SpawnPlayer()
        {
            Transform spawn = GetSpawnTransform();
            GameObject go = Instantiate(Singleton.spawnableObjects[Singleton.playerGameObject], spawn.position, spawn.rotation);
            return go;
        }

        public Transform GetSpawnTransform()
        {
            return spawnPositions[Random.Range(0, spawnPositions.Length)];
        }

        public override void ClientDisconnectedServer(int connectionId)
        {
            if (usedColors.ContainsKey(connectionId))
            {
                PlayerColor color = usedColors[connectionId];
                playerColors.Add(usedColors[connectionId]);
                usedColors.Remove(connectionId);

                PlayerConnection playerConnection = NetworkServer.Connections.Find(x => x.ConnectionId == connectionId);
                if (playerConnection == null)
                    return;

                playerConnection.PlayerObject.GetComponent<Player>()?.ServerRemoveScoreboardElement(color.colorName);
            }
        }
    }
}