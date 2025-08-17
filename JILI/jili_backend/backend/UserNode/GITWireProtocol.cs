using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GITProtocol;

namespace UserNode
{
    public class GITWireProtocol
    {
        #region Private fields

        /// <summary>
        /// Maximum length of a message.
        /// </summary>
        private const int MaxMessageLength = 256 * 1024; //128 Kilo bytes.

        /// <summary>
        /// This MemoryStream object is used to collect receiving bytes to build messages.
        /// </summary>
        private MemoryStream _receiveMemoryStream;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of BinarySerializationProtocol.
        /// </summary>
        public GITWireProtocol()
        {
            _receiveMemoryStream = new MemoryStream();
        }

        #endregion

        #region IScsWireProtocol implementation

        /// <summary>
        /// Serializes a message to a byte array to send to remote application.
        /// This method is synchronized. So, only one thread can call it concurrently.
        /// </summary>
        /// <param name="message">Message to be serialized</param>
        /// <exception cref="CommunicationException">Throws CommunicationException if message is bigger than maximum allowed message length.</exception>
        public byte[] GetBytes(GITMessage XE888Message)
        {
            //Serialize the message to a byte array
            var serializedMessage = XE888Message.GetPacketData();

            //Check for message length
            var messageLength = serializedMessage.Length;
            if (messageLength > MaxMessageLength)
            {
                throw new Exception("Message is too big (" + messageLength + " bytes). Max allowed length is " + MaxMessageLength + " bytes.");
            }

            //Create a byte array including the length of the message (4 bytes) and serialized message content
            var bytes = new byte[messageLength + 4];
            WriteInt32(bytes, 0, messageLength);
            Array.Copy(serializedMessage, 0, bytes, 4, messageLength);

            //Return serialized message by this protocol
            return bytes;
        }

        /// <summary>
        /// Builds messages from a byte array that is received from remote application.
        /// The Byte array may contain just a part of a message, the protocol must
        /// cumulate bytes to build messages.
        /// This method is synchronized. So, only one thread can call it concurrently.
        /// </summary>
        /// <param name="receivedBytes">Received bytes from remote application</param>
        /// <returns>
        /// List of messages.
        /// Protocol can generate more than one message from a byte array.
        /// Also, if received bytes are not sufficient to build a message, the protocol
        /// may return an empty list (and save bytes to combine with next method call).
        /// </returns>
        public IEnumerable<GITMessage> CreateMessages(byte[] receivedBytes)
        {
            //Write all received bytes to the _receiveMemoryStream
            _receiveMemoryStream.Seek(0, SeekOrigin.End);
            _receiveMemoryStream.Write(receivedBytes, 0, receivedBytes.Length);

            //Create a list to collect messages
            var messages = new List<GITMessage>();

            bool isMalformed = false;
            //Read all available messages and add to messages collection
            while (ReadSingleMessage(messages, out isMalformed))
            {

            }

            if (isMalformed)
                return null;

            //Return message list
            return messages;
        }

