namespace WinDbgDebug.WinDbg.Data
{
    public class DebuggedProcessInfo
    {
        public uint ProcessId { get; set; }
        public bool Is64BitProcess { get; set; }
        public int PointerSize
        {
            get
            {
                return Is64BitProcess ? 8 : 4;
            }
        }
    }
}
