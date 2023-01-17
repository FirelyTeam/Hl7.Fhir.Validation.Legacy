using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;

namespace Hl7.Fhir.Specification.Tests
{
    internal partial class TestProfileArtifactSource : IResourceResolver
    {
        public TestProfileArtifactSource()
        {
            TestProfiles.Add(buildSliceOnChoice());
            TestProfiles.Add(buildConstrainBindableType());
        }

        private static StructureDefinition buildSliceOnChoice()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/MedicationStatement-issue-2132", "MedicationStatement-issue-2132",
                "MedicationStatement sliced on asNeeded[x]", FHIRAllTypes.MedicationStatement);

            var cons = result.Differential.Element;

            var slicingIntro = new ElementDefinition("MedicationStatement.dosage.asNeeded[x]")
               .WithSlicingIntro(ElementDefinition.SlicingRules.Closed, (ElementDefinition.DiscriminatorType.Type, "$this"));

            cons.Add(slicingIntro);

            cons.Add(new ElementDefinition("MedicationStatement.dosage.asNeeded[x]")
            {
                ElementId = "MedicationStatement.dosage.asNeeded[x]:asNeededBoolean",
                SliceName = "asNeededBoolean",
            }.OfType(FHIRAllTypes.Boolean));

            cons.Add(new ElementDefinition("MedicationStatement.dosage.asNeeded[x]")
            {
                ElementId = "MedicationStatement.dosage.asNeeded[x]:asNeededCodeableConcept",
                SliceName = "asNeededCodeableConcept",
            }.OfType(FHIRAllTypes.CodeableConcept));

            return result;
        }

        private static StructureDefinition buildConstrainBindableType()
        {
            var result = createTestSD("http://validationtest.org/fhir/StructureDefinition/MedicationStatement-issue-2132-2", "MedicationStatement-issue-2132",
                "MedicationStatement sliced on asNeeded[x]", FHIRAllTypes.MedicationStatement);

            var cons = result.Differential.Element;

            var typeConstraint = new ElementDefinition("MedicationStatement.dosage.asNeeded[x]").OfType(FHIRAllTypes.Boolean);

            cons.Add(typeConstraint);

            return result;
        }

    }
}