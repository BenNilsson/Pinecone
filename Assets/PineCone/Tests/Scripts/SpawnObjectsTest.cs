using UnityEngine;
using Pinecone;

namespace Test2
{

    public partial class SpawnObjectsTest : NetworkBehaviour
    {
        
        private int myVar;

        public GameObject[] gameObjects;

        public void SpawnGameObject(int index)
        {
            HasAuthority = true;
            Generated.CommandSpawnPlayerObject(this, index);
        }

        [NetworkCommand]
        public void CommandSpawnPlayerObject(int index)
        {
            GameObject go = Instantiate(gameObjects[index], new Vector3(4,0,0), Quaternion.identity);
            NetworkServer.Spawn(go, this);
        }
    }
}