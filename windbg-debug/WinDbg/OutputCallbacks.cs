using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Text;

namespace windbg_debug.WinDbg
{
    public class OutputCallbacks : IDebugOutputCallbacks2
    {
        #region Fields

        private readonly VSCodeLogger _logger;
        private readonly StringBuilder _buffer = new StringBuilder();

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
            _buffer.Append(text);
            DoOutput(text);
            return HResult.Ok;
        }

        public int Output2(DEBUG_OUTCB Which, DEBUG_OUTCBF Flags, ulong Arg, string Text)
        {
            _buffer.Append(Text);
            DoOutput(Text);

            return HResult.Ok;
        }

        #endregion

        #region Private Methods

        private void DoOutput(string text)
        {
            if (text.Contains("\n"))
            {
                string message = _buffer.ToString();
                _logger.Log(message);
                _buffer.Clear();
            }
        }

        #endregion
    }
}
