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

    public class SetBreakpointsResponseBody : ResponseBody
    {
        public Breakpoint[] breakpoints { get; }

        public SetBreakpointsResponseBody(List<Breakpoint> bpts = null)
        {
            if (bpts == null)
                breakpoints = new Breakpoint[0];
            else
                breakpoints = bpts.ToArray<Breakpoint>();
        }
    }
}
