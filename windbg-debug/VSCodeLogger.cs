using System;

namespace windbg_debug
{
    public class VSCodeLogger
    {
        private readonly bool _verbose;
        private readonly Action<string> _logger;

        public VSCodeLogger(bool verbose, Action<string> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            _verbose = verbose;
            _logger = logger;
        }

        public void Log(string text)
        {
            if (_verbose)
                _logger(text);
        }
    }
}
