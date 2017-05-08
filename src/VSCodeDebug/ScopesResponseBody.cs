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

    public class ScopesResponseBody : ResponseBody
    {
        public Scope[] scopes { get; }

        public ScopesResponseBody(List<Scope> scps = null)
        {
            if (scps == null)
                scopes = new Scope[0];
            else
                scopes = scps.ToArray<Scope>();
        }
    }
}
