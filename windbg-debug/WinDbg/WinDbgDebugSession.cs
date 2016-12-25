using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using VSCodeDebug;

namespace windbg_debug.WinDbg
{
    public class WinDbgDebugSession : DebugSession
    {
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
            throw new NotImplementedException();
        }

        public override void Disconnect(Response response, dynamic arguments)
        {
            if (_wrapper != null)
            {
                _wrapper.EndSession();
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

            var launchResult = _wrapper.Launch(target, args);
            if (!launchResult.Success)
                SendErrorResponse(response, (int)ResponseCodes.FailedToLaunch, launchResult.Error);
            else
                SendResponse(response);

            LogFinish();
        }

        public override void Next(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Pause(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Scopes(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void SetBreakpoints(Response response, dynamic arguments)
        {
            LogStart();

            string source = arguments.source.path;
            int[] lines = arguments.lines.ToObject<int[]>();

            _wrapper.SetBreakpoints(lines.Select(x => new Breakpoint(source, x)));

            LogFinish();
        }

        public override void StackTrace(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void StepIn(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void StepOut(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Threads(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
        }

        public override void Variables(Response response, dynamic arguments)
        {
            throw new NotImplementedException();
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
