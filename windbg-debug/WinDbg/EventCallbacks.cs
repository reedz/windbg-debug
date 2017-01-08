using Microsoft.Diagnostics.Runtime.Interop;
using System;

namespace windbg_debug.WinDbg
{
    public class EventCallbacks : IDebugEventCallbacksWide
    {
        private const int CodeOk = 0;
        public event EventHandler<IDebugBreakpoint> BreakpointHit;
        private IDebugControl6 _control;
        public event EventHandler<EXCEPTION_RECORD64> ExceptionHit;
        public event EventHandler BreakHappened;

        public EventCallbacks(IDebugControl6 control)
        {
            _control = control;
        }

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
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int ChangeEngineState(DEBUG_CES Flags, ulong Argument)
        {
            if (Flags == DEBUG_CES.EXECUTION_STATUS && Argument == (ulong)DEBUG_STATUS.BREAK)
                BreakHappened?.Invoke(this, new EventArgs());

            return CodeOk;
        }

        public int ChangeSymbolState(DEBUG_CSS Flags, ulong Argument)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int CreateProcess(ulong ImageFileHandle, ulong Handle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp, ulong InitialThreadHandle, ulong ThreadDataOffset, ulong StartOffset)
        {
            return (int)DEBUG_STATUS.BREAK;
        }

        public int CreateThread(ulong Handle, ulong DataOffset, ulong StartOffset)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int Exception(ref EXCEPTION_RECORD64 Exception, uint FirstChance)
        {
            ExceptionHit?.Invoke(this, Exception);
            return (int)DEBUG_STATUS.BREAK;
        }

        public int ExitProcess(uint ExitCode)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int ExitThread(uint ExitCode)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int GetInterestMask(out DEBUG_EVENT Mask)
        {
            Mask = DEBUG_EVENT.BREAKPOINT 
                | DEBUG_EVENT.CHANGE_DEBUGGEE_STATE 
                | DEBUG_EVENT.CHANGE_ENGINE_STATE
                | DEBUG_EVENT.CHANGE_SYMBOL_STATE 
                | DEBUG_EVENT.CREATE_PROCESS 
                | DEBUG_EVENT.CREATE_THREAD
                | DEBUG_EVENT.EXCEPTION 
                | DEBUG_EVENT.EXIT_PROCESS 
                | DEBUG_EVENT.EXIT_THREAD
                | DEBUG_EVENT.LOAD_MODULE 
                | DEBUG_EVENT.SESSION_STATUS 
                | DEBUG_EVENT.SYSTEM_ERROR
                | DEBUG_EVENT.UNLOAD_MODULE;

            return CodeOk;
        }

        public int LoadModule(ulong ImageFileHandle, ulong BaseOffset, uint ModuleSize, string ModuleName, string ImageName, uint CheckSum, uint TimeDateStamp)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int SessionStatus(DEBUG_SESSION Status)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int SystemError(uint Error, uint Level)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }

        public int UnloadModule(string ImageBaseName, ulong BaseOffset)
        {
            //throw new NotImplementedException();
            return CodeOk;
        }
    }
}
