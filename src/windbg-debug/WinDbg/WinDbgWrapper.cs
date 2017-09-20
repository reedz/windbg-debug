using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;
using WinDbgDebug.WinDbg.Messages;
using WinDbgDebug.WinDbg.Results;
using WinDbgDebug.WinDbg.Visualizers;

namespace WinDbgDebug.WinDbg
{
    public sealed class WinDbgWrapper : IDisposable
    {
        private readonly CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly Dictionary<uint, Breakpoint> _breakpoints = new Dictionary<uint, Breakpoint>();
        private readonly Dictionary<Type, Func<Message, MessageResult>> _handlers = new Dictionary<Type, Func<Message, MessageResult>>();
        private readonly ILog _logger = LogManager.GetLogger(nameof(WinDbgWrapper));
        private readonly Thread _debuggerThread;
        private readonly BlockingCollection<MessageRecord> _messages = new BlockingCollection<MessageRecord>();
        private readonly object _messagesLock = new object();
        private readonly WinDbgOptions _options;

        private int _lastBreakpointId = 1;
        private bool _isDisposed;
        private bool _notAcceptingMessages;
        private List<string> _allSymbols = new List<string>();

        private IDebugControl4 _control;
        private RequestHelper _requestHelper;
        private CommandExecutor _commandExecutor;
        private IDebugClient4 _debugger;
        private IDebugSymbols4 _symbols;
        private EventCallbacks _callbacks;
        private IDebugAdvanced3 _advanced;
        private IDebugSystemObjects3 _systemObjects;
        private IDebugDataSpaces _spaces;
        private VisualizerRegistry _visualizers;
        private OutputCallbacks _output;

        public WinDbgWrapper(WinDbgOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (!string.IsNullOrWhiteSpace(options.EnginePath))
                NativeMethods.SetDllDirectory(Path.GetDirectoryName(options.EnginePath));

            _options = options;

            _debuggerThread = new Thread(MainLoop);
            _debuggerThread.Start(_cancel.Token);
            State = new DebuggerState();
        }

        ~WinDbgWrapper()
        {
            Dispose(false);
        }

        public event BreakpointHitHandler BreakpointHit;
        public event ExceptionHitHandler ExceptionHit;
        public event BreakHandler BreakHit;
        public event EventHandler Terminated;
        public event EventHandler<int> ThreadStarted;
        public event EventHandler<int> ThreadFinished;
        public event EventHandler ProcessExited;

        public DebuggerState State { get; private set; }
        public DebuggedProcessInfo ProcessInfo { get; private set; }

        public Task<TResult> HandleMessage<TResult>(Message message, TimeSpan timeout = default(TimeSpan))
            where TResult : MessageResult
        {
            lock (_messagesLock)
            {
                if (_isDisposed || _notAcceptingMessages)
                    return Task.FromResult(default(TResult));

                if (message is TerminateMessage)
                    _notAcceptingMessages = true;

                timeout = timeout == default(TimeSpan) ? Defaults.Timeout : timeout;
                TaskCompletionSource<TResult> taskSource = new TaskCompletionSource<TResult>();
                var messageRecord = new MessageRecord(message, (result) => taskSource.SetResult(result as TResult));
                _messages.Add(messageRecord);

                return taskSource.Task;
            }
        }

        public Task HandleMessageWithoutResult(Message message)
        {
            return HandleMessage<MessageResult>(message);
        }

