using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;

namespace lwshostcore
{
    [Serializable]
    public class HostConfig
    {
        public string[] BinaryDirectories = new string[0];
        public int[] Ports = new[] {80};
        public string WebserverFileDirectory = "\\web";

        private const string configFile = "lwsconfig.json";


        private static HostConfig _currentConfig = null;

        public static HostConfig CurrentHostConfig
        {
            get
            {
                if (_currentConfig != null)
                    return _currentConfig;

                try
                {
                    _currentConfig = Serializer.getJsonData<HostConfig>(configFile);
                    return _currentConfig;
                }
                catch (Exception e)
                {
                    _currentConfig = new HostConfig();

                    try
                    {
                        Serializer.writeJsonData(_currentConfig, configFile);
                    }
                    catch (Exception)
                    {
                        
                    }

                    return _currentConfig;
                }
            }
        }
    }
}
