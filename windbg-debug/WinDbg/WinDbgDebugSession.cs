using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using VSCodeDebug;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Messages;
using WinDbgDebug.WinDbg.Results;
using StackFrame = VSCodeDebug.StackFrame;

namespace WinDbgDebug.WinDbg
{
    public class WinDbgDebugSession : DebugSession
    {
        #region Fields

        private readonly InternalLogger _logger;
        private WinDbgWrapper _wrapper;

        #endregion

        #region Constructor

        public WinDbgDebugSession(InternalLogger logger, bool traceRequests = false, bool traceResponses = false)
            : base(true, false)
        {
            _logger = logger;
            TRACE = traceRequests;
            TRACE_RESPONSE = traceResponses;
        }

        #endregion

        #region DebugSession implementation

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

            var result = _wrapper.HandleMessage<AttachMessageResult>(new AttachMessage(processId), Defaults.Timeout).Result;

            if (!result.Success)
                SendErrorResponse(response, ErrorCodes.FailedToAttach, result.Error);
            else
                SendResponse(response);

            LogFinish();
        }

        public override void Continue(Response response, dynamic arguments)
        {
            LogStart();

            _wrapper.HandleMessageWithoutResult(new ContinueMessage());
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

            var result = _wrapper.HandleMessage<EvaluateMessageResult>(new EvaluateMessage(expression)).Result;
            SendResponse(response, new EvaluateResponseBody(result.Value));

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

            var result = _wrapper.HandleMessage<LaunchMessageResult>(new LaunchMessage(target, args), Defaults.Timeout).Result;

            if (!result.Success)
                SendErrorResponse(response, ErrorCodes.FailedToLaunch, result.Error);
            else
                SendResponse(response);

            LogFinish();
        }

        public override void Next(Response response, dynamic arguments)
        {
            LogStart();

            _wrapper.HandleMessageWithoutResult(new StepOverMessage());
            SendResponse(response);

            LogFinish();
        }

        public override void Pause(Response response, dynamic arguments)
        {
            LogStart();

            _wrapper.HandleMessageWithoutResult(new PauseMessage());
            _wrapper.Interrupt();
            SendResponse(response);

            LogFinish();
        }

        public override void Scopes(Response response, dynamic arguments)
        {
            LogStart();

            int frameId = DynamicHelpers.To<int>(arguments.frameId);

            var result = _wrapper.HandleMessage<ScopesMessageResult>(new ScopesMessage(frameId), Defaults.Timeout).Result;
            var responseBody = new ScopesResponseBody(new List<VSCodeDebug.Scope>(result.Scopes.Select(x => new VSCodeDebug.Scope(x.Name, x.Id))));
            SendResponse(response, responseBody);

            LogFinish();
        }

        public override void SetBreakpoints(Response response, dynamic arguments)
        {
            LogStart();

            string source = arguments.source.path;
            int[] lines = arguments.lines.ToObject<int[]>();

            var result = _wrapper.HandleMessage<SetBreakpointsMessageResult>(new SetBreakpointsMessage(lines.Select(x => new Breakpoint(source, x)))).Result;
            LogFinish();

            response.SetBody(new SetBreakpointsResponseBody(result.BreakpointsSet.Select(x => new VSCodeDebug.Breakpoint(x.Value, x.Key.Line)).ToList()));

            // May terminate debugger session
            // SendResponse(response);
            LogFinish();
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            LogStart();

            int threadId = DynamicHelpers.To<int>(arguments.threadId, 0);

            var result = _wrapper.HandleMessage<StackTraceMessageResult>(new StackTraceMessage(threadId), Defaults.Timeout).Result;
            SendResponse(response, new StackTraceResponseBody(result.Frames.Select(ToStackFrame).ToList()));

            LogFinish();
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            LogStart();

            _wrapper.HandleMessageWithoutResult(new StepIntoMessage());
            SendResponse(response);

            LogFinish();
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            LogStart();

            _wrapper.HandleMessageWithoutResult(new StepOutMessage());
            SendResponse(response);

            LogFinish();
        }

        public override void Threads(Response response, dynamic arguments)
        {
            LogStart();

            var result = _wrapper.HandleMessage<ThreadsMessageResult>(new ThreadsMessage(), Defaults.Timeout).Result;
            SendResponse(response, new ThreadsResponseBody(result.Threads.Select(x => new Thread(x.Id, x.Name)).ToList()));

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

            var result = _wrapper.HandleMessage<VariablesMessageResult>(new VariablesMessage(parentId), Defaults.Timeout).Result;
            SendResponse(response, new VariablesResponseBody(result.Variables.Select(ToVariable).ToList()));

            LogFinish();
        }

        #endregion

        #region Private Methods

        private void StopDebugging()
        {
            if (_wrapper != null)
            {
                _wrapper.HandleMessageWithoutResult(new TerminateMessage());
                _wrapper.Interrupt();
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
            var logger = new VSCodeLogger(DynamicHelpers.To<bool>(arguments.verbose, true), loggerAction);
            _wrapper = new WinDbgWrapper(debuggerEnginePath, logger);
            _wrapper.BreakpointHit += OnBreakpoint;
            _wrapper.ExceptionHit += OnException;
            _wrapper.BreakHit += OnBreak;
            _wrapper.Terminated += OnTerminated;
            _wrapper.ProcessExited += OnProcessExited;
            _wrapper.ThreadFinished += OnThreadFinished;
            _wrapper.ThreadStarted += OnThreadStarted;
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
            _logger.Log($"{method} started.");
        }

        private void LogFinish([CallerMemberName] string method = "<Unknown>")
        {
            _logger.Log($"{method} finished.");
        }

        #endregion
    }
}
