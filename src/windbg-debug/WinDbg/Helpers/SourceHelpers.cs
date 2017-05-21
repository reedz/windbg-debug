using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using log4net;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class SourceHelpers
    {
        private static readonly ILog Logger = LogManager.GetLogger(nameof(SourceHelpers));
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        public static IEnumerable<string> GetDefaultSourceLocations()
        {
            var result = new List<string>();
            result.Add(GetSafely(() => GetRustDefaultSourceLocation()));

            return result.Where(x => !string.IsNullOrEmpty(x));
        }

        private static string GetSafely(Func<string> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error reading sources location: '{ex.Message}'", ex);
                return null;
            }
        }

        private static string GetRustDefaultSourceLocation()
        {
            const string addendum = "lib\\rustlib\\src\\rust\\src";
            var startInfo = new ProcessStartInfo
            {
                Arguments = "--print sysroot",
                FileName = "rustc",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit((int)DefaultTimeout.TotalMilliseconds);
                var potentialPath = process.StandardOutput.ReadToEnd().Trim();
                if (Directory.Exists(potentialPath))
                {
                    return Path.Combine(potentialPath, addendum);
                }

                return null;
            }
        }
    }
}
