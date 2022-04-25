using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using Pinecone;

namespace PineconeTCP
{
    /// <summary>
    /// The server and client needs a ConnectionState.
    /// Server will need it to keep track of the connected clients.
    /// Clients will need it to create a new connection.
    /// </summary>
    public class ConnectionState
    {
        public TcpClient tcpClient;
        public Thread SendThread;
        public Thread ReceiveThread;

        public ConcurrentQueue<NetworkMessage> MessagePool;
        public int ConnectionID;

        public ConnectionState(TcpClient client)
        {
            tcpClient = client;
            MessagePool = new ConcurrentQueue<NetworkMessage>();
        }

        public void Release()
        {
            tcpClient?.Close();
            
            UnityEngine.Debug.Log($"[PineconeTCP] Client Closed.");
        }
    }
}