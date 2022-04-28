using System.Collections.Generic;
using UnityEngine;

namespace Pinecone
{
    [AddComponentMenu("Networking/" + nameof(NetworkManager))]
    [DisallowMultipleComponent]
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Singleton;

        [Header("General Configuration")]
        /// <summary>
        /// Should the NetworkManager persist throughout scenes?
        /// </summary>
        [Tooltip("Should the NetworkManager persist throughout scenes?")]
        public bool dontDestroyOnLoad = true;

        [Header("Network Configuration")]
        /// <summary>
        /// The Transport layer that will be used by the client and server.
        /// </summary>
        [SerializeField]
        [Tooltip("The Transport layer that will be used by the client and server.")]
        protected Transport transport;

        /// <summary>
        /// The address which a client should connect to.
        /// </summary>
        [Tooltip("The address which a client should connect to.")]
        public string serverIpAddress = "127.0.0.1";

        /// <summary>
        /// The maximum amount of clients allowed on a server.
        /// </summary>
        [Tooltip("The maximum amount of clients allowed on a server.")]
        public int maxConnections = 20;

        /// <summary>
        /// The rate of how fast the server updates. For slower-paced games, 30 should be fine. Modern FPS games will use 60-120.
        /// </summary>
        [Tooltip("The rate of how fast the server updates. For slower-paced games, 30 should be fine. Modern FPS games will use 60-120.")]
        public int tickRate = 30;

        [Header("GameObjects")]
        public int playerGameObject;

        /// <summary>
        /// Objects that can be spanwed on the network. Used to easily get a reference from objects.
        /// </summary>
        [Tooltip("Objects that can be spanwed on the network. GameObjects in here must have NetworkObject on them.")]
        public List<GameObject> spawnableObjects = new List<GameObject>();

        /// <summary>
        /// Number of players connection to the server. Only set on the server.
        /// </summary>
        public static int NumberOfPlayers => NetworkServer.Connections.Count;


        /// <summary>
        /// Set to true in StartHost. Will start a client when the server has booted if true.
        /// </summary>
        private bool startHost = false;


#if UNITY_EDITOR
        public virtual void OnValidate()
        {
            if (maxConnections < 0)
            {
                maxConnections = 0;
            }
        }
#endif

        public virtual void Update()
        {
            NetworkLoop.Tick();
        }

        public virtual void Awake()
        {
            if (!InitializeSingleton())
            {
                return;
            }
        }

        public virtual void Start()
        {
#if UNITY_SERVER
            StartServer();
#endif
        }

        /// <summary>
        /// Starts a server then connects to it when it is active.
        /// Will always connect to localhost. IP is ignored.
        /// </summary>
        public void StartHost()
        {
            if (NetworkClient.IsConnected || NetworkServer.IsActive)
                return;

            startHost = true;
            StartServer();
        }

        /// <summary>
        /// Stops the client then the server.
        /// </summary>
        public void StopHost()
        {
            if (NetworkServer.IsActive)
                StopServer();

            if (NetworkClient.IsConnected)
                StopClient();

            // Reset host state
            startHost = false;
        }

        /// <summary>
        /// Starts the server and allows for listening of clients.
        /// </summary>
        public void StartServer()
        {
            if (NetworkServer.IsActive)
            {
                Debug.LogWarning("The server has already been started...");
                return;
            }
            SetupServer();

            Transport activeTransport = Transport.activeTransport;
            activeTransport.OnClientConnected += ClientConnected;
            activeTransport.OnClientDisconnected += ClientDisconnected;
            activeTransport.OnClientConnectedServer += ClientConnectedServer;

            OnServerStart();
        }

        public void StopServer()
        {
            if (!NetworkServer.IsActive)
            {
                return;
            }

            Transport activeTransport = Transport.activeTransport;
            activeTransport.OnClientConnected -= ClientConnected;
            activeTransport.OnClientDisconnected -= ClientDisconnected;
            activeTransport.OnClientConnectedServer -= ClientConnectedServer;

            NetworkServer.DisconnectAllClients();
            NetworkServer.StopServer();
            OnServerStopped();
        }

        public void SendServer(NetworkMessage message)
        {
            if (!NetworkServer.IsActive)
            {
                return;
            }

            NetworkServer.Send(message);
        }

        /// <summary>
        /// Starts the client and connects to the server.
        /// </summary>
        public void StartClient(string ip)
        {
            if (NetworkClient.IsConnected)
                return;

            InitializeSingleton();

            Application.runInBackground = true;
            NetworkClient.Connect(ip);
        }

        public void SendClient(NetworkMessage message)
        {
            if (!NetworkClient.IsConnected)
                return;

            if (Transport.activeTransport == null)
                return;

            NetworkClient.Send(message);
        }

        public void StopClient()
        {
            if (!NetworkClient.IsConnected)
                return;

            NetworkClient.Disconnect();
        }

        /// <summary>
        /// Called on the client when a client has connected.
        /// </summary>
        public virtual void ClientConnected()
        {

        }

        /// <summary>
        /// Called on the client when a client has disconnected.
        /// </summary>
        public virtual void ClientDisconnected()
        {

        }

        /// <summary>
        /// Called on the server when it starts. Base contains host logic.
        /// </summary>
        public virtual void OnServerStart()
        {
            // If hosting, start a client as 
            if (startHost)
            {
                StartClient("127.0.0.1");
            }
        }

        /// <summary>
        /// Called on the server when a client has connected.
        /// </summary>
        public virtual void ClientConnectedServer(int connectionId)
        {

        }

        /// <summary>
        /// Called on the server when a client has connected
        /// </summary>
        public virtual void ClientDisconnectedServer(int connectionId)
        {

        }

        /// <summary>
        /// Called on the server when it stops
        /// </summary>
        public virtual void OnServerStopped()
        {

        }

        public virtual void OnApplicationQuit()
        {
            if (NetworkClient.IsHost)
            {
                StopHost();
                return;
            }

            if (NetworkClient.IsConnected)
            {
                NetworkClient.Disconnect();
            }

            if (NetworkServer.IsActive)
            {
                StopServer();
            }
        }

        private void SetupServer()
        {
            InitializeSingleton();

            Application.runInBackground = true;
            if (!startHost)
                Application.targetFrameRate = tickRate;

            NetworkServer.StartServer(maxConnections);
        }

        private bool InitializeSingleton()
        {
            if (Singleton != null && Singleton == this)
                return true;

            if (dontDestroyOnLoad)
            {
                if (Singleton != null)
                {
                    Destroy(gameObject);
                    return false;
                }

                Singleton = this;
                if (Application.isPlaying)
                {
                    transform.SetParent(null);
                    DontDestroyOnLoad(gameObject);
                }
            }else
            {
                Singleton = this;
            }

            Transport.activeTransport = transport;

            return true;
        }

        /// <summary>
        /// Called on server to tell other clients to spawn an object.
        /// </summary>
        /// <param name="spawn">The instantiated gameobject on the server.</param>
        /// <param name="connectionId">The connectionID of what client needs authority over the object. Server has authority on -1</param>
        public virtual GameObject SpawnPlayer()
        {
            GameObject go = Instantiate(Singleton.spawnableObjects[Singleton.playerGameObject]);
            return go;
        }
    }
}