using System;
using System.Collections.Generic;
using System.Linq;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class VisualizerRegistry
    {
        private readonly List<IVisualizer> _registry = new List<IVisualizer>();

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
                result = handler.Handle(description);
                return true;
            }

            result = null;
            return false;
        }

        public VisualizationResult Handle(VariableMetaData description)
        {
            var handler = FindHandler(description);
            return handler.Handle(description);
        }

        public IReadOnlyDictionary<VariableMetaData, VisualizationResult> GetChildren(VariableMetaData description)
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

        private IVisualizer FindHandler(VariableMetaData description)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? DefaultVisualizer;
            if (handler == null)
                throw new ArgumentException($"Visualizer for handling '{description.Name}' has not been registered.", nameof(description));
            return handler;
        }
    }
}
