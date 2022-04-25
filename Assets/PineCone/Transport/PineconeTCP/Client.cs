using Pinecone;
using System;
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
    public class Client : Common
    {
        public Action OnClientConnected;
        public Action OnClientDisconnected;

        public int sendTimeout;
        public int receiveTimeout;

        private NetworkStream networkStream;
        private BinaryWriter writer;
        private BinaryReader reader;

        public Client()
        {

        }

        private ClientConnectionState state;

        public bool IsConnected => state != null && state.IsConnected;

        public void Connect(string ip, int port)
        {
            try
            {
                state = new ClientConnectionState(new TcpClient());

                state.tcpClient.Connect(ip, port);
                state.tcpClient.NoDelay = true;
                state.tcpClient.SendTimeout = sendTimeout;
                state.tcpClient.ReceiveTimeout = receiveTimeout;

                networkStream = state.tcpClient.GetStream();
                writer = new BinaryWriter(networkStream);
                reader = new BinaryReader(networkStream);

                Debug.Log($"[PineconeTCP] Connected Client to: {ip}:{port}");

                state.ReceiveThread = new Thread(ReceiveThread);
                state.ReceiveThread.IsBackground = true;
                state.ReceiveThread.Start();

                OnClientConnected?.Invoke();
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (SocketException exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                // Happens if the connection failed. Should send disconnect event
            }
            catch (ThreadInterruptedException exception)
            {
                // Safe. Happens when the thread is interrupted
                Debug.Log($"Client thread interrupted. {exception}");
            }
            catch (ThreadAbortException exception)
            {
                // Safe. Happens when the thread is interrupted
                Debug.Log($"Client thread aborted. {exception}");
            }
            catch (ObjectDisposedException exception)
            {
                // Safe. Happens when the thread is interrupted
                Debug.Log($"Client object disposed. Interrupting thread. {exception}");
            }
            catch (Exception exception)
            {
                // Error
                Debug.Log($"[PineconeTCP] Client Exception: {exception}");
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                try
                {
                    NetworkMessage msg = new NetworkMessage(MessageType.Disconnected);
                    Send(msg);
                }catch
                {

                }
            }

            OnClientDisconnected?.Invoke();
            state?.Release();
        }

        public void Send(NetworkMessage message)
        {
            try
            {
                Utils.SendFunc(writer, message);
            }

#pragma warning disable CS0168 // Variable is declared but never used
            catch (SocketException exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                // Happens if the connection failed. Should send disconnect event
                Disconnect();
            }
            catch (ThreadInterruptedException exception)
            {
                Debug.Log($"Client thread interrupted. {exception}");
                Disconnect();
            }
            catch (ThreadAbortException exception)
            {
                Debug.Log($"Client thread aborted. {exception}");
                Disconnect();
            }
            catch (ObjectDisposedException exception)
            {
                Debug.Log($"Client object disposed. Interrupting thread. {exception}");
                Disconnect();
            }
            catch (Exception exception)
            {
                // Error
                Debug.Log($"[PineconeTCP] Client Exception: {exception}");
                Disconnect();
            }
        }

        private Queue<Methods> methodsToProcess = new Queue<Methods>();
        public override void Tick()
        {
            // Early out if client is not connected.
            if (state == null || methodsToProcess.Count <= 0)
                return;

            var method = methodsToProcess.Dequeue();
            if (method is MethodToProcess methodToProcess)
            {
                NetworkObject networkObject = GameObject.FindObjectsOfType<NetworkObject>().FirstOrDefault(x => x.NetworkObjectID == methodToProcess.objectId);
                List<NetworkBehaviour> behaviours = networkObject?.NetworkBehaviours;
                NetworkBehaviour networkBehaviour = behaviours?.Find(x => x.BehaviorIndex == methodToProcess.behaviourIndex);

                if (networkBehaviour == null)
                {
                    Debug.LogError($"[Pinecone][Client] Error finding network behaviour for command {methodToProcess.methodName}");
                    return;
                }

                void LogError()
                {
                    Debug.LogError($"[Pinecone][Client] Error calling {methodToProcess.methodName} with {methodToProcess.parameters.Length} parameters on " +
                                   $"BehaviorIndex: {networkBehaviour.BehaviorIndex} " +
                                   $"Name: ({networkBehaviour.gameObject.name})");
                }

                try
                {
                    System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var reflectedMethod = networkBehaviour.GetType().GetMethod(methodToProcess.methodName, bindingFlags);
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
            else if (method is MethodToProcessSpawnPlayer methodToProcessSpawnPlayer)
            {
                typeof(NetworkClient).GetMethod("SpawnPlayerObject").Invoke(null, methodToProcessSpawnPlayer.parameters);
            }
            else if (method is MethodToProcessSyncVar methodToProcessSyncVar)
            {
                NetworkObject networkObject = GameObject.FindObjectsOfType<NetworkObject>()
                    .FirstOrDefault(x => x.NetworkObjectID == methodToProcessSyncVar.objectId);

                List<NetworkBehaviour> behaviours = networkObject?.NetworkBehaviours;
                NetworkBehaviour networkBehaviour = behaviours?.Find(x => x.BehaviorIndex == methodToProcessSyncVar.behaviourIndex);

                if (networkBehaviour == null)
                {
                    Debug.LogError($"[Pinecone][Client] Error finding network behaviour for SyncVar {methodToProcessSyncVar.variableName}");
                    return;
                }

                void LogError()
                {
                    Debug.LogError($"[Pinecone][Client] Error calling SyncRPC with {methodToProcessSyncVar.variableName} {methodToProcessSyncVar.currentValue}" +
                                   $"BehaviorIndex: {networkBehaviour.BehaviorIndex} " +
                                   $"Name: ({networkBehaviour.gameObject.name})");
                }

                try
                {
                    System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var reflectedField = networkBehaviour.GetType().GetField(methodToProcessSyncVar.variableName, bindingFlags);

                    reflectedField.SetValue(networkBehaviour, methodToProcessSyncVar.currentValue);
                    networkBehaviour.InvokeOnSyncVarValueChanged(methodToProcessSyncVar.variableName);
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
        }

        private void ReceiveThread()
        {
            try
            {
                while (true)
                {
                    if (!networkStream.CanRead || !networkStream.DataAvailable)
                        continue;

                    NetworkMessage msg = Utils.ReceiveFunc(reader);
                    HandleMessage(msg);
                }
#pragma warning disable CS0168 // Variable is declared but never used
            }
            catch(Exception exception)
#pragma warning restore CS0168 // Variable is declared but never used
            {
                // Something went wrong.
            }
        }

        private void HandleMessage(NetworkMessage message)
        {
            switch(message.MessageType)
            {
                case MessageType.TargetRPC:
                case MessageType.RPC:
                {
                        // ObjectID, BehaviourIndex, MethodName, Number of Arguments, Argument Type Name, Argument Value, .....
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
                    break;
                }

                case MessageType.Connected:
                {
                    // MethodName, Number of Arguments, Argument Type Name, Argument Value, .....
                    string methodName = message.GetString();
                    int numberOfArgs = message.GetInt();
                    object[] parameters = new object[numberOfArgs];

                    for (int i = 0; i < numberOfArgs; i++)
                    {
                        string typeName = message.GetString();
                        Type type = Type.GetType(typeName);
                        parameters[i] = message.GetDynamic(FormatterServices.GetUninitializedObject(type));
                    }

                    methodsToProcess.Enqueue(new MethodToProcessSpawnPlayer(methodName, parameters));
                    break;
                }
                case MessageType.SyncVar:
                {
                    // ObjectID, BehaviourIndex, Variable Name, Argument Type Name, Argument Value
                    string objectId = message.GetString();
                    int behaviourIndex = message.GetInt();
                    string variableName = message.GetString();

                    string typeName = message.GetString();
                    Type type = Type.GetType(typeName);
                    var value = message.GetDynamic(FormatterServices.GetUninitializedObject(type));

                    methodsToProcess.Enqueue(new MethodToProcessSyncVar(objectId, behaviourIndex, variableName, value));
                    break;
                }
            }     
        }
    }

    public class ClientConnectionState : ConnectionState
    {
        public Thread ClientThread;

        /// <summary>
        /// Is the client currently connected to a server?
        /// </summary>
        public bool IsConnected => tcpClient != null && tcpClient.Connected;

        public ClientConnectionState(TcpClient client) : base(client) { }
    }
}