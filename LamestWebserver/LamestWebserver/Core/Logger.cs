using System;

namespace LamestWebserver.Core
{
    /// <summary>
    /// A simple Logger for LamestWebserver.
    /// </summary>
    public class Logger
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
        public static ELoggingLevel DefaultMinimumLoggingLevel = ELoggingLevel.Trace;

        /// <summary>
        /// The minimum logging level for this logger.
        /// </summary>
        public ELoggingLevel MinimumLoggingLevel = DefaultMinimumLoggingLevel;

        /// <summary>
        /// Logging a message on logging level 'Trace'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogTrace(string msg) => CurrentLogger.Instance.Trace(msg);

        /// <summary>
        /// Logging a message on logging level 'Trace'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Trace(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Trace)
                return;

            Log(ELoggingLevel.Trace, msg);
        }

        /// <summary>
        /// Logging a message on logging level 'Information'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogInformation(string msg) => CurrentLogger.Instance.Information(msg);

        /// <summary>
        /// Logging a message on logging level 'Information'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Information(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Information)
                return;

            Log(ELoggingLevel.Information, msg);
        }

        /// <summary>
        /// Logging a message on logging level 'Warning'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogWarning(string msg) => CurrentLogger.Instance.Warning(msg);

        /// <summary>
        /// Logging a message on logging level 'Warning'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Warning(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Warning)
                return;

            Log(ELoggingLevel.Warning, msg);
        }

        /// <summary>
        /// Logging a message on logging level 'Error'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogError(string msg) => CurrentLogger.Instance.Error(msg);

        /// <summary>
        /// Logging a message on logging level 'Error'.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Error(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Error)
                return;

            Log(ELoggingLevel.Error, msg);
        }

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        public static void LogDebugExcept(Exception exception) => CurrentLogger.Instance.DebugExcept(exception);

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        public void DebugExcept(Exception exception)
        {
            if (MinimumLoggingLevel < ELoggingLevel.DebugExcept)
                return;

            Log(ELoggingLevel.DebugExcept, "{" + exception.GetType() + "} " + exception.Message);

            if (LoggerDebugMode)
                throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. An Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogDebugExcept(string msg) => CurrentLogger.Instance.DebugExcept(msg);

        /// <summary>
        /// Logging a message on logging level 'DebugExcept'. The Exception will be thrown if Logger.LoggerDebugMode is true.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void DebugExcept(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.DebugExcept)
                return;

            Log(ELoggingLevel.DebugExcept, msg);

            if (LoggerDebugMode)
                throw new Exception(msg);
        }

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        public static void LogExcept(Exception exception) => CurrentLogger.Instance.Except(exception);

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="exception">The exception, of which the message will be logged.</param>
        public void Except(Exception exception)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Except)
                return;

            Log(ELoggingLevel.Except, "{" + exception.GetType() + "} " + exception.Message);

            throw exception;
        }

        /// <summary>
        /// Logging a message on logging level 'Except'.  An exception will be thrown.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogExcept(string msg) => CurrentLogger.Instance.Except(msg);

        /// <summary>
        /// Logging a message on logging level 'Except'. The Exception will be thrown.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void Except(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.Except)
                return;

            Log(ELoggingLevel.Except, msg);

            throw new Exception(msg);
        }

        /// <summary>
        /// Logging a message on logging level 'CrashAndBurn'. The logger will be closed, a dump file will be written and the application will be exited with error code -1.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public static void LogCrashAndBurn(string msg) => CurrentLogger.Instance.CrashAndBurn(msg);

        /// <summary>
        /// Logging a message on logging level 'CrashAndBurn'. The logger will be closed, a dump file will be written and the application will be exited with error code -1.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        public void CrashAndBurn(string msg)
        {
            if (MinimumLoggingLevel < ELoggingLevel.CrashAndBurn)
                return;

            Log(ELoggingLevel.CrashAndBurn, msg);

            try
            {
                Close();
            }
            catch { }

            MiniDump.Write();
            Environment.Exit(-1);
        }

        internal void Log(ELoggingLevel loggingLevel, string msg)
        {
            // TODO: Implement propper logging!

            ServerHandler.LogMessage($"[{loggingLevel.ToString()} \\\\\\\\ {msg}]");
        }

        /// <summary>
        /// Closes and Flushes the Logger stream.
        /// </summary>
        public void Close()
        {
            // TODO: Close and Flush stream!
            throw new NotImplementedException();
        }

        /// <summary>
        /// Represents the different LoggingLevels.
        /// </summary>
        public enum ELoggingLevel : byte
        {
            /// <summary>
            /// Quits the Application writing a CrashDump.
            /// </summary>
            CrashAndBurn = 0,

            /// <summary>
            /// Throws an Excption.
            /// </summary>
            Except = 1,

            /// <summary>
            /// Throws an Exception if Logger.LoggerDebugMode is true.
            /// </summary>
            DebugExcept = 2,

            /// <summary>
            /// A major Error.
            /// </summary>
            Error = 3,

            /// <summary>
            /// A warning message.
            /// </summary>
            Warning = 4,

            /// <summary>
            /// General Information.
            /// </summary>
            Information = 5,

            /// <summary>
            /// Debugging Information.
            /// </summary>
            Trace = 6
        }
    }
}