        public void Interrupt()
        {
            _control.SetInterrupt(DEBUG_INTERRUPT.EXIT);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static IDebugClient4 CreateDebuggerClient()
        {
            IDebugClient result;
            var errorCode = NativeMethods.DebugCreate(typeof(IDebugClient).GUID, out result);
            if (errorCode != HResult.Ok)
                throw new Exception($"Could not create debugger client. HRESULT = '{errorCode}'");

            return (IDebugClient4)result;
        }

        private static bool IsChild(uint parentSymbolListIndex, DEBUG_SYMBOL_PARAMETERS[] parameters, uint variableIndex)
        {
            return parameters[variableIndex].ParentSymbol == parentSymbolListIndex;
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
            _handlers.Add(typeof(StackTraceMessage), (message) => DoGetStackTrace((StackTraceMessage)message));
            _handlers.Add(typeof(VariablesMessage), (message) => DoGetVariables((VariablesMessage)message));
            _handlers.Add(typeof(StepOverMessage), (message) => DoStepOver());
            _handlers.Add(typeof(StepIntoMessage), (message) => DoStepInto());
            _handlers.Add(typeof(StepOutMessage), (message) => DoStepOut());
            _handlers.Add(typeof(ContinueMessage), (message) => DoContinue());
            _handlers.Add(typeof(EvaluateMessage), (message) => DoEvaluate((EvaluateMessage)message));
            _handlers.Add(typeof(ThreadsMessage), (message) => DoGetThreads());
            _handlers.Add(typeof(ScopesMessage), (message) => DoGetScopes((ScopesMessage)message));
            _handlers.Add(typeof(AttachMessage), (message) => DoAttach((AttachMessage)message));
        }

        private MessageResult DoAttach(AttachMessage message)
        {
            int hr = _debugger.AttachProcess(
                Defaults.NoServer,
                (uint)message.ProcessId,
                DEBUG_ATTACH.DEFAULT);

            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error attaching to process: {hr.ToString("X8")}");

            hr = _control.WaitForEvent(DEBUG_WAIT.DEFAULT, uint.MaxValue);
            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error attaching debugger: {hr.ToString("X8")}");

            var process = Process.GetProcessById(message.ProcessId);
            if (process == null)
                return new AttachMessageResult($"Error reading process info.");

            hr = InitializeSources(_options.SourcePaths);
            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error setting source paths: {hr.ToString("X8")}");

            var moduleName = GetModuleName(out hr);
            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error reading main module name: {hr.ToString("X8")}");

            hr = InitializeSymbols(process.StartInfo.FileName, _options.SymbolPaths);
            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error setting symbol paths: {hr.ToString("X8")}");

            hr = ForceLoadSymbols(moduleName);
            if (hr != HResult.Ok)
                return new AttachMessageResult($"Error loading debug symbols: {hr.ToString("X8")}");

            SetDebugSettings();
            CreateProcessInfo((uint)message.ProcessId);
            InitializeVisualizers();

            return new AttachMessageResult();
        }

        private int InitializeSymbols(string filePath, string[] symbolPaths)
        {
            var paths = new[] { Path.GetFullPath(Path.GetDirectoryName(filePath)) }
                .Union(symbolPaths.Select(Path.GetFullPath))
                .Distinct()
                .ToArray();

            return _symbols.SetSymbolPathWide(string.Join(";", paths));
        }

        private int SetDebugSettings()
        {
            // To make sure source stepping works one line at a time.
            int hr = _control.SetCodeLevel(DEBUG_LEVEL.SOURCE);

            // Evaluate expressions c++ style.
            hr = _control.SetExpressionSyntax(DEBUG_EXPR.CPLUSPLUS);

            // Show numbers in decimal system
            hr = _control.SetRadix(10);

            return hr;
        }

        private void CreateProcessInfo(uint systemId)
        {
            ProcessInfo = new DebuggedProcessInfo
            {
                ProcessId = systemId,
                Is64BitProcess = Process.GetProcessById((int)systemId).Is64BitProcess(),
            };
        }

        private MessageResult DoEndSession()
        {
            _cancel.Cancel();
            _debugger.EndSession(DEBUG_END.ACTIVE_TERMINATE);
            Terminated?.Invoke(this, null);
            return MessageResult.Empty;
        }

        private EvaluateMessageResult DoEvaluate(EvaluateMessage message)
        {
            if (State.CurrentThread != Defaults.NoThread)
                EnsureIsCurrentThread(State.CurrentThread);

            if (State.CurrentFrame != Defaults.NoFrame)
                EnsureIsCurrentFrame(State.CurrentFrame);

            var response = _requestHelper.Evaluate(message.Expression);
            if (response.TypeId == 0)
                return new EvaluateMessageResult(string.Empty);

            return new EvaluateMessageResult(Encoding.Default.GetString(ReadValue(response)));
        }

        private ScopesMessageResult DoGetScopes(ScopesMessage message)
        {
            int frameId = message.FrameId;
            EnsureIsCurrentFrame(frameId);

            List<Scope> result = new List<Scope>();
            foreach (var name in Scopes.GetNames())
                result.Add(State.AddScope(frameId, (id) => new Scope(id, name)));

            return new ScopesMessageResult(result);
        }

        private void EnsureIsCurrentFrame(int frameId)
        {
            var thread = State.GetThreadForFrame(frameId);
            EnsureIsCurrentThread(thread.Id);

            uint actualFrameIndex;
            var hr = _symbols.GetCurrentScopeFrameIndex(out actualFrameIndex);

            var desiredFrame = State.GetFrame(frameId);
            if (desiredFrame == null || hr != HResult.Ok)
                return;

            if (actualFrameIndex == desiredFrame.Order)
                return;

            hr = _symbols.SetScopeFrameByIndex((uint)desiredFrame.Order);
            if (hr != HResult.Ok)
                throw new Exception($"Couldn't get scope for frame '{frameId}' - couldn't switch frame scope.");

            State.CurrentFrame = frameId;
        }

        private void EnsureIsCurrentScope(Scope scope)
        {
            var frame = State.GetFrameForScope(scope.Id);
            EnsureIsCurrentFrame(frame.Id);
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

            InitializeProcessInfo();

            hr = InitializeSources(_options.SourcePaths);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error setting source paths: {hr.ToString("X8")}");

            var moduleName = GetModuleName(out hr);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error reading main module name: {hr.ToString("X8")}");

            hr = InitializeSymbols(message.FullPath, _options.SymbolPaths);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error setting symbol paths: {hr.ToString("X8")}");

            hr = ForceLoadSymbols(moduleName);
            if (hr != HResult.Ok)
                return new LaunchMessageResult($"Error loading debug symbols: {hr.ToString("X8")}");

            SetDebugSettings();
            InitializeVisualizers();

            return new LaunchMessageResult();
        }

        private string GetModuleNameByFileName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }

