using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.UI;
using LamestWebserver.Core.Parsing;
using LamestWebserver.UI.CachedByDefault;

namespace Demos.HelperClasses
{
    public class HCodeElement : HTextBlock
    {
        private readonly static string[] CSharpDelimiters = 
        {
            "\n", "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "single", "float",
            "double", "decimal", "char", "string", "var", "class", "struct", "delegate", "private",
            "public", "protected", "virtual", "override", "internal", "static", "const", "new ", "//",
            "for", "if", "else", "do", "while", "checked", "using", "lock", "(", ")", "{", "}", ":", "?",
            "[", "]", ",", "\\\'", "\'", "\\\"", "\"", "from ", " in ", " where ", " select ", "params", "/*",
            "*/", "base", "this", "foreach", "switch", "case", "enum", "bool", "true", "false", "null"
        };

        private readonly static string[] CSharpHighlighted =
        {
            "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "single", "float",
            "double", "decimal", "char", "string", "var", "class", "struct", "delegate", "private",
            "for", "if", "else", "do", "while", "checked", "using", "lock", "public", "protected",
            "virtual", "override", "internal", "static", "const", "new ", "from ", " in ", " where ",
            " select ", "params", "base", "this", "foreach", "switch", "case", "enum", "bool", "true",
            "false", "null"
        };

        private enum EStringState
        {
            None, InString, InChar, InComment, InMultilineComment
        }

        public HCodeElement(params string[] code) : base()
        {
            Class = "code";

            string resultingCode = "";

            foreach (string s in code)
                resultingCode += s + "\n";

            resultingCode = resultingCode.Replace("\r\n", "\n");

            var parsed = resultingCode.Parse(false, CSharpDelimiters);

            EStringState stringState = EStringState.None;

            foreach(string s in parsed)
            {
                switch (s)
                {
                    case "{":
                    case "}":
                    case "(":
                    case ")":
                    case "[":
                    case "]":
                    case ",":
                    case ":":
                        if (stringState == EStringState.None)
                        {
                            Elements.Add(new CBold(s) { Class = "symbol" });
                        }
                        else if (stringState == EStringState.InString || stringState == EStringState.InChar)
                        {
                            Elements.Add(new CItalic(s) { Class = "string" });
                        }
                        else if (stringState == EStringState.InComment || stringState == EStringState.InMultilineComment)
                        {
                            Elements.Add(new CItalic(s) { Class = "comment" });
                        }
                        break;

                    case "\"":
                        switch (stringState)
                        {
                            case EStringState.None:
                                stringState = EStringState.InString;
                                break;
                            case EStringState.InString:
                                stringState = EStringState.None;
                                break;
                        }


                        switch (stringState)
                        {
                            case EStringState.InComment:
                            case EStringState.InMultilineComment:
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            default:
                                Elements.Add(new CItalic(s) { Class = "string" });
                                break;
                        }
                        break;


                    case "\'":
                        switch (stringState)
                        {
                            case EStringState.None:
                                stringState = EStringState.InChar;
                                break;
                            case EStringState.InChar:
                                stringState = EStringState.None;
                                break;
                        }

                        switch (stringState)
                        {
                            case EStringState.InComment:
                            case EStringState.InMultilineComment:
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            default:
                                Elements.Add(new CItalic(s) { Class = "string" });
                                break;
                        }
                        break;


                    case "//":
                        switch (stringState)
                        {
                            case EStringState.None:
                            case EStringState.InComment:
                                stringState = EStringState.InComment;
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.InMultilineComment:
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.InChar:
                            case EStringState.InString:
                                Elements.Add(new CItalic(s) { Class = "string" });
                                break;
                        }

                        break;

                    case "\n":
                        if (stringState != EStringState.InMultilineComment)
                            stringState = EStringState.None;

                        Elements.Add(new HNewLine());
                        break;

                    case "/*":
                        switch (stringState)
                        {
                            case EStringState.None:
                                stringState = EStringState.InMultilineComment;
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.InMultilineComment:
                            case EStringState.InComment:
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.InChar:
                            case EStringState.InString:
                                Elements.Add(new CItalic(s) { Class = "string" });
                                break;
                        }
                        break;

                    case "*/":
                        switch (stringState)
                        {
                            case EStringState.InMultilineComment:
                                stringState = EStringState.None;
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.InComment:
                                Elements.Add(new CItalic(s) { Class = "comment" });
                                break;

                            case EStringState.None:
                                Elements.Add(new CString(s));
                                break;

                            case EStringState.InChar:
                            case EStringState.InString:
                                Elements.Add(new CItalic(s) { Class = "string" });
                                break;
                        }
                        break;

                    default:
                        if (stringState == EStringState.None)
                        {
                            if (CSharpHighlighted.Contains(s))
                                Elements.Add(new CBold(s) { Class = "highlight" });
                            else
                                Elements.Add(new CString(s));
                        }
                        else if (stringState == EStringState.InString || stringState == EStringState.InChar)
                        {
                            Elements.Add(new CItalic(s) { Class = "string" });
                        }
                        else if (stringState == EStringState.InComment || stringState == EStringState.InMultilineComment)
                        {
                            Elements.Add(new CItalic(s) { Class = "comment" });
                        }
                        break;
                }
            }
        }
    }
}
