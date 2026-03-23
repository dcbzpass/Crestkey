using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Crestkey.Core
{
    public class Vault
    {
        private static readonly string VaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Crestkey", "vault.ck"
        );

        public List<Entry> Entries { get; set; } = new List<Entry>();

        private byte[] _key;

        public void SetKey(byte[] key) => _key = key;

        public bool IsUnlocked => _key != null;

        public static bool VaultExists() => File.Exists(VaultPath);

        public void Save()
        {
            string json = JsonConvert.SerializeObject(Entries);
            byte[] plaintext = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = Crypto.Encrypt(plaintext, _key);

            Directory.CreateDirectory(Path.GetDirectoryName(VaultPath));

            using (var fs = new FileStream(VaultPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                byte[] salt = ExtractSalt();
                bw.Write(salt);
                bw.Write(encrypted);
            }
        }

        public static (Vault vault, byte[] salt) LoadRaw()
        {
            using (var fs = new FileStream(VaultPath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                byte[] salt = br.ReadBytes(32);
                byte[] encrypted = br.ReadBytes((int)(fs.Length - 32));
                return (new Vault { _saltCache = salt }, salt);
            }
        }

        public bool TryUnlock(string password)
        {
            try
            {
                _key = Crypto.DeriveKey(password, _saltCache);
                using (var fs = new FileStream(VaultPath, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    br.ReadBytes(32);
                    byte[] encrypted = br.ReadBytes((int)(fs.Length - 32));
                    byte[] plaintext = Crypto.Decrypt(encrypted, _key);
                    string json = Encoding.UTF8.GetString(plaintext);
                    Entries = JsonConvert.DeserializeObject<List<Entry>>(json);
                    return true;
                }
            }
            catch
            {
                _key = null;
                return false;
            }
        }

        public static Vault CreateNew(string password)
        {
            byte[] salt = Crypto.GenerateSalt();
            byte[] key = Crypto.DeriveKey(password, salt);
            var vault = new Vault { _saltCache = salt };
            vault._key = key;
            vault.Save();
            return vault;
        }

        private byte[] ExtractSalt() => _saltCache;
        private byte[] _saltCache;
    }
}