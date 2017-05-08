/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VSCodeDebug
{

    public class StackTraceResponseBody : ResponseBody
    {
        public StackFrame[] stackFrames { get; }

        public StackTraceResponseBody(List<StackFrame> frames = null)
        {
            if (frames == null)
                stackFrames = new StackFrame[0];
            else
                stackFrames = frames.ToArray<StackFrame>();
        }
    }
}
