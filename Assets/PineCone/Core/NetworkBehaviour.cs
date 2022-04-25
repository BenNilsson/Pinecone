using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Linq;

namespace Pinecone
{
    [RequireComponent(typeof(NetworkObject))]
    
    public partial class NetworkBehaviour : MonoBehaviour, INotifyPropertyChanged
    {

        /// <summary>
        /// Returns the NetworkIdentifier of this object.
        /// </summary>
        public NetworkObject NetworkObject { get; internal set; }

        /// <summary>
        /// Returns the index of this behavior on this object.
        /// </summary>
        public int BehaviorIndex { get; set; }

        /// <summary>
        /// Whether or not the client has control over this.
        /// </summary>
        public bool HasAuthority { get; set; }


        public int OwningID => NetworkObject.NetworkOwnerID;


        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler OnSyncVarValueChanged;
        public void InvokeOnSyncVarValueChanged(string propertyName = "")
        {
            OnSyncVarValueChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        

        /// <summary>
        /// Called when the NetworkObject has been spawned successfully on the server.
        /// </summary>
        public virtual void OnStart()
        {

        }

        public virtual void SetupSyncVars()
        {

        }

        [NetworkTargetRPC]
        public void SpawnObject(int objectToSpawn, string guidString, bool hasAuthority, Vector3 position, Quaternion rotation, bool IsHost)
        {
            if (NetworkClient.spawnedObjects.ContainsKey(guidString))
            {
                Debug.LogError("Tried to spawn an instance of an network object on a client that already exists");
                throw new NetworkClientException("Tried to spawn an instance of an network object on a client that already exists");
            }

            NetworkObject networkObject;
            if (!IsHost)
                networkObject = Instantiate(NetworkManager.Singleton.spawnableObjects[objectToSpawn], position, rotation).GetComponent<NetworkObject>();
            else 
                networkObject = GameObject.FindObjectsOfType<NetworkObject>().Where(x => x.NetworkObjectID == guidString).FirstOrDefault();
            
            networkObject.HasAuthority = hasAuthority;
            networkObject.NetworkObjectID = guidString;

            if (networkObject.HasAuthority)
            {
                NetworkClient.spawnedObjects.Add(guidString, networkObject);
            }

            if (!NetworkClient.IsHost)
            {
                networkObject.OnStart();
            }
        }

        [NetworkRPC]
        public void DestroyGameObject(string objectIDToDestroy)
        {
            // Check if this is part of the client
            if (NetworkClient.spawnedObjects.ContainsKey(objectIDToDestroy))
            {
                NetworkClient.spawnedObjects.Remove(objectIDToDestroy);
            }

            if (NetworkObject.NetworkObjectID == objectIDToDestroy)
            {
                Destroy(gameObject);
            }
        }

        [NetworkRPC]
        public void DestroyClientObjects(bool isServer = false, int connectionId = -1, params string[] guids)
        {
            // Check if this is part of the client
            foreach (string objectIDToDestroy in guids)
            {
                if (NetworkClient.spawnedObjects.ContainsKey(objectIDToDestroy) && !isServer)
                {
                    Destroy(NetworkClient.spawnedObjects[objectIDToDestroy].gameObject);
                    NetworkClient.spawnedObjects.Remove(objectIDToDestroy);
                }

                if (isServer)
                {
                    PlayerConnection playerConnection = NetworkServer.Connections.Find(x => x.ConnectionId == connectionId);

                    Destroy(playerConnection.PlayerOwnedObjects[objectIDToDestroy].gameObject);
                }
            }
        }

        [NetworkRPC]
        public void SpawnPlayerObjects(params object[] spawnData)
        {
            if (spawnData.Length <= 0)
                return;

            int length = spawnData.Length;

            for (int i = 0; i < length; i += 2)
            {
                string objectId = (string)spawnData[i];
                Vector3 position = (Vector3)spawnData[i + 1];
                NetworkObject networkObject = Instantiate(NetworkManager.Singleton.spawnableObjects[NetworkManager.Singleton.playerGameObject], position, Quaternion.identity).GetComponent<NetworkObject>();
                networkObject.HasAuthority = false;
                networkObject.NetworkObjectID = objectId;

                if (!NetworkClient.IsHost)
                {
                    networkObject.OnStart();
                }
            }
        }

        [NetworkRPC]
        public void DestroyClientPlayerObject()
        {
            Destroy(gameObject);
        }
    }
}