        private void InitializeProcessInfo()
        {
            uint systemId;
            var hr = _systemObjects.GetCurrentProcessSystemId(out systemId);
            if (hr == HResult.Ok)
                CreateProcessInfo(systemId);
        }

        private int ForceLoadSymbols(string moduleName)
        {
            var hr = HResult.Ok;

            hr = _symbols.ReloadWide(moduleName);
            if (hr != HResult.Ok)
                return hr;

            ulong handle, offset;
            uint matchSize;
            hr = _symbols.StartSymbolMatch("*", out handle);
            var name = new StringBuilder(Defaults.BufferSize);
            hr = _symbols.GetNextSymbolMatch(handle, name, Defaults.BufferSize, out matchSize, out offset);
            hr = _symbols.EndSymbolMatch(handle);
            return hr;
        }

        private string GetModuleName(out int hresult)
        {
            ulong moduleBase;
            hresult = _symbols.GetModuleByIndex(0, out moduleBase);
            if (hresult != HResult.Ok)
                return string.Empty;

            StringBuilder moduleName = new StringBuilder(Defaults.BufferSize);
            uint nameSize;
            DEBUG_MODNAME nameType = DEBUG_MODNAME.IMAGE;
            hresult = _symbols.GetModuleNameString(nameType, 0, moduleBase, moduleName, (uint)Defaults.BufferSize, out nameSize);
            if (hresult != HResult.Ok)
                return string.Empty;

            return moduleName.ToString();
        }

