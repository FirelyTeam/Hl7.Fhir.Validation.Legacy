using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Hl7.Fhir.Specification.Tests
{
    public partial class BasicValidationTests
    {
        public static IEnumerable<object[]> InvariantTestcases =>
        new List<object[]>
        {
            new object[] { "ref-1", new ResourceReference { Display = "Only a display element" }, true },
            new object[] { "eld-19", new ElementDefinition { Path = ":.ContainingSpecialCharacters" }, false },
            new object[] { "eld-19", new ElementDefinition { Path = "NoSpecialCharacters" }, true },
            new object[] { "eld-20", new ElementDefinition { Path = "   leadingSpaces" }, false },
            new object[] { "eld-19", new ElementDefinition { Path = "NoSpaces.withADot" }, true },
            new object[] { "que-7",
                    new Questionnaire.EnableWhenComponent
                        {
                            Operator = Questionnaire.QuestionnaireItemOperator.Exists,
                            Answer = new FhirBoolean(true)
                    }, true },
            new object[] { "sdf-29",
                    new StructureDefinition
                    {
                        Kind = StructureDefinition.StructureDefinitionKind.Resource,
                        Derivation = StructureDefinition.TypeDerivationRule.Specialization,
                        Differential =
                        new StructureDefinition.DifferentialComponent
                        {
                                Element = new List<ElementDefinition> {
                                    new ElementDefinition
                                    {
                                        ElementId = "Example.test",
                                        Min = 1
                                    },
                                }
                        }
                    }, true
            },
        };
    }
}
