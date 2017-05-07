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
        private static readonly string _stringTypeName = "struct &str";
        private static readonly string _dynamicStringTypeName = "string::String";
        private static readonly string _shortStringName = "&str";
        private readonly DebuggedProcessInfo _metaData;

        public RustStringVisualizer(RequestHelper helper, IDebugSymbols4 symbols, DebuggedProcessInfo metaData)
            : base(helper, symbols)
        {
            _metaData = metaData;
        }

        public override bool CanHandle(VariableMetaData meta)
        {
            return string.Equals(meta.TypeName, _stringTypeName, StringComparison.OrdinalIgnoreCase)
                || meta.TypeName.Contains(_dynamicStringTypeName)
                || string.Equals(meta.TypeName, _shortStringName, StringComparison.OrdinalIgnoreCase);
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            if (!CanHandle(meta))
                throw new ArgumentException($"Cannot handle '{meta}'.", nameof(meta));

            if (meta.TypeName.EndsWith(_dynamicStringTypeName))
                return ReadPointerString(meta);

            return ReadStaticString(meta);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            return Enumerable.Empty<VariableMetaData>();
        }

        private static string Enquote(string actualString)
        {
            return $"\"{actualString}\"";
        }

        private VisualizationResult ReadStaticString(VariableMetaData meta)
        {
            var variableRead = _helper.ReadVariable(meta.Entry);
            var stringLength = variableRead.Fields["length"].Data.Data;

            string actualString = ReadString(variableRead, stringLength);

            return new VisualizationResult(Enquote(actualString), false);
        }

        private string ReadString(TypedVariable stringContainer, ulong stringLength)
        {
            // stringContainer.Data.Size actually lies - we should use Process bitness instead.
            var pointerValue = _helper.ReadValue(stringContainer.Data.Offset, (uint)_metaData.PointerSize);
            var stringPointer = _metaData.Is64BitProcess ? BitConverter.ToUInt64(pointerValue, 0) : BitConverter.ToUInt32(pointerValue, 0);
            var actualString = _helper.ReadString(stringPointer, (uint)stringLength);
            return actualString;
        }

        private VisualizationResult ReadPointerString(VariableMetaData meta)
        {
            var variableTree = _helper.ReadVariable(meta.Entry);
            var stringLength = variableTree.Fields.First().Value.Fields["len"].Data.Data;
            var stringContainer = variableTree.Fields.First().Value.Fields["buf"];

            var actualString = ReadString(stringContainer, stringLength);

            return new VisualizationResult(Enquote(actualString), false);
        }
    }
}
