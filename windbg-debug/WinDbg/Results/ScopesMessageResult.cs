using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class ScopesMessageResult : MessageResult
    {
        public ScopesMessageResult(IEnumerable<Scope> scopes)
        {
            if (scopes == null)
                throw new ArgumentNullException(nameof(scopes));

            Scopes = scopes;
        }

        public IEnumerable<Scope> Scopes { get; private set; }
    }
}
