using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.Attributes;
using LamestWebserver.Collections;
using LamestWebserver.Core;

namespace lwshostcore
{
    public class Host
    {
        private FileSystemWatcher fileSystemWatcher;
        private string ID = SessionContainer.GenerateHash();
        private string directoryPath;

        public AVLHashMap<string, IEnumerable<Type>> TypesPerFile = new AVLHashMap<string, IEnumerable<Type>>();

        public event Action<string> OnPageRegister = (s) => { };

        public Host(string directory, Action<string> OnPageRegister = null)
        {
            directoryPath = directory;

            if (OnPageRegister != null)
                this.OnPageRegister = OnPageRegister;

            ServerHandler.LogMessage("Reading Directory...");

            ThreadedWorker worker = new ThreadedWorker();

            foreach (var file in Directory.GetFiles(directory))
            {
                worker.EnqueueJob((Action<string>) ProcessFile, file);
            }

            worker.Join(null);

            ServerHandler.LogMessage("Starting FileSystemWatcher...");

            fileSystemWatcher = new FileSystemWatcher(directory);
            RegisterFileSystemWatcher();

            ServerHandler.LogMessage("FileSystemWatcher is now listening.");
        }

        private void RegisterFileSystemWatcher()
        {
            fileSystemWatcher.Changed += (sender, args) =>
            {
                ID = SessionContainer.GenerateHash();
                ProcessFile(args.FullPath);

                if (args.FullPath.EndsWith(".exe") || args.FullPath.EndsWith(".dll"))
                    ServerHandler.LogMessage("[lwshost] [Updated File] " + args.FullPath);
            };

            fileSystemWatcher.Created += (sender, args) =>
            {
                ProcessFile(args.FullPath);

                if (args.FullPath.EndsWith(".exe") || args.FullPath.EndsWith(".dll"))
                    ServerHandler.LogMessage("[lwshost] [Added File] " + args.FullPath);
            };

            fileSystemWatcher.Renamed += (sender, args) =>
            {
                if (TypesPerFile.ContainsKey(args.OldFullPath))
                {
                    TypesPerFile[args.FullPath] = TypesPerFile[args.OldFullPath];
                    TypesPerFile.Remove(args.OldFullPath);
                }

                ServerHandler.LogMessage("[lwshost] [Renamed File] " + args.FullPath);
            };

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void ProcessFile(string file)
        {
            if (!(file.EndsWith(".dll") || file.EndsWith(".exe")))
                return;

            ServerHandler.LogMessage("[lwshost] [Processing File] " + file);

            try
            {
                string newFileName = Directory.GetCurrentDirectory() + "\\lwshost\\CurrentRun\\" + ID + file.Replace(directoryPath, "");

                if (!Directory.Exists("lwshost\\CurrentRun\\" + ID))
                    Directory.CreateDirectory("lwshost\\CurrentRun\\" + ID);

                File.Copy(file, newFileName);
                Thread.Sleep(100);
                file = newFileName;

                ServerHandler.LogMessage("[lwshost] [File Load] Created a local copy at " + newFileName);
            }
            catch (IOException e)
            {
                ServerHandler.LogMessage("[lwshost] [File Load] Failed to copy file to currentRun directory:\n" + e);
            }

            try
            {
                var assembly = Assembly.LoadFile(file);
                var types = assembly.GetTypes();

                bool addedAnything = false;

                foreach (var type in types)
                {
                    bool ignoreDiscovery = false;

                    try
                    {
                        foreach (var attribute in type.GetCustomAttributes())
                            ignoreDiscovery |= (attribute is IgnoreDiscovery);

                        if (ignoreDiscovery)
                        {
                            ServerHandler.LogMessage($"[lwshost] [HostIgnore] Ignoring: {type.Namespace}.{type.Name}");
                            continue;
                        }
                        foreach (var interface_ in type.GetInterfaces())
                        {
                            try
                            {
                                if (interface_ == typeof(IURLIdentifyable))
                                {
                                    var constructor = type.GetConstructor(new Type[] {});

                                    if (constructor == null)
                                        continue;

                                    if (!addedAnything && TypesPerFile.ContainsKey(file))
                                    {
                                        foreach (var type_ in TypesPerFile[file])
                                        {
                                            foreach (var method_ in type_.GetMethods())
                                            {
                                                try
                                                {
                                                    if (method_.IsStatic && method_.IsPublic)
                                                    {
                                                        foreach (var attribute in method_.GetCustomAttributes())
                                                        {
                                                            if (attribute is ExecuteOnUnload)
                                                            {
                                                                new Thread(() =>
                                                                {
                                                                    try
                                                                    {
                                                                        ServerHandler.LogMessage(
                                                                            $"[lwshost] [File Load] Execute on Unload: {type_.Namespace}.{type_.Name}.{method_.Name} (in {file})");

                                                                        method_.Invoke(null, ((ExecuteOnUnload) attribute).Args);
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        ServerHandler.LogMessage(
                                                                            $"[lwshost] [File Load] Failed to execute on unload: {type_.Namespace}.{type_.Name}.{method_.Name} (in {file})\n" +
                                                                            e);
                                                                    }
                                                                }).Start();

                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception)
                                                {

                                                }
                                            }
                                        }

                                        TypesPerFile.Remove(file);
                                    }

                                    OnPageRegister(type.Namespace + "." + type.Name);

                                    new Thread(() =>
                                    {
                                        try
                                        {
                                            ServerHandler.LogMessage("[lwshost] [Adding / Updating] " + type.Namespace + "." + type.Name);
                                            constructor.Invoke(new object[0]);
                                        }
                                        catch (Exception e)
                                        {
                                            ServerHandler.LogMessage("[lwshost] [Error] Failed to add '" + type.Namespace + "." + type.Name + "'\n" + e);
                                        }
                                    }).Start();

                                    addedAnything = true;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                foreach (var type in types)
                {
                    foreach (var method_ in type.GetMethods())
                    {
                        try
                        {
                            if (method_.IsStatic && method_.IsPublic)
                            {
                                foreach (var attribute in method_.GetCustomAttributes())
                                {
                                    if (attribute is ExecuteOnLoad)
                                    {
                                        new Thread(() =>
                                        {
                                            try
                                            {
                                                ServerHandler.LogMessage($"[lwshost] [File Load] Execute on Load: {type.Namespace}.{type.Name}.{method_.Name} (in {file})");
                                                method_.Invoke(null, ((ExecuteOnLoad) attribute).Args);
                                            }
                                            catch (Exception e)
                                            {
                                                ServerHandler.LogMessage(
                                                    $"[lwshost] [File Load] Failed to execute on Load: {type.Namespace}.{type.Name}.{method_.Name} (in {file})\n" + e);
                                            }
                                        }).Start();

                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }

                if (addedAnything)
                    TypesPerFile.Add(file, types);
            }
            catch (ReflectionTypeLoadException e)
            {
                ServerHandler.LogMessage("[lwshost] Failed to load Assembly " + file + " (" + e.LoaderExceptions.Length + " Loader Exceptions)");

                for (int i = 0; i < (e.LoaderExceptions.Length > 25 ? 20 : e.LoaderExceptions.Length); i++)
                {
                    ServerHandler.LogMessage($"[lwshost] ({(i + 1)}/{e.LoaderExceptions.Length}) {e.LoaderExceptions[i].Message}");
                }

                if (e.LoaderExceptions.Length > 25)
                    ServerHandler.LogMessage(" [...] " + (e.LoaderExceptions.Length - 20) + " more Loader Exceptions.");
            }
            catch (Exception e)
            {
                ServerHandler.LogMessage("[lwshost] Failed to load Assembly " + file + "\n" + e);
            }
        }

        public void Stop()
        {
            fileSystemWatcher.EnableRaisingEvents = false;

            foreach (var types in TypesPerFile)
            {
                try
                {
                    foreach (var type_ in types.Value)
                    {
                        try
                        {
                            foreach (var method_ in type_.GetMethods())
                            {
                                try
                                {
                                    if (method_.IsStatic && method_.IsPublic)
                                    {
                                        foreach (var attribute in method_.GetCustomAttributes())
                                        {
                                            if (attribute is ExecuteOnUnload)
                                            {
                                                new Thread(() =>
                                                {
                                                    try
                                                    {
                                                        ServerHandler.LogMessage(
                                                            $"[lwshost] [File Load] Execute on Unload: {type_.Namespace}.{type_.Name}.{method_.Name} (in {types.Key})");

                                                        method_.Invoke(null, ((ExecuteOnUnload) attribute).Args);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        ServerHandler.LogMessage(
                                                            $"[lwshost] [File Load] Failed to execute on unload: {type_.Namespace}.{type_.Name}.{method_.Name} (in {types.Key})\n" +
                                                            e);
                                                    }
                                                }).Start();

                                                break;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
            }
        }
    }
}
