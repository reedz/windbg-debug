using System;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg
{
    public class EventCallbacks : IDebugEventCallbacksWide
    {
        #region Fields

        private IDebugControl6 _control;

        #endregion

        #region Constructor

        public EventCallbacks(IDebugControl6 control)
        {
            _control = control;
        }

        #endregion

        #region Public Events

        public event EventHandler<IDebugBreakpoint> BreakpointHit;
        public event EventHandler<EXCEPTION_RECORD64> ExceptionHit;
        public event EventHandler BreakHappened;
        public event EventHandler<int> ProcessExited;
        public event EventHandler ThreadStarted;
        public event EventHandler ThreadFinished;

        #endregion

        #region Public Methods

        public int Breakpoint(IDebugBreakpoint2 breakpoint)
        {
            BreakpointHit?.Invoke(this, breakpoint);
            return (int)DEBUG_STATUS.BREAK;
        }

        public int Breakpoint(IDebugBreakpoint breakpoint)
        {
            BreakpointHit?.Invoke(this, breakpoint);
            return (int)DEBUG_STATUS.BREAK;
        }

        public int ChangeDebuggeeState(DEBUG_CDS Flags, ulong Argument)
        {
            return HResult.Ok;
        }

        public int ChangeEngineState(DEBUG_CES Flags, ulong Argument)
        {
            if (Flags == DEBUG_CES.EXECUTION_STATUS && Argument == (ulong)DEBUG_STATUS.BREAK)
                BreakHappened?.Invoke(this, new EventArgs());

            return HResult.Ok;
        }

        public int ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
        {
            return HResult.Ok;
        }

        public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
        {
            return (int)DEBUG_STATUS.BREAK;
        }

        public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
        {
            ThreadStarted?.Invoke(this, null);
            return HResult.Ok;
        }

        public int Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
        {
            ExceptionHit?.Invoke(this, Exception);
            return (int)DEBUG_STATUS.BREAK;
        }

        public int ExitProcess(uint ExitCode)
        {
            ProcessExited?.Invoke(this, (int)ExitCode);
            return HResult.Ok;
        }

        public int ExitThread(uint ExitCode)
        {
            ThreadFinished?.Invoke(this, null);
            return HResult.Ok;
        }

        public int GetInterestMask(out DEBUG_EVENT Mask)
        {
            Mask = DEBUG_EVENT.BREAKPOINT
                | DEBUG_EVENT.CHANGE_ENGINE_STATE
                | DEBUG_EVENT.CREATE_PROCESS
                | DEBUG_EVENT.CREATE_THREAD
                | DEBUG_EVENT.EXCEPTION
                | DEBUG_EVENT.EXIT_PROCESS
                | DEBUG_EVENT.EXIT_THREAD
                | DEBUG_EVENT.SESSION_STATUS
                | DEBUG_EVENT.SYSTEM_ERROR;

            return HResult.Ok;
        }

        public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
        {
            return HResult.Ok;
        }

        public int SessionStatus(DEBUG_SESSION Status)
        {
            return HResult.Ok;
        }

        public int SystemError(uint Error, uint Level)
        {
            ExceptionHit?.Invoke(this, new EXCEPTION_RECORD64 { ExceptionCode = Error });
            return HResult.Ok;
        }

        public int UnloadModule(string ImageBaseName, ulong BaseOffset)
        {
            return HResult.Ok;
        }

        #endregion
    }
}
