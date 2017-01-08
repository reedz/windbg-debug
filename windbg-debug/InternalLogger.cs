using System;
using System.IO;

namespace windbg_debug
{
    public class InternalLogger
    {
        private readonly bool _verbose;

        public InternalLogger(bool verbose)
        {
            _verbose = verbose;
        }

        public void Log(string text)
        {
            if (_verbose)
                Console.Error.WriteLine(text);
        }
    }
}
