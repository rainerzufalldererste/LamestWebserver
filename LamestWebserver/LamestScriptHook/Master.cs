using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace LamestScriptHook
{
    public class Master
    {
        public static Master master = new Master();

        private Master() { }
    }

    public static class Hook
    {
        public static string resolveScriptFromFile(string fileName, TcpClient client)
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
                        var script = CSharpScript.Create<string>(scripts[i]);
                        var del = script.CreateDelegate();

                        string output = del.Invoke().Result;

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

        public delegate string resolveScript(string fileName, Master master);
    }
}
