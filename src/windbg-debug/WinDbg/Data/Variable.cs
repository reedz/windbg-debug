namespace WinDbgDebug.WinDbg.Data
{
    public class Variable : IIndexedItem
    {
        public Variable(int id, string name, string type, string value, bool hasChildren, uint? index = null)
        {
            Name = name;
            Type = type;
            Value = value;
            Id = id;
            HasChildren = hasChildren;
            Index = index;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }
        public bool HasChildren { get; private set; }
        public uint? Index { get; private set; }
    }
}
