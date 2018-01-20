using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LamestWebserver.Collections;
using LamestWebserver.Serialization;
using LamestWebserver.Synchronization;
using LamestWebserver.RequestHandlers.DebugView;
using LamestWebserver.UI;

namespace LamestWebserver.RequestHandlers
{
    /// <summary>
    /// A RequestHandler contains tools to resolve HTTP-Requests to responses.
    /// </summary>
    public class RequestHandler : IDebugRespondable
    {
        /// <summary>
        /// The RequestHandler used across all default Webserver Instances.
        /// </summary>
        public static readonly RequestHandler CurrentRequestHandler = new RequestHandler();

        /// <summary>
        /// The RequestHandlers to look through primarily.
        /// </summary>
        protected List<IRequestHandler> RequestHandlers = new List<IRequestHandler>();

        /// <summary>
        /// The RequestHandlers to look through seconarily (e.g. ErrorRequestHandlers).
        /// </summary>
        protected List<IRequestHandler> SecondaryRequestHandlers = new List<IRequestHandler>();

        /// <summary>
        /// A WriteLock to safely add and remove response handlers.
        /// </summary>
        protected UsableWriteLock RequestWriteLock = new UsableWriteLock();

        /// <summary>
        /// The Root DebugResponseNode.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        /// <summary>
        /// Constructs a new RequestHandler.
        /// </summary>
        /// <param name="debugResponseNodeName">The DebugView name for this RequestHandler.</param>
        public RequestHandler(string debugResponseNodeName = null)
        {
            DebugResponseNode = new DebugContainerResponseNode(debugResponseNodeName == null ? GetType().Name : debugResponseNodeName, null, DebugViewResponse, null);
        }

        /// <summary>
        /// Adds a DebugResponseNode as Subnode to the Root DebugResponseNode.
        /// </summary>
        /// <param name="node">The node to add as subnode.</param>
        public void AddDebugResponseNode(DebugResponseNode node)
        {
            DebugResponseNode.AddNode(node);
        }

        /// <summary>
        /// Removes a DebugResponseNode from the Subnodes of the Root DebugResponseNode.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        public void RemoveDebugResponseNode(DebugResponseNode node)
        {
            DebugResponseNode.RemoveNode(node);
        }

        /// <summary>
        /// Clears the subnodes of the Root DebugResponseNode.
        /// </summary>
        public void ClearDebugResponseNodes()
        {
            DebugResponseNode.ClearNodes();
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        private HElement DebugViewResponse(SessionData sessionData)
        {
            return new HContainer
            {
                Elements =
                {
                    new HHeadline(nameof(RequestHandlers), 2),
                    new HList(HList.EListType.UnorderedList, (from rh in RequestHandlers select (rh is IDebugRespondable ? (HElement)DebugView.DebugResponseNode.GetLink((IDebugRespondable)rh) : new HItalic(rh.GetType().Name)))),
                    new HNewLine(),
                    new HHeadline(nameof(SecondaryRequestHandlers), 2),
                    new HList(HList.EListType.UnorderedList, (from srh in SecondaryRequestHandlers select (srh is IDebugRespondable ? (HElement)DebugView.DebugResponseNode.GetLink((IDebugRespondable)srh) : new HItalic(srh.GetType().Name))))
                }
            };
        }

        /// <summary>
        /// Retrieves a response (or null) from a given http packet by looking through all primary and secondary request handlers as long as none has a propper response to it.
        /// </summary>
        /// <param name="requestPacket">the http-packet to reply to</param>
        /// <returns>the response http packet or null</returns>
        public virtual HttpResponse GetResponse(HttpRequest requestPacket)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (requestPacket.RequestUrl.Length >= 2 && (requestPacket.RequestUrl[0] == ' ' || requestPacket.RequestUrl[0] == '/'))
            {
                requestPacket.RequestUrl = requestPacket.RequestUrl.Remove(0, 1);
            }

            using (RequestWriteLock.LockRead())
            {
                foreach (IRequestHandler requestHandler in RequestHandlers)
                {
                    var response = requestHandler.GetResponse(requestPacket, stopwatch);

                    if (response != null)
                    {
                        ServerHandler.LogMessage($"Completed Request on '{requestPacket.RequestUrl}' using '{requestHandler}'.", stopwatch);

                        return response;
                    }
                }

                foreach (IRequestHandler requestHandler in SecondaryRequestHandlers)
                {
                    var response = requestHandler.GetResponse(requestPacket, stopwatch);

                    if (response != null)
                    {
                        ServerHandler.LogMessage($"Completed Request on '{requestPacket.RequestUrl}' using [secondary] '{requestHandler}'.", stopwatch);

                        return response;
                    }
                }
            }

            ServerHandler.LogMessage($"Failed to complete Request on '{requestPacket.RequestUrl}'.", stopwatch);

            return null;
        }

        /// <summary>
        /// Adds a new request handler.
        /// </summary>
        /// <param name="handler">the handler to add</param>
        public void AddRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                if (!RequestHandlers.Contains(handler))
                    RequestHandlers.Add(handler);
        }


        /// <summary>
        /// Adds a new request handler at the specified position (or 0).
        /// </summary>
        /// <param name="handler">the handler to add</param>
        /// <param name="index">the position where to add the request handler.</param>
        public void InsertRequestHandler(IRequestHandler handler, int index = 0)
        {
            using (RequestWriteLock.LockWrite())
                RequestHandlers.Insert(index, handler);
        }

