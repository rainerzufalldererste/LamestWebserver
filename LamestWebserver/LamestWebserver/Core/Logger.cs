using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using LamestWebserver.Synchronization;
using LamestWebserver.Collections;
using LamestWebserver.RequestHandlers.DebugView;
using LamestWebserver.UI;
using System.Linq;
using System.Diagnostics;

namespace LamestWebserver.Core
{
    /// <summary>
    /// A simple Logger for LamestWebserver.
    /// </summary>
    public class Logger : IDebugRespondable
    {
        /// <summary>
        /// The Main Logger for LamestWebserver.
        /// </summary>
        public readonly static Singleton<Logger> CurrentLogger = new Singleton<Logger>();

        /// <summary>
        /// Is LamestWebserverRunning in Debug Mode.
        /// </summary>
        public static bool LoggerDebugMode = false;

        /// <summary>
        /// The default minimum logging level. (should probably be ELoggingLevel.Warning by default)
        /// </summary>
        public static ELoggingLevel DefaultMinimumLoggingLevel =
#if DEBUG
            ELoggingLevel.Trace;
#else
            ELoggingLevel.Information;
#endif

        /// <summary>
        /// The minimum logging level for this logger.
        /// </summary>
        public ELoggingLevel MinimumLoggingLevel = DefaultMinimumLoggingLevel;

        /// <summary>
        /// Currently used output source
        /// </summary>
        private EOutputSource _currentOutputSource = EOutputSource.Console;

        /// <summary>
        /// The Flags representing the Output Source(s) to write to.
        /// </summary>
        public EOutputSource OutputSourceFlags
        {
            get
            {
                return _currentOutputSource;
            }

            set
            {
                if (_currentOutputSource == value)
                    return;
                
                _currentOutputSource = value;
                RestartStream();
            }
        }

        /// <summary>
        /// The Output Source(s) the main Logger instance writes to.
        /// </summary>
        public static EOutputSource OutputSource
        {
            get { return CurrentLogger.Instance.OutputSourceFlags; }
            set { CurrentLogger.Instance.OutputSourceFlags = value; }
        }

        /// <summary>
        /// Stream Writer to handle the writing from multiple streams
        /// </summary>
        private MultiStreamWriter _multiStreamWriter;

        private UsableWriteLock _loggerWriteLock = new UsableWriteLock();
        private List<Stream> _streams = new List<Stream>();
        private FixedSizeQueue<Tuple<DateTime, ELoggingLevel, string, string>> _lastMessages = new FixedSizeQueue<Tuple<DateTime, ELoggingLevel, string, string>>(100);
        private DebugContainerResponseNode _debugResponseNode;

        /// <summary>
        /// The currently used File Path that the logger writes to.
        /// </summary>
        private string _currentFilePath = "lws.log";

        /// <summary>
        /// Path for the File the logger writes to (if OutputSourceFlags contains EOutputSource.File)
        /// </summary>
        public string FilePath
        {
            get
            {
                return _currentFilePath;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (_currentFilePath == value)
                    return;

                _currentFilePath = value;
                RestartStream();
            }
        }

        /// <summary>
        /// If manually closed or opened check this value
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Logging a message on logging level 'Trace'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogTrace(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.Trace(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Trace'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Trace(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Trace)
                return;

            Log(ELoggingLevel.Trace, msg, stopwatch);
        }

        /// <summary>
        /// Logging a message on logging level 'Information'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogInformation(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.Information(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Information'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Information(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Information)
                return;

            Log(ELoggingLevel.Information, msg, stopwatch);
        }

        /// <summary>
        /// Logging a message on logging level 'Warning'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogWarning(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.Warning(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Warning'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Warning(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Warning)
                return;

            Log(ELoggingLevel.Warning, msg, stopwatch);
        }

        /// <summary>
        /// Logging a message on logging level 'Error'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogError(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.Error(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Error'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Error(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Error)
                return;

