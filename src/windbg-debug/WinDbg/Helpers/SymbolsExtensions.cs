using System.Text;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug.WinDbg.Helpers
{
    public static class SymbolsExtensions
    {
        public static string GetSymbolType(this IDebugSymbols symbols, ulong moduleBase, uint typeId)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            var hr = symbols.GetTypeName(moduleBase, typeId, buffer, buffer.Capacity, out size);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }

        public static string GetSymbolName(this IDebugSymbols symbols, ulong offset)
        {
            StringBuilder buffer = new StringBuilder(Defaults.BufferSize);
            uint size;
            ulong displacement;
            var hr = symbols.GetNameByOffset(offset, buffer, buffer.Capacity, out size, out displacement);
            if (hr != HResult.Ok)
                return string.Empty;

            return buffer.ToString();
        }
    }
}
