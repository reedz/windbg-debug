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

    public class ThreadsResponseBody : ResponseBody
    {
        public Thread[] threads { get; }

        public ThreadsResponseBody(List<Thread> vars = null)
        {
            if (vars == null)
                threads = new Thread[0];
            else
                threads = vars.ToArray<Thread>();
        }
    }
}
