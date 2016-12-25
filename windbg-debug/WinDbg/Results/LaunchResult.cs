using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windbg_debug.WinDbg.Results
{
    public class LaunchResult
    {
        public LaunchResult():this(null)
        {

        }

        public LaunchResult(string error)
        {
            Error = error;
        }

        public bool Success {  get { return string.IsNullOrEmpty(Error); } }
        public string Error { get; private set; }

    }
}
