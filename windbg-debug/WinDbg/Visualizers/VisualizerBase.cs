using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public abstract class VisualizerBase
    {
        #region Fields

        protected static readonly Dictionary<VariableMetaData, VisualizationResult> _empty = new Dictionary<VariableMetaData, VisualizationResult>();
        protected readonly RequestHelper _helper;
        protected readonly IDebugSymbols5 _symbols;
        protected readonly VisualizerRegistry _registry;

        #endregion

        #region Constructor

        protected VisualizerBase(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry)
        {
            if (helper == null)
                throw new ArgumentNullException(nameof(helper));

            if (symbols == null)
                throw new ArgumentNullException(nameof(symbols));

            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            _helper = helper;
            _symbols = symbols;
            _registry = registry;
        }

        #endregion

        #region Public Methods

        public bool CanHandle(VariableMetaData meta)
        {
            return DoCanHandle(meta);
        }

        public VisualizationResult Handle(VariableMetaData meta)
        {
            return DoHandle(meta);
        }

        public IReadOnlyDictionary<VariableMetaData, VisualizationResult> GetChildren(VariableMetaData meta)
        {
            var result = DoGetChildren(meta);

            return new ReadOnlyDictionary<VariableMetaData, VisualizationResult>(result);
        }

        #endregion

        #region Protected Methods

        protected string GetTypeName(_DEBUG_TYPED_DATA typedData)
        {
            return GetSymbolType(typedData.ModBase, typedData.TypeId);
        }

        protected string ReadString(ulong offset, uint size)
        {
            return Encoding.Default.GetString(_helper.ReadValue(offset, size));
        }

        protected VisualizationResult ReHandle(VariableMetaData meta)
        {
            return _registry.Handle(meta);
        }

        protected Dictionary<VariableMetaData, VisualizationResult> ReadArray(_DEBUG_TYPED_DATA pointer, ulong size)
        {
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            var dereferenced = _helper.Dereference(pointer);
            for (ulong i = 0; i < size; i++)
            {
                var meta = new VariableMetaData($"[{i}]", GetTypeName(dereferenced), _helper.GetArrayItem(pointer, i));
                var visualized = ReHandle(meta);
                result.Add(meta, visualized);
            }

            return result;
        }

        protected abstract bool DoCanHandle(VariableMetaData meta);
        protected abstract VisualizationResult DoHandle(VariableMetaData meta);
        protected abstract Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta);

        #endregion

        #region Private Methods

        private string GetSymbolType(ulong moduleBase, uint typeId)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            var hr = _symbols.GetTypeNameWide(moduleBase, typeId, buffer, buffer.Capacity, out size);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }

        private string GetSymbolName(ulong offset)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            ulong displacement;
            var hr = _symbols.GetNameByOffsetWide(offset, buffer, buffer.Capacity, out size, out displacement);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }

        #endregion
    }
}
