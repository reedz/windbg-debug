using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using log4net;
using VSCodeDebug;
using WinDbgDebug.WinDbg.Data;
using StackFrame = VSCodeDebug.StackFrame;

namespace WinDbgDebug.WinDbg
{
    public class WinDbgDebugSession : DebugSession
    {
        private readonly ILog _logger = LogManager.GetLogger(nameof(WinDbgDebugSession));
        private WinDbgWrapper _wrapper;
        private DebuggerApi _api;

        public WinDbgDebugSession(bool traceRequests = false, bool traceResponses = false)
            : base(true, false)
        {
            TRACE = traceRequests;
            TRACE_RESPONSE = traceResponses;
        }

        public override void Attach(Response response, dynamic arguments)
        {
            LogStart();

            string workingDir = arguments.workingDir;
            if (!string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir))
                Environment.CurrentDirectory = workingDir;

            string target = arguments.target;
            int processId = 0;
            if (string.IsNullOrWhiteSpace(target) || !int.TryParse(target, out processId) || Process.GetProcessById(processId) == null)
            {
                SendErrorResponse(response, ErrorCodes.TargetDoesNotExist, $"Could not attach to '{target}' as it does not exist.");
            }

            string debuggerEnginePath = arguments.windbgpath;
            InitializeDebugger(arguments, debuggerEnginePath);

            var result = _api.Attach(processId);

            if (!string.IsNullOrEmpty(result))
                SendErrorResponse(response, ErrorCodes.FailedToAttach, result);
            else
                SendResponse(response);

            LogFinish();
        }

        public override void Continue(Response response, dynamic arguments)
        {
            LogStart();

            _api.Continue();
            SendResponse(response);

            LogFinish();
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            LogStart();

            StopDebugging();
            SendResponse(response);

            LogFinish();
        }

        public override void Evaluate(Response response, dynamic arguments)
        {
            LogStart();

            string expression = arguments.expression;

            var result = _api.Evaluate(expression);
            SendResponse(response, new EvaluateResponseBody(result));

            LogFinish();
        }

        public override void Initialize(Response response, dynamic args)
        {
            LogStart();

            OperatingSystem os = Environment.OSVersion;
            if (os.Platform != PlatformID.Win32NT)
            {
                SendErrorResponse(response, ErrorCodes.PlatformNotSupported, $"WinDbg is not supported on '{os.Platform.ToString()}' platform.");
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
                SendErrorResponse(response, ErrorCodes.TargetDoesNotExist, $"Could not launch '{target}' as it does not exist.");
            }

            string args = arguments.args;
            string debuggerEnginePath = arguments.windbgpath;
            InitializeDebugger(arguments, debuggerEnginePath);

            var result = _api.Launch(target, args);

            if (!string.IsNullOrEmpty(result))
                SendErrorResponse(response, ErrorCodes.FailedToLaunch, result);
            else
                SendResponse(response);

            LogFinish();
        }

        public override void Next(Response response, dynamic arguments)
        {
            LogStart();

            _api.StepOver();
            SendResponse(response);

            LogFinish();
        }

        public override void Pause(Response response, dynamic arguments)
        {
            LogStart();

            _api.Pause();
            SendResponse(response);

            LogFinish();
        }

        public override void Scopes(Response response, dynamic arguments)
        {
            LogStart();

            int frameId = DynamicHelpers.To<int>(arguments.frameId);

            var result = _api.GetCurrentScopes(frameId);
            var responseBody = new ScopesResponseBody(new List<VSCodeDebug.Scope>(result.Select(x => new VSCodeDebug.Scope(x.Name, x.Id))));
            SendResponse(response, responseBody);

            LogFinish();
        }

        public override void SetBreakpoints(Response response, dynamic arguments)
        {
            LogStart();

            string source = arguments.source.path;
            int[] lines = arguments.lines.ToObject<int[]>();

            var result = _api.SetBreakpoints(lines.Select(x => new Breakpoint(source, x)));
            LogFinish();

            response.SetBody(new SetBreakpointsResponseBody(result.Select(x => new VSCodeDebug.Breakpoint(x.Value, x.Key.Line)).ToList()));

            // May terminate debugger session
            // SendResponse(response);
            LogFinish();
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            LogStart();

            int threadId = DynamicHelpers.To<int>(arguments.threadId, 0);

            var result = _api.GetCurrentStackTrace(threadId);
            SendResponse(response, new StackTraceResponseBody(result.Select(ToStackFrame).ToList()));

            LogFinish();
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            LogStart();

            _api.StepInto();
            SendResponse(response);

            LogFinish();
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            LogStart();

            _api.StepOut();
            SendResponse(response);

            LogFinish();
        }

