using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Validators
{
    public class AccountValidatorTests
    {
        [Fact]
        public void ValidAccount_Passes()
        {
            var validator = CreateValidator();
            var account = new Account { UserName = "john.doe", Email = "john@example.com", Password = "P@ssw0rd" };

            var result = validator.TestValidate(account);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Account_WithHtmlInUserName_Fails()
        {
            var validator = CreateValidator();
            var account = new Account { UserName = "user<script>", Email = "john@example.com", Password = "P@ssw0rd" };

            var result = validator.TestValidate(account);

            result.ShouldHaveValidationErrorFor(x => x.UserName);
        }

        [Fact]
        public void Account_WithHtmlInUserName_DisabledCheck_Passes()
        {
            var options = new InputValidationOptions { EnableNoHtmlTagsValidation = false };
            var validator = CreateValidator(options);
            var account = new Account { UserName = "user<script>", Email = "john@example.com", Password = "P@ssw0rd" };

            var result = validator.TestValidate(account);

            result.ShouldNotHaveValidationErrorFor(x => x.UserName);
        }

        [Fact]
        public void Account_WithTooLongUserName_Fails()
        {
            var validator = CreateValidator();
            var account = new Account { UserName = new string('a', 257), Email = "john@example.com", Password = "P@ssw0rd" };

            var result = validator.TestValidate(account);

            result.ShouldHaveValidationErrorFor(x => x.UserName);
        }

        private static AccountValidator CreateValidator(InputValidationOptions options = null)
        {
            options ??= new InputValidationOptions();
            return new AccountValidator(Options.Create(options));
        }
    }
}
