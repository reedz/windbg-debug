using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using windbg_debug.WinDbg.Results;

namespace windbg_debug.WinDbg
{
    public class WinDbgWrapper
    {
        private const DEBUG_CREATE_PROCESS DEBUG = (DEBUG_CREATE_PROCESS)1;
        private static readonly int CodeOk = 0;
        private int _lastBreakpointId = 1;

        [ThreadStatic]
        private IDebugClient5 _debugger5;
        [ThreadStatic]
        private IDebugClient4 _debugger4;
        [ThreadStatic]
        private IDebugClient3 _debugger3;
        [ThreadStatic]
        private IDebugClient2 _debugger2;
        [ThreadStatic]
        private IDebugClient _debugger;

        [ThreadStatic]
        private IDebugControl _control;
        [ThreadStatic]
        private IDebugControl2 _control2;
        [ThreadStatic]
        private IDebugControl3 _control3;
        [ThreadStatic]
        private IDebugControl4 _control4;
        [ThreadStatic]
        private IDebugControl5 _control5;
        [ThreadStatic]
        private IDebugControl6 _control6;

        private IDebugSymbols _symbols;

        public WinDbgWrapper(string enginePath)
        {
            if (!string.IsNullOrWhiteSpace(enginePath))
                NativeMethods.SetDllDirectory(Path.GetDirectoryName(enginePath));

            _debugger = CreateDebuggerClient();
            _debugger2 = _debugger as IDebugClient2;
            _debugger3 = _debugger as IDebugClient3;
            _debugger4 = _debugger as IDebugClient4;
            _debugger5 = _debugger as IDebugClient5;

            _control = _debugger as IDebugControl;
            _control2 = _debugger as IDebugControl2;
            _control3 = _debugger as IDebugControl3;
            _control4 = _debugger as IDebugControl4;
            _control5 = _debugger as IDebugControl5;
            _control6 = _debugger as IDebugControl6;

            _symbols = _debugger as IDebugSymbols;
        }

        public void EndSession()
        {
            _debugger.EndSession(DEBUG_END.ACTIVE_TERMINATE);
        }

        public LaunchResult Launch(string path, string arguments)
        {
            int hr = _debugger.CreateProcess(
                0,
                $"cmd /k \"{Path.GetFullPath(path)} {arguments}\"",
                DEBUG);

            if (hr != CodeOk)
                return new LaunchResult($"IDebugClient::CreateProcessAndAttach2 {hr}");

            hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, (uint)TimeSpan.FromSeconds(20).TotalMilliseconds);
            if (hr != CodeOk)
                return new LaunchResult($"IDebugClient::CreateProcessAndAttach2 {hr}");

            return new LaunchResult();
        }

        public void SetBreakpoints(IEnumerable<Breakpoint> breakpoints)
        {
            foreach (var breakpoint in breakpoints)
            {
                var id = Interlocked.Increment(ref _lastBreakpointId);
                IDebugBreakpoint breakpointToSet;
                _control6.AddBreakpoint(DEBUG_BREAKPOINT_TYPE.CODE, (uint)id, out breakpointToSet);
                ulong offset;
                var result = _symbols.GetOffsetByLine((uint)breakpoint.Line, breakpoint.File, out offset);
                if (result != CodeOk)
                    throw new Exception("!");

                // TODO: Add hresult handling
                breakpointToSet.SetOffset(offset);
                breakpointToSet.SetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED);
            }
        }

        private static IDebugClient CreateDebuggerClient()
        {
            IDebugClient result;
            var errorCode = NativeMethods.DebugCreate(typeof(IDebugClient).GUID, out result);
            if (errorCode != CodeOk)
                throw new Exception($"Could not create debugger client. HRESULT = '{errorCode}'");

            return result;
        }
    }
}
