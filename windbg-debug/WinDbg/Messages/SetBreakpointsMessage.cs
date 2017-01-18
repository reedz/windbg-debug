using System.Collections.Generic;

namespace WinDbgDebug.WinDbg.Messages
{
    public class SetBreakpointsMessage : Message
    {
        #region Constructor

        public SetBreakpointsMessage(IEnumerable<Breakpoint> breakpoints)
        {
            Breakpoints = breakpoints;
        }

        #endregion

        #region Public Properties

        public IEnumerable<Breakpoint> Breakpoints { get; private set; }

        #endregion
    }
}
