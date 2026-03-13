using FluentValidation;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class AccountValidator : AbstractValidator<Account>
    {
        public AccountValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;

            RuleFor(x => x.UserName).NotNull().NotEmpty().MaximumLength(256);
            RuleFor(x => x.Password).NotNull().NotEmpty().MaximumLength(128);
            RuleFor(x => x.Email)
                .NotNull()
                .NotEmpty()
                .MaximumLength(256)
                .Must(x => x.IsValidEmail())
                .WithMessage("Invalid email format in the account");

            When(_ => options.EnableNoHtmlTagsValidation, () =>
            {
                RuleFor(x => x.UserName).NoHtmlTags();
            });
        }
    }
}
