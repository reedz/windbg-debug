using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WinDbgDebug.WinDbg.Results
{
    public class SetBreakpointsMessageResult : MessageResult
    {
        public SetBreakpointsMessageResult(Dictionary<Breakpoint, bool> breakpoints)
        {
            BreakpointsSet = new ReadOnlyDictionary<Breakpoint, bool>(breakpoints);
        }

        public IReadOnlyDictionary<Breakpoint, bool> BreakpointsSet { get; private set; }
    }
}
