//#define PERSISTENT_DATA

using System;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.Serialization;
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
            POST,
            /// <summary>
            /// Transmitts the SessionID via Cookie
            /// </summary>
            Cookie
        }

#if PERSISTENT_DATA
        private static Mutex persistencyMutex = new Mutex();
        private static List<List<string>> persistentUserDataHashes;
        private static List<List<object>> persistentUserData;
#endif

        private static Mutex mutex = new Mutex();
        
        private static List<string> FileNames = new List<string>();
        private static List<Dictionary<string, object>> FileObjects = new List<Dictionary<string, object>>();

        private static List<string> UserHashes = new List<string>();
        private static List<string> UserNames = new List<string>();
        private static List<Dictionary<string, object>> UserObjects = new List<Dictionary<string, object>>();

        private static List<List<string>> FilePerUserNames = new List<List<string>>();
        private static List<List<Dictionary<string, object>>> FilePerUserObjects = new List<List<Dictionary<string, object>>>();

        internal static Dictionary<string, object> globalVariables = new Dictionary<string, object>();

        /// <summary>
        /// This method also creates a user if none exists!
        /// </summary>
        /// <param name="user">the current user</param>
        /// <param name="isNewSSID">has a new ssid been created or are we reusing the old one</param>
        /// <returns></returns>
        internal static string getSSIDforUser(string user, out bool isNewSSID)
        {
            int? userID = getIndexFromList(user, UserNames);
            
            if(!userID.HasValue)
            {
                mutex.WaitOne();

                UserNames.Add(user);
                userID = UserNames.Count - 1;
                UserHashes.Add(null);
                FilePerUserNames.Add(new List<string>());
                FilePerUserObjects.Add(new List<Dictionary<string, object>>());
                UserObjects.Add(new Dictionary<string, object>());

#if PERSISTENT_DATA
                persistencyMutex.WaitOne();

                try
                {
                    if (persistentUserData == null)
                        readPersistency();

                    persistentUserDataHashes.Add(new List<string>());
                    persistentUserData.Add(new List<object>());
                    writePersistency();

                    persistencyMutex.ReleaseMutex();
                    mutex.ReleaseMutex();
                }
                catch (Exception e)
                {
                    persistencyMutex.ReleaseMutex();
                    mutex.ReleaseMutex();

                    throw new Exception(e.Message, e);
                }
#else
                mutex.ReleaseMutex();
#endif
            }

            isNewSSID = false;

            if (SessionIdRereferencingMode == ESessionIdRereferencingMode.AlwaysRenew)
            {
                UserHashes[userID.Value] = generateHash();
                isNewSSID = true;
            }
            else if (SessionIdRereferencingMode == ESessionIdRereferencingMode.Keep && UserHashes[userID.Value] == null)
            {
                isNewSSID = true;
                UserHashes[userID.Value] = generateHash();
            }

            return UserHashes[userID.Value];
        }

        internal static string forceGetNextSSID(string user)
        {
            int? userID = getIndexFromList(user, UserNames);

            if (!userID.HasValue)
            {
                mutex.WaitOne();

                UserNames.Add(user);
                userID = UserNames.Count - 1;
                UserHashes.Add(null);
                FilePerUserNames.Add(new List<string>());
                FilePerUserObjects.Add(new List<Dictionary<string, object>>());
                UserObjects.Add(new Dictionary<string, object>());

#if PERSISTENT_DATA
                persistencyMutex.WaitOne();

                try
                {
                    if (persistentUserData == null)
                        readPersistency();

                    persistentUserDataHashes.Add(new List<string>());
                    persistentUserData.Add(new List<object>());
                    writePersistency();

                    persistencyMutex.ReleaseMutex();
                    mutex.ReleaseMutex();
                }
                catch (Exception e)
                {
                    persistencyMutex.ReleaseMutex();
                    mutex.ReleaseMutex();

                    throw new Exception(e.Message, e);
                }
#else
                mutex.ReleaseMutex();
#endif
            }

            UserHashes[userID.Value] = generateHash();

            return UserHashes[userID.Value];
        }

