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

        public RustVectorVisualizer(RequestHelper helper, IDebugSymbols4 symbols)
            : base(helper, symbols)
        {
        }

        #endregion

        #region Public Methods

        public override bool CanHandle(VariableMetaData meta)
        {
            return meta.TypeName.StartsWith(_vectorTypeName, StringComparison.OrdinalIgnoreCase);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            var typedData = meta.Entry;

            var arrayField = _helper.GetField(typedData, "buf");
            var arrayLengthField = _helper.GetField(typedData, "len");

            var actualLength = _helper.ReadLong(arrayLengthField);
            for (long i = 0; i < actualLength; i++)
            {
                // @TODO
            }

            yield break;
        }

        public override VisualizationResult Handle(VariableMetaData meta)
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
