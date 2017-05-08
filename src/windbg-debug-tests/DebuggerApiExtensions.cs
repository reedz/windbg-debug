using System.Collections.Generic;
using System.Linq;
using WinDbgDebug.WinDbg;
using WinDbgDebug.WinDbg.Data;

namespace windbg_debug_tests
{
    internal static class DebuggerApiExtensions
    {
        private static readonly int MaxLevel = 5;

        public static VariableTree GetAllLocals(this DebuggerApi api)
        {
            var mainThread = api.GetCurrentThreads().First();
            var frame = api.GetCurrentStackTrace(mainThread.Id).First();
            var scopes = api.GetCurrentScopes(frame.Id);

            return new VariableTree
            {
                CurrentItem = new Variable(-1, "Root", string.Empty, "Root", true),
                Children = scopes.Select(x => GetVariables(api, x)).ToList()
            };
        }

        private static VariableTree GetVariables(DebuggerApi api, Scope scope)
        {
            var variables = api.GetCurrentVariables(scope.Id);
            return new VariableTree
            {
                CurrentItem = new Variable(scope.Id, scope.Name, string.Empty, string.Empty, true),
                Children = variables.Select(x => GetVariables(api, x, 0)).ToList()
            };
        }

        private static VariableTree GetVariables(DebuggerApi api, Variable variable, int level)
        {
            var children = variable.HasChildren && level <= MaxLevel ? api.GetCurrentVariables(variable.Id) : new List<Variable>();
            return new VariableTree
            {
                CurrentItem = variable,
                Children = children.Select(x => GetVariables(api, x, level + 1)).ToList()
            };
        }
    }
}
