using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using windbg_debug.WinDbg.Data;
using windbg_debug.WinDbg.Messages;
using windbg_debug.WinDbg.Results;

namespace windbg_debug.WinDbg
{
    public class WinDbgWrapper : IDisposable
    {
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly Dictionary<uint, Breakpoint> _breakpoints = new Dictionary<uint, Breakpoint>();
        private readonly Dictionary<Type, Func<Message, MessageResult>> _handlers = new Dictionary<Type, Func<Message, MessageResult>>();
        private readonly VSCodeLogger _logger;
        private int _lastBreakpointId = 1;

        [ThreadStatic]
        private IDebugClient6 _debugger;
        [ThreadStatic]
        private IDebugSymbols5 _symbols;
        [ThreadStatic]
        private EventCallbacks _callbacks;
        [ThreadStatic]
        private IDebugAdvanced3 _advanced;
        // not ThreadStatic to allow interrupting
        private IDebugControl6 _control;
        [ThreadStatic]
        private IDebugSystemObjects3 _systemObjects;

        private readonly Thread _debuggerThread;
        private readonly BlockingCollection<MessageRecord> _messages = new BlockingCollection<MessageRecord>();
        public event BreakpointHitHandler BreakpointHit;
        public event ExceptionHitHandler ExceptionHit;
        public event BreakHandler BreakHit;
        public int ProcessId { get; internal set; }

        public WinDbgWrapper(string enginePath, VSCodeLogger logger)
        {
            if (!string.IsNullOrWhiteSpace(enginePath))
                NativeMethods.SetDllDirectory(Path.GetDirectoryName(enginePath));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _debuggerThread = new Thread(MainLoop);
            _debuggerThread.Start(_cancel.Token);
            _logger = logger;
        }

        public Task<TResult> HandleMessage<TResult>(Message message, TimeSpan timeout = default(TimeSpan))
            where TResult : MessageResult
        {
            timeout = timeout == default(TimeSpan) ? TimeSpan.FromMinutes(3) : timeout;
            TaskCompletionSource<TResult> taskSource = new TaskCompletionSource<TResult>();
            var messageRecord = new MessageRecord(message, (result) => taskSource.SetResult(result as TResult));
            _messages.Add(messageRecord);

            return taskSource.Task;
        }

        public void HandleMessageWithoutResult(Message message)
        {
            var task = HandleMessage<MessageResult>(message);
            // TODO: What to do with task ?
        }

        private void OnBreakpoint(object sender, IDebugBreakpoint e)
        {
            var breakpoint = GetBreakpoint(e);
            var threadId = GetCurrentThread();
            if (breakpoint != null)
                BreakpointHit?.Invoke(breakpoint, threadId);
        }

        private Breakpoint GetBreakpoint(IDebugBreakpoint e)
        {
            uint breakpointId;
            e.GetId(out breakpointId);

            Breakpoint result;
            _breakpoints.TryGetValue(breakpointId, out result);

            return result;
        }

        private void InitializeHandlers()
        {
            _handlers.Add(typeof(SetBreakpointsMessage), (message) => DoSetBreakpoints((SetBreakpointsMessage)message));
            _handlers.Add(typeof(LaunchMessage), (message) => DoLaunch((LaunchMessage)message));
            _handlers.Add(typeof(TerminateMessage), (message) => DoEndSession());
            _handlers.Add(typeof(StackTraceMessage), (message) => DoGetStackTrace());
            _handlers.Add(typeof(VariablesMessage), (message) => DoGetVariables());
            _handlers.Add(typeof(StepOverMessage), (message) => DoStepOver());
            _handlers.Add(typeof(StepIntoMessage), (message) => DoStepInto());
            _handlers.Add(typeof(StepOutMessage), (message) => DoStepOut());
            _handlers.Add(typeof(ContinueMessage), (message) => DoContinue());
            _handlers.Add(typeof(EvaluateMessage), (message) => DoEvaluate((EvaluateMessage)message));
        }

        internal void Interrupt()
        {
            _control.SetInterrupt(DEBUG_INTERRUPT.EXIT);
        }

        private MessageResult DoEndSession()
        {
            _cancel.Cancel();

            return MessageResult.Empty;
        }

        private EvaluateMessageResult DoEvaluate(EvaluateMessage message)
        {
            DEBUG_VALUE value;
            uint remainderIndex;

            var hr = _control.Evaluate(message.Expression, DEBUG_VALUE_TYPE.INVALID, out value, out remainderIndex);

            return new EvaluateMessageResult(string.Empty);
        }

