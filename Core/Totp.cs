using System;
using System.Security.Cryptography;

namespace Crestkey.Core
{
    public static class Totp
    {
        private const int Step = 30;
        private const int Digits = 6;

        public static string Generate(string base32Secret)
        {
            byte[] key = Base32Decode(base32Secret.Trim().ToUpper());
            long counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Step;

            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

            using (var hmac = new HMACSHA1(key))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);
                int offset = hash[hash.Length - 1] & 0x0F;
                int code = ((hash[offset] & 0x7F) << 24)
                         | ((hash[offset + 1] & 0xFF) << 16)
                         | ((hash[offset + 2] & 0xFF) << 8)
                         | (hash[offset + 3] & 0xFF);
                return (code % (int)Math.Pow(10, Digits)).ToString($"D{Digits}");
            }
        }

        public static int SecondsRemaining()
            => Step - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % Step);

        public static bool IsValidSecret(string base32Secret)
        {
            if (string.IsNullOrWhiteSpace(base32Secret)) return false;
            try { Base32Decode(base32Secret.Trim().ToUpper()); return true; }
            catch { return false; }
        }

        private static byte[] Base32Decode(string input)
        {
            input = input.TrimEnd('=');
            int byteCount = input.Length * 5 / 8;
            byte[] result = new byte[byteCount];
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            int buffer = 0, bitsLeft = 0, index = 0;
            foreach (char c in input)
            {
                int val = alphabet.IndexOf(c);
                if (val < 0) throw new FormatException($"Invalid base32 character: {c}");
                buffer = (buffer << 5) | val;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    result[index++] = (byte)(buffer >> bitsLeft);
                }
            }
            return result;
        }
    }
}