#if PERSISTENT_DATA
        private static void readPersistency()
        {
            try
            {
                persistentUserData = Serializer.getData<List<List<object>>>("./user/userdata.xml");
                persistentUserDataHashes = Serializer.getData<List<List<string>>>("./user/userhashes.xml");
            }
            catch(Exception)
            {
                persistentUserData = new List<List<object>>();
                persistentUserDataHashes = new List<List<string>>();
            }
        }

        private static void writePersistency()
        {
            try
            {
                Serializer.writeData(persistentUserData, "./user/usersdata.xml");
                Serializer.writeData(persistentUserDataHashes, "./user/userhashes.xml");
            }
            catch(Exception)
            {
                if(!System.IO.Directory.Exists("./user/"))
                {
                    System.IO.Directory.CreateDirectory("./user/");
                }

                Serializer.writeData(persistentUserData, "./user/usersdata.xml");
                Serializer.writeData(persistentUserDataHashes, "./user/userhashes.xml");
            }
        }

        internal static void setPersistentUserVariable(int UserID, string hash, object value)
        {
            persistencyMutex.WaitOne();

            try
            {
                int? id = getIndexFromList(hash, persistentUserData[UserID]);

                if (value == null)
                {
                    if (id.HasValue)
                    {
                        persistentUserDataHashes[UserID].RemoveAt(id.Value);
                        persistentUserData[UserID].RemoveAt(id.Value);
                        writePersistency();
                    }
                }
                else
                {
                    if(id.HasValue)
                    {
                        persistentUserData[UserID][id.Value] = value;
                    }
                    else
                    {
                        persistentUserDataHashes[UserID].Add(hash);
                        persistentUserData[UserID].Add(value);
                    }

                    writePersistency();
                }

                persistencyMutex.ReleaseMutex();
            }
            catch(Exception e)
            {
                persistencyMutex.ReleaseMutex();

                throw new Exception(e.Message, e);
            }
        }

        internal static object getPersistentUserVariable(int UserID, string hash)
        {
            object ret;
            persistencyMutex.WaitOne();

            try
            {
                int? id = getIndexFromList(hash, persistentUserData[UserID]);

                if (id.HasValue)
                {
                    ret = persistentUserData[UserID][id.Value];
                }
                else
                {
                    ret = null;
                }

                persistencyMutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                persistencyMutex.ReleaseMutex();

                throw new Exception(e.Message, e);
            }

            return ret;
        }
