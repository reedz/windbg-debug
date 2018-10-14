/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace VSCodeDebug
{
    public class Capabilities : ResponseBody
    {
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
        public bool supportsConfigurationDoneRequest;
        public bool supportsFunctionBreakpoints;
        public bool supportsConditionalBreakpoints;
        public bool supportsEvaluateForHovers;
        public bool supportsSetVariable;
        public dynamic[] exceptionBreakpointFilters;
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
    }
}
