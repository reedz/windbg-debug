using Microsoft.Diagnostics.Runtime.Interop;

namespace windbg_debug
{
    public static class Defaults
    {
        public static int BufferSize = 1024;
        public static ulong CurrentOffset = 0;
        public static DEBUG_CREATE_PROCESS DEBUG = (DEBUG_CREATE_PROCESS)DEBUG_PROCESS.DETACH_ON_EXIT;
        public static ulong NoServer = 0;
        public static uint NoProcess = 0;
    }
}
