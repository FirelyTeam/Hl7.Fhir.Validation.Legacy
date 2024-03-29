﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System.Collections.Generic;
using System.Linq;

namespace Hl7.Fhir.Validation
{
    internal class ProfileDiscriminator : PathBasedDiscriminator
    {
        public ProfileDiscriminator(IEnumerable<string> profiles, string path, Validator validator) : base(path)
        {
            Validator = validator;
            Profiles = profiles?.ToArray() ?? throw new System.ArgumentNullException(nameof(profiles));
        }

        public readonly Validator Validator;
        public readonly string[] Profiles;

        protected override bool MatchInternal(ITypedElement instance, ValidationState state) =>
            Profiles.Any(profile => validates(instance, profile, state));

        private bool validates(ITypedElement instance, string profile, ValidationState state)
        {
            var validator = Validator.NewInstance();
            var result = validator.ValidateInternal(instance,
                                            declaredTypeProfile: null,
                                            statedCanonicals: new[] { profile },
                                            statedProfiles: null,
                                            state: state);
            return result.Success;
        }
    }
}
