using System;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class VariablesMessageResult : MessageResult
    {
        public VariablesMessageResult(Variable[] variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            Variables = variables;

        }

        public Variable[] Variables { get; private set; }
    }
}
