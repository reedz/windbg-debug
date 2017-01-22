namespace WinDbgDebug.WinDbg.Results
{
    public class AttachMessageResult : MessageResult
    {
        #region Constructor

        public AttachMessageResult(string error = default(string))
        {
            Error = error ?? string.Empty;
        }

        #endregion

        #region Public Properties

        public bool Success
        {
            get { return string.IsNullOrWhiteSpace(Error); }
        }
        public string Error { get; private set; }

        #endregion
    }
}