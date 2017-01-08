namespace windbg_debug.WinDbg.Data
{
    public class Variable : IIndexedItem
    {
        #region Constructor

        public Variable(int id, string name, string type, string value, bool hasChildren, uint symbolListIndex)
        {
            Name = name;
            Type = type;
            Value = value;
            Id = id;
            HasChildren = hasChildren;
            SymbolListIndex = symbolListIndex;
        }

        #endregion

        #region Public Properties

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }
        public bool HasChildren { get; private set; }
        public uint SymbolListIndex { get; private set; }

        #endregion
    }
}
