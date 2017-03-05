using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamestWebserver.Collections;
using LamestWebserver.Synchronization;

namespace LamestWebserver
{
    public class ResponseHandler
    {
        protected List<IRequestHandler> RequestHandlers = new List<IRequestHandler>();

        public HttpPacket GetResponse(HttpPacket requestPacket)
        {
            foreach (IRequestHandler requestHandler in RequestHandlers)
            {
                var response = requestHandler.GetResponse(requestPacket);

                if (response != null)
                    return response;
            }

            return null;
        }

        public void AddRequestHandler(IRequestHandler handler)
        {
            if (!RequestHandlers.Contains(handler))
                RequestHandlers.Add(handler);
        }

        public void RemoveRequestHandler(IRequestHandler handler)
        {
            RequestHandlers.Remove(handler);
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
            Folder = folder;
        }

        /// <inheritdoc />
        public virtual HttpPacket GetResponse(HttpPacket requestPacket)
        {
            string fileName = Folder + requestPacket.RequestUrl;

            if (File.Exists(fileName))
            {
                var extention = GetExtention(fileName);
                bool isBinary = FileIsBinary(fileName, extention);

                return new HttpPacket()
                {
                    BinaryData = ReadFile(fileName, isBinary),
                    ContentType = GetMimeType(extention)
                };
            }

            return null;
        }

        protected byte[] ReadFile(string filename, bool isBinary = false)
        {
            int i = 10;

            while (i-- > 0) // Chris: if the file has currently been changed you probably have to wait until the writing process has finished
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
                    Thread.Sleep(2); // Chris: if the file has currently been changed you probably have to wait until the writing process has finished
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
                    return new HttpPacket() { Status = "304 Not Modified", ContentType = null, ModifiedDate = lastModified, BinaryData = CrLf };
                }
                else
                {
                    return new HttpPacket() { ContentType = GetMimeType(extention), BinaryData = contents, ModifiedDate = lastModified };
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
                    ;
                }

                ServerHandler.LogMessage("The cache of the URL '" + e.Name + "' has been updated.");
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
}
