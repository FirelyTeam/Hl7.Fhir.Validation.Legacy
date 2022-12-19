/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Specification.Terminology;

namespace Hl7.Fhir.Specification.Schema
{
    internal class ValidationContext
    {
        public ITerminologyService TerminologyService;

        // TODO BIG_COMMON   public IExceptionSource ExceptionSink;
    }
}
