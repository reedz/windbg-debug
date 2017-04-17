using System.Collections.Generic;

namespace WinDbgDebug.WinDbg.Messages
{
    public class SetBreakpointsMessage : Message
    {
        public SetBreakpointsMessage(IEnumerable<Breakpoint> breakpoints)
        {
            Breakpoints = breakpoints;
        }

        public IEnumerable<Breakpoint> Breakpoints { get; private set; }
    }
}