        private MessageResult DoSetBreakpoints(SetBreakpointsMessage message)
        {
            var messageResult = new Dictionary<Breakpoint, bool>();
            foreach (var breakpoint in message.Breakpoints)
            {
                var id = (uint)Interlocked.Increment(ref _lastBreakpointId);
                IDebugBreakpoint2 breakpointToSet;
                var result = _control.AddBreakpoint2(DEBUG_BREAKPOINT_TYPE.CODE, id, out breakpointToSet);
                if (result != HResult.Ok)
                {
                    messageResult.Add(breakpoint, false);
                    continue;
                }

                ulong offset;
                result = _symbols.GetOffsetByLineWide((uint)breakpoint.Line, breakpoint.File, out offset);
                if (result != HResult.Ok)
                {
                    messageResult.Add(breakpoint, false);
                    continue;
                }

                result = breakpointToSet.SetOffset(offset);
                if (result != HResult.Ok)
                {
                    messageResult.Add(breakpoint, false);
                    continue;
                }
                result = breakpointToSet.SetFlags(DEBUG_BREAKPOINT_FLAG.ENABLED | DEBUG_BREAKPOINT_FLAG.GO_ONLY);
                if (result != HResult.Ok)
                {
                    messageResult.Add(breakpoint, false);
                    continue;
                }

                messageResult.Add(breakpoint, true);
                _breakpoints.Add(id, breakpoint);
            }

            return new SetBreakpointsMessageResult(messageResult);
        }