#endif

        private static byte[] lastHash = new byte[16];
        private static ICryptoTransform enc = null;
        private static char[] hashChars = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f',
        };

        /// <summary>
        /// generates a 128 bit AES hash
        /// </summary>
        /// <returns></returns>
        public static string generateHash()
        {
            GENERATE_NEW_HASH:
            string hash = "";

            // Chris: generate hash

            if (enc == null)
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
            
            if (getIndexFromList(hash, FileNames).HasValue)
                goto GENERATE_NEW_HASH;

            if (getIndexFromList(hash, UserHashes).HasValue)
                goto GENERATE_NEW_HASH;

            return hash;
        }

        private static int? getIndexFromList<T>(T value, List<T> list)
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

        internal static int? getUserIDFromSSID(string ssid)
        {
            return getIndexFromList(ssid, UserHashes);
        }

        internal static string getUserNameAt(int userID)
        {
            if (userID >= UserNames.Count || userID < 0)
                return null;

            return UserNames[userID];
        }
        
        internal static int getFileID(string file)
        {
            int? id = getIndexFromList(file, FileNames);

            if (id.HasValue)
                return id.Value;

            mutex.WaitOne();

            FileNames.Add(file);
            FileObjects.Add(new Dictionary<string, object>());
            id = FileNames.Count - 1;

            mutex.ReleaseMutex();

            return id.Value;
        }

        internal static Dictionary<string, object> getFileDictionary(int fileID)
        {
            return FileObjects[fileID];
        }

        internal static Dictionary<string, object> getUserDictionary(int userID)
        {
            return UserObjects[userID];
        }

        internal static Dictionary<string, object> getUserFileDictionary(int userID, int fileID)
        {
            if(FilePerUserNames.Count > userID && userID >= 0)
            {
                if(FilePerUserNames[userID].Count > fileID && fileID >= 0)
                {
                    return FilePerUserObjects[userID][fileID];
                }
                else
                {
                    throw new IndexOutOfRangeException("There is no file for the User \"" + UserNames[userID] + "\" at position " + fileID + ".");
                }
            }
            else
            {
                throw new IndexOutOfRangeException("There are not even " + (userID + 1) + " users.");
            }
        }
        
        internal static int getFilePerUserID(int userID, int fileID)
        {
            int? ID = getIndexFromList(FileNames[fileID], FilePerUserNames[userID]);

            if (ID.HasValue)
                return ID.Value;

            mutex.WaitOne();

            FilePerUserNames[userID].Add(FileNames[fileID]);
            FilePerUserObjects[userID].Add(new Dictionary<string, object>());
            ID = FilePerUserNames.Count - 1;

            mutex.ReleaseMutex();

            return ID.Value;
        }

        internal static int? getUserIDFromName(string userName)
        {
            return getIndexFromList(userName, UserNames);
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
        
        private int fileID;
        private int userFileID;

        /// <summary>
        /// The index of the current User in the database
        /// </summary>
        public int? userID { get; private set; }

        /// <summary>
        /// The SSID of the current Request
        /// </summary>
        public string ssid { get; private set; }

        /// <summary>
        /// The name of the current user (the sessionID handles this!) (the current user could by incognito due to a missing sessionID)
        /// </summary>
        public string userName;

        /// <summary>
        /// Represents the state of the current viewer of the page - true if this user has a special hash
        /// </summary>
        public bool knownUser { get; private set; }

        /// <summary>
        /// The Variables mentinoed in the HTTP head (http://www.link.com/?IamAHeadVariable)
        /// </summary>
        public List<string> varsHEAD;

        /// <summary>
        /// Cookies to set in the client browser
        /// </summary>
        public List<KeyValuePair<string, string>> Cookies = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// The Variables mentinoed in the HTTP POST packet
        /// </summary>
        public List<string> varsPOST;

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP head (they don't have to have values!) (http://www.link.com/?IamAHeadVariable=IamAHeadValue)
        /// </summary>
        public List<string> valuesHEAD;

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP POST packet (they don't have to have values!)
        /// </summary>
        public List<string> valuesPOST;

        /// <summary>
        /// the workingpath of the current server
        /// </summary>
        public string path;

        /// <summary>
        /// the currently requested file
        /// </summary>
        public string file;

        /// <summary>
        /// the raw packet sent to the server
        /// </summary>
        public string _rawPacket;

        /// <summary>
        /// The original tcpClient of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.TcpClient _tcpClient;

        /// <summary>
        /// The original networkStream of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.NetworkStream _networkStream;

        /// <summary>
        /// The EndPoint of the connected client
        /// </summary>
        public System.Net.EndPoint _remoteEndPoint;

        /// <summary>
        /// The EndPoint of the server
        /// </summary>
        public System.Net.EndPoint _localEndPoint;

        /// <summary>
        /// The cookies sent by the client to the server
        /// </summary>
        private List<KeyValuePair<string, string>> receivedCookies;

        /// <summary>
        /// The cookies sent by the client to the server
        /// </summary>
        public AVLTree<string, string> receivedCookiesTree;

        internal SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST, List<KeyValuePair<string, string>> Cookies, string path, string file, string packet, System.Net.Sockets.TcpClient client, System.Net.Sockets.NetworkStream nws)
        {
            this.varsHEAD = additionalHEAD;
            this.varsPOST = additionalPOST;
            this.valuesHEAD = valuesHEAD;
            this.valuesPOST = valuesPOST;
            this.path = path;
            this.file = file;
            this.receivedCookies = Cookies;
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
            
            fileID = SessionContainer.getFileID(file);

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.POST)
            {
                this.ssid = getHTTP_POST_Value("ssid");
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                this.ssid = receivedCookiesTree["ssid"];
            }

            this.userID = SessionContainer.getUserIDFromSSID(ssid);

            if (userID.HasValue)
            {
                knownUser = true;
                userName = SessionContainer.getUserNameAt(userID.Value);
                userFileID = SessionContainer.getFilePerUserID(userID.Value, fileID);
            }
            else
            {
                knownUser = false;
                userName = "";
            }

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
            this.userName = userName;
            knownUser = true;

            bool isNewSSID;

            ssid = SessionContainer.getSSIDforUser(userName, out isNewSSID);
            userID = SessionContainer.getUserIDFromSSID(ssid);

            userName = SessionContainer.getUserNameAt(userID.Value);
            userFileID = SessionContainer.getFilePerUserID(userID.Value, fileID);
            
            if(SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                Cookies.Add(new KeyValuePair<string, string>("ssid", ssid));

            return ssid;
        }

#if OLD_IMPLEMENTATION
        /// <summary>
        /// gets a new SSID for the current user needed for higher level security
        /// </summary>
        /// <returns>the new ssid</returns>
        public string getNextSSID(out bool isNewSSID)
        {
            if(!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            ssid = SessionContainer.getSSIDforUser(userName, out isNewSSID);

            if(SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                Cookies.Add(new KeyValuePair<string, string>("ssid", ssid));
            
            return ssid;
        }
#endif

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

        private T2 getObjectFromDictionary<T, T2>(T key, Dictionary<T, T2> dictionary)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default(T2);
        }

        private void setValueToDictionary<T, T2>(T key, T2 value, Dictionary<T, T2> dictionary)
        {
            if (value == null)
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = value;
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
            return (T)getVariable(name, scope);
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
            return (T)getGlobalVariable(name);
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

            setValueToDictionary(name, value, SessionContainer.getUserFileDictionary(userID.Value, userFileID));
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

            return getObjectFromDictionary(name, SessionContainer.getUserFileDictionary(userID.Value, userFileID));
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_AND_FILE_COMBINATION_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getUserFileVariable<T>(string name)
        {
            return (T)getUserFileVariable(name);
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

            setValueToDictionary(name, value, SessionContainer.getUserDictionary(userID.Value));
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

            return getObjectFromDictionary(name, SessionContainer.getUserDictionary(userID.Value));
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getUserVariable<T>(string name)
        {
            return (T)getUserVariable(name);
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
            setValueToDictionary(name, value, SessionContainer.getFileDictionary(fileID));
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object getFileVariable(string name)
        {
            return getObjectFromDictionary(name, SessionContainer.getFileDictionary(fileID));
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T getFileVariable<T>(string name)
        {
            return (T)getFileVariable(name);
        }

#if PERSISTENT_DATA
        /// <summary>
        /// get the value of a persistently saved variable
        /// </summary>
        /// <param name="hash">the name of the variable</param>
        /// <returns>the value of the variable</returns>
        public object getPersistentUserVariable(string hash)
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            return SessionContainer.getPersistentUserVariable(userID.Value, hash);
        }

        /// <summary>
        /// get the value of a persistently saved variable and cast it to T
        /// </summary>
        /// <param name="hash">the name of the variable</param>
        /// <returns>the value of the variable</returns>
        public T getPersistentUserVariable<T>(string hash) 
        {
            return (T)getPersistentUserVariable(hash);
        }

        /// <summary>
        /// set the value of a persistently saved variable
        /// </summary>
        /// <param name="hash">the name of the variable</param>
        /// <param name="value">the value of the variable</param>
        public void setPersistentUserVariable<T>(string hash, T value)
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            SessionContainer.setPersistentUserVariable(userID.Value, hash, value);
        }
#endif

        /// <summary>
        /// Tells if a user has ever been registered with the given name
        /// </summary>
        /// <param name="userName">the name of the user</param>
        /// <returns>true if the user has ever existed</returns>
        public bool userExists(string userName)
        {
            return getUserIndex(userName).HasValue;
        }

        /// <summary>
        /// Tells the Index of a user if he has ever been registered
        /// </summary>
        /// <param name="userName">the name of the user</param>
        /// <returns>the index or null - as close as an int? can get to null</returns>
        public int? getUserIndex(string userName)
        {
            return SessionContainer.getUserIDFromName(userName);
        }

        /// <summary>
        /// deletes the registration of the current user.
        /// </summary>
        public void unregiserUser()
        {
            if (!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");
            
            SessionContainer.forceGetNextSSID(userName);
            ssid = "";
        }
    }
}