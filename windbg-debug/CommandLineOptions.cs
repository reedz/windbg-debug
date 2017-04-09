using CommandLine;
using CommandLine.Text;

namespace WinDbgDebug
{
    public class CommandLineOptions
    {
        #region Constants

        private const string VerbosityHelpTextPlaceholder = "%%verbosity_help_text%%";

        #endregion

        #region Public Properties

        [Option('q', "trace-requests", Required = false, HelpText = "Set to trace requests.")]
        public bool TraceRequests { get; set; }

        [Option('s', "trace-responses", Required = false, HelpText = "Set to trace responses.")]
        public bool TraceResponses { get; set; }

        [Option('p', "port", Required = false, DefaultValue = -1, HelpText = "Server port.")]
        public int Port { get; set; }

        [Option('v', "verbosity", Required = false, DefaultValue = "INFO", HelpText = VerbosityHelpTextPlaceholder)]
        public string Verbosity { get; set; }

        [Option('c', "currentDirectory", Required = false, DefaultValue = "", HelpText = "Set working directory.")]
        public string CurrentDirectory { get; set; }

        #endregion

        #region Public Methods

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this).ToString()
                .Replace(VerbosityHelpTextPlaceholder, $"Print details during execution. Possible values: {string.Join(",", Logging.PossibleLogLevels)}");
        }

        #endregion
    }
}
