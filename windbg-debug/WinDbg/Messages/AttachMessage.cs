namespace WinDbgDebug.WinDbg.Messages
{
    public class AttachMessage : Message
    {
        #region Constructor

        public AttachMessage(int processId)
        {
            ProcessId = processId;
        }

        #endregion

        #region Public Properties

        public int ProcessId { get; private set; }

        #endregion
    }
}