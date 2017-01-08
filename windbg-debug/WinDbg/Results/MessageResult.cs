namespace windbg_debug.WinDbg.Results
{
    public class MessageResult
    {
        static MessageResult()
        {
            Empty = new MessageResult();
        }

        public static MessageResult Empty { get; internal set; }
    }
}
