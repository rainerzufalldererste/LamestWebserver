using System;
using System.Collections.Generic;
using System.Threading;
using System.Security.Cryptography;

namespace LameNetHook
{
    internal static class SessionContainer
    {
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
        /// <param name="user"></param>
        /// <returns></returns>
        public static string getSSIDforUser(string user)
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

                mutex.ReleaseMutex();
            }

            UserHashes[userID.Value] = generateHash();

            return UserHashes[userID.Value];
        }

        private static byte[] lastHash = new byte[16];
        private static ICryptoTransform enc = null;
        private static char[] hashChars = {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f',
        };

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

    public class SessionData
    {
        private int? userID;
        private int fileID;
        private int userFileID;

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

        public SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST, string path, string file)
        {
            this.varsHEAD = additionalHEAD;
            this.varsPOST = additionalPOST;
            this.valuesHEAD = valuesHEAD;
            this.valuesPOST = valuesPOST;
            this.path = path;
            this.file = file;

            fileID = SessionContainer.getFileID(file);

            this.ssid = getHTTP_POST_value("ssid");
            this.userID = SessionContainer.getUserIDFromSSID(ssid);

            if (userID.HasValue)
            {
                knownUser = true;
                userName = SessionContainer.getUserNameAt(userID.Value);
                
                // the user keeps his session id so that mutiple tabs are possible...

                userFileID = SessionContainer.getFilePerUserID(userID.Value, fileID);
            }
            else
            {
                knownUser = false;
                userName = "";
            }
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
        public string getHTTP_POST_value(string name)
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
            return ssid = SessionContainer.getSSIDforUser(userName);
        }

        /// <summary>
        /// gets a new SSID for the current user needed for higher security variants (multiple tabs are not possible!)
        /// </summary>
        /// <returns>the new ssid</returns>
        public string getNextSSID()
        {
            if(!knownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            return ssid = SessionContainer.getSSIDforUser(userName);
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        private T2 getObjectFromDictionary<T, T2>(T key, Dictionary<T, T2> dictionary)
        {
            return dictionary[key];
        }

        private void setValueToDictionary<T, T2>(T key, T2 value, Dictionary<T, T2> dictionary)
        {
            dictionary[key] = value;
        }

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        public enum EVariableScope : byte
        {
            File, User, FileAndUser, Global
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

            setValueToDictionary(name, value, SessionContainer.getUserFileDictionary(userID.Value, fileID));
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

            return getObjectFromDictionary(name, SessionContainer.getUserFileDictionary(userID.Value, fileID));
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
    }
}