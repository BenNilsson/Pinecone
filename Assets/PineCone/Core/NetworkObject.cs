using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pinecone
{
    [AddComponentMenu("Networking/" + nameof(NetworkObject))]
    [DisallowMultipleComponent]
    public sealed class NetworkObject : MonoBehaviour
    {
        [SerializeField]
        private List<NetworkBehaviour> _networkBehaviours = new List<NetworkBehaviour>();
        public List<NetworkBehaviour> NetworkBehaviours => _networkBehaviours;

        [ShowInDebugOnly]
        public string NetworkObjectID = Guid.Empty.ToString();

        public bool HasAuthority { get; set; }

        /// <summary>
        /// The Owner ID of the client. This is only set for the server and is always -1 on the client.
        /// </summary>
        public int NetworkOwnerID { get; set; } = -1;

        public NetworkBehaviour DefaultNetworkBehaviour;


        /// <summary>
        /// Every NetworkIdentity spawned by this connection.
        /// </summary>
        public static Dictionary<uint, NetworkObject> spawnedObjects = new Dictionary<uint, NetworkObject>();

        /// <summary>
        /// Called when the NetworkObject has been spawned successfully on the server.
        /// </summary>
        public void OnStart()
        {
            RebuildBehaviourIndexes();

            foreach (var networkBehaviour in NetworkBehaviours)
            {
                networkBehaviour.SetupSyncVars();
            }
        }

        private void Awake()
        {
            // Disallow child objects to contain this. All NetworkObjects must exist on the root of an object.
            Transform start = transform.root;
            if (start != null && start != transform)
            {
                NetworkObject parentNetworkObject = start.GetComponentInParent<NetworkObject>();
                if (parentNetworkObject != null)
                {
                    Debug.LogWarning($"NetworkObject removed from {gameObject.name}. NetworkObjects must only exist on root objects.");
                    DestroyImmediate(this);
                }
            }

            // Check if a NetworkBehaviour is present.
            DefaultNetworkBehaviour = GetComponent<NetworkBehaviour>();
            if (DefaultNetworkBehaviour == null)
            {
                Debug.LogWarning("NetworkObject was spawned with no NetworkBehaviour. " +
                    "Adding default one. Make sure to add at least one NetworkBehaviour on this object.");
                DefaultNetworkBehaviour = gameObject.AddComponent<NetworkBehaviour>();
            }
        }

        private void RebuildBehaviourIndexes()
        {
            _networkBehaviours.Clear();
            NetworkBehaviour[] networkBehaviours = gameObject.transform.GetComponentsInChildren<NetworkBehaviour>().OrderBy(x => x.gameObject.name).ToArray();
            int id = 0;

            foreach (var behaviour in networkBehaviours)
            {
                _networkBehaviours.Insert(id, behaviour);
                behaviour.NetworkObject = this;
                behaviour.BehaviorIndex = id++;
                behaviour.HasAuthority = HasAuthority;
                behaviour.OnStart();
            }
        }
    }
}