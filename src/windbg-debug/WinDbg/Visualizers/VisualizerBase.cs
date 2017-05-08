using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public abstract class VisualizerBase : IVisualizer
    {
        protected readonly RequestHelper _helper;
        protected readonly IDebugSymbols4 _symbols;

        protected VisualizerBase(RequestHelper helper, IDebugSymbols4 symbols)
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols));

            _helper = helper;
            _symbols = symbols;
        }

        public abstract bool CanHandle(VariableMetaData meta);
        public abstract IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta);
        public abstract VisualizationResult Handle(VariableMetaData meta);

        protected string GetTypeName(_DEBUG_TYPED_DATA typedData)
        {
            return _symbols.GetSymbolType(typedData.ModBase, typedData.TypeId);
        }

        protected IEnumerable<VariableMetaData> ReadArray(_DEBUG_TYPED_DATA pointer, ulong size)
        {
            var dereferenced = _helper.Dereference(pointer);
            for (ulong i = 0; i < size; i++)
            {
                yield return new VariableMetaData($"[{i}]", GetTypeName(dereferenced), _helper.GetArrayItem(pointer, i));
            }
        }

        protected uint GetArrayLength(_DEBUG_TYPED_DATA typedData)
        {
            var itemSize = _helper.Dereference(typedData).Size;
            var totalSize = typedData.Size;

            if (itemSize == 0)
                return 0;

            return totalSize / itemSize;
        }
    }
}
