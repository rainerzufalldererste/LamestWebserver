using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;
using LamestWebserver.Core;

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

        /// <summary>
        /// The maximum count of users being online at one time
        /// </summary>
        public static int MaxUsers = 256;

        private static UsableMutex mutex = new UsableMutex();

        private static AVLTree<string, AVLTree<string, object>> PerFileObjects = new AVLTree<string, AVLTree<string, object>>();
        
        private static AVLHashMap<string, UserInfo> UserInfos = new AVLHashMap<string, UserInfo>(UserHashMapSize);
        private static AVLHashMap<string, UserInfo> UserInfosByName = new AVLHashMap<string, UserInfo>(UserHashMapSize);

        internal static AVLTree<string, object> GlobalVariables = new AVLTree<string, object>();

        internal class UserInfo
        {
            /// <summary>
            /// The utc time this info was retrieved the last time.
            /// </summary>
            internal DateTime lastPullUtcTime = DateTime.UtcNow;

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
        /// <param name="userInfo">the UserInfo of the retrieved User</param>
        /// <returns></returns>
        internal static string GetSSIDforUser(string user, out bool isNewSSID, out UserInfo userInfo)
        {
            string hash = UserInfosByName[user]?.ID;

            if (hash == null)
            {
                userInfo = new UserInfo();
                userInfo.ID = GenerateUnusedHash();
                hash = userInfo.ID;
                userInfo.UserName = user;

                mutex.WaitOne();

                UserInfos.Add(userInfo.ID, userInfo);
                UserInfosByName.Add(userInfo.UserName, userInfo);

                UserCleanup();

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
                    userInfo.ID = GenerateUnusedHash();
                    UserInfos[userInfo.ID] = userInfo;

                    UserCleanup();

                    mutex.ReleaseMutex();

                    isNewSSID = true;
                    hash = userInfo.ID;
                }
                else if (SessionIdRereferencingMode == ESessionIdRereferencingMode.Keep && UserInfosByName[user].ID == null)
                {
                    mutex.WaitOne();

                    userInfo = UserInfosByName[user];
                    UserInfos.Remove(userInfo.ID);
                    userInfo.ID = GenerateUnusedHash();
                    UserInfos[userInfo.ID] = userInfo;

                    UserCleanup();

                    mutex.ReleaseMutex();

                    isNewSSID = true;
                    hash = userInfo.ID;
                }
                else
                {
                    userInfo = UserInfosByName[user];

                    if(userInfo != null)
                        userInfo.lastPullUtcTime = DateTime.UtcNow;
                }
            }

            return hash;
        }

        private static void UserCleanup()
        {
            if (UserInfos.Count > MaxUsers)
            {
                DateTime oldestTime = DateTime.UtcNow;
                string oldestIndex = null;

                foreach (var userInfoPair in UserInfos)
                {
                    if (userInfoPair.Value.lastPullUtcTime < oldestTime)
                    {
                        oldestTime = userInfoPair.Value.lastPullUtcTime;
                        oldestIndex = userInfoPair.Key;
                    }
                }

                if (oldestIndex != null)
                {
                    UserInfosByName.Remove(UserInfos[oldestIndex].UserName);
                    UserInfos.Remove(oldestIndex);
                }
            }
        }

        internal static string ForceGetNextSSID(string user)
        {
            string hash = UserInfosByName[user]?.ID;

            if (hash == null)
            {
                UserInfo info = new UserInfo();
                info.ID = GenerateUnusedHash();
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
                info.ID = GenerateUnusedHash();
                UserInfos[info.ID] = info;

                mutex.ReleaseMutex();
                
                hash = info.ID;
            }

            return hash;
        }

        /// <summary>
        /// generates a 128 bit AES hash that is not used in pagenames
        /// </summary>
        /// <returns></returns>
        public static string GenerateUnusedHash()
        {
            GENERATE_NEW_HASH:
            string hash = Hash.GetHash();

            // Chris: if(hash already exists in any hash list) {goto GENERATE_NEW_HASH;}
            
            if (UserInfos.ContainsKey(hash))
                goto GENERATE_NEW_HASH;

            if (PerFileObjects.ContainsKey(hash))
                goto GENERATE_NEW_HASH;

            return hash;
        }

        internal static UserInfo GetUserInfoFromSsid(string ssid)
        {
            var userInfo = UserInfos[ssid];

            if(userInfo != null)
                userInfo.lastPullUtcTime = DateTime.UtcNow;

            return userInfo;
        }

        internal static object GetUserInfoFromName(string userName)
        {
            return UserInfosByName[userName];
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
    }
}