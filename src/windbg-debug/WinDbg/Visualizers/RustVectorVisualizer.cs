using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustVectorVisualizer : VisualizerBase
    {
        private static readonly string _vectorTypeName = "struct collections::vec::Vec";

        public RustVectorVisualizer(RequestHelper helper, IDebugSymbols4 symbols)
            : base(helper, symbols)
        {
        }

        public override bool CanHandle(VariableMetaData meta)
        {
            return meta.TypeName.StartsWith(_vectorTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var arrayField = _helper.GetField(typedData, "buf");
            var arrayLengthField = _helper.GetField(typedData, "len");

            var actualLength = _helper.ReadLong(arrayLengthField);
            var fieldHierarchy = _helper.ReadVariable(arrayField);
            var dataPointer = fieldHierarchy.Flatten().FirstOrDefault(x => (SymTag)x.Tag == SymTag.PointerType);

            return ReadArray(dataPointer, (ulong)actualLength);
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var arrayField = _helper.GetField(typedData, "buf");
            var arrayLengthField = _helper.GetField(typedData, "len");
            long actualLength = _helper.ReadLong(arrayLengthField);

            var value = $"Vec{meta.TypeName.Substring(_vectorTypeName.Length)} [{actualLength}]";

            return new VisualizationResult(value, actualLength > 0);
        }
    }
}
