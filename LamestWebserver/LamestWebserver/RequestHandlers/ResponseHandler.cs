using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;

namespace LamestWebserver.RequestHandlers
{
    public class ResponseHandler
    {
        public static ResponseHandler CurrentResponseHandler => _responseHandler;
        private static ResponseHandler _responseHandler = new ResponseHandler();

        protected List<IRequestHandler> RequestHandlers = new List<IRequestHandler>();
        protected List<IRequestHandler> SecondaryRequestHandlers = new List<IRequestHandler>();

        protected UsableWriteLock RequestWriteLock = new UsableWriteLock();

        public HttpPacket GetResponse(HttpPacket requestPacket)
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

        public void AddRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                if (!RequestHandlers.Contains(handler))
                    RequestHandlers.Add(handler);
        }

        public void RemoveRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                RequestHandlers.Remove(handler);
        }

        public void AddSecondaryRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
            {
                if (SecondaryRequestHandlers.Contains(handler))
                    SecondaryRequestHandlers.Remove(handler);

                SecondaryRequestHandlers.Insert(0, handler);
            }
        }

        public void RemoveSecondaryRequestHandler(IRequestHandler handler)
        {
            using (RequestWriteLock.LockWrite())
                SecondaryRequestHandlers.Remove(handler);
        }
    }

    public interface IRequestHandler
    {
        HttpPacket GetResponse(HttpPacket requestPacket);
    }

    public class FileRequestHandler : IRequestHandler
    {
        public readonly string Folder;

        public FileRequestHandler(string folder)
        {
            if (folder.EndsWith("\\"))
                folder = folder.Substring(0, folder.Length - 1);

            Folder = folder;
        }

        /// <inheritdoc />
        public virtual HttpPacket GetResponse(HttpPacket requestPacket)
        {
            string fileName = requestPacket.RequestUrl;
            byte[] contents = null;
            DateTime? lastModified = null;
            bool notModified = false;
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

            return new HttpPacket() {ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified};
        }

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

        protected bool FileIsBinary(string fileName, string extention)
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

    public class CachedFileRequestHandler : FileRequestHandler
    {
        internal AVLTree<string, PreloadedFile> Cache = new AVLTree<string, PreloadedFile>();
        internal FileSystemWatcher FileSystemWatcher = null;
        internal UsableMutex CacheMutex = new UsableMutex();
        private static readonly byte[] CrLf = Encoding.UTF8.GetBytes("\r\n");

        /// <inheritdoc />
        public CachedFileRequestHandler(string folder) : base(folder)
        {
            SetupFileSystemWatcher();
        }

        /// <inheritdoc />
        public override HttpPacket GetResponse(HttpPacket requestPacket)
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
                            Cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, false));
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
                        Cache.Add(fileName, new PreloadedFile(fileName, contents, contents.Length, lastModified.Value, isBinary));
                    }

                    ServerHandler.LogMessage("The URL '" + requestPacket.RequestUrl + "' is now available through the cache.");
                }
                else
                {
                    return null;
                }

                if (notModified)
                {
                    return new HttpPacket() {Status = "304 Not Modified", ContentType = null, ModifiedDate = lastModified, BinaryData = CrLf};
                }
                else
                {
                    return new HttpPacket() {ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified};
                }
            }
            catch (Exception e)
            {
                return new HttpPacket()
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

        protected bool GetFromCache(string name, out PreloadedFile file)
        {
            using (CacheMutex.Lock())
            {
                if (Cache.TryGetValue(name, out file))
                {
                    file.LoadCount++;
                    file = file.Clone();
                }
                else
                    return false;
            }

            return true;
        }
    }

    public class PreloadedFile
    {
        public string Filename;
        public byte[] Contents;
        public int Size;
        public DateTime LastModified;
        public bool IsBinary;
        public int LoadCount;

        public PreloadedFile(string filename, byte[] contents, int size, DateTime lastModified, bool isBinary)
        {
            Filename = filename;
            Contents = contents;
            Size = size;
            LastModified = lastModified;
            IsBinary = isBinary;
            LoadCount = 1;
        }

        internal PreloadedFile Clone()
        {
            return new PreloadedFile((string) Filename.Clone(), Contents.ToArray(), Size, LastModified, IsBinary);
        }
    }

    public class ErrorRequestHandler : IRequestHandler
    {
        /// <inheritdoc />
        public HttpPacket GetResponse(HttpPacket requestPacket)
        {
            if (requestPacket.RequestUrl.EndsWith("/"))
            {
                return new HttpPacket()
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
                return new HttpPacket()
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

    public abstract class AbstractMutexRetriableResponse<T> : IRequestHandler
    {
        private Random random = new Random();

        /// <inheritdoc />
        public HttpPacket GetResponse(HttpPacket requestPacket)
        {
            var requestFunction = GetResponseFunction(requestPacket);

            if (requestFunction == null)
                return null;

            int tries = 0;
            var sessionData = new SessionData(requestPacket);

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

                    Thread.Sleep(random.Next(25 * tries));
                    goto RETRY;
                }
            }

            return null;
        }

        public abstract HttpPacket GetRetriableResponse(T requestFunction, HttpPacket requestPacket, SessionData sessionData);

        public abstract T GetResponseFunction(HttpPacket requestPacket);
    }

    public class PageResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetContents>
    {
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        protected AVLHashMap<string, Master.GetContents> PageResponses = new AVLHashMap<string, Master.GetContents>(WebServer.PageResponseStorageHashMapSize);

        public PageResponseRequestHandler()
        {
            Master.AddFunctionEvent += AddFunction;
            Master.RemoveFunctionEvent += RemoveFunction;
        }

        /// <inheritdoc />
        public override HttpPacket GetRetriableResponse(Master.GetContents requestFunction, HttpPacket requestPacket, SessionData sessionData)
        {
            return new HttpPacket()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData)),
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetContents GetResponseFunction(HttpPacket requestPacket)
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

    public class OneTimePageResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetContents>
    {
        protected QueuedAVLTree<string, Master.GetContents> OneTimeResponses = new QueuedAVLTree<string, Master.GetContents>(WebServer.OneTimePageResponsesStorageQueueSize);
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public OneTimePageResponseRequestHandler()
        {
            Master.AddOneTimeFunctionEvent += AddOneTimeFunction;
        }

        /// <inheritdoc />
        public override HttpPacket GetRetriableResponse(Master.GetContents requestFunction, HttpPacket requestPacket, SessionData sessionData)
        {
            return new HttpPacket()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData)),
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetContents GetResponseFunction(HttpPacket requestPacket)
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

    public class WebSocketRequestHandler : IRequestHandler
    {
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        protected AVLHashMap<string, WebSocketCommunicationHandler> WebSocketResponses = new AVLHashMap<string, WebSocketCommunicationHandler>(WebServer.WebSocketResponsePageStorageHashMapSize);

        public WebSocketRequestHandler()
        {
            Master.AddWebsocketHandlerEvent += AddWebsocketHandler;
            Master.RemoveWebsocketHandlerEvent += RemoveWebsocketHandler;
        }

        /// <inheritdoc />
        public HttpPacket GetResponse(HttpPacket requestPacket)
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

    public class DirectoryResponseRequestHandler : AbstractMutexRetriableResponse<Master.GetDirectoryContents>
    {
        protected ReaderWriterLockSlim ReaderWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        protected AVLHashMap<string, Master.GetDirectoryContents> DirectoryResponses = new AVLHashMap<string, Master.GetDirectoryContents>(WebServer.DirectoryResponseStorageHashMapSize);

        [ThreadStatic]
        private static string _subUrl = "";

        public DirectoryResponseRequestHandler()
        {
            Master.AddDirectoryFunctionEvent += AddDirectoryFunction;
            Master.RemoveDirectoryFunctionEvent += RemoveDirectoryFunction;
        }

        /// <inheritdoc />
        public override HttpPacket GetRetriableResponse(Master.GetDirectoryContents requestFunction, HttpPacket requestPacket, SessionData sessionData)
        {
            return new HttpPacket()
            {
                BinaryData = Encoding.UTF8.GetBytes(requestFunction.Invoke(sessionData, _subUrl)),
                Cookies = sessionData.SetCookies
            };
        }

        /// <inheritdoc />
        public override Master.GetDirectoryContents GetResponseFunction(HttpPacket requestPacket)
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
