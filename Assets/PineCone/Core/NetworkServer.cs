using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinecone
{
    public static class NetworkServer
    {
        public struct MethodToProcess
        {
            public string objectId;
            public int behaviourIndex;
            public string methodName;
            public object[] parameters;

            public MethodToProcess(string objectId, int behaviourIndex, string methodName, object[] parameters)
            {
                this.objectId = objectId;
                this.behaviourIndex = behaviourIndex;
                this.methodName = methodName;
                this.parameters = parameters;
            }
        }

        /// <summary>
        /// Is the server currently running?
        /// </summary>
        public static bool IsActive { get; internal set; }

        /// <summary>
        /// The maximum amount of clients that can be on the server.
        /// </summary>
        public static int MaxConnections { get; private set; }

        private static bool initialized;

        /// <summary>
        /// All clients currently connected to the server.
        /// </summary>
        public static List<PlayerConnection> Connections = new List<PlayerConnection>();


        private static Dictionary<string, GameObject> gameObjectsOnNetwork = new Dictionary<string, GameObject>();
        private static Queue<MethodToProcess> methodsToProcess = new Queue<MethodToProcess>();

        public static int HostID = -1;

        /// <summary>
        /// Called when a new user has player object has been spawned
        /// </summary>
        public static event Action<int> OnPlayerSpawned;

        /// <summary>
        /// Called when a new user has spawned all other players.
        /// </summary>
        public static event Action<int> OnOtherPlayersSpawned;

        public static void Tick()
        {
            // Early out if server is not active or has no messages to process.
            if (methodsToProcess.Count <= 0)
                return;

            MethodToProcess method = methodsToProcess.Dequeue();

            if (method is MethodToProcess methodToProcess)
            {
                NetworkObject networkObject = GameObject.FindObjectsOfType<NetworkObject>().Where(x => x.NetworkObjectID == methodToProcess.objectId).FirstOrDefault();
                List<NetworkBehaviour> behaviours = networkObject?.NetworkBehaviours;
                NetworkBehaviour networkBehaviour = behaviours?.Find(x => x.BehaviorIndex == methodToProcess.behaviourIndex);

                if (networkBehaviour != null)
                {
                    networkBehaviour.GetType().GetMethod(methodToProcess.methodName).Invoke(networkBehaviour, methodToProcess.parameters);
                }
            }
        }

        public static void Spawn(GameObject spawn, NetworkBehaviour clientWithAuthority, bool serverAuthority = false)
        {
            if (!initialized)
                return;

            if (!IsActive)
            {
                GameObject.DestroyImmediate(spawn);
                Debug.LogError("Tried to call NetworkServer.Spawn on a client");
                throw new ClientAccessViolationException("Tried to call NetworkServer.Spawn on a client");
            }

            if (!spawn.TryGetComponent<NetworkObject>(out var networkObject))
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a GameObject on the network that did not contain a NetworkObject script");
                throw new NetworkServerException("Tried to spawn a GameObject on the network that did not contain a NetworkObject script");
            }

            if (!Guid.TryParse(networkObject.NetworkObjectID, out var id))
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a NetworkObject without a valid Guid.");
                throw new NetworkServerException("Tried to spawn a NetworkObject without a valid Guid.");
            }

            int index = NetworkManager.Singleton.spawnableObjects.FindIndex(x => x.GetComponent<NetworkObject>().NetworkObjectID == networkObject.NetworkObjectID);
            if (index == -1)
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a NetworkObject without a valid Guid. Make sure the GameObject is inside of the Network Manager Spawnable List!");
                throw new NetworkServerException("Tried to spawn a NetworkObject without a valid Guid.");
            }

            if (!serverAuthority)
            {
                Connections.Find(x => x.ConnectionId == clientWithAuthority.OwningID).PlayerOwnedObjects
                    .Add(clientWithAuthority.NetworkObject.NetworkObjectID, networkObject);
            }

            string guidString = Guid.NewGuid().ToString();
            if (!gameObjectsOnNetwork.ContainsKey(guidString))
            {
                gameObjectsOnNetwork.Add(guidString, spawn);
            }
            else
            {
                Debug.LogError("Tried to spawn a GameObject on the network of the same instance more than once.");
                throw new NetworkServerException("Tried to spawn a GameObject on the network of the same instance more than once.");
            }
            NetworkObject spawnedNetworkObject = spawn.GetComponent<NetworkObject>();
            spawnedNetworkObject.HasAuthority = serverAuthority;
            spawnedNetworkObject.NetworkObjectID = guidString;

            if (!serverAuthority)
            {
                spawnedNetworkObject.NetworkOwnerID = clientWithAuthority.OwningID;
            }

            if (!NetworkClient.IsHost || serverAuthority)
            {
                networkObject.OnStart();
            }

            foreach (var client in Connections)
            {
                // Skip sending the message if we are the host. Avoids duplicate spawning
                if (client.ConnectionId == HostID)
                    continue;

                // NetworkObject ID, Behavioiur Index, Method Name, Index, Guidstring, HasAuthority, Position, Rotation
                NetworkMessage spawnRPCMessage = new NetworkMessage(MessageType.TargetRPC);
                spawnRPCMessage.AddString(clientWithAuthority.NetworkObject.NetworkObjectID.ToString());
                spawnRPCMessage.AddInt(clientWithAuthority.BehaviorIndex);
                spawnRPCMessage.AddString("SpawnObject");

                dynamic[] parameters = MessageSendHelper.CreateDynamicList(
                    index,
                    guidString,
                    serverAuthority ? false : networkObject.NetworkOwnerID == clientWithAuthority.OwningID,
                    spawn.transform.position,
                    spawn.transform.rotation,
                    client.ConnectionId == HostID);
                int argumentCount = parameters.Length / 2;
                spawnRPCMessage.AddInt(argumentCount);
                for (int i = 0; i < parameters.Length; i++)
                {
                    spawnRPCMessage.Add(parameters[i]);
                }
                Send(spawnRPCMessage, client.ConnectionId);
            }
        }

        public static void Destroy(GameObject gameObject, NetworkBehaviour clientWithAuthority, bool serverAuthority = false)
        {
            if (!initialized)
                return;

            if (!gameObject)
            {
                Debug.LogError("Tried to destroy a GameObject that was null");
                throw new NetworkServerException("Tried to destroy a GameObject that was null");
            }

            if (!IsActive)
            {
                Debug.LogError("Tried to call NetworkServer.Destroy on a client");
                throw new ClientAccessViolationException("Tried to call NetworkServer.Destroy on a client");
            }

            if (!gameObject.TryGetComponent<NetworkObject>(out var networkObject))
            {
                Debug.LogError("Tried to destroy a GameObject on the network that did not contain a NetworkObject script");
                throw new NetworkServerException("Tried to destroy a GameObject on the network that did not contain a NetworkObject script");
            }

            if (!Guid.TryParse(networkObject.NetworkObjectID, out var id))
            {
                Debug.LogError("Tried to destroy a NetworkObject without a valid Guid");
                throw new NetworkServerException("Tried to destroy a NetworkObject without a valid Guid");
            }

            // Authority check. Clients should not be able to destroy other client's objects.
            if (!serverAuthority && clientWithAuthority.OwningID != networkObject.NetworkOwnerID)
            {
                Debug.LogError("Client tried to destroy an object that they did not own");
                throw new ClientAccessViolationException("Client tried to destroy an object that they did not own");
            }

            if (!serverAuthority)
            {
                var connection = Connections.Find(x => x.ConnectionId == clientWithAuthority.OwningID);
                if (connection.PlayerOwnedObjects.ContainsKey(networkObject.NetworkObjectID))
                {
                    connection.PlayerOwnedObjects.Remove(networkObject.NetworkObjectID);
                }
            }

            string guidString = id.ToString();
            if (gameObjectsOnNetwork.ContainsKey(guidString))
            {
                gameObjectsOnNetwork.Remove(guidString);
            }
            else
            {
                Debug.LogError("Tried to remove a GameObject on the network that did not exist on the network.");
                throw new NetworkServerException("Tried to remove a GameObject on the network that did not exist on the network.");
            }

            // Destroy on server
            if (!NetworkClient.IsHost || serverAuthority)
                GameObject.Destroy(gameObject);

            // Let others know to destroy it. Keep in mind, some clients may not actually have this object
            // So when the client receives this message, they should check whether or not it actually exists.
            foreach (var client in Connections)
            {
                // Skip sending the message if we are the host. Avoids attempting to destroy null objects.
                if (client.ConnectionId == HostID)
                    continue;

                // NetworkObject ID, Behavioiur Index, Method Name, Index, Guidstring, HasAuthority, Position, Rotation
                NetworkMessage spawnRPCMessage = new NetworkMessage(MessageType.TargetRPC);
                spawnRPCMessage.AddString(clientWithAuthority.NetworkObject.NetworkObjectID.ToString());
                spawnRPCMessage.AddInt(clientWithAuthority.BehaviorIndex);
                spawnRPCMessage.AddString("DestroyGameObject");

                dynamic[] parameters = MessageSendHelper.CreateDynamicList(guidString);
                int argumentCount = parameters.Length / 2;
                spawnRPCMessage.AddInt(argumentCount);
                for (int i = 0; i < parameters.Length; i++)
                {
                    spawnRPCMessage.Add(parameters[i]);
                }
                Send(spawnRPCMessage, client.ConnectionId);
            }
        }

        public static void DisconnectAllClients()
        {
            foreach(var client in Connections.ToArray())
            {
                // Guid

                DisconnectClient(client);
            }
        }

        public static void DisconnectClient(PlayerConnection playerConnection)
        {
            Transport.activeTransport.ServerDisconnectClient(playerConnection.ConnectionId);
        }

        // Spawning works like this (client -> server):
        // NetworkBehaviour sends command about spawning an object (player for instance).
        // The server will know who sent the packet and tell client to spawn the object.
        // The server also includes the authority information, letting clients know whether they control.
        // the object or not. This is useful for things like player movement only registering on the owning client.

        public static void StartServer(int maxConnections)
        {
            Initialize();
            MaxConnections = maxConnections;

            Transport.activeTransport.OnClientConnectedServer += ClientConnected;
            Transport.activeTransport.OnClientDisconnectedServer += ClientDisconnected;

            Transport.activeTransport.ServerStart(maxConnections);
            IsActive = true;
            Debug.Log("Starting Server...");
        }

        public static void StopServer()
        {
            Debug.Log("Stopping Server...");

            Transport.activeTransport.OnClientConnectedServer -= ClientConnected;
            Transport.activeTransport.OnClientDisconnectedServer -= ClientDisconnected;

            Transport.activeTransport.ServerStop();
            IsActive = false;
        }

        private static void ClientConnected(int connectionId)
        {
            GameObject spawn = NetworkManager.Singleton.SpawnPlayer();

            // Tell other clients that a new player has connected
            if (!IsActive)
            {
                GameObject.DestroyImmediate(spawn);
                Debug.LogError("Tried to call NetworkServer.Spawn on a client");
                throw new ClientAccessViolationException("Tried to call NetworkServer.Spawn on a client");
            }

            if (!spawn.TryGetComponent<NetworkObject>(out var networkObject))
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a GameObject on the network that did not contain a NetworkObject script");
                throw new NetworkServerException("Tried to spawn a GameObject on the network that did not contain a NetworkObject script");
            }

            if (!Guid.TryParse(networkObject.NetworkObjectID, out var id))
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a NetworkObject without a valid Guid.");
                throw new NetworkServerException("Tried to spawn a NetworkObject without a valid Guid.");
            }

            int index = NetworkManager.Singleton.spawnableObjects.FindIndex(x => x.GetComponent<NetworkObject>().NetworkObjectID == networkObject.NetworkObjectID);
            if (index == -1)
            {
                GameObject.Destroy(spawn);
                Debug.LogError("Tried to spawn a NetworkObject without a valid Guid. Make sure the GameObject is inside of the Network Manager Spawnable List!");
                throw new NetworkServerException("Tried to spawn a NetworkObject without a valid Guid.");
            }

            // Set the player object of the client to be the spawned object.
            NetworkServer.Connections.Find(x => x.ConnectionId == connectionId).PlayerObject = networkObject;

            // If we are in host mode and the number of clients connected is 1, we can assume that we own
            // the same ID. This is rather unsafe as technically, a user could connect before you if they are fast enough.
            // Host information should probably be sent to the transport layer and let the massign it instead.
            if (HostID == -1)
            {
                if (NetworkClient.IsHost)
                {
                    HostID = connectionId;
                }
            }

            networkObject.OnStart();

            string guidString = Guid.NewGuid().ToString();
            if (!gameObjectsOnNetwork.ContainsKey(guidString))
            {
                gameObjectsOnNetwork.Add(guidString, spawn);
            }
            else
            {
                Debug.LogError("Tried to spawn a GameObject on the network of the same instance more than once.");
                throw new NetworkServerException("Tried to spawn a GameObject on the network of the same instance more than once.");
            }
            NetworkObject spawnedNetworkObject = spawn.GetComponent<NetworkObject>();
            spawnedNetworkObject.NetworkObjectID = guidString;
            spawnedNetworkObject.NetworkOwnerID = connectionId;

            foreach (var client in NetworkServer.Connections)
            {
                // Index of spawned object
                // GuidString
                // HasAuthority
                // Position
                // Rotation
                // Am I host?
                NetworkMessage newPlayerRPCMessage = new NetworkMessage(MessageType.Connected);
                newPlayerRPCMessage.AddString("SpawnObject");

                dynamic[] playerRPCParameters = MessageSendHelper.CreateDynamicList(
                    index,
                    guidString,
                    client.ConnectionId == connectionId,
                    spawn.transform.position,
                    spawn.transform.rotation,
                    client.ConnectionId == HostID);
                int argumentCount = playerRPCParameters.Length / 2;
                newPlayerRPCMessage.AddInt(argumentCount);
                for (int i = 0; i < playerRPCParameters.Length; i++)
                {
                    newPlayerRPCMessage.Add(playerRPCParameters[i]);
                }
                Send(newPlayerRPCMessage, client.ConnectionId);
            }

            OnPlayerSpawned?.Invoke(connectionId);

            // Tell newly connected player to spawn all other players
            if (Connections.Count <= 1)
                return;

            dynamic[] playerObjects = new dynamic[(Connections.Count - 1) * 2];

            int dynaminIndex = 0;
            foreach(PlayerConnection connection in Connections)
            {
                // Skip if host
                if (connection.ConnectionId == connectionId)
                    continue;

                playerObjects[dynaminIndex] = connection.PlayerObject.NetworkObjectID;
                playerObjects[dynaminIndex + 1] = connection.PlayerObject.transform.position;
                dynaminIndex += 2;
            }
            dynamic[] parameters = MessageSendHelper.CreateDynamicList(playerObjects);
            NetworkMessage spawnRPCMessage = new NetworkMessage(MessageType.TargetRPC);
            spawnRPCMessage.AddString(spawn.GetComponent<NetworkObject>().NetworkObjectID.ToString());
            spawnRPCMessage.AddInt(spawn.GetComponent<NetworkObject>().DefaultNetworkBehaviour.BehaviorIndex);
            spawnRPCMessage.AddString("SpawnPlayerObjects");
            spawnRPCMessage.AddInt(parameters.Length / 2);
            foreach(dynamic p in parameters)
            {
                spawnRPCMessage.AddDynamic(p);
            }
            Send(spawnRPCMessage, connectionId);
            OnOtherPlayersSpawned?.Invoke(connectionId);
        }

        private static void ClientDisconnected(int connectionId)
        {
            // Destroy all objects owned by the client
            PlayerConnection playerConnection = Connections.Find(x => x.ConnectionId == connectionId);
            if (playerConnection == null)
                return;

            // Inform NetworkManager that the client has disconnected (before we destroy the object so that RPCs can be sent)
            NetworkManager.Singleton.ClientDisconnectedServer(connectionId);

            string[] objectIdsOwnedByClient = playerConnection.PlayerOwnedObjects.Keys.ToArray();
            dynamic[] parameters = MessageSendHelper.CreateDynamicList(objectIdsOwnedByClient);
            NetworkBehaviour playerBehaviour = playerConnection.PlayerObject.DefaultNetworkBehaviour;
            if (playerBehaviour == null)
            {
                Debug.LogError($"PlayerObject for connection {connectionId} tried to destroy objects but it does not contain a networkbehaviour. " +
                    $"Ensure to add it to the player prefab!");
                throw new NetworkServerException($"PlayerObject for connection {connectionId} tried to destroy objects but it does not contain a networkbehaviour. " +
                    $"Ensure to add it to the player prefab!");
            }
            if (parameters.Length >= 1)
            {
                if (!NetworkClient.IsHost)
                    methodsToProcess.Enqueue(new MethodToProcess(playerConnection.PlayerObject.NetworkObjectID, playerBehaviour.BehaviorIndex, "DestroyClientObjects", parameters));
                
                MessageSendHelper.SendNetworkRPC(playerBehaviour, "DestroyClientObjects", parameters);
            }else
            {
                if (!NetworkClient.IsHost) 
                    methodsToProcess.Enqueue(new MethodToProcess(playerConnection.PlayerObject.NetworkObjectID, playerBehaviour.BehaviorIndex, "DestroyClientPlayerObject", new object[0]));
               
                MessageSendHelper.SendNetworkRPC(playerBehaviour, "DestroyClientPlayerObject");
            }
        }

        public static void Send(NetworkMessage message, int connectionId = -1)
        {
            Transport.activeTransport.ServerSend(message, connectionId);
        }

        private static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            IsActive = false;

            // Clear connections
            Connections.Clear();
            Connections = new List<PlayerConnection>();

            // Reset Network time

            initialized = true;
        }
    }
}