namespace windbg_debug.WinDbg.Messages
{
    public class ScopesMessage : Message
    {
        #region Constructor

        public ScopesMessage(int frameId)
        {
            FrameId = frameId;
        }

        #endregion

        #region Public Properties

        public int FrameId { get; private set; }

        #endregion
    }
}
