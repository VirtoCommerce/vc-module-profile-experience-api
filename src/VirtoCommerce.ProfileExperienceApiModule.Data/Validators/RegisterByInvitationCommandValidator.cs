using FluentValidation;
using Microsoft.Extensions.Options;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class RegisterByInvitationCommandValidator : AbstractValidator<RegisterByInvitationCommand>
    {
        public RegisterByInvitationCommandValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;
            var hasNamePattern = !string.IsNullOrWhiteSpace(options.NameValidationPattern);

            RuleFor(x => x.FirstName).NotNull().NotEmpty().MaximumLength(128);
            RuleFor(x => x.LastName).NotNull().NotEmpty().MaximumLength(128);
            RuleFor(x => x.Username).NotNull().NotEmpty().MaximumLength(256);
            RuleFor(x => x.Phone).MaximumLength(64);
            RuleFor(x => x.Password).NotNull().NotEmpty().MaximumLength(128);

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.FirstName).MatchesNamePattern(options.NameValidationPattern);
                RuleFor(x => x.LastName).MatchesNamePattern(options.NameValidationPattern);
            });

            When(_ => options.EnableNoHtmlTagsValidation, () =>
            {
                RuleFor(x => x.Username).NoHtmlTags();
                RuleFor(x => x.Phone).NoHtmlTags();
            });
        }
    }
}
