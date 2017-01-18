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

    public class StoppedEvent : Event
    {
        public StoppedEvent(int tid, string reasn, string txt = null)
            : base("stopped", new
            {
                threadId = tid,
                reason = reasn,
                text = txt
            })
        { }
    }
}