        /// <summary>
        /// This method is called when connection with remote application is reset (connection is renewing or first connecting).
        /// So, wire protocol must reset itself.
        /// </summary>
        public void Reset()
        {
            if (_receiveMemoryStream.Length > 0)
            {
                _receiveMemoryStream = new MemoryStream();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// This method tries to read a single message and add to the messages collection. 
        /// </summary>
        /// <param name="messages">Messages collection to collect messages</param>
        /// <returns>
        /// Returns a boolean value indicates that if there is a need to re-call this method.
        /// </returns>
        /// <exception cref="CommunicationException">Throws CommunicationException if message is bigger than maximum allowed message length.</exception>
        private bool ReadSingleMessage(ICollection<GITMessage> messages, out bool isMalformed)
        {
            isMalformed = false;

            //Go to the begining of the stream
            _receiveMemoryStream.Position = 0;

            int headerLen = GITMessage.SIGNATURE_LEN + GITMessage.MSGCODE_LEN + GITMessage.BODY_LEN;

            //If stream has less than header length, that means we can not even read length of the message
            //So, return false to wait more bytes from remore application.
            if (_receiveMemoryStream.Length < headerLen)
                return false;

            do
            {
                byte signature1 = ReadByte(_receiveMemoryStream);
                if (signature1 != GITMessage.SIGNATURE1)
                {
                    isMalformed = true;
                    break;
                }
                byte signature2 = ReadByte(_receiveMemoryStream);
                if (signature2 != GITMessage.SIGNATURE2)
                {
                    isMalformed = true;
                    break;
                }

                int msgCode     = ReadUShort(_receiveMemoryStream);
                int bodyLen     = ReadInt32(_receiveMemoryStream);

                if (_receiveMemoryStream.Length < (headerLen + bodyLen))
                    return false;

                if (bodyLen < MaxMessageLength)
                {
                    var serializedMessageBytes = ReadByteArray(_receiveMemoryStream, bodyLen);
                    GITMessage message = GITMessage.ParseMessage(msgCode, serializedMessageBytes);
                    if(message != null)
                        messages.Add(message);
                }
            } while (false);

            if(!isMalformed)
            {
                //Read remaining bytes to an array
                var remainingBytes = ReadByteArray(_receiveMemoryStream, (int)(_receiveMemoryStream.Length - _receiveMemoryStream.Position));

                //Re-create the receive memory stream and write remaining bytes
                _receiveMemoryStream = new MemoryStream();
                _receiveMemoryStream.Write(remainingBytes, 0, remainingBytes.Length);
                _receiveMemoryStream.Seek(0, SeekOrigin.Begin);

                //Return true to re-call this method to try to read next message
                return (remainingBytes.Length >= headerLen);
            }
            else
            {
                _receiveMemoryStream = new MemoryStream();
                return false;
            }
        }

        /// <summary>
        /// Writes a int value to a byte array from a starting index.
        /// </summary>
        /// <param name="buffer">Byte array to write int value</param>
        /// <param name="startIndex">Start index of byte array to write</param>
        /// <param name="number">An integer value to write</param>
        private static void WriteInt32(byte[] buffer, int startIndex, int number)
        {
            buffer[startIndex] = (byte)((number >> 24) & 0xFF);
            buffer[startIndex + 1] = (byte)((number >> 16) & 0xFF);
            buffer[startIndex + 2] = (byte)((number >> 8) & 0xFF);
            buffer[startIndex + 3] = (byte)((number) & 0xFF);
        }

        /// <summary>
        /// Deserializes and returns a serialized integer.
        /// </summary>
        /// <returns>Deserialized integer</returns>
        private static int ReadInt32(Stream stream)
        {
            var buffer = ReadByteArray(stream, 4);
            return ((buffer[3] << 24) |
                    (buffer[2] << 16) |
                    (buffer[1] << 8) |
                    (buffer[0])
                   );
        }

        private static byte ReadByte(Stream stream)
        {
            var buffer = ReadByteArray(stream, 1);
            return buffer[0];
        }

        private static ushort ReadUShort(Stream stream)
        {
            var buffer = ReadByteArray(stream, 2);
            return (ushort)((buffer[1] << 8) | (buffer[0]));
        }

        /// <summary>
        /// Reads a byte array with specified length.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="length">Length of the byte array to read</param>
        /// <returns>Read byte array</returns>
        /// <exception cref="EndOfStreamException">Throws EndOfStreamException if can not read from stream.</exception>
        private static byte[] ReadByteArray(Stream stream, int length)
        {
            var buffer = new byte[length];
            var totalRead = 0;
            while (totalRead < length)
            {
                var read = stream.Read(buffer, totalRead, length - totalRead);
                if (read <= 0)
                {
                    throw new EndOfStreamException("Can not read from stream! Input stream is closed.");
                }

                totalRead += read;
            }

            return buffer;
        }

        #endregion
        
    }
}
