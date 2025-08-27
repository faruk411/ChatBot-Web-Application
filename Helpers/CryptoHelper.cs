using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace chatbot.Helpers
{
    public static class CryptoHelper
    {

        private static byte[] HexToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        // Web.config'den okuyacağımız anahtar ve IV (Initialization Vector)
        private static readonly byte[] Key = HexToByteArray(ConfigurationManager.AppSettings["EncryptionKey"]);
        private static readonly byte[] IV = HexToByteArray(ConfigurationManager.AppSettings["EncryptionIV"]);

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using(MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }
        public static string Decrypt(string cipherText)
        {
            if(string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }
            try
            {
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(buffer))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();   
                    }
                }
            }
            catch (FormatException)
            {
                return cipherText; // Reğer veriler şifleri değilse (esli veriler) olduğu gibi geri döndürülür.
            }
            catch(CryptographicException)
            {
                return cipherText; // Şifreleme hatası durumunda da orijinal değer döndürülür.
            }
            catch (Exception)
            {
                return cipherText; // Diğer tüm hatalarda da orijinal değer döndürülür.
            }
        }

      
    }
}