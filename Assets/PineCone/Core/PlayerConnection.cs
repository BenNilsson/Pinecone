using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pinecone
{
    public class PlayerConnection
    {
        /// <summary>
        /// Set for the server only in the Transport layer.
        /// </summary>
        public int ConnectionId;

        public NetworkObject PlayerObject;

        public Dictionary<string, NetworkObject> PlayerOwnedObjects;

        public bool IsHost;

        public PlayerConnection(int connectionId, bool isHost = false)
        {
            ConnectionId = connectionId;
            PlayerOwnedObjects = new Dictionary<string, NetworkObject>();
            IsHost = isHost;
        }
    }
}
