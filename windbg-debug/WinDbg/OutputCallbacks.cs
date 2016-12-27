using Microsoft.Diagnostics.Runtime.Interop;
using System;

namespace windbg_debug.WinDbg
{
    public class OutputCallbacks : IDebugOutputCallbacks
    {
        private const int CodeOk = 0;
        private readonly Logger _logger;

        public OutputCallbacks(Logger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;
        }

        public int Output(DEBUG_OUTPUT mask, string text)
        {
            _logger.Log($"{mask.ToString()} :: {text}");
            return CodeOk;
        }
    }
}
