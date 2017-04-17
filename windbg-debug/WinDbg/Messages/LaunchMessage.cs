using System.IO;

namespace WinDbgDebug.WinDbg.Messages
{
    public class LaunchMessage : Message
    {
        public LaunchMessage(string path, string arguments)
        {
            FullPath = Path.GetFullPath(path);
            Arguments = arguments;
        }

        public string FullPath { get; private set; }
        public string Arguments { get; private set; }
    }
}