        /// <summary>
        /// Removes a specific requestHandler.
        /// </summary>
        /// <param name="handler">the handler to remove.</param>
        public void RemoveRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                RequestHandlers.Remove(handler);
        }

        /// <summary>
        /// Removes all request handlers with a certain type.
        /// </summary>
        /// <param name="handlertype">the type of the handler.</param>
        public void RemoveRequestHandlers(Type handlertype)
        {
            using (RequestWriteLock.LockWrite())
                for (int i = RequestHandlers.Count - 1; i >= 0; i--)
                    if (RequestHandlers[i].GetType() == handlertype)
                        RequestHandlers.RemoveAt(i);
        }

        /// <summary>
        /// Inserts a secondary request handler at a specified position (or 0).
        /// </summary>
        /// <param name="handler">the handler to insert</param>
        /// <param name="index">the index where to insert the handler</param>
        public void InsertSecondaryRequestHandler(IRequestHandler handler, int index = 0)
        {
            using (RequestWriteLock.LockWrite())
            {
                if (SecondaryRequestHandlers.Contains(handler))
                    SecondaryRequestHandlers.Remove(handler);

                SecondaryRequestHandlers.Insert(index, handler);
            }
        }

        /// <summary>
        /// Removes a handler from the secondary request handlers
        /// </summary>
        /// <param name="handler">the handler to remove</param>
        public void RemoveSecondaryRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                SecondaryRequestHandlers.Remove(handler);
        }

        /// <summary>
        /// Removes all request handlers of a specific type from the secondary request handlers.
        /// </summary>
        /// <param name="handlertype">the type of the handlers to remove</param>
        public void RemoveSecondaryRequestHandlers(Type handlertype)
        {
            using (RequestWriteLock.LockWrite())
                for (int i = SecondaryRequestHandlers.Count - 1; i >= 0; i--)
                    if (SecondaryRequestHandlers[i].GetType() == handlertype)
                        SecondaryRequestHandlers.RemoveAt(i);
        }
    }

    /// <summary>
    /// An Interface for HTTP-Request handlers.
    /// </summary>
    public interface IRequestHandler : IEquatable<IRequestHandler>
    {
        /// <summary>
        /// Retrieves a response from a http-request.
        /// </summary>
        /// <param name="requestPacket">the request packet</param>
        /// <param name="currentStopwatch">a reference to a started response time stopwatch.</param>
        /// <returns>the response packet</returns>
        HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch);
    }

    /// <summary>
    /// The Request Handler that delivers files from local storage.
    /// </summary>
    public class FileRequestHandler : IRequestHandler
    {
        /// <summary>
        /// The folder in local storage, where the files are located.
        /// </summary>
        public readonly string Folder;

        /// <summary>
        /// Constructs a new FileRequestHandler.
        /// </summary>
        /// <param name="folder">the folder where to look for the the requested files.</param>
        public FileRequestHandler(string folder)
        {
            if (folder.EndsWith("\\"))
                folder = folder.Substring(0, folder.Length - 1);

            Folder = folder;
        }

        /// <inheritdoc />
        public virtual HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            string fileName = requestPacket.RequestUrl;
            byte[] contents = null;
            DateTime? lastModified = null;
            string extention = null;

            if (fileName.Length == 0 || fileName[0] != '/')
                fileName = fileName.Insert(0, "/");

            if (fileName[fileName.Length - 1] == '/') // is directory?
            {
                fileName += "index.html";
                extention = "html";

                if (File.Exists(Folder + fileName))
                {
                    contents = ReadFile(fileName);
                    lastModified = File.GetLastWriteTimeUtc(Folder + fileName);
                }
                else
                {
                    return null;
                }
            }
            else if (File.Exists(Folder + fileName))
            {
                extention = GetExtention(fileName);
                bool isBinary = FileIsBinary(fileName, extention);
                contents = ReadFile(fileName, isBinary);
                lastModified = File.GetLastWriteTimeUtc(Folder + fileName);
            }
            else
            {
                return null;
            }

            return new HttpResponse(requestPacket, contents) {ContentType = GetMimeType(extention), ModifiedDate = lastModified};
        }

        /// <summary>
        /// Reads a file from local storage.
        /// </summary>
        /// <param name="filename">the name of the file</param>
        /// <param name="isBinary">shall the file be read as binary file?</param>
        /// <returns>a byte[] contatining the file contents</returns>
        protected byte[] ReadFile(string filename, bool isBinary = false)
        {
            int i = 10;

            while (i-- > 0) // if the file has currently been changed you probably have to wait until the writing process has finished
            {
                try
                {
                    if (isBinary)
                    {
                        return File.ReadAllBytes(Folder + filename);
                    }

                    if (Equals(WebServer.GetEncoding(Folder + filename), Encoding.UTF8))
                    {
                        return File.ReadAllBytes(Folder + filename);
                    }

                    string content = File.ReadAllText(Folder + filename);
                    return Encoding.UTF8.GetBytes(content);
                }
                catch (IOException)
                {
                    Thread.Sleep(2); // if the file has currently been changed you probably have to wait until the writing process has finished
                }
            }

            throw new Exception("Failed to read from '" + filename + "'.");
        }

        /// <summary>
        /// Reads a file from local storage.
        /// </summary>
        /// <param name="filename">the name of the file</param>
        /// <returns>a byte[] contatining the file contents</returns>
        public static byte[] ReadFile(string filename)
        {
            int i = 10;

            while (i-- > 0) // if the file has currently been changed you probably have to wait until the writing process has finished
            {
                try
                {
                    if (FileIsBinary(filename, GetExtention(filename)))
                    {
                        return File.ReadAllBytes(filename);
                    }

                    if (Equals(WebServer.GetEncoding(filename), Encoding.UTF8))
                    {
                        return File.ReadAllBytes(filename);
                    }

                    string content = File.ReadAllText(filename);
                    return Encoding.UTF8.GetBytes(content);
                }
                catch (IOException)
                {
                    Thread.Sleep(2); // if the file has currently been changed you probably have to wait until the writing process has finished
                }
            }

            throw new Exception("Failed to read from '" + filename + "'.");
        }

        /// <summary>
        /// Checks if a given file should be binary.
        /// </summary>
        /// <param name="fileName">the name of the file</param>
        /// <param name="extention">the extention of the file</param>
        /// <returns></returns>
        public static bool FileIsBinary(string fileName, string extention)
        {
            if (fileName.Length < 2)
                return true;

            switch (extention)
            {
                case "html":
                case "css":
                case "js":
                case "txt":
                case "htm":
                case "xml":
                case "json":
                case "rtf":
                case "xhtml":
                case "shtml":
                case "csv":
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Eetrieves the extention of a file.
        /// </summary>
        /// <param name="fileName">the file name</param>
        /// <returns>the extention</returns>
        public static string GetExtention(string fileName)
        {
            if (fileName.Length < 2)
                return "";

            for (int i = fileName.Length - 2; i >= 0; i--)
            {
                if (fileName[i] == '.')
                {
                    fileName = fileName.Substring(i + 1).ToLower();
                    return fileName;
                }
            }

            return "";
        }

        /// <summary>
        /// Returns the mime-type of a given file.
        /// </summary>
        /// <param name="extention">the extention of the file</param>
        /// <returns>the mime-type as string</returns>
        public static string GetMimeType(string extention)
        {
            switch (extention)
            {
                case "html":
                    return "text/html";
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "htm":
                case "xhtml":
                case "shtml":
                    return "text/html";
                case "txt":
                    return "text/plain";
                case "png":
                    return "image/png";
                case "jpeg":
                case "jpg":
                case "jpe":
                    return "image/jpeg";
                case "pdf":
                    return "application/pdf";
                case "zip":
                    return "application/zip";
                case "ico":
                    return "image/x-icon";
                case "xml":
                    return "text/xml";
                case "json":
                    return "application/json";
                case "rtf":
                    return "text/rtf";
                case "csv":
                    return "text/comma-separated-values";
                case "doc":
                case "dot":
                    return "application/msword";
                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "xls":
                case "xla":
                    return "application/msexcel";
                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument. spreadsheetml.sheet";
                case "ppt":
                case "ppz":
                case "pps":
                case "pot":
                    return "application/mspowerpoint";
                case "gif":
                    return "image/gif";
                case "bmp":
                    return "image";
                case "wav":
                    return "audio/x-wav";
                case "mp2":
                case "mp3":
                case "aac":
                    return "audio/x-mpeg";
                case "aif":
                case "aiff":
                case "aifc":
                    return "audio/x-aiff";
                case "mpeg":
                case "mpg":
                case "mpe":
                    return "video/mpeg";
                case "qt":
                case "mov":
                    return "video/quicktime";
                case "avi":
                    return "video/x-msvideo";
                case "tiff":
                case "tif":
                    return "image/tiff";
                case "swf":
                case "cab":
                    return "application/x-shockwave-flash";
                case "hlp":
                case "chm":
                    return "application/mshelp";
                case "midi":
                case "mid":
                    return "audio/x-midi";
                default:
                    return "application/octet-stream";
            }
        }

        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            if (((FileRequestHandler)(other)).Folder != Folder)
                return false;

            return true;
        }
    }

    /// <summary>
    /// A RequestHanlder that delivers Files from local storage - which will be cached on use.
    /// </summary>
    public class CachedFileRequestHandler : FileRequestHandler, IDebugRespondable
    {
        /// <summary>
        /// The size of the cache hash map. this does not limit the amount of cached items - it's just there to preference size or performance.
        /// </summary>
        public static int CacheHashMapSize = 1024;

        internal AVLHashMap<string, PreloadedFile> Cache = new AVLHashMap<string, PreloadedFile>(CacheHashMapSize);
        internal FileSystemWatcher FileSystemWatcher = null;
        internal UsableLockSimple CacheMutex = new UsableLockSimple();
        internal static readonly byte[] CrLf = Encoding.UTF8.GetBytes("\r\n");

        /// <summary>
        /// The DebugResponseNode for this CachedFileRequestHandler.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        /// <inheritdoc />
        public CachedFileRequestHandler(string folder) : base(folder)
        {
            DebugResponseNode = new DebugContainerResponseNode(GetType().Name, null, GetDebugResponse, RequestHandler.CurrentRequestHandler.DebugResponseNode);
            SetupFileSystemWatcher();
        }

        /// <inheritdoc />
        public override HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            string fileName = requestPacket.RequestUrl;
            byte[] contents = null;
            DateTime? lastModified = null;
            PreloadedFile file;
            bool notModified = false;
            string extention = null;

            try
            {
                if (fileName.Length == 0 || fileName[0] != '/')
                    fileName = fileName.Insert(0, "/");

                if (fileName[fileName.Length - 1] == '/') // is directory?
                {
                    fileName += "index.html";
                    extention = "html";

                    if (GetFromCache(fileName, out file))
                    {
                        lastModified = file.LastModified;

                        if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value < lastModified)
                        {
                            contents = file.Contents;

                            extention = GetExtention(fileName);
                        }
                        else
                        {
                            contents = file.Contents;

                            notModified = requestPacket.ModifiedDate != null;
                        }
                    }
                    else if (File.Exists(Folder + fileName))
                    {
                        contents = ReadFile(fileName);
                        lastModified = File.GetLastWriteTimeUtc(Folder + fileName);

                        using (CacheMutex.Lock())
                        {
                            Cache.Add(fileName, new PreloadedFile(fileName, contents, lastModified.Value, false));
                        }

                        ServerHandler.LogMessage("The URL '" + requestPacket.RequestUrl + "' is now available through the cache.");
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (GetFromCache(fileName, out file))
                {
                    extention = GetExtention(fileName);
                    lastModified = file.LastModified;

                    if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value < lastModified)
                    {
                        contents = file.Contents;

                        extention = GetExtention(fileName);
                    }
                    else
                    {
                        contents = file.Contents;

                        notModified = requestPacket.ModifiedDate != null;
                    }
                }
                else if (File.Exists(Folder + fileName))
                {
                    extention = GetExtention(fileName);
                    bool isBinary = FileIsBinary(fileName, extention);
                    contents = ReadFile(fileName, isBinary);
                    lastModified = File.GetLastWriteTimeUtc(Folder + fileName);

                    using (CacheMutex.Lock())
                    {
                        Cache.Add(fileName, new PreloadedFile(fileName, contents, lastModified.Value, isBinary));
                    }

                    ServerHandler.LogMessage("The URL '" + requestPacket.RequestUrl + "' is now available through the cache.");
                }
                else
                {
                    return null;
                }

                if (notModified)
                {
                    return new HttpResponse(null, CrLf) { Status = "304 Not Modified", ContentType = null, ModifiedDate = lastModified };
                }
                else
                {
                    return new HttpResponse(requestPacket, contents) { ContentType = GetMimeType(extention), ModifiedDate = lastModified };
                }
            }
            catch (Exception e)
            {
                return new HttpResponse(null, Master.GetErrorMsg(
                        "Error 500: Internal Server Error",
                        "<p>An Exception occurred while sending the response:<br><br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                        + WebServer.GetErrorMsg(e, null, requestPacket.RawRequest).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                        + "</div>"))
                {
                    Status = "500 Internal Server Error"
                };
            }
        }

        internal void SetupFileSystemWatcher()
        {
            if (!Directory.Exists(Folder))
            {
                ServerHandler.LogMessage($"The given Directory '{Folder}' to deliver responses from does not exist. Server Environment Path: '{Environment.CurrentDirectory}'.");
                return;
            }

            FileSystemWatcher = new FileSystemWatcher(Folder);

            FileSystemWatcher.Renamed += (object sender, RenamedEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file, oldfile = Cache["/" + e.OldName];

                    try
                    {
                        if (Cache.TryGetValue(e.OldName, out file))
                        {
                            Cache.Remove("/" + e.OldName);
                            file.Filename = "/" + e.Name;
                            file.Contents = ReadFile(file.Filename, file.IsBinary);
                            file.Size = file.Contents.Length;
                            file.LastModified = File.GetLastWriteTimeUtc(Folder + e.Name);
                            Cache.Add(e.Name, file);
                        }
                    }
                    catch (Exception)
                    {
                        oldfile.Filename = "/" + e.Name;
                        Cache["/" + e.Name] = oldfile;
                    }
                }

                ServerHandler.LogMessage("The URL '" + e.OldName + "' has been renamed to '" + e.Name + "' in the cache and filesystem.");
            };

            FileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    Cache.Remove("/" + e.Name);
                }

                ServerHandler.LogMessage("The URL '" + e.Name + "' has been deleted from the cache and filesystem.");
            };

            FileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                using (CacheMutex.Lock())
                {
                    PreloadedFile file = Cache["/" + e.Name];

                    try
                    {
                        if (file != null)
                        {
                            file.Contents = ReadFile(file.Filename, file.IsBinary);
                            file.Size = file.Contents.Length;
                            file.LastModified = DateTime.Now;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                ServerHandler.LogMessage("The cached file of the URL '" + e.Name + "' has been updated.");
            };

            FileSystemWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Gets a file from the cache.
        /// </summary>
        /// <param name="name">the name of the file.</param>
        /// <param name="file">the PreloadedFile object of this file</param>
        /// <returns>true if found - false if not found.</returns>
        protected bool GetFromCache(string name, out PreloadedFile file)
        {
            using (CacheMutex.Lock())
            {
                if (Cache.TryGetValue(name, out file))
                {
                    file.LoadCount++;
                    file = (PreloadedFile)file.Clone();
                }
                else
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        private HElement GetDebugResponse(SessionData sessionData)
        {
            using (CacheMutex.Lock())
            {
                return new HContainer()
                {
                    Elements =
                    {
                        new HText($"Cache HashMap Size: {Cache.HashMap.Length}.\nCache HashMap Entry Count: {Cache.Count}.\nOperating Folder: '{Folder}'."),
                        new HLine(),
                        new HHeadline("Cached Entries", 3),
                        new HTable((from e in Cache select new List<HElement> { e.Key, e.Value.Size.ToString() + " bytes", e.Value.LoadCount.ToString(), e.Value.LastModified.ToString() }))
                        {
                            TableHeader = new List<HElement> { "File Name", "Size", "Load Count", "Last Modified" }
                        }
                    }
                };
            }
        }
    }

    /// <summary>
    /// Reads or writes an entire dictionary from / to a file which can be loaded at startup so that the file size is compressed and not anyone can easily look at the files inside.
    /// </summary>
    public class PackedFileRequestHandler : IRequestHandler
    {
        private readonly AVLHashMap<string, PreloadedFile> _cache;

        /// <summary>
        /// Creates a new PackedFileRequestHandler from a directory. Use SaveToPackedFile-Method to save it.
        /// </summary>
        /// <param name="directoryPath">the path of the directory to read</param>
        /// <param name="includeSubdirectories">shall subdirectories be included</param>
        /// <param name="HashMapSize">the size of the hashmap containing the files. this does not limit the number of contained files - only to preference performance or size.</param>
        public PackedFileRequestHandler(string directoryPath, bool includeSubdirectories, int? HashMapSize = null)
        {
            if (HashMapSize.HasValue)
                _cache = new AVLHashMap<string, PreloadedFile>();
            else
                _cache = new AVLHashMap<string, PreloadedFile>(1024);

            string[] files = Directory.GetFiles(directoryPath, "*", includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} is adding {files.Length} Files...");

            foreach (string file in files)
            {
                var stopwatch = Stopwatch.StartNew();

                var contents = FileRequestHandler.ReadFile(file);
                _cache.Add(file.Substring(directoryPath.Length).TrimStart('\\', '/').Replace("\\", "/"), new PreloadedFile(file, contents, DateTime.UtcNow, true));

                ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} added '{file}'.", stopwatch);
            }
        }

        /// <summary>
        /// Creates a new PackedFileRequestHandler from a packed file.
        /// </summary>
        /// <param name="filename">the name of the file to load</param>
        public PackedFileRequestHandler(string filename)
        {
            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} is reading Files from '{filename}'...");

            byte[] bytes = File.ReadAllBytes(filename);
            byte[] decompressed = Compression.GZipCompression.Decompress(bytes);
            
            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} decompressed Files: {bytes.Length} bytes -> {decompressed.Length} bytes.");

            _cache = Serializer.ReadBinaryDataInMemory<AVLHashMap<string, PreloadedFile>>(decompressed);

            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} deserialized {_cache.Count} Files.");
        }

        /// <summary>
        /// Saves the storage to a packed file.
        /// </summary>
        /// <param name="filename">the name of the packed file.</param>
        public void SaveToPackedFile(string filename)
        {
            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} is saving Files to '{filename}'...");

            byte[] bytes = Serializer.WriteBinaryDataInMemory(_cache);

            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} serialized {_cache.Count} Files.");

            if(File.Exists(filename))
                File.Delete(filename);

            byte[] compressed = Compression.GZipCompression.Compress(bytes);

            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} compressed Files: {bytes.Length} bytes -> {compressed.Length} bytes.");

            File.WriteAllBytes(filename, compressed);

            ServerHandler.LogMessage($"{nameof(PackedFileRequestHandler)} wrote Files to '{filename}'.");
        }

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            var file = _cache[requestPacket.RequestUrl];

            if (file == null)
                return null;

            if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value <= file.LastModified)
                return new HttpResponse(null, CachedFileRequestHandler.CrLf) { Status = "304 Not Modified", ContentType = null, ModifiedDate = file.LastModified };

            return new HttpResponse(requestPacket, file.Contents) { ContentType = FileRequestHandler.GetMimeType(FileRequestHandler.GetExtention(requestPacket.RequestUrl)), ModifiedDate = file.LastModified };
        }

        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            if (((PackedFileRequestHandler)(other))._cache.Count != _cache.Count)
                return false;

            foreach(var e in ((PackedFileRequestHandler)(other))._cache)
                if(!_cache.Contains(e))
                    return false;

            return true;
        }
    }

    /// <summary>
    /// A Cacheable preloaded file.
    /// </summary>
    [Serializable]
    public class PreloadedFile : ICloneable, IEquatable<PreloadedFile>
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Filename;

        /// <summary>
        /// The contents of the file.
        /// </summary>
        public byte[] Contents;

        /// <summary>
        /// The size of the file.
        /// </summary>
        public int Size;
        
        /// <summary>
        /// The last-modified date of the file.
        /// </summary>
        public DateTime LastModified;

        /// <summary>
        /// is the file just binary data?
        /// </summary>
        public bool IsBinary;

        /// <summary>
        /// The amount of times the file has been requested.
        /// </summary>
        public int LoadCount;

        /// <summary>
        /// Empty Deserialization constructor
        /// </summary>
        public PreloadedFile() { }

        /// <summary>
        /// Constructs a new Preloaded file.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="contents">The contents of the file.</param>
        /// <param name="lastModified">The last-modified date of the file.</param>
        /// <param name="isBinary">is the file just binary data?</param>
        public PreloadedFile(string filename, byte[] contents, DateTime lastModified, bool isBinary)
        {
            Filename = filename;
            Contents = contents;
            Size = contents.Length;
            LastModified = lastModified;
            IsBinary = isBinary;
            LoadCount = 1;
        }

        /// <inheritdoc />
        public object Clone()
        {
            return new PreloadedFile((string) Filename.Clone(), Contents.ToArray(), LastModified, IsBinary);
        }

        /// <inheritdoc />
        public bool Equals(PreloadedFile other)
        {
            if (other.Filename != Filename)
                return false;

            if (other.Contents != Contents)
                return false;

            if (other.IsBinary != IsBinary)
                return false;

            if (other.LastModified != LastModified)
                return false;

            // LoadCount would not be a good idea and Size should be already checked by Contents.

            return true;
        }
    }

    /// <summary>
    /// Displays error messages for every request that passed through.
    /// </summary>
    public class ErrorRequestHandler : IRequestHandler, IDebugRespondable
    {
        /// <summary>
        /// Shall this RequestHandler store DebugView information?
        /// </summary>
        public static bool StoreErrorMessages = true;

        /// <summary>
        /// The DebugResponseNode for this ErrorRequestHandler.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        private AVLHashMap<string, (int, int)> _accumulatedErrors;

        private readonly byte[] _icon;

        /// <summary>
        /// Creates a new ErrorRequestHandler.
        /// </summary>
        public ErrorRequestHandler()
        {
            _icon = Convert.FromBase64String("AAABAAEAQEAAAAEAIAAoQgAAFgAAACgAAABAAAAAgAAAAAEAIAAAAAAAAEIAABMLAAATCwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACNf/8DjYD/HI2C/z+Ngv9sjYP/o46E/8yNhf/rjYb//42H//WOiP/ejYn/wI2L/6ONjP9sjY3/P42O/xyNj/8DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjn3/Co1//1GOgP+8jYD//42B//+Ogv//jYT//42E//+Nhf//job//42H//+NiP//jYn//42K//+NjP//jYz//42N//+Nj///jY///42Q/7yNkv9RjJP/CgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAI58/wGOff81jn7/tI5+//+OgP//jYD//42C//+Ngv//jYP//46E//+Nhf//jof//42H//+NiP//jYn//42L//+Ni///jYz//42O//+Njv//jY///42R//+Nkv//jJP//42U//+Mlv+0jJb/NY2Y/wEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjnv/AY18/0COfP/cjX7//41+//+Nf///jYD//46B//+Ogv//joP//42F//+Nhv//job//42H//+NiP//jon//42K//+Ni///jYz//42O//+Njv//jZD//42R//+Nkv//jZT//42V//+Nlf//jJb//42X//+Nmf/cjZr/QYyc/wEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjnr/JY18/9mNff//jn3//41+//+OgP//jYD//42B//+Ngv//joP//42E//+Nhf//jYb//42H//+OiP//jYn//42L//+Ni///jY3//46N//+Nj///jZD//42R//+Nkv//jJP//42V//+Mlf//jZf//42Y//+Nmf//jZr//4yb//+MnP/ZjJ7/JQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACNev8Fjnv/l418//+OfP//jn3//41+//+Of///jYD//42B//+Ngv//joP//42E//+Ohf//job//42H//+NiP//jYn//42L//+NjP//jY3//42O//+Nj///jZD//42R//+Nkv//jZP//42U//+Nlf//jJb//42Y//+Nmf//jZr//4yb//+MnP//jJ3//4yf//+NoP+XjaH/BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACNev8cjnv/5I18//+NfP//jn3//41+//+Nf///jYD//42B//+Ngv//joP//42E//+Nhf//job//42H//+NiP//jYn//42L//+NjP//jYz//42O//+Njv//jZD//42R//+Nkv//jZP//4yU//+Nlv//jZf//4yY//+Nmf//jJv//4yb//+NnP//jJ7//4yf//+NoP//jKL//4yj/+SMpP8cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev9NjXv//458//+OfP//jn3//41+//+Of///joH//42B//+Ngv//joP//42E//+Nhf//job//42H//+Nif//jYn//46L//+NjP//jYz//42N//+Njv//jZD//42R//+Mkv//jJT//42U//+Mlf//jJf//4yY//+Nmf//jZr//42b//+MnP//jJ7//42f//+NoP//jKH//42j//+MpP//jKX//42m/00AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev9rjXv//458//+OfP//jn7//45+//+NgP//jYH//46B//+Og///jYP//46F//+Ohv//jYb//42H//+Nif//jYn//46K//+Ni///jYz//42N//+Nj///jZD//42R//+Mkv//jZP//42U//+Nlf//jZb//42Y//+Nmf//jJr//4yc//+Mnf//jJ7//42f//+NoP//jaH//4yj//+MpP//jKX//4ym//+MqP//jKn/awAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev+Djnr//418//+OfP//jn7//41+//+NgP//jYD//42B//+Og///joP//46E//+Ohf//jYf//46H//+NiP//jon//42L//+Ni///jYz//42O//+Nj///jY///42R//+Nkv//jZP//42V//+Nlv//jZf//42Y//+Nmf//jZr//42c//+Mnf//jZ7//42f//+NoP//jaL//4yi//+MpP//jKX//4yn//+Mp///jKj//4yp//+Mq/+DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACNev9pjnv//418//+Off//jX7//41+//+Nf///joD//46B//+Ogv//joP//42E//+Nhf//job//46I//+NiP//jYn//42L//+NjP//jYz//42N//+Mj///jZD//42R//+Nkv//jZP//42U//+Nlf//jZf//42X//+Nmf//jZr//42b//+Nnf//jZ7//4ye//+NoP//jKH//42i//+MpP//jKX//4ym//+Mp///jKn//4yq//+Mq///jKz//4yu/2kAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev9LjXv//457//+OfP//jn7//45+//+Nf///jYD//42C//+Ngv//jYP//42F//+Ohf//job//42H//+NiP//jYn//42L//+Oi///jY3//42N//+Njv//jZD//42R//+Nkv//jZP//42U//+Mlf//jZf//42X//+Mmf//jZr//42b//+MnP//jZ7//4yf//+MoP//jaH//4yj//+Mo///jKX//4yn//+Mp///jKn//42q//+Mq///jK3//4yu//+Mr///jLD/TAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev8cjXr//458//+NfP//jX3//45///+Nf///jYH//46B//+Ogv//jYP//42E//+Nhf//jYb//46I//+NiP//jYr//42K//+NjP//jYz//42O//+Nj///jZD//42R//+Nkv//jZT//42U//+Nlf//jZb//4yY//+Mmf//jJr//42c//+NnP//jZ7//4ye//+Mof//jKL//4yi//+MpP//jKX//42m//+Mp///jKj//4yq//+Nq///jKz//4yu//+Mrv//jLD//4yy//+Ms/8cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACOev8Fjnv/4o57//+Nff//jn3//45///+Nf///jYD//42B//+Ngv//jYP//42E//+Nhf//jYb//42H//+Nif//jYn//42K//+NjP//jY3//42O//+Nj///jZD//42R//+Nkv//jJP//42U//+Nlv//jZf//4yY//+Nmf//jJv//42b//+Mnf//jZ7//4yf//+MoP//jKH//4yj//+Mo///jaX//4ym//+Np///jan//4yq//+Mq///jKz//4yu//+Mr///jLD//4yy//+Lsv//jLX/4oy2/wUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjnv/l457//+OfP//jn3//45+//+Nf///jYD//42B//+Ng///joP//42E//+Ohf//jYb//46H//+NiP//jYr//42L//+OjP//jY3//42O//+Nj///jZD//42R//+Nkv//jZT//42V//+Nlf//jJb//4yX//+Mmf//jZr//42b//+NnP//jJ7//42f//+Mof//jKL//42i//+NpP//jKX//4ym//+Mp///jKn//4yq//+Mq///jK3//4yt//+Mr///jLD//4yx//+Ms///jLT//4u1//+Lt/+XAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjXv/JY17//+Off//jX7//45+//+Nf///joD//46B//+Ogv//jYP//42E//+Nhf//jof//42H//+NiP//jYr//42K//+NjP//jYz//42O//+Njv//jZD//42R//+Nkv//jZP/8Y2U/9WMlv/FjZf/u4yY/7uNmf/FjZr/1Yyb//GMnf//jZ7//42f//+MoP//jKL//4yj//+MpP//jKX//4ym//+Mp///jan//4yq//+Mq///jKz//42t//+Mr///jLD//4yx//+Ls///jLX//4y2//+Mt///jLj//4u6/yUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjnv/AY18/9eOfP//jn3//41+//+Nf///joD//42B//+Ngv//joP//46E//+Nhf//jYb//42H//+Nif//jYn//42K//+Ni///jY3//42N//+Nj///jZD//42R//eNk/+sjZT/co2U/z6Mlf8YjJf/AQAAAAAAAAAAjJv/AY2b/xiNnP8+jZ3/co2f/6yMoP/3jKL//4yj//+MpP//jKX//42m//+MqP//jKn//42q//+Mq///jKz//42u//+Mr///jLD//4yx//+Ms///jLT//4y2//+Mt///jLn//4y5//+Lu//XjL3/AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAI57/z+Off//jX3//41+//+Nf///jYD//46C//+Ngv//jYP//46E//+Nhv//jYb//42H//+NiP//jYn//42K//+Ni///jY3//42N//+Nj///jZD//42R/5CNkv85AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjaH/OYyj/4+Mo///jKX//4yn//+MqP//jKn//42p//+Mq///jKz//4yu//+Mrv//jLD//4yx//+Ms///jLX//4y2//+Mt///jLj//4u6//+Lu///jL3//4y+/0AAAAAAAAAAAAAAAAAAAAAAAAAAAI57/wGOff/ejX3//41+//+Nf///joH//42B//+Ngv//jYP//42E//+Ohf//jYb//42H//+OiP//jon//42L//+Ni///jY3//42O//+Njv//jY//xY2R/zgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjKX/N4ym/8WMp///jan//4yp//+MrP//jK3//4yt//+Mr///jLD//4yx//+Lsv//jLT//4y2//+Mt///jLn//4u6//+Mu///i73//4y+//+MwP/ejMH/AQAAAAAAAAAAAAAAAAAAAACNfP81jn3//45///+Nf///jYD//42B//+Ng///jYP//46E//+Ohf//jYb//42H//+Oif//jon//42K//+Ni///jY3//42N//+Nj///jZD/mo2R/w8AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMp/8PjKn/moyq//+Mq///jKz//4yu//+Mr///jLD//4yx//+Ms///jLT//4u2//+Lt///i7n//4u6//+Mu///jL3//4u9//+Mv///jMD//4zC/zUAAAAAAAAAAAAAAAAAAAAAjn7/vI1+//+Of///jYH//42B//+Ogv//jYP//46E//+Nhv//jYb//42I//+NiP//jYn//46K//+NjP//jY3//42N//+Nj///jZD/jgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMq/+OjK3//4yt//+Mrv//jLD//4yx//+Ms///jLX//4y1//+Lt///i7n//4y6//+Mu///jL3//4u+//+Mv///i8H//4vC//+Lw/+8AAAAAAAAAAAAAAAAjX3/Do1///+NgP//jYH//46B//+Ogv//jYP//42F//+Nhf//jYb//42I//+NiP//jYn//42K//+Ni///jY3//46O//+Njv//jZD/ngAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIyt/56Mr///jLD//4yx//+Msv//jLT//4y1//+Lt///jLn//4y6//+Lu///jLz//4u+//+Lv///jMH//4vC//+Mw///i8X//4vG/w4AAAAAAAAAAI1//1GOf///jYD//46B//+Ngv//jYP//42F//+Ohv//jYb//42I//+NiP//jYn//42K//+NjP//jYz//42N//+Nj///jZD/yo2R/wkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMrv8JjLD/yoyx//+Ms///jLT//4y1//+Lt///i7n//4u6//+Lu///i7z//4u9//+LwP//i8H//4zB//+Lw///i8T//4vG//+Lx/9RAAAAAAAAAACOf/+3joD//42B//+Ngv//jYP//46E//+Nhf//jYf//42H//+Nif//jYn//42K//+Ni///jYz//42N//+Njv//jZD/942R/zUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIyy/zWMs//3jLT//4y1//+Mtv//jLj//4u6//+Lu///jLz//4u+//+Mv///jMD//4vC//+MxP//i8T//4vG//+Lx///i8j/uQAAAACNf/8DjYD//42B//+Ngv//jYP//42E//+Nhf//jYb//42H//+NiP//jYr//42L//+Oi///jY3//42N//+Nj///jZD//42R/5QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjLT/lIy2//+Mt///jLn//4y5//+Lu///jLz//4y+//+Mv///i8D//4vC//+Lw///jMX//4vG//+Lx///i8j//4vK//+Ly/8EjoH/HY6C//+Ngv//joP//42F//+Ohf//job//42H//+NiP//jYn//42L//+Ni///jY3//42O//+Njv//jY///42R//ONkv8xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIy1/zGMt//zjLn//4y5//+Lu///jLz//4u+//+LwP//i8D//4vC//+Mw///jMX//4vG//+LyP//i8n//4zK//+Ly///i8z/HY2B/0SNgv//joP//42E//+Nhf//job//42H//+Nif//jYn//42L//+Ni///jYz//42O//+Njv//jY///42R//+Nkv+wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjLj/sIy6//+Lu///jLz//4u+//+Mv///i8D//4zC//+Lw///jMX//4vG//+Lx///i8n//4vK//+Ly///i83//4vO/0SNgv91jYT//42E//+Nhf//job//42H//+NiP//jYn//42K//+NjP//jY3//42O//+Nj///jZD//42R//+Nkv//jZP/cQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIu6/3CMu///jLz//4u+//+Mv///jMD//4zB//+Lw///i8X//4vG//+Lx///jMn//4vK//+Ly///i83//4vO//+Lz/91joP/o42E//+Ohv//jYb//46I//+Nif//jon//42K//+Ni///jYz//42O//+Nj///jJD//42R//+Nkv//jZP/9Y2V/z4AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLu/8+i73/9Yy+//+Mv///jMD//4vB//+Lw///jMX//4vG//+Mx///i8n//4vK//+Ly///i83//4vO//+Lz///i9D/o42E/86Nhf//jYb//46H//+NiP//jYn//42K//+NjP//jY3//42O//+Nj///jY///42R//+Nkv//jZP//42U/9WNlf8XAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAi7z/Foy+/9WMv///i8H//4zC//+Mw///jMT//4zG//+Lx///i8n//4zK//+My///i8z//4vO//+Lz///i9D//4vS/8WNhf/mjYb//42H//+NiP//jon//42K//+NjP//jYz//42O//+Nj///jY///42Q//+Nkv//jZP//42U//+Nlf/EjZb/AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMv//Di8H//4zC//+Lw///jMT//4zF//+Lx///jMn//4vJ//+Ly///i8z//4vO//+Lz///i9H//4vR//+L0//ajYb//42H//+OiP//jYr//42K//+Ni///jY3//42N//+Nj///jZD//42R//+Nkv//jZP//4yU//+Nlf//jZf/vAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjMD/vIzB//+Lw///i8T//4vG//+LyP//i8j//4vK//+My///i8z//4vO//+Lz///i9H//4vR//+L0///i9T/842H//+NiP//jYn//42K//+Ni///jY3//42O//+Nj///jZD//42R//+Nkv//jZT//42U//+Nlv//jJf//42Y/7wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvC/7yLw///i8X//4vG//+Mx///i8j//4vK//+Ly///jM3//4vO//+Lz///i9D//4vR//+L0///i9T//4vW//ONiP/mjon//42L//+Ni///jYz//42O//+Njv//jZD//4yR//+Mkv//jZT//42U//+Nlf//jZf//42X//+Nmf/EjZr/AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvC/wGLw//Di8T//4zG//+Lx///jMj//4zK//+LzP//i83//4vO//+Lz///i9H//4vS//+L0///itT//4vW//+L1v/ajor/zo2L//+Ni///jY3//42N//+Njv//jJD//42R//+Nkv//jZP//42V//+Nlf//jZf//42Y//+Nmf//jZr/1Yyb/xcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMw/8XjMX/1YvG//+Lx///i8j//4vK//+LzP//i8z//4vO//+Lz///i9D//4vR//+L0///i9T//4vV//+K1v//itf/xY2K/6ONi///jYz//42O//+Nj///jZD//42R//+Nkv//jZP//42U//+Mlf//jZf//42Y//+Nmf//jJr//42b//WNnP8/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAi8X/PovG//WLx///i8n//4zK//+Ly///i83//4vN//+Mz///i9H//4vS//+L0///itT//4vW//+L1v//i9j//4vY/6ONi/91jY3//42N//+Nj///jZD//42R//+Mk///jZP//42U//+Nlv//jZf//42Y//+Mmf//jJr//42b//+NnP//jJ3/cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvG/3GMx///i8n//4vK//+My///i83//4vO//+Mz///i9H//4vR//+L0v//i9T//4vV//+L1///i9f//4vZ//+L2v91jY3/RI2O//+Nj///jZD//42R//+Mkv//jZP//42V//+Nlf//jZf//4yX//+Mmf//jZr//4yb//+Nnf//jJ7//4yf/7AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLyP+wi8n//4vK//+My///i8z//4vN//+Lz///i9D//4vR//+L0///i9T//4vW//+L1///i9j//4vZ//+K2v//itv/RI2N/xyNj///jZD//42R//+Mkv//jZP//42U//+Nlv//jZf//42Y//+Nmf//jZr//42b//+MnP//jZ3//4yf//+NoP/zjaL/MQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLx/8xjMn/84zK//+Ly///i8z//4vO//+Lz///i9D//4vS//+K0///i9P//4vV//+L1v//i9j//4vZ//+L2f//i9v//4vc/x2Nj/8DjZD//42R//+Nkv//jZP//42U//+Mlv//jJf//42Y//+Nmf//jZr//4yc//+MnP//jZ7//42f//+NoP//jKH//42i/5YAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjMn/lozJ//+Ly///i83//4vO//+Lz///i9D//4vR//+L0///itT//4rV//+L1v//i9j//4vZ//+K2v//i9v//4vd//+L3f8DAAAAAI2R/7WNkv//jZP//42U//+Mlv//jZf//42Y//+Nmf//jZr//4yc//+Mnf//jZ7//42f//+NoP//jKL//4yi//+MpP/3jaX/NgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjMj/NozK//eLy///i83//4vO//+Lzv//i9D//4vR//+L0v//i9T//4vW//+L1v//itj//4rZ//+L2v//i9v//4vd//+K3f+3AAAAAAAAAACNkv9QjZP//42U//+Nlf//jZf//4yY//+Mmf//jZr//4yc//+NnP//jZ7//4ye//+Mof//jKH//4yj//+MpP//jKT//42m/8uMp/8KAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAi8j/CYvK/8uLzP//i8z//4vO//+Lz///i9D//4vR//+L0///i9T//4vV//+L1v//itf//4vZ//+L2v//itv//4vc//+K3v//it7/UQAAAAAAAAAAjZP/DYyU//+Mlv//jZb//42Y//+Mmf//jZr//4yb//+NnP//jZ3//4yf//+NoP//jaL//4yi//+MpP//jKT//4ym//+Mp///jKn/nwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvK/56Ly///i83//4vN//+Lz///i9D//4vR//+L0///i9T//4vV//+L1v//itj//4rZ//+L2v//itv//4rc//+K3f//it///4rg/w0AAAAAAAAAAAAAAACNlv+6jZb//42X//+Nmf//jZr//42b//+Nnf//jJ7//4yf//+NoP//jKH//4yj//+No///jKX//4ym//+Mp///jKj//4yq//+Mq/+OAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvK/46Ly///i83//4vN//+Lz///i9D//4vS//+L0///i9T//4vV//+L1v//i9f//4vZ//+K2v//i9v//4vc//+K3v//i97//4vf/7wAAAAAAAAAAAAAAAAAAAAAjZb/NY2Y//+Nmf//jJr//4yb//+Mnf//jJ3//4yf//+MoP//jKH//42j//+MpP//jKX//4yn//+NqP//jan//4yq//+Mq///jK3//4yu/5qMr/8PAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjMn/D4vK/5qLy///i8z//4vO//+Lz///i9D//4vR//+L0v//i9T//4vV//+L1///i9f//4vZ//+L2f//itv//4vc//+L3f//i9///4rf//+K4f81AAAAAAAAAAAAAAAAAAAAAIyY/wGMmf/cjZr//42c//+Nnf//jZ7//42f//+Mof//jaL//42j//+MpP//jKX//4ym//+MqP//jKj//4yq//+Mq///jKz//42t//+Mr///jLD/x4yx/zkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAi8n/OYzK/8WLy///i83//4vO//+Lz///i9D//4vS//+L0///i9T//4vV//+L1v//i9j//4vZ//+L2f//i9v//4vc//+L3f//i9///4rf//+K4P/ci+H/AQAAAAAAAAAAAAAAAAAAAAAAAAAAjJr/P42b//+Mnf//jJ7//42f//+MoP//jKH//42j//+Mo///jKX//4ym//+MqP//jKj//42q//+Nq///jKz//4yt//+Mr///jLD//4yx//+Ms///jLT/joy1/zgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLx/85i8n/kIvK//+Ly///i8z//4vN//+Mz///i9D//4vS//+L0///i9T//4vV//+L1///itj//4vY//+L2v//itv//4vc//+K3f//i97//4rf//+L4f//iuL/PwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAI2b/wGNnf/XjJ7//42f//+MoP//jaH//42i//+MpP//jKX//4ym//+MqP//jKn//4yq//+Mq///jKz//4yu//+Mr///jLD//4yy//+Ms///jLT//4u2//+Mt//3jLj/rYy6/3OMu/8/jL3/GIu+/wEAAAAAAAAAAIzC/wGLw/8Yi8X/P4zG/3OLyP+ti8j/94vJ//+Ly///jMz//4vO//+Lz///i9D//4vR//+L0///i9P//4vV//+L1v//i9j//4vZ//+L2v//i9v//4rc//+L3v//i97//4vf//+K4f//iuL/14ri/wEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjJ3/JIyf//+MoP//jaL//42i//+NpP//jKX//42n//+Mp///jKj//4yq//+Mq///jKz//4yu//+Mr///jLD//4yx//+Msv//jLT//4y2//+Mt///i7j//4y6//+Mu///i73/8oy+/9WLv//FjMD/vIzC/7yLw//FjMT/1YzG//KLx///i8j//4vK//+Ly///i8z//4vO//+Lz///i9H//4vS//+L0///i9T//4vW//+L1///i9j//4vY//+L2v//itv//4vc//+L3v//i9///4rg//+K4f//i+L//4rj/yUAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMoP+VjKH//4yi//+NpP//jKX//42m//+MqP//jKn//4yq//+Mq///jK3//4yt//+Mr///jLD//4yx//+Msv//jLT//4y1//+Mt///i7j//4u6//+Lu///i73//4u+//+Mv///i8D//4zC//+Mw///jMX//4vG//+Lx///i8n//4zK//+Ly///i83//4zN//+Lz///i9D//4vR//+L0///i9T//4vV//+L1///itj//4vY//+L2v//i9v//4rc//+L3f//i9///4vg//+L4f//i+L//4vi/5UAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjKH/BYyi/+KNo///jKX//42m//+Mp///jKn//4yp//+Mq///jK3//4yt//+Mrv//jLD//4yx//+Ms///jLT//4y1//+Mt///jLn//4y6//+Lu///jLz//4u9//+Lv///jMH//4vC//+Lw///jMT//4vG//+Lx///i8n//4vK//+Ly///i8z//4zO//+Lz///i9D//4vR//+L0///i9T//4vV//+L1v//i9f//4vY//+L2f//i9v//4rd//+L3f//i97//4vg//+L4P//iuL//4vi/+KL4/8FAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMpP8cjaX//4yn//+MqP//jKn//4yq//+Mq///jKz//4yu//+Mr///jLD//4yx//+Ls///jLT//4y2//+Mt///jLj//4u5//+Lu///i73//4u+//+LwP//i8H//4zC//+Mw///i8T//4vG//+Lx///i8n//4zJ//+Ly///i8z//4vN//+Lz///i9H//4vR//+L0v//i9T//4rV//+L1v//i9f//4rY//+L2v//i9v//4rc//+L3v//it///4vf//+L4P//iuL//4ri//+K4/8cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIym/0mMp///jKn//4yq//+Mq///jK3//4yu//+Mr///jLD//4yy//+Msv//jLX//4u1//+Mt///jLn//4u5//+Mu///jLz//4y+//+Mv///i8H//4zC//+Lw///jMX//4vG//+MyP//i8j//4vK//+Ly///i83//4vO//+Mz///i9D//4vR//+L0///i9T//4vV//+L1v//itj//4rZ//+K2v//i9v//4vc//+L3f//it7//4rg//+L4f//iuH//4rj//+K5P9KAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjaj/aYyq//+Nq///jKz//4yt//+Mr///jLD//4yy//+Ms///jLT//4y2//+Mtv//jLn//4y5//+Mu///i73//4y+//+Lv///jMH//4vC//+Lw///i8X//4vG//+Lx///i8j//4vK//+Ly///i83//4vO//+Lz///i9D//4vS//+L0///i9T//4vW//+L1v//i9j//4vZ//+K2v//i9v//4rc//+L3f//it7//4rg//+K4P//iuL//4ri//+L5P9pAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACMq/+DjKz//4yu//+Mr///jLD//4yy//+Ms///jLT//4y2//+Mt///jLj//4y6//+Lu///i73//4y+//+Mv///i8D//4zC//+Lw///jMX//4vG//+MyP//jMn//4zK//+LzP//i83//4vO//+Lz///i9D//4vR//+L0///i9T//4vV//+K1v//i9f//4rY//+L2v//i9v//4vc//+K3v//i97//4vg//+K4f//i+H//4vj//+K5P+DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIyu/2uMr///jLD//4yy//+Ms///jLT//4y2//+Mt///jLj//4u6//+Mu///jLz//4y+//+Lv///i8D//4vC//+MxP//i8T//4vG//+LyP//jMj//4zK//+Ly///i8z//4vN//+Lz///i9H//4vR//+L0v//i9T//4vV//+L1///i9j//4vY//+L2v//itv//4vd//+L3f//i97//4vf//+K4f//iuL//4vj//+K5P9rAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjLD/TYyx//+Msv//jLT//4y1//+Mt///i7j//4y6//+Mu///jL3//4y+//+Lv///jMH//4zC//+Mw///jMT//4vG//+Lx///jMj//4vK//+MzP//i83//4vO//+Lzv//i9D//4vS//+L0///i9T//4vV//+L1v//i9f//4rZ//+K2f//i9v//4vd//+L3f//i97//4rg//+L4P//i+L//4rj//+L4/9NAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLs/8cjLX/5Iu2//+Mt///jLn//4y6//+Mu///jLz//4u9//+Lv///jMH//4vC//+Mw///i8T//4vG//+LyP//jMn//4vK//+Ly///i8z//4vN//+Lz///i9D//4vR//+L0///i9T//4rV//+L1v//i9j//4rY//+K2v//i9v//4rc//+L3v//it7//4rf//+L4f//i+L//4vj/+SK4/8cAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIy2/wWMt/+XjLj//4y5//+Mu///i7z//4y9//+Lv///jMH//4zC//+LxP//i8X//4zG//+LyP//jMn//4zK//+Ly///i8z//4vO//+Lz///i9H//4vR//+L0///i9T//4vV//+K1///i9j//4vZ//+L2v//i9v//4rc//+K3f//i9///4vf//+K4f//iuL//4ri/5eK4/8FAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIy6/ySLu//Zi7z//4y+//+Mv///jMH//4vC//+Mw///i8X//4zG//+MyP//i8j//4vK//+LzP//i83//4vN//+Lz///i9D//4vS//+L0///i9T//4rV//+L1///i9j//4vZ//+L2v//itv//4rc//+K3f//it7//4vg//+K4f//iuH/2Yrj/yQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAi73/AYy+/z+Mv//cjMD//4vC//+Lw///i8X//4zG//+Lx///jMj//4vK//+Ly///i8z//4vO//+Lz///i9D//4vR//+L0///i9T//4vV//+L1v//i9f//4vZ//+L2v//itv//4rc//+L3f//i9///4rf//+K4f/ciuH/P4vi/wEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAjMH/AYvC/zWLw/+yi8X//4vF//+Mx///i8j//4vJ//+My///jMz//4vN//+Lz///i9D//4vR//+L0///i9T//4vV//+K1v//i9j//4vZ//+K2v//i9v//4rc//+L3f//i9///4vg/7KL4P81iuL/AQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIvF/wqLx/9Oi8j/tYvK//+Ly///i8z//4vO//+Lz///i9H//4vR//+L0v//i9T//4vW//+L1v//i9j//4vZ//+L2v//i9v//4rc//+K3v+1i97/Tovf/woAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACLy/8Di8z/HIvN/z2Lz/9ri9H/oovR/8qL0v/mi9T//4vW//+L1v/mi9j/yovY/6KL2v9ritv/PYvc/xyK3f8DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////AAD///////gAAB//////wAAAA/////8AAAAA/////gAAAAB////4AAAAAB////AAAAAAD///4AAAAAAH///AAAAAAAP//4AAAAAAAf//AAAAAAAA//4AAAAAAAB//AAAAAAAAD/4AAAAAAAAH/gAAAAAAAAf8AAAAAAAAA/gAAABgAAAB+AAAD/8AAAHwAAA//8AAAPAAAH//4AAA8AAB///4AADgAAP///wAAGAAA////AAAYAAH///+AABAAA////8AAAAAD////wAAAAAf////gAAAAB////+AAAAAH////4AAAAAf////gAAAAB/////AAAAAP////8AAAAA/////wAAAAB////+AAAAAH////4AAAAAf////gAAAAB////+AAAAAH////4AAAAAP////AAAAAA////8AACAAB////gAAYAAD///8AABgAAP///wAAHAAAf//+AAA8AAAf//gAADwAAA//8AAAPgAAA//AAAB+AAAAGAAAAH8AAAAAAAAA/4AAAAAAAAH/gAAAAAAAAf/AAAAAAAAD/+AAAAAAAAf/8AAAAAAAD//4AAAAAAAf//wAAAAAAD///gAAAAAAf///AAAAAAD///+AAAAAAf///+AAAAAH////8AAAAA/////8AAAAP/////+AAAH///////AAD///8=");

            DebugResponseNode = new DebugContainerResponseNode(GetType().Name, null, GetDebugResponse, RequestHandler.CurrentRequestHandler.DebugResponseNode);

            if (StoreErrorMessages)
                _accumulatedErrors = new AVLHashMap<string, (int, int)>();
        }

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            if(StoreErrorMessages && _accumulatedErrors != null)
            {
                (int count, int error) t = _accumulatedErrors[requestPacket.RequestUrl];

                t.count++;
                t.error = requestPacket.RequestUrl.EndsWith("/") ? 403 : 404;

                _accumulatedErrors[requestPacket.RequestUrl] = t;
            }

            if(requestPacket.RequestUrl == "favicon.ico")
            {
                return new HttpResponse(null, _icon)
                {
                    ContentType = "image/x-icon"
                };
            }
            else if (requestPacket.RequestUrl.EndsWith("/"))
            {
                return new HttpResponse(null, Master.GetErrorMsg(
                        "Error 403: Forbidden",
                        "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p>" +
                        "</div></p>"))
                {
                    Status = "403 Forbidden"
                };
            }
            else
            {
                return new HttpResponse(null, Master.GetErrorMsg(
                        "Error 404: Page Not Found",
                        "<p>The URL you requested did not match any page or file on the server.</p>" +

                        "</div></p>"))
                {
                    Status = "404 File Not Found"
                };
            }
        }

        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            return true;
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        private HElement GetDebugResponse(SessionData arg)
        {
            if (!StoreErrorMessages || _accumulatedErrors == null)
                return new HText($"This {GetType().Name} does not store Error-Information. If you want Error-Information to be stored enable the '{StoreErrorMessages}' Flag.") { Class = "warning" };

            return new HTable(
                (from KeyValuePair<string, (int count, int error)> e in
                     (from KeyValuePair<string, (int count, int error)> _e in _accumulatedErrors select _e).OrderByDescending(x => x.Value.count)
                 select new List<HElement>
                 {
                     e.Key,
                     e.Value.count.ToHElement(),
                     e.Value.error.ToHElement()
                 }))
            {
                TableHeader = new HElement[]
                {
                    "Requested URL", "Failed Requests", "Last HTTP-Error"
                }
            };
        }
    }

    /// <summary>
    /// Provides functionality for Retriable Responses (MutexRetryException triggered retrying)
    /// </summary>
    /// <typeparam name="T">the type of method to call</typeparam>
    public abstract class AbstractMutexRetriableResponse<T> : IRequestHandler
    {
        private readonly Random _random = new Random();

        /// <summary>
        /// The maximum amount of retries if deadlocks prevented the execution.
        /// </summary>
        public static int Retries = 10;

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            T requestFunction = GetResponseFunction(requestPacket);

            if (requestFunction == null)
                return null;

            int tries = 0;
            var sessionData = new HttpSessionData(requestPacket);

            RETRY:

            try
            {
                while (tries < Retries)
                {
                    try
                    {
                        HttpResponse response = GetRetriableResponse(requestFunction, requestPacket, sessionData);

                        FinishResponse(requestFunction, null, currentStopwatch, requestPacket, response);

                        return response;
                    }
                    catch (MutexRetryException)
                    {
                        tries++;

                        if (tries == Retries)
                        {
                            throw;
                        }

                        Thread.Sleep(_random.Next(25 * tries));
                        goto RETRY;
                    }
                }
            }
            catch(Exception e)
            {
                FinishResponse(requestFunction, e, currentStopwatch, requestPacket, null);

                throw;
            }

            return null;
        }

        /// <summary>
        /// Responds to the request packet by calling the requestFunction with the sessionData and the requested packet.
        /// The retriable part of the response delivery.
        /// </summary>
        /// <param name="requestFunction">the function to call</param>
        /// <param name="requestPacket">the http-request</param>
        /// <param name="sessionData">the current sessionData</param>
        /// <returns>the http response-packet</returns>
        public abstract HttpResponse GetRetriableResponse(T requestFunction, HttpRequest requestPacket, HttpSessionData sessionData);

        /// <summary>
        /// Provides information about the last response and is called whenever a response finished.
        /// </summary>
        /// <param name="requestFunction">The called request function.</param>
        /// <param name="exception">The thrown exception.</param>
        /// <param name="stopwatch">The current stopwatch.</param>
        /// <param name="requestPacket">The original Request Packet.</param>
        /// <param name="httpResponse">The response Packet.</param>
        public virtual void FinishResponse(T requestFunction, Exception exception, Stopwatch stopwatch, HttpRequest requestPacket, HttpResponse httpResponse)
        {
            // default behaviour: do nothing.
        }

        /// <summary>
        /// Gets the response function which can be called multiple times in GetRetriableResponse.
        /// </summary>
        /// <param name="requestPacket">the http-request</param>
        /// <returns>the method to call.</returns>
        public abstract T GetResponseFunction(HttpRequest requestPacket);
        
        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            return true;
        }
    }

    /// <summary>
    /// A response handler for PageResponses
    /// </summary>
    public class PageResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetContents>, IDebugRespondable
    {
        /// <summary>
        /// Shall this RequestHandler store DebugView information?
        /// </summary>
        public static bool StoreDebugInformation = true;

        /// <summary>
        /// The DebugResponseNode for this PageResponseRequestHandler.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected UsableWriteLock ReaderWriterLock = new UsableWriteLock();

        /// <summary>
        /// The currently listed PageResponses.
        /// </summary>
        protected AVLHashMap<string, Master.GetContents> PageResponses = new AVLHashMap<string, Master.GetContents>(WebServer.PageResponseStorageHashMapSize);

        /// <summary>
        /// Constructs a new PageResponseRequestHandler and registers this RequestHandler as listening for new PageResponses.
        /// </summary>
        public PageResponseRequestHandler()
        {
            Master.AddPageResponseEvent += AddFunction;
            Master.RemovePageResponseEvent += RemoveFunction;
            
            DebugResponseNode = new DebugContainerResponseNode(GetType().Name, null, GetDebugResponse, RequestHandler.CurrentRequestHandler.DebugResponseNode);
        }

        private HElement GetDebugResponse(SessionData arg)
        {
            if(!StoreDebugInformation)
                return new HText($"This {GetType().Name} does not store Error-Information. If you want Error-Information to be stored enable the '{StoreDebugInformation}' Flag.") { Class = "warning" };

            using (ReaderWriterLock.LockRead())
            {
                var ret = new HTable((from e in PageResponses select new List<HElement> { new HString(e.Key), e.Value.Method.ToString(), e.Value.Target == null ? $"Declared in {e.Value.Method.DeclaringType.Name}" : $"[{e.Value.Target.GetType().Name}] {e.Value.Target.ToString()}" }))
                {
                    TableHeader = new HElement[] { "Registered URL", "Associated GetContents Function", "Instance Object or Declaring Class" }
                };

                return ret;
            }
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            return new HttpResponse(requestPacket, requestFunction.Invoke(sessionData))
            {
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetContents GetResponseFunction(HttpRequest requestPacket)
        {
            using (ReaderWriterLock.LockRead())
                return PageResponses[requestPacket.RequestUrl];
        }

        private void AddFunction(string url, Master.GetContents getc)
        {
            using (ReaderWriterLock.LockWrite())
            {
                PageResponses.Add(url, getc);

                if (getc.Target != null && getc.Target is IDebugRespondable)
                    DebugResponseNode.AddNode(((IDebugRespondable)getc.Target).GetDebugResponseNode());
            }

            ServerHandler.LogMessage("The URL '" + url + "' is now assigned to a Page. (WebserverApi)");
        }

        private void RemoveFunction(string url)
        {
            using (ReaderWriterLock.LockWrite())
            {
                Master.GetContents getc = PageResponses[url];

                if (getc != null)
                {
                    if (getc.Target is IDebugRespondable)
                        DebugResponseNode.RemoveNode(((IDebugRespondable)getc.Target).GetDebugResponseNode());

                    PageResponses.Remove(url);
                    ServerHandler.LogMessage("The URL '" + url + "' is not assigned to a Page anymore. (WebserverApi)");
                }
            }
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        /// <inheritdoc />
        public override void FinishResponse(Master.GetContents requestFunction, Exception exception, Stopwatch stopwatch, HttpRequest requestPacket, HttpResponse httpResponse)
        {
            if (StoreDebugInformation)
            {
                base.FinishResponse(requestFunction, exception, stopwatch, requestPacket, httpResponse);

                if (requestFunction.Target != null && requestFunction.Target is IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>)
                    ((IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>)requestFunction.Target).UpdateDebugResponseData(exception, stopwatch.Elapsed, requestPacket, httpResponse);
            }
        }
    }

    /// <summary>
    /// A response handler for OneTime-PageResponses
    /// </summary>
    public class OneTimePageResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetContents>
    {
        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The currently listed OneTime-PageResponses.
        /// </summary>
        protected QueuedAVLTree<string, Master.GetContents> OneTimeResponses = new QueuedAVLTree<string, Master.GetContents>(WebServer.OneTimePageResponsesStorageQueueSize);

        /// <summary>
        /// Constructs a new OneTimePageResponseRequestHandler and registers this RequestHandler as listening for new OneTime-PageResponses.
        /// </summary>
        public OneTimePageResponseRequestHandler()
        {
            Master.AddOneTimeFunctionEvent += AddOneTimeFunction;
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            return new HttpResponse(requestPacket, requestFunction.Invoke(sessionData))
            {
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetContents GetResponseFunction(HttpRequest requestPacket)
        {
            ReaderWriterLock.EnterWriteLock();
            var response = OneTimeResponses[requestPacket.RequestUrl];

            if (response != null)
                OneTimeResponses.Remove(requestPacket.RequestUrl);

            ReaderWriterLock.ExitWriteLock();

            return response;
        }

        private void AddOneTimeFunction(string url, Master.GetContents getc)
        {
            ReaderWriterLock.EnterWriteLock();
            OneTimeResponses.Add(url, getc);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + url + "' is now assigned to a Page. (WebserverApi/OneTimeFunction)");
        }
    }

    /// <summary>
    /// A response handler for WebSocketResponses
    /// </summary>
    public class WebSocketRequestHandler : IRequestHandler
    {
        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The currently listed WebSocketResponses.
        /// </summary>
        protected AVLHashMap<string, WebSocketCommunicationHandler> WebSocketResponses = new AVLHashMap<string, WebSocketCommunicationHandler>(WebServer.WebSocketResponsePageStorageHashMapSize);

        /// <summary>
        /// Constructs a new WebSocketRequestHandler and registers this RequestHandler as listening for new WebSocketResponses.
        /// </summary>
        public WebSocketRequestHandler()
        {
            Master.AddWebsocketHandlerEvent += AddWebsocketHandler;
            Master.RemoveWebsocketHandlerEvent += RemoveWebsocketHandler;
        }

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket, Stopwatch currentStopwatch)
        {
            if (!requestPacket.IsWebsocketUpgradeRequest)
                return null;

            ReaderWriterLock.EnterReadLock();

            WebSocketCommunicationHandler currentWebSocketHandler;

            if (WebSocketResponses.TryGetValue(requestPacket.RequestUrl, out currentWebSocketHandler))
            {
                ReaderWriterLock.ExitReadLock();
                var handler =
                    (Fleck.Handlers.ComposableHandler)
                    Fleck.HandlerFactory.BuildHandler(Fleck.RequestParser.Parse(Encoding.UTF8.GetBytes(requestPacket.RawRequest)), currentWebSocketHandler._OnMessage, currentWebSocketHandler._OnClose,
                        currentWebSocketHandler._OnBinary, currentWebSocketHandler._OnPing, currentWebSocketHandler._OnPong);

                byte[] msg = handler.CreateHandshake();
                requestPacket.Stream.Write(msg, 0, msg.Length);

                var proxy = new WebSocketHandlerProxy(requestPacket.Stream, currentWebSocketHandler, handler);

                throw new WebSocketManagementOvertakeFlagException(); // <- Signalize the outside world, that the websocket handler has taken over the network stream.
            }
            else
            {
                ReaderWriterLock.ExitReadLock();
            }

            return null;
        }

        private void AddWebsocketHandler(WebSocketCommunicationHandler handler)
        {
            ReaderWriterLock.EnterWriteLock();
            WebSocketResponses.Add(handler.URL, handler);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + handler.URL + "' is now assigned to a Page. (Websocket)");
        }

        private void RemoveWebsocketHandler(string URL)
        {
            ReaderWriterLock.EnterWriteLock();
            WebSocketResponses.Remove(URL);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + URL + "' is not assigned to a Page anymore. (Websocket)");
        }

        /// <inheritdoc />
        public bool Equals(IRequestHandler other)
        {
            if (other == null)
                return false;

            if (!other.GetType().Equals(GetType()))
                return false;

            return true;
        }
    }

    /// <summary>
    /// A response handler for DirectoryResponses
    /// </summary>
    public class DirectoryResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetDirectoryContents>, IDebugRespondable
    {
        /// <summary>
        /// Shall this RequestHandler store DebugView information?
        /// </summary>
        public static bool StoreDebugInformation = true;
        
        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected UsableWriteLock ReaderWriterLock = new UsableWriteLock();

        /// <summary>
        /// The currently listed DirectoryResponses.
        /// </summary>
        protected AVLHashMap<string, Master.GetDirectoryContents> DirectoryResponses = new AVLHashMap<string, Master.GetDirectoryContents>(WebServer.DirectoryResponseStorageHashMapSize);

        /// <summary>
        /// The DebugResponseNode for this PageResponseRequestHandler.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        [ThreadStatic]
        private static string _subUrl = "";

        /// <summary>
        /// Constructs a new DirectoryResponseRequestHandler and registers this RequestHandler as listening for new DirectoryResponses.
        /// </summary>
        public DirectoryResponseRequestHandler()
        {
            DebugResponseNode = new DebugContainerResponseNode(GetType().Name, null, GetDebugResponse, RequestHandler.CurrentRequestHandler.DebugResponseNode);

            Master.AddDirectoryFunctionEvent += AddDirectoryFunction;
            Master.RemoveDirectoryFunctionEvent += RemoveDirectoryFunction;
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetDirectoryContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            return new HttpResponse(requestPacket, requestFunction.Invoke(sessionData, _subUrl))
            {
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetDirectoryContents GetResponseFunction(HttpRequest requestPacket)
        {
            Master.GetDirectoryContents response = null;
            string bestUrlMatch = requestPacket.RequestUrl;

            if (bestUrlMatch.StartsWith("/"))
                bestUrlMatch = bestUrlMatch.Remove(0);

            using (ReaderWriterLock.LockRead())
                if(bestUrlMatch.Any() && bestUrlMatch.Last() == '/')
                    response = DirectoryResponses[bestUrlMatch];
                else
                    response = DirectoryResponses[bestUrlMatch + '/'];

            if (response != null || bestUrlMatch.Length == 0)
            {
                _subUrl = "";
                return response;
            }

            while (true)
            {
                for (int i = bestUrlMatch.Length - 1; i >= 0; i--)
                {
                    if (bestUrlMatch[i] == '/')
                    {
                        bestUrlMatch = bestUrlMatch.Substring(0, i + 1);
                        break;
                    }

                    if (i == 0)
                    {
                        bestUrlMatch = "";
                        break;
                    }
                }

                using (ReaderWriterLock.LockRead())
                    response = DirectoryResponses[bestUrlMatch];

                if (response != null || bestUrlMatch.Length == 0)
                {
                    break;
                }
            }

            _subUrl = requestPacket.RequestUrl.Substring(bestUrlMatch.Length).TrimStart('/');

            return response;
        }

        private void AddDirectoryFunction(string URL, Master.GetDirectoryContents function)
        {
            using (ReaderWriterLock.LockWrite())
            {
                DirectoryResponses.Add(URL, function);
                
                if (function.Target != null && function.Target is IDebugRespondable)
                    DebugResponseNode.AddNode(((IDebugRespondable)function.Target).GetDebugResponseNode());
            }

            ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is now available at the Webserver. (WebserverApi)");
        }

        private void RemoveDirectoryFunction(string URL)
        {
            using (ReaderWriterLock.LockWrite())
            {
                Master.GetDirectoryContents function = DirectoryResponses[URL];

                if (function != null)
                {
                    if (function.Target is IDebugRespondable)
                        DebugResponseNode.RemoveNode(((IDebugRespondable)function.Target).GetDebugResponseNode());

                    DirectoryResponses.Remove(URL);
                    ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is not available at the Webserver anymore. (WebserverApi)");
                }
            }
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        private HElement GetDebugResponse(SessionData arg)
        {
            if (!StoreDebugInformation)
                return new HText($"This {GetType().Name} does not store Error-Information. If you want Error-Information to be stored enable the '{StoreDebugInformation}' Flag.") { Class = "warning" };

            using (ReaderWriterLock.LockRead())
            {
                var ret = new HTable((from e in DirectoryResponses select new List<HElement> { new HString(e.Key), e.Value.Method.ToString(), e.Value.Target == null ? $"Declared in {e.Value.Method.DeclaringType.Name}" : $"[{e.Value.Target.GetType().Name}] {e.Value.Target.ToString()}" }))
                {
                    TableHeader = new HElement[] { "Registered URL", "Associated GetContents Function", "Instance Object or Declaring Class" }
                };

                return ret;
            }
        }

        /// <inheritdoc />
        public override void FinishResponse(Master.GetDirectoryContents requestFunction, Exception exception, Stopwatch stopwatch, HttpRequest requestPacket, HttpResponse httpResponse)
        {
            if (StoreDebugInformation)
            {
                base.FinishResponse(requestFunction, exception, stopwatch, requestPacket, httpResponse);

                if (requestFunction.Target != null && requestFunction.Target is IDebugUpdateableResponse<Exception, TimeSpan, string, HttpRequest, HttpResponse>)
                    ((IDebugUpdateableResponse<Exception, TimeSpan, string, HttpRequest, HttpResponse>)requestFunction.Target).UpdateDebugResponseData(exception, stopwatch.Elapsed, _subUrl, requestPacket, httpResponse);
            }
        }
    }


    /// <summary>
    /// A response handler for DataResponses
    /// </summary>
    public class DataResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetDataContents>, IDebugRespondable
    {
        /// <summary>
        /// Shall this RequestHandler store DebugView information?
        /// </summary>
        public static bool StoreDebugInformation = true;

        /// <summary>
        /// The DebugResponseNode for this DataResponseRequestHandler.
        /// </summary>
        public readonly DebugContainerResponseNode DebugResponseNode;

        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected UsableWriteLock ReaderWriterLock = new UsableWriteLock();

        /// <summary>
        /// The currently listed DataResponses.
        /// </summary>
        protected AVLHashMap<string, Master.GetDataContents> DataResponses = new AVLHashMap<string, Master.GetDataContents>(WebServer.DataResponseStorageHashMapSize);

        /// <summary>
        /// Constructs a new DataResponseRequestHandler and registers this RequestHandler as listening for new DataResponses.
        /// </summary>
        public DataResponseRequestHandler()
        {
            Master.AddDataResponseEvent += AddFunction;
            Master.RemoveDataResponseEvent += RemoveFunction;

            DebugResponseNode = new DebugContainerResponseNode(GetType().Name, null, GetDebugResponse, RequestHandler.CurrentRequestHandler.DebugResponseNode);
        }

        private HElement GetDebugResponse(SessionData arg)
        {
            if (!StoreDebugInformation)
                return new HText($"This {GetType().Name} does not store Error-Information. If you want Error-Information to be stored enable the '{StoreDebugInformation}' Flag.") { Class = "warning" };

            using (ReaderWriterLock.LockRead())
            {
                var ret = new HTable((from e in DataResponses select new List<HElement> { new HString(e.Key), e.Value.Method.ToString(), e.Value.Target == null ? $"Declared in {e.Value.Method.DeclaringType.Name}" : $"[{e.Value.Target.GetType().Name}] {e.Value.Target.ToString()}" }))
                {
                    TableHeader = new HElement[] { "Registered URL", "Associated GetDataContents Function", "Instance Object or Declaring Class" }
                };

                return ret;
            }
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetDataContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            string contentType;
            Encoding encoding = Encoding.Unicode;

            return new HttpResponse(requestPacket, requestFunction.Invoke(sessionData, out contentType, ref encoding))
            {
                Cookies = sessionData.SetCookies,
                ContentType = contentType,
                CharSet = encoding is UnicodeEncoding ? null : encoding.EncodingName
            };
        }

        /// <inheritdoc />
        public override Master.GetDataContents GetResponseFunction(HttpRequest requestPacket)
        {
            using (ReaderWriterLock.LockRead())
                return DataResponses[requestPacket.RequestUrl];
        }

        private void AddFunction(string url, Master.GetDataContents getDataFunction)
        {
            using (ReaderWriterLock.LockWrite())
            {
                DataResponses.Add(url, getDataFunction);

                if (getDataFunction.Target != null && getDataFunction.Target is IDebugRespondable)
                    DebugResponseNode.AddNode(((IDebugRespondable)getDataFunction.Target).GetDebugResponseNode());
            }

            ServerHandler.LogMessage("The URL '" + url + "' is now assigned to a DataResponse. (WebserverApi)");
        }

        private void RemoveFunction(string url)
        {
            using (ReaderWriterLock.LockWrite())
            {
                Master.GetDataContents getc = DataResponses[url];

                if (getc != null)
                {
                    if (getc.Target is IDebugRespondable)
                        DebugResponseNode.RemoveNode(((IDebugRespondable)getc.Target).GetDebugResponseNode());

                    DataResponses.Remove(url);
                    ServerHandler.LogMessage("The URL '" + url + "' is not assigned to a DataResponse anymore. (WebserverApi)");
                }
            }
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => DebugResponseNode;

        /// <inheritdoc />
        public override void FinishResponse(Master.GetDataContents requestFunction, Exception exception, Stopwatch stopwatch, HttpRequest requestPacket, HttpResponse httpResponse)
        {
            if (StoreDebugInformation)
            {
                base.FinishResponse(requestFunction, exception, stopwatch, requestPacket, httpResponse);

                if (requestFunction.Target != null && requestFunction.Target is IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>)
                    ((IDebugUpdateableResponse<Exception, TimeSpan, HttpRequest, HttpResponse>)requestFunction.Target).UpdateDebugResponseData(exception, stopwatch.Elapsed, requestPacket, httpResponse);
            }
        }
    }
}
