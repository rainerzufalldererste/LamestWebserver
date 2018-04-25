using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver.Core;
using LamestWebserver.WebServices;
using LamestWebserver.WebServices.Generators;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;

namespace LamestWebserver.WebServices.Generators
{
    /// <summary>
    /// Generates WebService Implementations from the WebService Implementation Templates.
    /// </summary>
    public class WebServiceImplementationGenerator
    {
        /// <summary>
        /// Compiles, builds and retrieves an instance of a local WebServiceImplementation inherited from the given type belonging to the given WebServiceHandler.
        /// </summary>
        /// <typeparam name="T">The type to inherit from.</typeparam>
        /// <param name="webServiceHandler">The corresponding WebServiceHandler.</param>
        /// <returns>An object of the local WebService.</returns>
        public static T GetWebServiceLocalImplementation<T>(WebServiceHandler webServiceHandler)
        {
            return (T)GetWebServiceLocalImplementation(typeof(T), webServiceHandler);
        }

        /// <summary>
        /// Compiles, builds and retrieves an instance of a local WebServiceImplementation inherited from the given type belonging to the given WebServiceHandler.
        /// </summary>
        /// <param name="type">The type to inherit from.</param>
        /// <param name="webServiceHandler">The corresponding WebServiceHandler.</param>
        /// <returns>An object of the local WebService.</returns>
        public static object GetWebServiceLocalImplementation(Type type, WebServiceHandler webServiceHandler)
        {
            if (type == null)
                throw new NullReferenceException(nameof(type));

            if (webServiceHandler == null)
                throw new NullReferenceException(nameof(webServiceHandler));

            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new LocalWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceLocalImplementationNamespace(), AssemblyNameSpace = type.Namespace };

            return CompileAndBuildObject(webservice.TransformText().Replace("global::", ""), type, GetWebServiceLocalImplementationNamespace() + "." + GetWebServiceLocalImplementationName(type), webServiceHandler);
        }

        /// <summary>
        /// Compiles, builds and retrieves an instance of a remote WebServiceImplementation inherited from the given type belonging to the given WebServiceHandler.
        /// </summary>
        /// <typeparam name="T">The type to inherit from.</typeparam>
        /// <param name="webServiceHandler">The corresponding WebServiceHandler.</param>
        /// <returns>An object of the remote WebService.</returns>
        public static T GetWebServiceRequestImplementation<T>(WebServiceHandler webServiceHandler)
        {
            return (T)GetWebServiceRequestImplementation(typeof(T), webServiceHandler);
        }

        /// <summary>
        /// Compiles, builds and retrieves an instance of a remote WebServiceImplementation inherited from the given type belonging to the given WebServiceHandler.
        /// </summary>
        /// <param name="type">The type to inherit from.</param>
        /// <param name="webServiceHandler">The corresponding WebServiceHandler.</param>
        /// <returns>An object of the remote WebService.</returns>
        public static object GetWebServiceRequestImplementation(Type type, WebServiceHandler webServiceHandler)
        {
            if (type == null)
                throw new NullReferenceException(nameof(type));

            if (webServiceHandler == null)
                throw new NullReferenceException(nameof(webServiceHandler));

            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new RequesterWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceRequestImplementationNamespace(), AssemblyNameSpace = type.Namespace };

            return CompileAndBuildObject(webservice.TransformText().Replace("global::", ""), type, GetWebServiceRequestImplementationNamespace() + "." + GetWebServiceRequestImplementationName(type), webServiceHandler);
        }

        /// <summary>
        /// Compiles a piece of code and builds an instance of the given type.
        /// </summary>
        /// <param name="code">The code to compile.</param>
        /// <param name="type">The baseType of the 'typeName' type.</param>
        /// <param name="typeName">The type to retrieve &amp; build an instance of from the compiled code.</param>
        /// <param name="webServiceHandler">The current WebServiceHandler.</param>
        /// <returns>Returns the instance of the given type.</returns>
        public static object CompileAndBuildObject(string code, Type type, string typeName, WebServiceHandler webServiceHandler)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.AddRange(AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => a.Location).ToArray());
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine();

