using System;
using System.Collections.Generic;
using System.Linq;
using WinDbgDebug.WinDbg.Data;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class VisualizerRegistry
    {
        #region Fields

        private readonly List<IVisualizer> _registry = new List<IVisualizer>();

        #endregion

        #region Constructor

        public VisualizerRegistry(IVisualizer defaultVisualizer)
        {
            if (defaultVisualizer == null)
                throw new ArgumentNullException(nameof(defaultVisualizer));

            DefaultVisualizer = defaultVisualizer;
        }

        #endregion

        #region Public Properties

        public IVisualizer DefaultVisualizer { get; private set; }

        #endregion

        #region Public Methods

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
            return handler.GetChildren(description);
        }

        #endregion

        #region Private Methods

        private IVisualizer FindHandler(VariableMetaData description)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? DefaultVisualizer;
            if (handler == null)
                throw new ArgumentException($"Visualizer for handling '{description.Name}' has not been registered.", nameof(description));
            return handler;
        }

        #endregion
    }
}
