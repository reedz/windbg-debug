using System;
using System.Collections.Generic;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Results
{
    public class StackTraceMessageResult : MessageResult
    {
        public StackTraceMessageResult(IEnumerable<StackTraceFrame> frames)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));

            Frames = frames;
        }

        public IEnumerable<StackTraceFrame> Frames { get; private set; }
    }
}
