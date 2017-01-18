namespace WinDbgDebug.WinDbg.Results
{
    public class EvaluateMessageResult : MessageResult
    {
        #region Constructor

        public EvaluateMessageResult(string value)
        {
            Value = value;
        }

        #endregion

        #region Public Properties

        public string Value { get; private set; }

        #endregion
    }
}
