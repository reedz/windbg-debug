using System;
using System.Collections.Generic;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Results
{
    public class VariablesMessageResult : MessageResult
    {
        public VariablesMessageResult(IEnumerable<Variable> variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            Variables = variables;
        }

        public IEnumerable<Variable> Variables { get; private set; }
    }
}
