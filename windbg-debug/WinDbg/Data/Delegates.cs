namespace WinDbgDebug.WinDbg.Data
{
    public delegate void BreakpointHitHandler(Breakpoint breakpoint, int threadId);
    public delegate void ExceptionHitHandler(int exceptionCode, int threadId);
    public delegate void BreakHandler(int threadId);
}
