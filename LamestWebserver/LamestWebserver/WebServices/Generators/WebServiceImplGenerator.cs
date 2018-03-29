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

        public static object GetWebServiceLocalImpl(Type type)
        {
            if (type.IsAbstract || type.IsInterface || !type.IsPublic || type.IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new LamestWebserver.WebServices.Generators.LocalWebServiceTemplate() { ClassName = type.Name, ClassType = type, Namespace = GetWebServiceLocalImplNamespace(), AssemblyNameSpace = type.Namespace };
            string text = webservice.TransformText().Replace("global::", "");

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerParameters parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.AddRange(AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => a.Location).ToArray());
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            CompilerResults results = provider.CompileAssemblyFromSource(parameters, text);

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
            Type _type = assembly.GetType(GetWebServiceLocalImplNamespace() + "." + GetWebServiceLocalImplName(type));

            if (_type == null)
            {
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{nameof(IWebService)}' (inherited from '{type.Namespace}.{type.Name}') could not be found."));
            }
            else if (_type.BaseType == type)
            {
                try
                {
                    object obj = _type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    return obj;
                }
                catch (Exception e)
                {
                    Logger.LogExcept(new IncompatibleTypeException($"An exception occured when retrieving or calling the constructor of '{GetWebServiceLocalImplName(type)}' (inherited from '{type.Namespace}.{type.Name}').", e));
                }
            }
            else
            {
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{GetWebServiceLocalImplName(type)}' was no '{nameof(IWebService)}' (inherited from '{type.Namespace}.{type.Name}')."));
            }

            throw new InvalidOperationException($"Failed to retrieve generated {nameof(IWebService)} '{GetWebServiceLocalImplName(type)}' (inherited from '{type.Namespace}.{type.Name}').");
        }

        public static string GetWebServiceLocalImplName<T>() => typeof(T).Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceLocalImplName(Type type) => type.Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceLocalImplNamespace() => "LamestWebserver.WebService.GeneratedCode.Local";
    }
}
