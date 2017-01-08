using Microsoft.Diagnostics.Runtime.Interop;
using System;

namespace windbg_debug.WinDbg
{
    public class OutputCallbacks : IDebugOutputCallbacks2
    {
        #region Fields

        private readonly VSCodeLogger _logger;

        #endregion

        #region Constructor

        public OutputCallbacks(VSCodeLogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _logger = logger;
        }

        #endregion

        #region Public Methods

        public int GetInterestMask(out DEBUG_OUTCBI Mask)
        {
            Mask = DEBUG_OUTCBI.ANY_FORMAT;
            return HResult.Ok;
        }

        public int Output(DEBUG_OUTPUT mask, string text)
        {
            string message = $"{mask.ToString()} :: {text}";
            _logger.Log(message);
            return HResult.Ok;
        }

        public int Output2(DEBUG_OUTCB Which, DEBUG_OUTCBF Flags, ulong Arg, string Text)
        {
            _logger.Log($"{Which} :: {Flags} :: {Text}");

            return HResult.Ok;
        }

        #endregion
    }
}
