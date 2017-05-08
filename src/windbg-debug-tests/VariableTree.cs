using System.Collections.Generic;
using WinDbgDebug.WinDbg.Data;

namespace windbg_debug_tests
{
    internal class VariableTree
    {
        public Variable CurrentItem { get; set; }
        public List<VariableTree> Children { get; set; }
    }
}
