namespace windbg_debug.WinDbg.Data
{
    public class Variable
    {
        public Variable(string name, string type, string value)
        {
            Name = name;
            Type = type;
            Value = value;
        }

        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }
    }
}
