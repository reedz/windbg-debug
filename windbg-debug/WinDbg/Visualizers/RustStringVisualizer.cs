using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustStringVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _stringTypeName = "struct &str";
        private static readonly string _dynamicStringTypeName = "struct &str *";
        private static readonly string _shortStringName = "&str";

        #endregion

        #region Constructor

        public RustStringVisualizer(RequestHelper helper, IDebugSymbols4 symbols, VisualizerRegistry registry)
            : base(helper, symbols, registry)
        {
        }

        #endregion

        #region Public Methods

        protected override bool DoCanHandle(VariableMetaData meta)
        {
            return string.Equals(meta.TypeName, _stringTypeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(meta.TypeName, _dynamicStringTypeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(meta.TypeName, _shortStringName, StringComparison.OrdinalIgnoreCase);
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            if (!DoCanHandle(meta))
                throw new ArgumentException($"Cannot handle '{meta}'.", nameof(meta));

            if (meta.TypeName.EndsWith("*"))
                return ReadPointerString(meta);

            return ReadStaticString(meta);
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta)
        {
            return _empty;
        }

        #endregion

        #region Private Methods

        private static string Enquote(string actualString)
        {
            return $"\"{actualString}\"";
        }

        private VisualizationResult ReadStaticString(VariableMetaData meta)
        {
            var variableRead = _helper.ReadVariable(meta.Entry);
            var stringLength = variableRead.Fields["length"].Data.Data;
            var stringPointer = BitConverter.ToUInt64(_helper.ReadValue(variableRead.Data.Offset, variableRead.Data.Size), 0);
            var actualString = ReadString(stringPointer, (uint)stringLength);

            return new VisualizationResult(Enquote(actualString), false);
        }

        private VisualizationResult ReadPointerString(VariableMetaData meta)
        {
            var dereferenced = _helper.Dereference(meta.Entry);

            var bigString = ReadString(dereferenced.Offset, (uint)Defaults.MaxStringSize);
            var endIndex = bigString.IndexOf('\0');
            string actualString = endIndex == Defaults.NotFound ? $"{bigString}..." : bigString.Substring(0, endIndex);

            return new VisualizationResult(Enquote(actualString), false);
        }

        #endregion
    }
}
