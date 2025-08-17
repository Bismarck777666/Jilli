using System;
using System.Security.Cryptography;
using System.Text;

public static class Crypto
{
    public static byte[] EncryptAesCbc(byte[] plainData, string aToken)
    {
        byte[] key = Encoding.UTF8.GetBytes(aToken.Substring(0, 32));
        byte[] iv = new byte[16];

        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(iv);
        }

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainData, 0, plainData.Length);

                // iv + 암호문 합치기
                byte[] result = new byte[iv.Length + encryptedBytes.Length];
                Array.Copy(iv, 0, result, 0, iv.Length);
                Array.Copy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

                return result;
            }
        }
    }

    public static byte[] DecryptAesCbc(byte[] encryptedData, string aToken)
    {
        byte[] key = Encoding.UTF8.GetBytes(aToken.Substring(0, 32));

        byte[] iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, iv.Length);

        byte[] cipherText = new byte[encryptedData.Length - iv.Length];
        Array.Copy(encryptedData, iv.Length, cipherText, 0, cipherText.Length);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return decryptedBytes;
            }
        }
    }

    static string DecryptAesCbc(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}