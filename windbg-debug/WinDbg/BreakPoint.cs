using System;

namespace windbg_debug.WinDbg
{
    public class Breakpoint
    {
        public Breakpoint(string file, int line)
        {
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file));

            if (line <= 0)
                throw new ArgumentOutOfRangeException(nameof(line), "Line should be a positive number.");

            File = file;
            Line = line;
        }

        public string File { get; private set; }
        public int Line { get; private set; }
    }
}
