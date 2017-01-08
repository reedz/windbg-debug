namespace windbg_debug.WinDbg.Results
{
    public class EvaluateMessageResult : MessageResult
    {
        public EvaluateMessageResult(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}
