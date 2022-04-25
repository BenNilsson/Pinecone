using UnityEngine;

namespace Pinecone
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    [AddComponentMenu("Networking/" + nameof(NetworkManagerControlsHUD))]
    public class NetworkManagerControlsHUD : MonoBehaviour
    {
        [SerializeField] private Vector2 position = new Vector2(10, 10);

        private NetworkManager networkManager;

        private string ipAddress = "127.0.0.1";

        private void Awake()
        {
            networkManager = GetComponent<NetworkManager>();
            ipAddress = PlayerPrefs.GetString("ClientIPAddressToConnectTo", "127.0.0.1");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(position.x, position.y, 300, 10000));

            if (!NetworkServer.IsActive && !NetworkClient.IsConnected)
            {
                DisplayStartButtons();
            }else
            {
                DrawRuntimeInfo();
            }

            DisplayStopButtons();
            GUILayout.EndArea();
        }

        private void DisplayStartButtons()
        {
            if (!NetworkClient.IsConnected)
            {
                if (!NetworkServer.IsActive)
                {
                    if (GUILayout.Button("Start Host"))
                    {
                        networkManager.StartHost();
                    }
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Ip Address", GUILayout.ExpandWidth(false));
                ipAddress = GUILayout.TextField(ipAddress, 45);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Start Client"))
                {
                    PlayerPrefs.SetString("ClientIPAddressToConnectTo", ipAddress);
                    PlayerPrefs.Save();
                    networkManager.StartClient(ipAddress);
                }
            }
            if (GUILayout.Button("Start Server"))
            {
                networkManager.StartServer();
            }
        }

        private void DrawRuntimeInfo()
        {
            if (NetworkServer.IsActive)
            {
                GUILayout.Label($"Running Server Via Transport: {Transport.activeTransport}");
            }else if (NetworkClient.IsConnected)
            {
                GUILayout.Label($"Client connected to {networkManager.serverIpAddress} via: {Transport.activeTransport}");
            }
        }

        private void DisplayStopButtons()
        {
            if (NetworkServer.IsActive && !NetworkClient.IsHost)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    networkManager.StopServer();
                }
            }else if (NetworkClient.IsHost)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    networkManager.StopHost();
                }
            }
            else if (NetworkClient.IsConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    networkManager.StopClient();
                }
            }
        }
    }
}