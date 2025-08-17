using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace UserNode
{
    public class ProxyV1Protocol
    {
        enum ParseStaus
        {
            ParsedOK = 0,
            ExceedMaxLength = 1,
            NeedWaitMore = 2,
        }
        private MemoryStream _receiveMemoryStream;

        public ProxyV1Protocol()
        {
            _receiveMemoryStream = new MemoryStream();
        }
        public ProxyV1Message parseMessage(byte[] receivedData)
        {
            int maximumLength = 256;
            int minimumLength = 20;
            try
            {
                _receiveMemoryStream.Seek(0, SeekOrigin.End);
                _receiveMemoryStream.Write(receivedData, 0, receivedData.Length);
                _receiveMemoryStream.Position = 0;

                if (_receiveMemoryStream.Length < minimumLength)
                    return new ProxyV1Message();

                int readByteCount = 0;
                byte previousData = 0;

                ParseStaus parseStatus = ParseStaus.NeedWaitMore;

                do
                {
                    if (_receiveMemoryStream.Length == 0)
                        break;

                    byte data = (byte)_receiveMemoryStream.ReadByte();
                    if (previousData == 13 && data == 10)
                    {
                        parseStatus = ParseStaus.ParsedOK;
                        break;
                    }

                    previousData = data;
                    readByteCount++;

                    if (readByteCount > maximumLength)
                    {
                        parseStatus = ParseStaus.ExceedMaxLength;
                        break;
                    }
                } while (true);

                if (parseStatus == ParseStaus.ExceedMaxLength)
                    return null;

                if (parseStatus == ParseStaus.NeedWaitMore)
                    return new ProxyV1Message();

                int byteCount = (int)_receiveMemoryStream.Position;
                _receiveMemoryStream.Position = 0;

                byte[] packetData = ReadByteArray(_receiveMemoryStream, byteCount);
                if (packetData == null)
                    return null;

                string strLine = Encoding.ASCII.GetString(packetData);
                string[] strParts = strLine.Split(new string[0], StringSplitOptions.RemoveEmptyEntries);

                if (strParts.Length != 6)
                    return null;

                ProxyV1Message message = new ProxyV1Message();
                message.ProxyString = strParts[0];
                message.InternetProtocol = strParts[1];
                message.ClientIP = IPAddress.Parse(strParts[2]);
                message.ProxyIP = IPAddress.Parse(strParts[3]);
                message.ClientPort = int.Parse(strParts[4]);
                message.ProxyPort = int.Parse(strParts[5]);
                message.IsMessageComposed = true;
                return message;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public byte[] getRemainData()
        {
            int length = (int)(_receiveMemoryStream.Length - _receiveMemoryStream.Position);
            if (length <= 0)
                return null;

            return ReadByteArray(_receiveMemoryStream, length);
        }

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
    }

    public class ProxyV1Message
    {

        public ProxyV1Message()
        {
            this.IsMessageComposed = false;
        }
        public bool IsMessageComposed { get; set; }
        public string ProxyString { get; set; }
        public string InternetProtocol { get; set; }
        public IPAddress ClientIP { get; set; }
        public IPAddress ProxyIP { get; set; }
        public int ClientPort { get; set; }
        public int ProxyPort { get; set; }
    }
}
