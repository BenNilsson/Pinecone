using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Pinecone
{
    public static class MessageSendHelper
    {
        public static dynamic[] CreateDynamicList(params dynamic[] parameters)
        {
            dynamic[] dynamicList = new dynamic[parameters.Length * 2];
            for (int i = 0; i < parameters.Length * 2; i+=2)
            {
                if (parameters[i / 2] == null)
                    continue;

                dynamicList[i] = parameters[i / 2].GetType().AssemblyQualifiedName;
                dynamicList[i + 1] = parameters[i / 2];
            }
            return dynamicList;
        }

        public static void SendNetworkRPC(NetworkBehaviour networkBehaviour, string methodName, params dynamic[] list)
        {
            // ObjectID, BehaviourIndex, MethodName, Number of Arguments, Argument Type Name, Argulent Value, .....
            NetworkMessage message = new NetworkMessage(MessageType.RPC);
            message.AddString(networkBehaviour.NetworkObject.NetworkObjectID.ToString());
            message.AddInt(networkBehaviour.BehaviorIndex);
            message.AddString(methodName);

            int argumentCount = list.Length / 2;
            message.AddInt(argumentCount);

           for(int i = 0; i < list.Length; i+=2)
            {
                message.AddString((string)list[i]);
                message.AddDynamic(list[i + 1]);
            }
            NetworkServer.Send(message);
        }

        public static void SendNetworkTargetRPC(NetworkBehaviour networkBehaviour, NetworkBehaviour behaviourToSendTo, string methodName, params dynamic[] list)
        {
            // ObjectID, BehaviourIndex, ConnectionID, MethodName, Number of Arguments, Argument Type Name, Argulent Value, .....
            NetworkMessage message = new NetworkMessage(MessageType.TargetRPC);
            message.AddString(networkBehaviour.NetworkObject.NetworkObjectID.ToString());
            message.AddInt(networkBehaviour.BehaviorIndex);
            message.AddString(methodName);

            int argumentCount = list.Length / 2;
            message.AddInt(argumentCount);

            for (int i = 0; i < list.Length; i += 2)
            {
                message.AddString((string)list[i]);
                message.AddDynamic(list[i + 1]);
            }
            NetworkServer.Send(message, behaviourToSendTo.OwningID);
        }

        public static void SendNetworkCommand(NetworkBehaviour networkBehaviour, string methodName, params dynamic[] list)
        {
            // ObjectID, BehaviourIndex, MethodName, Number of Arguments, Argument Type Name, Argulent Value, .....
            NetworkMessage message = new NetworkMessage(MessageType.RPC);
            message.AddString(networkBehaviour.NetworkObject.NetworkObjectID.ToString());
            message.AddInt(networkBehaviour.BehaviorIndex);
            message.AddString(methodName);

            int argumentCount = list.Length / 2;
            message.AddInt(argumentCount);

            for (int i = 0; i < list.Length; i += 2)
            {
                message.AddString((string)list[i]);
                message.AddDynamic(list[i + 1]);
            }
            NetworkClient.Send(message);
        }

        public static void SendNetworkSyncVar(NetworkBehaviour networkBehaviour, string variableName)
        {
            var type = networkBehaviour.GetType();

            var fields = type.GetFields(BindingFlags.Public |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Instance);

            variableName = variableName.Replace("Generated", "");

            var fieldInfo = fields.FirstOrDefault(field => field.Name == variableName);

            if (fieldInfo == null) return;
            var value = fieldInfo.GetValue(networkBehaviour);

            // ObjectID, BehaviourIndex, Variable Name, Argument Type Name, Argument Value
            var message = new NetworkMessage(MessageType.SyncVar);
            message.AddString(networkBehaviour.NetworkObject.NetworkObjectID);
            message.AddInt(networkBehaviour.BehaviorIndex);
            message.AddString(variableName);

            var parameters = CreateDynamicList(value);
            foreach (var parameter in parameters)
            {
                message.Add(parameter);
            }

            NetworkServer.Send(message);
        }
    }
}
