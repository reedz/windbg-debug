using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustWtf8Visualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _typeName = "struct std::sys_common::wtf8::Wtf8";

        #endregion

        #region Constructor

        public RustWtf8Visualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry)
            : base(helper, symbols, registry)
        {
        }

        #endregion

        #region Public Methods

        protected override bool DoCanHandle(VariableMetaData meta)
        {
            return string.Equals(meta.TypeName, _typeName, StringComparison.OrdinalIgnoreCase);
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData descriptor)
        {
            return _empty;
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var bigString = ReadString(typedData.Offset, (uint)Defaults.MaxStringSize);
            var endIndex = bigString.IndexOf('\0');
            string actualString = endIndex == Defaults.NotFound ? $"{bigString}..." : bigString.Substring(0, endIndex);

            return new VisualizationResult(actualString, false);
        }

        #endregion
    }
}
