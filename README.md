[![Build Status](https://dev.azure.com/firely/firely-net-sdk/_apis/build/status/FirelyTeam.Hl7.Fhir.Validation.Legacy?repoName=FirelyTeam%2FHl7.Fhir.Validation.Legacy&branchName=develop)](https://dev.azure.com/firely/firely-net-sdk/_build/latest?definitionId=125&repoName=FirelyTeam%2FHl7.Fhir.Validation.Legacy&branchName=develop)

## Introduction ##
In the latest release of the [Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk) (version 5.0), we have removed the FHIR profile validator (`Hl7.Fhir.Validation.Validator`). While we are working on a new and faster validator, we have kept the old validator in the SDK for backwards compatibility. This means that if you are using the new SDK version 5.0 and want to avoid potential regression, you can use the following packages.:
- [Hl7.Fhir.Validation.Legacy.STU3](https://www.nuget.org/packages/Hl7.Fhir.Validation.Legacy.STU3)
- [Hl7.Fhir.Validation.Legacy.R4](https://www.nuget.org/packages/Hl7.Fhir.Validation.Legacy.R4)
- [Hl7.Fhir.Validation.Legacy.R4B](https://www.nuget.org/packages/Hl7.Fhir.Validation.Legacy.R4B)
- [Hl7.Fhir.Validation.Legacy.R5](https://www.nuget.org/packages/Hl7.Fhir.Validation.Legacy.R5)

## Support 
We will not longer support this validator.	
