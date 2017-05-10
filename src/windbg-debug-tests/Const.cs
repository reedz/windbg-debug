using System;
using System.IO;
using System.Reflection;
using WinDbgDebug;

namespace windbg_debug_tests
{
    public static class Const
    {
        public static string PathToEngine = "C:\\Program Files (x86)\\Windows Kits\\10\\Debuggers\\x64\\dbgeng.dll";
        public static readonly int DefaultTimeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;
        public static readonly int DefaultPollingInterval = (int)TimeSpan.FromMilliseconds(500).TotalMilliseconds;
        public static readonly string TestDebuggeesFolder = Path.Combine(
            AssemblyHelpers.ResolveAssemblyDirectory(Assembly.GetExecutingAssembly()),
            "..\\..\\test-debuggees");
    }
}
