using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class StringExtensions
    {
        private static readonly string EnvironmentVariableDelimiter = "%";

        private static ILog _logger = LogManager.GetLogger(nameof(StringExtensions));
        private static Dictionary<string, string> environmentVariables = ReadEnvironmentVariablesSafely();

        public static string Replace(this string text, string oldValue, string newValue, StringComparison comparisonType)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = text.IndexOf(oldValue, comparisonType);
            while (index != -1)
            {
                sb.Append(text.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = text.IndexOf(oldValue, index, comparisonType);
            }
            sb.Append(text.Substring(previousIndex));

            return sb.ToString();
        }

        public static string ReplaceEnvironmentVariables(this string path)
        {
            if (!path.Contains(EnvironmentVariableDelimiter))
                return path;

            foreach (var variableName in environmentVariables.Keys)
                path = path.Replace(variableName, environmentVariables[variableName], StringComparison.OrdinalIgnoreCase);

            return path;
        }

        private static Dictionary<string, string> ReadEnvironmentVariablesSafely()
        {
            var result = new Dictionary<string, string>();

            Include(result, ReadEnvironmentVariablesSafely(EnvironmentVariableTarget.Machine));
            Include(result, ReadEnvironmentVariablesSafely(EnvironmentVariableTarget.User));
            Include(result, ReadEnvironmentVariablesSafely(EnvironmentVariableTarget.Process));

            return result;
        }

        private static Dictionary<TKey, TValue> Include<TKey, TValue>(Dictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            foreach (var pair in keyValuePairs)
            {
                dictionary[pair.Key] = pair.Value;
            }

            return dictionary;
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadEnvironmentVariablesSafely(EnvironmentVariableTarget target)
        {
            try
            {
                List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
                var variables = Environment.GetEnvironmentVariables(target);
                foreach (var key in variables.Keys)
                {
                    if (key == null || variables[key] == null)
                        continue;

                    result.Add(new KeyValuePair<string, string>($"%{key.ToString()}%", variables[key]?.ToString()));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error reading environment variables: {ex.Message}", ex);
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }
        }
    }
}
