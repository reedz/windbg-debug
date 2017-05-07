using System;
using System.Diagnostics;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class ProcessExtensions
    {
        public static bool Is64BitProcess(this Process process)
        {
            if (!Environment.Is64BitOperatingSystem)
                return false;

            bool is32Bit;
            if (!NativeMethods.IsWow64Process(process.Handle, out is32Bit))
                return false;

            return !is32Bit;
        }
    }
}
