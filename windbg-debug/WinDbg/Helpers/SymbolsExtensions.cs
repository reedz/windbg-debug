using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class SymbolsExtensions
    {
        public static string GetSymbolType(this IDebugSymbols4 symbols, ulong moduleBase, uint typeId)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            var hr = symbols.GetTypeNameWide(moduleBase, typeId, buffer, buffer.Capacity, out size);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }

        public static string GetSymbolName(this IDebugSymbols4 symbols, ulong offset)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            ulong displacement;
            var hr = symbols.GetNameByOffsetWide(offset, buffer, buffer.Capacity, out size, out displacement);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }
    }
}
