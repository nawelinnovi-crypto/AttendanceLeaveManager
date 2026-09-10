using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace backend.Utils
{
    public class PasswordSecurity
    {
        public static string EncryptString(string text, string password)
        {
            byte[] baPwd = Encoding.UTF8.GetBytes(password);
            byte[] baPwdHash = SHA256.HashData(baPwd);
            byte[] baText = Encoding.UTF8.GetBytes(text);
            byte[] baSalt = GetRandomBytes();
            byte[] baEncrypted = new byte[baSalt.Length + baText.Length];

            for (int i = 0; i < baSalt.Length; i++)
            {
                baEncrypted[i] = baSalt[i];
            }

            for (int i = 0; i < baText.Length; i++)
            {
                baEncrypted[i + baSalt.Length] = baText[i];
            }

            baEncrypted = AES_Encrypt(baEncrypted, baPwdHash);
            return Convert.ToBase64String(baEncrypted);
        }

        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using MemoryStream ms = new MemoryStream();
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;

            byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.KeySize / 8);
            byte[] ivBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.BlockSize / 8);
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;

            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }

        public static string DecryptString(string text, string password)
        {
            try
            {
                byte[] baPwd = Encoding.UTF8.GetBytes(password);
                byte[] baPwdHash = SHA256.HashData(baPwd);
                byte[] baText = Convert.FromBase64String(text);

                byte[] baDecrypted = AES_Decrypt(baText, baPwdHash);

                int saltLength = GetSaltLength();
                byte[] baResult = new byte[baDecrypted.Length - saltLength];
                for (int i = 0; i < baResult.Length; i++)
                {
                    baResult[i] = baDecrypted[i + saltLength];
                }

                return Encoding.UTF8.GetString(baResult);
            }
            catch
            {
                return text;
            }
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using MemoryStream ms = new MemoryStream();
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;

            byte[] keyBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.KeySize / 8);
            byte[] ivBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 1000, HashAlgorithmName.SHA1, aes.BlockSize / 8);
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;

            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }

        public static byte[] GetRandomBytes()
        {
            int saltLength = GetSaltLength();
            byte[] ba = new byte[saltLength];
            RandomNumberGenerator.Fill(ba);
            return ba;
        }

        public static int GetSaltLength()
        {
            return 8;
        }
    }
}
