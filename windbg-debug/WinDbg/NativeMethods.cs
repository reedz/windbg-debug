using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Runtime.InteropServices;

namespace windbg_debug.WinDbg
{
    public static class NativeMethods
    {
        private const string KernelLibraryName = "kernel32.dll";
        private const string DebuggerEngineLibraryName = "dbgeng.dll";

        [DllImport(DebuggerEngineLibraryName, EntryPoint = "DebugCreate", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int DebugCreate([In][MarshalAs(UnmanagedType.LPStruct)]Guid interfaceId, out IDebugClient debugClient);

        [DllImport(KernelLibraryName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDllDirectory(string folderPath);
    }
}
