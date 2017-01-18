namespace WinDbgDebug.WinDbg.Messages
{
    public class VariablesMessage : Message
    {
        #region Constructor

        public VariablesMessage(int parentId)
        {
            ParentId = parentId;
        }

        #endregion

        #region Public Properties

        // Might be both Scope or Variable identifier.
        public int ParentId { get; private set; }

        #endregion
    }
}
