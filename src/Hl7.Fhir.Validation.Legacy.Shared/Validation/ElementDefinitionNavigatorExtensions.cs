/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

#nullable enable

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System;

namespace Hl7.Fhir.Validation
{
    internal static class ElementDefinitionNavigatorExtensions
    {
        internal static string GetFhirPathConstraint(this ElementDefinition.ConstraintComponent cc)
        {
            // This was required for 3.0.0, but was rectified in the 3.0.1 technical update
            //if (cc.Key == "ele-1")
            //    return "(children().count() > id.count()) | hasValue()";
            return cc.Expression;
        }

        internal static string ConstraintDescription(this ElementDefinition.ConstraintComponent cc)
        {
            var desc = cc.Key;

            if (cc.Human != null)
                desc += " \"" + cc.Human + "\"";

            return desc;
        }

        /// <summary>
        /// Resolve a the contentReference in a navigator and returns a navigator that is located on the target of the contentReference.
        /// </summary>
        /// <remarks>The current navigator must be located at an element that contains a contentReference.</remarks>
        public static bool TryFollowContentReference(this ElementDefinitionNavigator sourceNavigator, Func<string, StructureDefinition?> resolver, out ElementDefinitionNavigator? targetNavigator)
        {
            targetNavigator = null;

            var reference = sourceNavigator.Current.ContentReference;
            if (reference is null) return false;

            var profileRef = TempProfileReference.Parse(reference);

            if (profileRef.IsAbsolute && profileRef.CanonicalUrl != sourceNavigator.StructureDefinition.Url)
            {
                // an external reference (e.g. http://hl7.org/fhir/StructureDefinition/Questionnaire#Questionnaire.item)

                var profile = resolver(profileRef.CanonicalUrl!);
                if (profile is null) return false;
                targetNavigator = ElementDefinitionNavigator.ForSnapshot(profile);
            }
            else
            {
                // a local reference
                targetNavigator = sourceNavigator.ShallowCopy();
            }

            return targetNavigator.JumpToNameReference("#" + profileRef.ElementName);
        }


        /// <summary>Represents a reference to an element type profile.</summary>
        /// <remarks>Useful to parse complex profile references of the form "canonicalUrl#Type.elementName".
        /// 
        /// TODO BIG_COMMON: this should be refactored when we have a good solution for Canonical </remarks>
        /// 
        private class TempProfileReference
        {
            private TempProfileReference(string url)
            {
                if (url == null) { throw new ArgumentNullException(nameof(url)); }

                var parts = url.Split('#');

                if (parts.Length == 1)
                {
                    // Just the canonical, no '#' present
                    CanonicalUrl = parts[0];
                    ElementName = null;
                }
                else
                {
                    // There's a '#', so both or just the element are present
                    CanonicalUrl = parts[0].Length > 0 ? parts[0] : null;
                    ElementName = parts[1].Length > 0 ? parts[1] : null;
                }
            }

            /// <summary>Initialize a new <see cref="TempProfileReference"/> instance from the specified url.</summary>
            /// <param name="url">A resource reference to a profile.</param>
            /// <returns>A new <see cref="TempProfileReference"/> structure.</returns>
            public static TempProfileReference Parse(string url) => new(url);

            /// <summary>Returns the canonical url of the profile.</summary>
            public string? CanonicalUrl { get; }

            /// <summary>Returns an optional profile element name, if included in the reference.</summary>
            public string? ElementName { get; }

            /// <summary>Returns <c>true</c> if the profile reference includes an element name, <c>false</c> otherwise.</summary>
            public bool IsComplex => ElementName is not null;

            /// <summary>
            /// Returns <c>true</c> of the profile reference includes a canonical url.
            /// </summary>
            public bool IsAbsolute => CanonicalUrl is not null;
        }

    }
}