using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SlotGamesNode.GameLogics
{
    public class SerializeUtils
    {
        public static void writeInt2AxisArray(BinaryWriter writer, int[][] arrayValues)
        {
            if (arrayValues == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(arrayValues.Length);
                for (int i = 0; i < arrayValues.Length; i++)
                {
                    writer.Write(arrayValues[i].Length);
                    for (int j = 0; j < arrayValues[i].Length; j++)
                        writer.Write(arrayValues[i][j]);
                }
            }
        }
        
        public static void writeByte2AxisWithSizeList(BinaryWriter writer, List<byte[][]> byteArrayList)
        {
            if (byteArrayList == null || byteArrayList.Count == 0)
            {
                writer.Write(0);
                return;
            }
            writer.Write(byteArrayList.Count);
            for (int i = 0; i < byteArrayList.Count; i++)
                writeByte2AxisArrayWithSize(writer, byteArrayList[i]);
        }
        public static void writeByte2AxisList(BinaryWriter writer, List<byte[][]> byteArrayList)
        {
            if(byteArrayList == null || byteArrayList.Count == 0)
            {
                writer.Write(0);
                return;
            }
            writer.Write(byteArrayList.Count);
            for (int i = 0; i < byteArrayList.Count; i++)
                writeByte2AxisArray(writer, byteArrayList[i]);
        }
        public static void writeByte2AxisArrayWithSize(BinaryWriter writer, byte[][] arrayValues)
        {
            if (arrayValues != null)
            {
                for (int i = 0; i < arrayValues.Length; i++)
                {
                    for (int j = 0; j < arrayValues[i].Length; j++)
                        writer.Write(arrayValues[i][j]);
                }
            }
        }
        public static void writeByte2AxisArray(BinaryWriter writer, byte[][] arrayValues)
        {
            if (arrayValues == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(arrayValues.Length);
                for (int i = 0; i < arrayValues.Length; i++)
                {
                    writer.Write(arrayValues[i].Length);
                    for (int j = 0; j < arrayValues[i].Length; j++)
                        writer.Write(arrayValues[i][j]);
                }
            }
        }        
        public static void writeIntDictionary(BinaryWriter writer, Dictionary<int, int> intDictionary)
        {
            if (intDictionary == null)
            {
                writer.Write(0);
                return;
            }
            writer.Write(intDictionary.Count);
            foreach(KeyValuePair<int, int> pair in intDictionary)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }

        public static void writeStringArray(BinaryWriter writer, string[] stringArray)
        {
            if (stringArray == null || stringArray.Length == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(stringArray.Length);
                for (int i = 0; i < stringArray.Length; i++)
                    writer.Write(stringArray[i]);
            }
        }
        public static void writeStringList(BinaryWriter writer, List<string> stringArray)
        {
            if (stringArray == null || stringArray.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(stringArray.Count);
                for (int i = 0; i < stringArray.Count; i++)
                    writer.Write(stringArray[i]);
            }
        }
        public static void writeIntArray(BinaryWriter writer, int[] intArray)
        {
            if (intArray == null || intArray.Length == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(intArray.Length);
                for (int i = 0; i < intArray.Length; i++)
                    writer.Write(intArray[i]);
            }
        }
        public static void writeByteListArray(BinaryWriter writer, List<byte>[] byteListArray)
        {
            if(byteListArray == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteListArray.Length);
                for(int i = 0; i < byteListArray.Length; i++)
                {
                    if(byteListArray[i] == null)
                    {
                        writer.Write(0);
                    }
                    else
                    {
                        writer.Write(byteListArray[i].Count);
                        for (int j = 0; j < byteListArray[i].Count; j++)
                            writer.Write(byteListArray[i][j]);
                    }
                }
            }
        }

        
        public static void writeByteArrayDictionary(BinaryWriter writer, Dictionary<byte, byte[]> byteDictionary)
        {
            if (byteDictionary == null || byteDictionary.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteDictionary.Count);
                foreach (KeyValuePair<byte, byte[]> pair in byteDictionary)
                {
                    writer.Write(pair.Key);
                    writeByteArray(writer, pair.Value);
                }
            }
        }

        public static void writeByteIntDictionary(BinaryWriter writer, Dictionary<byte, int> byteIntDictionary)
        {
            if (byteIntDictionary == null || byteIntDictionary.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteIntDictionary.Count);
                foreach (KeyValuePair<byte, int> pair in byteIntDictionary)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
            }
        }

        public static void writeByteByteIntDictionary(BinaryWriter writer, Dictionary<byte, Dictionary<byte, int>> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                writer.Write(0);
                return;
            }
            writer.Write(dictionary.Count);
            foreach(KeyValuePair<byte, Dictionary<byte, int>> pair in dictionary)
            {
                writer.Write(pair.Key);
               writeByteIntDictionary(writer, pair.Value);               
            }
        }

        public static Dictionary<byte, Dictionary<byte, int>> readByteByteIntDictionary(BinaryReader reader)
        {
            Dictionary<byte, Dictionary<byte, int>> dictionary = new Dictionary<byte, Dictionary<byte, int>>();
            int count = reader.ReadInt32();
            if (count == 0)
                return dictionary;

            for(int i = 0; i < count; i++)
            {
                byte key = reader.ReadByte();
                Dictionary<byte, int> byteIntDictionary = readByteIntDictionary(reader);
                dictionary.Add(key, byteIntDictionary);
            }
            return dictionary;
        }

        public static void writeByteDictionary(BinaryWriter writer, Dictionary<byte, byte> byteDictionary)
        {
            if (byteDictionary == null || byteDictionary.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteDictionary.Count);
                foreach(KeyValuePair<byte,byte> pair in byteDictionary)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
            }
        }

        public static void writeByteSortedDictionary(BinaryWriter writer, SortedDictionary<byte, byte> byteDictionary)
        {
            if (byteDictionary == null || byteDictionary.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteDictionary.Count);
                foreach (KeyValuePair<byte, byte> pair in byteDictionary)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
            }
        }
        public static void writeByteList(BinaryWriter writer, List<byte> byteList)
        {
            if (byteList == null || byteList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteList.Count);
                for (int i = 0; i < byteList.Count; i++)
                    writer.Write(byteList[i]);
            }
        }
        public static void writeByteArray(BinaryWriter writer, byte[] byteArray)
        {
            if (byteArray == null || byteArray.Length == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byteArray.Length);
                for (int i = 0; i < byteArray.Length; i++)
                    writer.Write(byteArray[i]);
            }
        }

        public static void writeIntArrayList(BinaryWriter writer, List<int[]> intArrayList)
        {
            if (intArrayList == null || intArrayList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(intArrayList.Count);
                for (int i = 0; i < intArrayList.Count; i++)
                    SerializeUtils.writeIntArray(writer, intArrayList[i]);
            }
        }
        
        public static void writeByteListList(BinaryWriter writer, List<List<byte>> byte2AxisList)
        {
            if (byte2AxisList == null || byte2AxisList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(byte2AxisList.Count);
                for (int i = 0; i < byte2AxisList.Count; i++)
                {
                    SerializeUtils.writeByteList(writer, byte2AxisList[i]);
                }
            }
        }
        public static void writeInt2AxisList(BinaryWriter writer, List<List<int>> int2AxisList)
        {
            if (int2AxisList == null || int2AxisList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(int2AxisList.Count);
                for (int i = 0; i < int2AxisList.Count; i++)
                {
                    SerializeUtils.writeIntList(writer, int2AxisList[i]);
                }
            }
        }
        public static void writeBoolList(BinaryWriter writer, List<bool> boolList)
        {
            if (boolList == null || boolList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(boolList.Count);
                for (int i = 0; i < boolList.Count; i++)
                    writer.Write(boolList[i]);
            }
        }
        public static void writeIntList(BinaryWriter writer, List<int> intList)
        {
            if (intList == null || intList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(intList.Count);
                for (int i = 0; i < intList.Count; i++)
                    writer.Write(intList[i]);
            }
        }
        public static void writeFloatList(BinaryWriter writer, List<float> floatList)
        {
            if (floatList == null || floatList.Count == 0)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(floatList.Count);
                for (int i = 0; i < floatList.Count; i++)
                    writer.Write(floatList[i]);
            }
        }


        public static int[][] readInt2AxisArray(BinaryReader reader)
        {
            int length1 = reader.ReadInt32();
            int[][] arrayValues = new int[length1][];
            for(int i = 0; i < length1; i++)
            {
                int length2 = reader.ReadInt32();
                arrayValues[i] = new int[length2];
                for (int j = 0; j < length2; j++)
                    arrayValues[i][j] = reader.ReadInt32();
            }
            return arrayValues;
        }

        public static byte[][] readByte2AxisArray(BinaryReader reader)
        {
            int length1 = reader.ReadInt32();
            byte[][] arrayValues = new byte[length1][];
            for (int i = 0; i < length1; i++)
            {
                int length2 = reader.ReadInt32();
                arrayValues[i] = new byte[length2];
                for (int j = 0; j < length2; j++)
                    arrayValues[i][j] = reader.ReadByte();
            }
            return arrayValues;
        }
        public static byte[][] readByte2AxisArrayWithSize(BinaryReader reader, int length1, int length2)
        {
            byte[][] arrayValues = new byte[length1][];
            for (int i = 0; i < length1; i++)
            {
                arrayValues[i] = new byte[length2];
                for (int j = 0; j < length2; j++)
                    arrayValues[i][j] = reader.ReadByte();
            }
            return arrayValues;
        }

        public static List<byte[][]> readByte2AxisWithSizeList(BinaryReader reader, int length1, int length2)
        {
            int count = reader.ReadInt32();
            List<byte[][]> byteArrayList = new List<byte[][]>();
            for (int i = 0; i < count; i++)
                byteArrayList.Add(readByte2AxisArrayWithSize(reader, length1, length2));

            return byteArrayList;
        }
        public static List<byte[][]> readByte2AxisList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<byte[][]> byteArrayList = new List<byte[][]>();
            for (int i = 0; i < count; i++)
                byteArrayList.Add(readByte2AxisArray(reader));

            return byteArrayList;

        }

        public static Dictionary<byte, byte[]> readByteArrayDictionary(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<byte, byte[]> byteDictionary = new Dictionary<byte, byte[]>();
            for (int i = 0; i < count; i++)
            {
                byte    key   = reader.ReadByte();
                byte[]  value = readByteArray(reader);
                byteDictionary.Add(key, value);
            }
            return byteDictionary;
        }
        public static Dictionary<byte, byte> readByteDictionary(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<byte, byte> byteDictionary = new Dictionary<byte, byte>();
            for (int i = 0; i < count;i++)
            {
                byte key = reader.ReadByte();
                byte value = reader.ReadByte();
                byteDictionary.Add(key, value);
            }
            return byteDictionary;
        }

        public static SortedDictionary<byte, byte> readByteSortedDictionary(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            SortedDictionary<byte, byte> byteDictionary = new SortedDictionary<byte, byte>();
            for (int i = 0; i < count; i++)
            {
                byte key = reader.ReadByte();
                byte value = reader.ReadByte();
                byteDictionary.Add(key, value);
            }
            return byteDictionary;
        }
        public static Dictionary<byte, int> readByteIntDictionary(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<byte, int> byteIntDictionary = new Dictionary<byte, int>();
            for (int i = 0; i < count; i++)
            {
                byte key = reader.ReadByte();
                int value = reader.ReadInt32();
                byteIntDictionary.Add(key, value);
            }
            return byteIntDictionary;
        }

        public static List<Dictionary<int, int>> readIntDictionaryList(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            List<Dictionary<int, int>> intDictionaryList = new List<Dictionary<int, int>>();
            for (int i = 0; i < count; i++)
                intDictionaryList.Add(readIntDictionary(reader));
            return intDictionaryList;
        }

        public static void writeIntDictionaryList(BinaryWriter writer, List<Dictionary<int, int>> intDictionaryList)
        {
            if (intDictionaryList == null || intDictionaryList.Count == 0)
            {
                writer.Write(0);
                return;
            }
            writer.Write(intDictionaryList.Count);
            for (int i = 0; i < intDictionaryList.Count; i++)
                writeIntDictionary(writer, intDictionaryList[i]);
        }

        public static Dictionary<int, int> readIntDictionary(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Dictionary<int, int> intDictionary = new Dictionary<int, int>();
            for(int i = 0; i < count; i++)
            {
                int key     = reader.ReadInt32();
                int value   = reader.ReadInt32();
                intDictionary.Add(key, value);
            }
            return intDictionary;
        }
        public static int[] readIntArray(BinaryReader reader)
        {
            int     length      = reader.ReadInt32();
            int[]   intArray    = new int[length];
            for (int i = 0; i < length; i++)
                intArray[i] = reader.ReadInt32();

            return intArray;
        }
        public static string[] readStringArray(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            string[] stringArray = new string[length];
            for (int i = 0; i < length; i++)
                stringArray[i] = reader.ReadString();

            return stringArray;
        }
        public static List<string> readStringList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<string> stringList = new List<string>();
            for (int i = 0; i < length; i++)
                stringList.Add(reader.ReadString());

            return stringList;
        }
        public static byte[] readByteArray(BinaryReader reader)
        {
            int     length      = reader.ReadInt32();
            byte[]  byteArray   = new byte[length];

            for (int i = 0; i < length; i++)
                byteArray[i] = reader.ReadByte();

            return byteArray;
        }
        public static List<byte> readByteList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<byte> byteList = new List<byte>();

            for (int i = 0; i < length; i++)
                byteList.Add(reader.ReadByte());

            return byteList;
        }

        public static List<int> readIntList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<int> intList = new List<int>();
            for (int i = 0; i < length; i++)
                intList.Add(reader.ReadInt32());
            return intList;
        }
        public static List<bool> readBoolList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<bool> boolList = new List<bool>();
            for (int i = 0; i < length; i++)
                boolList.Add(reader.ReadBoolean());
            return boolList;
        }
        public static List<List<int>> readInt2AxisList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<List<int>> intList = new List<List<int>>();
            for (int i = 0; i < length; i++)
                intList.Add(readIntList(reader));
            return intList;
        }

        public static List<List<byte>> readByteListList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<List<byte>> byteList = new List<List<byte>>();
            for (int i = 0; i < length; i++)
                byteList.Add(readByteList(reader));
            return byteList;
        }

        

        public static List<int[]> readIntArrayList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<int[]> intList = new List<int[]>();
            for (int i = 0; i < length; i++)
                intList.Add(readIntArray(reader));
            return intList;
        }

        
        public static List<float> readFloatList(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            List<float> floatList = new List<float>();
            for (int i = 0; i < length; i++)
                floatList.Add(reader.ReadSingle());
            return floatList;
        }

        public static List<byte>[] readByteListArray(BinaryReader reader)
        {
            List<byte>[] byteListArray = null;
            int length = reader.ReadInt32();
            if (length == 0)
                return null;
            byteListArray = new List<byte>[length];
            for(int i = 0; i < length; i++)
            {
                int count = reader.ReadInt32();
                byteListArray[i] = new List<byte>();
                for (int j = 0; j < count; j++)
                    byteListArray[i].Add(reader.ReadByte());
            }
            return byteListArray;
        }
    }
}
