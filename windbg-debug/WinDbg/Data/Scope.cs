using System;

namespace WinDbgDebug.WinDbg.Data
{
    public class Scope : IIndexedItem
    {
        #region Constructor

        public Scope(int id, string name)
        {
            if (id <= 0)
                throw new ArgumentException($"Scope should have positive index ('{id}').", nameof(id));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Id = id;
        }

        #endregion

        #region Public Properties

        public int Id { get; private set; }
        public string Name { get; private set; }

        #endregion
    }
}
