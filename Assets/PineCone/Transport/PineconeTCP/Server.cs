using Pinecone;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;

namespace PineconeTCP
{
    public class Server : Common
    {
        private TcpListener tcpListener;
        private Thread tcpListenerThread;
        private Thread sendThread;

        public bool IsActive => tcpListenerThread != null && tcpListenerThread.IsAlive;

        public int sendTimeout;
        public int receiveTimeout;

        /// <summary>
        /// Clients currently connected to the server.
        /// </summary>
        private ConcurrentDictionary<int, ConnectionState> clients = new ConcurrentDictionary<int, ConnectionState>();

        private int nextConnectionId;
        private int maxConnections = 0;

        public Action<int> OnClientConnected;
        public Action<int> OnClientDisconnected;

        public Server() 
        {

        }

        public bool Start(int port, int maxConnections = 0)
        {
            if (IsActive)
            {
                return false;
            }

            this.maxConnections = maxConnections;

            // Create and start a new thread for receiving connections & data from clients.
            tcpListenerThread = new Thread(() => Listen(port));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Priority = System.Threading.ThreadPriority.BelowNormal;
            tcpListenerThread.Start();

            // Create and start a new thread for sending data to clients.
            sendThread = new Thread(() => SendLoop());
            sendThread.IsBackground = true;
            sendThread.Start();

            return true;
        }

        public void Stop()
        {
            if (!IsActive)
            {
                return;
            }

            tcpListener?.Stop();
            tcpListenerThread?.Interrupt();
            tcpListenerThread?.Abort();
            tcpListenerThread = null;

            sendThread?.Interrupt();
            sendThread?.Abort();
            sendThread = null;

            // Disconnect all clients
            foreach(KeyValuePair<int, ConnectionState> connection in clients)
            {
                TcpClient client = connection.Value.tcpClient; 
                try
                {
                    client.Close();
                }catch { }
                client = null;
            }

            clients.Clear();

            nextConnectionId = 0;
        }

        /// <summary>
        /// Sends a given message to all clients.
        /// </summary>
        public void Send(NetworkMessage message)
        {
            foreach (var client in clients.Values)
            {
                client.MessagePool.Enqueue(message);
            }
        }

        /// <summary>
        /// Send a given message to a specific client.
        /// </summary>
        public void Send(NetworkMessage message, int connectionId)
        {
            // If -1, send to all
            if (connectionId == -1)
            {
                Send(message);
            }
            // Send to specific client.
            else if (connectionId > -1)
            {
                if (clients.TryGetValue(connectionId, out var client))
                {
                    client.MessagePool.Enqueue(message);
                }
            }
        }

        /// <summary>
        /// Disconnects a client
        /// </summary>
        public bool Disconnect(int connectionId)
        {
            if (clients.TryRemove(connectionId, out var client))
            {
                client.Release();
                UnityEngine.Debug.Log($"Disconnecting client with a connectionId of {connectionId}");
                methodsToProcess.Enqueue(new MethodToProcessDisconnected(connectionId));
                return true;
            }
            return false;
        }

