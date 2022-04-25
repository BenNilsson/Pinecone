using UnityEngine;

namespace Pinecone
{
    public static class NetworkLoop
    {
        public static void Tick()
        {
            Transport.activeTransport?.Tick();
            NetworkServer.Tick();
        }
    }
}
