using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSCodeDebug {
    public class SetVariableValueResponseBody : ResponseBody
    {
        public string value { get; private set; }

        public SetVariableValueResponseBody(string value)
        {
            this.value = value;
        }
    }
}
