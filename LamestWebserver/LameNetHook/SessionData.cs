using System;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;

namespace LameNetHook
{
    public static class SessionContainer
    {
        private static Mutex mutex = new Mutex();

        private static List<string> FileHashes = new List<string>();
        private static List<List<Object>> FileObjects;
        private static List<List<string>> FileObjectNames;

        private static List<string> UserHashes = new List<string>();
        private static List<string> UserNames = new List<string>();
        private static List<List<Object>> UserObjects;
        private static List<List<string>> UserObjectNames;

        private static List<List<string>> FilePerUserHashes;
        private static List<List<List<Object>>> FilePerUserObjects;
        private static List<List<List<string>>> FilePerUserObjectHashes;

        public static string getSSIDforUser(string user)
        {
            int? userID = getIDfromList(user, UserNames);
            
            if(!userID.HasValue)
            {
                mutex.WaitOne();

                UserNames.Add(user);
                userID = UserNames.Count - 1;
                UserHashes.Add(null);

                // Chris: TODO: Add entries to all user dependent lists

                mutex.ReleaseMutex();
            }

            UserHashes[userID.Value] = getNewHash();

            return UserHashes[userID.Value];
        }

        private static byte[] lastHash = new byte[16];
        private static ICryptoTransform enc = null;
        private static char[] hashChars = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f',
        };

        private static string getNewHash()
        {
            GENERATE_NEW_HASH:
            string hash = "";

            // Chris: generate hash
            if(enc == null)
            {
                Aes aes = new AesManaged() { Mode = CipherMode.ECB };
                aes.GenerateIV();
                aes.GenerateKey();
                enc = aes.CreateEncryptor();
            }

            enc.TransformBlock(lastHash, 0, 16, lastHash, 0);

            for (int i = 0; i < lastHash.Length; i++)
            {
                hash += hashChars[(lastHash[i] & 0xf0) >> 4];
                hash += hashChars[lastHash[i] & 0x0f];
            }

            // Chris: if(hash already exists in any hash list) {goto GENERATE_NEW_HASH;}
            if (getIDfromList(hash, FileHashes).HasValue)
                goto GENERATE_NEW_HASH;

            if (getIDfromList(hash, UserHashes).HasValue)
                goto GENERATE_NEW_HASH;

            return hash;
        }

        private static int? getIDfromList<T>(T value, List<T> list)
        {
            if (value == null)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                if(list[i] != null && list[i].Equals(value))
                {
                    return i;
                }
            }

            return null;
        }
    }

    public struct SessionData
    {
        public string ssid, user;
        private List<string> additionalHEAD;
        private List<string> additionalPOST;
        private List<string> valuesHEAD;
        private List<string> valuesPOST;
        public string path;

        public SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST, string path) : this()
        {
            this.additionalHEAD = additionalHEAD;
            this.additionalPOST = additionalPOST;
            this.valuesHEAD = valuesHEAD;
            this.valuesPOST = valuesPOST;

            this.ssid = getValuePost("ssid");

            this.path = path;
        }

        public string getValueHead(string name)
        {
            for (int i = 0; i < additionalHEAD.Count; i++)
            {
                if (name == additionalHEAD[i])
                    return valuesHEAD[i];
            }

            return null;
        }

        public string getValuePost(string name)
        {
            for (int i = 0; i < additionalPOST.Count; i++)
            {
                if (name == additionalPOST[i])
                    return valuesPOST[i];
            }

            return null;
        }
    }
}