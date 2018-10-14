using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinDbgDebug.WinDbg.Results
{
    public class SetVariableValueMessageResult : MessageResult
    {
        public SetVariableValueMessageResult(string value = null)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}
