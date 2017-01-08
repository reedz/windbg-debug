using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class ScopesMessageResult : MessageResult
    {
        #region Constructor

        public ScopesMessageResult(IEnumerable<Scope> scopes)
        {
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));

            Scopes = scopes;
        }

        #endregion

        #region Public Properties

        public IEnumerable<Scope> Scopes { get; private set; }

        #endregion
    }
}
