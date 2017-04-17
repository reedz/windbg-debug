using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;
using WinDbgDebug.WinDbg.Data;
using WinDbgDebug.WinDbg.Helpers;

namespace WinDbgDebug.WinDbg.Visualizers
{
    public class RustSliceVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _typeName = "&[";

        #endregion

        #region Constructor

        public RustSliceVisualizer(RequestHelper helper, IDebugSymbols4 symbols)
            : base(helper, symbols)
        {
        }

        #endregion

        #region Public Methods

        public override bool CanHandle(VariableMetaData meta)
        {
            return meta.TypeName.Contains(_typeName);
        }

        public override IEnumerable<VariableMetaData> GetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var length = variable.Fields["length"].Data.Data;
            var pointer = variable.Fields["data_ptr"];

            return ReadArray(pointer.Data, length);
        }

        public override VisualizationResult Handle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var length = variable.Fields["length"].Data.Data;

            return new VisualizationResult($"{meta.TypeName} [{length}]", length > 0);
        }

        #endregion
    }
}
