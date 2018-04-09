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
    public class WebServiceImplGenerator
    {
        public static T GetWebServiceLocalImpl<T>()
        {
            return (T)GetWebServiceLocalImpl(typeof(T));
        }

        public static object CompilAndBuildObject(string code, Type type, string typeName)
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

#if DEBUG
	                sb.Append(text);
	                sb.AppendLine();
	                sb.AppendLine();
#endif

                foreach (CompilerError error in results.Errors)
                    sb.AppendLine($"Error ({error.ErrorNumber}): {error.ErrorText}");

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
                    return Activator.CreateInstance(_type);
                }
                catch (Exception e)
                {
                    Logger.LogExcept(new IncompatibleTypeException($"An exception occured when trying to create an object of the type '{typeName}' (inherited from '{type.Namespace}.{type.Name}').", e));
                }
            }
            else
            {
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{typeName}' was no '{nameof(IWebService)}' (inherited from '{type.Namespace}.{type.Name}')."));
            }

            throw new InvalidOperationException($"Failed to retrieve generated {nameof(IWebService)} '{typeName}' (inherited from '{type.Namespace}.{type.Name}').");
        }

        public static object GetWebServiceLocalImpl(Type type)
        {
            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new LamestWebserver.WebServices.Generators.LocalWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceLocalImplName(type), AssemblyNameSpace = type.Namespace };

            return CompilAndBuildObject(webservice.TransformText().Replace("global::", ""), type, GetWebServiceLocalImplName(type) + "." + GetWebServiceLocalImplName(type));
        }

        public static string GetWebServiceLocalImplName<T>() => GetWebServiceLocalImplName(typeof(T));
        public static string GetWebServiceLocalImplName(Type type) => type.Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceLocalImplNamespace() => "LamestWebserver.WebService.GeneratedCode.Local";

        public static string GetWebServiceResponseImplName<T>() => GetWebServiceResponseImplName(typeof(T));
        public static string GetWebServiceResponseImplName(Type type) => type.Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceResponseImplNamespace() => "LamestWebserver.WebService.GeneratedCode.Response";

        public static string GetWebServiceRequestImplName<T>() => GetWebServiceRequestImplName(typeof(T));
        public static string GetWebServiceRequestImplName(Type type) => type.Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceRequestImplNamespace() => "LamestWebserver.WebService.GeneratedCode.Request";
    }
}
