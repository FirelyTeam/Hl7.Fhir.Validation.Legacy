/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Support;
using System;

namespace Hl7.Fhir.Validation
{
    internal static class FixedPatternValidationExtensions
    {
        public static OperationOutcome ValidateFixed(this Validator v, Element fixedValue, ITypedElement instance)
        {
            if (fixedValue is null) throw new ArgumentNullException(nameof(fixedValue));

            var outcome = new OperationOutcome();

            ITypedElement fixedValueNav = fixedValue.ToTypedElement();

            if (!instance.IsExactlyEqualTo(fixedValueNav, ignoreOrder: true))
            {
                v.Trace(outcome, $"Value is not exactly equal to fixed value '{toReadable(fixedValue)}'",
                        Issue.CONTENT_DOES_NOT_MATCH_FIXED_VALUE, instance);
            }

            return outcome;
        }

        public static OperationOutcome ValidateFixed(this Validator v, ElementDefinition definition, ITypedElement instance) =>
            definition.Fixed != null ? v.ValidateFixed(definition.Fixed, instance) : new OperationOutcome();

        public static OperationOutcome ValidatePattern(this Validator v, Element pattern, ITypedElement instance)
        {
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));

            var outcome = new OperationOutcome();

            ITypedElement patternValueNav = pattern.ToTypedElement();

            if (!instance.Matches(patternValueNav))
            {
                v.Trace(outcome, $"Value does not match pattern '{toReadable(pattern)}'",
                        Issue.CONTENT_DOES_NOT_MATCH_PATTERN_VALUE, instance);
            }

            return outcome;
        }


        public static OperationOutcome ValidatePattern(this Validator v, ElementDefinition definition, ITypedElement instance) =>
              definition.Pattern != null ? v.ValidatePattern(definition.Pattern, instance) : new OperationOutcome();

        private static string toReadable(Base value)
        {
            if (value is PrimitiveType)
                return value.ToString();
            else
                return new FhirJsonSerializer().SerializeToString(value);
        }
    }
}
