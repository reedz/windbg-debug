using System;

namespace WinDbgDebug
{
    public class VSCodeLogger
    {
        #region Fields

        private readonly bool _verbose;
        private readonly Action<string> _logger;

        #endregion

        #region Constructor

        public VSCodeLogger(bool verbose, Action<string> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _verbose = verbose;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public void Log(string text)
        {
            if (_verbose)
                _logger(text);
        }

        #endregion
    }
}
