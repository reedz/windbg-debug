using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WinDbgDebug.WinDbg;

namespace windbg_debug_tests
{
    [TestFixture]
    public class RustDebuggingTests
    {
        private static readonly string SourceFileName = "main.rs";
        private static readonly string PathToExecutable = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "..\\..\\test-debuggees\\rust\\target\\debug\\debugger_test.exe");
        
        private static readonly int PreExitLine = 103;

        private bool _hasExited;
        private DebuggerApi _api;
        private WinDbgWrapper _debugger;

        [SetUp]
        public void RunBeforeTests()
        {
            _debugger = new WinDbgWrapper(Const.PathToEngine);
            _api = new DebuggerApi(_debugger);
            Environment.CurrentDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\test-debuggees\\cpp\\src");
            _debugger.ProcessExited += (a, b) => _hasExited = true;
        }

        [TearDown]
        public void RunAfterTests()
        {
            if (!_hasExited)
                _api.Terminate().Wait();

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
        public void Run_WithBreakpointAtTheEnd_ShowsLocalVariables()
        {
            var breakpointHit = false;
            _debugger.BreakpointHit += (breakpoint, threadId) => breakpointHit = true;

            _api.Launch(PathToExecutable, string.Empty);
            var result = _api.SetBreakpoints(new[] { new Breakpoint(SourceFileName, PreExitLine) });
            Assert.IsTrue(result.Values.All(x => x));

            Assert.That(() => breakpointHit, Is.True.After(Const.DefaultTimeout, Const.DefaultPollingInterval));

            var locals = _api.GetAllLocals();

            var valueItem = locals.Children.First().Children.FirstOrDefault(x => x.CurrentItem.Name == "float");
            Assert.IsNotNull(valueItem);
            StringAssert.Contains("5.5", valueItem.CurrentItem.Value);
        }
    }
}
