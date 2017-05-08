using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class DefaultVisualizer : VisualizerBase
    {
        private readonly OutputCallbacks _output;

        public DefaultVisualizer(RequestHelper helper, IDebugSymbols4 symbols, OutputCallbacks output)
            : base(helper, symbols)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        public override bool CanHandle(VariableMetaData descriptor)
        {
            return true;
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData descriptor)
        {
            var result = new List<VariableMetaData>();
            var typedData = descriptor.Entry;
            if (typedData.Tag == (uint)SymTag.PointerType)
            {
                var pointerValue = _helper.Dereference(typedData);
                var meta = new VariableMetaData("inner", _symbols.GetSymbolType(pointerValue.ModBase, pointerValue.TypeId), pointerValue);
                result.Add(meta);
            }
            else if (typedData.Tag == (uint)SymTag.ArrayType)
            {
                var arrayLength = GetArrayLength(typedData);
                return ReadArray(typedData, arrayLength);
            }
            else
            {
                var fieldNames = _helper.ReadFieldNames(typedData);
                foreach (var field in fieldNames)
                {
                    var fieldData = _helper.GetField(typedData, field);
                    var meta = new VariableMetaData(field, _symbols.GetSymbolType(fieldData.ModBase, fieldData.TypeId), fieldData);
                    result.Add(meta);
                }
            }

            return result;
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            if (typedData.Tag == (uint)SymTag.PointerType)
            {
                return new VisualizationResult($"{meta.Entry.Data} ({meta.TypeName})", true);
            }

            if (typedData.Tag == (uint)SymTag.ArrayType)
            {
                return new VisualizationResult($"{meta.TypeName}", GetArrayLength(typedData) > 0);
            }

            var fieldNames = _helper.ReadFieldNames(typedData);
            return new VisualizationResult(GetDefaultValue(typedData), fieldNames.Length > 0);
        }

        private string GetDefaultValue(_DEBUG_TYPED_DATA typedData)
        {
            _output.Catch();
            _helper.OutputShortValue(typedData);
            return _output.StopCatching().Trim();
        }
    }
}
