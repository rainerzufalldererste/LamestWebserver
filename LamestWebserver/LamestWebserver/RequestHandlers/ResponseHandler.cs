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

namespace LamestWebserver.RequestHandlers
{
    /// <summary>
    /// A ResponseHandler contains tools to resolve HTTP-Requests to responses.
    /// </summary>
    public class ResponseHandler
    {
        /// <summary>
        /// The ResponseHandler used in the Webservers.
        /// </summary>
        public static ResponseHandler CurrentResponseHandler { get; } = new ResponseHandler();

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
                    var response = requestHandler.GetResponse(requestPacket);

                    if (response != null)
                    {
                        ServerHandler.LogMessage($"Completed Request on '{requestPacket.RequestUrl}' using '{requestHandler}'.", stopwatch);

                        return response;
                    }
                }

                foreach (IRequestHandler requestHandler in SecondaryRequestHandlers)
                {
                    var response = requestHandler.GetResponse(requestPacket);

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
    public interface IRequestHandler
    {
        /// <summary>
        /// Retrieves a response from a http-request.
        /// </summary>
        /// <param name="requestPacket">the request packet</param>
        /// <returns>the response packet</returns>
        HttpResponse GetResponse(HttpRequest requestPacket);
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
        public virtual HttpResponse GetResponse(HttpRequest requestPacket)
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

            return new HttpResponse() {ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified};
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
    }

    /// <summary>
    /// A RequestHanlder that delivers Files from local storage - which will be cached on use.
    /// </summary>
    public class CachedFileRequestHandler : FileRequestHandler
    {
        /// <summary>
        /// The size of the cache hash map. this does not limit the amount of cached items - it's just there to preference size or performance.
        /// </summary>
        public static int CacheHashMapSize = 1024;

        internal AVLHashMap<string, PreloadedFile> Cache = new AVLHashMap<string, PreloadedFile>(CacheHashMapSize);
        internal FileSystemWatcher FileSystemWatcher = null;
        internal UsableMutex CacheMutex = new UsableMutex();
        internal static readonly byte[] CrLf = Encoding.UTF8.GetBytes("\r\n");

        /// <inheritdoc />
        public CachedFileRequestHandler(string folder) : base(folder)
        {
            SetupFileSystemWatcher();
        }

        /// <inheritdoc />
        public override HttpResponse GetResponse(HttpRequest requestPacket)
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
                    return new HttpResponse() {Status = "304 Not Modified", ContentType = null, ModifiedDate = lastModified, BinaryData = CrLf};
                }
                else
                {
                    return new HttpResponse() {ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified};
                }
            }
            catch (Exception e)
            {
                return new HttpResponse()
                {
                    Status = "500 Internal Server Error",
                    BinaryData = Encoding.UTF8.GetBytes(Master.GetErrorMsg(
                        "Error 500: Internal Server Error",
                        "<p>An Exception occurred while sending the response:<br><br></p><div style='font-family:\"Consolas\",monospace;font-size: 13;color:#4C4C4C;'>"
                        + WebServer.GetErrorMsg(e, null, requestPacket.RawRequest).Replace("\r\n", "<br>").Replace(" ", "&nbsp;") + "</div><br>"
                        + "</div>"))
                };
            }
        }

        internal void SetupFileSystemWatcher()
        {
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
        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            var file = _cache[requestPacket.RequestUrl];

            if (file == null)
                return null;

            if (requestPacket.ModifiedDate != null && requestPacket.ModifiedDate.Value <= file.LastModified)
                return new HttpResponse() {Status = "304 Not Modified", ContentType = null, BinaryData = CachedFileRequestHandler.CrLf, ModifiedDate = file.LastModified};

            return new HttpResponse() {BinaryData = file.Contents, ContentType = FileRequestHandler.GetMimeType(FileRequestHandler.GetExtention(requestPacket.RequestUrl)), ModifiedDate = file.LastModified };
        }
    }

    /// <summary>
    /// A Cacheable preloaded file.
    /// </summary>
    [Serializable]
    public class PreloadedFile : ICloneable
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
    }

    /// <summary>
    /// Displays error messages for every request that passed through.
    /// </summary>
    public class ErrorRequestHandler : IRequestHandler
    {
        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            if (requestPacket.RequestUrl.EndsWith("/"))
            {
                return new HttpResponse()
                {
                    Status = "403 Forbidden",
                    BinaryData = Encoding.UTF8.GetBytes(Master.GetErrorMsg(
                        "Error 403: Forbidden",
                        "<p>The Requested URL cannot be delivered due to insufficient priveleges.</p>" +
                        "</div></p>"))
                };
            }
            else
            {
                return new HttpResponse()
                {
                    Status = "404 File Not Found",
                    BinaryData = Encoding.UTF8.GetBytes(Master.GetErrorMsg(
                        "Error 404: Page Not Found",
                        "<p>The URL you requested did not match any page or file on the server.</p>" +

                        "</div></p>"))
                };
            }
        }
    }

    /// <summary>
    /// Provides functionality for Retriable Responses (MutexRetryException triggered retrying)
    /// </summary>
    /// <typeparam name="T">the type of method to call</typeparam>
    public abstract class AbstractMutexRetriableResponse<T> : IRequestHandler
    {
        private readonly Random _random = new Random();

        /// <inheritdoc />
        public HttpResponse GetResponse(HttpRequest requestPacket)
        {
            var requestFunction = GetResponseFunction(requestPacket);

            if (requestFunction == null)
                return null;

            int tries = 0;
            var sessionData = new HttpSessionData(requestPacket);

            RETRY:

            while (tries < 10)
            {
                try
                {
                    return GetRetriableResponse(requestFunction, requestPacket, sessionData);
                }
                catch (MutexRetryException)
                {
                    tries++;

                    if (tries == 10)
                    {
                        throw;
                    }

                    Thread.Sleep(_random.Next(25 * tries));
                    goto RETRY;
                }
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
        /// Gets the response function which can be called multiple times in GetRetriableResponse.
        /// </summary>
        /// <param name="requestPacket">the http-request</param>
        /// <returns>the method to call.</returns>
        public abstract T GetResponseFunction(HttpRequest requestPacket);
    }

    /// <summary>
    /// A response handler for PageResponses
    /// </summary>
    public class PageResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetContents>
    {
        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The currently listed PageResponses.
        /// </summary>
        protected AVLHashMap<string, Master.GetContents> PageResponses = new AVLHashMap<string, Master.GetContents>(WebServer.PageResponseStorageHashMapSize);

        /// <summary>
        /// Constructs a new PageResponseRequestHandler and registers this RequestHandler as listening for new PageResponses.
        /// </summary>
        public PageResponseRequestHandler()
        {
            Master.AddFunctionEvent += AddFunction;
            Master.RemoveFunctionEvent += RemoveFunction;
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            return new HttpResponse()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData)),
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetContents GetResponseFunction(HttpRequest requestPacket)
        {
            ReaderWriterLock.EnterReadLock();
            var response = PageResponses[requestPacket.RequestUrl];
            ReaderWriterLock.ExitReadLock();

            return response;
        }

        private void AddFunction(string url, Master.GetContents getc)
        {
            ReaderWriterLock.EnterWriteLock();
            PageResponses.Add(url, getc);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + url + "' is now assigned to a Page. (WebserverApi)");
        }

        private void RemoveFunction(string url)
        {
            ReaderWriterLock.EnterWriteLock();
            PageResponses.Remove(url);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The URL '" + url + "' is not assigned to a Page anymore. (WebserverApi)");
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
            return new HttpResponse()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData)),
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
        public HttpResponse GetResponse(HttpRequest requestPacket)
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
    }

    /// <summary>
    /// A response handler for DirectoryResponses
    /// </summary>
    public class DirectoryResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetDirectoryContents>
    {
        /// <summary>
        /// A ReaderWriterLock for accessing pages synchronously.
        /// </summary>
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        /// <summary>
        /// The currently listed DirectoryResponses.
        /// </summary>
        protected AVLHashMap<string, Master.GetDirectoryContents> DirectoryResponses = new AVLHashMap<string, Master.GetDirectoryContents>(WebServer.DirectoryResponseStorageHashMapSize);

        [ThreadStatic]
        private static string _subUrl = "";

        /// <summary>
        /// Constructs a new DirectoryResponseRequestHandler and registers this RequestHandler as listening for new DirectoryResponses.
        /// </summary>
        public DirectoryResponseRequestHandler()
        {
            Master.AddDirectoryFunctionEvent += AddDirectoryFunction;
            Master.RemoveDirectoryFunctionEvent += RemoveDirectoryFunction;
        }

        /// <inheritdoc />
        public override HttpResponse GetRetriableResponse(Master.GetDirectoryContents requestFunction, HttpRequest requestPacket, HttpSessionData sessionData)
        {
            return new HttpResponse()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData, _subUrl)),
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

            while (true)
            {
                for (int i = bestUrlMatch.Length - 2; i >= 0; i--)
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

                ReaderWriterLock.EnterReadLock();
                response = DirectoryResponses[bestUrlMatch];
                ReaderWriterLock.ExitReadLock();

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
            ReaderWriterLock.EnterWriteLock();
            DirectoryResponses.Add(URL, function);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is now available at the Webserver. (WebserverApi)");
        }

        private void RemoveDirectoryFunction(string URL)
        {
            ReaderWriterLock.EnterWriteLock();
            DirectoryResponses.Remove(URL);
            ReaderWriterLock.ExitWriteLock();

            ServerHandler.LogMessage("The Directory with the URL '" + URL + "' is not available at the Webserver anymore. (WebserverApi)");
        }
    }
}
