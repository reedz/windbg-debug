using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg
{
    public sealed class CommandExecutor
    {
        private static readonly string _stepOutCommand = "gu";
        private readonly IDebugControl _control;

        public CommandExecutor(IDebugControl control)
        {
            _control = control;
        }

        public int StepOut()
        {
            return _control.Execute(DEBUG_OUTCTL.THIS_CLIENT, _stepOutCommand, DEBUG_EXECUTE.DEFAULT);
        }
    }
}
