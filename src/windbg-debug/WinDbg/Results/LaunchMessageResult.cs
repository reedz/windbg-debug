namespace WinDbgDebug.WinDbg.Results
{
    public class LaunchMessageResult : MessageResult
    {
        public LaunchMessageResult()
            : this(null)
        {
        }

        public LaunchMessageResult(string error)
        {
            Error = error;
        }

        public bool Success
        {
            get { return string.IsNullOrEmpty(Error); }
        }
        public string Error { get; private set; }
    }
}