        private LaunchMessageResult DoLaunch(LaunchMessage message)
        {
            int hr = _debugger.CreateProcessAndAttachWide(
                Defaults.NoServer,
                $"{message.FullPath} {message.Arguments}",
                Defaults.DEBUG,
                Defaults.NoProcess,
                DEBUG_ATTACH.DEFAULT);

            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error creating process: {hr.ToString("X8")}");

            hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error attaching debugger: {hr.ToString("X8")}");

            ReadCreatedProcessId(message.FullPath);
            hr = ForceLoadSymbols(message.FullPath);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error loading debug symbols: {hr.ToString("X8")}");

            // To make sure source stepping works one line at a time.
            hr = _control.SetCodeLevel(DEBUG_LEVEL.SOURCE);

            return new LaunchMessageResult();
        }

        private void ReadCreatedProcessId(string fullPath)
        {
            uint processId;
            var result = _debugger.GetRunningProcessSystemIdByExecutableName(0, Path.GetFileName(fullPath), DEBUG_GET_PROC.FULL_MATCH, out processId);
            if (result == HResult.Ok)
            {
                ProcessId = (int)processId;
            }
        }

        private int ForceLoadSymbols(string fullPath)
        {
            int hr = _symbols.SetSymbolPathWide(Path.GetDirectoryName(fullPath));
            hr = _symbols.SetSourcePathWide(Environment.CurrentDirectory);
            hr = _symbols.Reload(Path.GetFileNameWithoutExtension(fullPath));
            ulong handle, offset;
            uint matchSize;
            hr = _symbols.StartSymbolMatch("*", out handle);
            var name = new StringBuilder(Defaults.BufferSize);
            _symbols.GetNextSymbolMatch(handle, name, Defaults.BufferSize, out matchSize, out offset);
            hr = _symbols.EndSymbolMatch(handle);
            return hr;
        }

        private MessageResult DoSetBreakpoints(SetBreakpointsMessage message)
        {
            foreach (var breakpoint in message.Breakpoints)
            {
                var id = (uint)Interlocked.Increment(ref _lastBreakpointId);
                IDebugBreakpoint2 breakpointToSet;
                var result = _control.AddBreakpoint2(DEBUG_BREAKPOINT_TYPE.CODE, id, out breakpointToSet);
                if (result != HResult.Ok)
                    throw new Exception("!");

                ulong offset;
                result = _symbols.GetOffsetByLineWide((uint)breakpoint.Line, breakpoint.File, out offset);
                if (result != HResult.Ok)
                    throw new Exception("!");

                ulong[] buffer = new ulong[8000];
                uint fileLines;
                _symbols.GetSourceFileLineOffsetsWide(breakpoint.File, buffer, 8000, out fileLines);

                // TODO: Add hresult handling
                result = breakpointToSet.SetOffset(offset);
                result = breakpointToSet.SetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED | DEBUG_BREAKPOINT_FLAG.GO_ONLY);

                _breakpoints.Add(id, breakpoint);
            }

            return MessageResult.Empty;
        }

        private static IDebugClient6 CreateDebuggerClient()
        {
            IDebugClient result;
            var errorCode = NativeMethods.DebugCreate(typeof(IDebugClient).GUID, out result);
            if (errorCode != HResult.Ok)
                throw new Exception($"Could not create debugger client. HRESULT = '{errorCode}'");

            return (IDebugClient6)result;
        }

        private void MainLoop(object state)
        {
            var token = (CancellationToken)state;
            Initialize();

            // Process initial launch message
            ProcessMessages();

            // Give VS Code time to set up stuff.
            Thread.Sleep(2000);
            ProcessMessages();

            int hr;
            hr = _control.SetExecutionStatus(DEBUG_STATUS.GO_HANDLED);

            while (!token.IsCancellationRequested)
            {
                DEBUG_STATUS status;
                hr = _control.GetExecutionStatus(out status);
                if (hr != HResult.Ok)
                    break;

                if (status == DEBUG_STATUS.NO_DEBUGGEE)
                    break;

                if (status == DEBUG_STATUS.GO || status == DEBUG_STATUS.STEP_BRANCH
                    || status == DEBUG_STATUS.STEP_INTO || status == DEBUG_STATUS.STEP_OVER)
                {
                    hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
                    continue;
                }

                ProcessMessages();
            }

            ProcessMessages();
        }

