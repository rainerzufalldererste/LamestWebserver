using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace LamestWebserver.ScriptHook
{
    /// <summary>
    /// The main container for C#-Scripting support
    /// </summary>
    public static class Hook
    {
        /// <summary>
        /// Reads a script from local storage, executes it and replaces the definded parts of the document with the results
        /// </summary>
        /// <param name="fileName">the file to read</param>
        /// <param name="sessionData">current SessionData</param>
        /// <returns></returns>
        public static string resolveScriptFromFile(string fileName, SessionData sessionData)
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
                        Script script  = CSharpScript.Create(scripts[i], ScriptOptions.Default, typeof(SessionData));
                        Task<ScriptState> task = script.RunAsync(sessionData);

                        string output = task.Result.ReturnValue.ToString();

                        if (output == null)
                            output = "";

                        scripts[i] = output;
                    }
                    catch (Exception e)
                    {
                        int line_ = -1;
                        int char_ = -1;

                        string e_ = e.ToString();

                        try
                        {
                            if (e_.Substring(0, "Microsoft.CodeAnalysis.Scripting.CompilationErrorException: (".Length) == "Microsoft.CodeAnalysis.Scripting.CompilationErrorException: (")
                            {
                                int start = "Microsoft.CodeAnalysis.Scripting.CompilationErrorException: (".Length;
                                int k = start;
                                int length = e_.Length;
                                bool state = true;

                                for (; k < length; k++)
                                {
                                    if(state && e_[k] == ',')
                                    {
                                        int.TryParse(e_.Substring(start, k - start), out line_);
                                        state = false;
                                        start = k + 1;
                                    }
                                    else if(!state && e_[k] == ')')
                                    {
                                        int.TryParse(e_.Substring(start, k - start), out char_);
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception) { }

                        string text = e_.Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "<hr>Code:<br><br><div style='font-family:monospace;'>";

                        string[] lines = scripts[i].Replace("\r", "").Split('\n');

                        for (int j = 0; j < lines.Length; j++)
                        {
                            if (j+1 == line_ && line_ > 0 && char_ > 0)
                            {
                                if (char_ < lines[j].Length)
                                {
                                    text += "<b style='color:#BCD820;'>" + (j + 1).ToString("0000") + "|</b>    " + System.Web.HttpUtility.HtmlEncode(lines[j].Substring(0, char_ - 1)) + "<u style='color:#EC3939;font-weight: bold;'>" + System.Web.HttpUtility.HtmlEncode(lines[j].Substring(char_ - 1)) + "</u><br>";
                                }
                                else
                                {
                                    text += "<b style='color:#BCD820;'>" + (j + 1).ToString("0000") + "|</b>    " + System.Web.HttpUtility.HtmlEncode(lines[j]) + "<b style='color:#EC3939;'>_</b><br>";
                                }
                            }
                            else
                            {
                                text += "<b style='color:#507C42'>" + (j + 1).ToString("0000") + "|</b>    " + System.Web.HttpUtility.HtmlEncode(lines[j]) + "<br>";
                            }
                        }

                        text += "</div>";

                        scripts[i] = Master.GetErrorMsg("Script Error (in Script " + (i+1) + ")", text);
                        return scripts[i];
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
                return "<h2>ScriptHook Error:</h2> <br>" + e.ToString().Replace("\n", "<br>").Replace("\t", "&nbsp;&nbsp;&nbsp;") + "<br><br>Exiting";
            }
        }
    }
}
