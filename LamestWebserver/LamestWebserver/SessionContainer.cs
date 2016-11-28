using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    /// <summary>
    /// Here you can find all the Global SessionID related methods and fields
    /// </summary>
    public static class SessionContainer
    {
        /// <summary>
        /// The mode for SessionID recreation.
        /// </summary>
        public static ESessionIdRereferencingMode SessionIdRereferencingMode = ESessionIdRereferencingMode.Keep;

        /// <summary>
        /// Contains the available SessionID recreation modes
        /// </summary>
        public enum ESessionIdRereferencingMode
        {
            /// <summary>
            /// Keeps the SessionID for a specific session.
            /// </summary>
            Keep,
            /// <summary>
            /// always renews the sessionID during every newly processed page
            /// </summary>
            AlwaysRenew
        }

        /// <summary>
        /// The mode for sessionId transmission
        /// </summary>
        public static ESessionIdTransmissionType SessionIdTransmissionType = ESessionIdTransmissionType.Cookie;

        /// <summary>
        /// Contains all available modes for sessionId transmission
        /// </summary>
        public enum ESessionIdTransmissionType
        {
            /// <summary>
            /// Transmitts the SessionIDs via HTTP POST
            /// </summary>
            [Obsolete]
            POST,
            /// <summary>
            /// Transmitts the SessionID via Cookie
            /// </summary>
            Cookie
        }

        /// <summary>
        /// The default size of the HashMaps for UserGlobal-Variables
        /// </summary>
        public static int UserVariableHashMapSize = 512;

        /// <summary>
        /// The default size of the HashMap containing the User
        /// </summary>
        public static int UserHashMapSize = 128;

        private static UsableMutex mutex = new UsableMutex();

        private static AVLTree<string, AVLTree<string, object>> PerFileObjects = new AVLTree<string, AVLTree<string, object>>();
        
        private static AVLHashMap<string, UserInfo> UserInfos = new AVLHashMap<string, UserInfo>(UserHashMapSize);
        private static AVLHashMap<string, UserInfo> UserInfosByName = new AVLHashMap<string, UserInfo>(UserHashMapSize);

        internal static AVLTree<string, object> globalVariables = new AVLTree<string, object>();

        internal class UserInfo
        {
            internal string ID;
            internal string UserName;
            internal AVLHashMap<string, object> UserGlobalVariables = new AVLHashMap<string, object>(512);
            internal AVLTree<string, AVLTree<string, object>> PerFileVariables = new AVLTree<string, AVLTree<string, object>>();
        }

        /// <summary>
        /// This method also creates a user if none exists!
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="isNewSSID">has a new ssid been created or are we reusing the old one</param>
        /// <returns></returns>
        internal static string GetSSIDforUser(string user, out bool isNewSSID, out UserInfo userInfo)
        {
            string hash = UserInfosByName[user]?.ID;

            if (hash == null)
            {
                userInfo = new UserInfo();
                userInfo.ID = generateUnusedHash();
                hash = userInfo.ID;
                userInfo.UserName = user;

                mutex.WaitOne();

                UserInfos.Add(userInfo.ID, userInfo);
                UserInfosByName.Add(userInfo.UserName, userInfo);

                mutex.ReleaseMutex();

                isNewSSID = true;
            }
            else
            {
                isNewSSID = false;

                if (SessionIdRereferencingMode == ESessionIdRereferencingMode.AlwaysRenew)
                {
                    mutex.WaitOne();

                    userInfo = UserInfosByName[user];
                    UserInfos.Remove(userInfo.ID);
                    userInfo.ID = generateUnusedHash();
                    UserInfos[userInfo.ID] = userInfo;

                    mutex.ReleaseMutex();

                    isNewSSID = true;
                    hash = userInfo.ID;
                }
                else if (SessionIdRereferencingMode == ESessionIdRereferencingMode.Keep && UserInfosByName[user].ID == null)
                {
                    mutex.WaitOne();

                    userInfo = UserInfosByName[user];
                    UserInfos.Remove(userInfo.ID);
                    userInfo.ID = generateUnusedHash();
                    UserInfos[userInfo.ID] = userInfo;

                    mutex.ReleaseMutex();

                    isNewSSID = true;
                    hash = userInfo.ID;
                }
                else
                {
                    userInfo = UserInfosByName[user];
                }
            }

            return hash;
        }

        internal static string ForceGetNextSSID(string user)
        {
            string hash = UserInfosByName[user]?.ID;

            if (hash == null)
            {
                UserInfo info = new UserInfo();
                info.ID = generateUnusedHash();
                hash = info.ID;
                info.UserName = user;

                mutex.WaitOne();

                UserInfos.Add(info.ID, info);
                UserInfosByName.Add(info.UserName, info);

                mutex.ReleaseMutex();
            }
            else
            {
                mutex.WaitOne();

                UserInfo info = UserInfosByName[user];
                UserInfos.Remove(info.ID);
                info.ID = generateUnusedHash();
                UserInfos[info.ID] = info;

                mutex.ReleaseMutex();
                
                hash = info.ID;
            }

            return hash;
        }
        
        private static byte[] _lastHash = null;
        private static ICryptoTransform _cryptoTransform = null;

        private static Mutex _hashMutex = new Mutex();

        /// <summary>
        /// generates a 128 bit AES hash that is not used in pagenames
        /// </summary>
        /// <returns></returns>
        public static string generateUnusedHash()
        {
            GENERATE_NEW_HASH:
            string hash = generateHash();

            // Chris: if(hash already exists in any hash list) {goto GENERATE_NEW_HASH;}
            
            if (UserInfos.ContainsKey(hash))
                goto GENERATE_NEW_HASH;

            if (PerFileObjects.ContainsKey(hash))
                goto GENERATE_NEW_HASH;

            return hash;
        }

        /// <summary>
        /// generates a 128 bit AES hash
        /// </summary>
        /// <returns></returns>
        public static string generateHash()
        {
            if (_hashMutex == null)
                _hashMutex = new Mutex();

            _hashMutex.WaitOne();

            // Chris: generate hash

            if (_cryptoTransform == null)
            {
                Aes aes = new AesManaged() { Mode = CipherMode.ECB };
                aes.GenerateIV();
                aes.GenerateKey();
                _cryptoTransform = aes.CreateEncryptor();

                if (_lastHash == null)
                    _lastHash = new byte[16];
            }

            _cryptoTransform.TransformBlock(_lastHash, 0, 16, _lastHash, 0);

            string hash = _lastHash.ToHexString();

            _hashMutex.ReleaseMutex();

            return hash;
        }

        private static SHA3Managed _sha3;
        private static readonly Mutex Sha3HashMutex = new Mutex();

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input
        /// </summary>
        /// <param name="input">the text to hash</param>
        /// <returns>the hash as hex string</returns>
        public static string getComplexHash(string input)
        {
            return getComplexHash(Encoding.Unicode.GetBytes(input)).ToHexString();
        }

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input
        /// </summary>
        /// <param name="input">the byte[] to hash</param>
        /// <returns>the hash as byte[]</returns>
        public static byte[] getComplexHash(byte[] input)
        {
            // Chris: Preparation if sha3 hasn't been initialized
            if (_sha3 == null)
            {
                _sha3 = new SHA3Managed(512);
            }

            Sha3HashMutex.WaitOne();

            byte[] bytes = _sha3.ComputeHash(input);

            Sha3HashMutex.ReleaseMutex();

            return bytes;
        }

        private static string _lastSha3Hash = generateHash() + generateHash() + generateHash() + generateHash();

        /// <summary>
        /// Generates a SHA3 512bit hash of a random piece of code
        /// </summary>
        /// <returns>the hash as hex string</returns>
        public static string generateComplexHash()
        {
            return _lastSha3Hash = getComplexHash(_lastSha3Hash + generateHash());
        }

        internal static UserInfo GetUserInfoFromSsid(string ssid)
        {
            return UserInfos[ssid];
        }

        internal static AVLTree<string, object> GetFileDictionary(string fileName)
        {
            AVLTree<string, object> ret = PerFileObjects[fileName];

            if(ret == null)
            {
                ret = new AVLTree<string, object>();
                PerFileObjects[fileName] = ret;
            }

            return ret;
        }

        internal static object GetUserInfoFromName(string userName)
        {
            return UserInfosByName[userName];
        }
    }
}