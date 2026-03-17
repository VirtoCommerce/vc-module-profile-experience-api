using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Validators
{
    public class NewContactValidatorTests
    {
        [Fact]
        public void ValidContact_WithDefaultOptions_Passes()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();

            var result = validator.TestValidate(contact);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Contact_WithScriptInFirstName_Fails()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();
            contact.FirstName = "Paul<script src=//ooo.mn/></script>";

            var result = validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(x => x.FirstName);
        }

        [Fact]
        public void Contact_WithScriptInLastName_Fails()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();
            contact.LastName = "Bok<script>alert(1)</script>";

            var result = validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(x => x.LastName);
        }

        [Fact]
        public void Contact_WithNumbersInName_FailsNamePattern()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();
            contact.FirstName = "Paul123";

            var result = validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(x => x.FirstName);
        }

        [Fact]
        public void Contact_WithInternationalName_Passes()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();
            contact.FirstName = "José";
            contact.LastName = "O'Brien-Müller";

            var result = validator.TestValidate(contact);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Contact_WithExcessivelyLongName_Fails()
        {
            var validator = CreateValidator();
            var contact = CreateValidContact();
            contact.FirstName = new string('A', 129);

            var result = validator.TestValidate(contact);

            result.ShouldHaveValidationErrorFor(x => x.FirstName);
        }

        [Fact]
        public void Contact_WithDisabledNamePattern_AllowsAnything()
        {
            var options = new InputValidationOptions { NameValidationPattern = null };
            var validator = CreateValidator(options);
            var contact = CreateValidContact();
            contact.FirstName = "Paul123<html>";
            contact.LastName = "Test";

            var result = validator.TestValidate(contact);

            result.ShouldNotHaveAnyValidationErrors();
        }

        private static NewContactValidator CreateValidator(InputValidationOptions options = null)
        {
            options ??= new InputValidationOptions();
            return new NewContactValidator(Options.Create(options));
        }

        private static Contact CreateValidContact()
        {
            var contact = AbstractTypeFactory<Contact>.TryCreateInstance();
            contact.FirstName = "John";
            contact.LastName = "Doe";
            return contact;
        }
    }
}
