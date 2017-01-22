using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg
{
    public sealed class CommandExecutor
    {
        #region Fields

        private static readonly string _stepOutCommand = "gu";
        private readonly IDebugControl _control;

        #endregion

        #region Constructor

        public CommandExecutor(IDebugControl control)
        {
            _control = control;
        }

        #endregion

        #region Public Methods

        public int StepOut()
        {
            return _control.Execute(DEBUG_OUTCTL.THIS_CLIENT, _stepOutCommand, DEBUG_EXECUTE.DEFAULT);
        }

        #endregion
    }
}
