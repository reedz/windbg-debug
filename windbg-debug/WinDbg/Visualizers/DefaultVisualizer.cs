using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class DefaultVisualizer : IVisualizer
    {
        #region Fields

        private readonly OutputCallbacks _output;
        private readonly RequestHelper _helper;
        private readonly IDebugSymbols4 _symbols;

        #endregion

        #region Constructor

        public DefaultVisualizer(RequestHelper helper, IDebugSymbols4 symbols, OutputCallbacks output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols));

            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            _output = output;
            _helper = helper;
            _symbols = symbols;
        }

        #endregion

        #region Public Methods

        public bool CanHandle(VariableMetaData descriptor)
        {
            return true;
        }

        public IReadOnlyDictionary<VariableMetaData, VisualizationResult> GetChildren(VariableMetaData descriptor)
        {
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var typedData = descriptor.Entry;
            if (typedData.Tag == (uint)SymTag.PointerType)
            {
                var pointerValue = _helper.Dereference(typedData);
                var meta = new VariableMetaData("inner", _symbols.GetSymbolType(pointerValue.ModBase, pointerValue.TypeId), pointerValue);
                result.Add(meta, Handle(meta));
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
                    result.Add(meta, Handle(meta));
                }
            }

            return result;
        }

        public VisualizationResult Handle(VariableMetaData meta)
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

        #endregion

        #region Private Methods

        private string GetDefaultValue(_DEBUG_TYPED_DATA typedData)
        {
            _output.Catch();
            _helper.OutputShortValue(typedData);
            return _output.StopCatching().Trim();
        }

        private uint GetArrayLength(_DEBUG_TYPED_DATA typedData)
        {
            var itemSize = _helper.Dereference(typedData).Size;
            var totalSize = typedData.Size;

            if (itemSize == 0)
                return 0;

            return totalSize / itemSize;
        }

        private Dictionary<VariableMetaData, VisualizationResult> ReadArray(_DEBUG_TYPED_DATA pointer, ulong size)
        {
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var dereferenced = _helper.Dereference(pointer);
            for (ulong i = 0; i < size; i++)
            {
                var meta = new VariableMetaData($"[{i}]", _symbols.GetSymbolType(dereferenced.ModBase, dereferenced.TypeId), _helper.GetArrayItem(pointer, i));
                var visualized = Handle(meta);
                result.Add(meta, visualized);
            }

            return result;
        }

        #endregion
    }
}
