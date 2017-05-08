using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg.Data
{
    public static class Scopes
    {
        private static readonly string[] _names = new[] { "Locals", "Arguments" };
        private static readonly Dictionary<string, DEBUG_SCOPE_GROUP> _scopes = new Dictionary<string, DEBUG_SCOPE_GROUP>(StringComparer.OrdinalIgnoreCase)
        {
            { "Locals", DEBUG_SCOPE_GROUP.LOCALS },
            { "Arguments", DEBUG_SCOPE_GROUP.ARGUMENTS },
        };

        public static IEnumerable<string> GetNames()
        {
            return _scopes.Keys;
        }

        public static DEBUG_SCOPE_GROUP GetScopeByName(string name)
        {
            DEBUG_SCOPE_GROUP result;
            if (_scopes.TryGetValue(name, out result))
                return result;

            throw new Exception($"Unknown scope name: '{name}'.");
        }
    }
}
