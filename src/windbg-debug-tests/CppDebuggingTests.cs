using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WinDbgDebug.WinDbg;
using WinDbgDebug.WinDbg.Data;

namespace windbg_debug_tests
{
    [TestFixture]
    public class CppDebuggingTests
    {
        private static readonly string SourceFileName = "main.cpp";
        private static readonly string PathToExecutable = Path.Combine(
            Const.TestDebuggeesFolder,
            "cpp\\x64\\Debug\\CppDebuggee.exe");

        private static readonly int PreExitLine = 33;
        private static readonly int FunctionCallLine = 13;
        private static readonly int FunctionStartLine = 9;

        private bool _hasExited;
        private DebuggerApi _api;
        private WinDbgWrapper _debugger;

        [SetUp]
        public void RunBeforeTests()
        {
            InitializeDebugger();
        }

        private void InitializeDebugger(string[] symbolsPath = null)
        {
            var options = new WinDbgOptions(Const.PathToEngine, null, symbolsPath);
            _debugger = new WinDbgWrapper(options);
            _api = new DebuggerApi(_debugger);
            Environment.CurrentDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\test-debuggees\\cpp\\src");
            _debugger.ProcessExited += (a, b) => _hasExited = true;
        }

        [TearDown]
        public void RunAfterTests()
        {
            if (!_hasExited)
                _api.Terminate().Wait(Const.DefaultTimeout);

            _debugger.Dispose();
        }

        [Test]
        public void Run_WithoutBreakPoints_Closes()
        {
            var hasExited = false;
            _debugger.ProcessExited += (a, b) => hasExited = true;

            var launchResult = _api.Launch(PathToExecutable, string.Empty);

            Assert.IsTrue(string.IsNullOrEmpty(launchResult));
            Assert.That(() => hasExited, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));
        }

        [Test]
        public void Run_WithBreakpoint_StopsAtBreakpoint()
        {
            var breakpointHit = false;
            var hasExited = false;
            _debugger.BreakpointHit += (breakpoint, threadId) => breakpointHit = true;
            _debugger.ProcessExited += (a, b) => hasExited = true;

            _api.Launch(PathToExecutable, string.Empty);
            var result = _api.SetBreakpoints(new[] { new Breakpoint(SourceFileName, PreExitLine) });
            Assert.IsTrue(result.Values.All(x => x));

            Assert.That(() => breakpointHit, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));

            _api.Continue();

            Assert.That(() => hasExited, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));
        }

        [Test]
        public void StepInto_WithFunction_GoesIntoFunction()
        {
            var breakpointHit = false;
            var hasExited = false;
            _debugger.BreakpointHit += (breakpoint, threadId) => breakpointHit = true;
            _debugger.ProcessExited += (a, b) => hasExited = true;

            _api.Launch(PathToExecutable, string.Empty);
            var result = _api.SetBreakpoints(new[] { new Breakpoint(SourceFileName, FunctionCallLine) });
            Assert.IsTrue(result.Values.All(x => x));

            Assert.That(() => breakpointHit, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));

            // @TODO: hard to identify main thread, thread switching seems to fire off / continue thread left thread execution
            var mainThread = _api.GetCurrentThreads().First();

            // Getting into method requires 3 steps, possibly due to internal code organization
            _api.StepInto().Wait();
            _api.StepInto().Wait();
            _api.StepInto().Wait();
            var combinedStackTrace = _api.GetCurrentStackTrace(mainThread.Id);

            Assert.IsTrue(combinedStackTrace.Any(x => x.Line == FunctionStartLine && x.FilePath.EndsWith(SourceFileName)));

            _api.Continue();

            Assert.That(() => hasExited, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));
        }

        [Test]
        public void Run_WithBreakpointAtTheEnd_ShowsLocalVariables()
        {
            var breakpointHit = false;
            _debugger.BreakpointHit += (breakpoint, threadId) => breakpointHit = true;

            _api.Launch(PathToExecutable, string.Empty);
            var result = _api.SetBreakpoints(new[] { new Breakpoint(SourceFileName, PreExitLine) });
            Assert.IsTrue(result.Values.All(x => x));

            Assert.That(() => breakpointHit, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));

            var locals = _api.GetAllLocals();

            var valueItem = locals.Children.First().Children.FirstOrDefault(x => x.CurrentItem.Name == "value");
            Assert.IsNotNull(valueItem);
            StringAssert.Contains("128", valueItem.CurrentItem.Value);
        }

        [Test]
        public void Run_FileHasPdbInSeparateFolder_ShouldLoadPdb()
        {
            _debugger.Dispose();

            var pdbFolder = Path.GetDirectoryName(PathToExecutable);
            var executableFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var executablePath = Path.Combine(executableFolder.FullName, Path.GetFileName(PathToExecutable));
            File.Copy(PathToExecutable, executablePath);

            InitializeDebugger(new[] { pdbFolder });

            _api.Launch(executablePath, string.Empty);

            var breakpointHit = false;
            _debugger.BreakpointHit += (breakpoint, threadId) => breakpointHit = true;

            var result = _api.SetBreakpoints(new[] { new Breakpoint(SourceFileName, PreExitLine) });
            Assert.IsTrue(result.Values.All(x => x));

            Assert.That(() => breakpointHit, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));
        }
    }
}
