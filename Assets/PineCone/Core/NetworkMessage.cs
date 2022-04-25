using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Pinecone
{
    public enum MessageType
    {
        Connected = 0,
        Disconnected = 1,
        RPC = 2,
        TargetRPC = 3,
        SyncVar = 4,
    }

    /// <summary>
    /// Provides functionality for Serializing and Deserializing data.
    /// </summary>
    public class NetworkMessage
    {
        /// <summary>
        /// The maximum amount of bytes that a message can send + 1 byte for the header.
        /// </summary>
        public int MaxMessageSize = 16 * 1024;

        /// <summary>
        /// The message's data.
        /// </summary>
        public byte[] Buffer;

        /// <summary>
        /// The length in bytes of the buffer array that is not read yet.
        /// </summary>
        public int UnreadLength => writePosition - readPosition;

        /// <summary>
        /// The length in bytes of the buffer array that has been written.
        /// </summary>
        public int WrittenLength => writePosition;

        /// <summary>
        /// The length in byte sof the buffer array that has not been written yet.
        /// </summary>
        public int UnWrittenLength => Buffer.Length - writePosition;

        /// <summary>
        /// The position in the buffer array that the next bytes will be written to.
        /// </summary>
        private int writePosition = 0;

        /// <summary>
        /// The position in the buffer array that the next bytes will be read from.
        /// </summary>
        private int readPosition = 0;

        /// <summary>
        /// The type of message that will be sent. Used for handling of messages.
        /// </summary>
        public MessageType MessageType;

        public NetworkMessage(MessageType messageType)
        {
            Buffer = new byte[MaxMessageSize];
            this.MessageType = messageType;
            AddInt((int)this.MessageType);
        }

        public NetworkMessage(byte[] buffer, int writtenPosition)
        {
            Buffer = buffer;
            writePosition = writtenPosition;
            int index = GetInt();
            MessageType = (MessageType)index;
        }

        #region Data Types

        public void AddDynamic(dynamic value) => Add(value);
        public dynamic GetDynamic(dynamic value) => Get(value);

        #region Byte
        public NetworkMessage AddByte(byte value) { return Add(value); }
        public NetworkMessage Add(byte value)
        {
            if (UnWrittenLength < 1)
            {
                // Not enough capacity to add the byte. Consider throwing exception
                return null;
            }

            Buffer[writePosition++] = value;
            return this;
        }
        public void Get(out byte value) { value = GetByte(); }
        public byte GetByte()
        {
            if (UnreadLength < 1)
            {
                // No bytes
                return 0;
            }
            return Buffer[readPosition++];
        }

        public NetworkMessage AddBytes(byte[] bytes) { return Add(bytes); }
        public NetworkMessage Add(byte[] bytes)
        {
            // TODO : Consider what happens when the byte array is too big.
            // Might be better to change the array to a ushort?

            if (UnWrittenLength < bytes.Length)
            {
                // Not enough capacity to add the bytes. Consider throwing exception
                return null;
            }
            Array.Copy(bytes, 0, Buffer, writePosition, bytes.Length);
            writePosition += (ushort)bytes.Length;
            return this;
        }
        public byte[] GetBytes(int amount)
        {
            byte[] array = new byte[amount];
            ReadBytes(amount, array);
            return array;
        }
        public void GetBytes(int amount, byte[] array, int start = 0)
        {
            if (start + amount > array.Length)
            {
                // Array is not long enough to fit the amount of bytes.
                return;
            }
            ReadBytes(amount, array, start);
        }

        private void ReadBytes(int amount, byte[] array, int start = 0)
        {
            if (UnreadLength < amount)
            {
                // Message does not contain enough unread bytes to read the byte array.
                amount = UnreadLength;
            }
            Array.Copy(Buffer, readPosition, array, start, amount);
            readPosition += (ushort)amount;
        }
        #endregion

        #region Bool - DONE
        public NetworkMessage AddBool(bool value) { return Add(value); }
        public NetworkMessage Add(bool value)
        {
            if (UnWrittenLength < sizeof(bool))
            {
                // Not enough capacity to contain the bool.
                return null;
            }
            Buffer[writePosition++] = (byte)(value ? 1 : 0);
            return this;
        }
        public bool Get(bool value) { return GetBool(); }
        public bool GetBool()
        {
            if (UnreadLength < sizeof(bool))
            {
                // Buffer did not contain the bytes for value of int.
                return false;
            }

            // Bools are bytes so we just return the byte at the position we need
            return Buffer[readPosition++] == 1;
        }

        #endregion

        #region Int - DONE
        public NetworkMessage AddInt(int value) { return Add(value); }
        public NetworkMessage Add(int value)
        {
            if (UnWrittenLength < sizeof(int))
            {
                // Not enough capacity to contain the int.
                return null;
            }
            Array.Copy(BitConverter.GetBytes(value), 0, Buffer, writePosition, sizeof(int));
            writePosition += sizeof(int);
            return this;
        }
        public int Get(int value) { return GetInt(); }
        public int GetInt()
        {
            int value = 0;
            if (UnreadLength < sizeof(int))
            {
                // Buffer did not contain the bytes for value of int.
                return value;
            }
            value = BitConverter.ToInt32(Buffer, readPosition);
            readPosition += sizeof(int);
            return value;
        }
        #endregion

        #region Short - DONE
        public NetworkMessage AddShort(short value) { return Add(value); }
        public NetworkMessage Add(short value)
        {
            if (UnWrittenLength < sizeof(short))
            {
                // Not enough capacity to contain the int.
                return null;
            }
            Array.Copy(BitConverter.GetBytes(value), 0, Buffer, writePosition, sizeof(short));
            writePosition += sizeof(short);
            return this;
        }
        public short Get(short value) { return GetShort(); }
        public short GetShort()
        {
            short value = 0;
            if (UnreadLength < sizeof(short))
            {
                // Buffer did not contain the bytes for value of int.
                return value;
            }
            value = BitConverter.ToInt16(Buffer, readPosition);
            readPosition += sizeof(short);
            return value;
        }
        #endregion

        #region Strings - DONE
        public NetworkMessage AddString(string value) => Add(value);
        public NetworkMessage Add(string value)
        {
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            if (UnWrittenLength < sizeof(int) + stringBytes.Length)
            {
                // Not enough capacity to contain the length of string.
                return null;
            }
            Add(stringBytes.Length); // Add length of string in bytes to the message.
            Add(stringBytes);

            return this;
        }
        public string Get(string value) { return GetString(); }
        public string GetString()
        {
            int length = GetInt();
            if (UnreadLength < length)
            {
                // Buffer did not contain the bytes for value of string.
                return string.Empty;
            }

            string value = Encoding.UTF8.GetString(GetBytes(length), 0, length);
            return value;
        }

        #endregion

        #region Floats - DONE
        public NetworkMessage AddFloat(float value) => Add(value);
        public NetworkMessage Add(float value)
        {
            if (UnWrittenLength < sizeof(float))
            {
                // Not enough capacity to contain the length of float.
                return null;
            }

            Array.Copy(BitConverter.GetBytes(value), 0, Buffer, writePosition, sizeof(float));
            writePosition += sizeof(float);
            return this;
        }
        public float Get(float value) { return GetFloat(); }
        public float GetFloat()
        {
            if (UnreadLength < sizeof(float))
            {
                // Buffer did not contain the bytes for value of float.
                return 0;
            }
            float value = BitConverter.ToSingle(Buffer, readPosition);
            readPosition += sizeof(float);
            return value;
        }

        #endregion

        #region Double - DONE
        public NetworkMessage AddDouble(double value) => Add(value);
        public NetworkMessage Add(double value)
        {
            if (UnWrittenLength < sizeof(double))
            {
                // Not enough capacity to contain the length of double.
                return null;
            }

            Array.Copy(BitConverter.GetBytes(value), 0, Buffer, writePosition, sizeof(double));
            writePosition += sizeof(double);
            return this;
        }
        public double Get(double value) { return GetDouble(); }
        public double GetDouble()
        {
            if (UnreadLength < sizeof(double))
            {
                // Buffer did not contain the bytes for value of double.
                return 0;
            }
            double value = BitConverter.ToDouble(Buffer, readPosition);
            readPosition += sizeof(double);
            return value;
        }

        #endregion

        // =============== UNITY TYPES

        #region Vector3 - DONE
        public NetworkMessage AddVector3(Vector3 value) => Add(value);
        public NetworkMessage Add(Vector3 value)
        {
            if (UnreadLength < sizeof(float) * 3)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return null;
            }

            AddFloat(value.x);
            AddFloat(value.y);
            AddFloat(value.z);
            return this;
        }
        public Vector3 Get(Vector3 value) { return GetVector3(); }
        public Vector3 GetVector3()
        {
            if (UnreadLength < sizeof(float) * 3)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return Vector3.zero;
            }
            Vector3 vector = new Vector3();
            vector.x = GetFloat();
            vector.y = GetFloat();
            vector.z = GetFloat();
            return vector;
        }
        #endregion

        #region Vector2 - DONE
        public NetworkMessage AddVector2(Vector2 value) => Add(value);
        public NetworkMessage Add(Vector2 value)
        {
            if (UnreadLength < sizeof(float) * 2)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return null;
            }

            AddFloat(value.x);
            AddFloat(value.y);
            return this;
        }
        public Vector3 Get(Vector2 value) { return GetVector2(); }
        public Vector3 GetVector2()
        {
            if (UnreadLength < sizeof(float) * 2)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return Vector3.zero;
            }
            Vector3 vector = new Vector2();
            vector.x = GetFloat();
            vector.y = GetFloat();
            return vector;
        }
        #endregion

        #region Quaternion - DONE
        public NetworkMessage AddQuaternion(Quaternion value) => Add(value);
        public NetworkMessage Add(Quaternion value)
        {
            if (UnreadLength < sizeof(float) * 4)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return null;
            }

            AddFloat(value.x);
            AddFloat(value.y);
            AddFloat(value.z);
            AddFloat(value.w);
            return this;
        }
        public Quaternion Get(Quaternion value) { return GetQuaternion(); }
        public Quaternion GetQuaternion()
        {
            if (UnreadLength < sizeof(float) * 4)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return Quaternion.identity;
            }
            Quaternion quaternion = new Quaternion();
            quaternion.x = GetFloat();
            quaternion.y = GetFloat();
            quaternion.z = GetFloat();
            quaternion.w = GetFloat();
            return quaternion;
        }
        #endregion

        #region Color - DONE
        public NetworkMessage AddColor(Color32 value) => Add(value);
        public NetworkMessage Add(Color32 value)
        {
            if (UnreadLength < sizeof(byte) * 4)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return null;
            }

            AddByte(value.r);
            AddByte(value.g);
            AddByte(value.b);
            AddByte(value.a);
            return this;
        }
        public Color32 Get(Color32 value) { return GetColor(); }
        public Color32 GetColor()
        {
            if (UnreadLength < sizeof(byte) * 4)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return new Color32(0,0,0,0);
            }
            Color32 color = new Color32();
            color.r = GetByte();
            color.g = GetByte();
            color.b = GetByte();
            color.a = GetByte();
            return color;
        }
        #endregion

        #region Custom Animation Info - DONE
        public NetworkMessage AddAnimInfo(AnimationInfo value) => Add(value);
        public NetworkMessage Add(AnimationInfo value)
        {
            // Probably not the correct size of the struct lol
            int size = value.bools.Count * sizeof(bool);
            size += value.floats.Count * sizeof(float);

            if (UnreadLength < size)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return null;
            }

            AddInt(value.bools.Count);
            foreach (var (key, bValue) in value.bools)
            {
                AddInt(key);
                AddBool(bValue);
            }
            AddInt(value.floats.Count);
            foreach (var (key, fValue) in value.floats)
            {
                AddInt(key);
                AddFloat(fValue);
            }

            return this;
        }
        public AnimationInfo Get(AnimationInfo value) { return GetAnimInfo(); }
        public AnimationInfo GetAnimInfo()
        {
            // VERY UNSAFE. We are unsure of the size and cannot receive it.

            AnimationInfo animInfo = new AnimationInfo(new Dictionary<int, bool>(), new Dictionary<int, float>());
            if (UnreadLength < 1)
            {
                // Buffer did not contain the bytes for value of Vector3.
                return animInfo;
            }

            int numOfBools = GetInt();
            if (UnreadLength < numOfBools * sizeof(bool))
            {
                // Buffer did not contain the bytes for value of Vector3.
                return animInfo;
            }

            for (int i = 0; i < numOfBools; i++)
            {
                animInfo.bools.Add(GetInt(), GetBool());
            }

            int numOfFloats = GetInt();
            if (UnreadLength < numOfFloats * sizeof(float))
            {
                // Buffer did not contain the bytes for value of Vector3.
                return new AnimationInfo();
            }

            for (int i = 0; i < numOfFloats; i++)
            {
                animInfo.floats.Add(GetInt(), GetFloat());
            }
            return animInfo;
        }
        #endregion

        #endregion

    }
}
