using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Validators
{
    public class AddressValidatorTests
    {
        // Clients translate the rule by its code; without one, FluentValidation reports the generic "PredicateValidator"
        [Fact]
        public void Address_WithInvalidEmail_ReportsInvalidEmailCode()
        {
            var validator = new AddressValidator(Options.Create(new InputValidationOptions()));
            var address = new Address { Email = "john@example" };

            var result = validator.TestValidate(address);

            result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(ModuleConstants.ValidationErrorCodes.InvalidEmail);
        }
    }
}
