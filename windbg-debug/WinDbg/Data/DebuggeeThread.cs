using System;

namespace windbg_debug.WinDbg.Data
{
    public class DebuggeeThread : IIndexedItem
    {
        public DebuggeeThread(int id, string name)
        {
            if (id <= 0)
                throw new ArgumentException($"Scope should have positive index ('{id}').", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                name = $"Thread #{id}";

            Name = name;
            Id = id;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
    }
}
