using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Validators
{
    public class PersonalDataValidatorTests
    {
        // Clients translate the rule by its code; without one, FluentValidation reports the generic "PredicateValidator"
        [Fact]
        public void PersonalData_WithInvalidEmail_ReportsInvalidEmailCode()
        {
            var validator = new PersonalDataValidator(Options.Create(new InputValidationOptions()));
            var personalData = new PersonalData { Email = "john@example" };

            var result = validator.TestValidate(personalData);

            result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(ModuleConstants.ValidationErrorCodes.InvalidEmail);
        }
    }
}
