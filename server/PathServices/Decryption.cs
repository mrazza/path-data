namespace PathApi.Server.PathServices
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    static class Decryption
    {
        private static string ConfigurationDecryptKey = "TLckjEE2f4mdo6d6vqiHhgTfB";
        private static byte[] KeySalt = new byte[13] { 73, 118, 97, 110, 32, 77, 101, 100, 118, 101, 100, 101, 118 };

        public static string Decrypt(string cipherText)
        {
            string configurationDecryptKey = ConfigurationDecryptKey;
            cipherText = cipherText.Replace(" ", "+");
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(configurationDecryptKey, KeySalt);
                aes.Key = rfc2898DeriveBytes.GetBytes(32);
                aes.IV = rfc2898DeriveBytes.GetBytes(16);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(buffer, 0, buffer.Length);
                        cryptoStream.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(memoryStream.ToArray());
                }
            }
            return cipherText;
        }
    }
}