using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.Collections.Generic;
using windbg_debug.WinDbg.Data;

namespace windbg_debug.WinDbg.Visualizers
{
    public class RustSliceVisualizer : VisualizerBase
    {
        #region Fields

        private static readonly string _typeName = "&[";

        #endregion

        #region Constructor

        public RustSliceVisualizer(RequestHelper helper, IDebugSymbols5 symbols, VisualizerRegistry registry) : base(helper, symbols, registry)
        {
        }

        #endregion

        #region Public Methods

        protected override bool DoCanHandle(VariableMetaData meta)
        {
            return meta.TypeName.Contains(_typeName);
        }

        protected override Dictionary<VariableMetaData, VisualizationResult> DoGetChildren(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var length = variable.Fields["length"].Data.Data;
            var pointer = variable.Fields["data_ptr"];

            return ReadArray(pointer.Data, length);
        }

        protected override VisualizationResult DoHandle(VariableMetaData meta)
        {
            var variable = _helper.ReadVariable(meta.Entry);
            var length = variable.Fields["length"].Data.Data;


            return new VisualizationResult($"{meta.TypeName} [{length}]", length > 0);
        }

        #endregion
    }
}
