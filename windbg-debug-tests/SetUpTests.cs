using log4net.Core;
using NUnit.Framework;
using WinDbgDebug;

namespace windbg_debug_tests
{
    [SetUpFixture]
    public class SetUpTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            Logging.Configure(Level.Debug.Name);
        }
    }
}
