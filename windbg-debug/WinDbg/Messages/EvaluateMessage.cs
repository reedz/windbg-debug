namespace windbg_debug.WinDbg.Messages
{
    public class EvaluateMessage : Message
    {
        public EvaluateMessage(string expression)
        {
            Expression = expression;
        }

        public string Expression { get; private set; }
    }
}
