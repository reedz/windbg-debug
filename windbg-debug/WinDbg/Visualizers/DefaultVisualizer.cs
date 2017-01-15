using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Visualizers
{
    public class DefaultVisualizer : VisualizerBase
    {
        #region Fields

        private readonly OutputCallbacks _output;

        #endregion

        #region Constructor

        public DefaultVisualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry, OutputCallbacks output) 
            : base(helper, symbols, registry) 
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            _output = output;
        }

        #endregion

        #region Public Methods

        protected override bool DoCanHandle(VariableMetaData descriptor)
        {
            return true;
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData descriptor)
        {
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var typedData = descriptor.Entry;
            if (typedData.Tag == (uint)SymTag.PointerType)
            {
                var pointerValue = _helper.Dereference(typedData);
                var meta = new VariableMetaData("inner", GetTypeName(pointerValue), pointerValue);
                result.Add(meta, Handle(meta));
            }
            else if (typedData.Tag == (uint)SymTag.ArrayType)
            {
                var itemSize = _helper.Dereference(typedData).Size;
                var totalSize = typedData.Size;
                return ReadArray(typedData, totalSize / itemSize);
            }
            else
            {
                var fieldNames = _helper.ReadFieldNames(typedData);
                foreach (var field in fieldNames)
                {
                    var fieldData = _helper.GetField(typedData, field);
                    var meta = new VariableMetaData(field, GetTypeName(fieldData), fieldData);
                    result.Add(meta, Handle(meta));
                }
            }

            return result;
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var typedData = ToTypedData(meta);

            if (typedData.Tag == (uint)SymTag.PointerType)
            {
                return new VisualizationResult($"{meta.Entry.Data} ({meta.TypeName})", true);
            }

            if (typedData.Tag == (uint)SymTag.ArrayType)
            {
                return new VisualizationResult($"{meta.TypeName}", typedData.Size / 16 > 0);
            }

            var fieldNames = _helper.ReadFieldNames(typedData);
            return new VisualizationResult(GetDefaultValue(typedData), fieldNames.Length > 0);
        }

        #endregion

        #region Private Methods

        private string GetDefaultValue(_DEBUG_TYPED_DATA typedData)
        {
            _output.Catch();
            _helper.OutputShortValue(typedData);
            return _output.StopCatching().Trim();
        }

        #endregion
    }
}