        public override void Threads(Response response, dynamic arguments)
        {
            LogStart();

            var result = _api.GetCurrentThreads();
            SendResponse(response, new ThreadsResponseBody(result.Select(x => new Thread(x.Id, x.Name)).ToList()));

            LogFinish();
        }

        public override void Variables(Response response, dynamic arguments)
        {
            LogStart();

            int parentId = DynamicHelpers.To<int>(arguments.variablesReference, -1);
            if (parentId == -1)
            {
                SendErrorResponse(response, ErrorCodes.MissingVariablesReference, "variables: property 'variablesReference' is missing", null, false, true);
                return;
            }

            var result = _api.GetCurrentVariables(parentId);
            SendResponse(response, new VariablesResponseBody(result.Select(ToVariable).ToList()));

            LogFinish();
        }

        private void StopDebugging()
        {
            if (_wrapper != null)
            {
                _api.Terminate();
            }
        }

        private StackFrame ToStackFrame(StackTraceFrame frame)
        {
            return new StackFrame(frame.Id, $"StackFrame#{frame.Order}", new Source(frame.FilePath), frame.Line, 0);
        }

        private VSCodeDebug.Variable ToVariable(Data.Variable variable)
        {
            return new VSCodeDebug.Variable(variable.Name, variable.Value, variable.HasChildren ? variable.Id : Defaults.NoChildren);
        }

        private void InitializeDebugger(dynamic arguments, string debuggerEnginePath)
        {
            Action<string> loggerAction = (text) => SendEvent(new OutputEvent("stdout", text));
            _wrapper = new WinDbgWrapper(debuggerEnginePath);
            _wrapper.BreakpointHit += OnBreakpoint;
            _wrapper.ExceptionHit += OnException;
            _wrapper.BreakHit += OnBreak;
            _wrapper.Terminated += OnTerminated;
            _wrapper.ProcessExited += OnProcessExited;
            _wrapper.ThreadFinished += OnThreadFinished;
            _wrapper.ThreadStarted += OnThreadStarted;

            _api = new DebuggerApi(_wrapper, Defaults.Timeout);
        }

        private void OnThreadStarted(object sender, int threadId)
        {
            SendEvent(new ThreadEvent("started", threadId));
        }

        private void OnThreadFinished(object sender, int threadId)
        {
            SendEvent(new ThreadEvent("exited", threadId));
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            StopDebugging();
        }

        private void OnTerminated(object sender, EventArgs e)
        {
            _wrapper.BreakHit -= OnBreak;
            _wrapper.BreakpointHit -= OnBreakpoint;
            _wrapper.ExceptionHit -= OnException;
            _wrapper.ProcessExited -= OnProcessExited;
            _wrapper.Terminated -= OnTerminated;
            _wrapper.ThreadFinished -= OnThreadFinished;
            _wrapper.ThreadStarted -= OnThreadStarted;
            _wrapper.Dispose();

            _wrapper = null;
            _api = null;

            SendEvent(new TerminatedEvent());
        }

        private void OnBreak(int threadId)
        {
            SendEvent(new StoppedEvent(threadId, "break"));
        }

        private void OnException(int exceptionCode, int threadId)
        {
            SendEvent(new StoppedEvent(threadId, "exception", $"Error code: {exceptionCode.ToString("X8")}"));
        }

        private void OnBreakpoint(Breakpoint breakpoint, int threadId)
        {
            SendEvent(new StoppedEvent(threadId, "breakpoint"));
        }

        private void LogStart([CallerMemberName] string method = "<Unknown>")
        {
            _logger.Debug($"Method '{method}' started.");
        }

        private void LogFinish([CallerMemberName] string method = "<Unknown>")
        {
            _logger.Debug($"Method '{method}' finished.");
        }
    }
}
