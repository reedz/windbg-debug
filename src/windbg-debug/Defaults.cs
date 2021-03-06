﻿using System;
using Microsoft.Diagnostics.Runtime.Interop;

namespace WinDbgDebug
{
    public static class Defaults
    {
        public static readonly int BufferSize = 1024;
        public static readonly ulong CurrentOffset = 0;
        public static readonly DEBUG_CREATE_PROCESS DEBUG = (DEBUG_CREATE_PROCESS)DEBUG_PROCESS.DETACH_ON_EXIT;
        public static readonly ulong NoServer = 0;
        public static readonly uint NoProcess = 0;
        public static readonly int FirstIndex = 1;
        public static readonly int MaxFrames = 1000;
        public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);
        public static readonly uint NoParent = uint.MaxValue;
        public static readonly int NoChildren = 0;
        public static readonly byte[] NoPayload = new byte[0];
        public static readonly int NoThread = 0;
        public static readonly int NoFrame = 0;
        public static readonly int MaxStringSize = 200;
        public static readonly int NotFound = -1;
        public static readonly string UnknownValue = "<Unknown>";
    }
}
