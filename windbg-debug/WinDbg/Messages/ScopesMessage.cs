namespace windbg_debug.WinDbg.Messages
{
    public class ScopesMessage : Message
    {
        public ScopesMessage(int frameId)
        {
            FrameId = frameId;
        }

        public int FrameId { get; private set; }
    }
}
