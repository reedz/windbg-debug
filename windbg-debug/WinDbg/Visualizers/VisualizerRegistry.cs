using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using log4net;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class VisualizerRegistry
    {
        private readonly ILog _logger = LogManager.GetLogger(nameof(VisualizerRegistry));
        private readonly List<IVisualizer> _registry = new List<IVisualizer>();
        private readonly VisualizationResult _defaultResult = new VisualizationResult("<could not identify value>", false);
        private readonly IReadOnlyDictionary<VariableMetaData, VisualizationResult> _emptyChildren = new ReadOnlyDictionary<VariableMetaData, VisualizationResult>(new Dictionary<VariableMetaData, VisualizationResult>());

        public VisualizerRegistry(IVisualizer defaultVisualizer)
        {
            if (defaultVisualizer == null)
                throw new ArgumentNullException(nameof(defaultVisualizer));

            DefaultVisualizer = defaultVisualizer;
        }

        public IVisualizer DefaultVisualizer { get; private set; }

        public void AddVisualizer(VisualizerBase item)
        {
            if (item == null)
                return;

            _registry.Add(item);
        }

        public bool CanHandle(VariableMetaData description)
        {
            return DefaultVisualizer != null || _registry.Any(x => x.CanHandle(description));
        }

        public bool TryHandle(VariableMetaData description, out VisualizationResult result)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? DefaultVisualizer;
            if (handler != null)
            {
                try
                {
                    result = handler.Handle(description);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error resolving variable '{description.Name}': {ex.Message}", ex);
                    _logger.Debug("Falling back to default visualizer ..");
                    try
                    {
                        result = DefaultVisualizer.Handle(description);
                        return true;
                    }
                    catch (Exception defaultVisualizerException)
                    {
                        _logger.Error($"Default visualizer failed to resolve variable '{description.Name}': {defaultVisualizerException.Message}", defaultVisualizerException);
                    }
                }
            }

            result = _defaultResult;
            return false;
        }

        public VisualizationResult Handle(VariableMetaData description)
        {
            VisualizationResult result;
            if (!TryHandle(description, out result))
            {
                return _defaultResult;
            }

            return result;
        }

        public IReadOnlyDictionary<VariableMetaData, VisualizationResult> GetChildren(VariableMetaData description)
        {
            try
            {
                var handler = FindHandler(description);
                var metas = handler.GetChildren(description);

                var result = new Dictionary<VariableMetaData, VisualizationResult>();
                if (metas == null)
                    return result;

                foreach (var variable in metas)
                {
                    result.Add(variable, Handle(variable));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error expanding variable '{description.Name}': {ex.Message}", ex);
                return _emptyChildren;
            }
        }

        private IVisualizer FindHandler(VariableMetaData description)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? DefaultVisualizer;
            if (handler == null)
                throw new ArgumentException($"Visualizer for handling '{description.Name}' has not been registered.", nameof(description));
            return handler;
        }
    }
}
