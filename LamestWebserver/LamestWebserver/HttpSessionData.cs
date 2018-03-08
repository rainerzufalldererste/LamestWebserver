using System;
using System.Collections.Generic;
using LamestWebserver.Collections;
using LamestWebserver.Core;
using System.IO;
using System.Net;

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

        /// <summary>
        /// HttpPacket of the original Request.
        /// </summary>
        public HttpRequest HttpPacket { get; private set; }

        /// <summary>
        /// Stream of the original Request.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// The remoteEndpoint (if any) of the original Request.
        /// </summary>
        public EndPoint RemoteEndpoint;

        /// <summary>
        /// The remoteEndpoint (if any) of the original Request.
        /// </summary>
        public EndPoint LocalEndpoint;

        internal HttpSessionData(HttpRequest httpPacket)
        {
            base.HttpHeadVariables = httpPacket.VariablesHttpHead;
            base.HttpPostVariables = httpPacket.VariablesHttpPost;
            base.RequestedFile = httpPacket.RequestUrl;

            this.Cookies = new AVLTree<string, string>();
            this.HttpPacket = httpPacket;
            this.Stream = httpPacket.Stream;

            try
            {
                this.RemoteEndpoint = httpPacket.TcpClient?.Client?.RemoteEndPoint;
                this.LocalEndpoint = httpPacket.TcpClient?.Client?.LocalEndPoint;
            }
            catch { }

            if (httpPacket.Cookies != null)
            {
                foreach (KeyValuePair<string, string> kvp in httpPacket.Cookies)
                {
                    this.Cookies.Add(kvp);
                }
            }

            this.RawHttpPacket = httpPacket.RawRequest;

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
                base.Ssid = this.Cookies["ssid"];
            else
                Logger.LogExcept(new NotImplementedException($"The given SessionIdTransmissionType ({SessionContainer.SessionIdTransmissionType}) could not be handled in {GetType().ToString()}."));

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