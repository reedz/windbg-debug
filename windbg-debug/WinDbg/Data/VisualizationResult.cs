namespace windbg_debug.WinDbg.Data
{
    public class VisualizationResult
    {
        #region Constructor

        public VisualizationResult(string value, bool hasChildren)
        {
            Value = value ?? Defaults.UnknownValue;
            HasChildren = hasChildren;
        }

        #endregion

        #region Public Properties

        public string Value { get; private set; }
        public bool HasChildren { get; private set; }

        #endregion
    }
}
