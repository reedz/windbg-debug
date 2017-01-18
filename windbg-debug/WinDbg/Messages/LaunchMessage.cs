using System.IO;

namespace WinDbgDebug.WinDbg.Messages
{
    public class LaunchMessage : Message
    {
        #region Constructor

        public LaunchMessage(string path, string arguments)
        {
            FullPath = Path.GetFullPath(path);
            Arguments = arguments;
        }

        #endregion

        #region Public Properties

        public string FullPath { get; private set; }
        public string Arguments { get; private set; }

        #endregion
    }
}
