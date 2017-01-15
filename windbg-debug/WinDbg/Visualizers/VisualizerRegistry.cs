using System;
using System.Collections.Generic;
using System.Linq;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Visualizers
{
    public class VisualizerRegistry
    {
        #region Fields

        private readonly List<VisualizerBase> _registry = new List<VisualizerBase>();
        private VisualizerBase _defaultHandler;

        #endregion

        #region Public Methods

        public void AddVisualizer(VisualizerBase item)
        {
            if (item == null)
                return;

            _registry.Add(item);
        }

        public void SetDefaultVisualizer(VisualizerBase item)
        {
            _defaultHandler = item;
        }

        public bool CanHandle(VariableMetaData description)
        {
            return _defaultHandler != null || _registry.Any(x => x.CanHandle(description));
        }

        public bool TryHandle(VariableMetaData description, out VisualizationResult result)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? _defaultHandler;
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

        private VisualizerBase FindHandler(VariableMetaData description)
        {
            var handler = _registry.FirstOrDefault(x => x.CanHandle(description)) ?? _defaultHandler;
            if (handler == null)
                throw new ArgumentException($"Visualizer for handling '{description.Name}' has not been registered.", nameof(description));
            return handler;
        }

        #endregion
    }
}
