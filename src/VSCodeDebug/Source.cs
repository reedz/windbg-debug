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

    public class Source
    {
        public string name { get; }
        public string path { get; }
        public int sourceReference { get; }

        public Source(string name, string path, int sourceReference = 0)
        {
            this.name = name;
            this.path = path;
            this.sourceReference = sourceReference;
        }

        public Source(string path, int sourceReference = 0)
        {
            this.name = Path.GetFileName(path);
            this.path = path;
            this.sourceReference = sourceReference;
        }
    }
}
