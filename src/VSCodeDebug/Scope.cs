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

    public class Scope
    {
        public string name { get; }
        public int variablesReference { get; }
        public bool expensive { get; }

        public Scope(string name, int variablesReference, bool expensive = false)
        {
            this.name = name;
            this.variablesReference = variablesReference;
            this.expensive = expensive;
        }
    }
}
