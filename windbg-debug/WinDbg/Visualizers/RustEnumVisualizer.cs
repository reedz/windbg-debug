using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustEnumVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _enumFieldName = "RUST$ENUM$";
        private readonly OutputCallbacks _output;

        #endregion

        #region Constructor

        public RustEnumVisualizer(RequestHelper helper, IDebugSymbols4 symbols, OutputCallbacks output)
            : base(helper, symbols)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        #endregion

        #region Protected Methods

        public override bool CanHandle(VariableMetaData meta)
        {
            var fields = _helper.ReadFieldNames(meta.Entry);
            return fields.Any(x => x.StartsWith(_enumFieldName, StringComparison.OrdinalIgnoreCase));
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            int enumValue = GetEnumValue(meta.Entry);
            Dictionary<string, TypedVariable> actualFields = GetChildFields(variable, enumValue);

            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            foreach (var pair in actualFields)
            {
                yield return new VariableMetaData(pair.Key, GetTypeName(pair.Value.Data), pair.Value.Data);
            }
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);

            var names = ReadEnumNames(variable.Fields.First().Value.Data);
            var enumValue = GetEnumValue(meta.Entry);
            var enumName = names.ContainsKey(enumValue) ? names[enumValue] : Defaults.UnknownValue;
            var hasChildren = GetChildFields(variable, enumValue).Count > 0;

            return new VisualizationResult($"{meta.TypeName}::{enumName}", hasChildren);
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, TypedVariable> GetChildFields(TypedVariable variable, int enumValue)
        {
            int counter = -1;
            var actualFields = new Dictionary<string, TypedVariable>();
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

            return actualFields;
        }

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
                var enumValue = line.Substring(line.IndexOf('=') + 1).Replace("0n", string.Empty).Trim();
                result.Add(int.Parse(enumValue), enumName);
            }

            return result;
        }

        #endregion
    }
}
