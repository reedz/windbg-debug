namespace WinDbgDebug.WinDbg.Data
{
    public enum BasicType : uint
    {
        NoType = 0,
        Void = 1,
        Char = 2,
        WideChar = 3,
        Int = 6,
        UInt = 7,
        Float = 8,
        BCD = 9,
        Bool = 10,
        Long = 13,
        ULong = 14,
        Decimal = 25,
        DateTime = 26,
        Variant = 27,
        Complex = 28,
        Bit = 29,
        BStr = 30,
        HResult = 31,
    }
}
