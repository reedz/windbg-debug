namespace windbg_debug.WinDbg.Messages
{
    public class EvaluateMessage : Message
    {
        #region Constructor

        public EvaluateMessage(string expression)
        {
            Expression = expression;
        }

        #endregion

        #region Public Properties

        public string Expression { get; private set; }

        #endregion
    }
}
