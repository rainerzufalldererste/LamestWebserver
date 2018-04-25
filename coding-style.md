C# Coding Style
===============

The general rule we follow is "use Visual Studio defaults".

* We use [Allman style](http://en.wikipedia.org/wiki/Indent_style#Allman_style) braces, where each brace begins on a new line. A single line statement block can go without braces but the block must be properly indented on its own line and it must not be nested in other statement blocks that use braces (See issue [381](https://github.com/dotnet/corefx/issues/381) for examples). 
* We use four spaces of indentation (no tabs).
* We always specify the visibility, even if it's the default (i.e.
  `private string _foo` not `string _foo`). Visibility should be the first modifier (i.e. 
  `public abstract` not `abstract public`).
* Namespace imports should be specified at the top of the file, *outside* of
  `namespace` declarations.
* Avoid more than one empty line at any time unless you need to have a very clear distinction between one section (which has empty lines in it) from another, in which case use a maximum of two empty lines.
* Avoid spurious free spaces.
  For example avoid `if (someVar == 0)...`, where the dots mark the spurious free spaces.
  Consider enabling "View White Space (Ctrl+E, S)" if using Visual Studio, to aid detection.
* We use language keywords instead of BCL types (i.e. `int, string, float` instead of `Int32, String, Single`, etc) for both type references as well as method calls (i.e. `int.Parse` instead of `Int32.Parse`).
* If a file happens to differ in style from these guidelines (e.g. private members are named `m_member`
  rather than `_member`), the existing style in that file takes precedence.
* We do not comment on obvious things but document every method that is exposed by being `public` or `protected`.
* We prefer using `$"Some String {someVariable}."` instead of concatenating strings manually.
* Fields should be specified at the top within type declarations.
* We write UnitTests for our code if a component is to some degree unit-testable.
* We check all arguments to a method call on if they are within the certain criteria and throw `FormatException`s, `NullReferenceException`s, `IndexOutOfBoundsException`s where applicable.
* For non code files (xml etc) our current best guidance is consistency. When editing files, keep new code and changes consistent with the style in the files. For new files, it should conform to the style for that component. Last, if there's a completely new component, anything that is reasonably broadly accepted is fine.
* When including non-ASCII characters in the source code we prefer to use Unicode escape sequences (\uXXXX) instead of literal characters. Literal non-ASCII characters occasionally get garbled by a tool or editor.

* We use `_camelCase` for internal and private fields especially if they are only accessed by a property and use `readonly` where possible. When used on static fields, `readonly` should come after `static` (i.e. `static readonly` not `readonly static`).
* We use `PascalCasing` to name all our constant local variables and fields, `public`, `protected` and `internal` fields and **any** enum-entry, property or method. The only exception is for interop code where the constant value should exactly match the name and value of the code you are calling via interop.
* We use `ECamelCalse` for enums. We prefer to specify the type and value if they are somehow relevant rather than i.e. just using `[Flags]`.
* We avoid `this.` unless absolutely necessary. 
* We only use `var` when it's obvious what the variable type is (i.e. `var stream = new FileStream(...)` not `var stream = OpenStandardInput()`). We prefer to just use `var` for templated types which would be far more characters to write and read.
* We use properties instead of fields if that simplifies code or structure, usually by being virtual, by having more complex `get` or `set` behaviour or by having different accessors for `get` and `set`.
* Any statement like `if`, `for`, `while` etc. has an empty line before the statement and after the closing bracket or end of the block (which is after all `else if` and `else` statements in a complex `if`-statement).
* We don't use braces in `if`-statements that do not declare a body of more than one line in any of their `else` statements. 
* Regions with initializations, method calls (returning void), and `return` statements are usually separated by an empty line.
* We use ```nameof(...)``` instead of ```"..."``` whenever possible and relevant.
* If using `unsafe` code we prefix pointers with `p` (i.e. `byte* pData`).


### If you write code please always keep the following things in mind:

* Simplicity
* Reusability
* Performance
* Multi-Threading and Race-Conditions
* Serialization & Deserialization
* Conformity with the rest of the Codebase (i.e. inheriting from `NullCheckable` if applicable).
* Testability
* Multi-Execution & Caching


### Example:

```C#
using System;
using System.Reflection.Emit;
using LamestWebserver.Synchronization;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Provides a threadsafe, auto-initializing Singleton for a given type.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public class Singleton<T> : NullCheckable
    {
        private UsableMutexSlim _singletonLock;

        private bool _initialized = false;
        private T _instance;

        private Func<T> _getInstance = null;

        /// <summary>
        /// The Instance of the Singleton. The instance is automatically initalized when you first read from it.
        /// </summary>
        public T Instance
        {
            get
            {
                if (!_initialized) // Checked outside for higher performance in most cases but not threadsafe. Therefore checked again inside the lock.
                {
                    using (_singletonLock.Lock())
                    {
                        if (!_initialized)
                        {
                            _instance = _getInstance();
                            _initialized = true;
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Creates a new Singleton.
        /// </summary>
        /// <param name="getInstanceFunction">A function to create an instance of the given type. If null will be set to default constructor of this type.</param>
        /// <param name="initializeDirectly">Shall the Singleton be directly initialized upfront?</param>
        /// <exception cref="MissingMethodException">Throws a MissingMethodException when no getInstanceFunction is given and the type does not contain a default constructor and is no ValueType.</exception>
        public Singleton(Func<T> getInstanceFunction = null, bool initializeDirectly = false)
        {
            _instance = default(T);
            _getInstance = getInstanceFunction;

            if (_getInstance == null)
            {
                var ctor = typeof(T).GetConstructor(Type.EmptyTypes);

                if (ctor == null)
                {
                    if (typeof(T).IsValueType)
                    {
                        _initialized = true; // has already been initialized in the first line to default(T).
                        return;
                    }
                    else
                    {
                        throw new MissingMethodException("There is no empty constructor defined for this type. Please implement an empty constructor or provide a initialization function.");
                    }
                }
                else // just for code clarity.
                {
                    // Create method to call the empty constructor for the given type.
                    // See https://stackoverflow.com/questions/10593630/create-delegate-from-constructor

                    DynamicMethod dynamic = new DynamicMethod(string.Empty, typeof(T), Type.EmptyTypes, typeof(T));
                    ILGenerator il = dynamic.GetILGenerator();

                    il.DeclareLocal(typeof(T));
                    il.Emit(OpCodes.Newobj, ctor);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ret);

                    _getInstance = (Func<T>)dynamic.CreateDelegate(typeof(Func<T>));
                }
            }

            if(initializeDirectly)
            {
                if (!_initialized)
                    _instance = _getInstance();

                _initialized = true;
            }
            else
            {
                _singletonLock = new UsableMutexSlim();
            }
        }

        /// <summary>
        /// Retrieves the Instance of the singleton.
        /// </summary>
        /// <param name="singleton">The current singleton.</param>
        /// <returns>Returns the Instance of the singleton.</returns>
        public static implicit operator T(Singleton<T> singleton) => singleton.Instance;

        /// <summary>
        /// Retrieves the Instance of the singleton.
        /// </summary>
        /// <returns>Returns the Instance of the singleton.</returns>
        public T GetInstance() => Instance;
    }
}
```