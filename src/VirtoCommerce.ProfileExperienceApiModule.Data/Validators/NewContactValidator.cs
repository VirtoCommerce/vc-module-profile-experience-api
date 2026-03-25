using FluentValidation;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class NewContactValidator : AbstractValidator<Contact>
    {
        public NewContactValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;
            var hasNamePattern = !string.IsNullOrWhiteSpace(options.NameValidationPattern);

            RuleFor(x => x.FirstName).NotNull().NotEmpty().MaximumLength(128);
            RuleFor(x => x.LastName).NotNull().NotEmpty().MaximumLength(128);

            RuleForEach(x => x.Phones).MaximumLength(64);

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.FirstName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.LastName).MatchesNamePattern(options.NameValidationPattern);
            });

            When(_ => options.EnableNoHtmlTagsValidation, () =>
            {
                RuleForEach(x => x.Phones).NoHtmlTags();
            });

            RuleSet("strict", () =>
            {
                RuleFor(x => x).Custom((member, context) =>
                {
                    if (member.Addresses.IsNullOrEmpty())
                    {
                        context.AddFailure(ErrorDescriber.AddressesMissingError(member));
                    }
                });
            });
        }
    }
}
