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

    public class Variable
    {
        public string name { get; }
        public string value { get; }
        public int variablesReference { get; }

        public Variable(string name, string value, int variablesReference = 0)
        {
            this.name = name;
            this.value = value;
            this.variablesReference = variablesReference;
        }
    }
}
