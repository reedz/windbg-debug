using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Core;

namespace WinDbgDebug
{
    public static class Logging
    {
        public static readonly Level[] PossibleLogLevels = new[] { Level.Debug, Level.Error, Level.Fatal, Level.Info, Level.Warn };

        private static readonly string _logConfigurationFileName = "log4net.config";
        private static readonly string _logFilePathPropertyName = "logFilePath";
        private static readonly string _clientLogLevelPropertyName = "clientLogLevel";
        private static readonly string _defaultClientLogLevel = Level.Info.Name;
        private static readonly string _applicationName = "WinDbgDebugger";

        public static Level DefaultClientLogLevel => Level.Info;

        public static void Configure(string verbosity)
        {
            var logFilePath = GenerateLogFilePath();
            GlobalContext.Properties[_logFilePathPropertyName] = logFilePath;
            SetVerbosity(verbosity);
            var configFile = ResolveConfigFilePath(_logConfigurationFileName);
            XmlConfigurator.Configure(configFile);

            var logger = LogManager.GetLogger("Initialize");
            logger.Info($"Log file location: \"{logFilePath}\"");
        }

        public static void ChangeVerbosity(string verbosity)
        {
            SetVerbosity(verbosity);
            var configFile = ResolveConfigFilePath(_logConfigurationFileName);
            XmlConfigurator.Configure(configFile);
        }

        private static void SetVerbosity(string verbosity)
        {
            GlobalContext.Properties[_clientLogLevelPropertyName] = ParseLogLevel(verbosity, DefaultClientLogLevel.Name);
        }

        private static FileInfo ResolveConfigFilePath(string logConfigurationFileName)
        {
            var executingDirectoryPath = AssemblyHelpers.ResolveAssemblyDirectory(Assembly.GetExecutingAssembly());
            return new FileInfo(Path.Combine(executingDirectoryPath, logConfigurationFileName));
        }

        private static string ParseLogLevel(string verbosity, string defaultClientLogLevel)
        {
            var logLevel = PossibleLogLevels.FirstOrDefault(x => string.Equals(x.Name, verbosity, StringComparison.OrdinalIgnoreCase));
            return logLevel.Name ?? defaultClientLogLevel;
        }

        private static object GenerateLogFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            return Path.Combine(appData, _applicationName, $"debug.log");
        }
    }
}
