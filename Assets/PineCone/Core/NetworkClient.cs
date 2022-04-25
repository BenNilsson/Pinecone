using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinecone
{
    public enum ConnectionState
    {
        None = 0,
        Connecting,
        Connected,
        Disconnecting,
        Disconnected
    }

    public static class NetworkClient
    {
        // Network Objects spawned/owned by this client.
        public static Dictionary<string, NetworkObject> spawnedObjects = new Dictionary<string, NetworkObject>();

        public static bool IsConnected => connectionState == ConnectionState.Connected;
        public static ConnectionState connectionState { get; private set; }

        /// <summary>
        /// Set to true if a server is active and a client is connected.
        /// </summary>
        public static bool IsHost => NetworkServer.IsActive && NetworkClient.IsConnected;

        public static void Connect(string ipAddress)
        {
            if (IsConnected)
                return;

            if (Transport.activeTransport == null)
            {
                Debug.LogWarning("No active transport layer was set... Cancelling connection");
                return;
            }

            Transport.activeTransport.enabled = true;

            // Set up events to receive information from the transport layer
            RegisterTransportEvents();

            connectionState = ConnectionState.Connecting;
            Transport.activeTransport.ClientConnect(ipAddress);
        }

        public static void Send(NetworkMessage message)
        {
            Transport.activeTransport.ClientSend(message);
        }

        public static void Disconnect()
        {
            if (!IsConnected)
                return;

            Transport.activeTransport.ClientDisconnect();

            connectionState = ConnectionState.Disconnecting;
        }

        /// <summary>
        /// Allows the NetworkClient to receive information from the transport layer without being directly hooked.
        /// </summary>
        private static void RegisterTransportEvents()
        {
            Transport.activeTransport.OnClientConnected += OnTransportClientConnected;
            Transport.activeTransport.OnClientDisconnected += OnTransportClientDisconnected;
        }

        private static void UnregisterTransportEvents()
        {
            Transport.activeTransport.OnClientConnected -= OnTransportClientConnected;
            Transport.activeTransport.OnClientDisconnected -= OnTransportClientDisconnected;
        }

        /// <summary>
        /// Called when a client connects to the transport layer.
        /// </summary>
        private static void OnTransportClientConnected()
        {
            // Return if it has somehow been called twice. This is to avoid unsubscribing of events twice.
            if (IsConnected)
                return;

            connectionState = ConnectionState.Connected;
        }

        /// <summary>
        /// Called when a client disconnected from the transport layer.
        /// </summary>
        private static void OnTransportClientDisconnected()
        {
            // Return if it has somehow been called twice. This is to avoid unsubscribing of events twice.
            if (connectionState == ConnectionState.Disconnected)
                return;

            connectionState = ConnectionState.Disconnected;

            // Should probably not do this?
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

            UnregisterTransportEvents();
        }

        public static void SpawnPlayerObject(int playerSpawnIndex, string guidString, bool hasAuthority, Vector3 position, Quaternion rotation, bool isHost)
        {
            NetworkObject networkObject;
            if (!isHost)
                networkObject = GameObject.Instantiate(NetworkManager.Singleton.spawnableObjects[playerSpawnIndex], position, rotation).GetComponent<NetworkObject>();
            else
                networkObject = GameObject.FindObjectsOfType<NetworkObject>().Where(x => x.NetworkObjectID == guidString).FirstOrDefault();

            networkObject.HasAuthority = hasAuthority;
            networkObject.NetworkObjectID = guidString;
            networkObject.OnStart();
        }
    }
}
