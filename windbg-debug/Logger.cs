using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windbg_debug
{
    public class Logger
    {
        private readonly bool _verbose;

        public Logger(bool verbose)
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
