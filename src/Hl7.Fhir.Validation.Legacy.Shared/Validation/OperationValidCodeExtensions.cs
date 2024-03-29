﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Support;

#nullable enable

namespace Hl7.Fhir.Specification.Terminology
{
    public static class OperationValidCodeExtensions

    {
        /// <summary>
        /// This will tranform the Out Paramters of operation ValidCode to an OperationOutcome.
        /// </summary>
        /// <param name="parameters">the Out Paramters of operation ValidCode</param>
        /// <returns>OperationOutcome from parameters</returns>
        /// <remarks>This function will be removed, when the obsolete method ITerminologyService.ValidCode() 
        /// will be removed.</remarks>
        public static OperationOutcome ToOperationOutcome(this Parameters parameters)
        {
            var result = parameters.GetSingleValue<FhirBoolean>("result")?.Value ?? false;
            var message = parameters.GetSingleValue<FhirString>("message")?.Value;

            var outcome = new OperationOutcome();
            if (message is { })
            {
                var issue = result ? Issue.TERMINOLOGY_OUTPUT_WARNING : Issue.TERMINOLOGY_OUTPUT_ERROR;
                outcome.AddIssue(message, issue);
            }
            return outcome;
        }
    }
}
#nullable restore