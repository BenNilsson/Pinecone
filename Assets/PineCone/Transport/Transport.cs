using System;
using UnityEngine;

namespace Pinecone
{
    public abstract class Transport : MonoBehaviour
    {
        /// <summary>
        /// The current active transport layer
        /// </summary>
        public static Transport activeTransport;

        public abstract void Tick();

        // Server functions
        /////////////////////////////////////////////////
        public abstract void ServerStart(int maxConnections);
        public abstract void ServerStop();
        public abstract void ServerDisconnectClient(int connectionId);
        public abstract void ServerSend(NetworkMessage message, int connectionId = -1);

        // Server events
        public event Action<int> OnClientConnectedServer;
        protected void InvokeClientConnectedServer(int connectionId = 0) { OnClientConnectedServer?.Invoke(connectionId); } // C# does not allow to invoke the event unless in class? Created function for now to solve this issue.

        public event Action<int> OnClientDisconnectedServer;
        protected void InvokeClientDisconnectedServer(int connectionId = 0) { OnClientDisconnectedServer?.Invoke(connectionId); } // C# does not allow to invoke the event unless in class? Created function for now to solve this issue.
        public event Action<NetworkMessage> OnClientSendServer;
        public void InvokeClientSendServer(NetworkMessage message) { OnClientSendServer?.Invoke(message); }

        // Client functions
        /////////////////////////////////////////////////
        public abstract void ClientConnect(string ipAddress);
        public abstract void ClientDisconnect();
        public abstract void ClientSend(NetworkMessage message);

        // Client events
        public event Action OnClientConnected;
        protected void InvokeClientConnected() { OnClientConnected?.Invoke(); } // C# does not allow to invoke the event unless in class? Created function for now to solve this issue.

        public event Action OnClientDisconnected;
        protected void InvokeClientDisconnected() { OnClientDisconnected?.Invoke(); } // C# does not allow to invoke the event unless in class? Created function for now to solve this issue.
        public event Action<NetworkMessage> OnClientSend;
        public void InvokeClientSend(NetworkMessage message) { OnClientSend?.Invoke(message); }

    }
}