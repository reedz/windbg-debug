using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustEncodedEnumVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _enumEncodedFieldName = "RUST$ENCODED$ENUM";
        private readonly OutputCallbacks _output;

        #endregion

        #region Constructor

        public RustEncodedEnumVisualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry, OutputCallbacks output)
            : base(helper, symbols, registry)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        #endregion

        #region Protected Methods

        protected override bool DoCanHandle(VariableMetaData meta)
        {
            var fields = _helper.ReadFieldNames(meta.Entry);
            return fields.Any(x => x.StartsWith(_enumEncodedFieldName, StringComparison.OrdinalIgnoreCase));
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var pointerField = variable.Fields.First().Value.Data;

            var childMeta = new VariableMetaData("inner", GetTypeName(pointerField), pointerField);
            result.Add(childMeta, ReHandle(childMeta));

            return result;
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var fullName = variable.Fields.First().Key;
            var enumNames = ReadEnumNames(meta.Entry);
            var pointerValue = BitConverter.ToUInt64(_helper.ReadValue(meta.Entry.Offset + meta.Entry.Size - 8, 8), 0);

            var enumName = pointerValue == 0 ? enumNames.Item1 : enumNames.Item2;
            var hasChildren = enumName != enumNames.Item1;

            return new VisualizationResult($"{meta.TypeName}::{enumName}", hasChildren);
        }

        #endregion

        #region Private Methods

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

        #endregion
    }
}
