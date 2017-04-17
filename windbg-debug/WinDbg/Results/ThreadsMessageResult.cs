using System;
using System.Collections.Generic;
using System.Linq;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Results
{
    public class ThreadsMessageResult : MessageResult
    {
        public ThreadsMessageResult(IEnumerable<DebuggeeThread> threads)
        {
            if (threads == null)
                throw new ArgumentNullException(nameof(threads));

            Threads = threads.ToArray();
        }

        public IEnumerable<DebuggeeThread> Threads { get; private set; }
    }
}