            Log(ELoggingLevel.Error, msg, stopwatch);
        }

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogDebugExcept(Exception exception, Stopwatch stopwatch = null) => CurrentLogger.Instance.DebugExcept(exception, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void DebugExcept(Exception exception, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.DebugExcept)
                return;

            Log(ELoggingLevel.DebugExcept, "{" + exception.GetType() + "} " + exception.SafeMessage(), stopwatch);

            if (LoggerDebugMode)
                throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogDebugExcept(string message, Exception exception, Stopwatch stopwatch = null) => CurrentLogger.Instance.DebugExcept(message, exception, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void DebugExcept(string message, Exception exception, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.DebugExcept)
                return;

            Log(ELoggingLevel.DebugExcept, "{" + exception.GetType() + "} " + message, stopwatch);

            if (LoggerDebugMode)
                throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. An Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogDebugExcept(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.DebugExcept(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void DebugExcept(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.DebugExcept)
                return;

            Log(ELoggingLevel.DebugExcept, msg, stopwatch);

            if (LoggerDebugMode)
                throw new Exception(msg);
        }

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogExcept(Exception exception, Stopwatch stopwatch = null) => CurrentLogger.Instance.Except(exception, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Except(Exception exception, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Except)
                return;

            Log(ELoggingLevel.Except, "{" + exception.GetType() + "} " + exception.SafeMessage(), stopwatch);

            throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogExcept(string message, Exception exception, Stopwatch stopwatch = null) => CurrentLogger.Instance.Except(message, exception, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to throw.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Except(string message, Exception exception, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Except)
                return;

            Log(ELoggingLevel.Except, "{" + exception.GetType() + "} " + message, stopwatch);

            throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'Except'.  An exception will be thrown.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogExcept(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.Except(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void Except(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.Except)
                return;

            Log(ELoggingLevel.Except, msg, stopwatch);

            throw new Exception(msg);
        }

        /// <summary>
        /// Logging a message on logging level 'CrashAndBurn'. The logger will be closed, a dump file will be written and the application will be exited with error code -1.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public static void LogCrashAndBurn(string msg, Stopwatch stopwatch = null) => CurrentLogger.Instance.CrashAndBurn(msg, stopwatch);

        /// <summary>
        /// Logging a message on logging level 'CrashAndBurn'. The logger will be closed, a dump file will be written and the application will be exited with error code -1.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="stopwatch">A stopwatch carrying the elapsed time.</param>
        public void CrashAndBurn(string msg, Stopwatch stopwatch = null)
        {
            if (MinimumLoggingLevel > ELoggingLevel.CrashAndBurn)
                return;

            Log(ELoggingLevel.CrashAndBurn, msg, stopwatch);

            try
            {
                Close();
            }
            catch { }

            try
            {
                MiniDump.Write();
            }
            catch { }

            Environment.Exit(-1);
        }

        private void Log(ELoggingLevel loggingLevel, string msg, Stopwatch stopwatch = null)
        {
            if (_currentOutputSource != EOutputSource.None && _multiStreamWriter != null && !_multiStreamWriter.IsDisposed)
            {
                using (_loggerWriteLock.LockWrite())
                {
                    string stopwatchString = stopwatch != null ? $"{(stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond):0.000} ms" : "";

                    _multiStreamWriter.WriteLine($"{DateTime.Now} [{loggingLevel.ToString()}] {(stopwatch != null ? "(" + stopwatchString + ") " : "")}\\\\\\\\ {msg}");
                    _lastMessages.Push(new Tuple<DateTime, ELoggingLevel, string, string>(DateTime.Now, loggingLevel, msg, stopwatchString));
                }
            }
        }

        /// <summary>
        /// Add some Custom Streams.
        /// Call RestartStream to let the changes take action.
        /// </summary>
        /// <param name="stream"></param>
        public void AddCustomStream(Stream stream) => _streams.Add(stream);

        /// <summary>
        /// Remove some Custom Streams.
        /// Call RestartStream to let the changes take action.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public bool RemoveCustomStream(Stream stream) => _streams.Remove(stream);

        /// <summary>
        /// Clear all Custom Streams.
        /// Call RestartStream to let the changes take action.
        /// </summary>
        public void ClearCustomStreams() => _streams.Clear();

        /// <summary>
        /// Closes and Flushes the Logger stream.
        /// If you want to restart the stream manually use 'Restart'.
        /// </summary>
        public void Close()
        {
            using (_loggerWriteLock.LockWrite())
            {
                _multiStreamWriter.Dispose();
            }
        }

        /// <summary>
        /// Opens and initializes the steam to write to.
        /// If you want to restart the stream manually use 'Restart'.
        /// </summary>
        protected void Open()
        {
            using (_loggerWriteLock.LockWrite())
            {
                if (_currentOutputSource != EOutputSource.None)
                {
                    CreateMultiStreamWriter();
                }
            }
        }


        /// <summary>
        /// Flushes all available Streams.
        /// </summary>
        public void Flush()
        {
            _multiStreamWriter.Flush();
        }

        /// <summary>
        /// Atomic operation to close and open the Logger save by a mutex.
        /// </summary>
        public void RestartStream()
        {
            using (_loggerWriteLock.LockWrite())
            {
                _multiStreamWriter.DisposeExcept(_streams);
                CreateMultiStreamWriter();
            }
        }

        private void CreateMultiStreamWriter()
        {
            List<Stream> streamsToApply = new List<Stream>();
            
            if ((_currentOutputSource & EOutputSource.Console) == EOutputSource.Console)
                streamsToApply.Add(Console.OpenStandardOutput());

            if ((_currentOutputSource & EOutputSource.File) == EOutputSource.File)
                streamsToApply.Add(File.Open(_currentFilePath, FileMode.Append, FileAccess.Write));

            streamsToApply.AddRange(_streams);
            _multiStreamWriter = new MultiStreamWriter(streamsToApply);
        }

        /// <inheritdoc />
        public DebugResponseNode GetDebugResponseNode() => _debugResponseNode;

        private HElement GetDebugResponse(SessionData sessionData)
        {
            if (_lastMessages.Count == 0)
                return new HItalic("There have not been any logged messages yet.");
            else
                return new HTable((from e in _lastMessages select e.ToEnumerable())) { TableHeader = new string[] { "Timestamp", "Logging Level", "Message", "Elapsed Time" } };
        }

        /// <summary>
        /// Creates a new Logger instance.
        /// </summary>
        public Logger()
        {
            Open();
            _debugResponseNode = new DebugContainerResponseNode(nameof(Logger), null, GetDebugResponse);
        }

        /// <summary>
        /// Destructor for the logger closes the stream.
        /// </summary>
        ~Logger()
        {
            Close();
        }

        /// <summary>
        /// Represents the different LoggingLevels.
        /// </summary>
        public enum ELoggingLevel : byte
        {
            /// <summary>
            /// Quits the Application writing a CrashDump.
            /// </summary>
            CrashAndBurn = 6,

            /// <summary>
            /// Throws an Exception.
            /// </summary>
            Except = 5,

            /// <summary>
            /// Throws an Exception if Logger.LoggerDebugMode is true.
            /// </summary>
            DebugExcept = 4,

            /// <summary>
            /// A major Error.
            /// </summary>
            Error = 3,

            /// <summary>
            /// A warning message.
            /// </summary>
            Warning = 2,

            /// <summary>
            /// General Information.
            /// </summary>
            Information = 1,

            /// <summary>
            /// Debugging Information.
            /// </summary>
            Trace = 0
        }

        /// <summary>
        /// Flags for output Sources
        /// </summary>
        [Flags]
        public enum EOutputSource : byte
        {
            /// <summary>
            /// No Logging output at all.
            /// </summary>
            None = 0,

            /// <summary>
            /// Write To Console.
            /// </summary>
            Console = 1,

            /// <summary>
            /// Write into File.
            /// </summary>
            File = 2,
        }
    }
}
