namespace WinDbgDebug.WinDbg.Messages
{
    public class AttachMessage : Message
    {
        public AttachMessage(int processId)
        {
            ProcessId = processId;
        }

        public int ProcessId { get; private set; }
    }
}