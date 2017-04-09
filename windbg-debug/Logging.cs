using System;
using System.IO;
using System.Linq;
using log4net;
using log4net.Config;
using log4net.Core;

namespace WinDbgDebug
{
    public static class Logging
    {
        #region Public Fields

        public static readonly string[] PossibleLogLevels = new[] { Level.Debug.Name, Level.Error.Name, Level.Fatal.Name, Level.Info.Name, Level.Warn.Name };

        #endregion

        #region Private Fields

        private static readonly string _logConfigurationFileName = "log4net.config";
        private static readonly string _logFilePathPropertyName = "logFilePath";
        private static readonly string _clientLogLevelPropertyName = "clientLogLevel";
        private static readonly string _defaultClientLogLevel = Level.Info.Name;
        private static readonly string _applicationName = "WinDbgDebugger";

        #endregion

        #region Public Methods

        public static void Configure(string verbosity)
        {
            GlobalContext.Properties[_logFilePathPropertyName] = GenerateLogFilePath();
            GlobalContext.Properties[_clientLogLevelPropertyName] = ParseLogLevel(verbosity, _defaultClientLogLevel);
            XmlConfigurator.Configure(new FileInfo(_logConfigurationFileName));
        }

        #endregion

        #region Private Methods

        private static string ParseLogLevel(string verbosity, string defaultClientLogLevel)
        {
            var logLevel = PossibleLogLevels.FirstOrDefault(x => string.Equals(x, verbosity, StringComparison.OrdinalIgnoreCase));
            return logLevel ?? defaultClientLogLevel;
        }

        private static object GenerateLogFilePath()
        {
            var randomFile = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(appData, _applicationName, $"{randomFile}.log");
        }

        #endregion
    }
}
