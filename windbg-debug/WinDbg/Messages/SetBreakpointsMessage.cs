using System.Collections.Generic;

namespace windbg_debug.WinDbg.Messages
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
