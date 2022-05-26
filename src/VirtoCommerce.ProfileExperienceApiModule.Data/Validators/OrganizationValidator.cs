using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Validators
{
    public class OrganizationValidator:  AbstractValidator<Organization>
    {
        public OrganizationValidator()
        {
            RuleFor(x => x.Name).NotNull().NotEmpty();
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
