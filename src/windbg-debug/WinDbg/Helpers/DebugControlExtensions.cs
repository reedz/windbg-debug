using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class DebugControlExtensions
    {
        public static void ClearBreakpoints(this IDebugControl4 control)
        {
            if (control == null)
                return;

            uint number;
            var hr = control.GetNumberBreakpoints(out number);
            if (hr != HResult.Ok)
                return;

            for (uint breakpointIndex = 0; breakpointIndex < number; breakpointIndex++)
            {
                IDebugBreakpoint2 breakPoint;
                hr = control.GetBreakpointByIndex2(breakpointIndex, out breakPoint);
                if (hr != HResult.Ok)
                    continue;

                control.RemoveBreakpoint2(breakPoint);
            }
        }
    }
}