        private void MainLoop(object state)
        {
            var token = (CancellationToken)state;
            Initialize();

            // Process initial launch message
            Thread.Sleep(500);
            ProcessMessages();

            if (token.IsCancellationRequested)
                return;

            // Give VS Code time to set up stuff.
            Thread.Sleep(2000);
            ProcessMessages();

            if (token.IsCancellationRequested)
                return;

            int hr;
            hr = DoContinueExecution();

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

        private int DoContinueExecution()
        {
            State.Clear();
            return _control.SetExecutionStatus(DEBUG_STATUS.GO_HANDLED);
        }

        private void ProcessMessages()
        {
            while (!_isDisposed && _messages.Count > 0)
            {
                var message = _messages.Take();
                Func<Message, MessageResult> handler;
                if (_handlers.TryGetValue(message.Message.GetType(), out handler))
                    message.ResultSetter(handler(message.Message));
            }
        }

        private MessageResult DoContinue()
        {
            DoContinueExecution();

            return MessageResult.Empty;
        }

        private MessageResult DoStepOut()
        {
            _commandExecutor.StepOut();

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

        private VariablesMessageResult DoGetVariables(VariablesMessage message)
        {
            var parentId = message.ParentId;
            var scope = State.GetScope(parentId);
            if (scope == null)
                return DoGetVariablesByVariableId(parentId);
            else
                return DoGetVariablesByScope(scope);
        }

        private VariablesMessageResult DoGetVariablesByVariableId(int variableId)
        {
            var parentVariable = State.GetVariable(variableId);
            if (parentVariable == null)
                return new VariablesMessageResult(Enumerable.Empty<Variable>());

            var parentDescription = State.GetVariableDescription(variableId);
            var result = _visualizers.GetChildren(parentDescription);
            var actualResult = new List<Variable>();
            foreach (var pair in result)
            {
                var variable = State.AddVariable(variableId, (id) => new Variable(id, pair.Key.Name, pair.Key.TypeName, pair.Value.Value, pair.Value.HasChildren), pair.Key.Entry);
                actualResult.Add(variable);
            }

            return new VariablesMessageResult(actualResult);
        }

        private IEnumerable<Variable> DoGetVariablesFromSymbols(IDebugSymbolGroup2 symbols, uint startIndex, uint parentSymbolListIndex, int parentId)
        {
            List<Variable> result = new List<Variable>();
            uint variableCount;
            var hr = symbols.GetNumberSymbols(out variableCount);
            if (hr != HResult.Ok)
                return result;

            DEBUG_SYMBOL_PARAMETERS[] parameters = new DEBUG_SYMBOL_PARAMETERS[variableCount];
            hr = symbols.GetSymbolParameters(0, variableCount, parameters);
            if (hr != HResult.Ok)
                return result;

            for (uint variableIndex = startIndex; variableIndex < variableCount; variableIndex++)
            {
                if (parentSymbolListIndex != Defaults.NoParent && !IsChild(parentSymbolListIndex, parameters, variableIndex))
                    continue;

                DEBUG_SYMBOL_ENTRY entry;
                symbols.GetSymbolEntryInformation(variableIndex, out entry);

                StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
                uint nameSize;

                hr = symbols.GetSymbolNameWide(variableIndex, buffer, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var variableName = buffer.ToString();

                hr = symbols.GetSymbolTypeNameWide(variableIndex, buffer, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var typeName = buffer.ToString();

                hr = symbols.GetSymbolValueTextWide(variableIndex, buffer, Defaults.BufferSize, out nameSize);
                if (hr != HResult.Ok)
                    continue;
                var value = buffer.ToString();

                var typedData = _requestHelper.CreateTypedData(entry.ModuleBase, entry.Offset, entry.TypeId);
                VisualizationResult handledVariable;
                Variable variable;
                if (_visualizers.TryHandle(new VariableMetaData(variableName, typeName, typedData), out handledVariable))
                {
                    variable = State.AddVariable(
                        parentId,
                        (id) => new Variable(id, variableName, typeName, handledVariable.Value, handledVariable.HasChildren),
                        typedData);
                }
                else
                {
                    bool hasChildren = false;
                    if (parameters.Length > variableIndex)
                        hasChildren = parameters[variableIndex].SubElements > 0;

                    variable = State.AddVariable(parentId, (id) => new Variable(id, variableName, typeName, value, hasChildren), typedData);
                }
                result.Add(variable);
            }

            return result;
        }

        private byte[] ReadValue(_DEBUG_TYPED_DATA typedData)
        {
            var valueBuffer = new byte[typedData.Size];
            uint bytesRead;
            _spaces.ReadVirtual(typedData.Offset, valueBuffer, typedData.Size, out bytesRead);
            var valueTrimmed = new byte[bytesRead];
            Array.Copy(valueBuffer, valueTrimmed, bytesRead);

            return valueTrimmed;
        }

        private VariablesMessageResult DoGetVariablesByScope(Scope scope)
        {
            EnsureIsCurrentScope(scope);
            IDebugSymbolGroup2 group;
            IDebugSymbolGroup2 oldGroup = State.GetSymbolsForScope(scope.Id);
            uint oldItemsCount = 0;
            if (oldGroup != null)
                oldGroup.GetNumberSymbols(out oldItemsCount);

            var hr = _symbols.GetScopeSymbolGroup2(Scopes.GetScopeByName(scope.Name), oldGroup, out group);
            if (hr != HResult.Ok)
                return new VariablesMessageResult(Enumerable.Empty<Variable>());
            State.UpdateSymbolGroup(scope.Id, group);
            uint newItemsCount = 0;
            group.GetNumberSymbols(out newItemsCount);

            if (oldItemsCount == newItemsCount)
                return new VariablesMessageResult(State.GetVariablesByScope(scope.Id));

            var result = DoGetVariablesFromSymbols(group, 0, Defaults.NoParent, scope.Id);
            return new VariablesMessageResult(result);
        }

        private StackTraceMessageResult DoGetStackTrace(StackTraceMessage message)
        {
            int threadId = message.ThreadId;
            EnsureIsCurrentThread(threadId);
            List<StackTraceFrame> resultFrames = new List<StackTraceFrame>();
            var frames = new DEBUG_STACK_FRAME[Defaults.MaxFrames];
            uint framesGot;
            var hr = _control.GetStackTrace(Defaults.CurrentOffset, Defaults.CurrentOffset, Defaults.CurrentOffset, frames, Defaults.MaxFrames, out framesGot);
            if (hr != HResult.Ok)
                return new StackTraceMessageResult(resultFrames);

            for (int frameIndex = 0; frameIndex < framesGot; frameIndex++)
            {
                var frame = frames[frameIndex];
                uint line, filePathSize;
                ulong displacement;
                StringBuilder filePath = new StringBuilder(Defaults.BufferSize);
                hr = _symbols.GetLineByOffsetWide(frame.InstructionOffset, out line, filePath, filePath.Capacity, out filePathSize, out displacement);
                if (hr != HResult.Ok)
                    continue;

                var oldFilePath = filePath.ToString();
                uint foundElement;
                hr = _symbols.FindSourceFileWide(0, oldFilePath, DEBUG_FIND_SOURCE.BEST_MATCH | DEBUG_FIND_SOURCE.FULL_PATH, out foundElement, filePath, filePath.Capacity, out filePathSize);

                var indexedFrame = State.AddFrame(
                    threadId,
                    (id) => new StackTraceFrame(id, frame.InstructionOffset, (int)line, hr == HResult.Ok ? filePath.ToString() : oldFilePath, (int)frame.FrameNumber));
                resultFrames.Add(indexedFrame);
            }

            return new StackTraceMessageResult(resultFrames);
        }

        private void EnsureIsCurrentThread(int threadId)
        {
            uint actualCurrentId;
            var hr = _systemObjects.GetCurrentThreadSystemId(out actualCurrentId);
            if (hr != HResult.Ok)
                return;

            if (actualCurrentId == threadId)
                return;

            uint desiredEngineId;
            hr = _systemObjects.GetThreadIdBySystemId((uint)threadId, out desiredEngineId);
            if (hr != HResult.Ok)
                throw new Exception($"Could not get desired thread ('{threadId}') - error code '{hr.ToString("X8")}'.");

            hr = _systemObjects.SetCurrentThreadId(desiredEngineId);
            if (hr != HResult.Ok)
                throw new Exception($"Could not set desired thread ('{threadId}') - error code '{hr.ToString("X8")}'.");
            State.CurrentThread = threadId;
        }

        private ThreadsMessageResult DoGetThreads()
        {
            uint threadCount;
            var hr = _systemObjects.GetNumberThreads(out threadCount);
            if (hr != HResult.Ok)
                return new ThreadsMessageResult(Enumerable.Empty<DebuggeeThread>());

            uint[] engineIds = new uint[threadCount];
            uint[] systemIds = new uint[threadCount];
            hr = _systemObjects.GetThreadIdsByIndex(0, threadCount, engineIds, systemIds);
            if (hr != HResult.Ok)
                return new ThreadsMessageResult(Enumerable.Empty<DebuggeeThread>());

            var threads = systemIds.Select(x => new DebuggeeThread((int)x, null));
            State.SetThreads(threads);

            return new ThreadsMessageResult(threads);
        }

        private void Initialize()
        {
            _debugger = CreateDebuggerClient();
            _control = _debugger as IDebugControl4;
            _symbols = _debugger as IDebugSymbols4;
            _systemObjects = _debugger as IDebugSystemObjects3;
            _advanced = _debugger as IDebugAdvanced3;
            _spaces = _debugger as IDebugDataSpaces4;

            // in case previous debugging session hasn't finished correctly
            // some leftover breakpoints may exist (even if debugging target has changed)
            _control.ClearBreakpoints();
            _requestHelper = new RequestHelper(_advanced, _spaces, _symbols);
            _commandExecutor = new CommandExecutor(_control);
            _output = new OutputCallbacks();

            _callbacks = new EventCallbacks(_control);
            _callbacks.BreakpointHit += OnBreakpoint;
            _callbacks.ExceptionHit += OnException;
            _callbacks.BreakHappened += OnBreak;
            _callbacks.ThreadStarted += OnThreadStarted;
            _callbacks.ThreadFinished += OnThreadFinished;
            _callbacks.ProcessExited += OnProcessExited;

            _debugger.SetEventCallbacks(_callbacks);
            _debugger.SetOutputCallbacks(_output);
            _debugger.SetInputCallbacks(new InputCallbacks());

            _visualizers = new VisualizerRegistry(new DefaultVisualizer(_requestHelper, _symbols, _output));
            InitializeHandlers();
        }

        private int InitializeSources(string[] sourcePaths)
        {
            var sourcePathsExpanded = sourcePaths
                .Union(SourceHelpers.GetDefaultSourceLocations())
                .Select(x => x.ReplaceEnvironmentVariables());

            return _symbols.SetSourcePathWide(string.Join(";", sourcePathsExpanded));
        }

        private void InitializeVisualizers()
        {
            _visualizers.AddVisualizer(new RustStringVisualizer(_requestHelper, _symbols, ProcessInfo));
            _visualizers.AddVisualizer(new RustWtf8Visualizer(_requestHelper, _symbols));
            _visualizers.AddVisualizer(new RustVectorVisualizer(_requestHelper, _symbols));
            _visualizers.AddVisualizer(new RustSliceVisualizer(_requestHelper, _symbols));
            _visualizers.AddVisualizer(new RustEnumVisualizer(_requestHelper, _symbols, _output));
            _visualizers.AddVisualizer(new RustEncodedEnumVisualizer(_requestHelper, _symbols, _output));
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

        private void OnProcessExited(object sender, int exitCode)
        {
            ProcessExited?.Invoke(this, null);
        }

        private void OnThreadFinished(object sender, EventArgs e)
        {
            var threads = DoGetThreads().Threads;
            var previousThreads = State.GetThreads();

            var deadThreads = previousThreads.Except(threads).ToArray();
            foreach (var thread in deadThreads)
                ThreadFinished?.Invoke(this, thread.Id);
        }

        private void OnThreadStarted(object sender, EventArgs e)
        {
            var threads = DoGetThreads().Threads;
            var previousThreads = State.GetThreads();

            var newThreads = threads.Except(previousThreads).ToArray();
            foreach (var thread in newThreads)
                ThreadStarted?.Invoke(this, thread.Id);
        }

        private int GetCurrentThread()
        {
            uint threadId;
            var hr = _systemObjects.GetCurrentThreadSystemId(out threadId);
            return (int)threadId;
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _cancel.Cancel();

                    if (_debuggerThread != null)
                        _debuggerThread.Join(TimeSpan.FromMilliseconds(100));

                    if (_callbacks != null)
                    {
                        _callbacks.BreakpointHit -= OnBreakpoint;
                        _callbacks.ExceptionHit -= OnException;
                        _callbacks.BreakHappened -= OnBreak;
                        _callbacks.ThreadFinished -= OnThreadFinished;
                        _callbacks.ThreadStarted -= OnThreadStarted;
                        _callbacks.ProcessExited -= OnProcessExited;
                    }

                    if (_debugger != null)
                    {
                        _debugger.EndSession(DEBUG_END.ACTIVE_TERMINATE);
                        _debugger.SetEventCallbacks(null);
                        _debugger.SetOutputCallbacks(null);
                        _debugger.SetInputCallbacks(null);
                    }

                    _callbacks = null;
                    _messages.Dispose();
                }

                if (_debugger != null)
                {
                    while (Marshal.ReleaseComObject(_debugger) > 0)
                    {
                    }
                }

                _debugger = null;
                _control = null;
                _symbols = null;
                _spaces = null;
                _systemObjects = null;

                _advanced = null;
                _requestHelper = null;

                _isDisposed = true;
            }
        }
    }
}
