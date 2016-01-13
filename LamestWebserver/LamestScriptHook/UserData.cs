using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LamestScriptHook
{
    public class UserData
    {
        public const string INVALIDNAME = "";
        public string name;
        public string RANK;
        public IPAddress ipaddress;
        public DateTime loginTime;

        public List<AssocByFileUserData> associatedData;

        public UserData(string name, string RANK, IPAddress address, DateTime loginTime)
        {
            this.name = name;
            this.RANK = RANK;
            this.ipaddress = address;
            this.loginTime = loginTime;

            associatedData = new List<AssocByFileUserData>() { new AssocByFileUserData(INVALIDNAME) };
        }

        public AssocByFileUserData getFileData(string file)
        {
            for (int i = 0; i < associatedData.Count; i++)
            {
                if (associatedData[i].file == file)
                    return associatedData[i];
            }

            return null;
        }
    }

    public class AssocByFileUserData
    {
        public string file { get; private set; }
        public List<string> hashes { get; private set; }
        public List<object> datas { get; private set; }

        public AssocByFileUserData(string filename)
        {
            file = filename;
            hashes = new List<string>();
            datas = new List<object>();
        }

        public object getData(string hash)
        {
            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == hash)
                    return datas[i];
            }

            return null;
        }

        public void setData(string hash, object data)
        {
            for (int i = 0; i < hashes.Count; i++)
            {
                if (hashes[i] == hash)
                {
                    datas[i] = data;
                    return;
                }
            }

            hashes.Add(hash);
            datas.Add(data);
        }
    }
}
