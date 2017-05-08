namespace WinDbgDebug.WinDbg.Data
{
    public class VisualizationResult
    {
        public VisualizationResult(string value, bool hasChildren)
        {
            Value = value ?? Defaults.UnknownValue;
            HasChildren = hasChildren;
        }

        public string Value { get; private set; }
        public bool HasChildren { get; private set; }
    }
}
