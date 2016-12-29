namespace windbg_debug.WinDbg.Data
{
    public class StackTraceFrame
    {
        public StackTraceFrame(ulong offset, int line, string filePath, int order)
        {
            Offset = offset;
            Line = line;
            FilePath = filePath;
            Order = order;
        }

        public ulong Offset { get; private set; }
        public int Line { get; private set; }
        public string FilePath { get; private set; }
        public int Order { get; private set; }
    }
}
