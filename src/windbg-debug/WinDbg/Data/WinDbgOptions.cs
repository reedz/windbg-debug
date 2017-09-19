using System;

namespace WinDbgDebug.WinDbg.Data
{
    public class WinDbgOptions
    {
        public WinDbgOptions(string enginePath, string[] sourcePaths, string[] symbolPaths)
        {
            EnginePath = enginePath;
            SourcePaths = sourcePaths ?? new string[0];
            SymbolPaths = symbolPaths ?? new string[0];
        }

        public string EnginePath { get; set; }
        public string[] SourcePaths { get; set; }
        public string[] SymbolPaths { get; set; }
    }
}
