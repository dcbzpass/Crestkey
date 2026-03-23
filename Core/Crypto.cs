using System;
using System.IO;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace Crestkey.Core
{
    public static class Crypto
    {
        private const int SaltSize = 32;
        private const int KeySize = 32;
        private const int IvSize = 16;

        private const int Argon2Iterations = 4;
        private const int Argon2Memory = 65536;
        private const int Argon2Parallelism = 2;

        public static byte[] DeriveKey(string password, byte[] salt)
        {
            using (var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.Iterations = Argon2Iterations;
                argon2.MemorySize = Argon2Memory;
                argon2.DegreeOfParallelism = Argon2Parallelism;
                return argon2.GetBytes(KeySize);
            }
        }

        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.GenerateIV();

                byte[] iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        cs.Write(plaintext, 0, plaintext.Length);

                    byte[] encrypted = ms.ToArray();
                    byte[] hmac = ComputeHmac(key, encrypted);

                    using (var final = new MemoryStream())
                    {
                        final.Write(hmac, 0, hmac.Length);
                        final.Write(encrypted, 0, encrypted.Length);
                        return final.ToArray();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] data, byte[] key)
        {
            byte[] hmac = new byte[32];
            Buffer.BlockCopy(data, 0, hmac, 0, 32);

            byte[] encrypted = new byte[data.Length - 32];
            Buffer.BlockCopy(data, 32, encrypted, 0, encrypted.Length);

            byte[] expectedHmac = ComputeHmac(key, encrypted);
            if (!HmacEqual(hmac, expectedHmac))
                throw new CryptographicException("HMAC verification failed.");

            byte[] iv = new byte[IvSize];
            Buffer.BlockCopy(encrypted, 0, iv, 0, IvSize);

            byte[] ciphertext = new byte[encrypted.Length - IvSize];
            Buffer.BlockCopy(encrypted, IvSize, ciphertext, 0, ciphertext.Length);

            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(ciphertext))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var result = new MemoryStream())
                {
                    cs.CopyTo(result);
                    return result.ToArray();
                }
            }
        }

        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            return salt;
        }

        private static byte[] ComputeHmac(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA256(key))
                return hmac.ComputeHash(data);
        }

        private static bool HmacEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}