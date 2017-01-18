namespace WinDbgDebug.WinDbg.Results
{
    public class MessageResult
    {
        #region Constructor

        static MessageResult()
        {
            Empty = new MessageResult();
        }

        #endregion

        #region Public Properties

        public static MessageResult Empty { get; internal set; }

        #endregion
    }
}
