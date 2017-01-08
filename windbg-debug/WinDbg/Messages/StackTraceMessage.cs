namespace windbg_debug.WinDbg.Messages
{
    public class StackTraceMessage : Message
    {
        #region Constructor

        public StackTraceMessage(int threadId)
        {
            ThreadId = threadId;
        }

        #endregion

        #region Public Properties

        public int ThreadId { get; private set; }

        #endregion
    }
}
