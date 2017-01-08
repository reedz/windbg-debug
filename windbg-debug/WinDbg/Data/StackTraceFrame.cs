namespace windbg_debug.WinDbg.Data
{
    public class StackTraceFrame : IIndexedItem
    {
        #region Constructor

        public StackTraceFrame(int id, ulong offset, int line, string filePath, int order)
        {
            Offset = offset;
            Line = line;
            FilePath = filePath;
            Order = order;
            Id = id;
        }

        #endregion

        #region Public Properties

        public int Id { get; private set; }
        public ulong Offset { get; private set; }
        public int Line { get; private set; }
        public string FilePath { get; private set; }
        public int Order { get; private set; }

        #endregion
    }
}