        private void Listen(int port)
        {
            try
            {
                tcpListener = TcpListener.Create(port);
                tcpListener.Server.NoDelay = true;
                tcpListener.Start();

                UnityEngine.Debug.Log($"[PineconeTCP] Server: Listening to port: {port}");

                while (IsActive)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();

                    if (clients.Count >= this.maxConnections)
                    {
                        client.Close();
                        return;
                    }

                    client.NoDelay = true;

                    client.SendTimeout = sendTimeout;
                    client.ReceiveTimeout = receiveTimeout;

                    int connectionId = GetNextConnectionId();
                    ConnectionState connection = new ConnectionState(client);
                    connection.ConnectionID = connectionId;
                    clients[connectionId] = connection;

                    UnityEngine.Debug.Log($"[PineconeTCP] Client connected with a connectionId of {connectionId}");

                    // Create and start a new RECEIVE thread for this client
                    connection.ReceiveThread = new Thread(() => ReceiveLoop(connectionId, connection));
                    connection.ReceiveThread.IsBackground = true;
                    connection.ReceiveThread.Start();

                    methodsToProcess.Enqueue(new MethodToProcessSpawnPlayer("", new object[0], connectionId));
                }
            }
            catch (ThreadInterruptedException exception)
            {
                UnityEngine.Debug.Log($"Server thread interrupted. {exception}");
            }
            catch (ThreadAbortException exception)
            {
                UnityEngine.Debug.Log($"Server thread aborted. {exception}");
            }
            catch (SocketException exception)
            {
                UnityEngine.Debug.Log($"Server thread stopped. {exception}");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"Server Exception: {exception}");
            }
        }

        private int GetNextConnectionId()
        {
            int id = Interlocked.Increment(ref nextConnectionId);

            if (id == int.MaxValue)
            {
                throw new Exception($"Max Limit for connection ids reached: {id}");
            }

            return id;
        }

        private void ReceiveLoop(int connectionId, ConnectionState state)
        {
            NetworkStream networkStream = state.tcpClient.GetStream();
            BinaryReader reader = new BinaryReader(networkStream);
            while (IsActive)
            {
                if (!networkStream.CanRead || !networkStream.DataAvailable)
                    continue;

                NetworkMessage msg = Utils.ReceiveFunc(reader);
                HandleReceivedMessage(connectionId, msg);
            }
        }

        private void SendLoop()
        {
            NetworkStream networkStream;

            while (true)
            {
                foreach (var client in clients.Values)
                {
                    // Ignore if no messages are present.
                    if (client.MessagePool.Count <= 0)
                        continue;

                    // Try to get a message from the pool.
                    if (!client.MessagePool.TryDequeue(out NetworkMessage message))
                        continue;

                    try
                    {
                        networkStream = client.tcpClient.GetStream();
                        BinaryWriter writer = new BinaryWriter(networkStream);
                        Utils.SendFunc(writer, message);
                    }
                    catch (ObjectDisposedException _)
                    {
                        Disconnect(client.ConnectionID);
                    }
                    catch (SocketException _)
                    {
                        Disconnect(client.ConnectionID);
                    }
                }
            }
        }

        private void HandleReceivedMessage(int connectionId, NetworkMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Connected:
                    // Broadcast Connection
                    Send(message);
                    // Send newly connected client a message, telling them to spawn all network objects in the scene atm.
                    break;
                case MessageType.Disconnected:
                    // Broadcast Disconnection
                    Disconnect(connectionId);
                    Send(message);
                    break;
                case MessageType.RPC:
                    HandleCommand(message);
                    break;
            }
        }

        private void HandleCommand(NetworkMessage message)
        {
            // ObjectID, BehaviourIndex, MethodName, Number of Arguments, Argument Type Name, Argulent Value, .....
            string objectId = message.GetString();
            int behaviourIndex = message.GetInt();
            string methodName = message.GetString();
            int numberOfArgs = message.GetInt();
            object[] parameters = new object[numberOfArgs];

            for (int i = 0; i < numberOfArgs; i++)
            {
                string typeName = message.GetString();
                Type type = Type.GetType(typeName);
                parameters[i] = message.GetDynamic(FormatterServices.GetUninitializedObject(type));
            }

            methodsToProcess.Enqueue(new MethodToProcess(objectId, behaviourIndex, methodName, parameters));
        }

        private Queue<Methods> methodsToProcess = new Queue<Methods>();
        public override void Tick()
        {
            // Early out if server is not active or has no messages to process.
            if (!IsActive || methodsToProcess.Count <= 0)
                return;

            Methods method = methodsToProcess.Dequeue();

            if (method is MethodToProcess methodToProcess)
            {
                NetworkObject networkObject = GameObject.FindObjectsOfType<NetworkObject>().Where(x => x.NetworkObjectID == methodToProcess.objectId).FirstOrDefault();
                List<NetworkBehaviour> behaviours = networkObject?.NetworkBehaviours;
                NetworkBehaviour networkBehaviour = behaviours?.Find(x => x.BehaviorIndex == methodToProcess.behaviourIndex);

                if (networkBehaviour != null)
                {
                    void LogError()
                    {
                        Debug.LogError(
                            $"[Pinecone][Client] Error calling {methodToProcess.methodName} with {methodToProcess.parameters.Length} parameters on " +
                            $"BehaviorIndex: {networkBehaviour.BehaviorIndex} " +
                            $"Name: ({networkBehaviour.gameObject.name})");
                    }

                    try
                    {
                        System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public |
                                                                      System.Reflection.BindingFlags.NonPublic |
                                                                      System.Reflection.BindingFlags.Instance;
                        var reflectedMethod = networkBehaviour.GetType()
                            .GetMethod(methodToProcess.methodName, bindingFlags);
                        var param = reflectedMethod.GetParameters();

                        if (param.Count() == 1 && Attribute.IsDefined(param[0], typeof(ParamArrayAttribute)))
                        {
                            reflectedMethod.Invoke(networkBehaviour, new object[] { methodToProcess.parameters });
                        }
                        else
                        {
                            reflectedMethod.Invoke(networkBehaviour, methodToProcess.parameters);
                        }
                    }
                    catch (ArgumentException)
                    {
                        LogError();
                    }
                    catch (TargetParameterCountException)
                    {
                        LogError();
                    }
                    catch (MethodAccessException)
                    {
                        LogError();
                    }
                    catch (InvalidOperationException)
                    {
                        LogError();
                    }
                    catch (NotSupportedException)
                    {
                        LogError();
                    }
                }
                else
                {
                    Debug.LogError($"[Pinecone][Server] Error finding network behaviour for command {methodToProcess.methodName}");
                }
            }
            else if (method is MethodToProcessSpawnPlayer methodToProcessSpawn)
            {
                OnClientConnected?.Invoke(methodToProcessSpawn.connectionId);
            }
            else if (method is MethodToProcessDisconnected methodToProcessDisconnected)
            {
                OnClientDisconnected?.Invoke(methodToProcessDisconnected.connectionId);
            }
        }
    }
}