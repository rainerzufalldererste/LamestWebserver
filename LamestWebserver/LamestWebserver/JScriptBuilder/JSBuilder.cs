using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using LamestWebserver.UI;

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
        public string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
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
            if((from char c in input where c > 127 select true).Any())
                return HttpUtility.HtmlEncode(input).Replace("&lt;", "<").Replace("&gt;", ">");

            return input.Replace("&", "&amp;").Replace("\"", "&quot;");
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
        string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default);
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
        public IJSValue FunctionPointer => new JSValue(content);

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="name">the name of the function</param>
        /// <param name="parameters">the parameters of the Function Definition</param>
        public JSFunction(string name, List<IJSValue> parameters)
        {
            if (String.IsNullOrWhiteSpace(name))
                _content = "_func" + SessionContainer.GenerateHash();
            else
                _content = name;

            this.parameters = parameters;
        }

        /// <summary>
        /// Constructs a new JSFunction
        /// </summary>
        /// <param name="parameters">the parameters of the Function Definition</param>
        public JSFunction(List<IJSValue> parameters)
        {
            _content = "_func" + SessionContainer.GenerateHash();
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
                _content = "_func" + SessionContainer.GenerateHash();
            else
                _content = name;
        }

        /// <summary>
        /// Constructs a new empty JSFunction
        /// </summary>
        public JSFunction()
        {
            _content = "_func" + SessionContainer.GenerateHash();
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
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
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
                ret += pieces[i].getCode(sessionData);
            }

            return ret + "}";
        }

        /// <summary>
        /// Calls the given Function
        /// </summary>
        /// <param name="values">the parameters to input in to this call</param>
        /// <returns>A piece of JavaScript code</returns>
        public JSFunctionCall callFunction(params IJSValue[] values)
        {
            return new JSFunctionCall(this.content, values);
        }

        /// <summary>
        /// Calls the Function with the given parameters
        /// </summary>
        /// <param name="values">the values to call the function with</param>
        /// <returns>A piece of JavaScript code</returns>
        public JSFunctionCall this[params IJSValue[] values] => callFunction(values);

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
            _function = function;
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
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return "(" + _function.getCode(sessionData) + ")()" + (context == CallingContext.Default ? ";" : " ");
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
            _content = "_ifunc" + SessionContainer.GenerateHash();
            this.pieces = pieces.ToList();
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

        /// <summary>
        /// A way to quickly compare two values in JavaScript on Equality
        /// </summary>
        /// <param name="value">the value to compare to</param>
        /// <returns>A piece of JavaScript code</returns>
        public virtual IJSValue IsEqualTo(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Equals, this, value);
        }

        /// <summary>
        /// A way to quickly compare two values in JavaScript on Nonequality
        /// </summary>
        /// <param name="value">the value to compare to</param>
        /// <returns>A piece of JavaScript code</returns>
        public virtual IJSValue IsNotEqualTo(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.NotEquals, this, value);
        }

        /// <inheritdoc />
        public abstract string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default);
        
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

        /// <summary>
        /// Casts an HElement to a JSValue
        /// </summary>
        /// <param name="element">the HElement</param>
        public static implicit operator IJSValue(HElement element)
        {
            return new JSValue(element.GetContent(AbstractSessionIdentificator.CurrentSession));
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
        public override string content => getCode(AbstractSessionIdentificator.CurrentSession);

        /// <summary>
        /// Constructs a new Operation
        /// </summary>
        /// <param name="operatorType">the operator</param>
        /// <param name="a">first parameter</param>
        /// <param name="b">second parameter</param>
        public JSOperator(JSOperatorType operatorType, IJSValue a, IJSValue b)
        {
            _operatorType = operatorType;
            _a = a;
            _b = b;
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
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
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
                    ret += " === ";
                    break;

                case JSOperatorType.NotEquals:
                    ret += " !== ";
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

            return ret + (_b is JSStringValue ?_b.getCode(sessionData, CallingContext.Inner).EvalBase64() : _b.getCode(sessionData, CallingContext.Inner)) + (context == CallingContext.Default ? ";" : " ");
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
            _content = value;
        }

        /// <inheritdoc />
        public override string content => "\"" + _content + "\"";

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return "\"" + _content.JSEncode() + "\"" + (context == CallingContext.Default ? ";" : " ");
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
    /// Represents an already masked string value. A JSUnmaskedStringValue will not be encoded when processing.
    /// </summary>
    public class JSUnmaskedStringValue : JSStringValue
    {

        /// <summary>
        /// Constructs a new JSUnmaskedStringValue.
        /// </summary>
        /// <param name="value"></param>
        public JSUnmaskedStringValue(string value) : base(value)
        {
        }

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return "\"" + _content.Replace("&quot;", "&amp;quot;").Replace("\"", "&quot;") + "\"" + (context == CallingContext.Default ? ";" : " ");
        }

        /// <summary>
        /// Casts a string to a JSUnmaskedStringValue
        /// </summary>
        /// <param name="value">the string being casted</param>
        /// <returns>the string as JSUnmaskedStringValue</returns>
        public static implicit operator JSUnmaskedStringValue(string value)
        {
            return new JSUnmaskedStringValue(value);
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
            _content = content.ToString();
        }

        /// <summary>
        /// Constructs a new JSValue from a string. If you want a string literal, use JSStringValue instead.
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(string content)
        {
            _content = content;
        }

        /// <summary>
        /// Constructs a new JSValue from an integer
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(int content)
        {
            _content = content.ToString();
        }

        /// <summary>
        /// Constructs a new JSValue from a boolean value
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(bool content)
        {
            // Chris: way better than .ToString().ToLower()
            _content = content ? "true" : "false";
        }

        /// <summary>
        /// Constructs a new JSValue from a double
        /// </summary>
        /// <param name="content">the content of this value</param>
        public JSValue(double content)
        {
            _content = content.ToString();
        }

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
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
                this._content = "_var" + SessionContainer.GenerateHash();
            else this._content = name;
        }

        /// <inheritdoc />
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, new JSValue(content), value);
        }

        /// <inheritdoc />
        public override IJSValue IsEqualTo(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Equals, new JSValue(content), value);
        }

        /// <inheritdoc />
        public override IJSValue IsNotEqualTo(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.NotEquals, new JSValue(content), value);
        }

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return "var " + content + (context == CallingContext.Default ? ";" : " ");
        }
    }

    /// <summary>
    /// A JavaScript Function call
    /// </summary>
    public class JSFunctionCall : IJSValue
    {
        private readonly IJSValue[] _parameters;
        private readonly string _methodName;

        /// <summary>
        /// The name of the Method to call
        /// </summary>
        public override string content => _methodName;

        /// <summary>
        /// Constructs a new JavaScript functionCall
        /// </summary>
        /// <param name="methodName">the name of the Function</param>
        /// <param name="parameters">the parameters of the Function</param>
        public JSFunctionCall(string methodName, params IJSValue[] parameters)
        {
            _methodName = methodName;
            _parameters = parameters;
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
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
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
        /// Returns and starts an interval in which a function is called
        /// </summary>
        /// <param name="function">The function to start</param>
        /// <param name="milliseconds">The TimeSpan in Milliseconds at which the Function will be called</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue SetInterval(JSFunction function, int milliseconds)
        {
            return new JSValue($"setInterval({function.FunctionPointer.getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner)}, {milliseconds});");
        }

        /// <summary>
        /// Stops an interval.
        /// </summary>
        /// <param name="variable">The Variable the Interval has been stored in</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue ClearInterval(JSVariable variable)
        {
            return new JSValue("clearInterval(" + variable + ");");
        }

        /// <summary>
        /// Sets the innerHTML of an Element to the contents of a predefinded URL
        /// </summary>
        /// <param name="value">the element to set the new content to</param>
        /// <param name="URL">the URL where the new contents come from</param>
        /// <param name="executeOnComplete">the code to execute when the task has been completed</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue SetInnerHTMLAsync(IJSValue value, string URL, params IJSPiece[] executeOnComplete)
        {
            return new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + value.getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner) + ".innerHTML=this.responseText;"
                + ((Func<string>)(() => {string ret = ""; executeOnComplete.ToList().ForEach(piece => ret += piece.getCode(AbstractSessionIdentificator.CurrentSession)); return ret;})).Invoke()
                + " } }; xmlhttp.open(\"GET\",\"" + URL + "\",true);xmlhttp.send();");
        }

        /// <summary>
        /// Sets the outerHTML of an Element to the contents of a predefinded URL
        /// </summary>
        /// <param name="value">the element to set the new content to</param>
        /// <param name="URL">the URL where the new contents come from</param>
        /// <param name="executeOnComplete">the code to execute when the task has been completed</param>
        /// <returns>A piece of JavaScript code</returns>
        public static JSValue SetOuterHTMLAsync(IJSValue value, string URL, params IJSPiece[] executeOnComplete)
        {
            return new JSValue("var xmlhttp; if (window.XMLHttpRequest) {xmlhttp=new XMLHttpRequest();} else {xmlhttp=new ActiveXObject(\"Microsoft.XMLHTTP\"); }  xmlhttp.onreadystatechange=function() { if (this.readyState==4 && this.status==200) { " + value.getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner) + ".outerHTML=this.responseText;"
                + ((Func<string>)(() => { string ret = ""; executeOnComplete.ToList().ForEach(piece => ret += piece.getCode(AbstractSessionIdentificator.CurrentSession)); return ret; })).Invoke()
                + " } }; xmlhttp.open(\"GET\",\"" + URL + "\",true);xmlhttp.send();");
        }

        /// <summary>
        /// Encodes a URI component to a formatted string.
        /// </summary>
        /// <param name="value">the value to encode</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSValue EncodeURIComponent(IJSValue value)
        {
            return new JSValue("encodeURIComponent(" + value.content + ")");
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
        /// Shows a specified element. (Sets it's display style to Block)
        /// </summary>
        /// <param name="id">the id of the element</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSPiece DisplayElementByID(string id)
        {
            return new JSValue("document.getElementById(\"" + id + "\").style.display = \"block\";");
        }

        /// <summary>
        /// Removes a specified element from the current document.
        /// </summary>
        /// <param name="id">the id of the element</param>
        /// <returns>A piece of JavaScript code</returns>
        public static IJSPiece RemoveElementByID(string id)
        {
            string varName = "_var" + SessionContainer.GenerateHash();

            return new JSValue(varName + "=document.getElementById(\"" + id + "\");" + varName + ".remove();");
        }
    }

    /// <summary>
    /// A JavaScript Value of Type Element (representing a HTML Element)
    /// </summary>
    public class JSElementValue : IJSValue
    {
        /// <summary>
        /// Constructs a new JSElementValue from a Value
        /// </summary>
        /// <param name="value">the value</param>
        public JSElementValue(IJSValue value) { this._content = value.getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner); }

        /// <summary>
        /// Constructs a new JSElementValue from a string
        /// </summary>
        /// <param name="value">the value</param>
        public JSElementValue(string value) { this._content = value; }

        /// <summary>
        /// The Name of this Element or the Function retrieving it
        /// </summary>
        protected string _content;

        /// <summary>
        /// The Name of this Element or the Function retrieving it
        /// </summary>
        public override string content => _content;

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return content + (context == CallingContext.Default ? ";" : " ");
        }
        
        /// <inheritdoc />
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }

        /// <summary>
        /// The 'innerHTML' attribute of this Element
        /// </summary>
        public JSValue InnerHTML => new JSValue(content + ".innerHTML");

        /// <summary>
        /// The 'innerText' attribute of this Element
        /// </summary>
        public JSValue InnerText => new JSValue(content + ".innerText");

        /// <summary>
        /// The 'value' attribute of this Element
        /// </summary>
        public JSValue Value => new JSValue(content + ".value");

        /// <summary>
        /// The 'name' attribute of this Element
        /// </summary>
        public JSValue Name => new JSValue(content + ".name");

        /// <summary>
        /// The 'id' attribute of this Element
        /// </summary>
        public JSValue ID => new JSValue(content + ".id");

        /// <summary>
        /// The 'checked' attribute of this Element
        /// </summary>
        public JSValue Checked => new JSValue(content + ".checked");

        /// <summary>
        /// The 'className' attribute of this Element
        /// </summary>
        public JSValue ClassName => new JSValue(content + ".className");

        /// <summary>
        /// The 'outerHTML' attribute of this Element
        /// </summary>
        public JSValue OuterHTML => new JSValue(content + ".outerHTML");

        /// <summary>
        /// The 'outerText' attribute of this Element
        /// </summary>
        public JSValue OuterText => new JSValue(content + ".outerText");

        /// <summary>
        /// The 'selectedOptions[0].value' attribute of this Element
        /// </summary>
        public JSValue FirstSelected => new JSValue(content + ".selectedOptions[0].value");

        /// <summary>
        /// Displays (display = 'block') this Element
        /// </summary>
        public IJSPiece Show => JSFunctionCall.DisplayElementByID(ID.content);

        /// <summary>
        /// Hides (display = 'none') this Element
        /// </summary>
        public IJSPiece Hide => JSFunctionCall.HideElementByID(ID.content);

        /// <summary>
        /// Removes this Element from the page
        /// </summary>
        public IJSPiece Delete => JSFunctionCall.DisplayElementByID(ID.content);
    }

    /// <summary>
    /// A JavaScript If-Statement
    /// </summary>
    public class JSIf : IJSPiece
    {
        private readonly IJSPiece[] _pieces;
        private readonly IJSValue _booleanExpression;

        /// <summary>
        /// Constructs an If-Statement from a boolean Expression and the executed Code if true
        /// </summary>
        /// <param name="booleanExpression">the boolean Expression that has to be true to execute the code</param>
        /// <param name="code">the code that is executed if the boolean Expression is true</param>
        public JSIf(IJSValue booleanExpression, params IJSPiece[] code)
        {
            _booleanExpression = booleanExpression;
            _pieces = code;
        }

        /// <inheritdoc />
        public string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return GetContent(_pieces, _booleanExpression, sessionData);
        }
        
        internal static string GetContent(IJSPiece[] pieces, IJSValue headExpression, AbstractSessionIdentificator sessionData, string operation = "if")
        {
            string ret = operation + " (" + headExpression.getCode(sessionData, CallingContext.Inner) + ") {";

            for (int i = 0; i < pieces.Length; i++)
            {
                ret += pieces[i].getCode(sessionData);
            }

            return ret + "}";
        }
    }

    /// <summary>
    /// A JavaScript Else-If-Statement
    /// </summary>
    public class JSElseIf : IJSPiece
    {
        private readonly IJSPiece[] _pieces;
        private readonly IJSValue _booleanExpression;

        /// <summary>
        /// Constructs an Else-If-Statement from a boolean Expression and the executed Code if true
        /// </summary>
        /// <param name="booleanExpression">the boolean Expression that has to be true to execute the code</param>
        /// <param name="code">the code that is executed if the boolean Expression is true</param>
        public JSElseIf(IJSValue booleanExpression, params IJSPiece[] code)
        {
            _booleanExpression = booleanExpression;
            _pieces = code;
        }

        /// <inheritdoc />
        public string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.GetContent(_pieces, _booleanExpression, sessionData, "else if ");
        }
    }

    /// <summary>
    /// A JavaScript Else-Statement
    /// </summary>
    public class JSElse : IJSPiece
    {
        private readonly IJSPiece[] _pieces;

        /// <summary>
        /// Constructs an Else-Statement from  the executed Code if true
        /// </summary>
        /// <param name="code">the code that is executed</param>
        public JSElse(params IJSPiece[] code)
        {
            this._pieces = code;
        }

        /// <inheritdoc />
        public string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "else {";

            for (int i = 0; i < _pieces.Length; i++)
            {
                ret += _pieces[i].getCode(sessionData);
            }

            return ret + "}";
        }
    }

    /// <summary>
    /// A JavaScript inline If-Statement
    /// </summary>
    public class JSInlineIf : IJSValue
    {
        readonly IJSValue _booleanExpression, _ifTrue, _ifFalse;

        /// <summary>
        /// Constructs an Inline-If-Statement from a boolean Expression and the Values if true and if false
        /// </summary>
        /// <param name="booleanExpression">the boolean Expression</param>
        /// <param name="ifTrue">the value if true</param>
        /// <param name="ifFalse">the value if false</param>
        public JSInlineIf(IJSValue booleanExpression, IJSValue ifTrue, IJSValue ifFalse)
        {
            _booleanExpression = booleanExpression;
            _ifTrue = ifTrue;
            _ifFalse = ifFalse;
        }

        /// <summary>
        /// Retrieves the whole Inline-If-Statement
        /// </summary>
        public override string content => getCode(AbstractSessionIdentificator.CurrentSession, CallingContext.Inner);

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return "(" + _booleanExpression.getCode(sessionData, CallingContext.Inner) + " ? "
                + _ifTrue.getCode(sessionData, CallingContext.Inner) + " : "
                + _ifFalse.getCode(sessionData, CallingContext.Inner) + ")"
                + (context == CallingContext.Default ? ";" : " ");
        }

        /// <inheritdoc />
        public override IJSValue Set(IJSValue value)
        {
            return new JSOperator(JSOperator.JSOperatorType.Set, this, value);
        }
    }

    /// <summary>
    /// A JavaScript While-Loop
    /// </summary>
    public class JSWhileLoop : IJSPiece
    {
        /// <summary>
        /// The code in this Loop
        /// </summary>
        protected readonly IJSPiece[] Pieces;

        /// <summary>
        /// The boolean expression for this Loop
        /// </summary>
        protected readonly IJSValue BooleanExpression;

        /// <summary>
        /// Constructs a new JSWhileLoop
        /// </summary>
        /// <param name="booleanExpression">the boolean Expression that has to be true</param>
        /// <param name="code">the code to execute while the expression is true</param>
        public JSWhileLoop(IJSValue booleanExpression, params IJSPiece[] code)
        {
            BooleanExpression = booleanExpression;
            Pieces = code;
        }

        /// <inheritdoc />
        public virtual string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.GetContent(Pieces, BooleanExpression, sessionData, "while ");
        }
    }
    
    /// <summary>
    /// A JavaScript Do-While-Loop
    /// </summary>
    public class JSDoWhileLoop : JSWhileLoop
    {
        /// <summary>
        /// Constructs a new Do-While-Loop
        /// </summary>
        /// <param name="booleanExpression">the expression that has to be true to repeat the loop</param>
        /// <param name="code">the code to execute in the loop</param>
        public JSDoWhileLoop(IJSValue booleanExpression, params IJSPiece[] code) : base(booleanExpression, code) { }

        /// <inheritdoc />
        public override string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            string ret = "do {";

            for (int i = 0; i < Pieces.Length; i++)
            {
                ret += Pieces[i].getCode(sessionData);
            }

            return ret + "} while (" + BooleanExpression.getCode(sessionData, CallingContext.Inner) + ") ";
        }
    }

    /// <summary>
    /// A JavaScript For-Loop
    /// </summary>
    public class JSForLoop : IJSPiece
    {
        private readonly IJSPiece[] _pieces;
        private readonly JSVariable _variable;
        private readonly IJSValue _startValue;
        private readonly IJSPiece _stepOperation, _booleanExpression;

        /// <summary>
        /// Constructs a For-Loop iterating from Zero to the specified endValue
        /// </summary>
        /// <param name="endValue">the End-Value</param>
        /// <param name="code">the code to execute</param>
        public JSForLoop(IJSValue endValue, params IJSPiece[] code)
        {
            _variable = new JSVariable();
            _startValue = new JSValue(0);
            _booleanExpression = (new JSValue(_variable.content) < endValue);
            _stepOperation = new JSValue(_variable.content).Set(new JSValue(_variable.content) + new JSValue(1));
            _pieces = code;
        }

        /// <summary>
        /// Constructs a For-Loop iterating a Variable from Zero to the specified endValue
        /// </summary>
        /// <param name="variable">the Variable to iterate</param>
        /// <param name="endValue">the End-Value</param>
        /// <param name="code">the code to execute</param>
        public JSForLoop(JSVariable variable, IJSValue endValue, params IJSPiece[] code)
        {
            _variable = variable;
            _startValue = new JSValue(0);
            _booleanExpression = (new JSValue(variable.content) < endValue);
            _stepOperation = new JSValue(variable.content).Set(new JSValue(variable.content) + new JSValue(1));
            _pieces = code;
        }
        
        /// <summary>
        /// Constructs a For-Loop iterating a Variable from the specified startValue to the specified endValue
        /// </summary>
        /// <param name="variable">the Variable to iterate</param>
        /// <param name="startValue">the Start-Value</param>
        /// <param name="endValue">the End-Value</param>
        /// <param name="code">the code to execute</param>
        public JSForLoop(JSVariable variable, IJSValue startValue, IJSValue endValue, params IJSPiece[] code)
        {
            _variable = variable;
            _startValue = startValue;
            _booleanExpression = (new JSValue(variable.content) < endValue);
            _stepOperation = new JSValue(variable.content).Set(new JSValue(variable.content) + new JSValue(1));
            _pieces = code;
        }

        /// <summary>
        /// Constructs a For-Loop iterating a Variable from the specified startValue as long as the variable is within a certain relation with the endValue executing the given operation each step.
        /// </summary>
        /// <param name="variable">the Variable to iterate</param>
        /// <param name="startValue">the Start-Value</param>
        /// <param name="endValue">the End-Value</param>
        /// <param name="_operator">the relation the endValue stands in with the variable</param>
        /// <param name="stepOperation">the operation to execute each iteration</param>
        /// <param name="code">the code to execute</param>
        public JSForLoop(JSVariable variable, IJSValue startValue, IJSValue endValue, JSOperator.JSOperatorType _operator, IJSPiece stepOperation, params IJSPiece[] code)
        {
            _variable = variable;
            _startValue = startValue;
            _booleanExpression = new JSOperator(_operator, new JSValue(variable.content), endValue);
            _stepOperation = stepOperation;
            _pieces = code;
        }

        /// <summary>
        /// Constructs a For-Loop iterating a Variable with a value as long as a booleanExpression is true by modifying something each iteration in the specified stepOperation and executing the given piece of code.
        /// </summary>
        /// <param name="variable">the variable</param>
        /// <param name="value">the start-value of the variable</param>
        /// <param name="booleanExpression">the boolean expression</param>
        /// <param name="stepOperation">the operation to execute each step</param>
        /// <param name="code">the code to execute inside the loop</param>
        public JSForLoop(JSVariable variable, IJSValue value, IJSValue booleanExpression, IJSPiece stepOperation, params IJSPiece[] code)
        {
            _variable = variable;
            _startValue = value;
            _booleanExpression = booleanExpression;
            _stepOperation = stepOperation;
            _pieces = code;
        }

        /// <inheritdoc />
        public string getCode(AbstractSessionIdentificator sessionData, CallingContext context = CallingContext.Default)
        {
            return JSIf.GetContent(_pieces,
                new JSValue(_variable.Set(_startValue).getCode(sessionData) +
                            _booleanExpression.getCode(sessionData) +
                             _stepOperation.getCode(sessionData, CallingContext.Inner)), sessionData, "for ");
        }
    }
}
