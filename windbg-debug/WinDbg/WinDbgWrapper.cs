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
    public delegate void BreakpointHitHandler(Breakpoint breakpoint, int threadId);
    public delegate void ExceptionHitHandler(int exceptionCode, int threadId);

    public class WinDbgWrapper : IDisposable
    {
        private class MessageRecord
        {
            public MessageRecord(Message message, Action<MessageResult> resultSetter)
            {
                if (message == null)
                    throw new ArgumentNullException(nameof(message));

                Message = message;
                ResultSetter = resultSetter;
            }

            public Message Message { get; private set; }
            public Action<MessageResult> ResultSetter { get; private set; }
        }

        private const DEBUG_CREATE_PROCESS DEBUG = (DEBUG_CREATE_PROCESS)DEBUG_PROCESS.DETACH_ON_EXIT;
        private const int CodeOk = 0;
        private const int Current = 0;
        private static readonly int DefaultBufferSize = 1024;
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly Dictionary<uint, Breakpoint> _breakpoints = new Dictionary<uint, Breakpoint>();
        private int _lastBreakpointId = 1;

        [ThreadStatic]
        private IDebugClient6 _debugger;
        [ThreadStatic]
        private IDebugSymbols5 _symbols;
        [ThreadStatic]
        private EventCallbacks _callbacks;
        [ThreadStatic]
        private IDebugAdvanced3 _advanced;
        // not threadstatic to allow interrupting
        private IDebugControl6 _control;
        [ThreadStatic]
        private IDebugSystemObjects3 _systemObjects;

        private readonly Thread _debuggerThread;
        private readonly BlockingCollection<MessageRecord> _messages = new BlockingCollection<MessageRecord>();
        public event BreakpointHitHandler BreakpointHit;
        public event ExceptionHitHandler ExceptionHit;
        public int ProcessId { get; internal set; }

        public WinDbgWrapper(string enginePath)
        {
            if (!string.IsNullOrWhiteSpace(enginePath))
                NativeMethods.SetDllDirectory(Path.GetDirectoryName(enginePath));

            _debuggerThread = new Thread(MainLoop);
            _debuggerThread.Start(_cancel.Token);
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
            HandleMessage<MessageResult>(message);
        }

        private void OnBreakpoint(object sender, IDebugBreakpoint e)
        {
            var breakpoint = GetBreakpoint(e);
            uint threadId;
            var hr = _systemObjects.GetCurrentThreadSystemId(out threadId);
            if (breakpoint != null)
                BreakpointHit?.Invoke(breakpoint, (int)threadId);
        }

        private Breakpoint GetBreakpoint(IDebugBreakpoint e)
        {
            uint breakpointId;
            e.GetId(out breakpointId);

            Breakpoint result;
            _breakpoints.TryGetValue(breakpointId, out result);

            return result;
        }

        internal void Interrupt()
        {
            _control.SetInterrupt(DEBUG_INTERRUPT.EXIT);
        }

        private void DoEndSession()
        {
            _cancel.Cancel();
        }

        private LaunchMessageResult DoLaunch(LaunchMessage message)
        {
            int hr = _debugger.CreateProcessAndAttachWide(
                0,
                $"{message.FullPath} {message.Arguments}",
                (DEBUG_CREATE_PROCESS)DEBUG_PROCESS.ONLY_THIS_PROCESS,
                0,
                DEBUG_ATTACH.DEFAULT);

            if (hr != CodeOk)
                return new LaunchMessageResult($"Error creating process: {hr.ToString("X8")}");

            hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
            if (hr != CodeOk)
                return new LaunchMessageResult($"Error attaching debugger: {hr.ToString("X8")}");

            ReadCreatedProcessId(message.FullPath);
            hr = ForceLoadSymbols(message.FullPath);
            if (hr != CodeOk)
                return new LaunchMessageResult($"Error loading debug symbols: {hr.ToString("X8")}");

            return new LaunchMessageResult();
        }

        private void ReadCreatedProcessId(string fullPath)
        {
            uint processId;
            var result = _debugger.GetRunningProcessSystemIdByExecutableName(0, Path.GetFileName(fullPath), DEBUG_GET_PROC.FULL_MATCH, out processId);
            if (result == CodeOk)
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
            var name = new StringBuilder(DefaultBufferSize);
            _symbols.GetNextSymbolMatch(handle, name, DefaultBufferSize, out matchSize, out offset);
            hr = _symbols.EndSymbolMatch(handle);
            return hr;
        }

        private void DoSetBreakpoints(SetBreakpointsMessage message)
        {
            foreach (var breakpoint in message.Breakpoints)
            {
                var id = (uint)Interlocked.Increment(ref _lastBreakpointId);
                IDebugBreakpoint2 breakpointToSet;
                var result = _control.AddBreakpoint2(DEBUG_BREAKPOINT_TYPE.CODE, id, out breakpointToSet);
                if (result != CodeOk)
                    throw new Exception("!");

                ulong offset;
                result = _symbols.GetOffsetByLineWide((uint)breakpoint.Line, breakpoint.File, out offset);
                if (result != CodeOk)
                    throw new Exception("!");

                ulong[] buffer = new ulong[8000];
                uint fileLines;
                _symbols.GetSourceFileLineOffsetsWide(breakpoint.File, buffer, 8000, out fileLines);

                // TODO: Add hresult handling
                result = breakpointToSet.SetOffset(offset);
                result = breakpointToSet.SetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED | DEBUG_BREAKPOINT_FLAG.GO_ONLY);

                _breakpoints.Add(id, breakpoint);
            }
        }

        private static IDebugClient6 CreateDebuggerClient()
        {
            IDebugClient result;
            var errorCode = NativeMethods.DebugCreate(typeof(IDebugClient).GUID, out result);
            if (errorCode != CodeOk)
                throw new Exception($"Could not create debugger client. HRESULT = '{errorCode}'");

            return (IDebugClient6)result;
        }

        private void MainLoop(object state)
        {
            var token = (CancellationToken)state;
            Initialize();

            // Process initial launch message
            Thread.Sleep(500);
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
                if (hr != CodeOk)
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
                Process(message);
            }
        }

        private void Process(MessageRecord record)
        {
            var message = record.Message;
            if (message is SetBreakpointsMessage)
                DoSetBreakpoints(message as SetBreakpointsMessage);

            if (message is LaunchMessage)
                record.ResultSetter(DoLaunch(message as LaunchMessage));

            if (message is TerminateMessage)
                DoEndSession();

            if (message is StackTraceMessage)
                record.ResultSetter(DoGetStackTrace());

            if (message is VariablesMessage)
                record.ResultSetter(DoGetVariables());

            if (message is StepOverMessage)
                DoStepOver();

            if (message is StepIntoMessage)
                DoStepInto();

            if (message is StepOverMessage)
                DoStepOut();

            if (message is ContinueMessage)
                DoContinue();
        }

        private void DoContinue()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.GO_HANDLED);
        }

        private void DoStepOut()
        {
            //@ TODO
        }

        private void DoStepInto()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.STEP_INTO);
        }

        private void DoStepOver()
        {
            _control.SetExecutionStatus(DEBUG_STATUS.STEP_OVER);
        }

        private VariablesMessageResult DoGetVariables()
        {
            List<Variable> result = new List<Variable>();
            IDebugSymbolGroup2 group;
            var hr = _symbols.GetScopeSymbolGroup2(DEBUG_SCOPE_GROUP.ALL, null, out group);
            if (hr != CodeOk)
                return new VariablesMessageResult(result.ToArray());

            uint variableCount;
            hr = group.GetNumberSymbols(out variableCount);
            if (hr != CodeOk)
                return new VariablesMessageResult(result.ToArray());

            for (uint variableIndex = 0; variableIndex < variableCount; variableIndex++)
            {
                StringBuilder name = new StringBuilder(DefaultBufferSize);
                uint nameSize;

                hr = group.GetSymbolNameWide(variableIndex, name, DefaultBufferSize, out nameSize);
                if (hr != CodeOk)
                    continue;
                var variableName = name.ToString();

                hr = group.GetSymbolTypeNameWide(variableIndex, name, DefaultBufferSize, out nameSize);
                if (hr != CodeOk)
                    continue;
                var typeName = name.ToString();

                hr = group.GetSymbolValueTextWide(variableIndex, name, DefaultBufferSize, out nameSize);
                if (hr != CodeOk)
                    continue;
                var value = name.ToString();

                result.Add(new Variable(variableName, typeName, value));
            }

            return new VariablesMessageResult(result.ToArray());
        }

        private StackTraceMessageResult DoGetStackTrace()
        {
            List<StackTraceFrame> resultFrames = new List<StackTraceFrame>();
            var frames = new DEBUG_STACK_FRAME[1000];
            uint framesGot;
            var hr = _control.GetStackTrace(Current, Current, Current, frames, 1000, out framesGot);
            if (hr != CodeOk)
                return new StackTraceMessageResult(resultFrames.ToArray());

            for (int frameIndex = 0; frameIndex < framesGot; frameIndex++)
            {
                var frame = frames[frameIndex];
                uint line, filePathSize;
                ulong displacement;
                StringBuilder filePath = new StringBuilder(DefaultBufferSize);
                hr = _symbols.GetLineByOffsetWide(frame.InstructionOffset, out line, filePath, DefaultBufferSize, out filePathSize, out displacement);
                if (hr != CodeOk)
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

            _debugger.SetEventCallbacksWide(_callbacks);
            _debugger.SetOutputCallbacksWide(new OutputCallbacks(new Logger(true)));
            _debugger.SetInputCallbacks(new InputCallbacks());
        }

        private void OnException(object sender, EXCEPTION_RECORD64 e)
        {
            uint threadId;
            var hr = _systemObjects.GetCurrentThreadSystemId(out threadId);
            ExceptionHit?.Invoke((int)e.ExceptionCode, (int)threadId);
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
