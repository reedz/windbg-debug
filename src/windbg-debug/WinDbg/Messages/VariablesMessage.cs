namespace WinDbgDebug.WinDbg.Messages
{
    public class VariablesMessage : Message
    {
        public VariablesMessage(int parentId)
        {
            ParentId = parentId;
        }

        // Might be both Scope or Variable identifier.
        public int ParentId { get; private set; }
    }
}
