using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace UserNode.Database
{
    public class MD5Utils
    {
        public static string GetMD5Hash(string input)
        {
            try
            {
                MD5 md5Hash = MD5.Create();

                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
        public static bool CompareMD5Hash(string input, string hash)
        {
            try
            {
                MD5 md5Hash = MD5.Create();

                string hashOfInput = GetMD5Hash(input);

                // Create a StringComparer an compare the hashes.
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;

                if (0 == comparer.Compare(hashOfInput, hash))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool CompareMD5Hashes(string strMD51, string strMD52)
        {
            try
            {
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;

                if (0 == comparer.Compare(strMD51, strMD52))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }

        }


    }
}
