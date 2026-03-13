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
            RuleFor(x => x.FullName).NotNull().MaximumLength(254);
            RuleFor(x => x.Name).NotNull().MaximumLength(254);

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.FirstName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.LastName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.FullName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.Name).MatchesNamePattern(options.NameValidationPattern);
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
