using System;

namespace WinDbgDebug.WinDbg
{
    public class Breakpoint
    {
        #region Constructor

        public Breakpoint(string file, int line)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file));

            if (line <= 0)
                throw new ArgumentOutOfRangeException(nameof(line), "Line should be a positive number.");

            File = file;
            Line = line;
        }

        #endregion

        #region Public Properties

        public string File { get; private set; }
        public int Line { get; private set; }

        #endregion
    }
}
