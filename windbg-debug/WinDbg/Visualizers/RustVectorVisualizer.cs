using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustVectorVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _vectorTypeName = "struct collections::vec::Vec";

        #endregion

        #region Constructor

        public RustVectorVisualizer(RequestHelper helper, IDebugSymbols4 symbols, VisualizerRegistry registry)
            : base(helper, symbols, registry)
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

            var actualLength = _helper.ReadLong(arrayLengthField);
            var result = new Dictionary<VariableMetaData, VisualizationResult>();
            for (long i = 0; i < actualLength; i++)
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
            long actualLength = _helper.ReadLong(arrayLengthField);

            var value = $"Vec{meta.TypeName.Substring(_vectorTypeName.Length)} [{actualLength}]";

            return new VisualizationResult(value, actualLength > 0);
        }

        #endregion
    }
}
