//#define PERSISTENT_DATA

using System;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using LamestWebserver.Collections;
using System.Text;

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
        internal static string getSSIDforUser(string user, out bool isNewSSID, out UserInfo userInfo)
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

        internal static string forceGetNextSSID(string user)
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
        
        private static byte[] lastHash = null;
        private static ICryptoTransform enc = null;

        private static Mutex hashMutex = new Mutex();

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
            string hash = "";

            if (hashMutex == null)
                hashMutex = new Mutex();

            hashMutex.WaitOne();

            // Chris: generate hash

            if (enc == null)
            {
                Aes aes = new AesManaged() { Mode = CipherMode.ECB };
                aes.GenerateIV();
                aes.GenerateKey();
                enc = aes.CreateEncryptor();

                if (lastHash == null)
                    lastHash = new byte[16];
            }

            enc.TransformBlock(lastHash, 0, 16, lastHash, 0);

            for (int i = 0; i < lastHash.Length; i++)
            {
                hash += Master.hexLut[(lastHash[i] & 0xf0) >> 4];
                hash += Master.hexLut[lastHash[i] & 0x0f];
            }

            hashMutex.ReleaseMutex();

            return hash;
        }

        private static SHA3Managed sha3;
        private static UnicodeEncoding sha3UnicodeEncoding = new UnicodeEncoding();
        private static Mutex sha3HashMutex = new Mutex();

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input
        /// </summary>
        /// <param name="input">the text to hash</param>
        /// <returns>the hash as hex string</returns>
        public static string getComplexHash(string input)
        {
            return getComplexHash(sha3UnicodeEncoding.GetBytes(input)).ToHexString();
        }

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input
        /// </summary>
        /// <param name="input">the byte[] to hash</param>
        /// <returns>the hash as byte[]</returns>
        public static byte[] getComplexHash(byte[] input)
        {
            // Chris: Preparation if sha3 hasn't been initialized
            if (sha3 == null)
            {
                sha3 = new SHA3Managed(512);
            }

            sha3HashMutex.WaitOne();

            byte[] bytes = sha3.ComputeHash(input);

            sha3HashMutex.ReleaseMutex();

            return bytes;
        }

        /// <summary>
        /// Generates a SHA3 512bit hash of a random piece of code
        /// </summary>
        /// <returns>the hash as hex string</returns>
        public static string generateComplexHash()
        {
            return getComplexHash(generateHash());
        }

        internal static UserInfo getUserInfoFromSSID(string ssid)
        {
            return UserInfos[ssid];
        }

        internal static AVLTree<string, object> getFileDictionary(string fileName)
        {
            AVLTree<string, object> ret = PerFileObjects[fileName];

            if(ret == null)
            {
                ret = new AVLTree<string, object>();
                PerFileObjects[fileName] = ret;
            }

            return ret;
        }

        internal static object getUserInfoFromName(string userName)
        {
            return UserInfosByName[userName];
        }
    }

    /// <summary>
    /// Contains all Session dependent Information
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// contains the current session data of this thread
        /// </summary>
        [ThreadStaticAttribute]
        public static SessionData currentSessionData = null;

        private IDictionary<string, object> PerFileVariables;
        private SessionContainer.UserInfo userInfo;

        /// <summary>
        /// The SSID of the current Request
        /// </summary>
        public string ssid { get; private set; }

        /// <summary>
        /// The name of the current user (the sessionID handles this!) (the current user could by incognito due to a missing sessionID)
        /// </summary>
        public string userName
        {
            get { return userInfo?.UserName; }
        }

        /// <summary>
        /// Represents the state of the current viewer of the page - true if this user has a special hash
        /// </summary>
        public bool knownUser { get { return userInfo != null; } }

        /// <summary>
        /// The Variables mentinoed in the HTTP head (http://www.link.com/?IamAHeadVariable)
        /// </summary>
        public List<string> varsHEAD { get; private set; }

        /// <summary>
        /// Cookies to set in the client browser
        /// </summary>
        public List<KeyValuePair<string, string>> Cookies = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// The Variables mentinoed in the HTTP POST packet
        /// </summary>
        public List<string> varsPOST { get; private set; }

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP head (they don't have to have values!) (http://www.link.com/?IamAHeadVariable=IamAHeadValue)
        /// </summary>
        public List<string> valuesHEAD { get; private set; }

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP POST packet (they don't have to have values!)
        /// </summary>
        public List<string> valuesPOST { get; private set; }

        /// <summary>
        /// the workingpath of the current server
        /// </summary>
        public string path { get; private set; }

        /// <summary>
        /// the currently requested file
        /// </summary>
        public string file { get; private set; }

        /// <summary>
        /// the raw packet sent to the server
        /// </summary>
        public string _rawPacket { get; private set; }

        /// <summary>
        /// The original tcpClient of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.TcpClient _tcpClient { get; private set; }

        /// <summary>
        /// The original networkStream of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.NetworkStream _networkStream { get; private set; }

        /// <summary>
        /// The EndPoint of the connected client
        /// </summary>
        public System.Net.EndPoint _remoteEndPoint { get; private set; }

        /// <summary>
        /// The EndPoint of the server
        /// </summary>
        public System.Net.EndPoint _localEndPoint { get; private set; }

        /// <summary>
        /// The cookies sent by the client to the server
        /// </summary>
        public AVLTree<string, string> receivedCookiesTree { get; private set; }

        internal SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST, List<KeyValuePair<string, string>> Cookies, string path, string file, string packet, System.Net.Sockets.TcpClient client, System.Net.Sockets.NetworkStream nws)
        {
            this.varsHEAD = additionalHEAD;
            this.varsPOST = additionalPOST;
            this.valuesHEAD = valuesHEAD;
            this.valuesPOST = valuesPOST;
            this.path = path;
            this.file = file;
            this.receivedCookiesTree = new AVLTree<string, string>();

            if (Cookies != null)
            {
                foreach (KeyValuePair<string, string> kvp in Cookies)
                {
                    receivedCookiesTree.Add(kvp);
                }
            }

            this._rawPacket = packet;
            this._tcpClient = client;
            this._networkStream = nws;
            this._remoteEndPoint = _tcpClient.Client.RemoteEndPoint;
            this._localEndPoint = _tcpClient.Client.LocalEndPoint;

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                this.ssid = getHTTP_POST_Value("ssid");
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                this.ssid = receivedCookiesTree["ssid"];
            }

            this.PerFileVariables = SessionContainer.getFileDictionary(file);
            this.userInfo = SessionContainer.getUserInfoFromSSID(ssid);

            currentSessionData = this;
        }

        /// <summary>
        /// get the value of a HTTP HEAD variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP HEAD variable</param>
        /// <returns>the value of the given HTTP HEAD variable (or null if not existent)</returns>
        public string getHTTP_HEAD_Value(string name)
        {
            for (int i = 0; i < varsHEAD.Count; i++)
            {
                if (name == varsHEAD[i])
                    return valuesHEAD[i];
            }

            return null;
        }

        /// <summary>
        /// get the value of a HTTP POST variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP POST variable</param>
        /// <returns>the value of the given HTTP POST variable (or null if not existent)</returns>
        public string getHTTP_POST_Value(string name)
        {
            for (int i = 0; i < varsPOST.Count; i++)
            {
                if (name == varsPOST[i])
                    return valuesPOST[i];
            }

            return null;
        }

        /// <summary>
        /// Registers the user and assigns a SSID
        /// </summary>
        /// <param name="userName">the User to register</param>
        /// <returns>the SSID for the user</returns>
        public string registerUser(string userName)
        {
            bool isNewSSID;

            ssid = SessionContainer.getSSIDforUser(userName, out isNewSSID, out userInfo);
            
            if(SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                Cookies.Add(new KeyValuePair<string, string>("ssid", ssid));

            return ssid;
        }

        /// <summary>
        /// PLEASE CALL THIS BEFORE BUILDING THE SITE!
        /// _FORCES_ to get a new SSID for the current user needed for higher level security
        /// </summary>
        /// <returns>the new ssid</returns>
        public string forceGetNextSSID()
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            ssid = SessionContainer.forceGetNextSSID(userName);

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                Cookies.Add(new KeyValuePair<string, string>("ssid", ssid));

            return ssid;
        }


        // ===============================================================================================================================================
        // ===============================================================================================================================================

        private T2 getObjectFromDictionary<T, T2>(T key, IDictionary<T, T2> IDictionary)
        {
            return IDictionary.ContainsKey(key) ? IDictionary[key] : default(T2);
        }

        private void setValueToDictionary<T, T2>(T key, T2 value, IDictionary<T, T2> IDictionary)
        {
            if (value == null)
            {
                IDictionary.Remove(key);
            }
            else
            {
                IDictionary[key] = value;
            }
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// contains all available scopes for variables
        /// </summary>
        public enum EVariableScope : byte
        {
            /// <summary>
            /// available for all visitors of this page
            /// </summary>
            File,
            /// <summary>
            /// Available globally for this USER
            /// </summary>
            User,
            /// <summary>
            /// Available for the current User on only this page
            /// </summary>
            FileAndUser,
            /// <summary>
            /// Available for all Users on any page
            /// </summary>
            Global
        }

        /// <summary>
        /// Get the value of a variable defined at a certain scope by name
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <param name="scope">scope at which the variable is defined</param>
        /// <returns>returns the value of the variable (or null if not existent or exception if you really want to mess things up, dude!)</returns>
        public object getVariable(string name, EVariableScope scope)
        {
            switch(scope)
            {
                case EVariableScope.File:
                    return getFileVariable(name);

                case EVariableScope.User:
                    return getUserVariable(name);

                case EVariableScope.FileAndUser:
                    return getUserFileVariable(name);

                case EVariableScope.Global:
                    return getGlobalVariable(name);

                default:
                    throw new Exception("Error: The scope has to be set to one of the predefined states!");
            }
        }

        /// <summary>
        /// Get the value of a variable defined at a certain scope by name
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <param name="scope">scope at which the variable is defined</param>
        /// <returns>returns the value of the variable (or null if not existent or exception if you really want to mess things up, dude!)</returns>
        public T getVariable<T>(string name, EVariableScope scope)
        {
            object o = getVariable(name, scope);

            if (o == null)
                return default(T);

            return (T)o;
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// Set the value of a variable defined at a certain scope by name
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="name">name of the variable</param>
        /// <param name="value">the value to set to the variable</param>
        /// <param name="scope">scope at which the variable is/will be defined</param>
        public void setVariable<T>(string name, T value, EVariableScope scope)
        {
            switch (scope)
            {
                case EVariableScope.File:
                    setFileVariable(name, value);
                    break;

                case EVariableScope.User:
                    setUserVariable(name, value);
                    break;

                case EVariableScope.FileAndUser:
                    setUserFileVariable(name, value);
                    break;

                case EVariableScope.Global:
                    setGlobalVariable(name, value);
                    break;

                default:
                    throw new Exception("Error: The scope has to be set to one of the predefined states!");
            }
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// set the value of a variable saved globally (available from everywhere on this server)
        /// </summary>
        /// <typeparam name="T">The Type of the Value</typeparam>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        public void setGlobalVariable<T>(string name, T value)
        {
            setValueToDictionary(name, value, SessionContainer.globalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally (available from everywhere on this server)
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object getGlobalVariable(string name)
        {
            return getObjectFromDictionary(name, SessionContainer.globalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally (available from everywhere on this server) and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getGlobalVariable<T>(string name)
        {
            object o = getGlobalVariable(name);

            if (o == null)
                return default(T);

            return (T)o;
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// set the value of a variable saved globally for the current _USER_AND_FILE_COMBINATION_
        /// </summary>
        /// <typeparam name="T">The Type of the Value</typeparam>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        public void setUserFileVariable<T>(string name, T value)
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            setValueToDictionary(name, value, userInfo.PerFileVariables[file]);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_AND_FILE_COMBINATION_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object getUserFileVariable(string name)
        {
            if (!knownUser)
                return null;

            return getObjectFromDictionary(name, userInfo.PerFileVariables[file]);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_AND_FILE_COMBINATION_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getUserFileVariable<T>(string name)
        {
            object o = getUserFileVariable(name);

            if (o == null)
                return default(T);

            return (T)o;
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// set the value of a variable saved globally for the current _USER_
        /// </summary>
        /// <typeparam name="T">The Type of the Value</typeparam>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        public void setUserVariable<T>(string name, T value)
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            setValueToDictionary(name, value, userInfo.UserGlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object getUserVariable(string name)
        {
            if (!knownUser)
                return null;

            return getObjectFromDictionary(name, userInfo.UserGlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getUserVariable<T>(string name)
        {
            object o = getUserVariable(name);

            if (o == null)
                return default(T);

            return (T)o;
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// set the value of a variable saved globally for the current _FILE_
        /// </summary>
        /// <typeparam name="T">The Type of the Value</typeparam>
        /// <param name="name">The name of the variable</param>
        /// <param name="value">The value of the variable</param>
        public void setFileVariable<T>(string name, T value)
        {
            setValueToDictionary(name, value, PerFileVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object getFileVariable(string name)
        {
            return getObjectFromDictionary(name, PerFileVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getFileVariable<T>(string name)
        {
            object o = getFileVariable(name);

            if (o == null)
                return default(T);

            return (T)o;
        }

        /// <summary>
        /// Tells if a user has ever been registered with the given name
        /// </summary>
        /// <param name="userName">the name of the user</param>
        /// <returns>true if the user has ever existed</returns>
        public bool userExists(string userName)
        {
            return SessionContainer.getUserInfoFromName(userName) != null;
        }

        /// <summary>
        /// deletes the registration of the current user.
        /// </summary>
        public void unregiserUser()
        {
            if (!knownUser)
            {
                SessionContainer.forceGetNextSSID(userName);
                ssid = "";

                if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                    Cookies.Add(new KeyValuePair<string, string>("ssid", ""));
            }
        }
    }
}