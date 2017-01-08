using CommandLine;
using CommandLine.Text;

namespace windbg_debug
{
    public class CommandLineOptions
    {
        #region Public Properties

        [Option('q', "trace-requests", Required = false, HelpText = "Set to trace requests.")]
        public bool TraceRequests { get; set; }

        [Option('s', "trace-responses", Required  = false, HelpText = "Set to trace responses.")]
        public bool TraceResponses { get; set; }

        [Option('p', "port", Required = false, DefaultValue = -1, HelpText = "Server port.")]
        public int Port { get; set; }

        [Option('v', "verbose", Required = false, DefaultValue = true, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [Option('c', "currentDirectory", Required =false, DefaultValue = "", HelpText = "Set working directory.")]
        public string CurrentDirectory { get; set; }

        #endregion

        #region Public Methods

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this);
        }

        #endregion
    }
}
