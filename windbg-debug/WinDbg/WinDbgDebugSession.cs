using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using VSCodeDebug;
using windbg_debug.WinDbg.Data;
using windbg_debug.WinDbg.Messages;
using windbg_debug.WinDbg.Results;
using StackFrame = VSCodeDebug.StackFrame;

namespace windbg_debug.WinDbg
{
    public class WinDbgDebugSession : DebugSession
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);
        private readonly Logger _logger;
        private WinDbgWrapper _wrapper;

        public WinDbgDebugSession(Logger logger, bool traceRequests = false, bool traceResponses = false) : base(true, false)
        {
            _logger = logger;
            TRACE = traceRequests;
            TRACE_RESPONSE = traceResponses;
        }

        #region DebugSession implementation

        public override void Attach(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Continue(Response response, dynamic arguments)
        {
            _wrapper.HandleMessageWithoutResult(new ContinueMessage());
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            if (_wrapper != null)
            {
                _wrapper.HandleMessageWithoutResult(new TerminateMessage());
                _wrapper.Interrupt();
                _wrapper = null;
            }
        }

        public override void Evaluate(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Initialize(Response response, dynamic args)
        {
            LogStart();
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform != PlatformID.Win32NT)
            {
                SendErrorResponse(response, (int)ResponseCodes.PlatformNotSupported, $"WinDbg is not supported on '{os.Platform.ToString()}' platform.");
                return;
            }

            SendResponse(response, new Capabilities());
            SendEvent(new InitializedEvent());
            LogFinish();
        }

        public override void Launch(Response response, dynamic arguments)
        {
            LogStart();
            string workingDir = arguments.workingDir;
            if (!string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir))
                Environment.CurrentDirectory = workingDir;

            string target = arguments.target;
            if (string.IsNullOrWhiteSpace(target) || !File.Exists(target))
            {
                SendErrorResponse(response, (int)ResponseCodes.TargetDoesNotExist, $"Could not launch '{target}' as it does not exist.");
            }

            string args = arguments.args;
            string debuggerEnginePath = arguments.windbgpath;
            _wrapper = new WinDbgWrapper(debuggerEnginePath);
            _wrapper.BreakpointHit += OnBreakpoint;
            _wrapper.ExceptionHit += OnException;

            var result = _wrapper.HandleMessage<LaunchMessageResult>(new LaunchMessage(target, args), DefaultTimeout).Result;

            if (!result.Success)
                SendErrorResponse(response, (int)ResponseCodes.FailedToLaunch, result.Error);
            else
                SendResponse(response);

            LogFinish();
        }

        private void OnException(int exceptionCode, int threadId)
        {
            SendEvent(new StoppedEvent(threadId, "exception", $"Error code: {exceptionCode.ToString("X8")}"));
        }

        private void OnBreakpoint(Breakpoint breakpoint, int threadId)
        {
            SendEvent(new StoppedEvent(threadId, "breakpoint"));
        }

        public override void Next(Response response, dynamic arguments)
        {
            _wrapper.HandleMessageWithoutResult(new StepOverMessage());
        }

        public override void Pause(Response response, dynamic arguments)
        {
            _wrapper.HandleMessageWithoutResult(new PauseMessage());
            _wrapper.Interrupt();
        }

        public override void Scopes(Response response, dynamic arguments)
        {
            SendResponse(response, new ScopesResponseBody(new List<Scope> { new Scope("Current", 0) }));
        }

        public override void SetBreakpoints(Response response, dynamic arguments)
        {
            LogStart();

            string source = arguments.source.path;
            int[] lines = arguments.lines.ToObject<int[]>();

            _wrapper.HandleMessageWithoutResult(new SetBreakpointsMessage(lines.Select(x => new Breakpoint(source, x))));
            LogFinish();
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            var result = _wrapper.HandleMessage<StackTraceMessageResult>(new StackTraceMessage(), DefaultTimeout).Result;
            SendResponse(response, new StackTraceResponseBody(result.Frames.Select(ToStackFrame).ToList()));
        }

        private StackFrame ToStackFrame(StackTraceFrame frame)
        {
            return new StackFrame(frame.Order, $"StackFrame#{frame.Order}", new Source(frame.FilePath), frame.Line, 0);
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            _wrapper.HandleMessageWithoutResult(new StepIntoMessage());
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            _wrapper.HandleMessageWithoutResult(new StepOutMessage());
        }

        public override void Threads(Response response, dynamic arguments)
        {
            var process = Process.GetProcessById(_wrapper.ProcessId);

            List<Thread> threads = new List<Thread>();
            foreach (ProcessThread thread in process.Threads)
            {
                threads.Add(new Thread(thread.Id, $"thread #{thread.Id}"));
            }
            SendResponse(response, new ThreadsResponseBody(threads));
        }

        public override void Variables(Response response, dynamic arguments)
        {
            var result = _wrapper.HandleMessage<VariablesMessageResult>(new VariablesMessage(), DefaultTimeout).Result;
            SendResponse(response, new VariablesResponseBody(result.Variables.Select(ToVariable).ToList()));
        }

        private VSCodeDebug.Variable ToVariable(Data.Variable variable)
        {
            return new VSCodeDebug.Variable(variable.Name, variable.Value);
        }

        #endregion

        private void LogStart([CallerMemberName] string method = "<Unknown>")
        {
            _logger.Log($"{method} started.");
        }

        private void LogFinish([CallerMemberName] string method = "<Unknown>")
        {
            _logger.Log($"{method} finished.");
        }
    }
}
