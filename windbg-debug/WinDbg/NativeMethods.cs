using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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

        public static string GetDllPath()
        {
            const int MAX_PATH = 260;
            StringBuilder builder = new StringBuilder(MAX_PATH);
            IntPtr hModule = GetModuleHandle(DebuggerEngineLibraryName);  // might return IntPtr.Zero until 
                                                          // you call a method in  
                                                          // dll.dll causing it to be 
                                                          // loaded by LoadLibrary

            Debug.Assert(hModule != IntPtr.Zero);
            uint size = GetModuleFileName(hModule, builder, builder.Capacity);
            Debug.Assert(size > 0);
            return builder.ToString();   // might need to truncate nulls
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileName
        (
            [In] IntPtr hModule,
            [Out] StringBuilder lpFilename,
            [In][MarshalAs(UnmanagedType.U4)] int nSize
        );
    }
}
