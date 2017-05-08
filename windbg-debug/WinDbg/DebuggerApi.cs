using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Messages;
using WinDbgDebug.WinDbg.Results;

namespace WinDbgDebug.WinDbg
{
    public class DebuggerApi
    {
        private static readonly ILog _logger = LogManager.GetLogger(nameof(DebuggerApi));
        private readonly WinDbgWrapper _wrapper;
        private readonly TimeSpan _timeout;

        public DebuggerApi(WinDbgWrapper wrapper, TimeSpan operationTimeout = default(TimeSpan))
        {
            if (wrapper == null)
            {
                throw new ArgumentNullException(nameof(wrapper));
            }

            _wrapper = wrapper;
            _timeout = operationTimeout == default(TimeSpan) ? Defaults.Timeout : operationTimeout;
        }

        public string Launch(string path, string arguments)
        {
            _logger.Info($"Launching application for debugging: {path} {arguments}");

            var result = _wrapper.HandleMessage<LaunchMessageResult>(new LaunchMessage(path, arguments), _timeout).Result;

            return result.Error;
        }

        public Task StepOver()
        {
            return _wrapper.HandleMessageWithoutResult(new StepOverMessage());
        }

        public Task Pause()
        {
            var result = _wrapper.HandleMessageWithoutResult(new PauseMessage());
            _wrapper.Interrupt();

            return result;
        }

        public IEnumerable<Scope> GetCurrentScopes(int frameId)
        {
            var result = _wrapper.HandleMessage<ScopesMessageResult>(new ScopesMessage(frameId), _timeout).Result;

            return result.Scopes;
        }

        public IReadOnlyDictionary<Breakpoint, bool> SetBreakpoints(IEnumerable<Breakpoint> breakpoints)
        {
            _logger.Info($"Setting breakpoints ..");
            foreach (var breakpoint in breakpoints)
                _logger.Debug($"\t{breakpoint.File} : #{breakpoint.Line}");

            var result = _wrapper.HandleMessage<SetBreakpointsMessageResult>(new SetBreakpointsMessage(breakpoints), _timeout).Result;

            return result.BreakpointsSet;
        }

        public IEnumerable<StackTraceFrame> GetCurrentStackTrace(int threadId)
        {
            var result = _wrapper.HandleMessage<StackTraceMessageResult>(new StackTraceMessage(threadId), _timeout).Result;

            return result.Frames;
        }

        public Task StepInto()
        {
            return _wrapper.HandleMessageWithoutResult(new StepIntoMessage());
        }

        public Task StepOut()
        {
            return _wrapper.HandleMessageWithoutResult(new StepOutMessage());
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

        public Task Terminate()
        {
            _logger.Info($"Terminating ..");

            var result = _wrapper.HandleMessageWithoutResult(new TerminateMessage());
            _wrapper.Interrupt();

            return result;
        }

        public Task Continue()
        {
            return _wrapper.HandleMessageWithoutResult(new ContinueMessage());
        }

        public string Attach(int processId)
        {
            _logger.Info($"Attaching to PID: {processId}");

            var result = _wrapper.HandleMessage<AttachMessageResult>(new AttachMessage(processId), _timeout).Result;

            return result.Error;
        }
    }
}
