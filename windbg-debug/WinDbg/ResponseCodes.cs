using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windbg_debug.WinDbg
{
    public enum ResponseCodes
    {
        PlatformNotSupported = 1,
        TargetDoesNotExist = 2,
        FailedToLaunch = 3,
    }
}
