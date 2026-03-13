using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class OrganizationValidator : AbstractValidator<Organization>
    {
        public OrganizationValidator(IOptions<InputValidationOptions> validationOptions)
        {
            var options = validationOptions.Value;
            var hasNamePattern = !string.IsNullOrWhiteSpace(options.NameValidationPattern);

            RuleFor(x => x.Name).NotNull().NotEmpty().MaximumLength(256);
            RuleFor(x => x.Description).MaximumLength(1024);

            When(_ => hasNamePattern, () =>
            {
                RuleFor(x => x.Name).MatchesNamePattern(options.NameValidationPattern);
            });

            When(_ => options.EnableScriptInjectionValidation, () =>
            {
                RuleFor(x => x.Description).NoScriptInjection();
            });
        }

        public override Task<ValidationResult> ValidateAsync(ValidationContext<Organization> context, CancellationToken cancellation = new CancellationToken())
        {
            if (context.InstanceToValidate == null)
            {
                var result = new ValidationResult();
                return Task.FromResult(result);
            }

            return base.ValidateAsync(context, cancellation);
        }
    }
}
