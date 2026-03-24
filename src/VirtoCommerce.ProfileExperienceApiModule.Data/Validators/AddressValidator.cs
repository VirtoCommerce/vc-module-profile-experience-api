using FluentValidation;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class AddressValidator : AbstractValidator<Address>
    {
        public AddressValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;
            var hasNamePattern = !string.IsNullOrWhiteSpace(options.NameValidationPattern);
            var hasOrgNamePattern = !string.IsNullOrWhiteSpace(options.OrganizationNameValidationPattern);

            RuleFor(x => x.Email)
                .MaximumLength(256)
                .Must(x => string.IsNullOrEmpty(x) || x.IsValidEmail())
                .WithMessage("Invalid email format in the address");

            RuleFor(x => x.FirstName).MaximumLength(128);
            RuleFor(x => x.LastName).MaximumLength(128);
            RuleFor(x => x.Name).MaximumLength(256);
            RuleFor(x => x.Organization).MaximumLength(256);
            RuleFor(x => x.City).MaximumLength(128);
            RuleFor(x => x.Line1).MaximumLength(256);
            RuleFor(x => x.Line2).MaximumLength(256);
            RuleFor(x => x.Phone).MaximumLength(64);
            RuleFor(x => x.Description).MaximumLength(1024);

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.FirstName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.LastName).MatchesNamePattern(options.NameValidationPattern);
            });

            When(_ => hasOrgNamePattern, () =>
            {
                RuleFor(x => x.Organization).MatchesNamePattern(options.OrganizationNameValidationPattern);
            });

            When(_ => options.EnableNoHtmlTagsValidation, () =>
            {
                RuleFor(x => x.City).NoHtmlTags();
                RuleFor(x => x.Line1).NoHtmlTags();
                RuleFor(x => x.Line2).NoHtmlTags();
                RuleFor(x => x.Phone).NoHtmlTags();
            });

            When(_ => options.EnableScriptInjectionValidation, () =>
            {
                RuleFor(x => x.Description).NoScriptInjection();
            });
        }
    }
}
