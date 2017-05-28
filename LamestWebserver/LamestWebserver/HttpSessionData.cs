using System;
using System.Collections.Generic;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    /// <summary>
    /// Contains all Session dependent Information
    /// </summary>
    public class HttpSessionData : SessionData
    {
        /// <summary>
        /// Cookies to set in the client browser
        /// </summary>
        public List<KeyValuePair<string, string>> SetCookies = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// the raw packet sent to the server
        /// </summary>
        public string RawHttpPacket { get; private set; }

        /// <summary>
        /// The cookies sent by the client to the server
        /// </summary>
        public AVLTree<string, string> Cookies { get; private set; }

        internal HttpSessionData(HttpRequest httpPacket)
        {
            base.HttpHeadVariables = httpPacket.VariablesHttpHead;
            base.HttpPostVariables = httpPacket.VariablesHttpPost;
            base.RequestedFile = httpPacket.RequestUrl;

            this.Cookies = new AVLTree<string, string>();

            if (httpPacket.Cookies != null)
            {
                foreach (KeyValuePair<string, string> kvp in httpPacket.Cookies)
                {
                    this.Cookies.Add(kvp);
                }
            }

            this.RawHttpPacket = httpPacket.RawRequest;

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.HttpPost)
            {
                base.Ssid = GetHttpPostValue("ssid");
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                base.Ssid = this.Cookies["ssid"];
            }

            base.PerFileVariables = SessionContainer.GetFileDictionary(httpPacket.RequestUrl);
            this._userInfo = SessionContainer.GetUserInfoFromSsid(Ssid);

            CurrentSession = this;
        }

        /// <summary>
        /// Registers the user and assigns a SSID
        /// </summary>
        /// <param name="userName">the User to register</param>
        /// <returns>the SSID for the user</returns>
        public string RegisterUser(string userName)
        {
            bool isNewSSID;

            Ssid = SessionContainer.GetSSIDforUser(userName, out isNewSSID, out _userInfo);
            
            if(SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                SetCookies.Add(new KeyValuePair<string, string>("ssid", Ssid));

            return Ssid;
        }

        /// <summary>
        /// _FORCES_ to get a new SSID for the current user if needed for higher level security (call before building the site)
        /// </summary>
        /// <returns>the new ssid</returns>
        public string ForceGetNextSsid()
        {
            if (!KnownUser)
                throw new Exception("The current user is unknown. Please check for SessionData.knownUser before calling this method.");

            Ssid = SessionContainer.ForceGetNextSSID(UserName);

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                SetCookies.Add(new KeyValuePair<string, string>("ssid", Ssid));

            return Ssid;
        }


        
        /// <summary>
        /// deletes the registration of the current user.
        /// </summary>
        public void LogoutUser()
        {
            if (KnownUser)
            {
                SessionContainer.ForceGetNextSSID(UserName);
                Ssid = "";

                if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                    SetCookies.Add(new KeyValuePair<string, string>("ssid", ""));
            }
        }
    }
}