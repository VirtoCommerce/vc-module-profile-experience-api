using FluentValidation;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class PersonalDataValidator : AbstractValidator<PersonalData>
    {
        public PersonalDataValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;
            var hasNamePattern = !string.IsNullOrWhiteSpace(options.NameValidationPattern);

            RuleFor(x => x.FirstName).MaximumLength(128);
            RuleFor(x => x.LastName).MaximumLength(128);
            RuleFor(x => x.MiddleName).MaximumLength(128);
            RuleFor(x => x.FullName).MaximumLength(254);
            RuleFor(x => x.Email)
                .MaximumLength(256)
                .Must(x => string.IsNullOrEmpty(x) || x.IsValidEmail())
                .WithMessage("Invalid email format");

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.FirstName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.LastName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.MiddleName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.FullName).MatchesNamePattern(options.NameValidationPattern);
            });
        }
    }
}
