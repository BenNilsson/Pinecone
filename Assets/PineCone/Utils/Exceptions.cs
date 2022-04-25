using System;

namespace Pinecone
{
    public class NetworkClientException : Exception
    {
        public NetworkClientException(string message)
        {

        }
    }

    public class ClientAuthorityViolationException : Exception
    {
        public ClientAuthorityViolationException(string message)
        {

        }
    }

    public class NoNetworkConnectionException : Exception
    {
        public NoNetworkConnectionException(string message)
        {

        }
    }

    public class ClientAccessViolationException : Exception
    {
        public ClientAccessViolationException(string message)
        {

        }
    }

    public class NetworkServerException : Exception
    {
        public NetworkServerException(string message)
        {

        }
    }

    public class ServerAccessViolationException : Exception
    {
        public ServerAccessViolationException(string message)
        {

        }
    }
}