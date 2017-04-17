using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg
{
    public static class NativeMethods
    {
        private const string KernelLibraryName = "kernel32.dll";
        private const string DebuggerEngineLibraryName = "dbgeng.dll";

        [DllImport(DebuggerEngineLibraryName, EntryPoint = "DebugCreate", SetLastError = false, CallingConvention = CallingConvention.StdCall)]
        public static extern int DebugCreate([In][MarshalAs(UnmanagedType.LPStruct)]Guid interfaceId, out IDebugClient debugClient);

        [DllImport(KernelLibraryName, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDllDirectory(string folderPath);

        public static string GetDllPath()
        {
            const int MAX_PATH = 260;
            StringBuilder builder = new StringBuilder(MAX_PATH);
            IntPtr hModule = GetModuleHandle(DebuggerEngineLibraryName);

            uint size = GetModuleFileName(hModule, builder, builder.Capacity);
            return builder.ToString();
        }

        [DllImport(KernelLibraryName, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(KernelLibraryName, SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileName(
            [In] IntPtr hModule,
            [Out] StringBuilder lpFilename,
            [In][MarshalAs(UnmanagedType.U4)] int nSize);
    }
}
