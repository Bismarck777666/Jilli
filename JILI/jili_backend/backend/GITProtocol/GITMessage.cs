using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/****
 * 
 * 
 *          Created by Foresight(2021.03.03)
 * 
 */

namespace GITProtocol
{
    public class GITMessage
    {
        public const byte SIGNATURE1    = 0x88;
        public const byte SIGNATURE2    = 0x19;
        public const int  MAX_DATANUM   = 1024;
        public const int  SIGNATURE_LEN = 2;
        public const int  MSGCODE_LEN   = 2;
        public const int  BODY_LEN      = 4;
        public const int  METANUM_LEN   = 1;

        public List<object> DataArray
        {
            get;
            private set;
        }

        public List<TypeCode> DataTypes
        {
            get;
            private set;
        }
        public int DataNum
        {
            get
            {
                return DataArray.Count;
            }
        }
        public bool FreeSpinPlayLater { get; set; }

        public ushort MsgCode
        {
            get;
            private set;
        }

        public GITMessage(ushort msgCode)
        {
            this.MsgCode    = msgCode;
            this.DataArray  = new List<object>();
            this.DataTypes  = new List<TypeCode>();
        }

        public static GITMessage ParseMessage(int msgCode, byte[] netdata)
        {
            BinaryReader binReader = new BinaryReader(new MemoryStream(netdata));
            try
            {
                GITMessage message = new GITMessage((ushort)msgCode);
                int nDataCount = binReader.ReadInt16();
                List<int> dataTypes = new List<int>();
                for (int i = 0; i < nDataCount; i++)
                {
                    int nMetaDataInfo = binReader.ReadInt16();
                    dataTypes.Add(nMetaDataInfo & 0xFF);
                }
                message.DataArray.Clear();
                message.DataTypes.Clear();

                for (int i = 0; i < dataTypes.Count; i++)
                {
                    switch (dataTypes[i])
                    {
                        case 0:
                            UInt32 uintData = binReader.ReadUInt32();
                            message.DataArray.Add(uintData);
                            message.DataTypes.Add(TypeCode.UInt32);
                            break;
                        case 1:
                            UInt16 strLen = binReader.ReadUInt16();
                            string strData = Encoding.ASCII.GetString(binReader.ReadBytes(strLen));
                            message.DataArray.Add(strData);
                            message.DataTypes.Add(TypeCode.String);
                            break;
                        case 2:
                            Int32 intData = binReader.ReadInt32();
                            message.DataArray.Add(intData);
                            message.DataTypes.Add(TypeCode.Int32);
                            break;
                        case 3:
                            UInt16 ushortData = binReader.ReadUInt16();
                            message.DataArray.Add(ushortData);
                            message.DataTypes.Add(TypeCode.UInt16);
                            break;
                        case 4:
                            Int16 shortData = binReader.ReadInt16();
                            message.DataArray.Add(shortData);
                            message.DataTypes.Add(TypeCode.Int16);
                            break;
                        case 5:
                            float floatData = binReader.ReadSingle();
                            message.DataArray.Add(floatData);
                            message.DataTypes.Add(TypeCode.Single);
                            break;
                        case 6:
                            double doubleData = binReader.ReadDouble();
                            message.DataArray.Add(doubleData);
                            message.DataTypes.Add(TypeCode.Double);

                            break;
                        case 7:
                            Boolean booleanData = binReader.ReadBoolean();
                            message.DataArray.Add(booleanData);
                            message.DataTypes.Add(TypeCode.Boolean);

                            break;
                        case 8:
                            Byte byteData = binReader.ReadByte();
                            message.DataArray.Add(byteData);
                            message.DataTypes.Add(TypeCode.Byte);
                            break;
                        case 9:
                            long longData = binReader.ReadInt64();
                            message.DataArray.Add(longData);
                            message.DataTypes.Add(TypeCode.Int64);
                            break;
                    }
                }
                return message;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Append(object value)
        {
            DataArray.Add(value);
            TypeCode tcode = Type.GetTypeCode(value.GetType());
            DataTypes.Add(tcode);
        }

        public void ClearData()
        {
            DataArray.Clear();
            DataTypes.Clear();
        }

        public object GetData(int i)
        {
            if (i >= this.DataNum)
            {
                throw (new Exception("index exceeds limit."));
            }
            return DataArray[i];
        }
        public object Pop()
        {
            if (this.DataNum == 0)
                throw new Exception("There are no data to pop");

            object obj = GetDataObject(0);

            DataArray.RemoveAt(0);
            DataTypes.RemoveAt(0);
            return obj;
        }

        protected object GetDataObject(int index)
        {
            object obj = (object)DataArray[index];
            if (DataTypes[index] == TypeCode.Byte)
                obj = Convert.ToByte(obj);
            else if (DataTypes[index] == TypeCode.UInt16)
                obj = Convert.ToUInt16(obj);
            else if (DataTypes[index] == TypeCode.Int64)
                obj = Convert.ToInt64(obj);

            return obj;
        }
        public object Pop(int i)
        {
            if (i >= this.DataNum)
            {
                throw new Exception("index exceeds limit.");
            }

            object obj = GetDataObject(i);
            DataArray.RemoveAt(i);
            DataTypes.RemoveAt(i);
            return obj;
        }
        public object PopLast()
        {
            int i = this.DataNum - 1;
            return Pop(i);
        }
        public byte[] GetPacketData()
        {
            byte[] buff;
            MemoryStream mstream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(mstream);

            writer.Write(SIGNATURE1);
            writer.Write(SIGNATURE2);
            writer.Write((ushort)MsgCode);

            int nBodyLen = 0;
            byte[] metaData = GetMetaDataBytes(out nBodyLen);
            nBodyLen += (metaData.Length + 2);

            writer.Write(nBodyLen);
            writer.Write((ushort)DataArray.Count);
            writer.Write(metaData);
            for (int i = 0; i < DataArray.Count; i++)
            {
                WriteObject(DataArray[i], DataTypes[i], writer);
            }
            buff = new byte[mstream.Length];
            mstream.Seek(0, SeekOrigin.Begin);
            mstream.Read(buff, 0, buff.Length);
            writer.Close();
            return buff;
        }
        public void ResetData()
        {
            DataArray.Clear();
        }

        private byte[] GetMetaDataBytes(out int bodyLength)
        {
            List<ushort> metaData = new List<ushort>();
            bodyLength = 0;
            for (int i = 0; i < DataArray.Count; i++)
            {
                object obj = DataArray[i];
                TypeCode tcode = DataTypes[i];
                switch (tcode)
                {
                    case TypeCode.Boolean:
                        metaData.Add(7);
                        bodyLength++;
                        break;
                    case TypeCode.SByte:
                        metaData.Add(8);
                        bodyLength++;
                        break;
                    case TypeCode.Byte:
                        metaData.Add(8);
                        bodyLength++;
                        break;
                    case TypeCode.Int16:
                        metaData.Add(4);
                        bodyLength += 2;
                        break;
                    case TypeCode.UInt16:
                        metaData.Add(3);
                        bodyLength += 2;
                        break;
                    case TypeCode.Int32:
                        metaData.Add(2);
                        bodyLength += 4;
                        break;
                    case TypeCode.UInt32:
                        metaData.Add(0);
                        bodyLength += 4;
                        break;
                    case TypeCode.Single:
                        metaData.Add(5);
                        bodyLength += 4;
                        break;
                    case TypeCode.Double:
                        metaData.Add(6);
                        bodyLength += 8;
                        break;
                    case TypeCode.String:
                        metaData.Add(1);
                        bodyLength += (2 + Encoding.ASCII.GetBytes((string)obj).Length);
                        break;
                    case TypeCode.Int64:
                        metaData.Add(9);
                        bodyLength += 8;
                        break;
                }
            }
            byte[] retData = new byte[metaData.Count * 2];
            Buffer.BlockCopy(metaData.ToArray(), 0, retData, 0, retData.Length);
            return retData;
        }
        private void WriteObject(Object obj, TypeCode tcode, BinaryWriter writer)
        {
            switch (tcode)
            {
                case TypeCode.Boolean:
                    writer.Write((Boolean)obj);
                    break;
                case TypeCode.SByte:
                    writer.Write((SByte)obj);
                    break;
                case TypeCode.Byte:
                    writer.Write(Convert.ToByte(obj));
                    break;
                case TypeCode.Int16:
                    writer.Write(Convert.ToInt16(obj));
                    break;
                case TypeCode.UInt16:
                    writer.Write(Convert.ToUInt16(obj));
                    break;
                case TypeCode.Int32:
                    writer.Write((Int32)obj);
                    break;
                case TypeCode.UInt32:
                    writer.Write((UInt32)obj);
                    break;
                case TypeCode.Single:
                    writer.Write((Single)obj);
                    break;
                case TypeCode.Double:
                    writer.Write((Double)obj);
                    break;
                case TypeCode.Int64:
                    writer.Write(Convert.ToInt64(obj));
                    break;
                case TypeCode.String:
                    {
                        byte[] stringData = Encoding.UTF8.GetBytes((string)obj);
                        writer.Write((UInt16)stringData.Length);
                        if (stringData.Length > 0)
                            writer.Write(stringData);
                    }
                    break;
            }
        }
    }
}
