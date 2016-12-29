using System;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Results
{
    public class StackTraceMessageResult : MessageResult
    {
        public StackTraceMessageResult(StackTraceFrame[] frames)
        {
            if (frames == null)
                throw new ArgumentNullException(nameof(frames));

            Frames = frames;
        }

        public StackTraceFrame[] Frames { get; private set; }
    }
}
