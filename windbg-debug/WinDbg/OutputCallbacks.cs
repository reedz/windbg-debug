using Microsoft.Diagnostics.Runtime.Interop;
using System;

namespace windbg_debug.WinDbg
{
    public class OutputCallbacks : IDebugOutputCallbacks2
    {
        private const int CodeOk = 0;
        private readonly VSCodeLogger _logger;

        public OutputCallbacks(VSCodeLogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;
        }

        public int GetInterestMask(out DEBUG_OUTCBI Mask)
        {
            Mask = DEBUG_OUTCBI.ANY_FORMAT;
            return CodeOk;
        }

        public int Output(DEBUG_OUTPUT mask, string text)
        {
            string message = $"{mask.ToString()} :: {text}";
            _logger.Log(message);
            return CodeOk;
        }

        public int Output2(DEBUG_OUTCB Which, DEBUG_OUTCBF Flags, ulong Arg, string Text)
        {
            _logger.Log($"{Which} :: {Flags} :: {Text}");

            return CodeOk;
        }
    }
}
