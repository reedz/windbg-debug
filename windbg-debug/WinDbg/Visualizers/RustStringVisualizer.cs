using System;
using System.Collections.Generic;
using System.Linq;
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

        public RustStringVisualizer(RequestHelper helper, IDebugSymbols4 symbols)
            : base(helper, symbols)
        {
        }

        #endregion

        #region Public Methods

        public override bool CanHandle(VariableMetaData meta)
        {
            return string.Equals(meta.TypeName, _stringTypeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(meta.TypeName, _dynamicStringTypeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(meta.TypeName, _shortStringName, StringComparison.OrdinalIgnoreCase);
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            if (!CanHandle(meta))
                throw new ArgumentException($"Cannot handle '{meta}'.", nameof(meta));

            if (meta.TypeName.EndsWith("*"))
                return ReadPointerString(meta);

            return ReadStaticString(meta);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            return Enumerable.Empty<VariableMetaData>();
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
