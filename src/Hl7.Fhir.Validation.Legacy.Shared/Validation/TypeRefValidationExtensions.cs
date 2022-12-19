﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hl7.Fhir.Validation
{
    internal static class TypeRefValidationExtensions
    {
        internal static OperationOutcome ValidateType(this Validator validator, ElementDefinition definition, ScopedNode instance, ValidationState state, bool validateProfiles)
        {
            var outcome = new OperationOutcome();

            validator.Trace(outcome, "Validating against constraints specified by the element's defined type", Issue.PROCESSING_PROGRESS, instance);

            if (definition.Type.Any(tr => tr.Code == null))
                validator.Trace(outcome, "ElementDefinition contains a type with an empty type code", Issue.PROFILE_ELEMENTDEF_CONTAINS_NULL_TYPE, instance);

            // Check if this is a choice: there are multiple distinct Codes to choose from
            var typeRefs = definition.Type.Where(tr => tr.Code != null);
            var choices = typeRefs.Select(tr => tr.Code).Distinct();


            if (instance.InstanceType != null)
            {
                //find out what type is present in the instance data
                // (e.g. deceased[Boolean], or _resourceType in json). This is exposed by IElementNavigator.TypeName.
                var instanceType = ModelInfo.FhirTypeNameToFhirType(instance.InstanceType);

                if (choices.Count() > 1)
                {
                    if (instanceType is not null)
                    {
                        // The next statements are just an optimalization, without them, we would do an ANY validation
                        // against *all* choices, what we do here is pre-filtering for sensible choices, and report if there isn't
                        // any.
                        var applicableChoices = typeRefs.Where(tr => !tr.Code.StartsWith("http:"))
                                    .Where(tr => ModelInfo.IsInstanceTypeFor(ModelInfo.FhirTypeNameToFhirType(tr.Code).Value, instanceType.Value));

                        // Instance typename must be one of the applicable types in the choice
                        if (applicableChoices.Any())
                        {
                            outcome.Include(validator.ValidateTypeReferences(applicableChoices, instance, state, validateProfiles));
                        }
                        else
                        {
                            var choiceList = String.Join(",", choices.Select(t => "'" + t + "'"));
                            validator.Trace(outcome, $"Type specified in the instance ('{instance.InstanceType}') is not one of the allowed choices ({choiceList})",
                                        Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, instance);
                        }
                    }
                    else
                    {
                        validator.Trace(outcome, $"Instance indicates the element is of type '{instance.InstanceType}', which is not a known FHIR core type.",
                                     Issue.CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE, instance);
                    }
                }
                else if (choices.Count() == 1)
                {
                    if (instanceType is not null)
                    {
                        // Check if instance is of correct type
                        var isCorrectType = ModelInfo.IsInstanceTypeFor(ModelInfo.FhirTypeNameToFhirType(choices.Single()).Value, instanceType.Value);
                        if (!isCorrectType)
                        {
                            validator.Trace(outcome, $"Type specified in the instance ('{instance.InstanceType}') is not of the expected type ('{choices.Single()}')",
                                            Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, instance);
                        }
                    }
                    // Only one type present in list of typerefs, all of the typerefs are candidates
                    outcome.Include(validator.ValidateTypeReferences(typeRefs, instance, state, validateProfiles));
                }
            }
            else
            {
                validator.Trace(outcome, "Cannot determine the data type of this instance.",
                    Issue.CONTENT_ELEMENT_CANNOT_DETERMINE_TYPE, instance);
            }

            return outcome;
        }


        internal static OperationOutcome ValidateTypeReferences(this Validator validator,
            IEnumerable<ElementDefinition.TypeRefComponent> typeRefs, ScopedNode instance, ValidationState state, bool validateProfiles = true)
        {
            //TODO: It's more efficient to do the non-reference types FIRST, since ANY match would be ok,
            //and validating non-references is cheaper
            //TODO: For each choice, we will currently try to resolve the reference. If it fails, you'll get multiple errors and probably
            //better separate the fetching of the instance from the validation, so we do not run the rest of the validation (multiple times!)
            //when a reference cannot be resolved.  (this happens in a choice type where there are multiple references with multiple profiles)

            IEnumerable<Func<OperationOutcome>> validations = typeRefs.Select(tr => createValidatorForTypeRef(validator, instance, tr, validateProfiles, state));
            return validator.Combine("type and targetprofiles", BatchValidationMode.Any, instance, validations);
        }

        private static Func<OperationOutcome> createValidatorForTypeRef(
            Validator validator,
            ScopedNode instance,
            ElementDefinition.TypeRefComponent tr,
            bool validateProfiles,
            ValidationState state)
        {
            return validate;

            OperationOutcome validate()
            {
                OperationOutcome result = new();

                if (validateProfiles)
                {
                    // First, call Validate() for the current element (the reference itself) against the profile
                    result.Add(validator.ValidateInternal(instance, tr.GetTypeProfile(), statedCanonicals: null, statedProfiles: null, state: state));
                }

                // If this is a reference, also validate the reference against the targetProfile
                if (ModelInfo.FhirTypeNameToFhirType(tr.Code) == FHIRAllTypes.Reference)
                    result.Add(validator.ValidateResourceReference(instance, tr, state));

                return result;
            }
        }

        internal static OperationOutcome ValidateResourceReference(
            this Validator validator,
            ScopedNode instance,
            ElementDefinition.TypeRefComponent typeRef,
            ValidationState state)
        {
            var outcome = new OperationOutcome();

            var reference = instance.ParseResourceReference()?.Reference;

            if (reference == null)       // No reference found -> this is always valid
                return outcome;

            // Try to resolve the reference *within* the current instance (Bundle, resource with contained resources) first
            var referencedResource = validator.resolveReference(instance, reference,
                out ElementDefinition.AggregationMode? encounteredKind, outcome);

            // Validate the kind of aggregation.
            // If no aggregation is given, all kinds of aggregation are allowed, otherwise only allow
            // those aggregation types that are given in the Aggregation element
            bool hasAggregation = typeRef.Aggregation != null && typeRef.Aggregation.Count() != 0;
            if (hasAggregation && !typeRef.Aggregation.Any(a => a == encounteredKind))
                validator.Trace(outcome, $"Encountered a reference ({reference}) of kind '{encounteredKind}' which is not allowed", Issue.CONTENT_REFERENCE_OF_INVALID_KIND, instance);

            // Bail out if we are asked to follow an *external reference* when this is disabled in the settings
            if (validator.Settings.ResolveExternalReferences == false && encounteredKind == ElementDefinition.AggregationMode.Referenced)
                return outcome;

            // If we failed to find a referenced resource within the current instance, try to resolve it using an external method
            if (referencedResource == null && encounteredKind == ElementDefinition.AggregationMode.Referenced)
            {
                try
                {
                    referencedResource = validator.ExternalReferenceResolutionNeeded(reference, outcome, instance.Location);
                }
                catch (Exception e)
                {
                    validator.Trace(outcome, $"Resolution of external reference {reference} failed. Message: {e.Message}",
                           Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance);
                }
            }

            // If the reference was resolved (either internally or externally), validate it
            if (referencedResource != null)
            {
                validator.Trace(outcome, $"Starting validation of referenced resource {reference} ({encounteredKind})", Issue.PROCESSING_START_NESTED_VALIDATION, instance);

                // References within the instance are dealt with within the same validator,
                // references to external entities will operate within a new instance of a validator (and hence a new tracking context).
                // In both cases, the outcome is included in the result.
                OperationOutcome childResult;

                // TODO: BRIAN: Check that this TargetProfile.FirstOrDefault() is actually right, or should
                //              we be permitting more than one target profile here.
                if (encounteredKind != ElementDefinition.AggregationMode.Referenced)
                {
#if STU3
                    childResult = validator.ValidateInternal(referencedResource,
                                         typeRef.TargetProfile,
                                         statedProfiles: null,
                                         statedCanonicals: null,
                                         state: state);
#else
                    childResult = validator.validateReferences(reference,
                                                               referencedResource,
                                                               typeRef.TargetProfile,
                                                               state,
                                                               external: false);
#endif
                }
                else
                {
                    var newValidator = validator.NewInstance();
                    var newState = state.NewInstanceScope();
                    newState.Instance.ExternalUrl = reference;

#if STU3
                    childResult = newState.Global.ExternalValidations.Start(reference, typeRef.TargetProfile,
                        () => newValidator.ValidateInternal(referencedResource,
                                                        typeRef.TargetProfile,
                                                        statedProfiles: null,
                                                        statedCanonicals: null,
                                                        state: newState));
#else
                    childResult = newValidator.validateReferences(reference,
                                                                  referencedResource,
                                                                  typeRef.TargetProfile,
                                                                  newState,
                                                                  external: true);
#endif
                }

                // Prefix each path with the referring resource's path to keep the locations
                // interpretable
                foreach (var issue in childResult.Issue)
                {
                    issue.Expression = issue.Expression.Concat(new string[] { instance.Location });
                    // Location is deprecated, but we set this for backwards compatibility
                    issue.Location = issue.Expression.Concat(new string[] { instance.Location });
                }

                outcome.Include(childResult);
            }
            else
                validator.Trace(outcome, $"Cannot resolve reference {reference}", Issue.UNAVAILABLE_REFERENCED_RESOURCE, instance);

            return outcome;
        }


#if !STU3
        private static OperationOutcome validateReferences(
            this Validator validator,
            string reference,
            ITypedElement referencedResource,
            IEnumerable<string> targetProfiles,
            ValidationState state,
            bool external)
        {
            IEnumerable<Func<OperationOutcome>> validations =
                external == false
                    ? targetProfiles.Select(tp => createValidatorForReferenceResource(tp))
                    : targetProfiles.Select(tp => createValidatorForExternalReferenceResource(tp));

            return validator.Combine("references and targetprofiles", BatchValidationMode.Any, referencedResource, validations);

            Func<OperationOutcome> createValidatorForReferenceResource(string targetProfile)
            {
                return () => validator.ValidateInternal(referencedResource,
                                         targetProfile,
                                         statedProfiles: null,
                                         statedCanonicals: null,
                                         state: state);
            }

            Func<OperationOutcome> createValidatorForExternalReferenceResource(string targetProfile)
            {
                return () => state.Global.ExternalValidations.Start(reference, targetProfile,
                            () => validator.ValidateInternal(referencedResource,
                                                        targetProfile,
                                                        statedProfiles: null,
                                                        statedCanonicals: null,
                                                        state: state));
            }
        }
#endif

        private static ITypedElement resolveReference(this Validator validator, ScopedNode instance, string reference, out ElementDefinition.AggregationMode? referenceKind, OperationOutcome outcome)
        {
            var identity = new ResourceIdentity(reference);

            if (identity.Form == ResourceIdentityForm.Undetermined)
            {
                if (!Uri.IsWellFormedUriString(Uri.EscapeDataString(reference), UriKind.RelativeOrAbsolute))
                {
                    validator.Trace(outcome, $"Encountered an unparseable reference ({reference})", Issue.CONTENT_UNPARSEABLE_REFERENCE, instance);
                    referenceKind = null;
                    return null;
                }
            }

            var result = instance.Resolve(reference);

            if (identity.Form == ResourceIdentityForm.Local)
            {
                referenceKind = ElementDefinition.AggregationMode.Contained;
                if (result == null)
                    validator.Trace(outcome, $"Contained reference ({reference}) is not resolvable", Issue.CONTENT_CONTAINED_REFERENCE_NOT_RESOLVABLE, instance);
            }
            else
            {
                if (result != null)
                    referenceKind = ElementDefinition.AggregationMode.Bundled;
                else
                    referenceKind = ElementDefinition.AggregationMode.Referenced;
            }

            return result;
        }
    }
}
