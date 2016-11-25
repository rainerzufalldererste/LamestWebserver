using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using JSFunctionCall = LamestWebserver.JScriptBuilder.JSPMethodCall;

namespace LamestWebserver.JScriptBuilder
{
    /// <summary>
    /// A batch of javascript code pieces
    /// </summary>
    public class JScript : IJSPiece
    {
        /// <summary>
        /// The contained javascript code pieces
        /// </summary>
        public List<IJSPiece> pieces = new List<IJSPiece>();

        /// <summary>
        /// Constructs an empty JScript
        /// </summary>
        public JScript()
        {

        }

        /// <summary>
        /// Constructs a JScript containing the given code pieces
        /// </summary>
        /// <param name="pieces">the contained code pieces</param>
        public JScript(params IJSPiece[] pieces) : this(pieces.ToList()) { }

        /// <summary>
        /// Constructs a JScript containing the given code pieces
        /// </summary>
        /// <param name="pieces">the contained code pieces</param>
        public JScript(List<IJSPiece> pieces)
        {
            this.pieces = pieces;
        }

        /// <summary>
        /// Appends a given piece of code to the Script
        /// </summary>
        /// <param name="piece">the piece of code to add</param>
        public void AppendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }

        /// <summary>
        /// Appends a given piece of code to the Script
        /// </summary>
        /// <param name="piecesToAdd">the pieces of code to add</param>
        public void AppendCodePieces(params IJSPiece[] piecesToAdd)
        {
            this.pieces.AddRange(piecesToAdd);
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "";

            for (int i = 0; i < pieces.Count; i++)
            {
                ret += pieces[i].getCode(sessionData, context);
            }

            return ret;
        }
    }

    /// <summary>
    /// Contains Features for JavaScript parsing
    /// </summary>
    public static class JSMaster
    {
        /// <summary>
        /// Encodes the given string to a JavaScript inner String
        /// </summary>
        /// <param name="input">the given string</param>
        /// <returns>the encoded string</returns>
        public static string JSEncode(this string input)
        {
            return HttpUtility.HtmlEncode(input).Replace("&lt;", "<").Replace("&gt;", ">");
        }

        /// <summary>
        /// Returns a piece of JavaScript code to decode this string as Base64 back to normal text
        /// </summary>
        /// <param name="input">the given input</param>
        /// <returns>A piece of JavaScript code</returns>
        public static string Base64Encode(this string input)
        {
            return "window.atob(\"" + Convert.ToBase64String(new ASCIIEncoding().GetBytes(input)) + "\")";
        }

        /// <summary>
        /// Returns a piece of JavaScript code decoding and executing the given string as base64
        /// </summary>
        /// <param name="input">the given code to encode</param>
        /// <returns>A piece of JavaScript code</returns>
        public static string EvalBase64(this string input)
        {
            return "eval(" + input.Base64Encode() + ")";
        }
    }

    /// <summary>
    /// The context in which this piece of code will be executed.
    /// </summary>
    public enum CallingContext
    {
        /// <summary>
        /// The Default Calling Context: End command with Semicolon
        /// </summary>
        Default,

        /// <summary>
        /// Inside a Call - Don't end command with Semicolon 
        /// </summary>
        Inner,

        /// <summary>
        /// Don't end command with a Semicolon
        /// </summary>
        NoSemicolon
    }

    /// <summary>
    /// Some kind of JavaScript code
    /// </summary>
    public interface IJSPiece
    {

        /// <summary>
        /// Retrieves the JavaScript code for this Element
        /// </summary>
        /// <param name="sessionData">the current sessionData</param>
        /// <param name="context">the current context. Default: CallingContext.Default</param>
        /// <returns>the JavaScript code as string</returns>
        string getCode(SessionData sessionData, CallingContext context = CallingContext.Default);
    }

    /// <summary>
    /// A JavaScript function definition
    /// </summary>
    public class JSFunction : IJSValue
    {
        /// <summary>
        /// The pieces of code to execute
        /// </summary>
        public List<IJSPiece> pieces = new List<IJSPiece>();

        /// <summary>
        /// the parameters to feed to this function
        /// </summary>
        public List<IJSValue> parameters = new List<IJSValue>();

        /// <summary>
        /// The name of this Function
        /// </summary>
        protected string _content;

        /// <summary>
        /// The name of this Function
        /// </summary>
        public override string content => _content;

        /// <summary>
        /// The name of this Function as JSValue
        /// </summary>
        public IJSValue FunctionPointer { get { return new JSValue(content); } }

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="name">the name of the function</param>
        /// <param name="parameters">the parameters of the Function Definition</param>
        public JSFunction(string name, List<IJSValue> parameters)
        {
            if (String.IsNullOrWhiteSpace(name))
                this._content = "_func" + SessionContainer.generateHash();
            else
                this._content = name;

            this.parameters = parameters;
        }

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="parameters">the parameters of the Function Definition</param>
        public JSFunction(List<IJSValue> parameters)
        {
            this._content = "_func" + SessionContainer.generateHash();
            this.parameters = parameters;
        }

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="parameters">the parameters of the Function Definition</param>
        public JSFunction(params IJSValue[] parameters) : this(parameters.ToList()) { }

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="name">the name of the function</param>
        public JSFunction(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                this._content = "_func" + SessionContainer.generateHash();
            else
                this._content = name;
        }

        /// <summary>
        /// Constructs a new empty JSFunction
        /// </summary>
        public JSFunction()
        {
            this._content = "_func" + SessionContainer.generateHash();
        }

        /// <summary>
        /// Adds a given piece of JavaScript code to this function.
        /// </summary>
        /// <param name="piece">the piece to add</param>
        public void AppendCode(IJSPiece piece)
        {
            pieces.Add(piece);
        }
        
        /// <summary>
        /// Adds a bunch of given pieces of JavaScript code to this function.
        /// </summary>
        /// <param name="piecesToAdd">the pieces to add</param>
        public void AppendCodePieces(params IJSPiece[] piecesToAdd)
        {
            pieces.AddRange(piecesToAdd);
        }

        /// <summary>
        /// You can't set a function.
        /// </summary>
        /// <param name="value">the value to set this value to</param>
        /// <returns>throws an Exception, because you cannot set a Function</returns>
        public override IJSValue Set(IJSValue value)
        {
            throw new InvalidOperationException("You can't set a function");
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            string ret = "function " + content + " ( ";

            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += parameters[i].content;
            }

            ret += ") { ";

            for (int i = 0; i < pieces.Count; i++)
            {
                ret += pieces[i].getCode(sessionData, CallingContext.Default);
            }

            return ret + "}";
        }

        /// <summary>
        /// Calls the given Function
        /// </summary>
        /// <param name="values">the parameters to input in this call</param>
        /// <returns>A piece of JavaScript code</returns>
        public JSFunctionCall callFunction(params IJSValue[] values)
        {
            return new JSFunctionCall(this.content, values);
        }

        /// <summary>
        /// Defines and Calls this Function at the same time.
        /// </summary>
        /// <returns>A piece of JavaScript code</returns>
        public JSDirectFunctionCall DefineAndCall()
        {
            return new JSDirectFunctionCall(this);
        }
    }

    /// <summary>
    /// A JSDirectFunctionCall defines and calls a function at the same time.
    /// </summary>
    public class JSDirectFunctionCall : IJSValue
    {
        private readonly JSFunction _function;

        /// <summary>
        /// Constructs a new JSDirectFunctionCall from a given Function
        /// </summary>
        /// <param name="function">the function to define and execute</param>
        public JSDirectFunctionCall(JSFunction function)
        {
            this._function = function;
        }

        /// <summary>
        /// The name of the given function.
        /// </summary>
        public override string content => _function.content;

        /// <summary>
        /// Sets the result of this FunctionCall to a Value
        /// </summary>
        /// <param name="value">the value to set to</param>
        /// <returns>A piece of JavaScript code</returns>
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            return "(" + _function.getCode(sessionData, CallingContext.Default) + ")()" + (context == CallingContext.Default ? ";" : " ");
        }
    }

    /// <summary>
    /// A JSInstant functino is a quick way to generate an anonymus Function executing some code.
    /// </summary>
    public class JSInstantFunction : JSFunction
    {
        /// <summary>
        /// Constructs a new Function containing the given code
        /// </summary>
        /// <param name="pieces">the code to execute on execution</param>
        public JSInstantFunction(params IJSPiece[] pieces)
        {
            base._content = "_ifunc" + SessionContainer.generateHash();
            base.pieces = pieces.ToList();
        }
    }

    /// <summary>
    /// A Value in JavaScript code
    /// </summary>
    public abstract class IJSValue : IJSPiece
    {
        /// <summary>
        /// The name of the Value
        /// </summary>
        public abstract string content { get; }

        /// <summary>
        /// A way to quickly set this Value.
        /// </summary>
        /// <param name="value">the value to set this element to</param>
        /// <returns>A piece of JavaScript code</returns>
        public abstract IJSValue Set(IJSValue value);

        /// <inheritdoc />
        public abstract string getCode(SessionData sessionData, CallingContext context = CallingContext.Default);
        
        /// <summary>
        /// Adds two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator +(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Add, a, b);
        }

        /// <summary>
        /// Subtracts two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator -(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Subtract, a, b);
        }

        /// <summary>
        /// Multiplies two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator *(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Multiply, a, b);
        }

        /// <summary>
        /// Divides two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator /(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Divide, a, b);
        }

        /*
        public static IJSValue operator ==(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Equals, a, b);
        }

        public static IJSValue operator !=(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.NotEquals, a, b);
        }*/

        
        /// <summary>
        /// Compares two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator <(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Less, a, b);
        }

        /// <summary>
        /// Compares two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator >(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Greater, a, b);
        }

        /// <summary>
        /// Compares two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator <=(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.LessOrEqual, a, b);
        }

        /// <summary>
        /// Compares two Values
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator >=(IJSValue a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.GreaterOrEqual, a, b);
        }
        
        /// <summary>
        /// Adds this Value to a StringValue
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator + (string a, IJSValue b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Add, new JSStringValue(a), b);
        }

        /// <summary>
        /// Adds a StringVaue to this Value
        /// </summary>
        /// <param name="a">value a</param>
        /// <param name="b">value b</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue operator + (IJSValue a, string b)
        {
            return new JSOperator(JSOperator.JSOperatorType.Add, a, new JSStringValue(b));
        }
    }

    /// <summary>
    /// Performs operations on IJSValues
    /// </summary>
    public class JSOperator : IJSValue
    {
        readonly JSOperatorType _operatorType;
        readonly IJSValue _a;
        readonly IJSValue _b;

        /// <summary>
        /// Returns the value of this Operation
        /// </summary>
        public override string content => getCode(SessionData.currentSessionData);

        /// <summary>
        /// Constructs a new Operation
        /// </summary>
        /// <param name="operatorType">the operator</param>
        /// <param name="a">first parameter</param>
        /// <param name="b">second parameter</param>
        public JSOperator(JSOperatorType operatorType, IJSValue a, IJSValue b)
        {
            this._operatorType = operatorType;
            this._a = a;
            this._b = b;
        }

        /// <summary>
        /// Sets the resulting value of this operation to a specified value.
        /// Please make sure, that you really want to do this.
        /// </summary>
        /// <param name="value">the value to set to</param>
        /// <returns>A piece of JavaScript code</returns>
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperatorType.Set, this, value);
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = _a.getCode(sessionData, CallingContext.Inner);

            switch(_operatorType)
            {
                case JSOperatorType.Add:
                    ret += " + ";
                    break;

                case JSOperatorType.Subtract:
                    ret += " - ";
                    break;

                case JSOperatorType.Multiply:
                    ret += " * ";
                    break;

                case JSOperatorType.Divide:
                    ret += " / ";
                    break;

                case JSOperatorType.Set:
                    ret += " = ";
                    break;

                case JSOperatorType.Equals:
                    ret += " == ";
                    break;

                case JSOperatorType.NotEquals:
                    ret += " != ";
                    break;

                case JSOperatorType.Greater:
                    ret += " > ";
                    break;

                case JSOperatorType.Less:
                    ret += " < ";
                    break;

                case JSOperatorType.GreaterOrEqual:
                    ret += " >= ";
                    break;

                case JSOperatorType.LessOrEqual:
                    ret += " <= ";
                    break;

                default:
                    throw new InvalidOperationException("The operator '" + _operatorType + "' is not handled in getCode()");
            }

            return ret + _b.getCode(sessionData, CallingContext.Inner) + (context == CallingContext.Default ? ";" : " ");
        }

        /// <summary>
        /// The Types of Operators supported in JSOperator
        /// </summary>
        public enum JSOperatorType
        {
            /// <summary>
            /// Addition
            /// </summary>
            Add,

            /// <summary>
            /// Subtraction
            /// </summary>
            Subtract,

            /// <summary>
            /// Multiplication
            /// </summary>
            Multiply,

            /// <summary>
            /// Division
            /// </summary>
            Divide,

            /// <summary>
            /// Setting to a Value
            /// </summary>
            Set,

            /// <summary>
            /// Equality-Comparison
            /// </summary>
            Equals,

            /// <summary>
            /// Greater than
            /// </summary>
            Greater,

            /// <summary>
            /// Less than
            /// </summary>
            Less,

            /// <summary>
            /// Greater or Equal than
            /// </summary>
            GreaterOrEqual,

            /// <summary>
            /// Less or Equal than
            /// </summary>
            LessOrEqual,

            /// <summary>
            /// Not Equal to Value
            /// </summary>
            NotEquals
        }
    }

    /// <summary>
    /// A String literal Value in JavaScript
    /// </summary>
    public class JSStringValue : JSValue
    {
        /// <summary>
        /// Constructs a new JSStringValue of a given string
        /// </summary>
        /// <param name="value">the string to set this value to</param>
        public JSStringValue(string value) : base(value)
        {
            base._content = value;
        }

        /// <inheritdoc />
        public override string content => "\"" + _content + "\"";

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            return "\"" + content.JSEncode() + "\"" + (context == CallingContext.Default ? ";" : " ");
        }

        /// <summary>
        /// Casts a string to a JSStringValue
        /// </summary>
        /// <param name="value">the string being casted</param>
        /// <returns>the string as JSStringValue</returns>
        public static implicit operator JSStringValue(string value)
        {
            return new JSStringValue(value);
        }
    }

    /// <summary>
    /// A JavaScript value
    /// </summary>
    public class JSValue : IJSValue
    {
        /// <summary>
        /// The content of the Value
        /// </summary>
        protected string _content;

        /// <summary>
        /// Retrieves the Value
        /// </summary>
        public override string content => _content;

        /// <summary>
        /// Constructs a new JSValue from an Object. ToString will be Called.
        /// </summary>
        /// <param name="content">the object to read from</param>
        public JSValue(object content)
        {
            this._content = content.ToString();
        }

        /// <summary>
        /// Constructs a new JSValue from a string. If you want a string literal, use JSStringValue instead.
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(string content)
        {
            this._content = content;
        }

        /// <summary>
        /// Constructs a new JSValue from an integer
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(int content)
        {
            this._content = content.ToString();
        }

        /// <summary>
        /// Constructs a new JSValue from a boolean value
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(bool content)
        {
            // Chris: way better than .ToString().ToLower()
            this._content = content ? "true" : "false";
        }

        /// <summary>
        /// Constructs a new JSValue from a double
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(double content)
        {
            this._content = content.ToString();
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            return content + (context == CallingContext.Default ? ";" : " ");
        }

        /// <inheritdoc />
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }

        /// <summary>
        /// Returns the current browser URL (window.location)
        /// </summary>
        public static JSValue CurrentBrowserURL => new JSValue("window.location");
    }
    
    /// <summary>
    /// A JavaScript variable
    /// </summary>
    public class JSVariable : IJSValue
    {
        /// <summary>
        /// The name of this Variable
        /// </summary>
        protected string _content;

        /// <summary>
        /// The name of this Variable
        /// </summary>
        public override string content => _content;

        /// <summary>
        /// Constructs a new JSVariable
        /// </summary>
        /// <param name="name">the name of the variable</param>
        public JSVariable(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                this._content = "_var" + SessionContainer.generateHash();
        }

        /// <inheritdoc />
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            return "var " + content + (context == CallingContext.Default ? ";" : " ");
        }
    }

    /// <summary>
    /// A JavaScript Function call
    /// </summary>
    public class JSPMethodCall : IJSValue
    {
        private readonly IJSValue[] _parameters;
        private readonly string _methodName;

        /// <summary>
        /// The name of this Mehod to call
        /// </summary>
        public override string content => _methodName;

        /// <summary>
        /// Constructs a new JavaScript functionCall
        /// </summary>
        /// <param name="methodName">the name of the Function</param>
        /// <param name="parameters">the parameters of the Function</param>
        public JSPMethodCall(string methodName, params IJSValue[] parameters)
        {
            this._methodName = methodName;
            this._parameters = parameters;
        }

        /// <summary>
        /// Sets the resulting object to a value
        /// </summary>
        /// <param name="value">hte value to set to</param>
        /// <returns>A piece of JavaScript code</returns>
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }
        
        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context)
        {
            string ret = _methodName + "(";

            for (int i = 0; i < _parameters.Length; i++)
            {
                if (i > 0)
                    ret += ", ";

                ret += _parameters[i].getCode(sessionData, CallingContext.Inner);
            }

            return ret + ")" + (context == CallingContext.Default ? ";" : " ");
        }

        /// <summary>
        /// Sets the innerHTML of an Element to the contents of a predefinded URL
        /// </summary>
        /// <param name="value">the element to set the new content to</param>
        /// <param name="URL">the URL where the new contents come from</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue SetInnerHTMLAsync(IJSValue value, string URL)
        {
            return new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + value.getCode(SessionData.currentSessionData, CallingContext.Inner) + ".innerHTML=this.responseText; } }; xmlhttp.open(\"GET\",\"" + URL + "\",true);xmlhttp.send();");
        }

        /// <summary>
        /// Requests a page from the predefinded URL. This can be used as Notification to the Server without any response.
        /// </summary>
        /// <param name="URL">The URL to request</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue NotifyAsync(string URL)
        {
            return new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); } xmlhttp.open(\"GET\",\"" + URL + "\",true);xmlhttp.send();");
        }

        /// <summary>
        /// Hides a specified element.
        /// </summary>
        /// <param name="id">the id of the element</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSPiece HideElementByID(string id)
        {
            return new JSValue("document.getElementById(\"" + id + "\").style.display = \"none\";");
        }

        /// <summary>
        /// Removes a specified element from the current document.
        /// </summary>
        /// <param name="id">the id of the element</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSPiece RemoveElementByID(string id)
        {
            string varName = "_var" + SessionContainer.generateHash();

            return new JSValue(varName + "=document.getElementById(\"" + id + "\");" + varName + ".remove();");
        }
    }

    public class JSElementValue : IJSValue
    {
        public JSElementValue(IJSValue value) { this._content = value.getCode(SessionData.currentSessionData, CallingContext.Inner); }

        public JSElementValue(string value) { this._content = value; }

        protected string _content;
        public override string content { get { return _content; } }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return content + (context == CallingContext.Default ? ";" : " ");
        }

        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }

        public JSValue InnerHTML { get { return new JSValue(this.content + ".innerHTML"); } }
        public JSValue InnerText { get { return new JSValue(this.content + ".innerText"); } }
        public JSValue Value { get { return new JSValue(this.content + ".value"); } }
        public JSValue Name { get { return new JSValue(this.content + ".name"); } }
        public JSValue ID { get { return new JSValue(this.content + ".id"); } }
        public JSValue Checked { get { return new JSValue(this.content + ".checked"); } }
        public JSValue ClassName { get { return new JSValue(this.content + ".className"); } }
        public JSValue OuterHTML { get { return new JSValue(this.content + ".outerHTML"); } }
        public JSValue OuterText { get { return new JSValue(this.content + ".outerText"); } }
    }

    public class JSIf : IJSPiece
    {
        private IJSPiece[] pieces;
        private IJSValue boolenExpression;

        public JSIf(IJSValue boolenExpression, params IJSPiece[] code)
        {
            this.boolenExpression = boolenExpression;
            this.pieces = code;
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return getContent(pieces, boolenExpression, sessionData);
        }

        internal static string getContent(IJSPiece[] pieces, IJSValue headExpression, SessionData sessionData, string operation = "if")
        {
            string ret = operation + " (" + headExpression.getCode(sessionData, CallingContext.Inner) + ") {";

            for (int i = 0; i < pieces.Length; i++)
            {
                ret += pieces[i].getCode(sessionData);
            }

            return ret + "}";
        }
    }

    public class JSElseIf : IJSPiece
    {
        private IJSPiece[] pieces;
        private IJSValue boolenExpression;

        public JSElseIf(IJSValue boolenExpression, params IJSPiece[] code)
        {
            this.boolenExpression = boolenExpression;
            this.pieces = code;
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.getContent(pieces, boolenExpression, sessionData, "else if ");
        }
    }

    public class JSElse : IJSPiece
    {
        private IJSPiece[] pieces;

        public JSElse(params IJSPiece[] code)
        {
            this.pieces = code;
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "else {";

            for (int i = 0; i < pieces.Length; i++)
            {
                ret += pieces[i].getCode(sessionData);
            }

            return ret + "}";
        }
    }

    public class JSInlineIf : IJSValue
    {
        private IJSValue booleanExpression, ifTrue, ifFalse;

        public JSInlineIf(IJSValue booleanExpression, IJSValue ifTrue, IJSValue ifFalse)
        {
            this.booleanExpression = booleanExpression;
            this.ifTrue = ifTrue;
            this.ifFalse = ifFalse;
        }

        public override string content
        {
            get { return getCode(SessionData.currentSessionData, CallingContext.Inner); }
        }

        /// <inheritdoc />
        public override string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return "(" + booleanExpression.getCode(sessionData, CallingContext.Inner) + " ? "
                + ifTrue.getCode(sessionData, CallingContext.Inner) + " : "
                + ifFalse.getCode(sessionData, CallingContext.Inner) + ")"
                + (context == CallingContext.Default ? ";" : " ");
        }
        public override IJSValue Set(IJSValue value)
        {
            throw new InvalidOperationException("You can't set a value to an inline if.");
        }
    }

    public class JSWhile : IJSPiece
    {
        private IJSPiece[] pieces;
        private IJSValue boolenExpression;

        public JSWhile(IJSValue boolenExpression, params IJSPiece[] code)
        {
            this.boolenExpression = boolenExpression;
            this.pieces = code;
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.getContent(pieces, boolenExpression, sessionData, "while ");
        }
    }
    
    public class JSDoWhile : JSWhile
    {
        private IJSPiece[] pieces;
        private IJSValue boolenExpression;

        public JSDoWhile(IJSValue boolenExpression, params IJSPiece[] code) : base(boolenExpression, code) { }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "do {";

            for (int i = 0; i < pieces.Length; i++)
            {
                ret += pieces[i].getCode(sessionData);
            }

            return ret + "} while (" + boolenExpression.getCode(sessionData, CallingContext.Inner) + ") ";
        }
    }

    public class JSFor : IJSPiece
    {
        private IJSPiece[] pieces;
        private JSVariable variable;
        private IJSValue startValue;
        private IJSPiece stepOperation, booleanExpression;

        public JSFor(IJSValue endValue, params IJSPiece[] code)
        {
            this.variable = new JSVariable();
            this.startValue = new JSValue(0);
            this.booleanExpression = (new JSValue(variable.content) < endValue);
            this.stepOperation = new JSValue(variable.content).Set(new JSValue(variable.content) + new JSValue(1));
            this.pieces = code;
        }
        public JSFor(JSVariable variable, IJSValue endValue, params IJSPiece[] code)
        {
            this.variable = variable;
            this.startValue = new JSValue(0);
            this.booleanExpression = (new JSValue(variable.content) < endValue);
            this.stepOperation = new JSValue(variable.content).Set(new JSValue(variable.content) + new JSValue(1));
            this.pieces = code;
        }

        public JSFor(JSVariable variable, IJSValue startValue, IJSValue endValue, params IJSPiece[] code)
        {
            this.variable = variable;
            this.startValue = startValue;
            this.booleanExpression = (new JSValue(variable.content) < endValue);
            this.stepOperation = new JSValue(variable.content).Set(new JSValue(variable.content) + new JSValue(1));
            this.pieces = code;
        }

        public JSFor(JSVariable variable, IJSValue startValue, IJSValue endValue, JSOperator.JSOperatorType _operator, IJSPiece stepOperation, params IJSPiece[] code)
        {
            this.variable = variable;
            this.startValue = startValue;
            this.booleanExpression = new JSOperator(_operator, new JSValue(variable.content), endValue);
            this.stepOperation = stepOperation;
            this.pieces = code;
        }

        public JSFor(JSVariable variable, IJSValue value, IJSValue booleanExpression, IJSPiece stepOperation, params IJSPiece[] code)
        {
            this.variable = variable;
            this.startValue = value;
            this.booleanExpression = booleanExpression;
            this.stepOperation = stepOperation;
            this.pieces = code;
        }

        /// <inheritdoc />
        public string getCode(SessionData sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.getContent(pieces,
                new JSValue(variable.Set(startValue).getCode(sessionData) +
                            booleanExpression.getCode(sessionData) +
                             stepOperation.getCode(sessionData, CallingContext.Inner)), sessionData, "for ");
        }
    }
}
