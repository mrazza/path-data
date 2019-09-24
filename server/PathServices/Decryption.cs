namespace PathApi.Server.PathServices
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Static class providing decryption functionality.
    /// </summary>
    internal static class Decryption
    {
        private static readonly string LegacyConfigurationDecryptKey = "TLckjEE2f4mdo6d6vqiHhgTfB";
        private static readonly string ConfigurationDecryptKey = "PVTG16QwdKSbQhjIwSsQdAm0i";
        private static readonly byte[] KeySalt = new byte[13] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 };

        /// <summary>
        /// Decrypts the provided base64-encoded string.
        /// </summary>
        /// <param name="cipherText">The string to decrypt.</param>
        /// <returns>The decrypted version of the input.</returns>
        public static string Decrypt(string cipherText, bool legacyKey = false)
        {
            byte[] buffer = Convert.FromBase64String(cipherText.Replace(" ", "+"));
            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(legacyKey ? LegacyConfigurationDecryptKey : ConfigurationDecryptKey, KeySalt);
                aes.Key = rfc2898DeriveBytes.GetBytes(32);
                aes.IV = rfc2898DeriveBytes.GetBytes(16);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(buffer, 0, buffer.Length);
                    }
                    return Encoding.Unicode.GetString(memoryStream.ToArray());
                }
            }
        }
    }
}