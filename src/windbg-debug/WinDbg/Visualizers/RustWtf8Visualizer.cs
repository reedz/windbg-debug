using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustWtf8Visualizer : VisualizerBase
    {
        private static readonly string _typeName = "struct std::sys_common::wtf8::Wtf8";

        public RustWtf8Visualizer(RequestHelper helper, IDebugSymbols4 symbols)
            : base(helper, symbols)
        {
        }

        public override bool CanHandle(VariableMetaData meta)
        {
            return string.Equals(meta.TypeName, _typeName, StringComparison.OrdinalIgnoreCase);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            return Enumerable.Empty<VariableMetaData>();
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var bigString = _helper.ReadString(typedData.Offset, (uint)Defaults.MaxStringSize);
            var endIndex = bigString.IndexOf('\0');
            string actualString = endIndex == Defaults.NotFound ? $"{bigString}..." : bigString.Substring(0, endIndex);

            return new VisualizationResult(actualString, false);
        }
    }
}
