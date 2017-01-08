namespace windbg_debug.WinDbg.Messages
{
    public class VariablesMessage : Message
    {
        public VariablesMessage(int parentId)
        {
            ParentId = parentId;
        }

        /// <summary>
        /// Might be both Scope or Variable identifier.
        /// </summary>
        public int ParentId { get; private set; }
    }
}
