using System;

namespace WinDbgDebug
{
    public class InternalLogger
    {
        #region Fields

        private readonly bool _verbose;

        #endregion

        #region Public Methods

        public InternalLogger(bool verbose)
        {
            _verbose = verbose;
        }

        public void Log(string text)
        {
            if (_verbose)
                Console.Error.WriteLine(text);
        }

        #endregion
    }
}
