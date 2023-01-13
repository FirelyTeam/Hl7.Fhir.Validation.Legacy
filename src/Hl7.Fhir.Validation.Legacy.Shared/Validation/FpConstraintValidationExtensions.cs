﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using System;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Hl7.Fhir.Validation
{
    internal class CompiledConstraintAnnotation
    {
        public CompiledExpression Expression;
    }

    internal static class FpConstraintValidationExtensions
    {
        public static OperationOutcome ValidateFp(this Validator v, string structureDefinitionUrl, ElementDefinition definition, ScopedNode instance)
        {
            var outcome = new OperationOutcome();

            if (!definition.Constraint.Any()) return outcome;
            if (v.Settings.SkipConstraintValidation) return outcome;

            foreach (var constraintElement in definition.Constraint)
            {
                // 20190703 Issue 447 - rng-2 is incorrect in DSTU2 and STU3. EK
                // should be removed from STU3/R4 once we get the new normative version
                // of FP up, which could do comparisons between quantities.
                if (v.Settings.ConstraintsToIgnore.Contains(constraintElement.Key)) continue;

#if !STU3
                if (constraintElement.GetBoolExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice") == true)
                    if (v.Settings.ConstraintBestPracticesSeverity == ConstraintBestPracticesSeverity.Error)
                        constraintElement.Severity = ConstraintSeverity.Error;
                    else if (v.Settings.ConstraintBestPracticesSeverity == ConstraintBestPracticesSeverity.Warning)
                        constraintElement.Severity = ConstraintSeverity.Warning;
#endif

                // The following constraints will be repaired in R4B - pre-apply it for other R3+ here as well.
                constraintElement.Expression = constraintElement switch
                {
                    {
                        Key: "ref-1", Expression: @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids'))" or
                                                  @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %resource.contained.id.trace('ids'))" or
                                                  @"reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource)"
                    }
                                               => @"reference.exists() implies (reference.startsWith('#').not() or (reference.substring(1).trace('url') in %rootResource.contained.id.trace('ids')) or (reference='#' and %rootResource!=%resource))",

                    // matches should be applied on the whole string:
                    { Key: "eld-19", Expression: @"path.matches('[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*')" }
                                              => @"path.matches('^[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\.[^\\s\\.,:;\\\'""\\/|?!@#$%&*()\\[\\]{}]{1,64}(\\[x\\])?(\\:[^\\s\\.]+)?)*$')",
                    { Key: "eld-20", Expression: @"path.matches('[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*')" }
                                              => @"path.matches('^[A-Za-z][A-Za-z0-9]*(\\.[a-z][A-Za-z0-9]*(\\[x])?)*$')",
                    {
                        Key: "sdf-0", Expression: @"name.matches('[A-Z]([A-Za-z0-9_]){0,254}')" or
                                                  @"name.exists() implies name.matches('[A-Z]([A-Za-z0-9_]){0,254}')"
                    }
                                               => @"name.exists() implies name.matches('^[A-Z]([A-Za-z0-9_]){0,254}$')",

                    // do not use $this (see https://jira.hl7.org/browse/FHIR-37761)
                    { Key: "sdf-24", Expression: @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                              => @"element.where(type.code='Reference' and id.endsWith('.reference') and type.targetProfile.exists() and id.substring(0,$this.id.length()-10) in %context.element.where(type.code='CodeableReference').id).exists().not()",
                    { Key: "sdf-25", Expression: @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()" }
                                               => @"element.where(type.code='CodeableConcept' and id.endsWith('.concept') and binding.exists() and id.substring(0,$this.id.length()-8) in %context.element.where(type.code='CodeableReference').id).exists().not()",

                    // correct datatype in expression:
                    { Key: "que-7", Expression: @"operator = 'exists' implies (answer is Boolean)" }
                                             => @"operator = 'exists' implies (answer is boolean)",

                    // correct specialization = 'derivation' to derivation= 'specialization' ( see https://jira.hl7.org/browse/FHIR-39166)
                    { Key: "sdf-29", Expression: @"((kind in 'resource' | 'complex-type') and (specialization = 'derivation')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()" }
                                            => @"((kind in 'resource' | 'complex-type') and (derivation= 'specialization')) implies differential.element.where((min != 0 and min != 1) or (max != '1' and max != '*')).empty()",

                    var ce => ce.Expression
                };

                bool success = false;

                try
                {
                    var compiled = getExecutableConstraint(v, outcome, instance, constraintElement);
                    success = compiled.IsTrue(instance,
                        new FhirEvaluationContext(instance)
                        { ElementResolver = callExternalResolver });
                }
                catch (Exception e)
                {
                    v.Trace(outcome, $"Evaluation of FhirPath for constraint '{constraintElement.Key}' failed: {e.Message}",
                                    Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION, instance);
                }

                if (!success)
                {
                    var text = "Instance failed constraint " + constraintElement.ConstraintDescription();
                    var issue = constraintElement.Severity == ConstraintSeverity.Error ?
                        Issue.CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT : Issue.CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT;

                    // just use the constraint description in the error message, as this is to explain the issue
                    // to a human, the code for the error should be in the coding
                    var outcomeIssue = new OperationOutcome.IssueComponent()
                    {
                        Severity = issue.Severity,
                        Code = issue.Type,
                        Details = issue.ToCodeableConcept(text),
                        Diagnostics = constraintElement.GetFhirPathConstraint(), // Putting the fhirpath expression of the invariant in the diagnostics
                        Expression = new string[] { instance.Location },
                        // Location is deprecated, but we set this for backwards compatibility
                        Location = new string[] { instance.Location }
                    };
                    outcomeIssue.Details.Coding.Add(new Coding(structureDefinitionUrl, constraintElement.Key, constraintElement.Human));
                    outcome.AddIssue(outcomeIssue);
                }
            }

            return outcome;

            ITypedElement callExternalResolver(string url)
            {
                OperationOutcome o = new OperationOutcome();
                var result = v.ExternalReferenceResolutionNeeded(url, o, "dummy");

                if (o.Success && result != null) return result;

                return null;
            }
        }


        private static CompiledExpression getExecutableConstraint(Validator v, OperationOutcome outcome, ITypedElement instance,
                        ElementDefinition.ConstraintComponent constraintElement)
        {
            var compiledExpression = constraintElement.Annotation<CompiledConstraintAnnotation>()?.Expression;

            if (compiledExpression == null)
            {
                var fpExpressionText = constraintElement.GetFhirPathConstraint();

                if (fpExpressionText != null)
                {
                    try
                    {
                        compiledExpression = v.FpCompiler.Compile(fpExpressionText);
                        constraintElement.SetAnnotation(new CompiledConstraintAnnotation { Expression = compiledExpression });

                    }
                    catch (Exception e)
                    {
                        v.Trace(outcome, $"Compilation of FhirPath for constraint '{constraintElement.Key}' failed: {e.Message}",
                                        Issue.PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION, instance);
                    }
                }
                else
                    v.Trace(outcome, $"Encountered an invariant ({constraintElement.Key}) that has no FhirPath expression, skipping validation of this constraint",
                            Issue.UNSUPPORTED_CONSTRAINT_WITHOUT_FHIRPATH, instance);
            }

            return compiledExpression;
        }
    }
}
