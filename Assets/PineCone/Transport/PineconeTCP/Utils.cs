using System;
using System.IO;
using Pinecone;

namespace PineconeTCP
{
    public static class Utils
    {
        public static void SendFunc(BinaryWriter writer, NetworkMessage message)
        {
            writer.Write(message.WrittenLength);
            byte[] buffer = new byte[message.WrittenLength];
            Buffer.BlockCopy(message.Buffer, 0, buffer, 0, message.WrittenLength);
            writer.Write(buffer);
            writer.Flush();
        }

        public static NetworkMessage ReceiveFunc(BinaryReader reader)
        {
            byte[] length = reader.ReadBytes(reader.ReadInt32());

            return new NetworkMessage(length, length.Length);
        }
    }
}