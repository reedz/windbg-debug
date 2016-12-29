using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windbg_debug.WinDbg
{
    public class InputCallbacks : IDebugInputCallbacks
    {
        public int EndInput()
        {
            return 0;
        }

        public int StartInput(uint BufferSize)
        {
            return 0;
        }
    }
}
