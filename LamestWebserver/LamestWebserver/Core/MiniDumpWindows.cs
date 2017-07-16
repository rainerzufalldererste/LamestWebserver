using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Contains functionality to write Windows Mini DumpFiles from the current process.
    /// 
    /// Source: https://blogs.msdn.microsoft.com/dondu/2010/10/24/writing-minidumps-in-c/ , http://blog.kalmbach-software.de/2008/12/13/writing-minidumps-in-c/
    /// </summary>
    public static class MiniDump
    {
        /// <summary>
        /// Options for MiniDumps.
        /// From dbghelp.h
        /// </summary>
        [Flags]
        public enum Option : uint
        {
            Normal = 0x00000000,
            WithDataSegs = 0x00000001,
            WithFullMemory = 0x00000002,
            WithHandleData = 0x00000004,
            FilterMemory = 0x00000008,
            ScanMemory = 0x00000010,
            WithUnloadedModules = 0x00000020,
            WithIndirectlyReferencedMemory = 0x00000040,
            FilterModulePaths = 0x00000080,
            WithProcessThreadData = 0x00000100,
            WithPrivateReadWriteMemory = 0x00000200,
            WithoutOptionalData = 0x00000400,
            WithFullMemoryInfo = 0x00000800,
            WithThreadInfo = 0x00001000,
            WithCodeSegs = 0x00002000,
            WithoutAuxiliaryState = 0x00004000,
            WithFullAuxiliaryState = 0x00008000,
            WithPrivateWriteCopyMemory = 0x00010000,
            IgnoreInaccessibleMemory = 0x00020000,
            ValidTypeFlags = 0x0003ffff,
        };


        /// <summary>
        /// Exception Information for MiniDumps.
        /// </summary>
        public enum ExceptionInfo
        {
            None,
            Present
        }


        /// <summary>
        /// MiniDump File Exception Information.
        /// <code>
        /// typedef struct _MINIDUMP_EXCEPTION_INFORMATION {
        ///     DWORD ThreadId;
        ///     PEXCEPTION_POINTERS ExceptionPointers;
        ///     BOOL ClientPointers;
        /// } MINIDUMP_EXCEPTION_INFORMATION, *PMINIDUMP_EXCEPTION_INFORMATION;</code>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]  // Pack = 4 is important! So it works also for x64!
        public struct MiniDumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;

            [MarshalAs(UnmanagedType.Bool)]
            public bool ClientPointers;
        }

        /// <summary>
        /// Overload requiring MiniDumpExceptionInformation
        /// <code>
        /// BOOL
        /// WINAPI
        /// MiniDumpWriteDump(
        ///     __in HANDLE hProcess,
        ///     __in DWORD ProcessId,
        ///     __in HANDLE hFile,
        ///     __in MINIDUMP_TYPE DumpType,
        ///     __in_opt PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
        ///     __in_opt PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
        ///     __in_opt PMINIDUMP_CALLBACK_INFORMATION CallbackParam
        ///     );
        /// </code>
        /// </summary>
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, ref MiniDumpExceptionInformation expParam, IntPtr userStreamParam, IntPtr callbackParam);
        
        /// <summary>
        /// Overload supporting MiniDumpExceptionInformation == NULL
        /// </summary>
        [DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeHandle hFile, uint dumpType, IntPtr expParam, IntPtr userStreamParam, IntPtr callbackParam);
        
        /// <summary>
        /// Retrieves the current ThreadID.
        /// </summary>
        /// <returns>Returns the current ThreadID.</returns>
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
        static extern uint GetCurrentThreadId();

        /// <summary>
        /// Writes current state to MiniDump.
        /// </summary>
        /// <param name="fileHandle">The file handle of the dump file to write.</param>
        /// <param name="dumpType">MiniDump type.</param>
        /// <param name="exceptionInfo">Exception info options.</param>
        /// <returns>returns true if successfull.</returns>
        public static bool Write(SafeHandle fileHandle, Option dumpType, ExceptionInfo exceptionInfo)
        {
            Process currentProcess = Process.GetCurrentProcess();
            IntPtr currentProcessHandle = currentProcess.Handle;
            uint currentProcessId = (uint)currentProcess.Id;

            MiniDumpExceptionInformation exp;
            exp.ThreadId = GetCurrentThreadId();
            exp.ClientPointers = false;
            exp.ExceptionPointers = IntPtr.Zero;

            if (exceptionInfo == ExceptionInfo.Present)
            {
                exp.ExceptionPointers = Marshal.GetExceptionPointers();
            }

            bool bRet = false;

            if (exp.ExceptionPointers == IntPtr.Zero)
            {
                bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)dumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                bRet = MiniDumpWriteDump(currentProcessHandle, currentProcessId, fileHandle, (uint)dumpType, ref exp, IntPtr.Zero, IntPtr.Zero);
            }

            return bRet;
        }

        /// <summary>
        /// Writes current state to MiniDump.
        /// </summary>
        /// <param name="fileHandle">The file handle of the dump file to write.</param>
        /// <param name="dumpType">MiniDump type.</param>
        /// <returns>returns true if successfull.</returns>
        public static bool Write(SafeHandle fileHandle, Option dumpType) => Write(fileHandle, dumpType, ExceptionInfo.None);

        /// <summary>
        /// Writes current state to MiniDump.
        /// </summary>
        /// <param name="filename">the name of the file to write to. (usually *.mdmp)</param>
        /// <param name="dumpType">MiniDump type.</param>
        /// <returns>returns true if successfull.</returns>
        public static bool Write(string filename, Option dumpType)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                return Write(fs.SafeFileHandle, Option.WithFullMemory);
            }
        }

        /// <summary>
        /// Writes current state to MiniDump.
        /// </summary>
        /// <param name="dumpType">MiniDump type.</param>
        /// <returns>returns true if successfull.</returns>
        public static bool Write(Option dumpType = Option.Normal | Option.WithProcessThreadData | Option.WithThreadInfo) => Write("__dump_" + Environment.CurrentManagedThreadId + "-" + DateTime.UtcNow.ToBinary().ToString() + ".mdmp", dumpType);
    }
}
