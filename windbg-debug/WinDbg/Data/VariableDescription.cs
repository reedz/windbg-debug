using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg.Data
{
    public class VariableMetaData
    {
        #region Constructor

        public VariableMetaData(string name, string typeName, _DEBUG_TYPED_DATA entry)
        {
            Name = name;
            TypeName = typeName;
            Entry = entry;
        }

        #endregion

        #region Public Properties

        public string Name { get; private set; }
        public string TypeName { get; private set; }
        public _DEBUG_TYPED_DATA Entry { get; private set; }

        #endregion
    }
}