#if DEBUG
                int line = 1;
                var lines = code.Replace("\r", "").Split('\n');

                (from l in lines select l).ToList().ForEach(x => sb.AppendLine(line++.ToString(new string('0', (int)System.Math.Ceiling(System.Math.Log10(lines.Length)))) + " | " + x));

	            sb.AppendLine();
	            sb.AppendLine();
#endif

                foreach (CompilerError error in results.Errors)
                    sb.AppendLine($"Error ({error.ErrorNumber}) in {error.FileName} Line {error.Line}:{error.Column} : {error.ErrorText}");

                throw new InvalidOperationException(sb.ToString());
            }

            Assembly assembly = results.CompiledAssembly;
            Type _type = assembly.GetType(typeName);

            if (_type == null)
            {
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{typeName}' (inherited from '{type.Namespace}.{type.Name}') could not be found."));
            }
            else if (_type.BaseType == type)
            {
                try
                {
                    var constructors = (from c in _type.GetConstructors() where c.DeclaringType == _type && c.IsPublic && c.GetParameters().Count() == 1 && c.GetParameters().First().ParameterType == typeof(WebServiceHandler) select c);

                    if (!constructors.Any())
                        throw new IncompatibleTypeException($"No compatible constructor taking '{nameof(WebServiceHandler)}' as parameter found.");

                    return constructors.First().Invoke(new object[] { webServiceHandler });
                }
                catch (Exception e)
                {
                    Logger.LogExcept(new IncompatibleTypeException($"An exception occurred when trying to create an object of the type '{typeName}' (inherited from '{type.Namespace}.{type.Name}').", e));
                }
            }
            else
            {
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{typeName}' was no '{nameof(IWebService)}' (inherited from '{type.Namespace}.{type.Name}')."));
            }

            throw new InvalidOperationException($"Failed to retrieve generated {nameof(IWebService)} '{typeName}' (inherited from '{type.Namespace}.{type.Name}').");
        }

        /// <summary>
        /// Retrieves the Name of a local WebService that would derive from the given type.
        /// </summary>
        /// <typeparam name="T">The type to derive a WebService from.</typeparam>
        /// <returns>Returns the name of the type as string.</returns>
        public static string GetWebServiceLocalImplementationName<T>() => GetWebServiceLocalImplementationName(typeof(T));

        /// <summary>
        /// Retrieves the Name of a local WebService that would derive from the given type.
        /// </summary>
        /// <param name="type">The type to derive a WebService from.</param>
        /// <returns>Returns the name of the type as string.</returns>
        public static string GetWebServiceLocalImplementationName(Type type) => type.Name + "LocalWebServiceGenImpl";

        /// <summary>
        /// Retrieves the namespace of a local WebService.
        /// </summary>
        /// <returns>Returns the name of the namespace of a local WebService.</returns>
        public static string GetWebServiceLocalImplementationNamespace() => "LamestWebserver.WebService.GeneratedCode.Local";

        /// <summary>
        /// Retrieves the Name of a remote WebService that would derive from the given type.
        /// </summary>
        /// <typeparam name="T">The type to derive a WebService from.</typeparam>
        /// <returns>Returns the name of the type as string.</returns>
        public static string GetWebServiceRequestImplementationName<T>() => GetWebServiceRequestImplementationName(typeof(T));

        /// <summary>
        /// Retrieves the Name of a remote WebService that would derive from the given type.
        /// </summary>
        /// <param name="type">The type to derive a WebService from.</param>
        /// <returns>Returns the name of the type as string.</returns>
        public static string GetWebServiceRequestImplementationName(Type type) => type.Name + "RequesterWebServiceImpl";

        /// <summary>
        /// Retrieves the namespace of a remote WebService.
        /// </summary>
        /// <returns>Returns the name of the namespace of a remote WebService.</returns>
        public static string GetWebServiceRequestImplementationNamespace() => "LamestWebserver.WebService.GeneratedCode.Request";
    }
}
