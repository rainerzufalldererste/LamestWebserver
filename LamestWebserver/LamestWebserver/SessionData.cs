using System;
using System.Collections.Generic;
using LamestWebserver.Collections;

namespace LamestWebserver
{
    /// <summary>
    /// Contains all Session dependent Information
    /// </summary>
    public class SessionData : AbstractSessionIdentificator
    {
        /// <summary>
        /// The Variables mentioned in the HTTP head (http://www.link.com/?IamAHeadVariable)
        /// </summary>
        public List<string> HttpHeadParameters { get; private set; }

        /// <summary>
        /// Cookies to set in the client browser
        /// </summary>
        public List<KeyValuePair<string, string>> SetCookies = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// The Variables mentinoed in the HTTP POST packet
        /// </summary>
        public List<string> HttpPostParameters { get; private set; }

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP head (they don't have to have values!) (http://www.link.com/?IamAHeadVariable=IamAHeadValue)
        /// </summary>
        public List<string> HttpHeadValues { get; private set; }

        /// <summary>
        /// The Values of the Variables mentinoed in the HTTP POST packet (they don't have to have values!)
        /// </summary>
        public List<string> HttpPostValues { get; private set; }

        /// <summary>
        /// the workingpath of the current server
        /// </summary>
        public string ServerWorkingPath { get; private set; }

        /// <summary>
        /// the raw packet sent to the server
        /// </summary>
        public string RawHttpPacket { get; private set; }

        /// <summary>
        /// The original tcpClient of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.TcpClient ClientTpcConnection { get; private set; }

        /// <summary>
        /// The original networkStream of the server. Handle with care.
        /// </summary>
        public System.Net.Sockets.NetworkStream ServerNetworkStream { get; private set; }

        /// <summary>
        /// The EndPoint of the connected client
        /// </summary>
        public System.Net.EndPoint ClientEndPoint { get; private set; }

        /// <summary>
        /// The EndPoint of the server
        /// </summary>
        public System.Net.EndPoint ServerEndPoint { get; private set; }

        /// <summary>
        /// The cookies sent by the client to the server
        /// </summary>
        public AVLTree<string, string> Cookies { get; private set; }

        internal SessionData(List<string> additionalHEAD, List<string> additionalPOST, List<string> valuesHEAD, List<string> valuesPOST, List<KeyValuePair<string, string>> Cookies,
            string path, string file, string packet, System.Net.Sockets.TcpClient client, System.Net.Sockets.NetworkStream nws, ushort port)
        {
            this.HttpHeadParameters = additionalHEAD;
            this.HttpPostParameters = additionalPOST;
            this.HttpHeadValues = valuesHEAD;
            this.HttpPostValues = valuesPOST;
            this.ServerWorkingPath = path;
            base.RequestedFile = file;
            base.Port = port;

            this.Cookies = new AVLTree<string, string>();

            if (Cookies != null)
            {
                foreach (KeyValuePair<string, string> kvp in Cookies)
                {
                    this.Cookies.Add(kvp);
                }
            }

            this.RawHttpPacket = packet;
            this.ClientTpcConnection = client;
            this.ServerNetworkStream = nws;

            this.ClientEndPoint = ClientTpcConnection?.Client.RemoteEndPoint;
            this.ServerEndPoint = ClientTpcConnection?.Client.LocalEndPoint;

            if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.HttpPost)
            {
                base.Ssid = GetHttpPostValue("ssid");
            }
            else if (SessionContainer.SessionIdTransmissionType == SessionContainer.ESessionIdTransmissionType.Cookie)
            {
                base.Ssid = this.Cookies["ssid"];
            }

            base.PerFileVariables = SessionContainer.GetFileDictionary(file);
            this._userInfo = SessionContainer.GetUserInfoFromSsid(Ssid);

            CurrentSession = this;
        }

        /// <summary>
        /// get the value of a HTTP HEAD variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP HEAD variable</param>
        /// <returns>the value of the given HTTP HEAD variable (or null if not existent)</returns>
        public string GetHttpHeadValue(string name)
        {
            for (int i = 0; i < HttpHeadParameters.Count; i++)
            {
                if (name == HttpHeadParameters[i])
                    return HttpHeadValues[i];
            }

            return null;
        }

        /// <summary>
        /// get the value of a HTTP POST variable by name. (null if not existent)
        /// </summary>
        /// <param name="name">The name of the HTTP POST variable</param>
        /// <returns>the value of the given HTTP POST variable (or null if not existent)</returns>
        public string GetHttpPostValue(string name)
        {
            for (int i = 0; i < HttpPostParameters.Count; i++)
            {
                if (name == HttpPostParameters[i])
                    return HttpPostValues[i];
            }

            return null;
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