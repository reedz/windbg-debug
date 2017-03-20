using System;
using System.Collections.Generic;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Messages;
using WinDbgDebug.WinDbg.Results;

namespace WinDbgDebug.WinDbg
{
    public class DebuggerApi
    {
        private readonly WinDbgWrapper _wrapper;
        private readonly TimeSpan _timeout;

        public DebuggerApi(WinDbgWrapper wrapper, TimeSpan operationTimeout = default(TimeSpan))
        {
            _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
            _timeout = operationTimeout == default(TimeSpan) ? Defaults.Timeout : operationTimeout;
        }

        public string Launch(string path, string arguments)
        {
            var result = _wrapper.HandleMessage<LaunchMessageResult>(new LaunchMessage(path, arguments), _timeout).Result;
            return result.Error;
        }

        public void StepOver()
        {
            _wrapper.HandleMessageWithoutResult(new StepOverMessage());
        }

        public void Pause()
        {
            _wrapper.HandleMessageWithoutResult(new PauseMessage());
            _wrapper.Interrupt();
        }

        public IEnumerable<Scope> GetCurrentScopes(int frameId)
        {
            var result = _wrapper.HandleMessage<ScopesMessageResult>(new ScopesMessage(frameId), _timeout).Result;

            return result.Scopes;
        }

        public IReadOnlyDictionary<Breakpoint, bool> SetBreakpoints(IEnumerable<Breakpoint> breakpoints)
        {
            var result = _wrapper.HandleMessage<SetBreakpointsMessageResult>(new SetBreakpointsMessage(breakpoints), _timeout).Result;

            return result.BreakpointsSet;
        }

        public IEnumerable<StackTraceFrame> GetCurrentStackTrace(int threadId)
        {
            var result = _wrapper.HandleMessage<StackTraceMessageResult>(new StackTraceMessage(threadId), _timeout).Result;

            return result.Frames;
        }

        public void StepInto()
        {
            _wrapper.HandleMessageWithoutResult(new StepIntoMessage());
        }

        public void StepOut()
        {
            _wrapper.HandleMessageWithoutResult(new StepOutMessage());
        }

        public IEnumerable<DebuggeeThread> GetCurrentThreads()
        {
            var result = _wrapper.HandleMessage<ThreadsMessageResult>(new ThreadsMessage(), _timeout).Result;

            return result.Threads;
        }

        public IEnumerable<Variable> GetCurrentVariables(int scopeOrVariableId)
        {
            var result = _wrapper.HandleMessage<VariablesMessageResult>(new VariablesMessage(scopeOrVariableId), _timeout).Result;

            return result.Variables;
        }

        public string Evaluate(string expression)
        {
            var result = _wrapper.HandleMessage<EvaluateMessageResult>(new EvaluateMessage(expression), _timeout).Result;

            return result.Value;
        }

        public void Terminate()
        {
            _wrapper.HandleMessageWithoutResult(new TerminateMessage());
            _wrapper.Interrupt();
        }

        public void Continue()
        {
            _wrapper.HandleMessageWithoutResult(new ContinueMessage());
        }

        public string Attach(int processId)
        {
            var result = _wrapper.HandleMessage<AttachMessageResult>(new AttachMessage(processId), _timeout).Result;

            return result.Error;
        }
    }
}
