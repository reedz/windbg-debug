using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class VariablesMessageResult : MessageResult
    {
        #region Constructor

        public VariablesMessageResult(IEnumerable<Variable> variables)
        {
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            Variables = variables;
        }

        #endregion

        #region Public Properties

        public IEnumerable<Variable> Variables { get; private set; }

        #endregion
    }
}
