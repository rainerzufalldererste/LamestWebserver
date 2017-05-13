﻿using System;
using System.Collections.Generic;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    /// <summary>
    /// Contains the current SessionData
    /// </summary>
    public abstract class SessionData
    {
        internal IDictionary<string, object> PerFileVariables;
        internal SessionContainer.UserInfo _userInfo;
        
        /// <summary>
        /// contains the current session identificator for this thread
        /// </summary>
        [ThreadStatic]
        public static SessionData CurrentSession = null;

        /// <summary>
        /// The name of the current user (the sessionID handles this!) (the current user could by incognito due to a missing sessionID)
        /// </summary>
        public string UserName => _userInfo?.UserName;

        /// <summary>
        /// Represents the state of the current viewer of the page - true if this user has a special hash
        /// </summary>
        public bool KnownUser { get { return _userInfo != null; } }

        /// <summary>
        /// The SSID of the current Request
        /// </summary>
        public string Ssid { get; protected set; }

        /// <summary>
        /// the currently requested file
        /// </summary>
        public string RequestedFile { get; protected set; }

        /// <summary>
        /// The Variables mentioned in the HTTP head (http://www.link.com/?IamAHeadVariable=IamTheCorrespondingValue)
        /// </summary>
        public AVLTree<string, string> HttpHeadVariables { get; protected set; } = new AVLTree<string, string>();

        /// <summary>
        /// The Variables mentinoed in the HTTP POST packet (they don't have to have values!)
        /// </summary>
        public AVLTree<string, string> HttpPostVariables { get; protected set; } = new AVLTree<string, string>();

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        /// <summary>
        /// get the value of a HTTP HEAD variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP HEAD variable</param>
        /// <returns>the value of the given HTTP HEAD variable (or null if not existent)</returns>
        public string GetHttpHeadValue(string name) => HttpHeadVariables[name];

        /// <summary>
        /// get the value of a HTTP POST variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP POST variable</param>
        /// <returns>the value of the given HTTP POST variable (or null if not existent)</returns>
        public string GetHttpPostValue(string name) => HttpPostVariables[name];

        // ===============================================================================================================================================
        // ===============================================================================================================================================

        private T2 GetObjectFromDictionary<T, T2>(T key, IDictionary<T, T2> IDictionary)
        {
            return IDictionary.ContainsKey(key) ? IDictionary[key] : default(T2);
        }

        private void SetValueToDictionary<T, T2>(T key, T2 value, IDictionary<T, T2> IDictionary)
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
        /// Get the value of a variable defined at a certain scope by name
        /// </summary>
        /// <param name="name">name of the variable</param>
        /// <param name="scope">scope at which the variable is defined</param>
        /// <returns>returns the value of the variable (or null if not existent or exception if you really want to mess things up, dude!)</returns>
        public object GetVariable(string name, EVariableScope scope)
        {
            switch (scope)
            {
                case EVariableScope.File:
                    return GetFileVariable(name);

                case EVariableScope.User:
                    return GetUserVariable(name);

                case EVariableScope.FileAndUser:
                    return GetUserFileVariable(name);

                case EVariableScope.Global:
                    return GetGlobalVariable(name);

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
        public T GetVariable<T>(string name, EVariableScope scope)
        {
            object o = GetVariable(name, scope);

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
        public void SetVariable<T>(string name, T value, EVariableScope scope)
        {
            switch (scope)
            {
                case EVariableScope.File:
                    SetFileVariable(name, value);
                    break;

                case EVariableScope.User:
                    SetUserVariable(name, value);
                    break;

                case EVariableScope.FileAndUser:
                    SetUserFileVariable(name, value);
                    break;

                case EVariableScope.Global:
                    SetGlobalVariable(name, value);
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
        public void SetGlobalVariable<T>(string name, T value)
        {
            SetValueToDictionary(name, value, SessionContainer.GlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally (available from everywhere on this server)
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object GetGlobalVariable(string name)
        {
            return GetObjectFromDictionary(name, SessionContainer.GlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally (available from everywhere on this server) and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T GetGlobalVariable<T>(string name)
        {
            object o = GetGlobalVariable(name);

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
        public void SetUserFileVariable<T>(string name, T value)
        {
            if (!KnownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            SetValueToDictionary(name, value, _userInfo.PerFileVariables[RequestedFile]);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_AND_FILE_COMBINATION_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object GetUserFileVariable(string name)
        {
            if (!KnownUser)
                return null;

            return GetObjectFromDictionary(name, _userInfo.PerFileVariables[RequestedFile]);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_AND_FILE_COMBINATION_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T GetUserFileVariable<T>(string name)
        {
            object o = GetUserFileVariable(name);

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
        public void SetUserVariable<T>(string name, T value)
        {
            if (!KnownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            SetValueToDictionary(name, value, _userInfo.UserGlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object GetUserVariable(string name)
        {
            if (!KnownUser)
                return null;

            return GetObjectFromDictionary(name, _userInfo.UserGlobalVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _USER_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T GetUserVariable<T>(string name)
        {
            object o = GetUserVariable(name);

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
        public void SetFileVariable<T>(string name, T value)
        {
            SetValueToDictionary(name, value, PerFileVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_
        /// </summary>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public object GetFileVariable(string name)
        {
            return GetObjectFromDictionary(name, PerFileVariables);
        }

        /// <summary>
        /// get the value (or null if not existent) from the variables saved globally for the current _FILE_ and casts it to a specified Type T
        /// </summary>
        /// <typeparam name="T">The type T to cast the value to</typeparam>
        /// <param name="name">the name of the variable</param>
        /// <returns>the value of the variable (or null if not existent)</returns>
        public T GetFileVariable<T>(string name)
        {
            object o = GetFileVariable(name);

            if (o == null)
                return default(T);

            return (T)o;
        }

        /// <summary>
        /// Tells if a user has ever been registered with the given name
        /// </summary>
        /// <param name="userName">the name of the user</param>
        /// <returns>true if the user has ever existed</returns>
        public bool UserExists(string userName)
        {
            return SessionContainer.GetUserInfoFromName(userName) != null;
        }


        /// <summary>
        /// Retrieves a collection of all global variables.
        /// </summary>
        /// <returns>a collection of all global variables.</returns>
        public AVLTree<string, object> GetGlobalVariables()
        {
            return SessionContainer.GlobalVariables;
        }

        /// <summary>
        /// Retrieves a collection of the per file user variables.
        /// </summary>
        /// <returns>a collection of the per file user variables.</returns>
        public IDictionary<string, object> GetUserPerFileVariables()
        {
            return _userInfo.PerFileVariables[RequestedFile];
        }

        /// <summary>
        /// Retrieves a collection of the per file variables.
        /// </summary>
        /// <returns>a collection of the per file user variables.</returns>
        public IDictionary<string, object> GetPerFileVariables()
        {
            return PerFileVariables;
        }
    }

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
    /// A slim variant of SessionData used in WebSockets
    /// </summary>
    public class SessionIdentificatorSlim : SessionData
    {
        internal SessionIdentificatorSlim(string file, string sessionId)
        {
            base.RequestedFile = file;
            base.Ssid = sessionId;

            base.PerFileVariables = SessionContainer.GetFileDictionary(file);
            this._userInfo = SessionContainer.GetUserInfoFromSsid(Ssid);

            CurrentSession = this;
        }
    }

}
