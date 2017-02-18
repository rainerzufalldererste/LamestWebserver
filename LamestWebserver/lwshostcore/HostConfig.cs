using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.Serialization;

namespace lwshostcore
{
    [Serializable]
    public class HostConfig
    {
        public string[] BinaryDirectories = new string[0];
        public int[] Ports = new[] {80};
        public string WebserverFileDirectory = "lwshost\\web";
        public SessionContainer.ESessionIdRereferencingMode SessionIdRereferencingMode = SessionContainer.ESessionIdRereferencingMode.Keep;
        public SessionContainer.ESessionIdTransmissionType SessionIdTransmissionType = SessionContainer.ESessionIdTransmissionType.Cookie;
        public int MaxUserCount = 256;
        public int UserHashMapSize = 128;
        public int UserVariableStorageHashMapSize = 512;
        public int PageResponseStorageHashMapSize = 256;
        public int OneTimePageResponsesStorageQueueSize = 4096;
        public int WebSocketResponsePageStorageHashMapSize = 64;
        public int DirectoryResponseStorageHashMapSize = 128;

        private const string configFile = "lwshost\\lwsconfig.json";
        
        private static HostConfig _currentConfig = null;

        public static HostConfig CurrentHostConfig
        {
            get
            {
                if (_currentConfig != null)
                    return _currentConfig;

                try
                {
                    _currentConfig = Serializer.ReadJsonData<HostConfig>(configFile);

                    try
                    {
                        Serializer.WriteJsonData(_currentConfig, configFile, true);
                    }
                    catch
                    {

                    }

                    return _currentConfig;
                }
                catch
                {
                    _currentConfig = new HostConfig();

                    try
                    {
                        Serializer.WriteJsonData(_currentConfig, configFile, true);
                    }
                    catch
                    {
                        
                    }

                    return _currentConfig;
                }
            }
        }

        public void ApplyConfig()
        {
            SessionContainer.MaxUsers = MaxUserCount;
            ServerHandler.LogMessage($"[hostconfig] SessionContainer.MaxUsers = {MaxUserCount}");

            SessionContainer.SessionIdRereferencingMode = SessionIdRereferencingMode;
            ServerHandler.LogMessage($"[hostconfig] SessionContainer.SessionIdRereferencingMode = {SessionIdRereferencingMode}");

            SessionContainer.UserHashMapSize = UserHashMapSize;
            ServerHandler.LogMessage($"[hostconfig] SessionContainer.UserHashMapSize = {UserHashMapSize}");

            SessionContainer.UserVariableHashMapSize = UserVariableStorageHashMapSize;
            ServerHandler.LogMessage($"[hostconfig] SessionContainer.UserVariableHashMapSize = {UserVariableStorageHashMapSize}");

            SessionContainer.SessionIdTransmissionType = SessionIdTransmissionType;
            ServerHandler.LogMessage($"[hostconfig] SessionContainer.SessionIdTransmissionType = {SessionIdTransmissionType}");


            WebServer.PageResponseStorageHashMapSize = PageResponseStorageHashMapSize;
            ServerHandler.LogMessage($"[hostconfig] WebServer.PageResponseStorageHashMapSize = {PageResponseStorageHashMapSize}");

            WebServer.OneTimePageResponsesStorageQueueSize = OneTimePageResponsesStorageQueueSize;
            ServerHandler.LogMessage($"[hostconfig] WebServer.OneTimePageResponsesStorageQueueSize = {OneTimePageResponsesStorageQueueSize}");

            WebServer.WebSocketResponsePageStorageHashMapSize = WebSocketResponsePageStorageHashMapSize;
            ServerHandler.LogMessage($"[hostconfig] WebServer.WebSocketResponsePageStorageHashMapSize = {WebSocketResponsePageStorageHashMapSize}");

            WebServer.DirectoryResponseStorageHashMapSize = DirectoryResponseStorageHashMapSize;
            ServerHandler.LogMessage($"[hostconfig] WebServer.DirectoryResponseStorageHashMapSize = {DirectoryResponseStorageHashMapSize}");

        }
    }
}
