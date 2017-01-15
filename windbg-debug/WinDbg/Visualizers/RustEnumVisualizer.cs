using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using windbg_debug.WinDbg.Data;
using System.Linq;

namespace windbg_debug.WinDbg.Visualizers
{
    public class RustEnumVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _enumFieldName = "RUST$ENUM$";
        private static readonly string _enumEncodedFieldName = "RUST$ENCODED$ENUM";
        private readonly OutputCallbacks _output;
        #endregion

        #region Constructor

        public RustEnumVisualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry, OutputCallbacks output) : base(helper, symbols, registry)
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
            return fields.Any(x => x.StartsWith(_enumFieldName, StringComparison.OrdinalIgnoreCase)
                || x.StartsWith(_enumEncodedFieldName, StringComparison.OrdinalIgnoreCase));
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var actualFields = new Dictionary<string, TypedVariable>();
            int counter = -1;
            int enumValue = GetEnumValue(meta.Entry);
            foreach (var pair in variable.Fields)
            {
                if (pair.Key.StartsWith(_enumFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    counter++;
                    continue;
                }

                if (counter == enumValue)
                    actualFields.Add(pair.Key, pair.Value);
            }

            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            foreach (var pair in actualFields)
            {
                var childMeta = new VariableMetaData(pair.Key, GetTypeName(pair.Value.Data), pair.Value.Data);
                var childValue = ReHandle(childMeta);
                result.Add(childMeta, childValue);
            }

            return result;
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            string enumName;
            if (variable.Fields.First().Key.StartsWith(_enumEncodedFieldName, StringComparison.OrdinalIgnoreCase))
            {
                var fullName = variable.Fields.First().Key;
                enumName = fullName.Substring(fullName.LastIndexOf('$') + 1);
            }
            else
            {
                var names = ReadEnumNames(variable.Fields.First().Value.Data);
                var enumValue = GetEnumValue(meta.Entry);
                enumName = names.ContainsKey(enumValue) ? names[enumValue] : Defaults.UnknownValue;
            }

            return new VisualizationResult($"{meta.TypeName}::{enumName}", true);
        }

        #endregion

        #region Fields

        private int GetEnumValue(_DEBUG_TYPED_DATA typedData)
        {
            return _helper.ReadValue(typedData.Offset, 1)[0];
        }

        private Dictionary<int, string> ReadEnumNames(_DEBUG_TYPED_DATA typedData)
        {
            _output.Catch();
            _helper.OutputTypeDefinition(typedData);
            var typeDefinition = _output.StopCatching();

            var enumerationTypes = typeDefinition.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new Dictionary<int, string>();
            foreach (var line in enumerationTypes)
            {
                var enumName = line.Substring(0, line.IndexOf('=')).Trim();
                var enumValue = line.Substring(line.IndexOf('=') + 1).Replace("0n", "").Trim();
                result.Add(int.Parse(enumValue), enumName);
            }

            return result;
        }

        #endregion
    }
}
