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
    public class Message
    {
        public int id { get; }
        public string format { get; }
        public dynamic variables { get; }
        public dynamic showUser { get; }
        public dynamic sendTelemetry { get; }

        public Message(int id, string format, dynamic variables = null, bool user = true, bool telemetry = false)
        {
            this.id = id;
            this.format = format;
            this.variables = variables;
            this.showUser = user;
            this.sendTelemetry = telemetry;
        }
    }
}
