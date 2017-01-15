using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Visualizers
{
    public class RustVectorVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _vectorTypeName = "struct collections::vec::Vec";

        #endregion

        #region Constructor

        public RustVectorVisualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry) : base(helper, symbols, registry)
        {
        }

        #endregion

        #region Public Methods

        protected override bool DoCanHandle(VariableMetaData meta)
        {
            return meta.TypeName.StartsWith(_vectorTypeName, StringComparison.OrdinalIgnoreCase);
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var arrayField = _helper.GetField(typedData, "buf");
            var arrayLengthField = _helper.GetField(typedData, "len");

            var actualLength = BitConverter.ToUInt64(_helper.ReadValue(arrayLengthField.Offset, arrayLengthField.Size), 0);
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            for (ulong i = 0; i < actualLength; i++)
            {
                // ...
            }

            return result;
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var arrayField = _helper.GetField(typedData, "buf");
            var arrayLengthField = _helper.GetField(typedData, "len");

            var actualLength = BitConverter.ToInt64(_helper.ReadValue(arrayLengthField.Offset, arrayLengthField.Size), 0);
            var value = $"Vec{meta.TypeName.Substring(_vectorTypeName.Length)} [{actualLength}]";

            return new VisualizationResult(value, actualLength > 0);
        }

        #endregion
    }
}
