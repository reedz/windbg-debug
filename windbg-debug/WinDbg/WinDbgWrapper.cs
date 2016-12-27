using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using windbg_debug.WinDbg.Results;

namespace windbg_debug.WinDbg
{
    public class WinDbgWrapper
    {
        private const DEBUG_CREATE_PROCESS DEBUG = (DEBUG_CREATE_PROCESS)DEBUG_PROCESS.DETACH_ON_EXIT;
        private static readonly int CodeOk = 0;
        private int _lastBreakpointId = 1;
        private string _path;

        [ThreadStatic]
        private IDebugClient _debugger;
        [ThreadStatic]
        private IDebugControl _control;
        [ThreadStatic]
        private IDebugSymbols _symbols;

        public int ProcessId { get; internal set; }

        public WinDbgWrapper(string enginePath)
        {
            if (!string.IsNullOrWhiteSpace(enginePath))
                NativeMethods.SetDllDirectory(Path.GetDirectoryName(enginePath));

            _debugger = CreateDebuggerClient();
            _control = _debugger as IDebugControl;
            _symbols = _debugger as IDebugSymbols2;
            var callbacks = _debugger as IDebugEventCallbacks;
            _debugger.SetEventCallbacks(new EventCallbacks());
            _debugger.SetOutputCallbacks(new OutputCallbacks(new Logger(true)));

            var path = NativeMethods.GetDllPath();
        }

        public void EndSession()
        {
            _debugger.EndSession(DEBUG_END.ACTIVE_TERMINATE);
        }

        public LaunchResult Launch(string path, string arguments)
        {
            _path = Path.GetFullPath(path);

            int hr = _debugger.CreateProcess(
                0,
                $"{_path} {arguments}",
                (DEBUG_CREATE_PROCESS)1);

            if (hr != CodeOk)
                return new LaunchResult($"IDebugClient::CreateProcess {hr}");

            hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, (uint)TimeSpan.FromSeconds(30).TotalMilliseconds);
            if (hr != CodeOk)
                return new LaunchResult($"IDebugClient::CreateProcess {hr}");

            ReadCreatedProcessId();
            hr = ForceLoadSymbols();

            //hr = _control.SetExecutionStatus(DEBUG_STATUS.GO);
            //if (hr != CodeOk)
            //    return new LaunchResult($"IDebugClient::CreateProcess {hr}");

            return new LaunchResult();
        }

        private void ReadCreatedProcessId()
        {
            uint processId;
            var result = _debugger.GetRunningProcessSystemIdByExecutableName(0, Path.GetFileName(_path), DEBUG_GET_PROC.FULL_MATCH, out processId);
            if (result == CodeOk)
            {
                ProcessId = (int)processId;
            }
        }

        private int ForceLoadSymbols()
        {
            int hr = _symbols.SetSymbolPath(Path.GetDirectoryName(_path));
            hr = _symbols.Reload(Path.GetFileNameWithoutExtension(_path));
            ulong handle, offset;
            uint matchSize;
            hr = _symbols.StartSymbolMatch("*", out handle);
            _symbols.GetNextSymbolMatch(handle, new StringBuilder(1024), 1024, out matchSize, out offset);
            return hr;
        }

        public void SetBreakpoints(IEnumerable<Breakpoint> breakpoints)
        {
            foreach (var breakpoint in breakpoints)
            {
                var id = Interlocked.Increment(ref _lastBreakpointId);
                IDebugBreakpoint breakpointToSet;
                var result = _control.AddBreakpoint(DEBUG_BREAKPOINT_TYPE.CODE, (uint)id, out breakpointToSet);
                if (result != CodeOk)
                    throw new Exception("!");

                ulong offset;
                result = _symbols.GetOffsetByLine((uint)breakpoint.Line, breakpoint.File, out offset);
                if (result != CodeOk)
                    throw new Exception("!");

                // TODO: Add hresult handling
                result = breakpointToSet.SetOffset(offset);
                result = breakpointToSet.SetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED);
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