        private void ProcessMessages()
        {
            while (_messages.Count > 0)
            {
                var message = _messages.Take();
                Func<Message, MessageResult> handler;
                if (_handlers.TryGetValue(message.Message.GetType(), out handler))
                    message.ResultSetter(handler(message.Message));
            }
        }

        private MessageResult DoContinue()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.GO_HANDLED);

            return MessageResult.Empty;
        }

        private MessageResult DoStepOut()
        {
            //@ TODO

            return MessageResult.Empty;
        }

        private MessageResult DoStepInto()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.STEP_INTO);

            return MessageResult.Empty;
        }

        private MessageResult DoStepOver()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.STEP_OVER);

            return MessageResult.Empty;
        }

        private VariablesMessageResult DoGetVariables()
        {
            List<Variable> result = new List<Variable>();
            IDebugSymbolGroup2 group;
            var hr = _symbols.GetScopeSymbolGroup2(DEBUG_SCOPE_GROUP.ALL, null, out group);
            if (hr != HResult.Ok)
                return new VariablesMessageResult(result.ToArray());

            uint variableCount;
            hr = group.GetNumberSymbols(out variableCount);
            if (hr != HResult.Ok)
                return new VariablesMessageResult(result.ToArray());

            for (uint variableIndex = 0; variableIndex < variableCount; variableIndex++)
            {
                StringBuilder name = new StringBuilder(Defaults.BufferSize);
                uint nameSize;

                hr = group.GetSymbolNameWide(variableIndex, name, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var variableName = name.ToString();

                hr = group.GetSymbolTypeNameWide(variableIndex, name, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var typeName = name.ToString();

                hr = group.GetSymbolValueTextWide(variableIndex, name, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var value = name.ToString();

                DEBUG_SYMBOL_PARAMETERS[] parameters = new DEBUG_SYMBOL_PARAMETERS[1];
                hr = group.GetSymbolParameters(variableIndex, 1, parameters);

                result.Add(new Variable(variableName, typeName, value));
            }

            return new VariablesMessageResult(result.ToArray());
        }

        private StackTraceMessageResult DoGetStackTrace()
        {
            List<StackTraceFrame> resultFrames = new List<StackTraceFrame>();
            var frames = new DEBUG_STACK_FRAME[1000];
            uint framesGot;
            var hr = _control.GetStackTrace(Defaults.CurrentOffset, Defaults.CurrentOffset, Defaults.CurrentOffset, frames, 1000, out framesGot);
            if (hr != HResult.Ok)
                return new StackTraceMessageResult(resultFrames.ToArray());

            for (int frameIndex = 0; frameIndex < framesGot; frameIndex++)
            {
                var frame = frames[frameIndex];
                uint line, filePathSize;
                ulong displacement;
                StringBuilder filePath = new StringBuilder(Defaults.BufferSize);
                hr = _symbols.GetLineByOffsetWide(frame.InstructionOffset, out line, filePath, Defaults.BufferSize, out filePathSize, out displacement);
                if (hr != HResult.Ok)
                    continue;

                resultFrames.Add(new StackTraceFrame(frame.InstructionOffset, (int)line, filePath.ToString(), (int)frame.FrameNumber));
            }

            return new StackTraceMessageResult(resultFrames.ToArray());
        }

        private void Initialize()
        {
            _debugger = CreateDebuggerClient();
            _control = _debugger as IDebugControl6;
            _symbols = _debugger as IDebugSymbols5;
            _systemObjects = _debugger as IDebugSystemObjects3;

            _callbacks = new EventCallbacks(_control);
            _callbacks.BreakpointHit += OnBreakpoint;
            _callbacks.ExceptionHit += OnException;
            _callbacks.BreakHappened += OnBreak;

            _debugger.SetEventCallbacksWide(_callbacks);
            _debugger.SetOutputCallbacksWide(new OutputCallbacks(_logger));
            _debugger.SetInputCallbacks(new InputCallbacks());

            InitializeHandlers();
        }

        private void OnBreak(object sender, EventArgs e)
        {
            var threadId = GetCurrentThread();
            BreakHit?.Invoke(threadId);
        }

        private void OnException(object sender, EXCEPTION_RECORD64 e)
        {
            var threadId = GetCurrentThread();
            ExceptionHit?.Invoke((int)e.ExceptionCode, threadId);
        }

        private int GetCurrentThread()
        {
            uint threadId;
            var hr = _systemObjects.GetCurrentThreadSystemId(out threadId);
            return (int)threadId;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WinDbgWrapper() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
