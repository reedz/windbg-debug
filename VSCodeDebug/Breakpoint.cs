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
    public class Breakpoint
    {
        public Breakpoint(bool verified, int line)
        {
            this.verified = verified;
            this.line = line;
        }

#pragma warning disable SA1300 // Element must begin with upper-case letter
        public bool verified { get; }
        public int line { get; }
#pragma warning restore SA1300 // Element must begin with upper-case letter
    }
}
