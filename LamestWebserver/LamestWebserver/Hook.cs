using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using LamestWebserver;

namespace LamestScriptHook
{
    public static class Hook
    {
        public static string resolveScriptFromFile(string fileName, TcpClient client, HTTP_Packet packet, string shortFileName, UserData user, AssocByFileUserData globalData)
        {
            try
            {
                string completeFile = System.IO.File.ReadAllText(fileName);

                List<string> scripts = new List<string>();
                List<int> scriptPositions = new List<int>();

                for (int i = 0; i < completeFile.Length - 6; i++)
                {
                    if(completeFile[i] == '<' && completeFile[i+1] == '?' && completeFile.Substring(i + 2, 2) == "cs")
                    {
                        for (int j = i + 4; j < completeFile.Length - 1; j++)
                        {
                            if (completeFile[j] == '?' && completeFile[j + 1] == '>')
                            {
                                int size = j - i;

                                scripts.Add(completeFile.Substring(i + 4, j - i - 5));
                                scriptPositions.Add(i);
                                completeFile = completeFile.Remove(i, j - i + 2);
                                i--;
                                break;
                            }
                        }
                    }
                }

                for (int i = 0; i < scripts.Count; i++)
                {
                    try
                    {
                        AssocByFileUserData aud = null;

                        aud = user?.getFileData(fileName);

                        if (aud == null)
                        {
                            aud = new AssocByFileUserData(fileName);

                            if (user != null)
                                user.associatedData.Add(aud);
                        }

                        DataObject data = new DataObject(client, packet, fileName, shortFileName, user, aud, globalData);
                        var script = CSharpScript.Create(scripts[i], ScriptOptions.Default, typeof(DataObject));
                        var task = script.RunAsync(data);
                        ///var del = script.CreateDelegate();

                        // del.Invoke(data);

                        //var output = del.Invoke().Result;
                        string output = task.Result.ReturnValue.ToString();

                        for (int j = 0; j < task.Result.Variables.Length; j++)
                        {
                            if(task.Result.Variables[j].Name.Length > 0 && task.Result.Variables[j].Name[0] == '@')
                            {
                                aud.setData(task.Result.Variables[j].Name, task.Result.Variables[j].Value);
                            }
                        }

                        if (output == null)
                            output = "";

                        scripts[i] = output;
                    }
                    catch (Exception e)
                    {
                        scripts[i] = "<b>Script Error (in Script " + (i+1) + "):</b> <br>" + e.ToString().Replace("\n", "<br>") + "<br><br>Exiting";
                        break;
                    }
                }

                for (int i = scripts.Count - 1; i >= 0; i--)
                {
                    completeFile = completeFile.Insert(scriptPositions[i], scripts[i]);
                }

                return completeFile;
            }
            catch(Exception e)
            {
                return "<b>ScriptHook Error:</b> <br>" + e.ToString().Replace("\n", "<br>") + "<br><br>Exiting";
            }
        }
    }

    public class DataObject
    {
        public IPAddress RemoteIP;
        public UserData user;
        public TcpClient client;
        public string fileName;
        public AssocByFileUserData globalData;
        public HTTP_Packet packet;
        public string shortFileName;

        private List<object> values;
        private List<string> names;

        private List<object> gvalues;
        private List<string> gnames;

        public DataObject(TcpClient client, HTTP_Packet packet, string fileName, string shortFileName, UserData user, AssocByFileUserData userThisFileData, AssocByFileUserData globalData)
        {
            this.client = client;
            this.packet = packet;
            this.fileName = fileName;
            this.shortFileName = shortFileName;
            this.user = user;
            this.globalData = globalData;

            this.RemoteIP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;

            values = userThisFileData.datas;
            names = userThisFileData.hashes;

            gvalues = globalData.datas;
            gnames = globalData.hashes;
        }


        // file local vars

        public bool varExists(string name)
        {
            return varExistsInCollection(name, names);
        }

        public object getVar(string name, object nullparam)
        {
            return getVar<object>(name);
        }

        public T getVar<T>(string name)
        {
            return getVarFromCollection<T>(name, names, values);
        }

        public void setVar(string name, object value)
        {
            setVar<object>(name, value);
        }

        public T setVar<T>(string name, object value)
        {
            return setVarFromCollection<T>(name, value, names, values);
        }


        // global vars
        public bool gvarExists(string name)
        {
            return varExistsInCollection(name, gnames);
        }

        public object getGVar(string name, object nullparam)
        {
            return getGVar<object>(name);
        }

        public T getGVar<T>(string name)
        {
            return getVarFromCollection<T>(name, gnames, gvalues);
        }

        public void setGVar(string name, object value)
        {
            setGVar<object>(name, value);
        }

        public T setGVar<T>(string name, object value)
        {
            return setVarFromCollection<T>(name, value, gnames, gvalues);
        }


        // private prototypes

        private T setVarFromCollection<T>(string name, object value, List<string> names, List<object> values)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                {
                    values[i] = value;
                    return (T)value;
                }
            }

            names.Add(name);
            values.Add(value);
            return (T)value;
        }

        private T getVarFromCollection<T>(string name, List<string> names, List<object> values)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                {
                    return (T)values[i];
                }
            }

            throw new Exception("Variable Not Stored Yet.");
        }
        private bool varExistsInCollection(string name, List<string> names)
        {
            return names.Contains(name);
        }
    }
}
