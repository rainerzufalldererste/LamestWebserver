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
            if (typeof(T).IsAbstract || typeof(T).IsInterface || !typeof(T).IsPublic || typeof(T).IsSealed)
                throw new IncompatibleTypeException("Only public non-abstract non-sealed Types of classes can be WebServices.");

            var webservice = new LamestWebserver.WebServices.Generators.LocalWebServiceTemplate() { ClassName = typeof(T).Name, ClassType = typeof(T), Namespace = GetWebServiceLocalImplNamespace(), AssemblyNameSpace = typeof(T).Namespace };
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
            Type type = assembly.GetType(GetWebServiceLocalImplNamespace() + "." + GetWebServiceLocalImplName<T>());

            if (type == null)
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{nameof(IWebService)}' (inherited from '{typeof(T).Namespace}.{typeof(T).Name}') could not be found."));
            else if (type.BaseType == typeof(T))
            {
                try
                {
                    object obj = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    return (T)obj;
                }
                catch(Exception e)
                {
                    Logger.LogExcept(new IncompatibleTypeException($"An exception occured when retrieving or calling the constructor of '{GetWebServiceLocalImplName<T>()}' (inherited from '{typeof(T).Namespace}.{typeof(T).Name}').", e));
                }
            }
            else
                Logger.LogExcept(new IncompatibleTypeException($"Compiled Type '{GetWebServiceLocalImplName<T>()}' was no '{nameof(IWebService)}' (inherited from '{typeof(T).Namespace}.{typeof(T).Name}')."));

            throw new InvalidOperationException($"Failed to retrieve generated {nameof(IWebService)} '{GetWebServiceLocalImplName<T>()}' (inherited from '{typeof(T).Namespace}.{typeof(T).Name}').");
        }

        public static string GetWebServiceLocalImplName<T>() => typeof(T).Name + "LocalWebServiceGenImpl";
        public static string GetWebServiceLocalImplNamespace() => "LamestWebserver.WebService.GeneratedCode.Local";
    }
}
