using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Hl7.Fhir.Specification.Tests
{
    public partial class BasicValidationTests
    {
        public static IEnumerable<object[]> InvariantTestcases =>
            new List<object[]>
            {
                new object[] { "ref-1", new ResourceReference{ Display = "Only a display element" }, true },
                new object[] { "eld-19", new ElementDefinition { Path = ":.ContainingSpecialCharacters" }, false},
                new object[] { "eld-19", new ElementDefinition { Path = "NoSpecialCharacters" }, true },
                new object[] { "eld-20", new ElementDefinition { Path = "   leadingSpaces" }, false},
                new object[] { "eld-19", new ElementDefinition { Path = "NoSpaces.withADot" }, true },
                new object[] { "sdf-0", new StructureDefinition { Name = " leadingSpaces" }, false },
                new object[] { "sdf-0", new StructureDefinition { Name = "Name" }, true },
                new object[] { "sdf-24",
                    new StructureDefinition
                    {
                        Snapshot =
                        new StructureDefinition.SnapshotComponent
                            {
                                Element = new List<ElementDefinition> {
                                    new ElementDefinition
                                    {
                                        ElementId = "coderef.reference",
                                        Type = new List<ElementDefinition.TypeRefComponent>
                                               {
                                                    new ElementDefinition.TypeRefComponent { Code = "Reference", TargetProfile = new[] { "http://example.com/profile" }  }
                                               }
                                    },
                                    new ElementDefinition
                                    {
                                        ElementId = "coderef",
                                        Type = new List<ElementDefinition.TypeRefComponent>
                                               {
                                                    new ElementDefinition.TypeRefComponent { Code = "CodeableReference"}
                                               }
                                    },
                                 }
                        }
                    }, false },
                new object[] { "sdf-25",
                    new StructureDefinition
                    {
                        Snapshot =
                        new StructureDefinition.SnapshotComponent
                            {
                                Element = new List<ElementDefinition> {
                                    new ElementDefinition
                                    {
                                        ElementId = "coderef.concept",
                                        Type = new List<ElementDefinition.TypeRefComponent>
                                               {
                                                    new ElementDefinition.TypeRefComponent { Code = "CodeableConcept" }
                                               },
                                        Binding = new ElementDefinition.ElementDefinitionBindingComponent { Description = "Just a description" }
                                    },
                                    new ElementDefinition
                                    {
                                        ElementId = "coderef",
                                        Type = new List<ElementDefinition.TypeRefComponent>
                                               {
                                                    new ElementDefinition.TypeRefComponent { Code = "CodeableReference"}
                                               }
                                    },
                                 }
                        }
                    }, false
                },
                new object[] { "que-7",
                        new Questionnaire.EnableWhenComponent
                            {
                                Operator = Questionnaire.QuestionnaireItemOperator.Exists,
                                Answer = new FhirBoolean(true)
                        }, true
                },
            };
    }
}
