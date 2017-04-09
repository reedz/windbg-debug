using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using log4net;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public abstract class VisualizerBase : IVisualizer
    {
        #region Fields

        protected static readonly Dictionary<VariableMetaData, VisualizationResult> _empty = new Dictionary<VariableMetaData, VisualizationResult>();
        protected readonly RequestHelper _helper;
        protected readonly IDebugSymbols4 _symbols;
        protected readonly VisualizerRegistry _registry;
        protected readonly ILog _logger;

        #endregion

        #region Constructor

        protected VisualizerBase(RequestHelper helper, IDebugSymbols4 symbols, VisualizerRegistry registry)
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
            _logger = LogManager.GetLogger(GetType().Name);
        }

        #endregion

        #region Public Methods

        public bool CanHandle(VariableMetaData meta)
        {
            return DoCanHandle(meta);
        }

        public VisualizationResult Handle(VariableMetaData meta)
        {
            try
            {
                return DoHandle(meta);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error resolving variable '{meta.Name}': {ex.Message}", ex);
                _logger.Debug("Falling back to default visualizer ..");
                try
                {
                    return _registry.DefaultVisualizer.Handle(meta);
                }
                catch (Exception defaultVisualizerException)
                {
                    _logger.Error($"Default visualizer failed to resolve variable '{meta.Name}': {defaultVisualizerException.Message}", defaultVisualizerException);
                    return new VisualizationResult("<could not identify value>", false);
                }
            }
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
            return _symbols.GetSymbolType(typedData.ModBase, typedData.TypeId);
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
    }
}
