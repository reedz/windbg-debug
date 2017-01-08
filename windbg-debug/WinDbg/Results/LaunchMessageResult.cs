namespace windbg_debug.WinDbg.Results
{
    public class LaunchMessageResult : MessageResult
    {
        #region Constructor

        public LaunchMessageResult():this(null)
        {
        }

        public LaunchMessageResult(string error)
        {
            Error = error;
        }

        #endregion

        #region Public Properties

        public bool Success {  get { return string.IsNullOrEmpty(Error); } }
        public string Error { get; private set; }

        #endregion
    }
}
