using System;
using System.Collections.Generic;
using System.Linq;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class ThreadsMessageResult : MessageResult
    {
        #region Constructor

        public ThreadsMessageResult(IEnumerable<DebuggeeThread> threads)
        {
            if (threads == null)
                throw new ArgumentNullException(nameof(threads));

            Threads = threads.ToArray();
        }

        #endregion

        #region Public Properties

        public IEnumerable<DebuggeeThread> Threads { get; private set; }

        #endregion
    }
}
