using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinDbgDebug.WinDbg.Messages
{
    public class SetVariableValueMessage : Message
    {
        public SetVariableValueMessage(string variable, string value, int scope)
        {
            Variable = variable;
            Value = value;
            Scope = scope;
        }

        public string Variable { get; private set; }
        public string Value { get; private set; }
        public int Scope { get; private set; }
    }
}
