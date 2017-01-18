using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg
{
    public class InputCallbacks : IDebugInputCallbacks
    {
        #region Public Methods

        public int EndInput()
        {
            return 0;
        }

        public int StartInput(uint BufferSize)
        {
            return 0;
        }

        #endregion
    }
}
