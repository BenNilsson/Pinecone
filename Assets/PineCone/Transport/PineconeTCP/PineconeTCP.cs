using UnityEngine;
using Pinecone;

namespace PineconeTCP
{
    [RequireComponent(typeof(NetworkManager))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Networking/Transport/" + nameof(PineconeTCP))]
    public class PineconeTCP : Transport
    {
        [SerializeField] private int port = 7777;

        /// <summary>
        /// Time in miliseconds before send gets timed out.
        /// </summary>
        [Header("Advanced")]
        [Tooltip("Time in miliseconds before send gets timed out.")]
        [SerializeField] private int sendTimeout = 5000;

        /// <summary>
        /// Time in miliseconds before a receive gets timed out. High to avoid timeouts on large game changes (such as scene chanages).
        /// </summary>
        [Tooltip("Time in miliseconds before a receive gets timed out. High to avoid timeouts on large game changes (such as scene chanages).")]
        [SerializeField] private int receiveTimeout = 30000;

        private Server server;
        private Client client;

        public override void ServerStart(int maxConnections = 0)
        {
            if (server != null)
            {
                if (server.IsActive)
                {
                    return;
                }else
                {
                    server = null;
                }
            }
            server = new Server();

            server.sendTimeout = sendTimeout;
            server.receiveTimeout = receiveTimeout;

            server.OnClientConnected += ClientConnectedServer;
            server.OnClientDisconnected += ClientDisconnectedServer;

            server.Start(port, maxConnections);
        }

        public override void ServerStop()
        {
            server.OnClientConnected -= ClientConnectedServer;
            server.OnClientDisconnected -= ClientDisconnectedServer;

            server?.Stop();
            server = null;
        }

        private void ClientConnected()
        {
            InvokeClientConnected();
        }

        private void ClientDisconnected()
        {
            InvokeClientDisconnected();
            client.OnClientDisconnected -= ClientDisconnect;
        }

        private void ClientDisconnectedServer(int connectionId)
        {
            InvokeClientDisconnectedServer(connectionId);
            NetworkServer.Connections.Remove(NetworkServer.Connections.Find(x => x.ConnectionId == connectionId));
        }

        private void ClientConnectedServer(int connectionId)
        {
            NetworkServer.Connections.Add(new PlayerConnection(connectionId));
            InvokeClientConnectedServer(connectionId);
        }

        public override void ServerSend(NetworkMessage message, int connectionId)
        {
            if (server != null)
            {
                server.Send(message, connectionId);
            }
        }

        public override void ClientConnect(string ipAddress)
        {
            client = new Client();

            client.OnClientDisconnected += ClientDisconnected;

            client.Connect(ipAddress, port);
            bool connected = client.IsConnected;
            if (connected)
            {
                InvokeClientConnected();
            }
        }

        public override void ClientDisconnect()
        {
            client?.Disconnect();
            if (client != null && !client.IsConnected)
                InvokeClientDisconnected();

            client = null;
        }

        public override void ClientSend(NetworkMessage message)
        {
            if (client != null)
            {
                client.Send(message);
                InvokeClientSend(message);
            }
        }

        public override void Tick()
        {
            client?.Tick();
            server?.Tick();
        }

        public override void ServerDisconnectClient(int connectionId)
        {
            server.Disconnect(connectionId);
        }
    }
}
