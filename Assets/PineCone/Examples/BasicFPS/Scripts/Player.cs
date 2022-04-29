using UnityEngine;
using System.Collections;
using System.Linq;

namespace Pinecone.Examples.BasicFPS
{
    public partial class Player : NetworkBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Gun gun;
        [SerializeField] private GameObject deadGameobject;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Renderer[] suitRenderers;

        public PlayerColor playerColor;
        private ScoreboardUI scoreboardUI;
        private Killfeed killFeed;
        private FreezeCam freezeCam;

        public bool fellOffMap;

        [NetworkSync]
        public int Kills;
        [NetworkSync]
        public int Deaths;

        private void Start()
        {
            killFeed = FindObjectOfType<Killfeed>();
            scoreboardUI = FindObjectOfType<ScoreboardUI>();
            freezeCam = Camera.main.transform.GetChild(0).GetComponent<FreezeCam>();
        }



        // Called on the server to when a player got shot
        public void ServerPlayerHit(int gunDamage, NetworkObject shotBy)
        {
            PlayerHealth health = GetComponent<PlayerHealth>();
            Generated.RPCPlayerShot(this, GetComponent<Player>());
            if (health != null)
            {
                health.TakeDamage(gunDamage, shotBy.NetworkObjectID);
                if (health.healthGenerated <= 0)
                {
                    shotBy.gameObject.GetComponent<Player>().KillsGenerated++;
                }
            }
        }

        public void ServerSetPlayerColor(Color32 playerColor, string colorName)
        {
            Generated.RpcSetColor(this, playerColor, colorName);

            if (!NetworkClient.IsHost)
            {
                this.playerColor = new PlayerColor(playerColor, colorName);
                gun.gunReadyColor = playerColor;
                foreach (var renderer in suitRenderers)
                {
                    renderer.materials[0].color = playerColor;
                }
            }
        }

        public void ServerRemoveScoreboardElement(string colorName)
        {
            Generated.RpcRemoveScoreboardElement(this, colorName);
        }

        [NetworkRPC]
        public void RpcRemoveScoreboardElement(string colorName)
        {
            scoreboardUI.RemovePlayer(colorName);
        }

        public void ServerAddScoreboardElement(Player player)
        {
            Generated.RpcAddScoreboardElement(this, player.NetworkObject.NetworkObjectID);
        }

        [NetworkRPC]
        public void RpcAddScoreboardElement(string playerObjectId)
        {
            Player player = FindObjectsOfType<Player>().FirstOrDefault(x => x.NetworkObject.NetworkObjectID == playerObjectId);
            scoreboardUI.AddPlayer(player);
        }

        [NetworkRPC]
        public void RpcSetColor(Color32 playerColor, string colorName)
        {
            this.playerColor = new PlayerColor(playerColor, colorName);
            gun.gunReadyColor = playerColor;
            foreach (var renderer in suitRenderers)
            {
                renderer.materials[0].color = playerColor;
            }
        }

        public void ServerCallSetPlayerColors(NetworkBehaviour newPlayer, Color32 playerColor, string colorName)
        {
            Generated.TargetRpcSetPlayerColors(this, newPlayer, playerColor, colorName);
        }

        [NetworkTargetRPC]
        public void TargetRpcSetPlayerColors(Color32 playerColor, string colorName)
        {
            this.playerColor = new PlayerColor(playerColor, colorName);
            gun.gunReadyColor = playerColor;
            foreach (var renderer in suitRenderers)
            {
                renderer.materials[0].color = playerColor;
            }
        }

        public void ServerAddPlayerToScoreboard(NetworkBehaviour newPlayer, NetworkBehaviour playerToAdd)
        {
            Generated.TargetRpcAddScoreboard(this, newPlayer, playerToAdd.NetworkObject.NetworkObjectID);
        }

        [NetworkTargetRPC]
        public void TargetRpcAddScoreboard(string playerObjectId)
        {
            Player player = FindObjectsOfType<Player>().FirstOrDefault(x => x.NetworkObject.NetworkObjectID == playerObjectId);
            scoreboardUI.AddPlayer(player);
        }

        [NetworkTargetRPC]
        public void RPCPlayerShot()
        {
            // Player got shot. Could be used to play a hurt sound
        }

        private void Update()
        {
            if (!HasAuthority)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                scoreboardUI.Display();
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                scoreboardUI.Hide();
            }

            if (transform.position.y < -30.0f && !fellOffMap)
            {
                fellOffMap = true;
                Generated.CmdPlayerFellOffMap(this);
            }
        }

        public void Die(string killedById)
        {
            GameObject go = Instantiate(deadGameobject, transform.position, transform.rotation);
            Renderer[] renderers = go.transform.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.materials[0].color = playerColor.color;
            }
            go.GetComponent<Rigidbody>().AddExplosionForce(500, go.transform.position + Random.insideUnitSphere * Random.Range(-5, 5), 100f);
            Destroy(go, 8f);

            NetworkObject networkObjectKilledBy = FindObjectsOfType<NetworkObject>().FirstOrDefault(x => x.NetworkObjectID == killedById);
            if (killedById != "WORLD")
            {
                // Spawn kill feed element
                killFeed.AddKillfeed(networkObjectKilledBy.gameObject.GetComponent<Player>().playerColor, this.playerColor);
            }
            else
            {
                killFeed.AddKillfeed(this.playerColor, new PlayerColor(), true);
            }

            characterController.enabled = false;
            if (HasAuthority)
            {
                if (networkObjectKilledBy != null && killedById != "WORLD")
                {
                    cameraController.StartFreezeCam(networkObjectKilledBy.gameObject.transform.position, freezeCam);
                }

                playerController.enabled = false;
                cameraController.enabled = false;
                gun.enabled = false;
                gun.SetRendererEnabled(false);
            }
            else
            {
                gun.SetRendererEnabled(false);
                playerController.SetRenderer(false);
            }

            Invoke(nameof(Respawn), 2f);
        }

        public void Respawn()
        {
            if (HasAuthority)
            {
                Generated.CmdRespawn(this);
                Transform respawnPoint = ((FPSNetworkManager)NetworkManager.Singleton).GetSpawnTransform();
                transform.position = respawnPoint.position;
                playerController.enabled = true;
                playerController.CurrentSpeedMultiplier = 1.0f;
                playerController.Velocity = Vector3.zero;
                cameraController.enabled = true;
                cameraController.SetRotation(respawnPoint.rotation);
                Physics.SyncTransforms();
                cameraController.FinishFreezeCam(freezeCam);
                gun.enabled = true;
                fellOffMap = false;
                characterController.enabled = true;
                gun.SetRendererEnabled(true);
            }
            else
            {
                // Delay turning on the object again in order for the packet of teleporting to be sent first.
                StartCoroutine(EnablePlayerView(playerController, gun, characterController));
            }
        }

        private IEnumerator EnablePlayerView(PlayerController playerController, Gun gun, CharacterController controller)
        {
            yield return new WaitForSeconds(0.2f);
            playerController.SetRenderer(true);
            gun.SetRendererEnabled(true);
            controller.enabled = true;
        }

        [NetworkCommand]
        public void CmdRespawn()
        {
            PlayerHealth playerHealth = gameObject.GetComponent<PlayerHealth>();
            playerHealth.SetHealth(playerHealth.maxHealth);
            playerHealth.Dead = false;
        }

        [NetworkCommand]
        public void CmdPlayerFellOffMap()
        {
            GetComponent<PlayerHealth>()?.TakeDamage(10000, "WORLD");
        }

    }
}
