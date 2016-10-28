using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver
{
    public class JScript
    {
        public List<IJSPiece> pieces = new List<IJSPiece>();

        public JScript()
        {

        }

        public JScript(params IJSPiece[] pieces)
        {
            this.pieces = pieces.ToList();
        }

        public JScript(List<IJSPiece> pieces)
        {
            this.pieces = pieces;
        }

        public void appendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }
    }

    public interface IJSPiece
    {
        string getCode(SessionData sessionData);
    }

    public class JSFunction : IJSValue
    {
        public List<IJSPiece> pieces = new List<IJSPiece>();
        public List<IJSValue> parameters = new List<IJSValue>();

        public string content { get; set; }

        public JSFunction(string name, List<IJSValue> parameters)
        {
            if (String.IsNullOrWhiteSpace(name))
                this.content = SessionContainer.generateHash();

            this.parameters = parameters;
        }

        public JSFunction(List<IJSValue> parameters)
        {
            this.content = SessionContainer.generateHash();
            this.parameters = parameters;
        }

        public JSFunction(params IJSValue[] parameters)
        {
            this.content = SessionContainer.generateHash();
            this.parameters = parameters.ToList();
        }

        public JSFunction(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                this.content = SessionContainer.generateHash();
        }

        public JSFunction()
        {
            this.content = SessionContainer.generateHash();
        }

        public void appendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }

        public string getCode(SessionData sessionData)
        {
            string ret = "function " + content + " ( ";

            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += parameters[i].content;
            }

            ret += ") {\n";

            for (int i = 0; i < pieces.Count; i++)
			{
                ret += pieces[i].getCode(sessionData) + "\n";
			}

            return ret + "}";
        }

        public JSPMethodCall callFunction(params IJSValue[] values)
        {
            return new JSPMethodCall(this.content, values);
        }
    }

    public interface IJSValue : IJSPiece
    {
        string content { get; }
    }

    public class JSValue : IJSValue
    {
        public string content { get; set; }

        public JSValue(object content)
        {
            this.content = content.ToString();
        }

        public JSValue(string content)
        {
            this.content = "\"" + content + "\"";
        }

        public JSValue(int content)
        {
            this.content = content.ToString();
        }

        public JSValue(bool content)
        {
            this.content = content.ToString();
        }

        public JSValue(double content)
        {
            this.content = content.ToString();
        }

        public string getCode(SessionData sessionData)
        {
            return content + ";";
        }
    }

    public class JSVariable : IJSValue
    {
        public string content { get; set; }

        public JSVariable(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                this.content = SessionContainer.generateHash();
        }

        public string getCode(SessionData sessionData)
        {
            return "var " + content + ";";
        }
    }

    public class JSPMethodCall : IJSPiece
    {
        private IJSValue[] parameters;
        private string methodName;

        public JSPMethodCall(string methodName, params IJSValue[] parameters)
        {
            this.methodName = methodName;
            this.parameters = parameters;
        }

        public string getCode(SessionData sessionData)
        {
            string ret = methodName + "(";

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += parameters[i];
            }

            return ret + ");";
        }

        public static class window
        {
            public static JSPMethodCall requestAnimationFrame(JSFunction function)
            {
                return new JSPMethodCall("window.requestAnimationFrame", function);
            }
        }
    }
}
