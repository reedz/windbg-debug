using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustEncodedEnumVisualizer : VisualizerBase
    {
        private static readonly string _enumEncodedFieldName = "RUST$ENCODED$ENUM";
        private readonly OutputCallbacks _output;

        public RustEncodedEnumVisualizer(RequestHelper helper, IDebugSymbols4 symbols, OutputCallbacks output)
            : base(helper, symbols)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        public override bool CanHandle(VariableMetaData meta)
        {
            var fields = _helper.ReadFieldNames(meta.Entry);
            return fields.Any(x => x.StartsWith(_enumEncodedFieldName, StringComparison.OrdinalIgnoreCase));
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var pointerField = variable.Fields.First().Value.Data;

            yield return new VariableMetaData("inner", GetTypeName(pointerField), pointerField);
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var fullName = variable.Fields.First().Key;
            var enumNames = ReadEnumNames(meta.Entry);
            var pointerValue = BitConverter.ToUInt64(_helper.ReadValue(meta.Entry.Offset + meta.Entry.Size - 8, 8), 0);

            var enumName = pointerValue == 0 ? enumNames.Item1 : enumNames.Item2;
            var hasChildren = enumName != enumNames.Item1;

            return new VisualizationResult($"{meta.TypeName}::{enumName}", hasChildren);
        }

        private Tuple<string, string> ReadEnumNames(_DEBUG_TYPED_DATA typedData)
        {
            _output.Catch();
            _helper.OutputTypeDefinition(typedData);
            var name = _output.StopCatching();

            var nothingPart = name.Substring(0, name.IndexOf(':')).Trim();
            nothingPart = nothingPart.Substring(nothingPart.LastIndexOf('$') + 1);

            var pointerPart = name.Substring(name.IndexOf(':') + 1).Trim();
            pointerPart = pointerPart.Substring(pointerPart.IndexOf(':') + 2); // a::b::c

            return new Tuple<string, string>(nothingPart, pointerPart);
        }
    }
}
