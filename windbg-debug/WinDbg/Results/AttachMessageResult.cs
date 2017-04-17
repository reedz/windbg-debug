namespace WinDbgDebug.WinDbg.Results
{
    public class AttachMessageResult : MessageResult
    {
        public AttachMessageResult(string error = default(string))
        {
            Error = error ?? string.Empty;
        }

        public bool Success
        {
            get { return string.IsNullOrWhiteSpace(Error); }
        }
        public string Error { get; private set; }
    }
}