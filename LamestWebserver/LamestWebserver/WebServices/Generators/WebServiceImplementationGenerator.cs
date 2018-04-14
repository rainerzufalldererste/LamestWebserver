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
    public class WebServiceImplementationGenerator
    {
        public static T GetWebServiceLocalImplementation<T>()
        {
            return (T)GetWebServiceLocalImplementation(typeof(T));
        }

        public static object GetWebServiceLocalImplementation(Type type)
        {
            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new LocalWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceLocalImplementationNamespace(), AssemblyNameSpace = type.Namespace };

            return CompileAndBuildObject(webservice.TransformText().Replace("global::", ""), type, GetWebServiceLocalImplementationNamespace() + "." + GetWebServiceLocalImplementationName(type));
        }

        public static T GetWebServiceRequestImplementation<T>()
        {
            return (T)GetWebServiceRequestImplementation(typeof(T));
        }

        public static object GetWebServiceRequestImplementation(Type type)
        {
            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new RequesterWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceRequestImplementationNamespace(), AssemblyNameSpace = type.Namespace };

            return CompileAndBuildObject(webservice.TransformText().Replace("global::", ""), type, GetWebServiceRequestImplementationNamespace() + "." + GetWebServiceRequestImplementationName(type));
        }

        public static object CompileAndBuildObject(string code, Type type, string typeName)
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

        public static string GetWebServiceLocalImplementationName<T>() => GetWebServiceLocalImplementationName(typeof(T));
        public static string GetWebServiceLocalImplementationName(Type type) => type.Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceLocalImplementationNamespace() => "LamestWebserver.WebService.GeneratedCode.Local";

        public static string GetWebServiceRequestImplementationName<T>() => GetWebServiceRequestImplementationName(typeof(T));
        public static string GetWebServiceRequestImplementationName(Type type) => type.Name + "RequesterWebServiceImpl";
        public static string GetWebServiceRequestImplementationNamespace() => "LamestWebserver.WebService.GeneratedCode.Request";
    }
}
