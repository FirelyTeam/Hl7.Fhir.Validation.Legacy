using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Hl7.Fhir.Specification.Tests
{
    public partial class BasicValidationTests
    {
        public static IEnumerable<object[]> InvariantTestcases =>
            new List<object[]>
            {
                new object[] { "ref-1", new ResourceReference{ Display = "Only a display element" }, true }
            };
    }
}
